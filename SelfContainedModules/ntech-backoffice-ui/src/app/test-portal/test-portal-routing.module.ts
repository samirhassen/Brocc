import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService, CrossModuleNavigationTarget } from '../common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { OnlyInTestGuard } from '../login/guards/only-in-test.guard';
import { CreateMortgageLoanApplicationComponent } from './pages/mortgage-standard/create-application/create-application.component';
import { CreateMortgageLoanComponent } from './pages/mortgage-standard/create-mortgage-loan/create-mortgage-loan.component';
import { CreateMortgageloanSeComponent } from './pages/mortgage-standard/create-mortgageloan-se/create-mortgageloan-se.component';
import { CreateApplicationComponent } from './pages/unsecured-standard/create-application/create-application.component';
import { ShowReportComponent } from './pages/show-report/show-report.component';
import { SingleCreditFunctionComponent } from './pages/single-credit-function/single-credit-function.component';

let unsecuredStandard = ['ntech.feature.unsecuredloans', 'ntech.feature.unsecuredloans.standard'];
let mortgageLoansStandard = ['ntech.feature.mortgageloans', 'ntech.feature.mortgageloans.standard'];

// All routes in this testmodule will have backTarget to nTest startpage.
let backToTest = CrossModuleNavigationTarget.create('TestModuleStartPage', {});

// Guards can be added on multiple levels, ex. OnlyInTestGuard on 'test' and RequireFeaturesGuard on its children paths.
const routes: Routes = [
    {
        path: 'test',
        children: [
            {
                path: 'unsecured-standard/createapplication',
                component: CreateApplicationComponent,
                data: {
                    pageTitle: 'Unsecured standard loan application',
                    useFluidLayoutShell: true,
                    requireFeatures: unsecuredStandard,
                },
                canActivate: [RequireFeaturesGuard],
            },
            {
                path: 'mortgage-standard/createapplication',
                component: CreateMortgageLoanApplicationComponent,
                data: {
                    pageTitle: 'Mortgage standard loan application',
                    useFluidLayoutShell: true,
                    requireFeatures: mortgageLoansStandard,
                },
                canActivate: [RequireFeaturesGuard],
            },
            {
                path: 'mortgage-standard/createloan',
                component: CreateMortgageLoanComponent,
                data: {
                    pageTitle: 'Mortgage standard loan',
                    useFluidLayoutShell: true,
                    requireFeatures: mortgageLoansStandard,
                },
                canActivate: [RequireFeaturesGuard],
            },
            {
                path: 'mortgage-standard/createloan-se',
                component: CreateMortgageloanSeComponent,
                data: {
                    pageTitle: 'Mortgage standard loan',
                    useFluidLayoutShell: true,
                    requireFeatures: mortgageLoansStandard,
                },
                canActivate: [RequireFeaturesGuard],
            },
            {
                path: 'single-credit-function',
                component: SingleCreditFunctionComponent,
                data: {
                    pageTitle: 'Single credit function',
                    useFluidLayoutShell: true,
                    requireAnyFeature: ['ntech.feature.unsecuredloans', 'ntech.feature.mortgageloans'],
                },
                canActivate: [RequireFeaturesGuard],
            },            
            {
                path: 'show-report/:moduleName ', 
                children: [{
                    pathMatch: 'prefix',
                    path: '**', 
                    component: ShowReportComponent,
                    data: {
                        pageTitle: 'Loading report',
                        useFluidLayoutShell: true
                    }
                }]

            }         
        ],
        resolve: { backTarget: BackTargetResolverService },
        data: { fixedBackTarget: backToTest },
        canActivate: [OnlyInTestGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class TestPortalRoutingModule {}
