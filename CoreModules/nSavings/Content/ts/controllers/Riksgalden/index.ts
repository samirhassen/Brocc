class RiksgaldenCtrl {
    static $inject = ['$scope', '$http', '$q']
    constructor(
        private $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        window.scope = this;

        this.backUrl = initialData.backUrl;
        this.firstFileUrlPattern = initialData.firstFileUrlPattern;
        this.secondFileUrlPattern = initialData.secondFileUrlPattern;
        this.alsoEncryptAndSign = 'False';
    }

    isLoading: boolean
    backUrl: string
    firstFileUrlPattern: string
    secondFileUrlPattern: string
    maxBusinessEventId: string
    alsoEncryptAndSign: string
    
    getFirstFileUrl() {
        return this.firstFileUrlPattern.replace('BBBBB', this.alsoEncryptAndSign.toString());
    }

    getSecondFileUrl() {
        if (!this.maxBusinessEventId) {
            return null;
        } else {
            return this.secondFileUrlPattern.replace('BBBBB', this.alsoEncryptAndSign.toString()).replace('NNNNN', this.maxBusinessEventId.toString());
        }
    }
}

var app = angular.module('app', ['ntech.forms'])
app.controller('riksgaldenCtrl', RiksgaldenCtrl)

module RiksgaldenCtrlNs {

}