import { Injectable } from "@angular/core";
import { generateUniqueId } from "src/app/common.types";
import { CustomerPagesApiService } from "../../common-services/customer-pages-api.service";

function storageKey(preApplicationId: string) {
    return `UlStandardPreApplicationV1_${preApplicationId}`
}

@Injectable({
    providedIn: 'root',
})
export class UlStandardPreApplicationService {
    constructor(private apiService: CustomerPagesApiService) {

    }

    public async beginPreApplication(initialModel: PreApplicationStoredDataModel) {
        let preApplicationId: string = `p${generateUniqueId(25)}`;
        this.save(preApplicationId, initialModel);
        return preApplicationId;
    }

    public save(preApplicationId: string, model: PreApplicationStoredDataModel) {
        localStorage.setItem(storageKey(preApplicationId), JSON.stringify(model));
    }

    public load(preApplicationId: string) : PreApplicationStoredDataModel | null {
        let value = localStorage.getItem(storageKey(preApplicationId));
        if(!value){
            return null;
        }
        return JSON.parse(value);
    }

    public exists(preApplicationId: string) {
        return !!localStorage.getItem(storageKey(preApplicationId));
    }

    public delete(preApplicationId: string) {
        localStorage.removeItem(storageKey(preApplicationId))
    }

    public createApplication(model: PreApplicationStoredDataModel)  {
        //BEWARE: Do not add bank account sharing here. Instead pass a onetime use id to an already stored result and exapnd it serverside so the user cant manipulate the data.
        let applicationRequest: UlStandardApplicationRequest = {
            requestedAmount: model.requestedLoanAmount,
            loanObjective: model.loanObjective,
            housingType: model.housing,
            housingCostPerMonthAmount: model.housingCostPerMonthAmount,
            otherHouseholdFixedCostsAmount: model.otherHouseholdFixedCostsAmount,
            childBenefitAmount: model.childBenefitAmount,
            preScoreResultId: model.preScoreResultId
        };
        if(model.requestedRepaymentTime.endsWith('d')) {
            applicationRequest.requestedRepaymentTimeInDays = parseInt(model.requestedRepaymentTime.replace('d', ''))
        } else if(model.requestedRepaymentTime.endsWith('m')) {
            applicationRequest.requestedRepaymentTimeInMonths = parseInt(model.requestedRepaymentTime.replace('m', ''))
        }

        applicationRequest.applicants = [];
        for(let applicant of (model.applicants ?? [])) {
            applicationRequest.applicants.push({
                civicRegNr: applicant.civicRegNr,
                email: applicant.email,
                phone: applicant.phone,
                monthlyIncomeAmount: applicant.incomePerMonthAmount,
                employmentStatus: applicant.employment,
                employerName: applicant.employer,
                employerPhone: applicant.employerPhone,
                employedSince: applicant.employedSince,
                employedTo: applicant.employedTo,
                hasConsentedToShareBankAccountData: applicant.hasConsentedToCreditReport,
                hasConsentedToCreditReport: applicant.hasConsentedToCreditReport,
                hasLegalOrFinancialGuardian: applicant.hasLegalOrFinancialGuardian,
                claimsToBeGuarantor: applicant.claimsToBeGuarantor,
                dataShareProviderName: applicant.kreditzCaseId ? 'kreditz' : undefined,
                dataShareSessionId: applicant.kreditzCaseId ??  undefined
            });
        }

        applicationRequest.householdOtherLoans = [];
        for(let otherLoan of (model.loansToSettle ?? [])) {
            applicationRequest.householdOtherLoans.push({
                loanType: otherLoan.loanType,
                currentDebtAmount: otherLoan.currentDebtAmount,
                monthlyCostAmount: otherLoan.monthlyCostAmount,
                shouldBeSettled: false
            });
        }

        applicationRequest.householdChildren = [];
        for(let child of (model.householdChildren)) {
            applicationRequest.householdChildren.push({
                exists: true,
                ageInYears: child.ageInYears,
                sharedCustody: child.sharedCustody
            });
        }

        return this.apiService.postLocal<{ ApplicationNr: string }> ('api/embedded-customerpages/create-ul-standard-application', applicationRequest);
    }

    public getUlStandardWebApplicationSettings() {
        return this.apiService.postLocal<UlStandardWebApplicationSettings>('api/embedded-customerpages/ul-web-application-settings', {});
    }

    public hasAccountSharingDataArrived(caseId: string, skipLoadingIndicator: boolean) {
        return this.apiService.postLocal<{ hasAccountData: boolean }>('api/kz/poll', { caseId }, {skipLoadingIndicator: skipLoadingIndicator });
    }

    /*
    A person has properties like: civicRegNr, email, phone
     */
    public generateTestPersons(
        nrOfApplicants: number,
        isAccepted: boolean,
        useCommonAddress: boolean
    ): Promise<{
        Persons: { Properties: any }[];
    }> {
        return this.apiService.post('nTest', 'Api/TestPerson/GetOrGenerate', {
            persons: nrOfApplicants === 1 ? [{ isAccepted }] : [{ isAccepted }, { isAccepted }],
            useCommonAddress: useCommonAddress,
        }, { isAnonymous: true });
    }

    public async preScoreApplication(application: PreApplicationStoredDataModel) {
        let request :WebPreScorePolicyFilterRequest = {
            scoringVariables: [],
            nrOfApplicants:  1,
            persistResult: true
        };
        if(application?.applicants) {
            request.nrOfApplicants = application.applicants.length;
            let applicantNr = 1;
            for(let applicant of application.applicants) {
                if(applicant.civicRegNr) {
                    let civicRegNrResult = await this.apiService.parseCivicRegNr(applicant.civicRegNr);
                    if(civicRegNrResult.isValid && civicRegNrResult.ageInYears) {
                        request.scoringVariables.push({ name: 'applicantAgeInYears', value: civicRegNrResult.ageInYears.toFixed(0), applicantNr: applicantNr })
                    }
                }
                request.scoringVariables.push({ name: 'applicantIncomePerMonth', value: Math.ceil(applicant.incomePerMonthAmount ?? 0).toFixed(0) , applicantNr: applicantNr });
                if(applicant.employment) {
                    request.scoringVariables.push({ name: 'applicantEmploymentFormCode', value:  applicant.employment, applicantNr: applicantNr })
                }
                request.scoringVariables.push({ name: 'applicantClaimsLegalOrFinancialGuardian', value: applicant.hasLegalOrFinancialGuardian ? 'true' : 'false', applicantNr: applicantNr });
                applicantNr++;
            }
        }
        if(application.loanObjective) {
            request.scoringVariables.push({ name: 'loanObjective', value: application.loanObjective, applicantNr: null })
        }
        return this.apiService.post<WebPreScorePolicyFilterResponse>('NTechHost', 'Api/PreCredit/PolicyFilters/PreScore-WebApplication', request, { isAnonymous: true });
    }
}

export interface UlStandardWebApplicationEnabledSettings {
    loanObjectives: string[],
    repaymentTimes: string[],
    loanAmounts: number[],
    exampleMarginInterestRatePercent: number
    exampleInitialFeeWithheldAmount: number,
    exampleInitialFeeCapitalizedAmount: number,
    exampleInitialFeeOnFirstNotificationAmount: number,
    exampleNotificationFee: number,
    personalDataPolicyUrl: string,
    dataSharing ?: {
        providerName: string,
        useMock: boolean,
        testCivicRegNr ?: string
        iFrameClientId ?: string
        fetchMonthCount: number
    }
}
export interface UlStandardWebApplicationSettings {
    isEnabled: boolean
    settings: UlStandardWebApplicationEnabledSettings
}

export interface BeginPreApplicationRequest {
    requestedLoanAmount: number
    requestedRepaymentTime: string
}

export interface PreApplicationStoredDataModel {
    requestedLoanAmount ?: number
    requestedRepaymentTime ?: string //Like 10d or 3m for 10 days or 3 months
    loanObjective ?: string
    housing ?: string
    housingCostPerMonthAmount ?: number
    otherHouseholdFixedCostsAmount ?: number
    childBenefitAmount ?: number
    preScoreResultId ?: string
    applicants ?: {
        civicRegNr: string
        hasConsentedToCreditReport ?: boolean
        phone: string
        email: string
        incomePerMonthAmount: number
        employment: string
        employer: string
        employerPhone: string
        employedSince: string //yyyy-mm
        employedTo: string //yyyy-mm
        hasLegalOrFinancialGuardian: boolean,
        claimsToBeGuarantor: boolean,
        kreditzCaseId ?: string,
    }[]
    householdChildren ?: {
        ageInYears: number
        sharedCustody: boolean
    }[]
    loansToSettle ?: {
        loanType: string
        monthlyCostAmount: number
        currentDebtAmount: number
    }[]
}

interface UlStandardApplicationRequest {
    requestedAmount?: number;
    requestedRepaymentTimeInMonths?: number | null;
    requestedRepaymentTimeInDays?: number | null;
    loanObjective ?: string;
    housingType?: string;
    housingCostPerMonthAmount?: number | null;
    otherHouseholdFixedCostsAmount?: number | null;
    childBenefitAmount ?: number;
    preScoreResultId ?: string
    applicants ?: UlStandardApplicationRequestApplicantModel[];
    householdOtherLoans?: UlStandardApplicationRequestOtherLoanModel[];
    householdChildren?: {
        exists: boolean
        ageInYears: number
        sharedCustody: boolean
    }[]
}

interface UlStandardApplicationRequestApplicantModel {
    civicRegNr: string;
    firstName?: string;
    lastName?: string;
    email?: string;
    phone?: string;
    birthDate?: Date | null;
    isOnPepList?: boolean | null;
    claimsToBePep?: boolean | null;
    claimsToHaveKfmDebt?: boolean | null;
    civilStatus?: string;
    monthlyIncomeAmount?: number | null;
    employmentStatus?: string;
    employerName?: string;
    employerPhone?: string;
    employedSince?: string | null;
    employedTo?: string | null;
    addressStreet?: string;
    addressZipcode?: string;
    addressCity?: string;
    hasConsentedToCreditReport?: boolean | null;
    hasConsentedToShareBankAccountData?: boolean | null;
    hasLegalOrFinancialGuardian: boolean | null;
    claimsToBeGuarantor: boolean | null;
    dataShareProviderName : string
    dataShareSessionId : string
}

interface UlStandardApplicationRequestOtherLoanModel {
    loanType?: string;
    currentDebtAmount?: number | null;
    monthlyCostAmount?: number | null;
    shouldBeSettled?: boolean | null;
}

export interface WebPreScorePolicyFilterRequest {
    scoringVariables: {
        name: string
        value: string
        applicantNr: number | null
    }[],
    nrOfApplicants: number
    persistResult: boolean
}

export interface WebPreScorePolicyFilterResponse {
    isAcceptRecommended: boolean
    preScoreResultId: string
}