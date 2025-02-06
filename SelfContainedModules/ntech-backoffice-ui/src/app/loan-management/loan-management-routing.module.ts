import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { creditRoutes } from './credit/credit-routes';
import { defaultManagementRoutes } from './default-management/default-management-routes';
import { fixedReferenceInterestRoutes } from './fixed-reference-interests/fixed-reference-interests-routes';
import { scheduledTasksRoutes } from './scheduled-tasks/scheduled-tasks-routes';
import { registerMlSeRoutes } from './register-ml-se/register-ml-se-routes';
import { loanOwnerManagementRoutes } from './loan-owner-management/loan-owner-management-routes';
import { paymentsRoutes } from './payments/payments-routes';

const routes: Routes = [
    ...defaultManagementRoutes,
    ...scheduledTasksRoutes,
    ...fixedReferenceInterestRoutes,
    ...loanOwnerManagementRoutes,
    ...creditRoutes,
    ...registerMlSeRoutes,
    ...paymentsRoutes
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class LoanManagementRoutingModule {}
