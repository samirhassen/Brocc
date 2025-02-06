import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MortgageLoanApplicationsRoutingModule } from './mortgage-loan-applications-routing.module';
import { StartWebapplicationComponent } from './secure-pages/initial-application/start-webapplication/start-webapplication.component';
import { LoanCalculatorComponent } from './open-pages/loan-calculator/loan-calculator.component';
import { MlShellComponent } from './components/ml-shell/ml-shell.component';
import { CalculatorTabMoveComponent } from './open-pages/loan-calculator/calculator-tab-move/calculator-tab-move.component';
import { CalculatorTabPurchaseComponent } from './open-pages/loan-calculator/calculator-tab-purchase/calculator-tab-purchase.component';
import { CalculatorTabAdditionalloanComponent } from './open-pages/loan-calculator/calculator-tab-additionalloan/calculator-tab-additionalloan.component';
import { ApplicationWizardShellComponent } from './secure-pages/initial-application/components/application-wizard-shell/application-wizard-shell.component';
import { ApplicationStepObjectComponent } from './secure-pages/initial-application/components/application-step-object/application-step-object.component';
import { ApplicationStepApplicantsComponent } from './secure-pages/initial-application/components/application-step-applicants/application-step-applicants.component';
import { ApplicationStepEconomyComponent } from './secure-pages/initial-application/components/application-step-economy/application-step-economy.component';
import { ApplicationStepConsentComponent } from './secure-pages/initial-application/components/application-step-consent/application-step-consent.component';
import { ApplicationCreatedComponent } from './open-pages/application-created/application-created.component';
import { SharedComponentsModule } from '../shared-components/shared-components.module';
import { JoinPipe } from 'src/app/common-components/pipes/join.pipe';
import { OverviewComponent } from './secure-pages/overview/overview.component';
import { OngoingApplicationComponent } from './secure-pages/ongoing-application/ongoing-application.component';
import { KycTaskApplicationComponent } from './secure-pages/kyc-task-application/kyc-task-application.component';

@NgModule({
    declarations: [
        StartWebapplicationComponent,
        LoanCalculatorComponent,
        MlShellComponent,
        CalculatorTabMoveComponent,
        CalculatorTabPurchaseComponent,
        CalculatorTabAdditionalloanComponent,
        ApplicationWizardShellComponent,
        ApplicationStepObjectComponent,
        ApplicationStepApplicantsComponent,
        ApplicationStepEconomyComponent,
        ApplicationStepConsentComponent,
        ApplicationCreatedComponent,
        JoinPipe,
        OverviewComponent,
        OngoingApplicationComponent,
        KycTaskApplicationComponent,
    ],
    imports: [
        SharedComponentsModule,
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        MortgageLoanApplicationsRoutingModule,
    ],
})
export class MortgageLoanApplicationsModule {}
