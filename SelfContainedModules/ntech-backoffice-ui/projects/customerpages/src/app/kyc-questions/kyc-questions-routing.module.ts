import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerPagesAuthGuard } from '../common-services/customer-pages.auth.guard';
import { KycOverviewComponent } from './pages/kyc-overview/kyc-overview.component';
import { QuestionsSessionComponent } from './pages/questions-session/questions-session.component';

const loggedInRoutes: Routes = [
    { path: 'overview', component: KycOverviewComponent }
];

const publicRoutes : Routes = [
    { path: 'questions-session/:sessionId', component: QuestionsSessionComponent }
];

const routes: Routes = [
    {
        path: 'kyc',
        children: loggedInRoutes,
        resolve: {},
        data: {
            requireFeatures: ['feature.customerpages.kyc'],
            loginTargetName: 'KycQuestions',
            requiredUserRole: 'EmbeddedCustomerPagesStandardCustomer',
        },
        canActivate: [CustomerPagesAuthGuard],
    },
    {
        path: 'public-kyc',
        children: publicRoutes,
        resolve: {},
        data: {
            requireFeatures: ['feature.customerpages.kyc']
        },
        canActivate: [],
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class KycQuestionsRoutingModule {}
