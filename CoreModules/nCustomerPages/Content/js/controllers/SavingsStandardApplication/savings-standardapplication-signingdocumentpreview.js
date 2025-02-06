var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms'])

ntech.angular.setupTranslation(app)

app.controller('ctr', ['$scope', '$http', '$q', '$translate', '$timeout', function ($scope, $http, $q, $translate, $timeout) {
    window.scope = $scope

    $scope.publicDocumentUrl = initialData.publicDocumentUrl
    PDFObject.embed(initialData.publicDocumentUrl, "#pdf-container", { fallbackLink: "<p>Your browser does not support showing the document directly. Please use the link below to download and view it before signing.</p>" });

    $scope.sign = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.isLoading = true
        $timeout(function () {
            document.location = initialData.signUrl
        })        
    }
    //initialData.signUrl
    //initialData.publicDocumentUrl    
    //initialData.cancelUrl
}])