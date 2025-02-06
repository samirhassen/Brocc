import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SettingsRoutingModule } from './settings-routing.module';
import { SettingsListComponent } from './pages/settings-list/settings-list.component';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { ReactiveFormsModule } from '@angular/forms';
import { SettingFormComponent } from './components/setting-types/setting-form/setting-form.component';
import { SettingHtmltemplateComponent } from './components/setting-types/setting-htmltemplate/setting-htmltemplate.component';
import { QuillModule } from 'ngx-quill';
import { SettingGroupComponent } from './components/setting-group/setting-group.component';
import { SettingSingleComponent } from './components/setting-single/setting-single.component';
import { SettingBankaccountComponent } from './components/setting-types/setting-bankaccount/setting-bankaccount.component';
import { BankAccountNrViewComponent } from './components/setting-types/setting-bankaccount/bank-account-nr-view/bank-account-nr-view.component';
import { SettingKycUpdateFrequencyComponent } from './components/setting-types/setting-kyc-update-frequency/setting-kyc-update-frequency.component';
import { SettingAddRemoveSingleComponent } from './components/setting-types/setting-addremovesingle/setting-add-remove-rows.component';
import { CustomerKycModule } from '../customer-kyc/customer-kyc.module';
import { LoanPolicyFiltersModule } from '../loan-policyfilters/loan-policy-filters.module';
import { SettingComponentComponent } from './components/setting-types/setting-component/setting-component.component';
import { SharedApplicationComponentsModule } from '../shared-application-components/shared-application-components.module';

@NgModule({
    declarations: [
        SettingsListComponent,
        SettingComponentComponent,
        SettingFormComponent,
        SettingHtmltemplateComponent,
        SettingGroupComponent,
        SettingSingleComponent,
        SettingBankaccountComponent,
        BankAccountNrViewComponent,
        SettingKycUpdateFrequencyComponent,
        SettingAddRemoveSingleComponent
    ],
    imports: [
        CommonModule,
        ReactiveFormsModule,
        SettingsRoutingModule,
        CommonComponentsModule,
        QuillModule,
        SharedApplicationComponentsModule,

        //Component settings
        CustomerKycModule,
        LoanPolicyFiltersModule,
    ],
})
export class SettingsModule {}
