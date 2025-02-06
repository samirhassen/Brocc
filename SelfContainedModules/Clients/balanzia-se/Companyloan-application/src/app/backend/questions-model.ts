import { StringDictionary, DateOnly, NullableNumber, Dictionary } from './common.types';
import { CreateApplicationResponseOfferModel, CompanyInformationModel } from './real-api.service';
import { CompanyLoanExtendedOfferModel, SubmitAdditionalQuestionsRequest } from './api-service';
import { QuestionsServerRequest, QuestionModel } from './questions-server-request';
import { environment } from 'src/environments/environment.embeddeddev';

const StepCount : number = 14

export class QuestionsModel {
    constructor() {

    }

    hasBeenSentToServer: boolean

    id: string = null
    applicationNr: string = null
    submissionToken: string = null
    offer: CompanyLoanExtendedOfferModel = null
    offerStepPassed: boolean = null
    companyInformation: CompanyInformationModel = null
    bankAccountNr: string = null
    bankAccountNrType: string = null
    collateral: QuestionsCollateralModel = null
    beneficialOwners: BeneficialOwnersModel = null
    cashHandling: CashHandlingModel = null
    currencyExchange: CurrencyExchangeModel = null
    consent: ConsentModel = null
    sector: CompanySectorModel = null
    psp: PaymentServiceProviderModel = null
    employeeCount: EmployeeCountModel = null
    extraPayments: ExtraPaymentsModel = null
    paymentSource: PaymentSourceModel = null

    getFilledInStepsCount() {
        if(this.consent) {
            return StepCount
        } else {
            let count = 0
            for(let s of [this.offerStepPassed, this.collateral, this.beneficialOwners, this.sector,
                this.cashHandling, this.currencyExchange, this.bankAccountNr, this.consent, this.psp,
                this.employeeCount, this.extraPayments, this.paymentSource]) {
                if(s) {
                    count += 1
                }
            }
            return count
        }
    }

    getProgressPercent() {
        let c = this.getFilledInStepsCount()

        let p = c / StepCount
        if(p > 1) {
            p = 1
        }

        return Math.round(100 * p)
    }

    toServerModel() : QuestionsServerRequest {
        let r : QuestionsServerRequest = {
            QuestionsSubmissionToken: this.submissionToken,
            SkipRemoveToken: !environment.production, //NOTE: Even if you hardcode this to true it wont work in production since the serverside code will ignore it
            BankAccountNr: this.bankAccountNr,
            BankAccountNrType: this.bankAccountNrType,
            Collateral: null,
            BeneficialOwners: null,
            ProductQuestions: null
        }

        //-----------------------------------
        //----- Collateral ------------------
        //-----------------------------------
        if(!this.collateral) {
            throw new Error('Missing collateral')
        }
        if(this.collateral.isApplicant) {
            r.Collateral = { IsApplicant: true, NonApplicantPerson: null }
        } else {
            let p = this.collateral.nonApplicantPerson
            //NOTE: Collaterals dont get asked the pep question
            r.Collateral = {
                IsApplicant: false,
                NonApplicantPerson: {
                    CivicNr: p.civicNr,
                    FirstName: p.firstName,
                    LastName: p.lastName,
                    Email: p.email,
                    Phone: p.phone,
                    AnsweredYesOnPepQuestion: null,
                    AnsweredYesOnIsUSPersonQuestion: null,
                    PepRole: null
                }
            }
        }

        //-----------------------------------
        //----- BeneficialOwners-------------
        //-----------------------------------
        if(!this.beneficialOwners) {
            throw new Error('Missing beneficial owners')
        }
        r.BeneficialOwners = []
        if(this.beneficialOwners.hasBeneficialOwners1) {
            for(let o of this.beneficialOwners.beneficialOwners) {
                r.BeneficialOwners.push({
                    CivicNr: o.civicNr,
                    FirstName: o.firstName,
                    LastName: o.lastName,
                    AnsweredYesOnPepQuestion: o.isPep,
                    PepRole: null,
                    OwnershipPercent: o.ownershipPercent,
                    Connection: null,
                    AnsweredYesOnIsUSPersonQuestion: o.isUsPerson
                })
            }
        }

        r.ProductQuestions = []

        this.addCashHandlingQuestionsToRequest(r.ProductQuestions)
        this.addCurrencyExchangeQuestions(r.ProductQuestions)
        this.addSectorQuestion(r.ProductQuestions)
        this.addPspQuestion(r.ProductQuestions)
        this.addEmployeeCountQuestions(r.ProductQuestions)
        this.addExtraPaymentsOptionQuestion(r.ProductQuestions)
        this.addPaymentSourceQuestions(r.ProductQuestions)

        return r
    }

    addPaymentSourceQuestions(questions: QuestionModel[]) {
        let s = this.paymentSource
        if(!s) {
            throw new Error('Missing payment source')
        }
        questions.push({
            QuestionCode: 'paymentSource',
            QuestionText: 'Varifrån kommer pengarna?',
            AnswerCode: s.paymentSourceCode,
            AnswerText: s.paymentSourceText
        })
    }

    addExtraPaymentsOptionQuestion(questions: QuestionModel[]) {
        let e = this.extraPayments
        if(!e) {
            throw new Error('Missing extra payments')
        }
        questions.push({
            QuestionCode: 'extraPayments',
            QuestionText: 'Planeras extra betalningar?',
            AnswerCode: this.extraPayments.areExtraPaymentsExpected ? 'true' : 'false',
            AnswerText: this.extraPayments.areExtraPaymentsExpected ? 'Ja' : 'Nej',
        })
    }

    addEmployeeCountQuestions(questions: QuestionModel[]) {
        let c = this.employeeCount
        if(!c) {
            throw new Error('Missing employee count')
        }
        questions.push({
            QuestionCode: 'companyEmployeeCount',
            QuestionText: 'Antal anställda?',
            AnswerCode: c.employeeCount.toString(),
            AnswerText: c.employeeCount.toString()
        })
    }

    addPspQuestion(questions: QuestionModel[]) {
        let p = this.psp
        if(!p) {
            throw new Error('Missing psp')
        }
        questions.push({
            QuestionCode: 'isPaymentServiceProvider',
            QuestionText: 'Är betaltjänstleverantör?',
            AnswerCode: p.isPaymentServiceProvider ? 'true' : 'false',
            AnswerText: p.isPaymentServiceProvider ? 'Ja' : 'Nej'
        })
    }

    addSectorQuestion(questions: QuestionModel[]) {
        let s = this.sector
        if(!s) {
            throw new Error('Missing sector')
        }
        questions.push({
            QuestionCode: 'companySector',
            QuestionText: 'Verksam i bransch?',
            AnswerText: s.sectorName,
            AnswerCode: null
        })
    }

    private addCashHandlingQuestionsToRequest(questions: QuestionModel[]) {
        let c = this.cashHandling
        if(!c) {
            throw new Error('Missing cash handling')
        }

        let qc1 = 'hasCashHandling'
        let qt1 = 'Har kontanthantering?'

        if(!c.hasCashHandling) {
            questions.push({
                QuestionCode: qc1,
                QuestionText: qt1,
                AnswerText: 'Nej',
                AnswerCode: 'false'
            })
        } else {
            questions.push({
                QuestionCode: qc1,
                QuestionText: qt1,
                AnswerText: 'Ja',
                AnswerCode: 'true'
            })
            questions.push({
                QuestionCode: 'cashHandlingYearlyVolume',
                QuestionText: 'Kontanthantering - vad är den förväntade årsvolymen?',
                AnswerText: c.volume.toString(),
                AnswerCode: c.volume.toString()
            })
            questions.push({
                QuestionCode: 'cashHandlingDescription',
                QuestionText: 'Kontanthantering - vad består den i?',
                AnswerText: c.description.toString(),
                AnswerCode: null
            })
        }
    }

    private addCurrencyExchangeQuestions(questions: QuestionModel[]) {
        let c = this.currencyExchange
        if(!c) {
            throw new Error('Missing currency exchange')
        }
        let qc = 'hasCurrencyExchange'
        let qt = 'Omfattar verksamhet valutaväxling, betalningsförmedling, betalplattformar eller virtuella valutor?'
        if(c.hasCurrencyExchange) {
            questions.push({
                QuestionCode: qc,
                QuestionText: qt,
                AnswerCode: 'true',
                AnswerText: 'Ja'
            })
            questions.push({
                QuestionCode: 'currencyExchangeDescription',
                QuestionText: 'Beskriv valutaväxling, betalningsförmedling, betalplattformar eller virtuella valutor?',
                AnswerCode: null,
                AnswerText: c.description
            })
        } else {
            questions.push({
                QuestionCode: qc,
                QuestionText: qt,
                AnswerCode: 'false',
                AnswerText: 'Nej'
            })
        }
    }

    setCollateralOtherValue(a: (p : QuestionsCollateralPersonModel) => void) {
        if(!this.collateral) {
            this.collateral = {
                isApplicant: false,
                nonApplicantPerson: {}
            }
        } else if(this.collateral.isApplicant) {
            this.collateral.isApplicant = false
        }
        if(!this.collateral.nonApplicantPerson) {
            this.collateral.nonApplicantPerson = {}
        }
        a(this.collateral.nonApplicantPerson)
    }

    getCollateralOtherValue<T>(f :(p:  QuestionsCollateralPersonModel) => T) : T {
        if(!this.collateral || this.collateral.isApplicant || !this.collateral.nonApplicantPerson) {
            return null
        }
        return f(this.collateral.nonApplicantPerson)
    }
}

export class QuestionsCollateralModel {
    isApplicant: boolean
    nonApplicantPerson: QuestionsCollateralPersonModel
}

export class QuestionsCollateralPersonModel {
    civicNr?: string
    firstName?: string
    lastName?: string
    email?: string
    phone?: string
}

export class BeneficialOwnersModel {
    hasBeneficialOwners1: boolean
    beneficialOwners: BeneficialOwnersItemModel[]

    public static filterOwners(hasBeneficialOwners1: boolean, beneficialOwners: BeneficialOwnersItemModel[]) :BeneficialOwnersItemModel[] {
        let r : BeneficialOwnersItemModel[] = []
        if(!beneficialOwners) {
            return r
        }
        for(let i of beneficialOwners) {
            if(i.ownershipPercent && hasBeneficialOwners1 === true) {
                r.push(i)
            }
        }
        return r
    }
}

export class BeneficialOwnersItemModel {
    civicNr: string
    firstName: string
    lastName: string
    ownershipPercent: number
    isUsPerson: boolean
    isPep: boolean
}

export class PepPersonModel {
    civicRegnr: string;
    isPep: boolean;
}

export class CashHandlingModel {
    hasCashHandling: boolean
    volume: number
    description: string
}

export class CurrencyExchangeModel {
    hasCurrencyExchange: boolean
    description: string
}

export class ConsentModel {
    applicantConsent: boolean
    othersConsent: boolean
}

export class CompanySectorModel {
    sectorName: string
}

export class PaymentServiceProviderModel {
    isPaymentServiceProvider: boolean
}

export class EmployeeCountModel {
    employeeCount: number
}

export class ExtraPaymentsModel {
    areExtraPaymentsExpected: boolean
}

export class PaymentSourceModel {
    paymentSourceCode: string
    paymentSourceText: string
}
