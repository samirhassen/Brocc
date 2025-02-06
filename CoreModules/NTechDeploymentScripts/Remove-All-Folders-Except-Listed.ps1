Param(
    [Parameter(Mandatory=$True)]
    $rootPath, 
    [Parameter(Mandatory=$True)]
    $keptFolderNames)
#############################################################################################
# Say the folder "c:\temp\foo" has this content
# a\
# b\
# c\
# d\
# kitten.txt
# foo.bat
#Then calling this with rootPath = "c:\temp\foo" and keptFolderNames = "a,c" will result in this content:
# a\
# c\
# kitten.txt
# foo.bat
#############################################################################################
$ErrorActionPreference = "Stop"
$k = New-Object System.Collections.Generic.List[string]
$keptFolderNames.Split(',') | ForEach-Object {
    $k.Add($_.Trim())
}

$allFolderNames = Get-ChildItem -Directory $rootPath | Select-Object -ExpandProperty Name

#Sanity check. This could be any known module name. Just to prevent the user from accidently wiping some random other folder.
if (-Not $allFolderNames.Contains('nCredit')) { 
    throw '$rootPath does not contain the nCredit module. Make sure to point the script at the correct folder'
}

$allFolderNames | ForEach-Object {
    if(-Not $k.Contains($_)) {
        $f = Join-Path -Path $rootPath -ChildPath $_
        [System.IO.Directory]::Delete($f, $true)
    }
}
