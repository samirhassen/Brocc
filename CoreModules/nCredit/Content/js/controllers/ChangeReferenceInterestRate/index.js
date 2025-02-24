var app = angular.module('app', ['ntech.forms', 'ntech.components']);
var ChangeReferenceInterestRateComponentNs;
(function (ChangeReferenceInterestRateComponentNs) {
    var ChangeReferenceInterestRateController = /** @class */ (function () {
        function ChangeReferenceInterestRateController($http, $q, $scope, $timeout, ntechComponentService) {
            var _this = this;
            this.$http = $http;
            this.$q = $q;
            this.$scope = $scope;
            this.$timeout = $timeout;
            this.ntechComponentService = ntechComponentService;
            this.p = {};
            this.currentReferenceInterestRate = initialData.currentReferenceInterestRate;
            this.apiClient = new NTechCreditApi.ApiClient(function (x) { return toastr.error(x); }, $http, $q);
            this.pagingHelper = new NTechTables.PagingHelper($q, $http);
            this.gotoPage(0, null);
            window.scope = $scope;
            window.ctrChangeReferenceInterestRate = this;
            this.apiClient.fetchPendingReferenceInterestChange().then(function (x) {
                if (x) {
                    _this.pending = x;
                    if (initialData.isTest) {
                        _this.testFunctions = [
                            {
                                title: 'Force approve',
                                run: function () {
                                    _this.commitChange(true, null);
                                }
                            }
                        ];
                    }
                }
                else {
                    _this.initial = {
                        newReferenceInterestRate: ''
                    };
                }
            });
        }
        ChangeReferenceInterestRateController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, new NTechCreditApi.ApiClient(toastr.error, this.$http, this.$q), this.$q);
        };
        ChangeReferenceInterestRateController.prototype.calculate = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.isLoading = true;
            this.$timeout(function () {
                _this.calculated = {
                    newReferenceInterestRate: _this.newValue(),
                    userName: initialData.currentUserDisplayName,
                    now: initialData.now
                };
                _this.isLoading = false;
            }, 100);
        };
        ChangeReferenceInterestRateController.prototype.beginChange = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.isLoading = true;
            this.apiClient.beginReferenceInterestChange(this.calculated.newReferenceInterestRate).then(function (x) {
                _this.initial = null;
                _this.calculated = null;
                _this.pending = x;
                _this.isLoading = false;
            });
        };
        ChangeReferenceInterestRateController.prototype.cancelChange = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.isLoading = true;
            this.apiClient.cancelPendingReferenceInterestChange().then(function () {
                _this.pending = null;
                _this.initial = {
                    newReferenceInterestRate: ''
                };
                _this.isLoading = false;
            });
        };
        ChangeReferenceInterestRateController.prototype.commitChange = function (requestOverrideDuality, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.isLoading = true;
            this.apiClient.commitPendingReferenceInterestChange(this.pending.NewInterestRatePercent, requestOverrideDuality).then(function (x) {
                _this.currentReferenceInterestRate = _this.pending.NewInterestRatePercent;
                _this.initial = {
                    newReferenceInterestRate: ''
                };
                _this.pending = null;
                _this.isLoading = false;
                _this.gotoPage(0, evt);
            });
        };
        ChangeReferenceInterestRateController.prototype.isChangeAllowed = function () {
            return (this.f().$valid && (this.isReasonableChange() || this.overrideSafeguard));
        };
        ChangeReferenceInterestRateController.prototype.changeSize = function () {
            if (this.f().$invalid) {
                return NaN;
            }
            return Math.abs(this.newValue() - this.currentReferenceInterestRate);
        };
        ChangeReferenceInterestRateController.prototype.newValue = function () {
            if (this.f().$invalid) {
                return NaN;
            }
            return parseFloat(this.initial.newReferenceInterestRate.replace(',', '.'));
        };
        ChangeReferenceInterestRateController.prototype.isCurrentUser = function (userId) {
            return initialData.currentUserId === userId;
        };
        ChangeReferenceInterestRateController.prototype.f = function () {
            var s = this.$scope;
            return s.f;
        };
        ChangeReferenceInterestRateController.prototype.isValidDecimal = function (v) {
            return ntech.forms.isValidDecimal(v);
        };
        ChangeReferenceInterestRateController.prototype.isReasonableChange = function () {
            if (this.f().$invalid) {
                return false;
            }
            return (this.changeSize() < 5.0);
        };
        ChangeReferenceInterestRateController.prototype.gotoPage = function (pageNr, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.isLoading = true;
            this.apiClient.fetchReferenceInterestChangePage(50, pageNr).then(function (x) {
                _this.isLoading = false;
                _this.files = x;
                _this.updatePaging();
            });
        };
        ChangeReferenceInterestRateController.prototype.updatePaging = function () {
            if (!this.files) {
                return {};
            }
            var h = this.files;
            this.filesPaging = this.pagingHelper.createPagingObjectFromPageResult({
                CurrentPageNr: h.CurrentPageNr,
                TotalNrOfPages: h.TotalNrOfPages
            });
        };
        ChangeReferenceInterestRateController.$inject = ['$http', '$q', '$scope', '$timeout', 'ntechComponentService'];
        return ChangeReferenceInterestRateController;
    }());
    ChangeReferenceInterestRateComponentNs.ChangeReferenceInterestRateController = ChangeReferenceInterestRateController;
    var CalculateModel = /** @class */ (function () {
        function CalculateModel() {
        }
        return CalculateModel;
    }());
    ChangeReferenceInterestRateComponentNs.CalculateModel = CalculateModel;
    var InitialModel = /** @class */ (function () {
        function InitialModel() {
        }
        return InitialModel;
    }());
    ChangeReferenceInterestRateComponentNs.InitialModel = InitialModel;
})(ChangeReferenceInterestRateComponentNs || (ChangeReferenceInterestRateComponentNs = {}));
app.controller('ctr', ChangeReferenceInterestRateComponentNs.ChangeReferenceInterestRateController);
