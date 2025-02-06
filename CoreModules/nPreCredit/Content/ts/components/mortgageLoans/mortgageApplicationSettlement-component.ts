namespace MortgageApplicationSettlementComponentNs {

    export class MortgageApplicationSettlementController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData
        m: Model
        
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageApplicationSettlement'
        }

        isSettlementAllowed() {
            return this.initialData
                && this.initialData.applicationInfo.IsActive
                && this.initialData.workflowModel.areAllStepBeforeThisAccepted(this.initialData.applicationInfo)
                && !this.initialData.workflowModel.isStatusAccepted(this.initialData.applicationInfo)
        }

        onChanges() {
            this.m = null;

            if (!this.initialData) {
                return
            }

            if (!(this.isSettlementAllowed() || this.initialData.workflowModel.isStatusAccepted(this.initialData.applicationInfo))) {
                return 
            }

            this.apiClient.fetchMortgageLoanSettlementData(this.initialData.applicationInfo).then(result => {
                let amd = result.AmortizationPlanDocument ? angular.copy(result.AmortizationPlanDocument) : null
                if (amd) {
                    amd.DownloadUrl = NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(amd)
                }

                this.m = {
                    AmortizationModel: result.AmortizationModel,
                    AmortizationPlanDocument: amd,
                    FinalOffer: result.FinalOffer,
                    LoanTypeCode: result.LoanTypeCode
                }
                if (result.PendingSettlementPayment) {
                    this.m.PendingSettlementPayment = result.PendingSettlementPayment
                } else {
                    this.m.Edit = {
                        CurrentLoans : []
                    }
                    for (let loan of result.CurrentLoansModel.Loans) {
                        this.m.Edit.CurrentLoans.push({
                            BankName: loan.BankName,
                            MonthlyAmortizationAmount: loan.MonthlyAmortizationAmount,
                            LastKnownCurrentBalance: loan.CurrentBalance,
                            LoanNr: loan.LoanNr,
                            ActualLoanAmount: '',
                            InterestDifferenceAmount: ''
                        })
                    }
                }
            })
        }

        calculateSettlementSuggestion(evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            let actualLoanAmount = this.getEditActualLoanAmount()
            let interestDifferenceAmount = this.getEditInterestDifferenceAmount()
            if (actualLoanAmount == null || interestDifferenceAmount == null) {
                return
            }
            
            this.m.Preview = {
                applicationNr: this.initialData.applicationInfo.ApplicationNr,
                interestDifferenceAmount: interestDifferenceAmount,
                actualLoanAmount: actualLoanAmount,
                grantedLoanAmount: this.getGrantedLoanAmount(),
                totalPaidAmount: interestDifferenceAmount + actualLoanAmount,
                actualVsGrantedDifferenceAmount: this.getGrantedLoanAmount() - actualLoanAmount
            }
        }

        scheduleOutgoingPayment(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.scheduleMortgageLoanOutgoingSettlementPayment(this.m.Preview.applicationNr,
                this.m.Preview.interestDifferenceAmount,
                this.m.Preview.actualLoanAmount).then(() => {
                toastr.info('Ok')
                this.signalReloadRequired();
            })
        }

        cancelScheduledOutgoingPayment(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.cancelScheduledMortgageLoanOutgoingSettlementPayment(this.initialData.applicationInfo.ApplicationNr).then(() => {
                toastr.info('Ok')
                this.signalReloadRequired();
            })
        }

        createNewLoan(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m || !this.m.PendingSettlementPayment) {
                return
            }
            this.apiClient.createMortgageLoan(this.initialData.applicationInfo.ApplicationNr).then(result => {
                toastr.info('Ok')
                this.signalReloadRequired();
            })
        }

        private getEditSum( getFieldValue: (loan: EditMortageLoanCurrentLoansLoanModel) => string) {
            if (!this.m || !this.m.Edit || !this.m.Edit.CurrentLoans) {
                return null
            }
            let sum = 0
            for (let loan of this.m.Edit.CurrentLoans) {
                let amount = this.parseDecimalOrNull(getFieldValue(loan))
                if (amount === null) {
                    return null
                }
                sum = sum + amount
            }
            return sum
        }

        getGrantedLoanAmount() {
            if (!this.m || !this.m.AmortizationModel) {
                return null
            }
            return this.m.AmortizationModel.CurrentLoanAmount
        }

        getEditActualLoanAmount() {
            return this.getEditSum(x => x.ActualLoanAmount)
        }

        getEditInterestDifferenceAmount() {
            return this.getEditSum(x => x.InterestDifferenceAmount)
        }
    }

    class Model  {
        LoanTypeCode: string
        FinalOffer: any
        AmortizationPlanDocument: any
        PendingSettlementPayment?: NTechPreCreditApi.MortgageLoansSettlementPendingModel
        AmortizationModel: NTechPreCreditApi.MortgageLoanAmortizationBasisModel
        Edit?: EditModel      
        Preview?: PreviewModel
    }

    class EditModel {
        CurrentLoans: EditMortageLoanCurrentLoansLoanModel[]
    }

    class EditMortageLoanCurrentLoansLoanModel {
        BankName: string
        MonthlyAmortizationAmount?: number
        LastKnownCurrentBalance?: number
        LoanNr: string
        ActualLoanAmount: string
        InterestDifferenceAmount: string
    }

    class PreviewModel {
        applicationNr: string
        grantedLoanAmount: number
        actualLoanAmount: number
        actualVsGrantedDifferenceAmount: number
        interestDifferenceAmount: number
        totalPaidAmount: number

    }
    
    export class MortgageApplicationSettlementComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationSettlementController;
            this.templateUrl = 'mortgage-application-settlement.html';
        }
    }
}

angular.module('ntech.components').component('mortgageApplicationSettlement', new MortgageApplicationSettlementComponentNs.MortgageApplicationSettlementComponent())