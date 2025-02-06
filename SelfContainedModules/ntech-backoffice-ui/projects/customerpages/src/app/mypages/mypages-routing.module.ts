import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerPagesAuthGuard } from '../common-services/customer-pages.auth.guard';
import { MyContactinfoComponent } from './pages/my-data/my-contactinfo/my-contactinfo.component';
import { MyDataComponent } from './pages/my-data/my-data.component';
import { MyDocumentsComponent } from './pages/my-documents/my-documents.component';
import { MyLoansComponent } from './pages/my-loans/my-loans.component';
import { MyMessagesComponent } from './pages/my-messages/my-messages.component';
import { MypagesOverviewComponent } from './pages/mypages-overview/mypages-overview.component';
import { MypagesTestpageComponent } from './pages/mypages-testpage/mypages-testpage.component';
import { LoanComponent } from './pages/loan/loan.component';

const alwaysActiveRoutes: Routes = [
    {
        path: 'my',
        children: [
            { path: 'overview', component: MypagesOverviewComponent },
            { path: 'data', component: MyDataComponent },
            { path: 'data/contactinfo', component: MyContactinfoComponent },
            { path: 'loans', component: MyLoansComponent },
            { path: 'documents', component: MyDocumentsComponent },
            { path: 'messages', component: MyMessagesComponent },
            { path: 'menu-test/:menuCode', component: MypagesTestpageComponent },
        ],
        resolve: {},
        data: {
            requireFeatures: [],
            loginTargetName: 'StandardOverview',
            requiredUserRole: 'EmbeddedCustomerPagesStandardCustomer',
        },
        canActivate: [CustomerPagesAuthGuard],
    },
];

const loansOnlyRoutes: Routes = [
    {
        path: 'my/sl',
        children: [{ path: 'loan/:creditNr', component: LoanComponent }],
        resolve: {},
        data: {
            requireAnyFeature: ['ntech.feature.unsecuredloans.standard', 'ntech.feature.mortgageloans.standard'],
            loginTargetName: 'StandardOverview',
            requiredUserRole: 'EmbeddedCustomerPagesStandardCustomer',
        },
        canActivate: [CustomerPagesAuthGuard],
    },
];

let routes = [...alwaysActiveRoutes, ...loansOnlyRoutes];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class MypagesRoutingModule {}
