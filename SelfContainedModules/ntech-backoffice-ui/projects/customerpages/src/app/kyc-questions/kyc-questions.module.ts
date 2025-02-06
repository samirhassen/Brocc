import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KycQuestionsRoutingModule } from './kyc-questions-routing.module';
import { SharedComponentsModule } from '../shared-components/shared-components.module';
import { ReactiveFormsModule } from '@angular/forms';
import { QuillModule } from 'ngx-quill';
import { KycOverviewComponent } from './pages/kyc-overview/kyc-overview.component';
import { NtechComponentsModule } from 'projects/ntech-components/src/public-api';
import { QuestionsSessionComponent } from './pages/questions-session/questions-session.component';

@NgModule({
  declarations: [  
    KycOverviewComponent, QuestionsSessionComponent
  ],
  imports: [
    CommonModule,
    SharedComponentsModule,
    KycQuestionsRoutingModule,
    ReactiveFormsModule,
    QuillModule.forRoot(),
    NtechComponentsModule
  ]
})
export class KycQuestionsModule { }
