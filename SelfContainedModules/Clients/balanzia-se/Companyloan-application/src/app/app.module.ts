import { BrowserModule } from '@angular/platform-browser';
import { NgModule, LOCALE_ID, APP_INITIALIZER } from '@angular/core';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { ConsentComponent } from './application-steps/consent/consent.component';
import { environment } from '../environments/environment';
import { API_SERVICE } from './backend/api-service';
import { StandaloneTestApiService } from './backend/standalone-test-api.service';
import { RealApiService } from './backend/real-api.service';
import { StorageServiceModule } from 'ngx-webstorage-service';
import { RequireApplicationGuard } from './guards/require-application.guard';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { ForwardBackButtonsComponent } from './forward-back-buttons/forward-back-buttons.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

//Builtin, used to format date and currency pipes and similar
import { registerLocaleData } from '@angular/common';
import localeSv from '@angular/common/locales/sv';

import { ResultSuccessComponent } from './application-steps/result-success/result-success.component';
import { ResultFailedComponent } from './result-failed/result-failed.component';
import { ApplicationTemplateComponent } from './application-template/application-template.component';
import { ConfigService } from './backend/config.service';
import { Observable, of } from 'rxjs';
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

registerLocaleData(localeSv, 'sv')

export function initializeConfig(configService: ConfigService) {
    return () => 
        configService.initialize()
}

@NgModule({
  declarations: [
    AppComponent,
    NotFoundComponent,
    ConsentComponent,
    ForwardBackButtonsComponent,
    ResultSuccessComponent,
    ResultFailedComponent,
    ApplicationTemplateComponent,
    LoginCompleteComponent,
    LoginFailedComponent,
    EidLoginComponent,
    LocalTestEidComponent,
    OrgnrComponent,
    LoanAmountComponent,
    RepaymentTimeComponent,
    PurposeComponent,
    ApplicantEmailComponent,
    ApplicantPhoneComponent,
    CompanyAgeComponent,
    CompanyResultComponent,
    CompanyRevenueComponent,
    HasOtherLoansComponent,
    OtherLoansAmountComponent,
    QEidLoginComponent,
    QOfferComponent,
    QBankAccountComponent,
    QResultSuccessComponent,
    QCollateralOptionComponent,
    QCollateralCivicNrComponent,
    QCollateralNameComponent,
    QCollateralEmailComponent,
    QCollateralPhoneComponent,
    QBeneficialOwners1Component,
    QPepComponent,
    QCashHandlingOptionComponent,
    QCashHandlingComponent,
    QCurrencyExchangeOptionComponent,
    QCurrencyExchangeComponent,
    QConsentComponent,
    QBeneficialOwnersOptionComponent,
    SignatureSuccessComponent,
    SignatureFailedComponent,
    QUsPersonComponent,
    QSectorNameComponent,
    QPspOptionComponent,
    QEmployeeCountComponent,
    QExtraPaymentsComponent,
    QPaymentSourceComponent
  ],
  imports: [
    BrowserModule,    
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    StorageServiceModule,
    HttpClientModule
  ],
  providers: [
    { provide: API_SERVICE, useClass: environment.useMockApi ? StandaloneTestApiService : RealApiService }, 
    ConfigService,
    {
      provide: APP_INITIALIZER,
      useFactory: initializeConfig,
      deps: [ConfigService], // dependancy
      multi: true
    },
    RequireApplicationGuard,
    { provide: LOCALE_ID, useValue: 'sv' }],
  bootstrap: [AppComponent]
})
export class AppModule { }
