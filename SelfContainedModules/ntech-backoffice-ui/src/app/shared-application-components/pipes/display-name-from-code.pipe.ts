import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'displayNameFromCode',
})
export class DisplayNameFromCodePipe implements PipeTransform {
    transform(value: string, options: DropDownOptionModel[], defaultValue?: string): string {
        if (!options) {
            return defaultValue ? defaultValue : value;
        }
        for (let option of options) {
            if (option.Code === value) {
                return option.DisplayName;
            }
        }
        return defaultValue ? defaultValue : value;
    }
}

export interface DropDownOptionModel {
    Code: string;
    DisplayName: string;
}
