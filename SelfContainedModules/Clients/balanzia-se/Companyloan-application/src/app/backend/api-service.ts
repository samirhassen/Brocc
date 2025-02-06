import { InjectionToken, Inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { StorageService } from 'ngx-webstorage-service';
import { ApplicationModel } from './application-model';
import { Router } from '@angular/router';
import { StringDictionary, NullableNumber, Dictionary } from './common.types';
import { CreateApplicationResponseModel } from './real-api.service';
import { CreateApplicationRequestModel } from './application-model.server';
import { NTechPaymentPlanService } from './ntech-paymentplan.service';
import { ConfigService } from './config.service';
import { PlatformLocation, Location, JsonPipe } from '@angular/common';
import { QuestionsModel, QuestionsCollateralModel } from './questions-model';
import { QuestionsServerRequest } from './questions-server-request';
export let API_SERVICE = new InjectionToken<ApiService>('api-service-token');

export interface ApiService {
    startApplication(loginResult: CompleteEidLoginSessionResult): Observable<ApplicationModel>
    saveApplication(a: ApplicationModel)
    deleteApplication(a: ApplicationModel)
    getApplication(id: string): Observable<ApplicationModel>
    navigateToApplicationRoute(stepName: string, applicationId: string, loanType?: string)
    navigateToRouteRaw(route: string)
    getLoanLimits(): LoanLimitsModel
    createApplication(request: CreateApplicationRequestModel): Observable<CreateApplicationResponseModel>
    createEidLoginSession(expectedCivicRegNr: string, externalVariables: StringDictionary, additionalQuestionsApplicationNr: string): Observable<CreateEidLoginSessionResult>
    completeEidLoginSession(sessionId: string, loginToken: string): Observable<CompleteEidLoginSessionResult>

    //NOTE: location and platformLocation are parameters instead of being injected since DI breaks when the app loads if we inject them in ApiService
    getRelativeExternalUrlToRoute(stepName: string, applicationId: string, loanType: string): string

    navigateToQuestionsRoute(stepName: string, sessionId: string)
    saveQuestions(a: QuestionsModel)
    deleteQuestions(a: QuestionsModel)
    getQuestions(id: string): Observable<QuestionsModel> 
    startAdditionalQuestionsSession(loginSessionDataToken: string, applicationNr: string): Observable<QuestionsModelOrErrorCode>
    validateBankAccountNr(bankAccountNr: string, bankAccountNrType: string) : Observable<BankAccountNrValidationResult>
    submitAdditionalQuestions(request: QuestionsServerRequest): Observable<SubmitAdditionalQuestionsResponse>
}

export abstract class ApiServiceBase implements ApiService {
    constructor(protected storage: StorageService, private router: Router, 
        private paymentPlanService: NTechPaymentPlanService, protected configService: ConfigService, 
        protected platformLocation: PlatformLocation, protected location: Location) {

    }

    startApplication(loginResult: CompleteEidLoginSessionResult): Observable<ApplicationModel> {
        var m = new ApplicationModel()
        m.id = this.generateUniqueUrlSafeId(16)
        m.civicRegNr = loginResult.CivicRegNr
        m.firstName = loginResult.FirstName
        m.lastName = loginResult.LastName
        m.loginSessionDataToken = loginResult.LoginSessionDataToken

        let externalVariables: StringDictionary = null
        if(loginResult.CustomData) {
            let d =  this.parseLoginSessionCustomDataModel(loginResult.CustomData)
            externalVariables = d.externalVariables
            if(d.purpose !== 'application') {
                throw 'Invalid token'
            }
        }

        m.externalVariables = externalVariables ? externalVariables : {}

        this.saveApplication(m)

        return of(m)
    }

    saveApplication(a: ApplicationModel) {
        this.storage.set(a.id, JSON.stringify(a))
    }

    deleteApplication(a: ApplicationModel) {
        this.storage.set(a.id, null)
    }

    getApplication(id: string): Observable<ApplicationModel> {
        let m = this.storage.get(id)
        if (m) {
            let a: ApplicationModel = new ApplicationModel()
            Object.assign(a, JSON.parse(m))
            return of(a)
        } else {
            return of(null)
        }
    }

    private getApplicationRoute(stepName: string, applicationId: string, loanType?: string): string[] {
        if (loanType) {
            return [`app/${applicationId}/${stepName}/${loanType}`]
        } else if(applicationId){
            return [`app/${applicationId}/${stepName}`]
        } else {
            return [`${stepName}`]
        }
    }

    navigateToQuestionsRoute(stepName: string, sessionId: string) {        
        this.router.navigate(this.getQuestionsRoute(stepName, sessionId))
    }

    private getQuestionsRoute(stepName: string, sessionId: string): string[] {
        if(sessionId){
            return [`q/${sessionId}/${stepName}`]
        } else {
            return [`q/${stepName}`]
        }
    }

    navigateToApplicationRoute(stepName: string, applicationId: string, loanType?: string) {        
        this.router.navigate(this.getApplicationRoute(stepName, applicationId, loanType))
    }    

    getRelativeExternalUrlToRoute(stepName: string, applicationId: string, loanType: string) {
        let localUrl = this.location.prepareExternalUrl(this.router.createUrlTree(this.getApplicationRoute(stepName, applicationId, loanType)).toString()) //something like '#/foo/bar'

        let baseHref = this.platformLocation.getBaseHrefFromDOM() //Something like '/' or '/a/'
        let url = baseHref.substr(1) + localUrl
        return url
    }

    navigateToRouteRaw(route: string) {
        this.router.navigate([route])
    }

    getLoanLimits(): LoanLimitsModel {
        var r = new LoanLimitsModel()
        r.MinimumLoanAmount = 50000
        r.MaximumLoanAmount = 300000
        r.MaximumRepaymentTimeInYears = 5
        r.MinimumRepaymentTimeInYears = 1
        return r
    }

    saveQuestions(a: QuestionsModel) {
        this.storage.set(a.id, JSON.stringify(a))
    }

    deleteQuestions(a: QuestionsModel) {
        this.storage.set(a.id, null)
    }

    getQuestions(id: string): Observable<QuestionsModel> {
        let m = this.storage.get(id)
        if (m) {
            let a: QuestionsModel = new QuestionsModel()
            Object.assign(a, JSON.parse(m))
            return of(a)
        } else {
            return of(null)
        }
    }

    abstract validateBankAccountNr(bankAccountNr: string, bankAccountNrType: string) : Observable<BankAccountNrValidationResult>
    abstract createApplication(request: CreateApplicationRequestModel): Observable<CreateApplicationResponseModel>
    abstract createEidLoginSession(expectedCivicRegNr: string, externalVariables: StringDictionary, additionalQuestionsApplicationNr: string): Observable<CreateEidLoginSessionResult>    
    abstract completeEidLoginSession(sessionId: string, loginToken: string): Observable<CompleteEidLoginSessionResult> 
    abstract startAdditionalQuestionsSession(loginSessionDataToken: string, applicationNr: string): Observable<QuestionsModelOrErrorCode>
    abstract submitAdditionalQuestions(request: QuestionsServerRequest): Observable<SubmitAdditionalQuestionsResponse>

    generateUniqueUrlSafeId(length: number): string {
        let id = 'T'
        for (var i = 0; i < length; i++) {
            id += Math.floor(Math.random() * 16).toString(16).toUpperCase()
        }
        return id
    }

    parseLoginSessionCustomDataModel(d: string) : LoginSessionCustomDataModel {
        return JSON.parse(d)
    }    
}

export interface LoginSessionCustomDataModel {
    externalVariables: Dictionary<string>
    purpose: string
}

export class CompleteEidLoginSessionResult {
    CivicRegNr: string
    FirstName: string
    LastName: string
    LoginSessionDataToken: string
    CustomData: string
}

export class CreateEidLoginSessionResult {
    SignicatInitialUrl: string    
}

export class CompanyLoanExtendedOfferModel {
    LoanAmount: number    
    NominalInterestRatePercent: number
    ReferenceInterestRatePercent: number
    TotalInterestRatePercent: number
    MonthlyFeeAmount: number
    InitialFeeAmount: number
    AnnuityAmount: number
    RepaymentTimeInMonths: number
    TotalPaidAmount: number
    EffectiveInterestRatePercent: number
}

export class LoanLimitsModel {
    MinimumLoanAmount: number
    MaximumLoanAmount: number
    MinimumRepaymentTimeInYears: number
    MaximumRepaymentTimeInYears: number
}

export class BankAccountNrValidationResult {
    IsValid: boolean
    AccountNrType?: string
    NormalizedNr?: string
    DisplayFormattedNr?: string
    BankName?: string
    AccountNrPart?: string
    ClearingNrPart?: string
}

export class SubmitAdditionalQuestionsRequest {
    QuestionsSubmissionToken: string
    BankAccountNr: string
    BankAccountNrType: string
}

export class SubmitAdditionalQuestionsResponse {
    
}

export class QuestionsModelOrErrorCode {
    IsError: boolean
    Model?: QuestionsModel
    ErrorCode?: string
}