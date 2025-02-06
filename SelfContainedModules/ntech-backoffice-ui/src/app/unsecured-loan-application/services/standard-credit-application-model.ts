import { ComplexApplicationList } from 'src/app/common-services/complex-application-list';
import { NumberDictionary } from 'src/app/common.types';
import {
    ApplicantInfoModel,
    StandardApplicationModelBase,
    StandardLoanApplicationDocumentModel,
} from 'src/app/shared-application-components/services/standard-application-base';
import {
    CreditRecommendationModel,
    PreScoreRecommendationModel,
    UnsecuredLoanApplicationInitialDataModel,
} from './unsecured-loan-application-api.service';

const FirstNotificationCostDecisionItemPrefix = 'firstNotificationCost_';

export class StandardCreditApplicationModel extends StandardApplicationModelBase {
    constructor(private initialData: UnsecuredLoanApplicationInitialDataModel) {
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
        this.applicantInfoByApplicantNr = this.initialData.ApplicantInfoByApplicantNr;
        this.preScoreRecommendation = initialData.PreScoreRecommendation;
    }

    public readonly applicantInfoByApplicantNr: NumberDictionary<ApplicantInfoModel>;
    public readonly preScoreRecommendation: PreScoreRecommendationModel;

    public hasCurrentCreditDecision() {
        return this.initialData.CurrentCreditDecisionItems.length > 0;
    }

    public getUniqueCurrentCreditDecisionItem(itemName: string) {
        let item = this.initialData.CurrentCreditDecisionItems.find((x) => x.ItemName == itemName && !x.IsRepeatable);
        return item ? item.Value : null;
    }

    public getFirstNotificationCostCreditDecisionItems() {
        if(!this.hasCurrentCreditDecision()) {
            return [];
        }

        return this.initialData.CurrentCreditDecisionItems.filter(x => x.ItemName.startsWith(FirstNotificationCostDecisionItemPrefix) && !x.IsRepeatable).map(x => ({
            itemName: x.ItemName,
            value: x.Value,
            //Pattern: firstNotificationCost_<costCode>_Amount
            costCode: x.ItemName.substring(FirstNotificationCostDecisionItemPrefix.length, x.ItemName.length - '_Amount'.length)
        }));
    }

    public getRepeatableCurrentCreditDecisionItem(itemName: string) {
        let result = this.initialData.CurrentCreditDecisionItems.filter(
            (x) => x.ItemName == itemName && x.IsRepeatable
        ).map((x) => x.Value);
        return result;
    }

    public getCurrentReferenceInterestRatePercent() {
        return this.initialData.CurrentReferenceInterestRatePercent;
    }

    getLoansToSettleAmount(): number {
        let loans = this.getOtherLoans();
        return loans
            .filter((x) => x.shouldBeSettled === true)
            .reduce((sum, current) => sum + current.currentDebtAmount, 0);
    }

    getApplicationDocuments(): StandardLoanApplicationDocumentModel[] {
        return this.initialData.Documents;
    }

    getCustomerPagesApplicationsUrl() {
        return this.initialData.CustomerPagesApplicationsUrl;
    }

    getCurrentCreditDecisionRecommendation(): CreditRecommendationModel {
        return this.initialData.CurrentCreditDecisionRecommendation;
    }
}
