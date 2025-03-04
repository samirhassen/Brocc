var FatcaExportCtrl = /** @class */ (function () {
    function FatcaExportCtrl($q, $http) {
        this.backUrl = initialData.backUrl;
        this.exportProfileName = initialData.exportProfileName;
        this.allYears = [];
        for (var i = -1; i < 2; i++) {
            this.allYears.push(moment(initialData.today).startOf('year').subtract(1, 'days').subtract(i, 'years').format('YYYY'));
        }
        this.exportYear = this.allYears[1]; //Last year is the norm but you will sometimes want to use this year to check what the file will be like.
        this.apiClient = new NTechSavingsApi.ApiClient(function (x) { return toastr.error(x); }, $http, $q);
        window.scope = this;
        this.refresh();
    }
    FatcaExportCtrl.prototype.createFile = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.apiClient.createFatcaExportFile(parseInt(this.exportYear), this.exportProfileName).then(function (x) {
            var hasFailedProfiles = (x.ExportResult && x.ExportResult.FailedProfileNames && x.ExportResult.FailedProfileNames.length > 0);
            var hasErrors = (x.ExportResult && x.ExportResult.Errors && x.ExportResult.Errors.length > 0);
            if (hasFailedProfiles || hasErrors) {
                toastr.warning('There were problems with the export');
            }
            else {
                toastr.info('Ok');
            }
            _this.refresh();
        });
    };
    FatcaExportCtrl.prototype.refresh = function () {
        var _this = this;
        this.isLoading = true;
        this.apiClient.fetchFatcaExportFiles().then(function (x) {
            _this.rows = x.Files;
            _this.isLoading = false;
        });
    };
    FatcaExportCtrl.$inject = ['$q', '$http'];
    return FatcaExportCtrl;
}());
var app = angular.module('app', ['ntech.forms']);
app.controller('fatcaExportCtr', FatcaExportCtrl);
