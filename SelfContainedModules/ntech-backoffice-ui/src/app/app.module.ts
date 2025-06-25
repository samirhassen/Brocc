import {HTTP_INTERCEPTORS, HttpClient, HttpClientModule} from '@angular/common/http';
import {
    APP_INITIALIZER,
    ApplicationRef,
    DEFAULT_CURRENCY_CODE,
    ErrorHandler,
    LOCALE_ID,
    NgModule,
    NgZone,
    Provider,
} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';
import {BrowserAnimationsModule} from '@angular/platform-browser/animations';
import {BehaviorSubject} from 'rxjs';

import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';
import {CommonComponentsModule} from './common-components/common-components.module';
import {ConfigService} from './common-services/config.service';
import {SecureMessagesModule} from './secure-messages/secure-messages.module';

import {DatePipe, DecimalPipe, getLocaleCurrencySymbol, PlatformLocation, registerLocaleData} from '@angular/common';
import localeFi from '@angular/common/locales/fi';
import localeSv from '@angular/common/locales/sv';
import {ModalModule} from 'ngx-bootstrap/modal';
import {ToastrModule} from 'ngx-toastr';
import {LoaderInterceptor} from './common-services/loader.interceptor';
import {LoginManager} from './login/login-manager';
import {LoginCompleteGuard} from './login/login-complete/login-complete.component';
import {BackTargetResolverService} from './common-services/backtarget-resolver.service';
import {ManualCreditreportsModule} from './manual-creditreports/manual-creditreports.module';
import {environment} from 'src/environments/environment';
import {ErrorHandlerService} from './common-services/error-handler.service';
import {UnsecuredLoanApplicationModule} from './unsecured-loan-application/unsecured-loan-application.module';

import {QuillModule} from 'ngx-quill';
import {UserManagementModule} from './user-management/user-management.module';
import {TestPortalModule} from './test-portal/test-portal.module';
import {PolicyfilterComponentsModule} from './policyfilter-components/policyfilter-components.module';
import {LoanManagementModule} from './loan-management/loan-management.module';
import {MortgageLoanApplicationModule} from './mortgage-loan-application/mortgage-loan-application.module';
import {SettingsModule} from './settings/settings.module';
import {SystemHealthModule} from './system-health/system-health.module';
import {LoanPolicyFiltersModule} from './loan-policyfilters/loan-policy-filters.module';
import {CustomerOverviewModule} from './customer-overview/customer-overview.module';
import {ApiKeysModule} from './api-keys/api-keys.module';
import {CustomerCheckpointsModule} from './customer-checkpoints/customer-checkpoints.module';
import {CustomerKycModule} from './customer-kyc/customer-kyc.module';
import {UlLegacyApplicationModule} from './ul-legacy-application/ul-legacy-application.module';
import {
    PositiveCreditRegisterModule
} from './ul-legacy-credit/positive-credit-register/positive-credit-register.module';

registerLocaleData(localeFi, 'fi');
registerLocaleData(localeSv, 'sv');
let selectedLocale = 'fi';

function createLocale() {
    return selectedLocale;
}

function createCurrencyCode() {
    return getLocaleCurrencySymbol(selectedLocale);
}

let loginManager = new LoginManager();

let isInitialized = new BehaviorSubject<boolean>(false);

let boostrapLoadingIndicator = document?.querySelector('#boostrapLoadingIndicator');

export function initializeConfig(configService: ConfigService, httpClient: HttpClient, platform: PlatformLocation) {
    return async () => {
        let x = await loginManager.onInit(httpClient, platform);
        if (x.wasRedirected) {
            return null;
        }
        return configService.initialize(x, (cfg) => {
            if (!cfg.isConfigLoaded()) {
                if (boostrapLoadingIndicator) {
                    boostrapLoadingIndicator.textContent = 'Failed to load configuration from server';
                }
                return;
            }
            let baseCountry = cfg.config()?.Client.BaseCountry;
            // Does not currently work since it will load the with the default value when bootstraped
            // i.e. it will not get the value from config currently.
            // Adding comment here to highlight future change. TODO
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

let providers: Provider[] = [
    ConfigService,
    {
        provide: APP_INITIALIZER,
        useFactory: initializeConfig,
        deps: [ConfigService, HttpClient, PlatformLocation], // dependancy
        multi: true,
    },
    {provide: LOCALE_ID, useFactory: createLocale, deps: [APP_INITIALIZER]},
    {provide: DEFAULT_CURRENCY_CODE, useFactory: createCurrencyCode, deps: [APP_INITIALIZER]},
    {
        provide: HTTP_INTERCEPTORS,
        useClass: LoaderInterceptor,
        multi: true,
    },
    LoginCompleteGuard,
    BackTargetResolverService,
    DecimalPipe,
    DatePipe,
];

if (!environment.disableErrorHandler) {
    providers.push({provide: ErrorHandler, useClass: ErrorHandlerService});
}

@NgModule({
    declarations: [AppComponent],
    imports: [
        BrowserModule,
        SecureMessagesModule,
        ManualCreditreportsModule,
        UnsecuredLoanApplicationModule,
        MortgageLoanApplicationModule,
        UserManagementModule,
        TestPortalModule,
        PolicyfilterComponentsModule,
        LoanManagementModule,
        SettingsModule,
        SystemHealthModule,
        LoanPolicyFiltersModule,
        CustomerOverviewModule,
        ApiKeysModule,
        CustomerCheckpointsModule,
        PositiveCreditRegisterModule,
        CustomerKycModule,
        UlLegacyApplicationModule,
        //These are order dependent. Add new feature modules above this comment.
        CommonComponentsModule,
        AppRoutingModule,
        HttpClientModule,
        ModalModule.forRoot(),
        BrowserAnimationsModule, // required for ToastrModule
        ToastrModule.forRoot(),
        QuillModule.forRoot({
            customOptions: [
                {
                    import: 'formats/font',
                    whitelist: ['mirza', 'roboto', 'aref', 'serif', 'sansserif', 'monospace'],
                },
            ],
        }),
    ],
    providers: providers,
    bootstrap: [],
})
export class AppModule {
    constructor(private ngZone: NgZone) {
    }

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
