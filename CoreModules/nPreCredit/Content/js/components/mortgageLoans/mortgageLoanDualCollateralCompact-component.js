var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var MortgageLoanDualCollateralCompactComponentNs;
(function (MortgageLoanDualCollateralCompactComponentNs) {
    var MortgageLoanDualCollateralCompactController = /** @class */ (function (_super) {
        __extends(MortgageLoanDualCollateralCompactController, _super);
        function MortgageLoanDualCollateralCompactController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        MortgageLoanDualCollateralCompactController.prototype.componentName = function () {
            return 'mortgageLoanDualCollateralCompact';
        };
        MortgageLoanDualCollateralCompactController.prototype.onChanges = function () {
            this.reload();
        };
        MortgageLoanDualCollateralCompactController.prototype.reload = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.m = {
                infoBlocks: []
            };
            ComplexApplicationListHelper.fetch(this.initialData.applicationNr, MortgageApplicationCollateralEditComponentNs.ListName, this.apiClient, MortgageApplicationCollateralEditComponentNs.CompactFieldNames).then(function (x) {
                var shownNrs = NTechLinq.where(x.getNrs(), function (nr) {
                    if (_this.initialData.onlyMainCollateral) {
                        return nr === 1;
                    }
                    else if (_this.initialData.onlyOtherCollaterals) {
                        return nr > 1;
                    }
                    else {
                        return true;
                    }
                });
                for (var _i = 0, shownNrs_1 = shownNrs; _i < shownNrs_1.length; _i++) {
                    var nr = shownNrs_1[_i];
                    var id = new TwoColumnInformationBlockComponentNs.InitialData();
                    var uniqueItems = x.getUniqueItems(nr);
                    for (var _a = 0, _b = MortgageApplicationCollateralEditComponentNs.CompactFieldNames; _a < _b.length; _a++) {
                        var fieldName = _b[_a];
                        var m = x.getEditorModel(fieldName);
                        var value = uniqueItems[fieldName];
                        if (m) {
                            id.applicationItem(true, value, m, 3);
                        }
                        else {
                            id.item(true, uniqueItems[fieldName], null, fieldName, null, 3);
                        }
                    }
                    _this.m.infoBlocks.push({
                        data: id,
                        viewDetailsUrl: _this.initialData.allowViewDetails ? _this.getLocalModuleUrl('/Ui/MortgageLoan/Edit-Collateral', [
                            ['applicationNr', _this.initialData.applicationNr],
                            ['listNr', nr.toString()],
                            ['backTarget', _this.initialData.viewDetailsUrlTargetCode]
                        ]) : null,
                        allowDelete: _this.initialData.allowDelete && nr > 1,
                        nr: nr
                    });
                }
            });
        };
        MortgageLoanDualCollateralCompactController.prototype.getInfoBlockWidthClass = function (infoBlock) {
            var width = 6;
            if (!infoBlock.viewDetailsUrl) {
                width += 2;
            }
            if (!infoBlock.allowDelete) {
                width += 4;
            }
            return "col-sm-".concat(width);
        };
        MortgageLoanDualCollateralCompactController.prototype.deleteCollateral = function (nr, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (nr <= 1) {
                return;
            }
            ComplexApplicationListHelper.deleteRow(this.initialData.applicationNr, MortgageApplicationCollateralEditComponentNs.ListName, nr, this.apiClient).then(function (x) {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanDualCollateralCompactController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanDualCollateralCompactController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanDualCollateralCompactComponentNs.MortgageLoanDualCollateralCompactController = MortgageLoanDualCollateralCompactController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanDualCollateralCompactComponentNs.Model = Model;
    var MortgageLoanDualCollateralCompactComponent = /** @class */ (function () {
        function MortgageLoanDualCollateralCompactComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanDualCollateralCompactController;
            this.template = "<div ng-if=\"$ctrl.m\">\n    <div class=\"row {{$index > 0 ? 'pt-3' : '0'}}\" ng-repeat=\"i in $ctrl.m.infoBlocks track by $index\">\n        <div class=\"{{$ctrl.getInfoBlockWidthClass(i)}}\">\n            <two-column-information-block initial-data=\"i.data\">\n            </two-column-information-block>\n        </div>\n        <div class=\"col-sm-2 text-right\" ng-if=\"i.viewDetailsUrl\">\n            <a class=\"n-anchor\" ng-href=\"{{i.viewDetailsUrl}}\">View details</a>\n        </div>\n        <div class=\"col-sm-4 text-right\" ng-if=\"i.allowDelete\">\n            <button class=\"n-icon-btn n-red-btn\" ng-click=\"$ctrl.deleteCollateral(i.nr, $event)\"><span class=\"glyphicon glyphicon-minus\"></span></button>\n        </div>\n    </div>\n</div>";
        }
        return MortgageLoanDualCollateralCompactComponent;
    }());
    MortgageLoanDualCollateralCompactComponentNs.MortgageLoanDualCollateralCompactComponent = MortgageLoanDualCollateralCompactComponent;
})(MortgageLoanDualCollateralCompactComponentNs || (MortgageLoanDualCollateralCompactComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanDualCollateralCompact', new MortgageLoanDualCollateralCompactComponentNs.MortgageLoanDualCollateralCompactComponent());
