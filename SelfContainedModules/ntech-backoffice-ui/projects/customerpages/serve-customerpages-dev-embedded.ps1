# To run embedded in some site. Requires symlink
# New-Item -Path "<path to customer pages>" -ItemType SymbolicLink -Value "<path to dist>" -Force -ErrorAction Stop
# Where <path to customer page> will be something like C:\Projects\core-naktergal\CoreModules\nCustomerPages\n
# And path to dist something like C:\Projects\core-naktergal\SelfContainedModules\ntech-backoffice-ui\dist\customerpages
ng build customerpages --base-href='/n/' --watch  --output-hashing=all