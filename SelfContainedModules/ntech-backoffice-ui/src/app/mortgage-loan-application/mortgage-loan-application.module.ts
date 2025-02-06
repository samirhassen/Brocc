import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MortgageLoanApplicationRoutingModule } from './mortgage-loan-application-routing.module';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { MortgageLoanApplicationApiService } from './services/mortgage-loan-application-api.service';
import { ApplicationsComponent } from './pages/applications/applications.component';
import { ApplicationComponent } from './pages/application/application.component';
import { SharedApplicationComponentsModule } from '../shared-application-components/shared-application-components.module';
import { MlApplicationsMenuComponent } from './components/ml-applications-menu/ml-applications-menu.component';
import { SearchApplicationsComponent } from './pages/search-applications/search-applications.component';
import { AgreementStandardMLComponent } from './steps/agreement-standard/agreement-standard.component';
import { KycStandardMLComponent } from './steps/kyc-standard/kyc-standard.component';
import { PaymentStandardMLComponent } from './steps/payment-standard/payment-standard.component';
import { CollateralStandardMLComponent } from './steps/collateral-standard/collateral-standard.component';
import { FinalCreditCheckStandardMLComponent } from './steps/final-credit-check-standard/final-credit-check-standard.component';
import { AuditAgreementStandardMLComponent } from './steps/audit-agreement-standard/audit-agreement-standard.component';
import { InitialCreditCheckStandardMLComponent } from './steps/initial-credit-check-standard/initial-credit-check-standard.component';
import { CreditCheckNewComponent } from './pages/credit-check-new/credit-check-new.component';
import { MlPropertyEditorComponent } from './pages/credit-check-new/ml-property-editor/ml-property-editor.component';
import { MlApplicationGeneralDataEditorComponent } from './pages/credit-check-new/ml-application-general-data-editor/ml-application-general-data-editor.component';
import { MlApplicationPolicyInfoComponent } from './components/ml-application-policy-info/ml-application-policy-info.component';
import { MlCreditCheckDecisionEditorComponent } from './components/ml-credit-check-decision-editor/ml-credit-check-decision-editor.component';
import { MlCreditCheckDecisionViewComponent } from './components/ml-credit-check-decision-view/ml-credit-check-decision-view.component';
import { ApplicationValuationsComponent } from './components/application-valuations/application-valuations.component';
import { UcbvValuationBuyerComponent } from './components/ucbv-valuation-buyer/ucbv-valuation-buyer.component';
import { ApplicationValuationPreviewComponent } from './components/application-valuations/application-valuation-preview/application-valuation-preview.component';
import { ApplicationDocumentsComponent } from './pages/application-documents/application-documents.component';
import { WaitingForInfoStandardComponent } from './steps/waiting-for-info-standard/waiting-for-info-standard.component';
import { PropertyLoansComponent } from './pages/credit-check-new/property-loans/property-loans.component';
import { OwnershipAndPossessionComponent } from './pages/ownership-and-possession/ownership-and-possession.component';
import { OwnershipCustomerlistComponent } from './pages/ownership-and-possession/ownership-customerlist/ownership-customerlist.component';
import { ApplicationPartiesComponent } from './pages/application-parties/application-parties.component';

@NgModule({
    providers: [MortgageLoanApplicationApiService],
    declarations: [
        ApplicationsComponent,
        ApplicationComponent,
        InitialCreditCheckStandardMLComponent,
        CollateralStandardMLComponent,
        KycStandardMLComponent,
        FinalCreditCheckStandardMLComponent,
        AuditAgreementStandardMLComponent,
        AgreementStandardMLComponent,
        PaymentStandardMLComponent,
        MlApplicationsMenuComponent,
        SearchApplicationsComponent,
        CreditCheckNewComponent,
        MlPropertyEditorComponent,
        MlApplicationGeneralDataEditorComponent,
        MlApplicationPolicyInfoComponent,
        MlCreditCheckDecisionEditorComponent,
        MlCreditCheckDecisionViewComponent,
        ApplicationValuationsComponent,
        UcbvValuationBuyerComponent,
        ApplicationValuationPreviewComponent,
        ApplicationDocumentsComponent,
        WaitingForInfoStandardComponent,
        PropertyLoansComponent,
        OwnershipAndPossessionComponent,
        OwnershipCustomerlistComponent,
        ApplicationPartiesComponent,
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        MortgageLoanApplicationRoutingModule,
        SharedApplicationComponentsModule,
        CommonComponentsModule,
    ],
})
export class MortgageLoanApplicationModule {}
