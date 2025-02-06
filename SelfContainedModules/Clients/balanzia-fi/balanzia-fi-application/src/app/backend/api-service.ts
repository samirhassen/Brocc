import { InjectionToken, Inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { StorageService } from 'ngx-webstorage-service';
import { ApplicationModel } from './application-model';
import { Router } from '@angular/router';
import { StringDictionary, NullableNumber } from './common.types';
import { CreateApplicationResponseModel } from './real-api.service';
import { CreateApplicationRequestModel } from './application-model.server';
import { NTechPaymentPlanService } from './ntech-paymentplan.service';
import { ConfigService } from './config.service';
export let API_SERVICE = new InjectionToken<ApiService>('api-service-token');

export interface ApiService {
    startApplication(externalVariables: StringDictionary): Observable<ApplicationModel>
    saveApplication(a: ApplicationModel)
    deleteApplication(a: ApplicationModel)
    getApplication(id: string): Observable<ApplicationModel>
    navigateToRoute(stepName: string, applicationId: string, applicantNr?: number, loanType?: string)
    navigateToRouteRaw(route: string)
    calculateLoanPreview(request: LoanPreviewRequestModel): Observable<LoanPreviewResponseModel>
    getLoanLimits(): LoanLimitsModel
    createApplication(request: CreateApplicationRequestModel): Observable<CreateApplicationResponseModel>
}

export abstract class ApiServiceBase implements ApiService {
    constructor(private storage: StorageService, private router: Router, private paymentPlanService: NTechPaymentPlanService, protected configService: ConfigService) {

    }

    startApplication(externalVariables: StringDictionary): Observable<ApplicationModel> {
        var m = new ApplicationModel()

        m.id = this.generateUniqueUrlSafeId(16)
        m.externalVariables = externalVariables ? externalVariables : {}
        if (m.externalVariables["coid"]) {
            m.skipCampaignStep = true
            m.hasCampaignCode = true
            m.setDataCampaignCodeCode("A00010")
        }
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

    private getRoute(stepName: string, applicationId: string, applicantNr?: number, loanType?: string): string[] {
        if (loanType) {
            if (!applicantNr) {
                throw new Error('Missing applicantNr')
            }
            return [`app/${applicationId}/${applicantNr}/${stepName}/${loanType}`]
        } else if (applicantNr) {
            return [`app/${applicationId}/${applicantNr}/${stepName}`]
        } else {
            return [`app/${applicationId}/${stepName}`]
        }
    }

    navigateToRoute(stepName: string, applicationId: string, applicantNr?: number, loanType?: string) {
        this.router.navigate(this.getRoute(stepName, applicationId, applicantNr, loanType))
    }

    navigateToRouteRaw(route: string) {
        this.router.navigate([route])
    }

    getLoanLimits(): LoanLimitsModel {
        var r = new LoanLimitsModel()
        r.MinimumLoanAmount = this.configService.isLegalInterestCeilingEnabled() ? 3000 : 2000
        r.MaximumLoanAmount = 50000
        r.MaximumRepaymentTimeInYears = 15
        r.MinimumRepaymentTimeInYears = 1
        return r
    }

    calculateLoanPreview(request: LoanPreviewRequestModel): Observable<LoanPreviewResponseModel> {
        let referenceInterestRatePercent = 0
        let requestedLoanAmount = request.RequestedLoanAmount ? request.RequestedLoanAmount.value : 13000
        let requestedRepaymentTimeInYears = request.RequestedRepaymentTimeInYears ? request.RequestedRepaymentTimeInYears.value : 7
        let notificationFee = this.configService.isLegalInterestCeilingEnabled ? 8 : 5
        let getInterestRatePercent: (amount: number) => number = amt => {
            if (amt + Number.EPSILON > 25000)
                return 7.9;
            else if (amt + Number.EPSILON > 15000)
                return 9.9;
            else if (amt + Number.EPSILON > 7000)
                return 10.9;
            else
                return 11.9;
        }

        let getInitialFee: (amount: number) => number = amt => {
            if (this.configService.isLegalInterestCeilingEnabled()) {
                return 0;
            }
            if (amt < 5000 + Number.EPSILON)
                return 49;
            else if (amt < 10000 + Number.EPSILON)
                return 89;
            else if (amt < 30000 + Number.EPSILON)
                return 129;
            else
                return 169;
        }

        let interestRatePercent = getInterestRatePercent(requestedLoanAmount) + referenceInterestRatePercent
        let capitalizedInitialFee = getInitialFee(requestedLoanAmount)

        let pp = this.paymentPlanService.calculatePaymentPlanWithAnnuitiesFromRepaymentTime(requestedLoanAmount, requestedRepaymentTimeInYears * 12, interestRatePercent, capitalizedInitialFee, notificationFee)

        return of({
            RequestedLoanAmount: requestedLoanAmount,
            RequestedRepaymentTimeInYears: requestedRepaymentTimeInYears,
            Offer: {
                LoanAmount: pp.LoanAmount,
                RepaymentTimeInMonths: pp.RepaymentTimeInMonths,
                RepaymentTimeInYears: pp.RepaymentTimeInYears,
                MonthlyCostIncludingFeesAmount: pp.MonthlyCostIncludingFeesAmount,
                MonthlyCostExcludingFeesAmount: pp.MonthlyCostExcludingFeesAmount,
                MonthlyFeeAmount: pp.MonthlyFeeAmount,
                InitialFeeAmount: pp.InitialFeeAmount,
                NominalInterestRatePercent: pp.NominalInterestRatePercent,
                EffectiveInterstRatePercent: pp.EffectiveInterstRatePercent ? pp.EffectiveInterstRatePercent.value : null,
                TotalPaidAmount: pp.TotalPaidAmount
            }
        })
    }

    abstract createApplication(request: CreateApplicationRequestModel): Observable<CreateApplicationResponseModel>

    private generateUniqueUrlSafeId(length: number): string {
        let id = 'T'
        for (var i = 0; i < length; i++) {
            id += Math.floor(Math.random() * 16).toString(16).toUpperCase()
        }
        return id
    }
}

export class LoanPreviewRequestModel {
    RequestedLoanAmount?: NullableNumber
    RequestedRepaymentTimeInYears?: NullableNumber
}

export class LoanPreviewResponseModel {
    RequestedLoanAmount: number
    RequestedRepaymentTimeInYears: number
    Offer?: OfferModel
}

export class OfferModel {
    LoanAmount: number
    RepaymentTimeInMonths: number
    RepaymentTimeInYears: number
    MonthlyCostIncludingFeesAmount: number
    MonthlyCostExcludingFeesAmount: number
    MonthlyFeeAmount: number
    InitialFeeAmount: number
    NominalInterestRatePercent: number
    EffectiveInterstRatePercent: number
    TotalPaidAmount: number
}

export class LoanLimitsModel {
    MinimumLoanAmount: number
    MaximumLoanAmount: number
    MinimumRepaymentTimeInYears: number
    MaximumRepaymentTimeInYears: number
}