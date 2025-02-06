import { ComplexApplicationList } from 'src/app/common-services/complex-application-list';
import { Dictionary } from 'src/app/common.types';
import {
    ApplicantInfoModel,
    StandardApplicationModelBase,
} from 'src/app/shared-application-components/services/standard-application-base';
import { MortgageLoanApplicationInitialDataModel } from './mortgage-loan-application-api.service';

export class StandardMortgageLoanApplicationModel extends StandardApplicationModelBase {
    constructor(private initialData: MortgageLoanApplicationInitialDataModel) {
        super(
            initialData.ApplicationNr,
            initialData.NrOfApplicants,
            initialData.ApplicationInfo,
            initialData.CustomerIdByApplicantNr,
            {
                ApplicationVersion: initialData.ApplicationWorkflowVersion,
                Model: initialData.CurrentWorkflowModel,
            },
            ComplexApplicationList.createListsFromFlattenedItems(initialData.ComplexListItems),
            initialData.Enums,
            initialData.CustomerPagesApplicationsUrl
        );
        this.applicationNr = initialData.ApplicationNr;
        this.nrOfApplicants = initialData.NrOfApplicants;
        this.applicantInfoByApplicantNr = initialData.ApplicantInfoByApplicantNr;
    }

    public readonly applicationNr: string;
    public readonly nrOfApplicants: number;
    private applicantInfoByApplicantNr: Dictionary<ApplicantInfoModel>;

    public getApplicantNrs() {
        let n = [];
        for (var applicantNr = 1; applicantNr <= this.nrOfApplicants; applicantNr++) {
            n.push(applicantNr);
        }
        return n;
    }

    public getApplicantInfo(applicantNr: number) {
        return this.applicantInfoByApplicantNr[applicantNr];
    }

    getMortgageLoans(): MortgageLoanApplicationLoanModel[] {
        let otherLoansList = this.getComplexApplicationList('MortgageLoansToSettle', true);
        let loans: MortgageLoanApplicationLoanModel[] = [];
        for (let loanRowNr of otherLoansList.getRowNumbers()) {
            let row = otherLoansList.getRow(loanRowNr, false);
            loans.push({
                complexListRowNr: loanRowNr,
                bankName: row.getUniqueItem('bankName'),
                currentDebtAmount: row.getUniqueItemInteger('currentDebtAmount'),
                currentMonthlyAmortizationAmount: row.getUniqueItemInteger('currentMonthlyAmortizationAmount'),
                shouldBeSettled: row.getUniqueItemBoolean('shouldBeSettled'),
                loanNumber: row.getUniqueItem('loanNumber'),
                interestRatePercent: row.getUniqueItemDecimal('interestRatePercent'),
            });
        }
        return loans;
    }

    hasAnyConnectedMortgageLoans(): boolean {
        let mortgageLoans = this.getComplexApplicationList('MortgageLoansToSettle', true);
        return mortgageLoans.getRowNumbers().length > 0;
    }

    getUniqueCurrentCreditDecisionItem(isFinal: boolean, itemName: string) {
        let source = this.getCurrentCreditDecision(isFinal);
        return source?.CreditDecisionItems?.find((x) => x.ItemName === itemName && !x.IsRepeatable)?.Value;
    }

    getRepeatableCurrentCreditDecisionItem(isFinal: boolean, itemName: string) {
        let source = isFinal
            ? this.initialData?.CurrentFinalCreditDecision
            : this.initialData?.CurrentInitialCreditDecision;
        return source?.CreditDecisionItems?.filter((x) => x.ItemName === itemName && x.IsRepeatable)?.map(
            (x) => x.Value
        );
    }

    getCurrentCreditDecisionRecommendation(isFinal: boolean) {
        return this.getCurrentCreditDecision(isFinal)?.Recommendation;
    }

    isPropertyValuationActive() {
        return this.initialData?.Settings?.IsPropertyValuationActive === true;
    }

    public getAllConnectedCustomerIdsWithRoles() {
        return this.initialData.AllConnectedCustomerIdsWithRoles;
    }

    private getCurrentCreditDecision(isFinal: boolean) {
        let source = isFinal
            ? this.initialData?.CurrentFinalCreditDecision
            : this.initialData?.CurrentInitialCreditDecision;
        return source;
    }
}

export class MortgageLoanApplicationLoanModel {
    complexListRowNr?: number;
    bankName?: string;
    currentDebtAmount?: number;
    currentMonthlyAmortizationAmount?: number;
    interestRatePercent?: number;
    shouldBeSettled?: boolean;
    loanNumber?: string;
}
