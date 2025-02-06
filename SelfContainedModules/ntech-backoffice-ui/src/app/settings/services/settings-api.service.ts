import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary } from 'src/app/common.types';

@Injectable({
    providedIn: 'root',
})
export class SettingsApiService {
    constructor(public apiService: NtechApiService) {}

    public saveSettingValuesWithValidation(settingCode: string, newValues: Dictionary<string>): Promise<{ IsSaved: boolean, ValidationErrors: string[] }> {
        return this.apiService.post('nCustomer', 'api/Settings/SaveValues', { settingCode, settingValues: newValues });
    }

    public async saveSettingValues(settingCode: string, newValues: Dictionary<string>): Promise<void> {
        let result = await this.saveSettingValuesWithValidation(settingCode, newValues);
        if(!result.IsSaved) {
            throw new Error(result.ValidationErrors.join(", "));
        }
    }
}
