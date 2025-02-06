import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'join',
})
export class JoinPipe implements PipeTransform {
    /**
     * Ex. HouseHildChildren | join:', ':'AgeInYears
     * Above will yield a commaseparated list with the parameters AgeInYears
     * @param input an array with ex. strings, or objects
     * @param separator the separator, ex ', '
     * @param parameterName if array of objects, the name of the parameter to print, ex. 'AgeInYears'
     * @returns
     */
    transform(input: Array<any>, separator: string, parameterName?: string): any {
        if (parameterName) return input.map((obj) => obj[parameterName]).join(separator);

        return input.join(separator);
    }
}
