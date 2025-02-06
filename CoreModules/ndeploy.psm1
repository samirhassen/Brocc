function Ntech-DeleteFolderIfExists
{
	param(
        [Parameter(Position=0, Mandatory=$true)]
        [string]$folder)

	if(-not ([System.IO.Path]::IsPathRooted($folder)))
	{
		throw "Cannot delete '$folder' because it is not an absolute path"
	}
	if (Test-Path $folder)
	{
		[System.IO.Directory]::Delete($folder, $true)
	}
}

function Ntech-CreateFolderThatDoesNotExist
{
	param(
        [Parameter(Position=0, Mandatory=$true)]
        [string]$folder)

	if(-not ([System.IO.Path]::IsPathRooted($folder)))
	{
		throw "Cannot create '$folder' because it is not an absolute path"
	}
	if (Test-Path $folder)
	{
        throw "Cannot create '$folder' because it already exists"
	}
	
	New-Item -ItemType Directory -Force -Path $folder | Out-Null
}

function Ntech-CopyFolderToFolder
{
	param(
        [Parameter(Position=0, Mandatory=$true)]
        [string]$sourceFolder, 
        [Parameter(Position=1, Mandatory=$true)]
        [string]$targetFolder)
	
	if(-not ([System.IO.Path]::IsPathRooted($sourceFolder)))
	{
		throw "Cannot copy from '$sourceFolder' because it is not an absolute path"
	}
	if (-not (Test-Path $sourceFolder))
	{
        throw "Cannot copy from '$sourceFolder' because it does not exist"
	}
	if(-not ([System.IO.Path]::IsPathRooted($targetFolder)))
	{
		throw "Cannot copy to '$targetFolder' because it is not an absolute path"
	}
	if (-not (Test-Path $targetFolder))
	{
        throw "Cannot copy to '$targetFolder' because it does not exist"
	}
	Copy-Item $sourceFolder -destination $targetFolder -recurse
}

function Ntech-CopyFileToFolder
{
	param(
        [Parameter(Position=0, Mandatory=$true)]
        [string]$sourceFile, 
        [Parameter(Position=1, Mandatory=$true)]
        [string]$targetFolder, 
        [Parameter(Position=2, Mandatory=$false)]
        [bool]$throwIfSourceMissing=$false)

	if(-not ([System.IO.Path]::IsPathRooted($sourceFile)))
	{
		throw "Cannot copy '$sourceFile' because it is not an absolute path"
	}
	if(-not ([System.IO.Path]::IsPathRooted($targetFolder)))
	{
		throw "Cannot copy to '$targetFolder' because it is not an absolute path"
	}
	if (-not (Test-Path $targetFolder))
	{
          throw "Cannot copy to '$targetFolder' because it does not exist"
	}
	if (Test-Path $sourceFile)
	{
        Copy-Item $sourceFile $targetFolder -Force        
	}
	elseif($throwIfSourceMissing)
    {
        throw "Cannot copy from '$sourceFile' because it does not exist"
    }
}

Import-Module WebAdministration | Out-Null
function Ntech-DeployService
{
   param(
   [Parameter(Position=0, Mandatory=$true)]
   [string]$ServiceName, 
   [Parameter(Position=1, Mandatory=$true)]
   [string]$DeploymentRootPath,
   [Parameter(Position=2, Mandatory=$true)]
   [string]$ConfigFilesRootPath
   )   

    if (-not [System.IO.Path]::IsPathRooted($DeploymentRootPath))
    {
        throw "Cannot deploy from $DeploymentRootPath since it's not an absolute path"
    }

    if (-not (Test-Path $DeploymentRootPath))
    {
        throw "Cannot deploy from $DeploymentRootPath since does not exist"
    }

    $ServiceRoot = Join-Path -Path $DeploymentRootPath -ChildPath "drop\$ServiceName\nDeploy"

    if (-not (Test-Path $ServiceRoot)) 
    {
        throw "Service does not seem to be part of the release!"
    }

   Write-Host "Starting deploy of '$ServiceName' from source '$ServiceRoot'"

   $WebSite = Get-Item "IIS:\sites\$ServiceName"
   if(!$WebSite)
   {
        throw "Website does not exist '$ServiceName'"
   }
   
   Write-Host "Stopping website '$ServiceName'"
   $WebSite.Stop()

   $SitePath = $WebSite.physicalPath
   Write-Host "Deleting old files"
   Ntech-DeleteFolderIfExists $SitePath
   Ntech-CreateFolderThatDoesNotExist $SitePath   
   $SourceSitePath = (Join-Path -Path $ServiceRoot -ChildPath "*")
   if (!(Test-Path $SourceSitePath))
   { 
        throw "$SourceSitePath does not exist"
   }
   Write-Host "Copying over shared files ($SourceSitePath -> $SitePath)"
   Ntech-CopyFolderToFolder  $SourceSitePath  $SitePath

   Write-Host 'Copying over appsettings'
   $AppSettingsFile = Join-Path -Path $ConfigFilesRootPath -ChildPath "$ServiceName.appsettings.config"
   Ntech-CopyFileToFolder $AppSettingsFile $SitePath

   Write-Host 'Copying over connectionstrings'
   $ConnectionStringFile = Join-Path -Path $ConfigFilesRootPath -ChildPath "$ServiceName.connectionstrings.config"
   Ntech-CopyFileToFolder $ConnectionStringFile $SitePath $false

   Write-Host 'Copying over robots.txt'
   $RobotsFile = Join-Path -Path $ConfigFilesRootPath -ChildPath "robots.txt"
   Ntech-CopyFileToFolder $RobotsFile $SitePath $false
      
   Write-Host "Starting website '$ServiceName'"
   $WebSite.Start()
}