import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { StartApplicationComponent } from './application-steps/start-application/start-application.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { CalculatorComponent } from './application-steps/calculator/calculator.component';
import { environment } from '../environments/environment';
import { RequireApplicationGuard } from './guards/require-application.guard';
import { CampaignCodeComponent } from './application-steps/campaign-code/campaign-code.component';
import { NrOfApplicantsComponent } from './application-steps/nr-of-applicants/nr-of-applicants.component';
import { CampaignCodeCodeComponent } from './application-steps/campaign-code-code/campaign-code-code.component';
import { CampaignCodeChannelComponent } from './application-steps/campaign-code-channel/campaign-code-channel.component';
import { SsnComponent } from './application-steps/ssn/ssn.component';
import { RequireApplicantNrGuard } from './guards/require-applicantnr.guard';
import { ConsentComponent } from './application-steps/consent/consent.component';
import { EmailComponent } from './application-steps/email/email.component';
import { PhoneComponent } from './application-steps/phone/phone.component';
import { HousingComponent } from './application-steps/housing/housing.component';
import { CostOfLivingComponent } from './application-steps/cost-of-living/cost-of-living.component';
import { MarriageComponent } from './application-steps/marriage/marriage.component';
import { NrOfChildrenComponent } from './application-steps/nr-of-children/nr-of-children.component';
import { EmploymentComponent } from './application-steps/employment/employment.component';
import { EmploymentDetailsComponent } from './application-steps/employment-details/employment-details.component';
import { IncomeComponent } from './application-steps/income/income.component';
import { HasOtherLoansComponent } from './application-steps/has-other-loans/has-other-loans.component';
import { OtherLoansOptionsComponent } from './application-steps/other-loans-options/other-loans-options.component';
import { OtherLoansAmountComponent } from './application-steps/other-loans-amount/other-loans-amount.component';
import { ConsolidationOptionComponent } from './application-steps/consolidation-option/consolidation-option.component';
import { ConsolidationAmountComponent } from './application-steps/consolidation-amount/consolidation-amount.component';
import { RequireApplicationNrGuard } from './guards/require-applicationnr';
import { ResultSuccessComponent } from './result-success/result-success.component';
import { ResultFailedComponent } from './result-failed/result-failed.component';
import { RequireUserLanguageGuard } from './guards/require-userlanguage.guard';
  
const routes: Routes = [  
  { path: 'start-application', component: StartApplicationComponent }, 
  { path: '', pathMatch: 'full', component: StartApplicationComponent },
  { path: 'not-found', component: NotFoundComponent },
  { path: 'app/:id/calculator', component: CalculatorComponent, resolve : { application: RequireApplicationGuard }, data: { isCalculator: true } },
  { path: 'app/:id/campaign-code', component: CampaignCodeComponent, resolve : { application: RequireApplicationGuard } },
  { path: 'app/:id/nr-of-applicants', component: NrOfApplicantsComponent, resolve : { application: RequireApplicationGuard } },
  { path: 'app/:id/campaign-code-code', component: CampaignCodeCodeComponent, resolve : { application: RequireApplicationGuard } },
  { path: 'app/:id/campaign-code-channel', component: CampaignCodeChannelComponent, resolve : { application: RequireApplicationGuard } },
  { path: 'app/:id/:applicantNr/ssn', component: SsnComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/email', component: EmailComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/phone', component: PhoneComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/housing', component: HousingComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/cost-of-living', component: CostOfLivingComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/marriage', component: MarriageComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/nr-of-children', component: NrOfChildrenComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/employment', component: EmploymentComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },  
  { path: 'app/:id/:applicantNr/employment-details', component: EmploymentDetailsComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },  
  { path: 'app/:id/:applicantNr/income', component: IncomeComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/has-other-loans', component: HasOtherLoansComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/other-loans-options', component: OtherLoansOptionsComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard } },
  { path: 'app/:id/:applicantNr/other-loans-amount/mortgageLoan', component: OtherLoansAmountComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard }, data: { loanType: 'mortgageLoan' } },
  { path: 'app/:id/:applicantNr/other-loans-amount/carOrBoatLoan', component: OtherLoansAmountComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard }, data: { loanType: 'carOrBoatLoan' } },
  { path: 'app/:id/:applicantNr/other-loans-amount/creditCard', component: OtherLoansAmountComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard }, data: { loanType: 'creditCard' } },  
  { path: 'app/:id/:applicantNr/other-loans-amount/otherLoan', component: OtherLoansAmountComponent, resolve : { application: RequireApplicationGuard, applicantNr: RequireApplicantNrGuard }, data: { loanType: 'otherLoan' } },
  { path: 'app/:id/consolidation-option', component: ConsolidationOptionComponent, resolve : { application: RequireApplicationGuard } },
  { path: 'app/:id/consolidation-amount', component: ConsolidationAmountComponent, resolve : { application: RequireApplicationGuard } },
  { path: 'app/:applicationNr/result-success', component: ResultSuccessComponent, resolve : { applicationNr: RequireApplicationNrGuard }, data: { isIdependent: true } },
  { path: 'app/result-failed', component: ResultFailedComponent, data: { isIdependent: true } },
  { path: 'app/:id/consent', component: ConsentComponent, resolve : { application: RequireApplicationGuard, userLanguage: RequireUserLanguageGuard }, data: { isFinalStep: true } },
  { path: '**', redirectTo: '/not-found', data: { isIdependent: true } }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true, enableTracing: !environment.production })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
