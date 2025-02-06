import { Injectable } from "@angular/core";
import { KycQuestionsSet } from "projects/ntech-components/src/public-api";
import { NtechApiService } from "src/app/common-services/ntech-api.service";

@Injectable({
    providedIn: 'root',
})
export class KycTemplateService {
    constructor(private apiService: NtechApiService) {

    }

    public getAllKycQuestionTemplates(): Promise<GetAllKycTemplatesResponse> {
        return this.apiService.post('NTechHost', 'Api/Customer/Kyc/QuestionTemplates/Get-All', {});
    }

    public setKycQuestionTemplate(relationType: string, modelData: string): Promise<{
        id: string
        version: string
    }> {
        return this.apiService.post('NTechHost', 'Api/Customer/Kyc/QuestionTemplates/Set', {
            relationType, modelData
        });
    }

    public getKycQuestionTemplateModelData(id: number): Promise<{ modelData: string }> {
        return this.apiService.post('NTechHost', 'Api/Customer/Kyc/QuestionTemplates/Get-ModelData', { id });
    }

    public validateKycQuestionTemplateModelData(modelData: string): Promise<{ 
        isValid: boolean
        validationErrorMessage: string 
    }> {
        return this.apiService.post('NTechHost', 'Api/Customer/Kyc/QuestionTemplates/Validate-Template', { modelData });
    }
}

export interface GetAllKycTemplatesResponse {
    activeProducts: {
        relationType: string
        currentQuestionsTemplate: KycQuestionsSet
        historicalModels: {
            id: number
            date: string
        }[]
    }[]
}