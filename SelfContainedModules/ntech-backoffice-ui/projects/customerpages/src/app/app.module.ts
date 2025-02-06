import { getLocaleCurrencySymbol, PlatformLocation, registerLocaleData } from '@angular/common';
import { HttpClient, HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { ApplicationRef, APP_INITIALIZER, DEFAULT_CURRENCY_CODE, LOCALE_ID, NgModule, NgZone } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import localeFi from '@angular/common/locales/fi';
import localeSv from '@angular/common/locales/sv';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { UnsecuredLoanApplicationsModule } from './unsecured-loan-applications/unsecured-loan-applications.module';
import { BehaviorSubject } from 'rxjs';
import { CustomerPagesConfigService } from './common-services/customer-pages-config.service';
import { CustomerPagesLoaderInterceptor } from './common-services/customerpages.loader.interceptor';
import { MortgageLoanApplicationsModule } from './mortgage-loan-applications/mortgage-loan-applications.module';
import { MypagesModule } from './mypages/mypages.module';
import { SharedComponentsModule } from './shared-components/shared-components.module';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ToastrModule } from 'ngx-toastr';
import { ModalModule } from 'ngx-bootstrap/modal';
import { KycQuestionsModule } from './kyc-questions/kyc-questions.module';

registerLocaleData(localeFi, 'fi');
registerLocaleData(localeSv, 'sv');
let selectedLocale = 'sv';

function createCurrencyCode() {
    return getLocaleCurrencySymbol(selectedLocale);
}

let isInitialized = new BehaviorSubject<boolean>(false);

let boostrapLoadingIndicator = document?.querySelector('#boostrapLoadingIndicator');
export function initializeConfig(
    configService: CustomerPagesConfigService,
    httpClient: HttpClient,
    platform: PlatformLocation
) {
    return () => {
        configService.initialize((cfg) => {
            if (!cfg.isConfigLoaded()) {
                if (boostrapLoadingIndicator) {
                    boostrapLoadingIndicator.textContent = 'Failed to load configuration from server';
                }
                return;
            }
            let baseCountry = cfg.config()?.Client.BaseCountry;
            if (baseCountry == 'SE') {
                selectedLocale = 'sv';
            } else if (baseCountry == 'FI') {
                selectedLocale = 'fi';
            }

            isInitialized.next(true);
        });
    };
}

let wasBootstrapped = false;

@NgModule({
    declarations: [AppComponent],
    imports: [
        SharedComponentsModule,
        UnsecuredLoanApplicationsModule,
        MortgageLoanApplicationsModule,
        MypagesModule,
        KycQuestionsModule,
        BrowserModule,
        AppRoutingModule,
        HttpClientModule,
        BrowserAnimationsModule, // required for ToastrModule
        ToastrModule.forRoot(),
        ModalModule.forRoot(),
    ],
    providers: [
        {
            provide: APP_INITIALIZER,
            useFactory: initializeConfig,
            deps: [CustomerPagesConfigService, HttpClient, PlatformLocation], // dependancy
            multi: true,
        },
        { provide: LOCALE_ID, useValue: selectedLocale },
        { provide: DEFAULT_CURRENCY_CODE, useFactory: createCurrencyCode, deps: [APP_INITIALIZER] },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: CustomerPagesLoaderInterceptor,
            multi: true,
        },
    ],
    bootstrap: [],
})
export class AppModule {
    constructor(private ngZone: NgZone) {}
    ngDoBootstrap(app: ApplicationRef) {
        isInitialized.subscribe((x) => {
            this.ngZone.run(() => {
                if (x && !wasBootstrapped) {
                    wasBootstrapped = true;
                    if (boostrapLoadingIndicator) {
                        boostrapLoadingIndicator.remove();
                    }
                    app.bootstrap(AppComponent);
                }
            });
        });
    }
}
