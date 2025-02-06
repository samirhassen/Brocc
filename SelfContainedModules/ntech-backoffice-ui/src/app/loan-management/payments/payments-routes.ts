import { Routes } from "@angular/router";
import { UnplacedPaymentPageComponent } from "./unplaced-payment-page/unplaced-payment-page.component";
import { BackTargetResolverService } from "src/app/common-services/backtarget-resolver.service";
import { RequireFeaturesGuard } from "src/app/common-services/require-features.guard";
import { MiddlePermissionsGuard } from "src/app/login/guards/middle-permissions-guard";
import { ImportPaymentfilePageComponent } from "./import-paymentfile-page/import-paymentfile-page.component";

export const paymentsRoutes: Routes = [
    {
        path: 'credit-payments',
        children: [
            {
                path: 'handle-unplaced/:paymentId',
                component: UnplacedPaymentPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit - Unplaced payment' },
            },
            {
                path: 'import-incoming-paymentfile',
                component: ImportPaymentfilePageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Credit - Import incoming payment file' },
            }            
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