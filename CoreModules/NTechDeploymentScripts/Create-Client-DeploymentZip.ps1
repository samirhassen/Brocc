Param(
    ##Send in $(Build.SourcesDirectory)
    [Parameter(Mandatory=$true)]
    [string]$CoreNaktergalSourceDirectory,

    ##Send in $(build.artifactstagingdirectory)
    [Parameter(Mandatory=$true)]
    [string]$ArtifactStagingDirectory,

    [Parameter(Mandatory=$true)]
    [string]$ClientName,

    ##Send in $(build.buildNumber)
    [Parameter(Mandatory=$true)]
    [string]$BuildNumber,
    
    ##Comma separated like nCredit,nCustomer
    [Parameter(Mandatory=$True)]
    [string]$KeptModuleFolderNames,

    [switch]$SkipSharedClientResources,
    [switch]$SkipCompress,

    ##Will use $ClientName if not specified
    [string]$ArtifactPrefix,

    ##Will use 'ClientResources' if not specified
    [string]$ClientResourcesName
)

$ErrorActionPreference = "Stop"

function Remove-AllModules-Except-Listed(
    [Parameter(Mandatory=$True)]
    $RootDirectory) {

        $k = New-Object System.Collections.Generic.List[string]
        $KeptModuleFolderNames.Split(',') | ForEach-Object {
            $k.Add($_.Trim())
        }
        
        $allFolderNames = Get-ChildItem -Directory $RootDirectory | Select-Object -ExpandProperty Name
        
        #Sanity check. This could be any known module name. Just to prevent the user from accidently wiping some random other folder.
        if (-Not $allFolderNames.Contains('nCustomer')) { 
            throw '$RootDirectory does not contain the nCustomer module. Make sure to point the script at the correct folder'
        }
        
        $allFolderNames | ForEach-Object {
            if(-Not $k.Contains($_)) {
                $f = Join-Path -Path $RootDirectory -ChildPath $_
                [System.IO.Directory]::Delete($f, $true)
            }
        }
}

function Expand-Archives($SourceDirectory, $TargetDirectory) {
    if (!(Test-Path -Path $SourceDirectory)) {
        Write-Host "Missing source directory $SourceDirectory. Make sure to download the trunk zip file to this directory."
    }
    if (!(Test-Path -Path $TargetDirectory)) {
        New-Item -ItemType Directory -Path $TargetDirectory
    }
    
    Get-ChildItem -Path $SourceDirectory -Filter *.zip | ForEach-Object {
        Expand-Archive -Path $_.FullName -DestinationPath $TargetDirectory
    }
}

$ClientResourcesName = if ($ClientResourcesName.Length -eq 0) { 'ClientResources' } else { $ClientResourcesName }

$NaktergalTrunkArtifactDirectory = (Join-Path -Path $ArtifactStagingDirectory -ChildPath "Naktergal-Trunk-$ClientName")

##Extract trunk zip file
##The trunk zip file should be downloaded to the directory $(build.artifactstagingdirectory)/Naktergal-Trunk-Zip
##It will be extracted to $(build.artifactstagingdirectory)/Naktergal-Trunk
Write-Host 'Extracting trunk zip file'
Expand-Archives -SourceDirectory (Join-Path -Path $ArtifactStagingDirectory -ChildPath 'Naktergal-Trunk-Zip') -TargetDirectory $NaktergalTrunkArtifactDirectory

Write-Host "Filtering out all modules except the ones listed in $KeptModuleFolderNames"
Remove-AllModules-Except-Listed -RootDirectory (Join-Path -Path $NaktergalTrunkArtifactDirectory -ChildPath 'drop')

Write-Host 'Copying client resources'
$ClientResourcesTargetDirectory = Join-Path -Path $NaktergalTrunkArtifactDirectory -ChildPath $ClientResourcesName
Copy-Item -Path (Join-Path -Path $CoreNaktergalSourceDirectory -ChildPath "SelfContainedModules\Clients\$ClientName\Resources") -Destination $ClientResourcesTargetDirectory -Recurse

if(-not $SkipSharedClientResources) {
    Write-Host 'Copying shared resources'
    Copy-Item -Path (Join-Path -Path $CoreNaktergalSourceDirectory -ChildPath 'SelfContainedModules\SharedClientResources') -Destination (Join-Path -Path $ClientResourcesTargetDirectory -ChildPath 'Shared') -Recurse
}

##Generating version file
Write-Host 'Generating version file'
New-Item -Path $ClientResourcesTargetDirectory -Name "CurrentReleaseMetadata.txt" -ItemType "file" -Value "releaseNumber=$BuildNumber"

##Creating artifact
$ArtifactFileNamePrefix = if ($ArtifactPrefix.Length -eq 0) { $ClientName } else { $ArtifactPrefix }
$ArtifactFileName = Join-Path -Path $ArtifactStagingDirectory -ChildPath "$ArtifactFileNamePrefix-$BuildNumber.zip"
Write-Host "Generating artifact $ArtifactFileName"

if(-not $SkipCompress) {
    Write-Host "Compressing artifact"
    Compress-Archive -Path "$NaktergalTrunkArtifactDirectory\*" -DestinationPath $ArtifactFileName
} else {
    Write-Host "Skipping compress artifact"
}