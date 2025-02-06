import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ConsentComponent } from './application-steps/consent/consent.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { environment } from '../environments/environment';
import { RequireApplicationGuard } from './guards/require-application.guard';
import { RequireApplicationNrGuard } from './guards/require-applicationnr';
import { ResultSuccessComponent } from './application-steps/result-success/result-success.component';
import { ResultFailedComponent } from './result-failed/result-failed.component';
import { LoginCompleteComponent } from './login-complete/login-complete.component';
import { LoginFailedComponent } from './login-failed/login-failed.component';
import { EidLoginComponent } from './application-steps/eid-login/eid-login.component';
import { LocalTestEidComponent } from './local-test-eid/local-test-eid.component';
import { OrgnrComponent } from './application-steps/orgnr/orgnr.component';
import { LoanAmountComponent } from './application-steps/loan-amount/loan-amount.component';
import { RepaymentTimeComponent } from './application-steps/repayment-time/repayment-time';
import { PurposeComponent } from './application-steps/purpose/purpose.component';
import { ApplicantEmailComponent } from './application-steps/applicant-email/applicant-email.component';
import { ApplicantPhoneComponent } from './application-steps/applicant-phone/applicant-phone.component';
import { CompanyAgeComponent } from './application-steps/company-age/company-age.component';
import { CompanyResultComponent } from './application-steps/company-result/company-result.component';
import { CompanyRevenueComponent } from './application-steps/company-revenue/company-revenue.component';
import { HasOtherLoansComponent } from './application-steps/has-other-loans/has-other-loans.component';
import { OtherLoansAmountComponent } from './application-steps/other-loans-amount/other-loans-amount.component';
import { QEidLoginComponent } from './additional-questions/q-eid-login/q-eid-login.component';
import { QOfferComponent } from './additional-questions/q-offer/q-offer.component';
import { RequireQuestionsGuard } from './guards/require-questions.guard';
import { QBankAccountComponent } from './additional-questions/q-bankaccount/q-bankaccount.component';
import { QResultSuccessComponent } from './additional-questions/q-result-success/q-result-success.component';
import { QCollateralOptionComponent } from './additional-questions/q-collateral-option/q-collateral-option.component';
import { QCollateralCivicNrComponent } from './additional-questions/q-collateral-civicnr/q-collateral-civicnr.component';
import { QCollateralNameComponent } from './additional-questions/q-collateral-name/q-collateral-name.component';
import { QCollateralEmailComponent } from './additional-questions/q-collateral-email/q-collateral-email.component';
import { QCollateralPhoneComponent } from './additional-questions/q-collateral-phone/q-collateral-phone.component';
import { QBeneficialOwners1Component } from './additional-questions/q-beneficial-owners1/q-beneficial-owners1.component';
import { QPepComponent } from './additional-questions/q-pep/q-pep.component';
import { QCashHandlingOptionComponent } from './additional-questions/q-cashhandling-option/q-cashhandling-option.component';
import { QCashHandlingComponent } from './additional-questions/q-cashhandling/q-cashhandling.component';
import { QCurrencyExchangeOptionComponent } from './additional-questions/q-currency-exchange-option/q-currency-exchange-option.component';
import { QCurrencyExchangeComponent } from './additional-questions/q-currency-exchange/q-currency-exchange.component';
import { QConsentComponent } from './additional-questions/q-consent/q-consent.component';
import { SignatureSuccessComponent } from './signature-result/signature-success/signature-success.component';
import { SignatureFailedComponent } from './signature-result/signature-failed/signature-failed.component';
import { QBeneficialOwnersOptionComponent } from './additional-questions/q-beneficial-owners-option/q-beneficial-owners-option.component';
import { QUsPersonComponent } from './additional-questions/q-usperson/q-usperson.component';
import { QSectorNameComponent } from './additional-questions/q-company-sector.component';
import { QPspOptionComponent } from './additional-questions/q-psp-option.component';
import { QEmployeeCountComponent } from './additional-questions/q-employee-count/q-employee-count.component';
import { QExtraPaymentsComponent } from './additional-questions/q-extra-payments-option.component';
import { QPaymentSourceComponent } from './additional-questions/q-payment-source.component';

let sharedRoutes: Routes = [
    { path: 'eid-login', component: EidLoginComponent, data: { isIdependent: true } }, 
    { path: '', pathMatch: 'full', redirectTo: '/eid-login' },
    { path: 'not-found', component: NotFoundComponent },
    { path: 'login-complete', component: LoginCompleteComponent },
    { path: 'login-failed', component: LoginFailedComponent },
    { path: 'signature/success', component: SignatureSuccessComponent, data: { isIdependent: true } },
    { path: 'signature/failed', component: SignatureFailedComponent, data: { isIdependent: true } }
]

let applicationRoutes: Routes = [
    { path: 'app/:id/orgnr', component: OrgnrComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/loan-amount', component: LoanAmountComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/repayment-time', component: RepaymentTimeComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/purpose', component: PurposeComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/applicant-email', component: ApplicantEmailComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/applicant-phone', component: ApplicantPhoneComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/company-age', component: CompanyAgeComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/company-result', component: CompanyResultComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/company-revenue', component: CompanyRevenueComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/has-other-loans', component: HasOtherLoansComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/other-loans-amount', component: OtherLoansAmountComponent, resolve : { application: RequireApplicationGuard } },
    { path: 'app/:id/result-success', component: ResultSuccessComponent, resolve : { application: RequireApplicationGuard }, data: { isIdependent: true } },
    { path: 'app/result-failed', component: ResultFailedComponent, data: { isIdependent: true } },
    { path: 'app/:id/consent', component: ConsentComponent, resolve : { application: RequireApplicationGuard }, data: { isFinalStep: true } },
]

let additionalQuestionRoutes: Routes = [
    { path: 'q-eid-login/:applicationNr/start', component: QEidLoginComponent, resolve: { applicationNr: RequireApplicationNrGuard }, data: { isIdependent: true } }, 
    { path: 'q/:id/result-success', component: QResultSuccessComponent, resolve : { questions: RequireQuestionsGuard }, data: { isIdependent: true } },    
    { path: 'q/result-failed', component: ResultFailedComponent, data: { isIdependent: true } },    
    { path: 'q/result-not-found', component: ResultFailedComponent, data: { isIdependent: true, failureCode: 'questionsNotFound' } },
    { path: 'q/result-not-pending', component: ResultFailedComponent, data: { isIdependent: true, failureCode: 'questionsNotPendingAnswers' } },
    { path: 'q/:id/offer', component: QOfferComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/bankaccount', component: QBankAccountComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/collateral-option', component: QCollateralOptionComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/collateral-civicnr', component: QCollateralCivicNrComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/collateral-name', component: QCollateralNameComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/collateral-email', component: QCollateralEmailComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/collateral-phone', component: QCollateralPhoneComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/beneficial-owners-option', component: QBeneficialOwnersOptionComponent, resolve : { questions: RequireQuestionsGuard } },    
    { path: 'q/:id/beneficial-owners1', component: QBeneficialOwners1Component, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/pep', component: QPepComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/cashhandling-option', component: QCashHandlingOptionComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/cashhandling', component: QCashHandlingComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/currency-exchange-option', component: QCurrencyExchangeOptionComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/currency-exchange', component: QCurrencyExchangeComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/consent', component: QConsentComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/usperson', component: QUsPersonComponent, resolve : { questions: RequireQuestionsGuard } },  
    { path: 'q/:id/company-sector', component: QSectorNameComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/psp-option', component: QPspOptionComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/employee-count', component: QEmployeeCountComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/extra-payments-option', component: QExtraPaymentsComponent, resolve : { questions: RequireQuestionsGuard } },
    { path: 'q/:id/payment-source', component: QPaymentSourceComponent, resolve : { questions: RequireQuestionsGuard } }    
]

let routes: Routes = [
    ...sharedRoutes,
    ...applicationRoutes,
    ...additionalQuestionRoutes
]

if(environment.useMockApi) {
    routes.push({ path: 'local-test-eid', component: LocalTestEidComponent, data: { isIdependent: true } })
}

routes.push({ path: '**', redirectTo: '/not-found', data: { isIdependent: true } })

@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true, enableTracing: false })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
