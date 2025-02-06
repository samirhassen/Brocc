import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { ApplicationCancelButtonsComponent } from './components/application-cancel-buttons/application-cancel-buttons.component';
import { ApplicationCommentsComponent } from './components/application-comments/application-comments.component';
import { ApplicationAssignedHandlersComponent } from './components/application-assigned-handlers/application-assigned-handlers.component';
import { ApplicationsListComponent } from './components/applications-list/applications-list.component';
import { ApplicationsSearchComponent } from './components/applications-search/applications-search.component';
import { StepStatusBlockComponent } from './components/step-status-block/step-status-block.component';
import { EditblockFormFieldComponent } from './components/editblock-form-field/editblock-form-field.component';
import { EditFormComponent } from './components/edit-form/edit-form.component';
import { ApplicantDataEditorComponent } from './components/applicant-data-editor/applicant-data-editor.component';
import { HouseholdEconomyDataEditorComponent } from './components/household-economy-data-editor/household-economy-data-editor.component';
import { DisplayNameFromCodePipe } from './pipes/display-name-from-code.pipe';
import { PolicyFilterDetailsComponent } from './components/policy-filter-details/policy-filter-details.component';
import { PolicyFilterIconComponent } from './components/policy-filter-icon/policy-filter-icon.component';
import { PolicyFilterRuleResultIconComponent } from './components/policy-filter-rule-result-icon/policy-filter-rule-result-icon.component';
import { ApplicationPolicyInfoWrapperComponent } from './components/application-policy-info-wrapper/application-policy-info-wrapper.component';
import { ApplicationCreditreportsComponent } from './components/application-creditreports/application-creditreports.component';
import { ApplicationPolicyInfoLtlDetailsComponent } from './components/application-policy-info-ltl-details/application-policy-info-ltl-details.component';
import { CreditDecisionRejectionEditorComponent } from './components/credit-decision-rejection-editor/credit-decision-rejection-editor.component';
import { CreditDecisionEditorTabsComponent } from './components/credit-decision-editor-tabs/credit-decision-editor-tabs.component';
import { PrototypeFormComponent } from '../common-components/prototype-form/prototype-form.component';
import { ApplicationNumberedIconWithLinkComponent } from './components/application-numbered-icon-with-link/application-numbered-icon-with-link.component';
import { StatusIconComponent } from './components/status-icon/status-icon.component';
import { NtechToggleComponent } from './components/ntech-toggle/ntech-toggle.component';
import { CustomerLinkButtonComponent } from './components/customer-link-button/customer-link-button.component';
import { KycStepApplicantStatusesComponent } from './components/kyc-step-applicant-statuses/kyc-step-applicant-statuses.component';
import { PolicyFilterRejectionRecommendationsComponent } from './components/policy-filter-rejection-recommendations/policy-filter-rejection-recommendations.component';
import { BookkeepingRulesEditComponent } from './components/bookkeeping/bookkeeping-rules-edit.component';
import { CreditHandlerLimitComponent } from './components/credit-handler-limit/credit-handler-limit.component';
import { CreditHandlerSearchFilter } from './components/credit-handler-limit/filters/credit-handler-search-filter.component';
import { ApplicationSharedBankdataComponent } from './components/application-shared-bankdata/application-shared-bankdata.component';

@NgModule({
    declarations: [
        ApplicationCancelButtonsComponent,
        ApplicationCommentsComponent,
        ApplicationAssignedHandlersComponent,
        ApplicationsListComponent,
        ApplicationsSearchComponent,
        StepStatusBlockComponent,
        EditblockFormFieldComponent,
        EditFormComponent,
        ApplicantDataEditorComponent,
        HouseholdEconomyDataEditorComponent,
        DisplayNameFromCodePipe,
        PolicyFilterDetailsComponent,
        PolicyFilterIconComponent,
        PolicyFilterRuleResultIconComponent,
        ApplicationPolicyInfoWrapperComponent,
        ApplicationCreditreportsComponent,
        ApplicationPolicyInfoLtlDetailsComponent,
        CreditDecisionRejectionEditorComponent,
        CreditDecisionEditorTabsComponent,
        PrototypeFormComponent,
        ApplicationNumberedIconWithLinkComponent,
        StatusIconComponent,
        NtechToggleComponent,
        CustomerLinkButtonComponent,
        KycStepApplicantStatusesComponent,
        PolicyFilterRejectionRecommendationsComponent,
        BookkeepingRulesEditComponent,
        CreditHandlerLimitComponent,
        CreditHandlerSearchFilter,
        ApplicationSharedBankdataComponent,
    ],
    imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, CommonComponentsModule],
    exports: [
        ApplicationCancelButtonsComponent,
        ApplicationCommentsComponent,
        ApplicationAssignedHandlersComponent,
        ApplicationsListComponent,
        ApplicationsSearchComponent,
        StepStatusBlockComponent,
        EditblockFormFieldComponent,
        EditFormComponent,
        ApplicantDataEditorComponent,
        HouseholdEconomyDataEditorComponent,
        DisplayNameFromCodePipe,
        PolicyFilterDetailsComponent,
        PolicyFilterIconComponent,
        PolicyFilterRuleResultIconComponent,
        ApplicationPolicyInfoWrapperComponent,
        ApplicationCreditreportsComponent,
        ApplicationPolicyInfoLtlDetailsComponent,
        CreditDecisionRejectionEditorComponent,
        CreditDecisionEditorTabsComponent,
        PrototypeFormComponent,
        ApplicationNumberedIconWithLinkComponent,
        StatusIconComponent,
        NtechToggleComponent,
        CustomerLinkButtonComponent,
        KycStepApplicantStatusesComponent,
        PolicyFilterRejectionRecommendationsComponent,
        BookkeepingRulesEditComponent,
        CreditHandlerLimitComponent,
        ApplicationSharedBankdataComponent,
    ],
})
export class SharedApplicationComponentsModule {}
