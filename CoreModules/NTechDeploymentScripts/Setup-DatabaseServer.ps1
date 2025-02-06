Param(
  [parameter(mandatory=$true)][string]$file,
  [parameter(mandatory=$true)][string]$secretsFile
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction']='Stop'

Find-PackageProvider -Name 'Nuget' -ForceBootstrap -IncludeDependencies | Out-Null

if (!(Get-Module -ListAvailable -Name SqlServer)) {
    try {
        Write-Host 'Installing SqlServer powershell module'
        Install-Module -Name SqlServer -AllowClobber -Confirm:$False -Force  
    }
    catch [Exception] {
        $_.message 
        exit
    }
}

$f = Get-Content -Path $file | ConvertFrom-Json
$secrets = (Get-Content -Path $secretsFile | ConvertFrom-Json).secrets

function ReplaceSecretsInString($v) {
    $r = $v
    foreach($s in $secrets) {
        $sn = '{secrets.' + $s.key + '}'
        $r = $r.replace($sn, $s.value)
    }
    return $r
}

Write-Host 'Checking for instance ' $f.servers.sqlServer.name

$dbInstance = (Get-SqlInstance -ServerInstance $f.servers.sqlServer.name)
if(!$dbInstance) {
    Write-Host 'Database instance missing!'
    exit
}

Write-Host 'Checking version'

if($dbInstance.Version.Major -ge 11) {
    $v = $dbInstance.Version.Major
    Write-Host "Database version $v >= 11: Ok"
} else {
    Write-Host "Database version $v >= 11: Failed. This version is not supported."
    exit
}

Write-Host 'Checking edition'
$version = ($dbInstance | Invoke-SqlCmd -query "select @@version").Column1

if($version.contains('Standard Edition')) {
    Write-Host 'Standard edition: Ok'
} elseif($version.contains('Enterprise Edition')) {
    Write-Host 'Enterprise edition: Ok'
}  elseif($version.contains('Developer Edition')) {
    Write-Host 'Developer edition: Ok'
} elseif($version.contains('Express')) {
    Write-Host 'Express edition: Failed'
    Write-Host 'Express edition is known not to work. Please upgrade to at least standard edition.'
    exit
} else {
    Write-Host 'Unknown edition encounted. Manual intervention required to see it it works or not'
    Write-Host $version
    exit
}

if($f.servers.sqlServer.appPoolUserDomainUserName) {
    Write-Host 'Setting up app pool user' $f.servers.sqlServer.appPoolUserDomainUserName

    $u = $dbInstance | Get-SqlLogin | Where-Object {$_.Name -eq $f.servers.sqlServer.appPoolUserDomainUserName }
    If($u) {
        Write-Host 'App pool user: Ok'
    } else {
        Write-Host 'Adding app pool user'
        $un = $f.servers.sqlServer.appPoolUserDomainUserName
        $sql = 'USE [master]'
        $sql += " ;CREATE LOGIN [$un] FROM WINDOWS"
        $sql += " ;ALTER SERVER ROLE [sysadmin] ADD MEMBER [$un]"
        $output = ($dbInstance | Invoke-SqlCmd -query $sql)
        $u = $dbInstance | Get-SqlLogin | Where-Object {$_.Name -eq $f.servers.sqlServer.appPoolUserDomainUserName }
        if(!$u) {
            Write-Host 'Could not add app pool user'
            exit
        } else {
            Write-Host 'App pool user added'
        }
    }
}

##Firewall
Write-Host 'Checking windows firewall'
$fw = Get-NetFirewallRule | Where-Object {$_.DisplayName -eq 'Naktergal allow sql server port 1433' }
if(!$fw) {
    Write-Host 'Port 1433 appears to be closed. Adding a rule to open it'
    New-NetFirewallRule -DisplayName 'Naktergal allow sql server port 1433' -Direction Inbound -Protocol TCP -LocalPort 1433 -Action allow | Out-Null
    Write-Host 'Rule added'
} else {
    Write-Host 'Windows firewall port 1433 open: Ok'
}

$supportedModules = @('nUser', 'nAudit', 'nCredit', 'nPreCredit', 'nCreditReport', 'nCustomer', 'nDataWarehouse', 'nScheduler', 'nTest')

##Create databases
function Get-PropOrNull {
    param($thing, [string]$prop)
    Try {
        $thing.$prop
    } Catch {
    }
}

function Get-ModuleFriendlyName($moduleName) {
    if($moduleName[0] -eq 'n') {
        return $moduleName.Substring(1).ToLower()
    } else {
        return $moduleName.ToLower()
    }
}

function Get-CreateDatabaseScript($databaseName, $dataFile, $logFile) {
    $alters = @('COMPATIBILITY_LEVEL = 120',
    'AUTO_SHRINK OFF',
    'AUTO_UPDATE_STATISTICS ON',
    'ALLOW_SNAPSHOT_ISOLATION ON',
    'READ_COMMITTED_SNAPSHOT ON', #Down to here are know required. The rest are defaults from our working copies but we dont actually know if they matter.
    'NUMERIC_ROUNDABORT OFF',
    'QUOTED_IDENTIFIER OFF',
    'CURSOR_CLOSE_ON_COMMIT OFF',
    'CURSOR_DEFAULT GLOBAL',
    'CONCAT_NULL_YIELDS_NULL OFF',
    'ANSI_NULL_DEFAULT OFF',
    'ANSI_NULLS OFF',
    'ANSI_PADDING OFF',
    'ANSI_WARNINGS OFF',
    'ARITHABORT OFF',
    'AUTO_CLOSE OFF',
    'RECURSIVE_TRIGGERS OFF',
    'DISABLE_BROKER',
    'AUTO_UPDATE_STATISTICS_ASYNC OFF',
    'DATE_CORRELATION_OPTIMIZATION OFF',
    'TRUSTWORTHY OFF',
    'PARAMETERIZATION SIMPLE',
    'HONOR_BROKER_PRIORITY OFF',
    'PAGE_VERIFY CHECKSUM',
    'DB_CHAINING OFF',
    'FILESTREAM(NON_TRANSACTED_ACCESS = OFF)',
    'TARGET_RECOVERY_TIME = 0 SECONDS',
    'DELAYED_DURABILITY = DISABLED'
    )
    if($f.isproduction -eq $True) {
        $alters += 'RECOVERY FULL'
    } else {
        $alters += 'RECOVERY SIMPLE'
    }

    foreach($a in $alters) {
        $alterStatement += [Environment]::NewLine + "ALTER DATABASE [$databaseName] SET $a"
    }

    $createDatabasePattern = 
@'
USE MASTER
CREATE DATABASE [{{DATABASE_NAME}}] 
            ON PRIMARY (NAME = N'PrimaryRowData', FILENAME='{{DATA_FILENAME}}', MAXSIZE = UNLIMITED, FILEGROWTH = 10%) 
            LOG ON (NAME = N'PrimaryLogData', FILENAME='{{LOG_FILENAME}}', MAXSIZE = 2048GB , FILEGROWTH = 10%)
            COLLATE Finnish_Swedish_CI_AS
{{ALTER_STATEMENTS}}            
'@
    return $createDatabasePattern.Replace('{{DATABASE_NAME}}', $databaseName).Replace('{{DATA_FILENAME}}', $dataFile).Replace('{{LOG_FILENAME}}', $logFile).replace('{{ALTER_STATEMENTS}}', $alterStatement)
}

$databaseRootPath = $f.servers.sqlServer.databasesPath

if (!(Test-Path $databaseRootPath)) {
    New-Item -ItemType Directory -Force -Path $databaseRootPath | Out-Null 
}

Write-Host 'Creating databases'

$databaseNames = @()
foreach($moduleName in $supportedModules) {
    $m = Get-PropOrNull $f.modules $moduleName

    if($m) {
        $friendlyName = Get-ModuleFriendlyName $moduleName
        $databaseName = $f.conventions.databaseNamePattern.replace('{moduleFriendlyName}', $friendlyName)

        $databaseNames += $databaseName
        $dataFile = Join-Path -Path $databaseRootPath -ChildPath "$databaseName.mdf"
        $logFile = Join-Path -Path $databaseRootPath -ChildPath "$databaseName.ldf"
        $q = Get-CreateDatabaseScript $databaseName $dataFile $logFile
        Write-Host "Creating database $databaseName"
        $dbInstance | Invoke-SqlCmd -query $q | Write-Host
    }
}

Write-Host 'Creating scheduler timeslot jobs'

function Get-CreateTimeslotPowershellScript($timeslotName) {
    $scriptPattern = 
@'
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction']='Stop'

$tokenRequest = @{
    client_id = 'nTechSystemUser'
    client_secret = 'nTechSystemUser'
    scope = 'nTech1'
    username = '{{USER_NAME}}'
    password = '{{PASSWORD}}'
    grant_type = 'password'
}
$tokenResponse = Invoke-WebRequest -Body $tokenRequest -Method 'POST' -Uri '{{TOKEN_URL}}' -TimeoutSec 120 -UseBasicParsing | ConvertFrom-Json
$accessToken = $tokenResponse.access_token

$callRequest = @{
    name = '{{TIMESLOT_NAME}}'
} | ConvertTo-Json

$callHeaders = @{
    'Authorization' = "Bearer $accessToken"
}

Invoke-WebRequest -ContentType 'application/json' -Body $callRequest -Method 'POST' -Uri '{{TIMESLOT_URL}}' -Headers $callHeaders -TimeoutSec 14400 -UseBasicParsing
'@
    $un = $f.servers.sqlServer.scheduledJobsAutomationUserName
    $pw = ReplaceSecretsInString $f.servers.sqlServer.scheduledJobsAutomationPassword
    $tokenUrl = New-Object -TypeName System.Uri -ArgumentList @([System.Uri]$f.conventions.appurlpattern.replace('{moduleFriendlyName}', (Get-ModuleFriendlyName 'nUser')), 'id/connect/token')
    $timeslotUrl = New-Object -TypeName System.Uri -ArgumentList @([System.Uri]$f.conventions.appurlpattern.replace('{moduleFriendlyName}', (Get-ModuleFriendlyName 'nScheduler')), 'Api/TriggerTimeslot')

    return $scriptPattern.replace('{{USER_NAME}}', $un).replace('{{PASSWORD}}', $pw).replace('{{TOKEN_URL}}', $tokenUrl).replace('{{TIMESLOT_NAME}}', $timeslotName).replace('{{TIMESLOT_URL}}', $timeslotUrl).replace("'", "''")
}

function Get-CreateTimeslotScript($userName, $timeslotName, $time) {
    $timeSlotjobPattern = 
@'
    USE [msdb]
    GO
    
    BEGIN TRANSACTION
    DECLARE @ReturnCode INT
    SELECT @ReturnCode = 0
    
    IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name=N'[Naktergal (local)]' AND category_class=1)
    BEGIN
    EXEC @ReturnCode = msdb.dbo.sp_add_category @class=N'JOB', @type=N'LOCAL', @name=N'[Naktergal (local)]'
    IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
    
    END
    
    DECLARE @jobId BINARY(16)
    EXEC @ReturnCode =  msdb.dbo.sp_add_job @job_name=N'Nakergal scheduler: {{TIMESLOT_NAME}}', 
            @enabled=0, 
            @notify_level_eventlog=0, 
            @notify_level_email=0, 
            @notify_level_netsend=0, 
            @notify_level_page=0, 
            @delete_level=0, 
            @description=N'No description available.', 
            @category_name=N'[Naktergal (local)]', 
            @owner_login_name=N'{{DATABASE_USER_NAME}}', @job_id = @jobId OUTPUT
    IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback

    EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'Run', 
            @step_id=1, 
            @cmdexec_success_code=0, 
            @on_success_action=1, 
            @on_success_step_id=0, 
            @on_fail_action=2, 
            @on_fail_step_id=0, 
            @retry_attempts=0, 
            @retry_interval=0, 
            @os_run_priority=0, @subsystem=N'PowerShell', 
            @command=N'{{POWERSHELL_SCRIPT}}', 
            @database_name=N'master', 
            @flags=0
    IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
    EXEC @ReturnCode = msdb.dbo.sp_update_job @job_id = @jobId, @start_step_id = 1
    IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
    EXEC @ReturnCode = msdb.dbo.sp_add_jobschedule @job_id=@jobId, @name=N'NaktergalTimeslot{{TIMESLOT_NAME}}', 
            @enabled=1, 
            @freq_type=8, 
            @freq_interval=127, 
            @freq_subday_type=1, 
            @freq_subday_interval=0, 
            @freq_relative_interval=0, 
            @freq_recurrence_factor=1, 
            @active_start_date=20191202, 
            @active_end_date=99991231, 
            @active_start_time={{START_TIME}}, 
            @active_end_time=235959
    IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
    EXEC @ReturnCode = msdb.dbo.sp_add_jobserver @job_id = @jobId, @server_name = N'(local)'
    IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
    COMMIT TRANSACTION
    GOTO EndSave
    QuitWithRollback:
        IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION
    EndSave:
    
    GO
'@
    $powershellScript = Get-CreateTimeslotPowershellScript $timeslotName
    return $timeSlotjobPattern.replace('{{DATABASE_USER_NAME}}', $userName).replace('{{TIMESLOT_NAME}}', $timeslotName).replace('{{START_TIME}}', $time).replace('{{POWERSHELL_SCRIPT}}', $powershellScript)
}

$dbInstance | Invoke-SqlCmd -query (Get-CreateTimeslotScript $f.servers.sqlServer.scheduledJobsUserDomainUserName 'Morning' '50000')
$dbInstance | Invoke-SqlCmd -query (Get-CreateTimeslotScript $f.servers.sqlServer.scheduledJobsUserDomainUserName 'Evening' '200000')