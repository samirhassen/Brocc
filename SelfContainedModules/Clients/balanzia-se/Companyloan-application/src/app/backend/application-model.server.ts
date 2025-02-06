import { ApplicationModel } from './application-model';
import { format } from 'util';
import { DateOnly, Dictionary } from './common.types';

export class CreateApplicationRequestModel {
    RequestedAmount: number
    RequestedRepaymentTimeInMonths: number
    LoginSessionDataToken: string //Applicant data is stored serverside to prevent tampering. This token is onetime use
    Applicant: CreateApplicationRequestApplicantModel
    Customer: CreateApplicationRequestCustomerModel    
    AdditionalApplicationProperties: Dictionary<string>
    ExternalVariables: Dictionary<string>
}

export class CreateApplicationRequestCustomerModel {
    Orgnr: string
}

export class CreateApplicationRequestApplicantModel {
    //Civicregnr and name is stored serverside tied to the e-id login
    Email: string
    Phone: string
}

function formatInt(n: number) {
    return Math.round(n).toFixed(0)
}

export function translateApplicationToServerModel(application: ApplicationModel) : CreateApplicationRequestModel {
    let a = application

    let m : CreateApplicationRequestModel = {
        RequestedAmount: a.loanAmount,
        RequestedRepaymentTimeInMonths: a.repaymentTimeInMonths,
        LoginSessionDataToken: a.loginSessionDataToken,
        Applicant: {
            Email: a.applicantEmail,
            Phone: a.applicantPhone
        },
        Customer: {
            Orgnr: a.companyOrgnr
        },
        ExternalVariables: a.externalVariables,
        AdditionalApplicationProperties: null
    } 

    let ap : Dictionary<string> = {}

    a.applicantEmail

    ap['companyAgeInMonths'] = formatInt(a.companyAgeInMonths)
    ap['companyYearlyRevenue'] = formatInt(a.companyRevenue)
    ap['companyYearlyResult'] = formatInt(a.companyResult)
    ap['companyCurrentDebtAmount'] = formatInt(a.hasOtherLoans() ? a.otherLoans.loansAmount : 0)
    ap['loanPurposeCode'] = a.purpose

    m.AdditionalApplicationProperties = ap

    return m
}