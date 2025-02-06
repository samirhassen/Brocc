import { Routes } from '@angular/router';
import { BackTargetResolverService } from 'src/app/common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from 'src/app/common-services/require-features.guard';
import { MiddlePermissionsGuard } from 'src/app/login/guards/middle-permissions-guard';
import { ChangeTermsPageComponent } from './change-terms-page/change-terms-page.component';
import { CreditDetailsPageComponent } from './credit-details-page/credit-details-page.component';
import { CustomerPageComponent } from './customer-page/customer-page.component';
import { DirectDebitPageComponent } from './direct-debit-page/direct-debit-page.component';
import { DocumentsPageComponent } from './documents-page/documents-page.component';
import { LegacyMlCollateralPageComponent } from './legacy-ml-collateral-page/legacy-ml-collateral-page.component';
import { MlAmortizationPageSeComponent } from './ml-amortization-se-page/ml-amortization-se-page.component';
import { MlChangeTermsPageComponent } from './ml-change-terms-page/ml-change-terms-page.component';
import { MlCollateralPageComponent } from './ml-collateral-page/ml-collateral-page.component';
import { NotificationPageComponent } from './notification-page/notification-page.component';
import { NotificationsPageComponent } from './notifications-page/notifications-page.component';
import { SearchPageComponent } from './search-page/search-page.component';
import { SettlementComponent } from './settlement/settlement.component';

export const creditRoutes: Routes = [
    {
        path: 'credit',
        children: [
            {
                path: 'details/:creditNr',
                component: CreditDetailsPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit details' },
            },
            {
                path: 'customer/:creditNr',
                component: CustomerPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit customer' },
            },
            {
                path: 'notifications/:creditNr',
                component: NotificationsPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit notifications' },
            },
            {
                path: 'notification/:notificationId',
                component: NotificationPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit notification' },
            },
            {
                path: 'changeTerms/:creditNr',
                component: ChangeTermsPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit change terms' },
            },
            {
                path: 'mortgageLoanStandardChangeTerms/:creditNr',
                component: MlChangeTermsPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit change terms' },
            },
            {
                path: 'settlement/:creditNr',
                component: SettlementComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit settlement' },
            },
            {
                path: 'mortgageloanStandardCollateral/:creditNr',
                component: MlCollateralPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit collateral' },
            },
            {
                path: 'mortgageloanStandardAmortizationSe/:creditNr',
                component: MlAmortizationPageSeComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Amortization' },
            },            
            {
                path: 'documents/:creditNr',
                component: DocumentsPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit documents' },
            },
            {
                path: 'security/:creditNr',
                component: LegacyMlCollateralPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit collateral' },
            },
            {
                path: 'search',
                component: SearchPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit search' },
            },
            {
                path: 'directDebit/:creditNr',
                component: DirectDebitPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit direct debit' },
            },
        ],
        data: {
            requireAnyFeature: [
                'ntech.feature.unsecuredloans',
                'ntech.feature.companyloans',
                'ntech.feature.mortgageloans.standard',
                'ntech.feature.mortgageloans',
            ],
        },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, MiddlePermissionsGuard],
    },
];
