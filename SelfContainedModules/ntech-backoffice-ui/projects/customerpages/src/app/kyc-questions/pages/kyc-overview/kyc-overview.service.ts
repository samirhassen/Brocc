import { Injectable } from "@angular/core";
import { KycCustomerStatus, KycQuestionAnswerModel } from "projects/ntech-components/src/public-api";
import { CustomerPagesApiService } from "../../../common-services/customer-pages-api.service";

@Injectable({
    providedIn: 'root',
})
export class KycOverviewService {
    constructor(private apiService: CustomerPagesApiService) {

    }

    public async getCustomerStatus() {
        return await this.apiService.post<KycCustomerStatus>('NTechHost', 'Api/Customer/KycQuestionUpdate/GetCustomerStatus', {});
    }

    public async updateAnswers(request: {
        relationType: string,
        relationId: string,
        answers: KycQuestionAnswerModel[]
    }) {
        return await this.apiService.post<KycCustomerStatus>('NTechHost', 'Api/Customer/KycQuestionUpdate/UpdateAnswers', request);
    }
}



