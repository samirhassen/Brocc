import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { HighPermissionsGuard } from '../login/guards/high-permissions-guard';
import { MiddlePermissionsGuard } from '../login/guards/middle-permissions-guard';
import { KycQuestionTemplatesComponent } from './pages/kyc-question-templates/kyc-question-templates.component';
import { KycQuestionsComponent } from './pages/kyc-questions/kyc-questions.component';

const middleRoutes: Routes = [
    {
        path: 'questions/:customerId',
        component: KycQuestionsComponent,
        data: { useFluidLayoutShell: true, pageTitle: 'KYC questions' },
    },
];
const highRoutes: Routes = [
    {
        path: 'templates',
        component: KycQuestionTemplatesComponent,
        data: { useFluidLayoutShell: true, pageTitle: 'Kyc question templates' },
    },
];

const requireAnyFeature: string[] = ['feature.customerpages.kyc'];

const routes: Routes = [
    {
        path: 'customer-kyc',
        children: [...middleRoutes],
        data: {
            requireAnyFeature: requireAnyFeature,
        },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, MiddlePermissionsGuard],
    },
    {
        path: 'customer-kyc',
        children: [...highRoutes],
        data: {
            requireAnyFeature: requireAnyFeature,
        },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, HighPermissionsGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class CustomerKycRoutingModule {}
