import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KycQuestionsComponent } from './pages/kyc-questions/kyc-questions.component';
import { KycQuestionTemplatesComponent } from './pages/kyc-question-templates/kyc-question-templates.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { CustomerKycRoutingModule } from './customer-kyc-routing.module';
import { ViewQuestionsTemplateComponent } from './components/view-questions-template/view-questions-template.component';
import { EditQuestionsTemplateComponent } from './components/edit-questions-template/edit-questions-template.component';
import { NtechComponentsModule } from 'projects/ntech-components/src/public-api';
import { KycHistoryComponent } from './components/kyc-history/kyc-history.component';

@NgModule({
    declarations: [
        KycQuestionsComponent,
        KycQuestionTemplatesComponent,
        ViewQuestionsTemplateComponent,
        EditQuestionsTemplateComponent,
        KycHistoryComponent,
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        CommonComponentsModule,
        CustomerKycRoutingModule,
        NtechComponentsModule,
    ],

    exports: [
        KycQuestionTemplatesComponent, //Component setting
    ],
})
export class CustomerKycModule {}
