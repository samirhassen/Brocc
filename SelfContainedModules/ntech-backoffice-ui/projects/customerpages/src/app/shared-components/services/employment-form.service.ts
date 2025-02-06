import { Injectable } from "@angular/core";
import { CustomerPagesConfigService } from "../../common-services/customer-pages-config.service";
import { CustomerPagesValidationService } from "../../common-services/customer-pages-validation.service";

@Injectable({
    providedIn: 'root',
})
export class EmploymentFormService {
    constructor(private config: CustomerPagesConfigService, private validationService: CustomerPagesValidationService) {
        
    }

    public getEmploymentStatuses() {
        return this.config.getEnums().EmploymentStatuses;
    }
    
    public isEmployerEmploymentCode(code: string) {
        let codes = [
            'full_time',
            'project_employee',
            'hourly_employment',
            'part_time',
            'probationary',
            'self_employed',
            'substitute',
        ];
        return codes.indexOf(code) >= 0;
    }

    public isEmployedSinceEmploymentCode(code: string) {
        return !(['unemployed'].indexOf(code) >= 0 || this.validationService.isNullOrWhitespace(code));
    }

    public isEmployedToEmploymentCode(code: string) {
        return ['project_employee', 'probationary'].indexOf(code) >= 0;
    }
}