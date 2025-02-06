import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { ApiKeyModel, ApiKeyService } from '../api-key-service';

@Component({
    selector: 'app-key',
    templateUrl: './key.component.html',
    styles: [],
})
export class KeyComponent implements OnInit {
    constructor(private route: ActivatedRoute, private apiKeyService: ApiKeyService, private toastr: ToastrService) {}

    m: KeyComponentModel;

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('keyId'));
        });
    }

    async reload(keyId: string) {
        this.m = null;

        let initialData = await this.apiKeyService.getInitialData();
        let apiKey = await this.apiKeyService.getApiKey(keyId);

        let scope = initialData.availableScopes.find((x) => x.name === apiKey.scopeName);
        let provider = initialData.availableProviders.find((x) => x.providerName === apiKey.providerName);

        if (!apiKey) {
            this.toastr.warning('No such key exists');
            return;
        }

        let expirationInfo = this.apiKeyService.getExpirationInfo(apiKey);
        this.m = {
            key: apiKey,
            providerDisplayName: provider?.providerDisplayName ?? apiKey.providerName,
            scopeDisplayName: scope?.displayName ?? apiKey.scopeName,
            ageText: this.apiKeyService.getExpirationText(expirationInfo),
            isKeyActive: expirationInfo.code === 'active',
            hasConsentedToRevoke: false,
        };
    }

    onAllowRevokeChanged(evt?: Event) {
        this.m.hasConsentedToRevoke = (evt.currentTarget as any).checked === true;
    }

    async revoke(evt?: Event) {
        evt?.preventDefault();

        let keyId = this.m.key.id;

        let { wasRevoked } = await this.apiKeyService.revoke(keyId);
        if (wasRevoked) {
            this.reload(keyId);
        } else {
            this.toastr.warning('Key was not revoked');
        }
    }

    async testAuthentication(apiKey: string, callingIpAddress: string, evt?: Event) {
        let result = await this.apiKeyService.authenticate({
            rawApiKey: apiKey,
            authenticationScope: this.m.key.scopeName,
            callerIpAddress: callingIpAddress,
        });
        if (result.isAuthenticated) {
            if (result.authenticatedKeyModel.id === this.m.key.id) {
                this.toastr.info('Accepted');
            } else {
                this.toastr.warning('Accepted but as a different key with id ' + result.authenticatedKeyModel.id);
            }
        } else {
            this.toastr.info('Rejected - ' + result.failedAuthenticationReason);
        }
    }
}

class KeyComponentModel {
    key: ApiKeyModel;
    ageText: string;
    isKeyActive: boolean;
    hasConsentedToRevoke: boolean;
    providerDisplayName: string;
    scopeDisplayName: string;
}
