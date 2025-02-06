import { ApplicationModel } from './application-model';
import { format } from 'util';
import { DateOnly } from './common.types';
import { TranslateService } from '@ngx-translate/core';

export class CreateApplicationRequestModel {
    UserLanguage: string
    NrOfApplicants: number
    Items: CreateApplicationRequestItemModel[]
    ExternalVariables: ExternalVariableItemModel[]
}

export class CreateApplicationRequestItemModel {
    Group: string
    Name: string
    Value: string
}

export class ExternalVariableItemModel {
    Name: string
    Value: string
}

export class ConsentAnswersModel {
    date: string
    applicantNr: number
    commonTextConsent: string
    linkConsent: ConsentLinkmodel
    headerConsent: string
    customerConsent: CheckConsentModel
    informationConsent: CheckConsentModel
    satConsent: CheckConsentModel
}

export class CheckConsentModel {
    consentChecked: boolean
    consentRawText: string
}

export class ConsentLinkmodel {
    uri: string
    linkRawText: string
}

function formatInt(n: number) {
    return Math.round(n).toFixed(0)
}


function getRawConsentTextJson(application: ApplicationModel, applicantNr: number, translateService: TranslateService) {
    let consentAnswers: ConsentAnswersModel = {
        date: new Date().toLocaleString(),
        applicantNr: applicantNr,
        commonTextConsent: translateService.instant("steps.consent.commonText"),
        linkConsent: {
            uri: translateService.instant('steps.consent.linkUri'),
            linkRawText: translateService.instant('steps.consent.linkText'),
        },
        headerConsent: translateService.instant(`steps.consent.header${applicantNr}`),
        customerConsent: {
            consentChecked: application.consent.customerConsent,
            consentRawText: translateService.instant("steps.consent.customerConsentHeader")
        },
        informationConsent: {
            consentChecked: application.consent.informationConsent,
            consentRawText: translateService.instant("steps.consent.informationConsentHeader")
        },
        satConsent: {
            consentChecked: application.consent.satConsent,
            consentRawText: translateService.instant("steps.consent.satConsentHeader")
        }
    }

    return JSON.stringify(consentAnswers); 
}

export function translateApplicationToServerModel(application: ApplicationModel, userLanguage: string, translateService: TranslateService) : CreateApplicationRequestModel {
   
    let a = application

    let m : CreateApplicationRequestModel = {
       UserLanguage: userLanguage,
       NrOfApplicants: a.nrOfApplicants.value,
       Items: [],
       ExternalVariables: []
    }

    if(a.externalVariables) {
        for(var key of Object.keys(a.externalVariables)) {
            m.ExternalVariables.push({ Name: key, Value: a.externalVariables[key] })
        }
    }

    let addItem = (name: string, group: string, value: string) => m.Items.push({Name: name, Group: group, Value: value})
    
    let offer = a.offer
    addItem('amount', 'application', formatInt(offer.LoanAmount))
    addItem('repaymentTimeInYears', 'application', formatInt(offer.RepaymentTimeInYears))

    let preFilledCampaignCode = a.getPreFilledCampaignCode()
    if(preFilledCampaignCode) {
        addItem('campaignCode', 'application', preFilledCampaignCode)
    } else if(a.hasCampaignCode === true || a.hasCampaignCode === false) {
        addItem('campaignCode', 'application', a.campaignCodeOrChannel.code)
    }
    if(a.hasConsolidation === true) {
        addItem('loansToSettleAmount', 'application', format(a.consolidationAmount.value))
    }
    addItem('applicantsHaveSameAddress', 'application', (a.nrOfApplicants.value === 2).toString())

    for(let applicantNr of a.nrOfApplicants.value === 2 ? [1,2] : [1]) {
        let mainApplicant = application.getApplicant(1)
        let aa = application.getApplicant(applicantNr)
        let g = `applicant${applicantNr}`
        addItem('creditReportConsent', g, a.consent.satConsent.toString()) //NOTE: This is not a typo. The question has changed to ask for both
        addItem('customerConsent', g, a.consent.customerConsent.toString())
        addItem('informationConsent', g, a.consent.informationConsent.toString()) //NOTE: Not stored currently but sent for possible future use serverside
        addItem('approvedSat', g, a.consent.satConsent.toString())
        addItem('civicRegNr', g, aa.ssn)
        addItem('civicRegNrCountry', g, 'FI')
        addItem('email', g, aa.email)
        addItem('phone', g, aa.phone)
        addItem('housing', g, mainApplicant.housing)
        addItem('housingCostPerMonthAmount', g, formatInt(mainApplicant.costOfLiving.value))
        addItem('employment', g, aa.employment)
        addItem('employedSinceMonth', g, DateOnly.format(aa.employmentDetails.employedSinceMonth, 'YYYY-MM'))
        addItem('consentRawJson', g, getRawConsentTextJson(application, applicantNr, translateService));

        if(aa.employmentDetails.employer) {
            addItem('employer', g, aa.employmentDetails.employer)
        }
        addItem('incomePerMonthAmount', g, formatInt(aa.income.value))
        addItem('marriage', g, mainApplicant.marriage)
        if(aa.hasOtherLoans === true) {
            for(let loanType of aa.otherLoansOptions) {
                let lt = aa.otherLoansAmounts[loanType]
                addItem(`${loanType}Amount`, g, formatInt(lt.totalAmount.value))
                addItem(`${loanType}CostPerMonthAmount`, g, formatInt(lt.monthAmount.value))
            }
        }
        
        addItem('nrOfChildren', g, formatInt(applicantNr === mainApplicant.applicantNr ? mainApplicant.nrOfChildren.value: 0))
    }

    return m
}