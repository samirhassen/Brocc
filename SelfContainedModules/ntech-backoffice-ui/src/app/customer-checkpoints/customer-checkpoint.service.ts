import { Injectable } from "@angular/core";
import { ConfigService } from "../common-services/config.service";
import { NtechApiService } from "../common-services/ntech-api.service";

export const CreditApplicationCheckpointCode = 'CreditApplicationCheckpoint';
export const SavingsAccountCreationRemarkCode = 'SavingsAccountCreationRemark';
export const SavingsAccountBlockTransactionsCode = 'SavingsAccountBlockTransactions';

@Injectable({
    providedIn: 'root',
})
export class CustomerCheckpointService {
    constructor(private apiService: NtechApiService, private config: ConfigService) { }
            
    async fetchStateAndHistoryForCustomer(customerId: number) {
        return this.apiService.post<FetchStateAndHistoryForCustomerResult>(
            'NTechHost', 'Api/Customer/Checkpoint/Get-State-And-History-On-Customer',
            { customerId }, { forceCamelCase: true })
    }

    fetchReasonText(checkpointId: number) {
        return this.apiService.post<{ reasonText: string }>(
            'NTechHost', 'Api/Customer/Checkpoint/Fetch-ReasonText',
            { checkpointId }, { forceCamelCase: true })
    }

    setCheckpointState(customerId: number, codes: string[], reasonText: string) {
        return this.apiService.post<{ Id: number }>('NTechHost', 'Api/Customer/Checkpoint/Set-State-On-Customer', {
            customerId, 
            codes: codes,
            reasonText
        }, { forceCamelCase: true })
    }

    getActiveCheckpointCodes() : CheckpointCode[] {
        let serviceRegistry = this.config.getServiceRegistry();
        let codes : CheckpointCode[] = [];
        if(serviceRegistry.containsService('nPreCredit')) {
            codes.push({
                code: CreditApplicationCheckpointCode,
                displayName: 'Credit application checkpoint'
            });
        }        
        if(serviceRegistry.containsService('nSavings')) {
            codes.push({
                code: SavingsAccountCreationRemarkCode,
                displayName: 'Savings account creation remark'
            });
            codes.push({
                code: SavingsAccountBlockTransactionsCode,
                displayName: 'Block savings transactions'
            });
        }
        return codes;
    }
}


export interface FetchStateAndHistoryForCustomerResult {
    customerId: number,
    currentState: StateModel,
    historyStates: StateModel[]
}

export interface StateModel {
    id: number
    codes: string[]
    stateBy: number
    stateDate: string
}

export interface CheckpointCode {
    code: string
    displayName: string
}