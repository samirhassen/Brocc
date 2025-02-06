import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerPagesAuthGuard } from '../common-services/customer-pages.auth.guard';
import { CustomerPagesApplicationComponent } from './pages/customer-pages-application/customer-pages-application.component';
import { OverviewComponent } from './pages/overview/overview.component';
import { PreApplicationEconomyComponent } from './pages/pre-application-economy/pre-application-economy.component';
import { PreApplicationCalculatorComponent } from './pages/pre-application-calculator/pre-application-calculator.component';
import { PreApplicationApplicantsComponent } from './pages/pre-application-applicants/pre-application-applicants.component';
import { PreApplicationReceivedComponent } from './pages/pre-application-received/pre-application-received.component';
import { PreApplicationDatashareComponent } from './pages/pre-application-datashare/pre-application-datashare.component';

const routes: Routes = [
    {
        path: 'unsecured-loan-applications',
        children: [
            { path: 'overview', component: OverviewComponent },
            { path: 'application/:applicationNr', component: CustomerPagesApplicationComponent },
        ],
        resolve: {},
        data: {
            requireFeatures: ['ntech.feature.unsecuredloans', 'ntech.feature.unsecuredloans.standard'],
            loginTargetName: 'ApplicationsOverview',
            requiredUserRole: 'EmbeddedCustomerPagesStandardCustomer',
        },
        canActivate: [CustomerPagesAuthGuard],
    },
    {
        path: 'ul-webapplications',
        children: [
            { path: 'application-calculator', component: PreApplicationCalculatorComponent },
            { path: 'application-applicants/:preApplicationId', component: PreApplicationApplicantsComponent },
            { path: 'application-economy/:preApplicationId', component: PreApplicationEconomyComponent },
            { path: 'application-datashare/:preApplicationId', component: PreApplicationDatashareComponent },
            { path: 'application-received/:applicationNr', component: PreApplicationReceivedComponent }
        ],
        resolve: {},
        data: {
            requireFeatures: ['ntech.feature.unsecuredloans', 'ntech.feature.unsecuredloans.standard', 'ntech.feature.unsecuredloans.webapplication'],
        },
        canActivate: [],
    }
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class UnsecuredLoanApplicationsRoutingModule {}
