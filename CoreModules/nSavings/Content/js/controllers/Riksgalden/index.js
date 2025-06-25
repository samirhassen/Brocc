class RiksgaldenCtrl {
    constructor($scope, $http, $q, $timeout) {
        this.$scope = $scope;
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        window.scope = this;
        this.backUrl = initialData.backUrl;
        this.firstFileUrlPattern = initialData.firstFileUrlPattern;
        this.secondFileUrlPattern = initialData.secondFileUrlPattern;
        this.alsoEncryptAndSign = 'False';
    }
    getFirstFileUrl() {
        return this.firstFileUrlPattern.replace('BBBBB', this.alsoEncryptAndSign.toString());
    }
    getSecondFileUrl() {
        if (!this.maxBusinessEventId) {
            return null;
        }
        else {
            return this.secondFileUrlPattern.replace('BBBBB', this.alsoEncryptAndSign.toString()).replace('NNNNN', this.maxBusinessEventId.toString());
        }
    }
}
RiksgaldenCtrl.$inject = ['$scope', '$http', '$q'];
var app = angular.module('app', ['ntech.forms']);
app.controller('riksgaldenCtrl', RiksgaldenCtrl);
