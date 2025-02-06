import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MypagesRoutingModule } from './mypages-routing.module';
import { MypagesOverviewComponent } from './pages/mypages-overview/mypages-overview.component';
import { SharedComponentsModule } from '../shared-components/shared-components.module';
import { MypagesTestpageComponent } from './pages/mypages-testpage/mypages-testpage.component';
import { MypagesOverviewLoansComponent } from './pages/mypages-overview/mypages-overview-loans/mypages-overview-loans.component';
import { MyDataComponent } from './pages/my-data/my-data.component';
import { MyContactinfoComponent } from './pages/my-data/my-contactinfo/my-contactinfo.component';
import { ReactiveFormsModule } from '@angular/forms';
import { MyLoansComponent } from './pages/my-loans/my-loans.component';
import { LoanComponent } from './pages/loan/loan.component';
import { UnpaidNotificationsComponent } from './components/unpaid-notifications/unpaid-notifications.component';
import { UlAmortizationPlanComponent } from './pages/loan/ul-amortization-plan/ul-amortization-plan.component';
import { InterestHistoryComponent } from './pages/loan/interest-history/interest-history.component';
import { CapitalTransactionHistoryComponent } from './pages/loan/capital-transaction-history/capital-transaction-history.component';
import { MyDocumentsComponent } from './pages/my-documents/my-documents.component';
import { MyMessagesComponent } from './pages/my-messages/my-messages.component';
import { QuillModule } from 'ngx-quill';
import { MypagesShellComponent } from './components/mypages-shell/mypages-shell.component';
import { MlAmortizationPlanComponent } from './pages/loan/ml-amortization-plan/ml-amortization-plan.component';
import { NtechComponentsModule } from 'projects/ntech-components/src/public-api';

@NgModule({
    declarations: [
        MypagesOverviewComponent,
        MypagesTestpageComponent,
        MypagesOverviewLoansComponent,
        MyDataComponent,
        MyContactinfoComponent,
        MyLoansComponent,
        LoanComponent,
        UnpaidNotificationsComponent,
        UlAmortizationPlanComponent,
        MlAmortizationPlanComponent,
        InterestHistoryComponent,
        CapitalTransactionHistoryComponent,
        MyDocumentsComponent,
        MyMessagesComponent,
        MypagesShellComponent,
    ],
    imports: [CommonModule, SharedComponentsModule, MypagesRoutingModule, ReactiveFormsModule, QuillModule.forRoot(), NtechComponentsModule],
})
export class MypagesModule {}
