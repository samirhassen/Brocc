import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { ConfigService } from 'src/app/common-services/config.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { ApiKeyProviderModel, ApiKeyScopeModel, ApiKeyService } from '../api-key-service';

@Component({
    selector: 'app-generate',
    templateUrl: './generate.component.html',
    styles: [],
})
export class GenerateComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private apiKeyService: ApiKeyService,
        private validationService: NTechValidationService,
        private toastr: ToastrService,
        private configService: ConfigService
    ) {}

    public m: GenerateComponentModel;

    async ngOnInit(): Promise<void> {
        await this.reload();
    }

    async reload() {
        if (this.m) {
            for (let sub of this.m.subscriptions) {
                sub.unsubscribe();
            }
        }
        this.m = null;

        let initialData = await this.apiKeyService.getInitialData();

        initialData.availableScopes;

        let subs: Subscription[] = [];

        let ipValidatorExpression = new RegExp('^[0-9:][0-9.,s]*[0-9]$', 'i');
        let fh = new FormsHelper(
            this.fb.group({
                scopeName: ['', [Validators.required]],
                providerName: ['', []],
                description: ['', [Validators.required]],
                expirationDays: ['', [this.validationService.getPositiveIntegerValidator()]],
                ipAddressFilter: [
                    '',
                    [
                        this.validationService.getValidator('ipAddressFilterValidator', (x) =>
                            ipValidatorExpression.test(x)
                        ),
                    ],
                ],
            })
        );

        fh.setFormValidator((ctrl) => {
            if (fh.invalid()) {
                //Handled by the individual controls
                return true;
            }

            let scopeModel = this.selectedScope();
            if (scopeModel && scopeModel.isConnectedToProvider) {
                let providerModel = this.selectedProvider();
                if (!providerModel) {
                    return false;
                }
            }

            return true;
        });

        this.m = {
            form: fh,
            availableScopes: initialData.availableScopes,
            availableProviders: initialData.availableProviders,
            subscriptions: subs,
            isIpFilterHelpVisible: false,
            generatedKey: null,
        };
    }

    selectedScope(): ApiKeyScopeModel {
        return !this.m ? null : this.m.availableScopes.find((x) => x.name === this.m.form.getValue('scopeName'));
    }

    selectedProvider(): ApiKeyProviderModel {
        if (!this.m) {
            return null;
        }
        let scope = this.selectedScope();
        if (!scope || !scope.isConnectedToProvider) {
            return null;
        }
        return this.m.availableProviders.find((x) => x.providerName === this.m.form.getValue('providerName'));
    }

    isProviderAndScopeSelected() {
        let scope = this.selectedScope();
        return scope && (!scope.isConnectedToProvider || this.selectedProvider());
    }

    updateDescription() {
        let scope = this.selectedScope();
        let provider = this.selectedProvider();
        if (!scope || (scope.isConnectedToProvider && !provider)) {
            this.m.form.setValue('description', '');
        } else {
            let text = scope.displayName;
            if (scope.isConnectedToProvider) {
                text += ` for ${provider.providerDisplayName}`;
            }
            text += ` ${this.configService.getCurrentDateAndTime().format('YYYY-MM-DD')}`;
            this.m.form.setValue('description', text);
        }
    }

    toggleIpAddressFilterHelp(evt?: Event) {
        evt?.preventDefault();
        this.m.isIpFilterHelpVisible = !this.m.isIpFilterHelpVisible;
    }

    async generateKey(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.form;

        let expiresAfterDaysRaw = f.getValue('expirationDays');

        try {
            let result = await this.apiKeyService.createApiKey({
                scopeName: this.selectedScope().name,
                description: f.getValue('description'),
                providerName: this.selectedProvider()?.providerName,
                expiresAfterDays: expiresAfterDaysRaw ? parseInt(expiresAfterDaysRaw) : null,
                ipAddressFilter: f.getValue('ipAddressFilter'),
            });

            this.m.generatedKey = result.rawApiKey;
        } catch (error: any) {
            let httpError = error as HttpErrorResponse;
            if (httpError?.status === 400) {
                this.toastr.error(httpError.error.errorMessage);
            } else {
                this.toastr.error('Failed');
            }
        }
    }

    copyKey(evt?: Event) {
        evt?.preventDefault();
        navigator.clipboard
            .writeText(this.m.generatedKey)
            .then(() => {
                this.toastr.info('Key copied to clipboard');
            })
            .catch((e) => {
                this.toastr.error('Failed to copy the key to the clipboard. Copy it directly instead.');
            });
    }
}

class GenerateComponentModel {
    form: FormsHelper;
    availableScopes: ApiKeyScopeModel[];
    availableProviders: ApiKeyProviderModel[];
    subscriptions: Subscription[];
    isIpFilterHelpVisible: boolean;
    generatedKey: string;
}
