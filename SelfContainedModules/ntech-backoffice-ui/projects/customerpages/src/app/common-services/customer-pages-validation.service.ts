import { Injectable } from '@angular/core';
import { NTechValidationSharedBaseService } from 'src/app/common-services/ntech-validation-shared-base.service';
import { CustomerPagesConfigService } from './customer-pages-config.service';

@Injectable({
    providedIn: 'root',
})
export class CustomerPagesValidationService extends NTechValidationSharedBaseService {
    constructor(configService: CustomerPagesConfigService) {
        super(configService.baseCountry());
    }
}
