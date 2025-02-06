var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms'])

ntech.angular.setupTranslation(app)

//NOTE: Dont add a controller here. If it becomes needed on any page, make a separate js file for that page (like overview for instance)