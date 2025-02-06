namespace BookKeepingRulesEditComponentNs {
    export class BookKeepingRulesEditController extends NTechComponents.NTechComponentControllerBase {
        initialData: ComponentHostNs.ComponentHostInitialData
        m: Model
        editform : ng.IFormController

        static $inject = ['$http', '$q', 'ntechComponentService', 'ntechLocalStorageService', '$scope']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private ntechLocalStorageService: NTechComponents.NTechLocalStorageService,
            private $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'bookkeepingRulesEdit'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.reload()
        }

        reload() {
            this.apiClient.fetchBookKeepingRules().then(x => {
                let m: Model = {
                    accountNames: x.allAccountNames,
                    accountNrByAccountName: x.accountNrByAccountName,
                    allConnections: x.allConnections,
                    ruleRows: x.ruleRows,
                    exportCode: null,
                    importText: null,
                    backUrl: this.initialData.backTarget
                        ? this.initialData.crossModuleNavigateUrlPattern.replace('[[[TARGET_CODE]]]', this.initialData.backTarget)
                        : this.initialData.backofficeMenuUrl,
                    isTest: this.initialData.isTest
                }
                let code : BookKeepingCode = {
                    accountNames: m.accountNames,
                    accountNrByAccountName: m.accountNrByAccountName
                }
                m.exportCode = `B_${btoa(JSON.stringify(code))}_B` 
                
                this.m = m
            })
        }
        
        beginEdit(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.m.edit = {
                accountNrByAccountName: angular.copy(this.m.accountNrByAccountName)
            }
        }

        cancelEdit(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.m.edit = null
        }

        commitEdit(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let initialAccountNrByAccountName = this.m.accountNrByAccountName
            let editedAccountNrByAccountName = this.m.edit.accountNrByAccountName
            this.m.edit = null
            let saves = []
            for (let accountName of this.m.accountNames) {
                if (editedAccountNrByAccountName[accountName] !== initialAccountNrByAccountName[accountName]) {
                    saves.push(this.apiClient.keyValueStoreSet(accountName, 'BookKeepingAccountNrsV1', editedAccountNrByAccountName[accountName]))
                }
            }
            this.$q.all(saves).then(x => {
                this.reload()
            })
        }

        hasConnection(row: NTechCreditApi.BookKeepingRuleDescriptionTableRow, connectionName: string) {
            return row && row.Connections && row.Connections.indexOf(connectionName) >= 0
        }

        getRowAccountNr(row: NTechCreditApi.BookKeepingRuleDescriptionTableRow, isCredit: boolean): {
            isEdited: boolean,
            currentValue: string
            } {

            var result = {
                isEdited: false,
                currentValue: null
            }

            if (!row || !this.m) {
                return result
            }
                        
            result.currentValue = isCredit ? row.CreditAccountNr : row.DebetAccountNr

            var accountName = isCredit ? row.CreditAccountName : row.DebetAccountName
            if (!this.m.edit || !accountName) {                
                return result
            }

            result.isEdited = this.m.accountNrByAccountName[accountName] !== this.m.edit.accountNrByAccountName[accountName]
            if (this.editform[accountName].$invalid) {
                result.currentValue = '-'
            } else {
                result.currentValue = this.m.edit.accountNrByAccountName[accountName]
            }

            return result
        }
    
        onImportTextChanged(importText: string) {
            if (!this.m || this.m.edit || !importText || importText.length < 5) {
                return
            }
            if(importText.substr(0, 2) !== 'B_' || importText.substr(importText.length - 2, 2) !== '_B') {
                return
            }

            let code : BookKeepingCode = JSON.parse(atob(importText.substr(2, importText.length - 4)))
            let missingAccountNamesInImport: string[] = []
            let extraAccountNamesInImport: string[] = []

            this.beginEdit()

            for (let accountName of this.m.accountNames) {
                let importedAccountNr = code.accountNrByAccountName[accountName]
                if (importedAccountNr) {
                    this.m.edit.accountNrByAccountName[accountName] = importedAccountNr
                } else {
                    missingAccountNamesInImport.push(accountName)
                }
            }

            for (let accountName of code.accountNames) {
                if (this.m.accountNames.indexOf(accountName) < 0) {
                    extraAccountNamesInImport.push(accountName)
                }
            }

            if (missingAccountNamesInImport.length > 0 || extraAccountNamesInImport.length > 0) {
                let stringJoin = (a: string[]) => {
                    let result = ''
                    for (let s of a) {
                        if (result.length > 0) {
                            result += ', '
                        }
                        result += s
                    }
                }
                let warningMessage = ''
                if (missingAccountNamesInImport.length > 0) {
                    warningMessage += `These account names are in the import but not here: ${stringJoin(missingAccountNamesInImport)}`
                }
                if (extraAccountNamesInImport.length > 0) {
                    warningMessage += `These account names are in the here but not in the import: ${stringJoin(extraAccountNamesInImport)}`
                }
                toastr.warning(warningMessage)
            }            
        }
    }

    export class BookKeepingCode {
        accountNames: string[]
        accountNrByAccountName: NTechCreditApi.IStringStringDictionary
    }

    export class Model {
        edit?: {
            accountNrByAccountName: NTechCreditApi.IStringStringDictionary
        }
        accountNames: string[]
        accountNrByAccountName: NTechCreditApi.IStringStringDictionary
        allConnections: string[]
        ruleRows: NTechCreditApi.BookKeepingRuleDescriptionTableRow[]
        importText: string
        exportCode: string
        backUrl: string
        isTest: boolean
    }

    export class BookKeepingRulesEditComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = BookKeepingRulesEditController;
            this.templateUrl = 'bookkeeping-rules-edit.html'
        }
    }   
}

angular.module('ntech.components').component('bookkeepingRulesEdit', new BookKeepingRulesEditComponentNs.BookKeepingRulesEditComponent())