import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { ConsumerCreditMiddlePermissionsGuard } from '../login/guards/consumercredit-middle-permissions-guard';
import { ApplicationBasisPageComponent } from './pages/application-basis-page/application-basis-page.component';
import { ApplicationManualRegistrationComponent } from './pages/application-manual-registration/application-manual-registration.component';
import { ApplicationComponent } from './pages/application/application.component';
import { ApplicationsComponent } from './pages/applications/applications.component';
import { CreditCheckNewComponent } from './pages/credit-check-new/credit-check-new.component';
import { DirectDebitManagementComponent } from './pages/direct-debit-management/direct-debit-management.component';
import { BankshareTestComponent } from './pages/bankshare-test/bankshare-test.component';

let applicationRoutes = {
    path: 'unsecured-loan-application',
    children: [
        {
            path: 'application/:applicationNr',
            component: ApplicationComponent,
            data: {
                pageTitle: 'Application',
                customDefaultBackRoute: ['/unsecured-loan-application/applications'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'application-basis/:applicationNr',
            component: ApplicationBasisPageComponent,
            data: {
                pageTitle: 'Application basis',
                customDefaultBackRoute: ['/unsecured-loan-application/application/:applicationNr'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'applications',
            component: ApplicationsComponent,
            data: { pageTitle: 'Applications', useFluidLayoutShell: true },
        },
        {
            path: 'new-credit-check/:applicationNr',
            component: CreditCheckNewComponent,
            data: {
                pageTitle: 'Credit check',
                isNewCreditCheck: true,
                customDefaultBackRoute: ['/unsecured-loan-application/application/:applicationNr'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'view-credit-check/:applicationNr',
            component: CreditCheckNewComponent,
            data: {
                pageTitle: 'Credit check details',
                isNewCreditCheck: false,
                customDefaultBackRoute: ['/unsecured-loan-application/application/:applicationNr'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'direct-debit/:applicationNr',
            component: DirectDebitManagementComponent,
            data: {
                pageTitle: 'Direct debit management',
                customDefaultBackRoute: ['/unsecured-loan-application/application/:applicationNr'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'register-application',
            component: ApplicationManualRegistrationComponent,
            data: { pageTitle: 'Register application', useFluidLayoutShell: true },
        },
        {
            path: 'bankshare-test',
            component: BankshareTestComponent,
            data: { pageTitle: 'Test banksharing', useFluidLayoutShell: true }
        }
    ],
    resolve: { backTarget: BackTargetResolverService },
    data: { requireFeatures: ['ntech.feature.unsecuredloans', 'ntech.feature.unsecuredloans.standard'] },
    canActivate: [RequireFeaturesGuard, ConsumerCreditMiddlePermissionsGuard],
};

const routes: Routes = [applicationRoutes];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class UnsecuredLoanApplicationRoutingModule {}
