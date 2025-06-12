namespace TwoColumnInformationBlockComponentNs {
    export class TwoColumnInformationBlockController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;

        static $inject = ['$http', '$q', '$filter', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            private $filter: ng.IFilterService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'twoColumnInformationBlock'
        }

        onChanges() {
        }

        getItemValue(i: DataItem) {
            if (i.filterName) {
                if (i.filterName === 'percent') {
                    let f: any = this.$filter('number')
                    return f(i.value, 2) + ' %'
                } else {
                    let f: any = this.$filter(i.filterName)
                    return f(i.value)
                }
            } else {
                return i.value
            }
        }

        lblColCnt(item: DataItem): number {
            if (!this.initialData) {
                return 6
            }
            let v = this.initialData.rightItems.length > 0 ? 6 : 3
            return item && item.extraLabelColumnCount ? v + item.extraLabelColumnCount : v
        }

        valColCnt(item: DataItem): number {
            if (!this.initialData) {
                return 6
            }
            let v = this.initialData.rightItems.length > 0 ? 6 : 9
            return item && item.extraLabelColumnCount ? v - item.extraLabelColumnCount : v
        }
    }

    export class TwoColumnInformationBlockComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = TwoColumnInformationBlockController;
            this.template = `    <div>
        <div class="form-horizontal" ng-if="$ctrl.initialData.rightItems.length > 0">
            <div class="form-group col-xs-12 text-right" ng-if="$ctrl.initialData.viewDetailsUrl">
                <a class="n-anchor" ng-href="{{$ctrl.initialData.viewDetailsUrl}}">View details</a>
            </div>
            <div class="col-xs-6">
                <div class="form-group" ng-repeat="c in $ctrl.initialData.leftItems track by $index">
                    <label class="col-xs-{{$ctrl.lblColCnt(c)}} control-label" ng-if="c.labelText && !c.labelKey">{{c.labelText}}</label>
                    <label class="col-xs-{{$ctrl.lblColCnt(c)}} control-label" ng-if="c.labelKey">{{c.labelKey | translate}}</label>
                    <div class="col-xs-{{$ctrl.valColCnt(c)}} form-control-static copyable">{{$ctrl.getItemValue(c)}}</div>
                </div>
            </div>
            <div class="col-xs-6">
                <div class="form-group" ng-repeat="c in $ctrl.initialData.rightItems track by $index">
                    <label class="col-xs-{{$ctrl.lblColCnt(c)}} control-label" ng-if="c.labelText && !c.labelKey">{{c.labelText}}</label>
                    <label class="col-xs-{{$ctrl.lblColCnt(c)}} control-label" ng-if="c.labelKey">{{c.labelKey | translate}}</label>
                    <div class="col-xs-{{$ctrl.valColCnt(c)}} form-control-static copyable">{{$ctrl.getItemValue(c)}}</div>
                </div>
            </div>
            <div class="clearfix"></div>
        </div>
        <div class="form-horizontal" ng-if="$ctrl.initialData.rightItems.length === 0">
            <div class="form-group col-xs-12 text-right" ng-if="$ctrl.initialData.viewDetailsUrl">
                <a class="n-anchor" ng-href="{{$ctrl.initialData.viewDetailsUrl}}">View details</a>
            </div>
            <div class="form-group" ng-repeat="c in $ctrl.initialData.leftItems track by $index">
                <label class="col-xs-{{$ctrl.lblColCnt(c)}} control-label" ng-if="c.labelText && !c.labelKey">{{c.labelText}}</label>
                <label class="col-xs-{{$ctrl.lblColCnt(c)}} control-label" ng-if="c.labelKey">{{c.labelKey | translate}}</label>
                <div class="col-xs-{{$ctrl.valColCnt(c)}} form-control-static copyable">{{$ctrl.getItemValue(c)}}</div>
            </div>
            <div class="clearfix"></div>
        </div>
    </div>`
        }
    }

    export class InitialData {
        constructor() {
            this.leftItems = []
            this.rightItems = []
        }

        leftItems: DataItem[]
        rightItems: DataItem[]
        viewDetailsUrl: string

        item(isLeft: boolean, value: any, labelKey?: string, labelText?: string, filterName?: string, extraLabelColumnCount?: number): InitialData {
            let item: DataItem = {
                labelText: labelText,
                labelKey: labelKey,
                value: value,
                filterName: filterName,
                extraLabelColumnCount: extraLabelColumnCount
            }
            if (isLeft) {
                this.leftItems.push(item)
            } else {
                this.rightItems.push(item)
            }
            return this
        }

        applicationItem(isLeft: boolean, value: string, editorModel: NTechPreCreditApi.FetchApplicationEditItemDataResponseEditModel, extraLabelColumnCount?: number) {
            let actualValue = ApplicationItemEditorComponentNs.getItemDisplayValueShared(value, editorModel, this.parseDecimalOrNull)
            this.item(isLeft, actualValue, null, editorModel.LabelText || 'Unknown', null, extraLabelColumnCount)
        }

        private isNullOrWhitespace = (input: any) => {
            if (typeof input === 'undefined' || input == null) return true;

            if ($.type(input) === 'string') {
                return $.trim(input).length < 1;
            } else {
                return false
            }
        }

        private isValidDecimal = (value: any) => {
            if (this.isNullOrWhitespace(value))
                return true;
            var v = value.toString()
            return (/^([-]?[0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v)
        }

        private parseDecimalOrNull = (n: any) => {
            if (this.isNullOrWhitespace(n) || !this.isValidDecimal(n)) {
                return null
            }
            if ($.type(n) === 'string') {
                return parseFloat(n.replace(',', '.'))
            } else {
                return parseFloat(n)
            }
        }
    }

    export class InitialDataFromObjectBuilder {
        constructor(private valueObject: any,
            private translationPrefix: string,
            private leftItemNames: string[],
            private rightItemNames: string[]) {
            this.currencyItemNames = {}
            this.mappedValues = {}
        }

        private currencyItemNames: { [index: string]: boolean }
        private mappedValues: { [index: string]: ((value: any) => string) }

        addCurrencyItems(itemNames: string[]) {
            for (let i of itemNames) {
                this.currencyItemNames[i] = true
            }
            return this
        }

        addMappedValue(itemName: string, map: ((value: any) => any)) {
            this.mappedValues[itemName] = map
            return this
        }

        buildInitialData(): InitialData {
            let d = new InitialData()

            let addItems = (isLeft: boolean, itemNames: string[]) => {
                for (let n of itemNames) {
                    let filterName: string = null
                    if (this.currencyItemNames[n]) {
                        filterName = 'currency'
                    }
                    let value = this.valueObject[n]
                    if (this.mappedValues[n]) {
                        value = this.mappedValues[n](value)
                    }

                    d.item(isLeft, value, this.translationPrefix ? this.translationPrefix + n : null, this.translationPrefix ? null : n, filterName)
                }
            }

            addItems(true, this.leftItemNames)
            addItems(false, this.rightItemNames)

            return d
        }
    }

    export class DataItem {
        value: any
        labelText?: string
        labelKey?: string
        filterName?: string
        extraLabelColumnCount?: number
    }
}

angular.module('ntech.components').component('twoColumnInformationBlock', new TwoColumnInformationBlockComponentNs.TwoColumnInformationBlockComponent())