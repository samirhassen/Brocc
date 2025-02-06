import { Injectable } from '@angular/core';
import { KycCustomerStatus, KycQuestionAnswerModel } from 'projects/ntech-components/src/public-api';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Injectable({
    providedIn: 'root',
})
export class KycQuestionsService {
    constructor(private apiService: NtechApiService) {}

    public async getCustomerStatus(customerId: number) {
        return await this.apiService.post<KycCustomerStatus>('NTechHost', 'Api/Customer/KycQuestionUpdate/GetCustomerStatus', {
            customerId: customerId,
        });
    }

    public async updateAnswers(request: {
        relationType: string;
        relationId: string;
        answers: KycQuestionAnswerModel[];
        customerId: number;
    }) {
        return await this.apiService.post<KycCustomerStatus>('NTechHost', 'Api/Customer/KycQuestionUpdate/UpdateAnswers', request);
    }
}
