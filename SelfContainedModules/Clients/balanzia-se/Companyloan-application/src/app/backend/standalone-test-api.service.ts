import { Inject } from '@angular/core';
import { ApiServiceBase, CreateEidLoginSessionResult, CompleteEidLoginSessionResult, LoginSessionCustomDataModel, BankAccountNrValidationResult, SubmitAdditionalQuestionsResponse, SubmitAdditionalQuestionsRequest, QuestionsModelOrErrorCode } from './api-service';
import { SESSION_STORAGE, StorageService } from 'ngx-webstorage-service';
import { of, Observable } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Router } from '@angular/router';
import { CreateApplicationResponseModel } from './real-api.service';
import * as moment from 'moment'
import { CreateApplicationRequestModel } from './application-model.server';
import { NTechPaymentPlanService } from './ntech-paymentplan.service';
import { ConfigService } from './config.service';
import { PlatformLocation, Location } from '@angular/common';
import { StringDictionary } from './common.types';
import { QuestionsModel } from './questions-model';
import { QuestionsServerRequest } from './questions-server-request';

export class StandaloneTestApiService extends ApiServiceBase {

    constructor(@Inject(SESSION_STORAGE) storage: StorageService, router: Router, 
        paymentPlanService: NTechPaymentPlanService, configService: ConfigService, 
        platformLocation: PlatformLocation, location: Location) {
        super(storage, router, paymentPlanService, configService, platformLocation, location);
    }

    createApplication(request: CreateApplicationRequestModel): Observable<CreateApplicationResponseModel> {        
        let now = moment().format('YYYYMMDD-HHmmSS')
        let r: CreateApplicationResponseModel = {
            ApplicationNr:`TEST-${now}`,
            DecisionStatus: (request.RequestedAmount < 10000 ? 'Rejected' : request.RequestedAmount > 100000 ? 'Accepted' : 'Pending'),
            Offer: null,
            LoginSessionDataToken: null
        }        
        
        if(r.DecisionStatus === 'Accepted') {
            r.Offer = {
                LoanAmount: request.RequestedAmount,
                AnnuityAmount: 1500,
                RepaymentTimeInMonths: 24,
                EffectiveInterestRatePercent: 16,
                NominalInterestRatePercent: 13,
                ReferenceInterestRatePercent: 1,
                TotalInterestRatePercent: 14,
                MonthlyFeeAmount: 50,
                InitialFeeAmount: 1500,
                TotalPaidAmount: 455600, 
            }
            r.LoginSessionDataToken = this.generateUniqueUrlSafeId(16)
            let d = this.storage.get('test-login')
            if(d) {
                d = JSON.parse(d)
            } else {
                d = {}
            }
            let cd : LoginSessionCustomDataModel = {
                externalVariables: d.externalVariables, purpose: d.purpose
            }
            let sr : CompleteEidLoginSessionResult = {
                CivicRegNr: d.expectedCivicRegNr,
                FirstName: 'Test',
                LastName: 'Testsson',
                LoginSessionDataToken: 'test-token',
                CustomData: JSON.stringify(cd)                
            }
            this.storage.set('tmp-questions', JSON.stringify(sr))
        }
        return of(r).pipe(delay(2000))
    }

    createEidLoginSession(expectedCivicRegNr: string, externalVariables: StringDictionary, additionalQuestionsApplicationNr: string): Observable<CreateEidLoginSessionResult> {
        this.storage.set('test-login', JSON.stringify({ expectedCivicRegNr: expectedCivicRegNr, externalVariables: externalVariables, purpose: additionalQuestionsApplicationNr ? 'questions' : 'application' }))
        return of({ SignicatInitialUrl: '' }).pipe(delay(500))
    }

    completeEidLoginSession(sessionId: string, loginToken: string): Observable<CompleteEidLoginSessionResult> {
        let d = this.storage.get('test-login')
        if(d) {
            d = JSON.parse(d)
        } else {
            d = {}
        }

        let cd : LoginSessionCustomDataModel = {
            externalVariables: d.externalVariables, purpose: d.purpose
        }
        let r : CompleteEidLoginSessionResult = {
            CivicRegNr: d.expectedCivicRegNr,
            FirstName: 'Test',
            LastName: 'Testsson',
            LoginSessionDataToken: 'test-token',
            CustomData: JSON.stringify(cd)
        }

        if(d.purpose === 'questions') {
            this.storage.set('tmp-questions', JSON.stringify(r))
        }

        return of(r).pipe(delay(500))
    }

    startAdditionalQuestionsSession(loginSessionDataToken: string, applicationNr: string): Observable<QuestionsModelOrErrorCode> {
        let r : CompleteEidLoginSessionResult = JSON.parse(this.storage.get('tmp-questions'))
        let cd = this.parseLoginSessionCustomDataModel(r.CustomData)
        let m = new QuestionsModel()
        m.id = this.generateUniqueUrlSafeId(16)
        m.applicationNr = applicationNr
        m.submissionToken = 'new-token'
        m.offer = {
            LoanAmount: 350000,
            AnnuityAmount: 1500,
            RepaymentTimeInMonths: 24,
            EffectiveInterestRatePercent: 16,
            NominalInterestRatePercent: 13,
            ReferenceInterestRatePercent: 1,
            TotalInterestRatePercent: 14,
            MonthlyFeeAmount: 50,
            InitialFeeAmount: 1500,
            TotalPaidAmount: 455600,            
        }
        m.companyInformation = {
            Name: 'Test AB',
            Orgnr: '559040-6483'
        }
        this.saveQuestions(m)
        return of({ IsError: false, Model: m }).pipe(delay(500))
    }

    validateBankAccountNr(bankAccountNr: string, bankAccountNrType: string) : Observable<BankAccountNrValidationResult> {
        let result : BankAccountNrValidationResult
        if(bankAccountNr && bankAccountNr.length > 1) {
            result = {
                IsValid: true,
                NormalizedNr: bankAccountNr,
                DisplayFormattedNr: bankAccountNr + ' (formatted)',
                AccountNrType: bankAccountNrType,
                AccountNrPart: null,
                ClearingNrPart: null                
            }
        } else {
            result = {
                IsValid: false                
            }
        }
        return of(result).pipe(delay(200))
    }

    submitAdditionalQuestions(request: QuestionsServerRequest): Observable<SubmitAdditionalQuestionsResponse> {
        console.log(request)
        return of({}).pipe(delay(1000))
    }
}
