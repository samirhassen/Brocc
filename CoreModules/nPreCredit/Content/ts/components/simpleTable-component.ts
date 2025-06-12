namespace SimpleTableComponentNs {

    export class SimpleTableController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'simpleTable'
        }

        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }
            
            let rows : RowModel[] = []
            for (let rowData of this.initialData.tableRows) {
                rows.push({columnValues: rowData})
            }
            this.m = {
                headerCells: this.initialData.columns,
                rows: rows
            }
        }
    }

    export class SimpleTableComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = SimpleTableController;
            this.templateUrl = 'simple-table.html';
        }
    }

    export class InitialData {
        columns: HeaderCellModel[]
        tableRows: string[][]
    }
    export class Model {
        headerCells: HeaderCellModel[]
        rows: RowModel[]
    }
    export class HeaderCellModel {
        className: string
        labelText: string
    }
    export class RowModel {        
        columnValues: string[]
    }
}

angular.module('ntech.components').component('simpleTable', new SimpleTableComponentNs.SimpleTableComponent())