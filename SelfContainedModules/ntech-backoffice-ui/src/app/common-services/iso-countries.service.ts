import { Injectable } from "@angular/core";
import { IsoCountry } from "projects/ntech-components/src/public-api";
import { NtechApiService } from "./ntech-api.service";

@Injectable({
    providedIn: 'root',
})
export class IsoCountriesService {
    private isoCountries: IsoCountry[] = null;

    constructor(private apiService: NtechApiService) {
        
    }

    public async getIsoCountries(): Promise<IsoCountry[]> {
        if(this.isoCountries) {
            return new Promise<IsoCountry[]>(resolve => resolve(this.isoCountries));
        } else {
            this.isoCountries = await this.apiService.post<IsoCountry[]>('NTechHost', 'Api/IsoCountries', {});
            return this.isoCountries;
        }        
    }
}

export function normalizeIsoCountryCode(value: string) {
    return (value ?? '').toUpperCase();
}

export function isValidTwoLetterIsoCountryCode(value: string, isoCountries: IsoCountry[]) {
    return isoCountries.findIndex(x => x.iso2Name === normalizeIsoCountryCode(value)) >= 0;
}

