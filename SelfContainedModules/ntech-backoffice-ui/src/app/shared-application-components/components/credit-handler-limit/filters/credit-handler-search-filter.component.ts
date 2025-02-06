import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'creditHandlerSearchFilter',
})
export class CreditHandlerSearchFilter implements PipeTransform {
    transform(value: any[], query: string): any[] {
        if (!value) return [];
        if (!query) return value;

        return value.filter((item) => item.DisplayName.toLowerCase().indexOf(query.toLowerCase()) !== -1);
    }
}
