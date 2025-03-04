var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
if (ntech.angular) {
    ntech.angular.setupTranslation(app);
}
var ComponentHostCtr = /** @class */ (function () {
    function ComponentHostCtr($scope, $http, $q, $timeout, $translate, ntechComponentService, trafficCop, ntechLog) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        this.$translate = $translate;
        this.ntechComponentService = ntechComponentService;
        var onError = function (errMsg) {
            toastr.error(errMsg);
        };
        var apiClient = new NTechCreditApi.ApiClient(onError, $http, $q);
        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(function () {
            $scope.isLoading = trafficCop.pending.all > 0;
        });
        var d = initialData;
        d.apiClient = apiClient;
        d.isLoading = function () { return $scope.isLoading; };
        d.setIsLoading = function (x) { return $scope.isLoading = x; };
        d.testFunctions = new ComponentHostNs.TestFunctionsModel();
        if (d.isTest) {
            $scope.testFunctions = d.testFunctions;
        }
        $scope.componentInitialData = d;
        window.scope = $scope; //for console debugging
    }
    ComponentHostCtr.$inject = ['$scope', '$http', '$q', '$timeout', '$translate', 'ntechComponentService', 'trafficCop', 'ntechLog'];
    return ComponentHostCtr;
}());
app.controller('ctr', ComponentHostCtr);
var ComponentHostNs;
(function (ComponentHostNs) {
    var TestFunctionsModel = /** @class */ (function () {
        function TestFunctionsModel() {
            this.items = [];
        }
        TestFunctionsModel.prototype.clearItems = function (exceptForScopeName) {
            var i2 = [];
            for (var _i = 0, _a = this.items; _i < _a.length; _i++) {
                var i = _a[_i];
                if (!!exceptForScopeName && i.scopeName === exceptForScopeName) {
                    i2.push(i);
                }
            }
            this.items = i2;
        };
        TestFunctionsModel.prototype.generateUniqueScopeName = function () {
            return NTechComponents.generateUniqueId(6);
        };
        TestFunctionsModel.prototype.addLink = function (scopeName, text, linkUrl) {
            this.clearItems(scopeName);
            this.items.push({ scopeName: scopeName, text: text, isLink: true, linkUrl: linkUrl });
        };
        TestFunctionsModel.prototype.addFunctionCall = function (scopeName, text, functionCall) {
            this.clearItems(scopeName);
            this.items.push({
                scopeName: scopeName, text: text, isFunctionCall: true, functionCall: function (evt) {
                    if (evt) {
                        evt.preventDefault();
                    }
                    functionCall();
                }
            });
        };
        return TestFunctionsModel;
    }());
    ComponentHostNs.TestFunctionsModel = TestFunctionsModel;
    var TestFunctionItem = /** @class */ (function () {
        function TestFunctionItem() {
        }
        return TestFunctionItem;
    }());
    ComponentHostNs.TestFunctionItem = TestFunctionItem;
})(ComponentHostNs || (ComponentHostNs = {}));
