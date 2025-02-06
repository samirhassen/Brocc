import { OfferModel } from './api-service';
import { StringDictionary, DateOnly, NullableNumber, Dictionary } from './common.types';

const StepCount : number = 10

export class ApplicationModel {
    id: string = null

    //Steps
    externalVariables: StringDictionary = null
    offer: OfferModel = null
    nrOfApplicants: NullableNumber = null
    hasCampaignCode: boolean = null    
    campaignCodeOrChannel: CampaignCodeCodeModel = null
    hasConsolidation: boolean = null
    consolidationAmount: NullableNumber = null
    consent: ConsentModel = null
    applicant1: ApplicantModel = null
    applicant2: ApplicantModel = null
    skipCampaignStep: boolean = null

    private getFilledInStepsCount(): number {
        let count = 0
        let steps : any[] = [this.offer, this.nrOfApplicants, this.hasCampaignCode, this.campaignCodeOrChannel]

        let applicants = [this.getApplicant(1)]
        if(this.nrOfApplicants && this.nrOfApplicants.value === 2) {
            applicants.push(this.getApplicant(2))
        }
        for(let a of applicants) {
            steps.push(a.ssn)
            steps.push(a.phone)
            steps.push(a.email)
            if(a.applicantNr === 1) {
                steps.push(a.housing)
                steps.push(a.marriage)
            }
            steps.push(a.costOfLiving)
            steps.push(a.employment)
            steps.push(a.employmentDetails)
            steps.push(a.income)
            steps.push(a.hasOtherLoans)
            steps.push(a.otherLoansOptions)
            steps.push(a.otherLoansAmounts) //TODO: Should be repeated based on selected types
        }

        for(let x of steps) {
            if(x !== null) {
                count = count + 1
            }
        }
        return count
    }

    setDataCalculator(offer: OfferModel) {
        this.offer = offer
    }

    setDataNrOfApplicants(nrOfApplicants: NullableNumber) {
        this.nrOfApplicants = nrOfApplicants
    }

    setDataCampaignCode(hasCampaignCode: boolean) {
        this.hasCampaignCode = hasCampaignCode
    }

    setDataCampaignCodeCode(campaignCode: string) {
        this.campaignCodeOrChannel = {
            isChannel: false,
            code: campaignCode
        }
    }

    setDataCampaignCodeChannel(channelCode: string) {
        this.campaignCodeOrChannel = {
            isChannel: true,
            code: channelCode
        }
    }

    setDataCostOfLiving(costOfLiving: NullableNumber, applicantNr: number) {
        this.getApplicant(applicantNr).costOfLiving = costOfLiving
    }

    setDataEmploymentDetails(employedSinceMonth: DateOnly, applicantNr: number, employer?: string) {
        this.getApplicant(applicantNr).employmentDetails = {
            employer: employer,
            employedSinceMonth: employedSinceMonth
        }
    }

    setDataConsolidationOption(hasConsolidation: boolean) {
        this.hasConsolidation = hasConsolidation
    }

    setDataSsn(ssn: string, applicantNr: number) {
        this.getApplicant(applicantNr).ssn = ssn
    }

    setDataEmail(email: string, applicantNr: number) {
        this.getApplicant(applicantNr).email = email
    }

    setDataPhone(phone: string, applicantNr: number) {
        this.getApplicant(applicantNr).phone = phone
    }

    setDataHousing(housing: string, applicantNr: number) {
        this.getApplicant(applicantNr).housing = housing
    }

    setDataMarriage(marriage: string, applicantNr: number) {
        this.getApplicant(applicantNr).marriage = marriage
    }

    setNrOfChildren(nrOfChildren: NullableNumber, applicantNr: number) {
        this.getApplicant(applicantNr).nrOfChildren = nrOfChildren
    }

    setDataEmployment(employment: string, applicantNr: number) {
        this.getApplicant(applicantNr).employment = employment
    }

    setDataIncome(income: NullableNumber, applicantNr: number) {
        this.getApplicant(applicantNr).income = income
    }

    setDataHasOtherLoans(hasOtherLoans: boolean, applicantNr: number) {
        this.getApplicant(applicantNr).hasOtherLoans = hasOtherLoans
    }

    setDataOtherLoansOptions(options: string[], applicantNr: number) {
        this.getApplicant(applicantNr).otherLoansOptions = options
    }

    setDataOtherLoansAmount(loanType: string, totalAmount: NullableNumber, monthAmount: NullableNumber, applicantNr: number) {
        let a = this.getApplicant(applicantNr)
        if(!a.otherLoansAmounts) {
            a.otherLoansAmounts = {}
        }
        a.otherLoansAmounts[loanType] = {
            loanType: loanType,
            totalAmount: totalAmount,
            monthAmount: monthAmount
        }
    }

    setDataConsolidationAmount(consolidationAmount: NullableNumber) {
        this.consolidationAmount = consolidationAmount
    }

    setDataConsent(satConsent: boolean, informationConsent: boolean, customerConsent: boolean) {
        this.consent = {
            satConsent: satConsent,
            informationConsent: informationConsent,
            customerConsent: customerConsent
        }
    }

    getApplicant(applicantNr: number): ApplicantModel {
        if(applicantNr === 1) {
            if(!this.applicant1) {
                this.applicant1 = new ApplicantModel(1)
            }
            return this.applicant1
        } else if(applicantNr === 2) {
            if(!this.applicant2) {
                this.applicant2 = new ApplicantModel(2)
            }            
            return this.applicant2
        } else {
            throw `invalid applicantNr: ${applicantNr}`
        }
    }

    getProgressPercent() {
        let percent = 0
        let c = this.getFilledInStepsCount()
        for(var i = 0; i<c; i++) {
            percent = percent + ((100 - percent) / StepCount)
        }
        return percent
    }

    getPreFilledCampaignCode(): string {
        if(!this.externalVariables || !this.externalVariables['cc'] || this.externalVariables['cc'].trim().length === 0) {
            return null
        }
        return this.externalVariables['cc'].trim()
    }
}

export class CampaignCodeCodeModel {
    isChannel: boolean = null
    code: string = null
}

export class EmploymentDetailsModel {
    employedSinceMonth: DateOnly = null
    employer: string = null
}

export class ApplicantModel {
    constructor(public applicantNr: number) {

    }
    ssn: string = null
    email: string = null
    phone: string = null
    housing: string = null
    costOfLiving: NullableNumber = null
    marriage: string = null
    nrOfChildren: NullableNumber = null
    employment: string = null
    employmentDetails: EmploymentDetailsModel = null
    income: NullableNumber = null
    hasOtherLoans: boolean = null
    otherLoansOptions: string[] = null    
    otherLoansAmounts: Dictionary<OtherLoansAmountsModel>
}

export class OtherLoansAmountsModel {
    loanType: string
    totalAmount: NullableNumber
    monthAmount: NullableNumber
}

export class ConsentModel {
    satConsent: boolean
    customerConsent: boolean
    informationConsent: boolean
}