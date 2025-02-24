var app = angular.module('app', []);
var NavMenuCtr = /** @class */ (function () {
    function NavMenuCtr($scope, $http, $window, $timeout) {
        this.$http = $http;
        this.$window = $window;
        this.$timeout = $timeout;
        var i = initialData;
        $scope.userRoles = {};
        for (var _i = 0, _a = i.currentUser.menuRoles; _i < _a.length; _i++) {
            var roleName = _a[_i];
            $scope.userRoles[roleName] = true;
        }
        $scope.$watch('navigateToUrl', function () {
            if ($scope.navigateToUrl) {
                $scope.isLoading = true;
                $timeout(function () {
                    window.location.href = $scope.navigateToUrl;
                });
            }
        });
        var toItem = function (groupName, subGroupName, item) {
            var i = angular.copy(item);
            i.groupName = groupName;
            i.subGroupName = subGroupName;
            i.isPermitted = !i.requiredRole || $scope.userRoles[i.requiredRole];
            return i;
        };
        var allItems = [];
        var CoreGroupName = 'Core';
        var _loop_1 = function (g) {
            allItems.push.apply(allItems, _.map(g.items, function (x) { return toItem(g.groupName, CoreGroupName, x); }));
            var _loop_3 = function (sg) {
                allItems.push.apply(allItems, _.map(sg.items, function (x) { return toItem(g.groupName, sg.name, x); }));
            };
            for (var _e = 0, _f = g.subGroups; _e < _f.length; _e++) {
                var sg = _f[_e];
                _loop_3(sg);
            }
        };
        for (var _b = 0, _c = i.menuGroups; _b < _c.length; _b++) {
            var g = _c[_b];
            _loop_1(g);
        }
        var products = [];
        var productNames = _.uniq(_.map(allItems, function (x) { return x.subGroupName; }));
        var _loop_2 = function (productName) {
            var productItems = _.filter(allItems, function (x) { return x.subGroupName === productName; });
            var productGroups = [];
            var tmp = _.groupBy(productItems, function (x) { return x.groupName; });
            for (var groupName in tmp) {
                var items = tmp[groupName];
                productGroups.push({ groupName: groupName, items: items, hasAnyPermittedItem: _.any(items, function (x) { return x.isPermitted; }) });
            }
            var productGroupGroups = [];
            for (var _g = 0, _h = _.chain(productGroups).groupBy(function (e, i) { return Math.floor(i / 3); }).toArray().value(); _g < _h.length; _g++) {
                var g = _h[_g];
                productGroupGroups.push({ groups: g, hasAnyPermittedItem: _.any(g, function (x) { return x.hasAnyPermittedItem; }) });
            }
            products.push({
                className: productName === 'Core' ? null : productName.toLowerCase(),
                productName: productName,
                hasAnyPermittedItem: _.any(productGroupGroups, function (x) { return x.hasAnyPermittedItem; }),
                groupGroups: productGroupGroups
            });
        };
        for (var _d = 0, productNames_1 = productNames; _d < productNames_1.length; _d++) {
            var productName = productNames_1[_d];
            _loop_2(productName);
        }
        $scope.m = {
            products: _.sortBy(products, function (x) { return i.subGroupOrder.indexOf(x.productName); }),
            allowShowDetails: i.allowShowDetails
        };
        var d = document;
        d.scope = $scope;
    }
    NavMenuCtr.$inject = ['$scope', '$http', '$window', '$timeout'];
    return NavMenuCtr;
}());
app.controller('ctr', NavMenuCtr);
