Param(
    [Parameter(Mandatory=$True)]
    $rootPath, 
    [Parameter(Mandatory=$True)]
    $dedupeRootPath)
#############################################################################################
# Let take EntityFramework.dll as an example
# It will be in every bin folder and take 5MB each time
# We find all files in any folder at or below $rootPath > 10k that have the same name and the same md5 hash and replace them with a zero length file 
# named EntityFramework.dll_A35746D7A8C835F4CDAA90EFE1F11511.ntechdedupe (<orginal name>_<hash>.ntechdedupe)
# We also store a single copy of the actual file using the same name in the $dedupeRootPath
# Rebuilding is just a matter of replacing by filename from the dedupe folder
#############################################################################################
$ErrorActionPreference = "Stop"
$ntechext = '.ntechdedupe'
$dedupeMinSizeInBytes = 10000

$files = Get-ChildItem -Path $rootPath  -recurse -force
$d = @{}
foreach($file in $files) {
    if((!$file.PSIsContainer) -and ($file.Extension -eq $ntechext) -and ($file.Length -eq 0)) {
        $originalFileName = (Join-Path $file.Directory -ChildPath $file.Name.Substring(0, $file.Name.LastIndexOf('_')))
        $dedupedSourceFile = (Join-Path -Path $dedupeRootPath -ChildPath $file.Name)
        if(!(Test-Path $originalFileName)) {
            Copy-Item $dedupedSourceFile $originalFileName | Out-Null
        }
        Remove-Item $file.FullName | Out-Null
    }
}