import { Component, OnInit } from '@angular/core';
import { ApiKeyModel, ApiKeyService, ApiKeysInitialData } from '../api-key-service';

@Component({
    selector: 'app-list',
    templateUrl: './list.component.html',
    styles: [],
})
export class ListComponent implements OnInit {
    constructor(private keyService: ApiKeyService) {}

    public m: ListComponentModel;

    async ngOnInit(): Promise<void> {
        let keys = await this.keyService.getApiKeys();

        let m: ListComponentModel = {
            keys: keys,
            activeKeys: [],
            expiredOrRevokedKeys: [],
        };
        for (let key of keys) {
            let expirationInfo = this.keyService.getExpirationInfo(key);
            let ageText = this.keyService.getExpirationText(expirationInfo);

            (expirationInfo.code !== 'active' ? m.expiredOrRevokedKeys : m.activeKeys).push({
                id: key.id,
                description: key.description,
                ageText: ageText,
                providerName: key.providerName,
                scope: key.scopeName,
            });
        }

        if (m.keys.some((x) => !!x.providerName)) {
            let initialData: ApiKeysInitialData = await this.keyService.getInitialData();
            for (let key of m.activeKeys.concat(m.expiredOrRevokedKeys)) {
                key.providerName =
                    initialData.availableProviders.find((x) => x.providerName === key.providerName)
                        ?.providerDisplayName ?? key.providerName;
            }
        }

        this.m = m;
    }
}

class ListComponentModel {
    keys: ApiKeyModel[];
    activeKeys: KeyModel[];
    expiredOrRevokedKeys: KeyModel[];
}

class KeyModel {
    id: string;
    description: string;
    scope: string;
    ageText: string;
    providerName: string;
}
