Param(
    [Parameter(Mandatory=$True)]
    $rootPath, 
    [Parameter(Mandatory=$True)]
    $nameToRemove)
########################################
#Say c:\temp looks like this
# nCredit\
#  nDeploy\
#      bin\
#      web.config
# nAudit
#  nDeploy\
#      bin\
#      web.config
#
# Then the result of running this script with $rootPath c:\temp and $nameToRemove nDeploy will be
# nCredit\ 
#  bin\
#  web.config
# nAudit
#  bin\
#  web.config
#
#########################################
Get-ChildItem -Directory $rootPath -Recurse | Where-Object {$_.Name -Eq $nameToRemove} | ForEach-Object {
    $item = $_
    Get-ChildItem $item.FullName | ForEach-Object {
        Move-Item -Path $_.FullName  -Destination (Join-Path -Path $item.Parent.FullName -ChildPath $_.Name)
    }
    Remove-Item $item.FullName -Recurse
}
