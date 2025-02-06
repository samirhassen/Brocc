import { Injectable } from "@angular/core";
import { NtechApiService } from "src/app/common-services/ntech-api.service";
import { ConfigService } from "./config.service";

@Injectable({
    providedIn: 'root',
})
export class PaymentOrderService {
    constructor(private apiService: NtechApiService, private configService: ConfigService) {

    }

    public getPaymentOrderUiItems() {
        return this.apiService.post<PaymentOrderUiItem[]>('NTechHost', 'Api/Credit/PaymentOrder/UiItems', {});
    }

    public async getCustomCosts()  {
        if(!this.configService.isFeatureEnabled('ntech.feature.customcosts')) {
            return [];
        }
        return await this.apiService.post<CustomCostItem[]>('NTechHost', 'Api/Credit/CustomCosts/All', {});
    }
}

export interface PaymentOrderUiItem {
    text: string
    uniqueId: string
    orderItem: PaymentOrderItem
}

export interface PaymentOrderItem {
    code: string
    isBuiltin: boolean
}

export interface CustomCostItem {
    text: string
    code: string
}