import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { ConfigService } from 'src/app/common-services/config.service';
import { AbstractPermissionsGuard } from './abstract-permissions-guard';

@Injectable({
    providedIn: 'root',
})
export class AdminPermissionsGuard extends AbstractPermissionsGuard {
    constructor(configService: ConfigService, router: Router) {
        super(configService, router);
    }

    getAllowedProductsOrNullForAll(): string[] {
        return null;
    }
    getAllowedGroupNamesOrNullForAll(): string[] {
        return ['Admin'];
    }
    getAllowedRolesNamesOrNullForAll(): string[] {
        return null;
    }
}
