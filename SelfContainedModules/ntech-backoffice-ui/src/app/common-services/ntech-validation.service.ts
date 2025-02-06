import { Injectable } from '@angular/core';
import { ConfigService } from './config.service';
import { NTechValidationSharedBaseService } from './ntech-validation-shared-base.service';

@Injectable({
    providedIn: 'root',
})
export class NTechValidationService extends NTechValidationSharedBaseService {
    constructor(configService: ConfigService) {
        super(configService.baseCountry());
    }
}
