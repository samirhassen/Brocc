import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { LoanManagementRoutingModule } from './loan-management-routing.module';
import { TerminationLetterComponent } from './default-management/termination-letter/termination-letter.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DebtCollectionComponent } from './default-management/debt-collection/debt-collection.component';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { DebtCollectionScheduledTaskComponent } from './default-management/debt-collection-scheduled-task/debt-collection-scheduled-task.component';
import { AnnualStatementsComponent } from './scheduled-tasks/annual-statements/annual-statements.component';
import { ChangeRatesPageComponent } from './fixed-reference-interests/change-rates-page/change-rates-page.component';
import { RateEditorComponent } from './fixed-reference-interests/rate-editor/rate-editor.component';
import { PendingChangeComponent } from './fixed-reference-interests/pending-change/pending-change.component';
import { RemindersComponent } from './scheduled-tasks/reminders/reminders.component';
import { CreditMenuComponent } from './credit/credit-menu/credit-menu.component';
import { CreditDetailsPageComponent } from './credit/credit-details-page/credit-details-page.component';
import { CustomerPageComponent } from './credit/customer-page/customer-page.component';
import { TransactionsComponent } from './credit/credit-details-page/transactions/transactions.component';
import { CreditAmortizationPlanComponent } from './credit/credit-details-page/credit-amortization-plan/credit-amortization-plan.component';
import { MortgageAmortizationPlanComponent } from './credit/credit-details-page/mortgage-amortization-plan/mortgage-amortization-plan.component';
import { CreditCommentsComponent } from './credit/credit-comments/credit-comments.component';
import { NotificationsPageComponent } from './credit/notifications-page/notifications-page.component';
import { NotificationsListComponent } from './credit/notifications-page/notifications-list/notifications-list.component';
import { NotificationPageComponent } from './credit/notification-page/notification-page.component';
import { ChangeTermsPageComponent } from './credit/change-terms-page/change-terms-page.component';
import { SettlementComponent } from './credit/settlement/settlement.component';
import { CompanyConnectionsComponent } from './credit/customer-page/company-connections/company-connections.component';
import { MlStdConnectionsComponent } from './credit/customer-page/ml-std-connections/ml-std-connections.component';
import { MlCollateralPageComponent } from './credit/ml-collateral-page/ml-collateral-page.component';
import { DocumentsPageComponent } from './credit/documents-page/documents-page.component';
import { LegacyMlCollateralPageComponent } from './credit/legacy-ml-collateral-page/legacy-ml-collateral-page.component';
import { SearchPageComponent } from './credit/search-page/search-page.component';
import { DirectDebitPageComponent } from './credit/direct-debit-page/direct-debit-page.component';
import { CreditSearchComponent } from './credit/credit-search/credit-search.component';
import { CreditNumberEditorComponent } from './credit/credit-details-page/credit-number-editor/credit-number-editor.component';
import { MlChangeTermsPageComponent } from './credit/ml-change-terms-page/ml-change-terms-page.component';
import { MlSeAmortizationBasisComponent } from './credit/credit-details-page/ml-se-amortization-basis/ml-se-amortization-basis.component';
import { MlAmortizationPageSeComponent } from './credit/ml-amortization-se-page/ml-amortization-se-page.component';
import { MlSeLoansComponent } from './credit/ml-amortization-se-page/ml-se-loans/ml-se-loans.component';
import { MlSeLoansAmortizationComponent } from './credit/ml-amortization-se-page/ml-se-loans-amortization/ml-se-loans-amortization.component';
import { RegisterMlSePageComponent } from './register-ml-se/register-ml-se-page/register-ml-se-page.component';
import { LoannrsMlSePageComponent } from './register-ml-se/loannrs-ml-se-page/loannrs-ml-se-page.component';
import { MlSeLoanOwnerManagementComponent } from './credit/credit-details-page/ml-se-loan-owner-management/ml-se-loan-owner-management.component';
import { LoanOwnerManagementPageComponent } from './loan-owner-management/loan-owner-management-page/loan-owner-management-page.component';
import { AltPaymentplanComponent } from './credit/notifications-page/alt-paymentplan/alt-paymentplan.component';
import { MlSeAmortizationBasisLoanComponent } from './credit/credit-details-page/ml-se-amortization-basis-loan/ml-se-amortization-basis-loan.component';
import { UnplacedPaymentPageComponent } from './payments/unplaced-payment-page/unplaced-payment-page.component';
import { RepaymentComponent } from './payments/repayment/repayment.component';
import { PlaceCreditsComponent } from './payments/place-credits/place-credits.component';
import { ImportPaymentfilePageComponent } from './payments/import-paymentfile-page/import-paymentfile-page.component';
import { NotificationsComponent } from './scheduled-tasks/notifications/notifications.component';
import { TerminationLettersComponent } from './scheduled-tasks/termination-letters/termination-letters.component';
import { DailyKycScreenComponent } from './scheduled-tasks/daily-kyc-screen/daily-kyc-screen.component';
import { BookKeepingComponent } from './scheduled-tasks/book-keeping/book-keeping.component';
import { Cm1AmlExportComponent } from './scheduled-tasks/cm1-aml-export/cm1-aml-export.component';
import { SatExportComponent } from './scheduled-tasks/sat-export/sat-export.component';
import { TreasuryAmlExportComponent } from './scheduled-tasks/treasury-aml-export/treasury-aml-export.component';
import { TrapetsAmlExportComponent } from './scheduled-tasks/trapets-aml-export/trapets-aml-export.component';

@NgModule({
    declarations: [
        TerminationLetterComponent,
        DebtCollectionComponent,
        DebtCollectionScheduledTaskComponent,
        AnnualStatementsComponent,
        ChangeRatesPageComponent,
        RateEditorComponent,
        PendingChangeComponent,
        RemindersComponent,
        CreditMenuComponent,
        CreditDetailsPageComponent,
        CustomerPageComponent,
        TransactionsComponent,
        CreditAmortizationPlanComponent,
        MortgageAmortizationPlanComponent,
        CreditCommentsComponent,
        NotificationsPageComponent,
        NotificationsListComponent,
        NotificationPageComponent,
        ChangeTermsPageComponent,
        MlChangeTermsPageComponent,
        SettlementComponent,
        CompanyConnectionsComponent,
        CustomerPageComponent,
        MlStdConnectionsComponent,
        MlCollateralPageComponent,
        DocumentsPageComponent,
        LegacyMlCollateralPageComponent,
        SearchPageComponent,
        DirectDebitPageComponent,
        CreditSearchComponent,
        CreditNumberEditorComponent,
        MlSeAmortizationBasisComponent,
        MlAmortizationPageSeComponent,
        MlSeLoansComponent,
        MlSeLoansAmortizationComponent,
        RegisterMlSePageComponent,
        LoannrsMlSePageComponent,
        MlSeLoanOwnerManagementComponent,
        LoanOwnerManagementPageComponent,
        AltPaymentplanComponent,
        MlSeAmortizationBasisLoanComponent,
        UnplacedPaymentPageComponent,
        RepaymentComponent,
        PlaceCreditsComponent,
        ImportPaymentfilePageComponent,
        NotificationsComponent,
        TerminationLettersComponent,
        DailyKycScreenComponent,
        BookKeepingComponent,
        Cm1AmlExportComponent,
        SatExportComponent,
        TreasuryAmlExportComponent,
        TrapetsAmlExportComponent
    ],
    imports: [CommonModule, LoanManagementRoutingModule, FormsModule, ReactiveFormsModule, CommonComponentsModule],
})
export class LoanManagementModule {}
