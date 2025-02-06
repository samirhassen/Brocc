Import-Module WebAdministration -Force

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get_NTechDeploy_Website_Name {
    Param (
        [Parameter(Mandatory=$true)]
        [string]$ApplicationPrefix, 

        [Parameter(Mandatory=$true)]
        [string]$ServiceName
    )
    return $ApplicationPrefix + '-' + $ServiceName
}

function Test_NTechDeploy_AppPool {
    Param(
        [Parameter(Mandatory=$true)]
        [string]$Name
    )
   $p = Join-Path -Path 'IIS:\AppPools\' -ChildPath $Name
   return Test-Path $p -pathType container
}

function Test_NTechDeploy_Website {
    Param(
        [Parameter(Mandatory=$true)]
        [string]$Name
    )
   $p = Join-Path -Path 'IIS:\Sites\' -ChildPath $name
   return Test-Path $p -pathType container
}

function Remove_NTechDeploy_AppPool {
    Param(
        [Parameter(Mandatory=$true)]
        [string]$Name
    )
    if(Test_NTechDeploy_AppPool -Name $name) {
        Write-Host 'Removing app pool:' $name
        $s = Get-WebAppPoolState -Name $name
        if($s.Value -eq 'Started') {
            Stop-WebAppPool -Name $name
        }
        Remove-WebAppPool -Name $name
    }
}

function Remove_NTechDeploy_Website {
    Param(
        [Parameter(Mandatory=$true)]
        [string]$Name
    )
    if(Test_NTechDeploy_Website -Name $name) {
        Write-Host 'Removing website:' $name
        $s = Get-WebsiteState -Name $name
        if($s.Value -eq 'Started') {
            Stop-Website -Name $name
        }
        Remove-Website -Name $name
    }
}

function New_NTechDeploy_AppPool {
    Param (
        [Parameter(Mandatory=$true)]
        [string]$Name, 

        [Parameter(Mandatory=$true)]
        [boolean]$IsDotnetCore
    )
    Write-Host 'Creating app pool:' $Name

    $appPool = New-Item (Join-Path -Path 'IIS:\AppPools\' -ChildPath $Name)

    #Without this the user module wont load certificates from a file
    $appPool | Set-ItemProperty -Name "processModel.loadUserProfile" -Value "True"

    if($IsDotnetCore) {
        $appPool | Set-ItemProperty -Name "managedRuntimeVersion" -Value ""
    } else {
        $appPool | Set-ItemProperty -Name "managedRuntimeVersion" -Value "v4.0"
    }
}

function New_NTechDeploy_Junction {
    Param (
        [Parameter(Mandatory=$true)]
        [string]$RealPath, 

        [Parameter(Mandatory=$true)]
        [string]$VirtualPath
    )
    New-Item -Path $VirtualPath -ItemType SymbolicLink -Value $RealPath -Force
}

function Copy_NTechDeploy_File_If_Exists {
    Param (
        [Parameter(Mandatory=$true)]
        [string]$SourceFilePath, 

        [Parameter(Mandatory=$true)]
        [string]$TargetFilePath
    )

    if(Test-Path $SourceFilePath) {
        Write-Host 'Copying' ([System.IO.Path]::GetFileName($SourceFilePath))
        Copy-Item $SourceFilePath $TargetFilePath
    }
}

function New_NTechDeploy_Website {
    Param (
        [Parameter(Mandatory=$true)]
        [string]$ApplicationPrefix, 

        [Parameter(Mandatory=$true)]
        [string]$ServiceName,

        [Parameter(Mandatory=$true)]
        [boolean]$IsDotnetCore,

        [Parameter(Mandatory=$true)]
        [string]$HostHeader,

        [Parameter(Mandatory=$true)]
        [string]$ReleaseSymRoot,

        [Parameter(Mandatory=$true)]
        [string]$SslCertThumbprint,

        [Parameter(Mandatory=$true)]
        [string]$ConfigSourceFolder,

        [Parameter(Mandatory=$true)]
        [boolean]$UseWindowsAuthentication,

        [Parameter(Mandatory=$true)]
        [string]$AppPoolName
    )

    $websiteName = Get_NTechDeploy_Website_Name -ApplicationPrefix $applicationPrefix -ServiceName $ServiceName

    Write-Host 'Creating website:' $websiteName

    $sitePath = Join-Path -Path $ReleaseSymRoot -ChildPath "drop\$ServiceName"
    $site = New-WebSite -Name $websiteName -PhysicalPath $sitePath -ApplicationPool $AppPoolName -Port 443 -Ssl -HostHeader $HostHeader -SslFlags 1 -Force
    $site.Stop()
    $site.bindings.Collection[0].AddSslCertificate($SslCertThumbprint, 'My')

    if($UseWindowsAuthentication) {
        Write-Host 'Changing to windows (AD) authentication'
        
        Try {
            Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Name Enabled -Value True -PSPath "IIS:\Sites\$websiteName"
        } Catch {
            #Typically because the section is locked. See if we can unlock it and try again.
            Write-Host 'Attempting to unlock anonymousAuthentication section'
            Invoke-Expression "$env:SystemRoot\system32\inetsrv\appcmd unlock config /section:anonymousAuthentication"
            Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Name Enabled -Value True -PSPath "IIS:\Sites\$websiteName"
        }

        Try {
            Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -Name Enabled -Value True -PSPath "IIS:\Sites\$websiteName"
        } Catch {
            #Typically because the section is locked. See if we can unlock it and try again.
            Write-Host 'Attempting to unlock windowsAuthentication section'
            Invoke-Expression "$env:SystemRoot\system32\inetsrv\appcmd unlock config /section:windowsAuthentication"
            Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -Name Enabled -Value True -PSPath "IIS:\Sites\$websiteName"
        }        
    }

    Write-Host 'Configuring:' $websiteName
    if($IsDotnetCore) {
        Copy_NTechDeploy_File_If_Exists -SourceFilePath (Join-Path -Path $ConfigSourceFolder -ChildPath "$ServiceName.appsettings.json") -TargetFilePath (Join-Path -Path $site.physicalPath -ChildPath "appsettings.json")
    } else {
        Copy_NTechDeploy_File_If_Exists -SourceFilePath (Join-Path -Path $ConfigSourceFolder -ChildPath "$ServiceName.appsettings.config") -TargetFilePath $site.physicalPath
        Copy_NTechDeploy_File_If_Exists -SourceFilePath (Join-Path -Path $ConfigSourceFolder -ChildPath "$ServiceName.connectionstrings.config") -TargetFilePath $site.physicalPath
        Copy_NTechDeploy_File_If_Exists -SourceFilePath (Join-Path -Path $ConfigSourceFolder -ChildPath "robots.txt") -TargetFilePath $site.physicalPath
    }
    Copy_NTechDeploy_File_If_Exists -SourceFilePath (Join-Path -Path $ConfigSourceFolder -ChildPath "$ServiceName.web.config") -TargetFilePath (Join-Path -Path $site.physicalPath -ChildPath "web.config")
}

function Publish_NTechDeploy_Website {
    Param (
        [Parameter(Mandatory=$true)]
        [string]$ApplicationPrefix, 

        [Parameter(Mandatory=$true)]
        [string]$ServiceName,

        [Parameter(Mandatory=$true)]
        [boolean]$IsDotnetCore,

        [Parameter(Mandatory=$true)]
        [string]$HostHeader,

        [Parameter(Mandatory=$true)]
        [string]$ReleaseSymRoot,

        [Parameter(Mandatory=$true)]
        [string]$SslCertThumbprint,
        
        [Parameter(Mandatory=$true)]
        [string]$ConfigSourceFolder,
        
        [Parameter(Mandatory=$true)]
        [boolean]$UseWindowsAuthentication,
        
        [Parameter(Mandatory=$false)]
        [boolean]$RemoveAndReCreateAppPools
    )

    $websiteName = Get_NTechDeploy_Website_Name -ApplicationPrefix $applicationPrefix -ServiceName $ServiceName
    Remove_NTechDeploy_Website -Name $websiteName

    $appPoolName 
    # If true, will remove and recreate one app pool per service. 
    # If false, will have two app pools; one for .NET Framework, one for .NET Core. 
    If ($RemoveAndReCreateAppPools) {
        $appPoolName  = $websiteName
        Remove_NTechDeploy_AppPool -Name $appPoolName
        New_NTechDeploy_AppPool -Name $appPoolName -IsDotnetCore $IsDotnetCore
    }
    Else {
        $appPoolName = If($IsDotnetCore) { $ApplicationPrefix + "-Core" } Else { $applicationPrefix }
        If((Test_NTechDeploy_AppPool -Name $appPoolName) -eq $false) {
            New_NTechDeploy_AppPool -Name $appPoolName -IsDotnetCore $IsDotnetCore
        }
    }

    New_NTechDeploy_Website -ApplicationPrefix $ApplicationPrefix -ServiceName $ServiceName -IsDotnetCore $IsDotnetCore -HostHeader $HostHeader -ReleaseSymRoot $ReleaseSymRoot -SslCertThumbprint $SslCertThumbprint -ConfigSourceFolder $ConfigSourceFolder -UseWindowsAuthentication $UseWindowsAuthentication -AppPoolName $appPoolName
}

function Start_NTechDeploy_Website {
    Param (
        [Parameter(Mandatory=$true)]
        [string]$ApplicationPrefix, 

        [Parameter(Mandatory=$true)]
        [string]$ServiceName        
    )    
    $n =  Get_NTechDeploy_Website_Name -ApplicationPrefix $ApplicationPrefix -ServiceName $ServiceName
    Write-Host 'Starting' $n
    Start-Website -Name $n

}

function New_NTechDeploy_Directory {
    Param (
        [Parameter(Mandatory=$true)]
        [string]$Name
    ) 
    New-Item -ItemType Directory -Force -Path $Name | Out-Null
}

function Expand_NTechDeploy_Release {
    Param (
        [Parameter(Mandatory=$true)]
        [string]$Releasefile, 

        [Parameter(Mandatory=$true)]
        [string]$ReleaseRoot
    )
    ##########################################
    ######### Unzip release ##################
    ##########################################
    $deploymentGuid = [guid]::NewGuid()
    $SourceRoot = Join-Path -Path $ReleaseRoot -ChildPath $deploymentGuid
    if (Test-Path $SourceRoot) {
        Remove-Item -Recurse -Force $SourceRoot
    }

    New_NTechDeploy_Directory $SourceRoot

    Write-Host 'Unzipping release to: ' $SourceRoot

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($releasefile, $SourceRoot)

    ############################
    #Restore deduped files #####
    ############################
    $restoreScript = (Join-Path -Path $SourceRoot -ChildPath "Restore-Deduped-Files.ps1")
    if (Test-Path $restoreScript) {
        Write-Host 'Restoring deduped files'
        $restoreRootPath = (Join-Path -Path $SourceRoot -ChildPath "drop")
        $restoreDedupeRootPath  = (Join-Path -Path $SourceRoot -ChildPath "dedupe")

        $restoreArgs = @()
        $restoreArgs += ("-rootPath", "$restoreRootPath")
        $restoreArgs += ("-dedupeRootPath", "$restoreDedupeRootPath")

        Invoke-Expression "$restoreScript $restoreArgs"
    }

    return $SourceRoot
}