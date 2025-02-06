import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { MortgageLoanMiddlePermissionsGuard } from '../login/guards/mortgageloan-middle-permissions-guard';
import { ApplicationDocumentsComponent } from './pages/application-documents/application-documents.component';
import { ApplicationPartiesComponent } from './pages/application-parties/application-parties.component';
import { ApplicationComponent } from './pages/application/application.component';
import { ApplicationsComponent } from './pages/applications/applications.component';
import { CreditCheckNewComponent } from './pages/credit-check-new/credit-check-new.component';
import { OwnershipAndPossessionComponent } from './pages/ownership-and-possession/ownership-and-possession.component';
import { SearchApplicationsComponent } from './pages/search-applications/search-applications.component';

let applicationRoutes = {
    path: 'mortgage-loan-application',
    children: [
        {
            path: 'application/:applicationNr',
            component: ApplicationComponent,
            data: {
                pageTitle: 'Application',
                customDefaultBackRoute: ['/mortgage-loan-application/applications'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'applications',
            component: ApplicationsComponent,
            data: { pageTitle: 'Applications', useFluidLayoutShell: true },
        },
        {
            path: 'search',
            component: SearchApplicationsComponent,
            data: { pageTitle: 'Search', useFluidLayoutShell: true },
        },
        {
            path: 'new-credit-check/:applicationNr',
            component: CreditCheckNewComponent,
            data: {
                pageTitle: 'Credit check',
                isNewCreditCheck: true,
                isFinal: false,
                customDefaultBackRoute: ['/mortgage-loan-application/application/:applicationNr'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'view-credit-check/:applicationNr',
            component: CreditCheckNewComponent,
            data: {
                pageTitle: 'Credit check',
                isNewCreditCheck: false,
                isFinal: false,
                customDefaultBackRoute: ['/mortgage-loan-application/application/:applicationNr'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'documents/:applicationNr',
            component: ApplicationDocumentsComponent,
            data: {
                pageTitle: 'Documents',
                customDefaultBackRoute: ['/mortgage-loan-application/application/:applicationNr'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'ownership-and-possession/:applicationNr',
            component: OwnershipAndPossessionComponent,
            data: {
                pageTitle: 'Ownership and right of possession',
                customDefaultBackRoute: ['/mortgage-loan-application/application/:applicationNr'],
                useFluidLayoutShell: true,
            },
        },
        {
            path: 'parties/:applicationNr',
            component: ApplicationPartiesComponent,
            data: {
                pageTitle: 'Parties',
                customDefaultBackRoute: ['/mortgage-loan-application/application/:applicationNr'],
                useFluidLayoutShell: true,
            },
        },
    ],
    resolve: { backTarget: BackTargetResolverService },
    data: { requireFeatures: ['ntech.feature.mortgageloans', 'ntech.feature.mortgageloans.standard'] },
    canActivate: [RequireFeaturesGuard, MortgageLoanMiddlePermissionsGuard],
};

const routes: Routes = [applicationRoutes];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class MortgageLoanApplicationRoutingModule {}
