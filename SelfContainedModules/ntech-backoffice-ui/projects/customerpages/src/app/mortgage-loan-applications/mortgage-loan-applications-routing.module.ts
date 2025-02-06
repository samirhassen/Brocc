import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerPagesAuthGuard } from '../common-services/customer-pages.auth.guard';
import { ApplicationCreatedComponent } from './open-pages/application-created/application-created.component';
import { LoanCalculatorComponent } from './open-pages/loan-calculator/loan-calculator.component';
import { StartWebapplicationComponent } from './secure-pages/initial-application/start-webapplication/start-webapplication.component';
import { KycTaskApplicationComponent } from './secure-pages/kyc-task-application/kyc-task-application.component';
import { OngoingApplicationComponent } from './secure-pages/ongoing-application/ongoing-application.component';
import { OverviewComponent } from './secure-pages/overview/overview.component';

const secureRoutes = [
    { path: 'webapplication/:sessionId/start', component: StartWebapplicationComponent },
    { path: 'overview', component: OverviewComponent },
    { path: 'application/:applicationNr', component: OngoingApplicationComponent },
    { path: 'application/:applicationNr/kyc', component: KycTaskApplicationComponent },
];
const openRoutes = [
    { path: 'calculator', component: LoanCalculatorComponent },
    { path: 'application/:applicationNr/created', component: ApplicationCreatedComponent },
];

const features = ['ntech.feature.mortgageloans', 'ntech.feature.mortgageloans.standard'];
const routes: Routes = [
    {
        path: 'mortgage-loan-applications/secure',
        children: secureRoutes,
        resolve: {},
        data: { requireFeatures: features, requiredUserRole: 'EmbeddedCustomerPagesStandardCustomer' },
        canActivate: [CustomerPagesAuthGuard],
    },
    {
        path: 'mortgage-loan-applications/open',
        children: openRoutes,
        resolve: {},
        data: { requireFeatures: features },
        canActivate: [CustomerPagesAuthGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class MortgageLoanApplicationsRoutingModule {}
