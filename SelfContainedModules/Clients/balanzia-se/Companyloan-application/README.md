# BalanziaSeCompanyLoanApplication

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 7.2.3.

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The app will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory. Use the `--prod` flag for a production build.

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

## Running end-to-end tests

Run `ng e2e` to execute the end-to-end tests via [Protractor](http://www.protractortest.org/).

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI README](https://github.com/angular/angular-cli/blob/master/README.md).

##Linking into the a site
During development, link the folder into the app using New-Junction -LiteralPath "C:\Projects\Näktergal\Trunk\Naktergal\nCustomerPages\a" -TargetPath "C:\Projects\Git\balanzia-se-companyloan-application\dist\balanzia-se-companyloan-application"
and then run 
>> ng build --watch --base-href='/a/'
 to enable live development. (F5 after edit required) Note that the trailing / is important or resources wont resolve properly.

In production copy the content of dist/balanzia-se-companyloan-application into the /a folder of some site in either the build process or deploy process.

You can also test the application by itself using
>> ng serve

##Testing on local wifi
Figure out you local ip
>> ipconfig
Look for 'Something Wifi' and 'IPv4 Address'. It should be something like 192.168.x.xxx or similar unroutable address.

Then run the app using
>> ng serve --host [your ip]

##Merge to acceptancetest
Show all  branches
>> git show-ref

##Merge to acceptancetest
>> git checkout master
>> git pull
>> git checkout acceptancetest
>> git pull
>> git merge master
>> git push