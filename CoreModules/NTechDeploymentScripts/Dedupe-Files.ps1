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

New-Item -ItemType Directory -Force -Path $dedupeRootPath | Out-Null

$files = Get-ChildItem -Path $rootPath  -recurse -force
$d = @{}
foreach($file in $files){
    if((!$file.PSIsContainer) -and (!($file.Extension -eq $ntechext))) {
        if(!$d.ContainsKey($file.Name)) {
            $d.Set_Item($file.Name, [System.Collections.ArrayList]@()) | Out-Null
        }
        $d.Get_Item($file.Name).Add($file) | Out-Null
    }
}

#Only dedupe if all of them have the same hash
function Dedupe ($files) {
    $filesByHash = @{}
    foreach($file in $files) {
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm 'MD5').Hash
        if(!$filesByHash.ContainsKey($hash)) {
            $filesByHash.Set_Item($hash, [System.Collections.ArrayList]@()) | Out-Null
        }
        $filesByHash.Get_Item($hash).Add($file) | Out-Null     
    }

    foreach($hash in $filesByHash.Keys) {        
        $f = $filesByHash.Get_Item($hash)
        if($f.Count -gt 1) {
            $file = $f[0]
            if($file.Length -gt $dedupeMinSizeInBytes) {
                $dedupedname = $file.Name + '_' + $hash + $ntechext
                $dedupefile = Join-Path -Path $dedupeRootPath -ChildPath $dedupedname
                if(!(Test-Path $dedupefile)) {
                    Copy-Item $file.FullName $dedupefile
                }
                foreach($file in $f) {
                    New-Item -Path (Join-Path -Path $file.DirectoryName -ChildPath $dedupedname) -ItemType File -Force | Out-Null
                    Remove-Item -Path $file.FullName -Force | Out-Null
                }
            }
        }
    }
}

foreach($filename in $d.Keys) {
    $fs = $d.Get_Item($filename)
    
    if($files.Count -gt 1) {        
        Dedupe $fs
    }
}