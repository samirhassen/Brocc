import { StringDictionary, DateOnly, NullableNumber, Dictionary } from './common.types';
import { CreateApplicationResponseModel } from './real-api.service';

const StepCount : number = 11

export class ApplicationModel {
    constructor() {
        
    }

    hasBeenSentToServer: boolean
    serverResponse: CreateApplicationResponseModel

    id: string = null
    civicRegNr: string = null
    firstName: string = null
    lastName: string = null
    loginSessionDataToken: string = null
    externalVariables: StringDictionary = null

    companyOrgnr: string = null
    loanAmount: number = null
    repaymentTimeInMonths: number = null
    purpose: string = null
    applicantEmail: string = null
    applicantPhone: string = null
    companyAgeInMonths: number = null
    companyResult: number = null
    companyRevenue: number = null
    otherLoans: OtherLoansModel = null

    //Steps
    consent: ConsentModel = null

    private getFilledInStepsCount(): number {
        let count = 0
        let steps : any[] = [this.companyOrgnr, this.loanAmount, this.repaymentTimeInMonths, this.purpose, this.applicantEmail, 
            this.applicantPhone, this.companyAgeInMonths, this.companyResult, this.companyRevenue, this.otherLoans]

        for(let x of steps) {
            if(x !== null) {
                count = count + 1
            }
        }
        return count
    }

    setOrgnr(orgnr: string) {
        this.companyOrgnr = orgnr
    }

    setLoanAmount(loanAmount: number) {
        this.loanAmount = loanAmount
    }

    setRepaymentTime(repaymentTimeInMonths: number) {
        this.repaymentTimeInMonths = repaymentTimeInMonths
    }

    setDataConsent(creditReportConsent: boolean) {
        this.consent = {
            creditReportConsent: creditReportConsent
        }
    }

    setPurpose(purpose: string) {
        this.purpose = purpose
    }

    setApplicantEmail(email: string) {
        this.applicantEmail = email
    }

    setApplicantPhone(phone: string) {
        this.applicantPhone = phone
    }

    setCompanyAgeInMonths(companyAgeInMonths: number) {
        this.companyAgeInMonths = companyAgeInMonths
    }

    setCompanyResult(companyResult: number) {
        this.companyResult = companyResult
    }

    setCompanyRevenue(companyRevenue: number) {
        this.companyRevenue = companyRevenue
    }

    setHasOtherLoans(hasLoans: boolean) {
        let p = this.otherLoans
        this.otherLoans = {
            hasLoans: hasLoans,
            loansAmount: p && p.hasLoans ? p.loansAmount : null
        }
    }

    setOtherLoansAmount(amount: number) {
        if(amount) {
            this.otherLoans = {
                hasLoans: true,
                loansAmount: amount
            }
        } else {
            this.otherLoans = {
                hasLoans: false,
                loansAmount: null
            }
        }
    }

    hasOtherLoans() {
        return this.otherLoans && this.otherLoans.hasLoans
    }

    getProgressPercent() {
        let c = this.getFilledInStepsCount()

        let p = c / StepCount
        if(p > 1) {
            p = 1
        }

        return Math.round(100 * p)
    }
}

export class ConsentModel {
    creditReportConsent: boolean
}

export class OtherLoansModel {
    hasLoans: boolean
    loansAmount: number
}
