import { Injectable, Inject } from '@angular/core';
import { ApiService, ApiServiceBase, CreateEidLoginSessionResult, CompleteEidLoginSessionResult, CompanyLoanExtendedOfferModel, BankAccountNrValidationResult, SubmitAdditionalQuestionsResponse, QuestionsModelOrErrorCode } from './api-service';
import { SESSION_STORAGE, StorageService } from 'ngx-webstorage-service';
import { Observable, of, OperatorFunction, throwError } from 'rxjs';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { map, catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { CreateApplicationRequestModel } from './application-model.server';
import { NTechPaymentPlanService } from './ntech-paymentplan.service';
import { ConfigService } from './config.service';
import { PlatformLocation, Location } from '@angular/common';
import { StringDictionary } from './common.types';
import { QuestionsModel } from './questions-model';
import { QuestionsServerRequest } from './questions-server-request';

export class RealApiService extends ApiServiceBase {
    constructor(@Inject(SESSION_STORAGE) storage: StorageService, router: Router, private httpClient: HttpClient, paymentPlanService: NTechPaymentPlanService, configService: ConfigService,
    platformLocation: PlatformLocation,location: Location) {
        super(storage, router, paymentPlanService, configService, platformLocation, location);
    }

    createApplication(request: CreateApplicationRequestModel): Observable<CreateApplicationResponseModel> {
        return this.httpClient.post<CreateApplicationResponseModel>('/api/company-loans/create-application', request)
    }

    createEidLoginSession(expectedCivicRegNr: string, externalVariables: StringDictionary, additionalQuestionsApplicationNr: string): Observable<CreateEidLoginSessionResult> {
        return this.httpClient.post<CreateEidLoginSessionResult>('/api/signicat/create-local-login-session', {
            expectedCivicRegNr: expectedCivicRegNr,
            successRedirectUrl: this.getRelativeExternalUrlToRoute('login-complete', null, null),
            failedRedirectUrl: this.getRelativeExternalUrlToRoute('login-failed', null, null),
            customData: JSON.stringify({ externalVariables : externalVariables, purpose: additionalQuestionsApplicationNr ? 'questions' : 'application', applicationNr: additionalQuestionsApplicationNr })
        })
    }
    
    completeEidLoginSession(sessionId: string, loginToken: string): Observable<CompleteEidLoginSessionResult> {
        return this.httpClient.post<CompleteEidLoginSessionResult>('/api/signicat/complete-local-login-session', {
             sessionId: sessionId,
             loginToken: loginToken
        })
    }
    
    startAdditionalQuestionsSession(loginSessionDataToken: string, applicationNr: string): Observable<QuestionsModelOrErrorCode> {
        return this.httpClient.post<StartAdditionalQuestionsResponse>('/api/company-loans/start-additional-questions-session', { loginSessionDataToken, applicationNr, returnCompanyInformation: true }).pipe(
            catchError((err : HttpErrorResponse) => {
                if(err.error && err.error.errorCode === 'notFound') {
                    return of({ IsNotFoundError: true } as StartAdditionalQuestionsResponse)
                } else {
                    throwError(err)
                }
            }),
            map(x => {
                if(x.IsNotFoundError) {
                    return { IsError: true, ErrorCode: 'notFound'}
                } else if(!x.IsPendingAnswers) {
                    return { IsError: true, ErrorCode: 'notPendingAnswers' }
                }

                let m = new QuestionsModel()
                m.id = this.generateUniqueUrlSafeId(16)
                m.applicationNr = applicationNr
                m.offer = x.Offer
                m.submissionToken = x.QuestionsSubmissionToken
                m.companyInformation = x.CompanyInformation

                this.saveQuestions(m)
                return { IsError: false, Model: m }
            }))
    }

    submitAdditionalQuestions(request: QuestionsServerRequest): Observable<SubmitAdditionalQuestionsResponse> {
        return this.httpClient.post<SubmitAdditionalQuestionsResponse>('/api/company-loans/complete-additional-questions-session', request)
    }

    validateBankAccountNr(bankAccountNr: string, bankAccountNrType: string) : Observable<BankAccountNrValidationResult> {
        return this.httpClient.post<BankAccountNrValidationResult>('/api/company-loans/validate-bankaccountnr', { bankAccountNr, bankAccountNrType })
    }
}

class StartAdditionalQuestionsResponse {
    IsNotFoundError?: boolean
    IsPendingAnswers?: boolean
    CompanyInformation?: CompanyInformationModel
    Offer?: CompanyLoanExtendedOfferModel
    QuestionsSubmissionToken?: string
}

export class CompanyInformationModel {
    Orgnr: string
    Name: string
}

export class CreateApplicationResponseModel {
    ApplicationNr: string
    DecisionStatus: string
    Offer: CompanyLoanExtendedOfferModel
    LoginSessionDataToken: string
    //note, dont put RejectionCodes here as we likely dont want to leak that directly. Maybe some filtered version that is safe to expose.
}

export class CreateApplicationResponseOfferModel {
    LoanAmount: number
    AnnuityAmount: number
    NominalInterestRatePercent: number
    MonthlyFeeAmount: number
    InitialFeeAmount: number
}