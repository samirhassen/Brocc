import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OverviewComponent } from './pages/overview/overview.component';
import { UnsecuredLoanApplicationsRoutingModule } from './unsecured-loan-applications-routing.module';
import { ShellComponent } from './components/shell/shell.component';
import { CustomerPagesApplicationComponent } from './pages/customer-pages-application/customer-pages-application.component';
import { CustomerPagesApplicationOfferComponent } from './components/customer-pages-application-offer/customer-pages-application-offer.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CustomerPagesAgreementComponent } from './components/customer-pages-agreement/customer-pages-agreement.component';
import { CustomerPagesBankaccountsComponent } from './components/customer-pages-bankaccounts/customer-pages-bankaccounts.component';
import { CustomerPagesDirectDebitComponent } from './components/customer-pages-direct-debit/customer-pages-direct-debit.component';
import { TaskToggleBlockComponent } from './components/task-toggle-block/task-toggle-block.component';
import { CustomerPagesApplicationTasksComponent } from './pages/customer-pages-application/customer-pages-application-tasks.component';
import { SharedComponentsModule } from '../shared-components/shared-components.module';
import { PreApplicationEconomyComponent } from './pages/pre-application-economy/pre-application-economy.component';
import { PreApplicationCalculatorComponent } from './pages/pre-application-calculator/pre-application-calculator.component';
import { PreApplicationApplicantsComponent } from './pages/pre-application-applicants/pre-application-applicants.component';
import { PreApplicationReceivedComponent } from './pages/pre-application-received/pre-application-received.component';
import { PreApplicationDatashareComponent } from './pages/pre-application-datashare/pre-application-datashare.component';
import { NtechComponentsModule } from 'projects/ntech-components/src/public-api';

@NgModule({
    declarations: [
        OverviewComponent,
        ShellComponent,
        TaskToggleBlockComponent,
        CustomerPagesApplicationTasksComponent,
        CustomerPagesApplicationComponent,
        CustomerPagesApplicationOfferComponent,
        CustomerPagesAgreementComponent,
        CustomerPagesBankaccountsComponent,
        CustomerPagesDirectDebitComponent,
        PreApplicationEconomyComponent,
        PreApplicationCalculatorComponent,
        PreApplicationApplicantsComponent,
        PreApplicationReceivedComponent,
        PreApplicationDatashareComponent,
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        SharedComponentsModule,
        NtechComponentsModule,
        UnsecuredLoanApplicationsRoutingModule,
    ],
})
export class UnsecuredLoanApplicationsModule {} //angular-in-memory-web-api for testing
