var PhoneListCtrl = /** @class */ (function () {
    function PhoneListCtrl($scope, $http, $q, $timeout) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        window.scope = this;
        this.workListUrl = initialData.workListUrl;
        this.overdueCount = '';
    }
    PhoneListCtrl.prototype.onBack = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, new NTechCreditApi.ApiClient(toastr.error, this.$http, this.$q), this.$q);
    };
    PhoneListCtrl.prototype.downloadPhoneList = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        var nrOfDueDatesPassedFilter = [];
        if (this.overdueCount) {
            nrOfDueDatesPassedFilter.push(this.overdueCount);
        }
        this.isLoading = true;
        var data = {};
        this.$http({
            method: 'POST',
            url: '/Api/PreCollection/PreviewWorkListCreditNrs',
            data: {
                nrOfDueDatesPassedFilter: nrOfDueDatesPassedFilter
            }
        }).then(function (response) {
            _this.$http({
                method: 'POST',
                url: '/Api/Reports/CreditPhoneList',
                data: { creditNrs: response.data.creditNrs },
                responseType: 'arraybuffer'
            }).then(function (response) {
                _this.isLoading = false;
                var excelMimetype = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
                var blobData = new Blob([response.data], { type: excelMimetype });
                download(blobData, 'phonelist_' + moment().format('YYYY-MM-DD') + '.xlsx', excelMimetype);
            }, function (response) {
                _this.isLoading = false;
                toastr.error('Failed!');
            });
        }, function (response) {
            _this.isLoading = false;
            toastr.error('Failed!');
        });
    };
    PhoneListCtrl.$inject = ['$scope', '$http', '$q'];
    return PhoneListCtrl;
}());
var app = angular.module('app', ['ntech.forms', 'ntech.components']);
app.controller('phoneListCtrl', PhoneListCtrl);
