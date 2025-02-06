import * as moment from 'moment';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';

//TODO: This should be in validationService but we havent decided
//      yet how to deal with localisation for dates. The user function is migrated as is though and it uses yyyy-mm-dd hardcoded.
export function getYearMonthDayDateValidator(validationService: NTechValidationService, validatorName?: string) {
    return validationService.getValidator(validatorName || 'date', (input) =>
        parseYearMonthDayDate(input, true).isValid()
    );
}

export function parseYearMonthDayDate(input: string, allowInvalid?: boolean) {
    let m = moment(input, 'YYYY-MM-DD', true);
    if (!allowInvalid && !m.isValid()) {
        throw new Error('Invalid date. Expected format YYYY-MM-DD');
    }
    return m;
}
