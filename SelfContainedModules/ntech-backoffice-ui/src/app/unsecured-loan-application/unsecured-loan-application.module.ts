import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { UnsecuredLoanApplicationRoutingModule } from './unsecured-loan-application-routing.module';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { ApplicationComponent } from './pages/application/application.component';
import { UnsecuredLoanApplicationApiService } from './services/unsecured-loan-application-api.service';
import { KycStandardComponent } from './steps/kyc-standard/kyc-standard.component';
import { CreditCheckStandardComponent } from './steps/credit-check-standard/credit-check-standard.component';
import { CustomerOfferDecisionStandardComponent } from './steps/customer-offer-decision-standard/customer-offer-decision-standard.component';
import { FraudStandardComponent } from './steps/fraud-standard/fraud-standard.component';
import { AgreementStandardComponent } from './steps/agreement-standard/agreement-standard.component';
import { PaymentStandardComponent } from './steps/payment-standard/payment-standard.component';
import { ApplicationsComponent } from './pages/applications/applications.component';
import { CreditCheckNewComponent } from './pages/credit-check-new/credit-check-new.component';
import { ApplicationDataEditorComponent } from './components/application-data-editor/application-data-editor.component';
import { CreditCheckDecisionEditorComponent } from './components/credit-check-decision-editor/credit-check-decision-editor.component';
import { CreditCheckDecisionViewComponent } from './components/credit-check-decision-view/credit-check-decision-view.component';
import { PolicyfilterComponentsModule } from '../policyfilter-components/policyfilter-components.module';
import { ApplicationPolicyInfoComponent } from './components/application-policy-info/application-policy-info.component';
import { ApplicationManualRegistrationComponent } from './pages/application-manual-registration/application-manual-registration.component';
import { DirectDebitManagementComponent } from './pages/direct-debit-management/direct-debit-management.component';
import { SharedApplicationComponentsModule } from '../shared-application-components/shared-application-components.module';
import { ApplicationBasisComponent } from './components/application-basis/application-basis.component';
import { ApplicationBasisPageComponent } from './pages/application-basis-page/application-basis-page.component';
import { BankshareTestComponent } from './pages/bankshare-test/bankshare-test.component';
import { NtechComponentsModule } from 'projects/ntech-components/src/public-api';

@NgModule({
    providers: [UnsecuredLoanApplicationApiService],
    declarations: [
        ApplicationComponent,
        KycStandardComponent,
        CreditCheckStandardComponent,
        CustomerOfferDecisionStandardComponent,
        AgreementStandardComponent,
        FraudStandardComponent,
        PaymentStandardComponent,
        ApplicationsComponent,
        CreditCheckNewComponent,
        ApplicationDataEditorComponent,
        CreditCheckDecisionEditorComponent,
        CreditCheckDecisionViewComponent,
        ApplicationPolicyInfoComponent,
        ApplicationManualRegistrationComponent,
        DirectDebitManagementComponent,
        ApplicationBasisComponent,
        ApplicationBasisPageComponent,
        BankshareTestComponent,
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        UnsecuredLoanApplicationRoutingModule,
        CommonComponentsModule,
        SharedApplicationComponentsModule,
        PolicyfilterComponentsModule,
        NtechComponentsModule
    ],
})
export class UnsecuredLoanApplicationModule {}
