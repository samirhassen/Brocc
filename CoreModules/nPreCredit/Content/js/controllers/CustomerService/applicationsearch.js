var app = angular.module('app', ['ntech.forms']);
app.controller('ctr', ['$scope', '$http', '$q', '$timeout', function ($scope, $http, $q, $timeout) {
        $scope.s = {};
        $scope.backUrl = initialData.backUrl;
        $scope.search = function (civicRegNr, applicationNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!civicRegNr && !applicationNr || (civicRegNr && $scope.searchform.civicRegNr.$invalid)) {
                return;
            }
            var f = {};
            f.backUrl = initialData.backUrl;
            f.civicRegNr = civicRegNr;
            f.applicationNr = applicationNr;
            $http({
                method: 'POST',
                url: initialData.searchUrl,
                data: f
            }).then(function successCallback(response) {
                if (response.data.redirectToUrl) {
                    document.location = response.data.redirectToUrl;
                }
                else {
                    toastr.warning('No hit');
                }
            }, function errorCallback(response) {
                location.reload();
            });
        };
        function isNullOrWhitespace(input) {
            if (typeof input === 'undefined' || input == null)
                return true;
            if ($.type(input) === 'string') {
                return $.trim(input).length < 1;
            }
            else {
                return false;
            }
        }
        $scope.isValidCivicNr = function (value) {
            if (isNullOrWhitespace(value))
                return true;
            return ntech.fi.isValidCivicNr(value);
        };
        window.scope = $scope;
    }]);
