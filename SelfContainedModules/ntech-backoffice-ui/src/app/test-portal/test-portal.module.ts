import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { TestPortalRoutingModule } from './test-portal-routing.module';
import { CreateApplicationComponent } from './pages/unsecured-standard/create-application/create-application.component';
import { NgJsonEditorModule } from 'ang-jsoneditor';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CreateMortgageLoanComponent } from './pages/mortgage-standard/create-mortgage-loan/create-mortgage-loan.component';
import { CreateMortgageLoanApplicationComponent } from './pages/mortgage-standard/create-application/create-application.component';
import { CreateMortgageloanSeComponent } from './pages/mortgage-standard/create-mortgageloan-se/create-mortgageloan-se.component';
import { ShowReportComponent } from './pages/show-report/show-report.component';
import { SingleCreditFunctionComponent } from './pages/single-credit-function/single-credit-function.component';

@NgModule({
    declarations: [CreateApplicationComponent, CreateMortgageLoanApplicationComponent, CreateMortgageLoanComponent, CreateMortgageloanSeComponent, ShowReportComponent, SingleCreditFunctionComponent],
    imports: [CommonModule, ReactiveFormsModule, TestPortalRoutingModule, FormsModule, NgJsonEditorModule],
})
export class TestPortalModule {}
