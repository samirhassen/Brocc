import { BrowserModule } from '@angular/platform-browser';
import { NgModule, LOCALE_ID, APP_INITIALIZER } from '@angular/core';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { StartApplicationComponent } from './application-steps/start-application/start-application.component';
import { environment } from '../environments/environment';
import { API_SERVICE } from './backend/api-service';
import { StandaloneTestApiService } from './backend/standalone-test-api.service';
import { RealApiService } from './backend/real-api.service';
import { CalculatorComponent } from './application-steps/calculator/calculator.component';
import { StorageServiceModule } from 'ngx-webstorage-service';
import { CampaignCodeComponent } from './application-steps/campaign-code/campaign-code.component';
import { RequireApplicationGuard } from './guards/require-application.guard';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { NrOfApplicantsComponent } from './application-steps/nr-of-applicants/nr-of-applicants.component';
import { CalculatorFooterComponent } from './application-steps/calculator/calculator-footer.component';
import { ForwardBackButtonsComponent } from './forward-back-buttons/forward-back-buttons.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

//https://github.com/ngx-translate/core#installation
import {TranslateModule, TranslateLoader} from '@ngx-translate/core';
import {TranslateHttpLoader} from '@ngx-translate/http-loader'; 
export function HttpLoaderFactory(http: HttpClient) {
    return new TranslateHttpLoader(http, './assets/i18n/');
}

import * as contentFi from '../assets/i18n/fi.json';
import * as contentSv from '../assets/i18n/sv.json';

const TRANSLATIONS = {
  sv: contentSv,
  fi: contentFi
};

export class TranslateUniversalLoader implements TranslateLoader {
  getTranslation(lang: string): Observable<any> {
    return of(TRANSLATIONS[lang].default);
  }
}

//Builtin, used to format date and currency pipes and similar
import { registerLocaleData } from '@angular/common';
import localeFi from '@angular/common/locales/fi';
import { CampaignCodeCodeComponent } from './application-steps/campaign-code-code/campaign-code-code.component';
import { CampaignCodeChannelComponent } from './application-steps/campaign-code-channel/campaign-code-channel.component';
import { SsnComponent } from './application-steps/ssn/ssn.component';
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
import { ResultSuccessComponent } from './result-success/result-success.component';
import { ResultFailedComponent } from './result-failed/result-failed.component';
import { ApplicationTemplateComponent } from './application-template/application-template.component';
import { ConfigService } from './backend/config.service';
import { Observable, of } from 'rxjs';

registerLocaleData(localeFi, 'fi')

export function initializeConfig(configService: ConfigService) {
    return () => 
        configService.initialize()
}

@NgModule({
  declarations: [
    AppComponent,
    NotFoundComponent,
    StartApplicationComponent,
    CalculatorComponent,
    CampaignCodeComponent,
    NrOfApplicantsComponent,
    CalculatorFooterComponent,
    ForwardBackButtonsComponent,
    CampaignCodeCodeComponent,
    CampaignCodeChannelComponent,
    SsnComponent,
    ConsentComponent,
    EmailComponent,
    PhoneComponent,
    HousingComponent,
    CostOfLivingComponent,
    MarriageComponent,
    NrOfChildrenComponent,
    EmploymentComponent,
    EmploymentDetailsComponent,
    IncomeComponent,
    HasOtherLoansComponent,
    OtherLoansOptionsComponent,
    OtherLoansAmountComponent,
    ConsolidationOptionComponent,
    ConsolidationAmountComponent,
    ResultSuccessComponent,
    ResultFailedComponent,
    ApplicationTemplateComponent
  ],
  imports: [
    BrowserModule,    
    environment.production 
        ? TranslateModule.forRoot({
            loader: {
                provide: TranslateLoader,
                useClass: TranslateUniversalLoader
            }
        })
        : TranslateModule.forRoot({
                loader: {
                    provide: TranslateLoader,
                    useFactory: HttpLoaderFactory,
                    deps: [HttpClient]
                }
            }),
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    StorageServiceModule,
    HttpClientModule
  ],
  providers: [
    { provide: API_SERVICE, useClass: environment.useMockApi ? StandaloneTestApiService : RealApiService }, 
    ConfigService,
    {
      provide: APP_INITIALIZER,
      useFactory: initializeConfig,
      deps: [ConfigService], // dependancy
      multi: true
    },
    RequireApplicationGuard,
    { provide: LOCALE_ID, useValue: 'fi' }],
  bootstrap: [AppComponent]
})
export class AppModule { }
