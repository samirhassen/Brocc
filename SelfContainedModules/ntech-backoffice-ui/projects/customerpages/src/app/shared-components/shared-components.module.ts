import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CustomerpagesShellComponent } from './customerpages-shell/customerpages-shell.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SecureMessagesEditListComponent } from './secure-messages-edit-list/secure-messages-edit-list.component';
import { QuillModule } from 'ngx-quill';
import { ApplicationsListComponent } from './applications-list/applications-list.component';
import { CustomerPagesApplicationMessagesComponent } from './customer-pages-application-messages/customer-pages-application-messages.component';
import { TaskPageComponent } from './task-page/task-page.component';
import { TaskKycQuestionsComponent } from './task-kyc-questions/task-kyc-questions.component';
import { CalculatorSliderComponent } from './calculator-slider/calculator-slider.component';
import { NgxSliderModule } from 'ngx-slider-v2';

@NgModule({
    declarations: [
        CustomerpagesShellComponent,
        SecureMessagesEditListComponent,
        ApplicationsListComponent,
        CustomerPagesApplicationMessagesComponent,
        TaskPageComponent,
        TaskKycQuestionsComponent,
        CalculatorSliderComponent
    ],
    imports: [CommonModule, FormsModule, ReactiveFormsModule, NgxSliderModule, RouterModule, QuillModule.forRoot()],
    exports: [
        CustomerpagesShellComponent,
        SecureMessagesEditListComponent,
        ApplicationsListComponent,
        CustomerPagesApplicationMessagesComponent,
        TaskPageComponent,
        TaskKycQuestionsComponent,
        CalculatorSliderComponent
    ],
})
export class SharedComponentsModule {}
