namespace MortgageLoanApplicationHouseholdIncomeComponentNs {

    export class MortgageLoanApplicationHouseholdIncomeController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'mortgageLoanApplicationHouseholdIncome'
        }

        onChanges() {
            this.m = null
            if (this.initialData == null) {
                return
            }

            this.apiClient.fetchHouseholdIncomeModel(this.initialData.applicationInfo.ApplicationNr, true).then(x => {
                let model = x.model
                this.m = {
                    onBack: (this.initialData.onBack || this.initialData.backUrl) ? (evt => {
                        if (evt) {
                            evt.preventDefault()
                        }
                        if (this.initialData.onBack) {
                            this.initialData.onBack(this.m.wasChanged ? this.getViewHouseholdGrossTotalMonthlyIncome() : null)
                        } else if (this.initialData.backUrl) {
                            document.location.href = this.initialData.backUrl
                        }
                    }) : null,
                    wasChanged: false,
                    usernames: x.usernames,
                    viewApplicants: model.ApplicantIncomes,
                    documentComments: {
                        applicationInfo: this.initialData.applicationInfo,
                        reloadPageOnWaitingForAdditionalInformation: false,
                        newCommentEventType: 'HouseholdIncomeEdit',
                        showOnlyTheseEventTypes: ['HouseholdIncomeEdit'],
                        alwaysShowAttachedFiles: true
                    },
                    showHeader: !this.initialData.hideHeader
                }
            })
        }

        edit() {
            let e : ApplicantEditModel[] = []
            for (let a of this.m.viewApplicants) {
                e.push({
                    applicantNr: a.ApplicantNr,
                    capitalGrossMonthlyIncome: this.formatNumberForEdit(a.CapitalGrossMonthlyIncome),
                    employmentGrossMonthlyIncome: this.formatNumberForEdit(a.EmploymentGrossMonthlyIncome),
                    serviceGrossMonthlyIncome: this.formatNumberForEdit(a.ServiceGrossMonthlyIncome)
                })
            }
            this.m.editApplicants = e
        }

        cancel() {
            this.m.editApplicants = null
        }

        save() {
            let a: NTechPreCreditApi.HouseholdIncomeApplicantModel[] = []
            for (let e of this.m.editApplicants) {
                a.push({
                    ApplicantNr: e.applicantNr,
                    CapitalGrossMonthlyIncome: this.nullZero(this.parseDecimalOrNull(e.capitalGrossMonthlyIncome)),
                    EmploymentGrossMonthlyIncome: this.nullZero(this.parseDecimalOrNull(e.employmentGrossMonthlyIncome)),
                    ServiceGrossMonthlyIncome: this.nullZero(this.parseDecimalOrNull(e.serviceGrossMonthlyIncome))
                })
            }
            this.apiClient.setHouseholdIncomeModel(this.initialData.applicationInfo.ApplicationNr, {
                ApplicantIncomes: a
            }).then(() => {
                this.apiClient.fetchHouseholdIncomeModel(this.initialData.applicationInfo.ApplicationNr, true).then(result => {
                    this.m.viewApplicants = result.model.ApplicantIncomes
                    this.m.usernames = result.usernames
                    this.m.editApplicants = null
                    this.m.wasChanged = true
                    if (this.initialData.onIncomeChanged) {
                        this.initialData.onIncomeChanged(this.getViewHouseholdGrossTotalMonthlyIncome())
                    }
                })
            })
        }

        getUserDisplayName(userId: number) {
            if (this.m && this.m.usernames) {
                for (let u of this.m.usernames) {
                    if (u.UserId === userId) {
                        return u.DisplayName
                    }
                }
            }
            return 'User ' + userId
        }

        private nullZero(n: number): number {
            return n ? n : 0
        }

        getEditApplicantGrossTotalMonthlyIncome(a: ApplicantEditModel): number {
            if (this.m == null || this.m.editApplicants == null) {
                return null
            }
            let parts = [this.parseDecimalOrNull(a.capitalGrossMonthlyIncome), this.parseDecimalOrNull(a.employmentGrossMonthlyIncome), this.parseDecimalOrNull(a.serviceGrossMonthlyIncome)]
            if (_.some(parts, x => x === null)) {
                return null
            }
            let sum = 0
            for (let p of parts) {
                sum += this.nullZero(p)
            }
            return sum
        }

        getEditHouseholdGrossTotalMonthlyIncome() : number {
            if (this.m == null || this.m.editApplicants == null) {
                return null
            }
            let sum = 0
            for (let a of this.m.editApplicants) {
                let asum = this.getEditApplicantGrossTotalMonthlyIncome(a)
                if (asum === null) {
                    return null
                }
                sum += this.nullZero(asum)
            }
            return sum
        }

        getViewHouseholdGrossTotalMonthlyIncome(): number {
            if (this.m == null || this.m.viewApplicants == null) {
                return null
            }
            let sum = 0
            for (let a of this.m.viewApplicants) {
                sum += this.nullZero(a.CapitalGrossMonthlyIncome) + this.nullZero(a.EmploymentGrossMonthlyIncome) + this.nullZero(a.ServiceGrossMonthlyIncome)
            }
            return sum
        }
    }

    export class MortgageLoanApplicationHouseholdIncomeComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationHouseholdIncomeController;
            this.templateUrl = 'mortgage-loan-application-household-income.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        onBack?: (newIncome: number) => void
        onIncomeChanged?: (newIncome: number) => void
        backUrl?: string
        hideHeader?: boolean
    }

    export class Model {
        onBack: (evt :Event) => void
        documentComments: ApplicationCommentsComponentNs.InitialData        
        wasChanged: boolean
        showHeader: boolean
        viewApplicants: NTechPreCreditApi.HouseholdIncomeApplicantModel[]
        editApplicants?: ApplicantEditModel[]
        usernames?: NTechPreCreditApi.UserIdAndDisplayName[]
    }

    export class ApplicantEditModel {
        applicantNr: number
        capitalGrossMonthlyIncome: string
        employmentGrossMonthlyIncome: string
        serviceGrossMonthlyIncome: string
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationHouseholdIncome', new MortgageLoanApplicationHouseholdIncomeComponentNs.MortgageLoanApplicationHouseholdIncomeComponent())