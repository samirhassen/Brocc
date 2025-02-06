Param(
  [parameter(mandatory=$true)][string]$envSettingsFile,
  [parameter(mandatory=$true)][string]$envSecretsFile,
  [parameter(mandatory=$true)][string]$serverName
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction']='Stop'

################################################
########### Initialize IIS #####################
################################################

##On windows 2019 this needs Install-WindowsFeature web-mgmt-console
##Can we detect?
Import-Module WebAdministration -Force

function Initialize_IIS {
    Write-Host 'Enabling required IIS features'
    
    $requiredIISFeatureNames = @("IIS-WebServerRole", "IIS-WebServer", "IIS-CommonHttpFeatures", "IIS-HttpErrors", "IIS-HttpRedirect", "IIS-ApplicationDevelopment","IIS-NetFxExtensibility",
    "IIS-NetFxExtensibility45", "IIS-HealthAndDiagnostics", "IIS-HttpLogging", "IIS-LoggingLibraries", "IIS-RequestMonitor", "IIS-HttpTracing", "IIS-Security", "IIS-RequestFiltering"
    "IIS-Performance", "IIS-WebServerManagementTools", "IIS-IIS6ManagementCompatibility", "IIS-Metabase", "IIS-StaticContent", "IIS-DefaultDocument", "IIS-DirectoryBrowsing", "IIS-WebSockets",
    "IIS-ApplicationInit","IIS-ASPNET", "IIS-ASPNET45", "IIS-ASP", "IIS-ISAPIExtensions", "IIS-ISAPIFilter", "IIS-BasicAuthentication", "IIS-HttpCompressionStatic", "IIS-ManagementConsole",
    "IIS-WindowsAuthentication")

    $currentActiveFeatureNames = Get-WindowsOptionalFeature -Online | Where-Object FeatureName -like 'IIS-*' | Where-Object State -like 'Enabled' | Select-Object -ExpandProperty FeatureName
    foreach($featureName in $requiredIISFeatureNames) {
        if($currentActiveFeatureNames.Contains($featureName) -eq $false) {
            Write-Host 'Enabling iis feature' $featureName
            Enable-WindowsOptionalFeature -Online -FeatureName $featureName -All -NoRestart
        }
    }
}

Initialize_IIS

################################################
########## Setup the environment folder ########
################################################

$supportedModules = @('nUser', 'nAudit', 'nBackOffice', 'nCredit', 'nPreCredit', 'nCreditReport', 'nCustomer', 'nDataWarehouse', 'nScheduler', 'nWindowsAuthIdentityServer', 'nTest', 'nDocument', 'nCustomerPages', 'NTechSignicat')

$webModuleNames = New-Object System.Collections.Generic.HashSet[string]
$webModuleNames.Add('nCustomerPages') | Out-Null
$webModuleNames.Add('NTechSignicat') | Out-Null

$f = Get-Content -Path $envSettingsFile | ConvertFrom-Json
$secrets = (Get-Content -Path $envSecretsFile | ConvertFrom-Json).secrets

function ReplaceSecretsInString($v) {
    $r = $v
    foreach($s in $secrets) {
        $sn = '{secrets.' + $s.key + '}'
        $r = $r.replace($sn, $s.value)
    }
    return $r
}

function IIf($If, $Right, $Wrong) {If ($If) {$Right} Else {$Wrong}}

function Coalesce($v1, $v2) {
    if($v1) {
        return $v1
    }
    return $v2
}

Function Get-PropOrNull {
    param($thing, [string]$prop)
    Try {
        $thing.$prop
    } Catch {
    }
}

filter Get-DeepProperty([string] $Property) {
    $path = $Property -split '\.'
    $obj = $_
        foreach($node in $path){
            if($node -match '.*\[\d*\]'){
                $keyPieces = $node -split ('\[')
                $arrayKey = $keyPieces[0]
                $arrayIndex = $keyPieces[1] -replace ('\]','')
                $obj = $obj.$arrayKey[$arrayIndex]
            } else { 
                $obj = $obj.$node 
            }
        }
    $obj
}

function New-SqlServerConnectionStringFile($configFilesPath, $moduleName, $cn1, $cn2) {
    $fn = Get-ModuleFriendlyName $moduleName
    $a = '<connectionStrings>'

    $connectionStringNames = @($cn1)
    if($cn2) {
        $connectionStringNames += $cn2
    }

    foreach($connectionStringName  in $connectionStringNames) {
        $a += [Environment]::NewLine + ' <add name="' + $connectionStringName + '" providerName="System.Data.SqlClient"'
        $a += [Environment]::NewLine + 'connectionString="' + (ReplaceSecretsInString $f.conventions.sqlconnectionstringpattern.replace('{moduleFriendlyName}', $fn)) + '" />'    
    }

    $a  += [Environment]::NewLine + '</connectionStrings>'

    New-Item -Path $configFilesPath -Name ($moduleName + '.connectionstrings.config') -ItemType "file" -Value $a | Out-Null
}

function New-RobotsTxtFile($configFilesPath) {
    $content = 
@"
User-Agent: *
Disallow: /
"@
    New-Item -Path $configFilesPath -Name "robots.txt" -ItemType "file" -Value $content | Out-Null
}

function New-ServiceRegistryFile($staticResourcesPath, $serviceUrls) {
    $a = '#Service registry'

    foreach($s in $serviceUrls) {
        $a += [Environment]::NewLine + $s.key + '=' + $s.value
    }

    New-Item -Path $staticResourcesPath -Name 'serviceregistry.txt' -ItemType "file" -Value ($a) | Out-Null
}

function New-AppSettings($settings) {
    $a = '<appSettings>'

    foreach($s in $settings) {
        $a += [Environment]::NewLine + '    <add key="'+ $s.key + '" value="' + (ReplaceSecretsInString $s.value) + '" />'
    }
    $a  += [Environment]::NewLine + '</appSettings>'
    return $a
}

function Get-RandomCharacters($length, $characters) { 
    $random = 1..$length | ForEach-Object { Get-Random -Maximum $characters.length } 
    $private:ofs="" 
    return [String]$characters[$random]
}

function Get-ModuleFriendlyName($moduleName) {
    if($moduleName[0] -eq 'n') {
        return $moduleName.Substring(1).ToLower()
    } else {
        return $moduleName.ToLower()
    }
}

function Get-ModuleUrl($moduleName) {
    $fn = Get-ModuleFriendlyName $moduleName
    If($webModuleNames.Contains($moduleName) -eq $True) {
        return $f.conventions.weburlpattern.replace("{moduleFriendlyName}", $fn)
    } else {
        return $f.conventions.appurlpattern.replace("{moduleFriendlyName}", $fn)
    }
}

function New-Certificate([SecureString]$CertificatePassword, $FriendlyName, $ExportFilePath) {
    $CertificateName = Get-RandomCharacters -length 10 -characters 'abcdefghiklmnoprstuvwxyz'
    $ExpirationDate = "2099-01-15"
    $Subject = "CN=" + $CertificateName
    $Result1 = New-SelfSignedCertificate -certstorelocation cert:\CurrentUser\my -Subject $Subject  -FriendlyName $FriendlyName  -NotAfter $ExpirationDate -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider"
    $Thumbprint = $Result1.Thumbprint
    $CertPath = "cert:\CurrentUser\my\" + $Thumbprint
    Export-PfxCertificate -cert $CertPath -FilePath $ExportFilePath -Password $CertificatePassword | Out-Null
    Remove-Item -Path $CertPath | Out-Null
}

$naktergalPath = Coalesce $f.paths.naktergalPath 'C:\Naktergal'
$configFilesPath = Join-Path -Path $naktergalPath -ChildPath 'Deployment\ConfigFiles'
$staticResources = Join-Path -Path $naktergalPath -ChildPath 'StaticResources'
$tempPath = Join-Path -Path $naktergalPath -ChildPath 'NaktergalTemp'
$logPath = Join-Path -Path $tempPath -ChildPath 'Logs'
$clientResourcePath = Join-Path -Path $tempPath -ChildPath 'CurrentClientResources' #Dont create, will be junctioned to
$isAppServer = $serverName -eq $f.servers.appServer.name
$isWebServer = $serverName -eq $f.servers.webServer.name

if(($isAppServer -or -$isWebServer) -eq $False) {
    $e = $serverName + ' must be either the appserver or the webserver'
    throw $e
}

Write-Host 'Setting up' $serverName

if (Test-Path $naktergalPath) {    
    $e = 'NaktergalPath ' + $naktergalPath + ' already exists!'
    throw $e
} else {
    Write-Host 'Creating' $naktergalPath 'as NaktergalPath'
}
New-Item -ItemType Directory -Force -Path $naktergalPath | Out-Null 
New-Item -ItemType Directory -Force -Path $tempPath | Out-Null
New-Item -ItemType Directory -Force -Path $logPath | Out-Null

###############################
####### Config files ##########
###############################
Write-Host 'Creating config files'
New-Item -ItemType Directory -Force -Path $configFilesPath | Out-Null 
New-Item -ItemType Directory -Force -Path $staticResources | Out-Null 

$machineSettings = @()
$machineSettings += @{ key = 'ntech.isproduction'; value = (IIf ($f.isproduction -eq $True) 'true' 'false') }
$machineSettings += @{ key = 'ntech.skinning.enabled'; value = 'true' }
$machineSettings += @{ key = 'ntech.clientresourcefolder'; value = $clientResourcePath }
$machineSettings += @{ key = 'ntech.staticresourcefolder'; value = $staticResources }
$machineSettings += @{ key = 'ntech.logfolder'; value = $logPath }
$machineSettings += @{ key = 'ntech.performancelog.enabled'; value = 'false' }
$machineSettings += @{ key = 'ntech.isbundlingenabled'; value = 'true' }
$machineSettings += @{ key = 'ntech.isverboseloggingenabled'; value = 'false' }
if($f.isproduction -eq $False) {
    $machineSettings += @{ key = 'ntech.credit.testing.overridedatefile'; value = (Join-Path -Path $tempPath -ChildPath 'Test\TestOverrideDate.txt') }
}

if($isAppServer) {
    foreach($s in $f.servers.appServer.extraMachineSettings) {
        $machineSettings += @{ key = $s.key; value = $s.value }
    }
}
if($isWebServer) {
    foreach($s in $f.servers.webServer.extraMachineSettings) {
        $machineSettings += @{ key = $s.key; value = $s.value }
    }
}

## Create machine settings ##
$machineSettingsFile = Join-Path -Path $staticResources -ChildPath 'ntech.machinesettings.config'
New-Item -Path $staticResources -Name 'ntech.machinesettings.config' -ItemType "file" -Value (New-AppSettings $machineSettings) | Out-Null

## Create robots file that prevents test from being indexed ##
if($f.isproduction -eq $False) {
    New-RobotsTxtFile $configFilesPath
}

## Create a user certificate ##
$certPwd = Get-RandomCharacters -length 25 -characters 'abcdefghiklmnoprstuvwxyz123456789'
$certFile = Join-Path -Path $staticResources -ChildPath 'nUser-certificate.pfx'
New-Certificate (ConvertTo-SecureString $certPwd -AsPlainText -Force) 'nUserCertificate' $certFile

## Create an encryption file ##
$encKeyName = $f.environmentName + '_' + $serverName + '_' + (Get-Date -format "yyyyMMddHHmmss")
$encPwd =  (Get-RandomCharacters -length 25 -characters 'abcdefghiklmnoprstuvwxyz123456789')
$encFileContent = @{ CurrentKeyName = $encKeyName; AllKeys = @( @{ Name = $encKeyName; Key = $encPwd } ) } | ConvertTo-Json
New-Item -Path $staticResources -Name 'encryptionkeys.txt' -ItemType "file" -Value $encFileContent | Out-Null

$serviceUrls = @()

$deployedModuleNames = @()

foreach($moduleName in $supportedModules) {
    Write-Host 'Setting up module' $moduleName
    
    $appSettings = @()
    $appSettings += @{ key = 'webpages:Version'; value = '3.0.0.0' }
    $appSettings += @{ key = 'webpages:Enabled'; value = 'false' }
    $appSettings += @{ key = 'PreserveLoginUrl'; value = 'true' }
    $appSettings += @{ key = 'ClientValidationEnabled'; value = 'true' }
    $appSettings += @{ key = 'UnobtrusiveJavaScriptEnabled'; value = 'true' }
    $appSettings += @{ key = 'ntech.machinesettingsfile'; value = $machineSettingsFile }

    $m = Get-PropOrNull $f.modules $moduleName

    if($m) {
        $serviceUrls += @{ key = $moduleName; value = Get-ModuleUrl $moduleName }
    }

    if($webModuleNames.Contains($moduleName)) {
        if(!$isWebServer) {
            $m = $Null
            Write-Host 'Skipping ' $moduleName
        }
    } else {
        if(!$isAppServer) {
            $m = $Null
            Write-Host 'Skipping ' $moduleName
        }
    }

    if($m) {
        $deployedModuleNames += $moduleName

        $friendlyName = Get-ModuleFriendlyName $moduleName

        If($moduleName -eq 'nUser') {
            $appSettings += @{ key = 'ntech.identityserver.certificate'; value = 'filewithpw:' + $certFile + ';' + $certPwd }
    
            if(Get-PropOrNull $f.modules.nUser 'googlelogin') {
                $appSettings += @{ key = 'ntech.identityserver.googlelogin.enabled'; value = 'true' }
                $appSettings += @{ key = 'ntech.identityserver.googlelogin.clientid'; value = $m.googlelogin.clientid }
                $appSettings += @{ key = 'ntech.identityserver.googlelogin.clientsecret'; value = $m.googlelogin.clientsecret }
            }
            if(Get-PropOrNull $f.modules.nUser 'windowslogin') {
                $appSettings += @{ key = 'ntech.identityserver.windowslogin.enabled'; value = 'true' }
                $appSettings += @{ key = 'ntech.identityserver.windowslogin.servicename'; value = 'nWindowsAuthIdentityServer' }
                $appSettings += @{ key = 'ntech.identityserver.windowslogin.url'; value = Get-ModuleUrl 'nWindowsAuthIdentityServer' }
            }
            if(Get-PropOrNull $f.modules.nUser 'azureadlogin') {
                $appSettings += @{ key = 'ntech.identityserver.azureadlogin.enabled'; value = 'true' }
                $appSettings += @{ key = 'ntech.identityserver.azureadlogin.authority'; value = $m.azureadlogin.authority }
                $appSettings += @{ key = 'ntech.identityserver.azureadlogin.applicationclientid'; value = $m.azureadlogin.applicationclientid }
            }

            New-SqlServerConnectionStringFile $configFilesPath $moduleName 'UsersContext'
        } elseif($moduleName -eq 'nAudit') {
            $appSettings += @{ key = 'ntech.telemetry.loggingmode'; value = 'none' }
            New-SqlServerConnectionStringFile $configFilesPath $moduleName 'AuditContext'
        } elseif($moduleName -eq 'nCredit') {
            New-SqlServerConnectionStringFile $configFilesPath $moduleName 'CreditContext'
        } elseif($moduleName -eq 'nPreCredit') {
            New-SqlServerConnectionStringFile $configFilesPath $moduleName 'PreCreditContext'
        } elseif($moduleName -eq 'nCreditReport') {
            New-SqlServerConnectionStringFile $configFilesPath $moduleName 'CreditReportContext'
        } elseif($moduleName -eq 'nCustomer') {
            New-SqlServerConnectionStringFile $configFilesPath $moduleName 'CustomersContext'
        } elseif($moduleName -eq 'nDataWarehouse') {
            New-SqlServerConnectionStringFile $configFilesPath $moduleName 'AnalyticsContext' 'Datawarehouse'
        } elseif($moduleName -eq 'nDocument') {
            
        } elseif($moduleName -eq 'nScheduler') {
            New-SqlServerConnectionStringFile $configFilesPath $moduleName 'SchedulerContext'
        } elseif($moduleName -eq 'nWindowsAuthIdentityServer') {
            $appSettings += @{ key = 'ntech.identityserver.certificate'; value = 'filewithpw:' + $certFile + ';' + $certPwd }
            $appSettings += @{ key = 'ntech.identityserver.windows.debugpage.enable'; value = 'true' }

            #This is to enable windows auth when testing from the local machine. It will work from everywhere but the local server with this off making installation verification really hard
            #https://stackoverflow.com/questions/5402381/receiving-login-prompt-using-integrated-windows-authentication
            Try {
                New-ItemProperty HKLM:\System\CurrentControlSet\Control\Lsa -Name  "DisableLoopbackCheck" -Value "1" -PropertyType dword | Out-Null
            } Catch {
                #There seems to be no good way to test if the property exists so we do this to swallow the error if it exists.
            }
            

        } elseif($moduleName -eq 'nTest') {
            $appSettings += @{ key = 'ntech.ntest.usesqlserverdocumentdb'; value = 'true' }
            New-SqlServerConnectionStringFile $configFilesPath $moduleName 'TestSqlServerDb'
        }

        if(Get-PropOrNull $m 'extraAppSettings') {
            foreach($s in $m.extraAppSettings) {
                $appSettings += @{ key = $s.key; value = $s.value }
            }
        }

        ##Dont add new modules below this
        New-Item -Path $configFilesPath -Name ($moduleName + '.appsettings.config') -ItemType "file" -Value (New-AppSettings $appSettings) | Out-Null
    }
}

New-ServiceRegistryFile $staticResources $serviceUrls

#################################################
########### Creating deployment script ##########
#################################################
if($isWebServer -or $isAppServer) {
    Write-Host 'Creating deployment script'

    if($isWebServer) {
        $server = $f.servers.webServer
    } else {
        $server = $f.servers.appServer
    }

    $deploymentScriptTemplate = 
@'
    param ([Parameter(Mandatory=$true)][string]$releasefile)
    ###above is input
    $ErrorActionPreference = "Stop"
    
    Import-Module (Join-Path $PSScriptRoot -ChildPath '\azure-deploy-functions.psm1') -Force
    
    ####CONFIG####
    $serviceNames = [[DEPLOYED_SERVICE_NAMES]]
    
    $hostHeaders = @{
    [[HOST_HEADERS]]
    }
    
    $ApplicationPrefix = "[[APPLICATION_PREFIX]]"
    $RemoveAndReCreateAppPools = "[[REMOVE_AND_RECREATE_APPPOOLS]]" -eq $True
    $BaseFolder = "[[NAKTERGAL_FOLDER]]"
    $NaktergalTempFolder = Join-Path -Path $BaseFolder -ChildPath  "NaktergalTemp"
    $ReleaseRoot = Join-Path -Path $BaseFolder -ChildPath "NaktergalTemp\AllReleases"
    $ReleaseSymRoot = Join-Path -Path $BaseFolder -ChildPath "NaktergalTemp\CurrentRelease"
    $ResourcesFolder = Join-Path -Path $BaseFolder -ChildPath "NaktergalTemp\CurrentClientResources"
    $DeploymentConfigFolder = Join-Path -Path $BaseFolder -ChildPath "Deployment\ConfigFiles"
    $sslCertThumbprint = "[[SSL_CERT_THUMBPRINT]]"
    $sslStoreName = "My"
    $ClientResourcesSourceName = "[[CLIENT_RESOURCES_NAME]]"
    $ConfigSourceFolder = Join-Path -Path $BaseFolder -ChildPath "Deployment\ConfigFiles"
    
    function Get-Uses-WindowsAuthentication {
        Param (
            [Parameter(Mandatory=$true)]
            [string]$ServiceName
            )
            return $ServiceName -eq "nWindowsAuthIdentityServer"
    }

    function Get-Uses-DotnetCore {
        Param (
            [Parameter(Mandatory=$true)]
            [string]$ServiceName
            )
            return $ServiceName -eq "NTechSignicat"
    }
    
    ###############
    
    New_NTechDeploy_Directory $ReleaseRoot
    
    $oldReleaseFolders = Get-ChildItem -Path $releaseRoot | Where-Object { $_.PSIsContainer }
    
    $SourceRoot = Expand_NTechDeploy_Release $releasefile $ReleaseRoot
    
    New_NTechDeploy_Junction -RealPath $SourceRoot -VirtualPath $ReleaseSymRoot
    
    New_NTechDeploy_Junction -RealPath (Join-Path $SourceRoot -ChildPath $ClientResourcesSourceName) -VirtualPath $ResourcesFolder
    
    foreach($ServiceName in $serviceNames)
    {
        $UseWindowsAuthentication = Get-Uses-WindowsAuthentication -ServiceName $ServiceName
        $UsesDotnetCore = Get-Uses-DotnetCore -ServiceName $ServiceName
        Publish_NTechDeploy_Website -ApplicationPrefix $ApplicationPrefix -ServiceName $ServiceName -IsDotnetCore $UsesDotnetCore -HostHeader $hostHeaders.Get_Item($ServiceName) -ReleaseSymRoot $ReleaseSymRoot -SslCertThumbprint $sslCertThumbprint -ConfigSourceFolder $ConfigSourceFolder -UseWindowsAuthentication $UseWindowsAuthentication -RemoveAndReCreateAppPools $RemoveAndReCreateAppPools
    }
    foreach($ServiceName in $serviceNames)
    {
        Start_NTechDeploy_Website -ApplicationPrefix $ApplicationPrefix -ServiceName $ServiceName
    }
    
    #######################
    ## Cleanup ############
    #######################
    
    #Delete old releases
    foreach($oldReleaseFolder in $oldReleaseFolders) {
        Write-Host 'Removing old release' $oldReleaseFolder
        try {
            &cmd.exe /c rd /s /q $oldReleaseFolder.FullName
        } 
        catch { 
            #Write-Host ('Could not delete: ' + $oldReleaseFolder.FullName)
        }    
    }
'@
    
    function Create-DeployedServiceNames($items) {
        $s = ''
        foreach($m in $items) {
            if($s) {
                $s += ', '
            }
            $s += '"' + $m + '"'
        }
        return $s
    }
    
    function Create-HostHeaders($deployedModuleNames) {
        $s = ''
        foreach($m in $deployedModuleNames) {
            $url = [System.Uri](Get-ModuleUrl $m)
            if($s) {
                $s += [Environment]::NewLine
            }
            $s += '    "' + $m + '"' + ' = "' + $url.Host + '";'
        }
        return $s
    }
    
    $ds = $deploymentScriptTemplate.replace('[[DEPLOYED_SERVICE_NAMES]]', (Create-DeployedServiceNames $deployedModuleNames))
    $ds = $ds.replace('[[HOST_HEADERS]]', (Create-HostHeaders $deployedModuleNames))
    $ds = $ds.replace('[[APPLICATION_PREFIX]]', $f.environmentName)
    $ds = $ds.replace('[[REMOVE_AND_RECREATE_APPPOOLS]]', $f.removeAndReCreateAppPools)
    $ds = $ds.replace('[[NAKTERGAL_FOLDER]]', $naktergalPath)
    $ds = $ds.replace('[[SSL_CERT_THUMBPRINT]]', $server.sslCertThumbprint)
    $ds = $ds.replace('[[CLIENT_RESOURCES_NAME]]', $f.clientFolderName)    

    $deploymentFolder = (Join-Path -Path $naktergalPath -ChildPath 'Deployment')
    
    New-Item -Path $deploymentFolder -Name 'deploy.ps1'  -ItemType "file" -Value $ds | Out-Null

    $deploymentLibFilePath = Join-Path -Path (Get-Item $envSettingsFile).Directory.FullName -ChildPath 'azure-deploy-functions.psm1'
    if(Test-Path $deploymentLibFilePath) {
        Copy-Item $deploymentLibFilePath -Destination $deploymentFolder
    } else {
        Write-Host 'azure-deploy-functions.psm1 is missing. Locate it manually and copy it to ' $deploymentFolder
    }

    New-Item -ItemType Directory -Force -Path (Join-Path -Path $deploymentFolder -ChildPath 'Releases') | Out-Null
}