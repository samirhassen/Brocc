import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { DateOnly, NumberDictionary } from 'src/app/common.types';

@Injectable({
    providedIn: 'root',
})
export class CustomerInfoService {
    constructor(private apiService: NtechApiService) {}

    fetchCustomerItems(customerId: number, itemNames: string[]) {
        return this.apiService.shared.fetchCustomerItems(customerId, itemNames);
    }

    fetchCustomerComponentInitialData(
        applicationNr: string,
        applicantNr: number,
        backTarget: string
    ): Promise<CustomerComponentInitialDataResult> {
        return this.apiService.post('nPreCredit', '/api/CustomerInfoComponent/FetchInitial', {
            applicationNr: applicationNr,
            applicantNr: applicantNr,
            backTarget: backTarget,
        });
    }

    fetchCustomerComponentInitialDataByItemCompoundName(
        applicationNr: string,
        customerIdApplicationItemCompoundName: string,
        customerBirthDateApplicationItemCompoundName?: string,
        backTarget?: string
    ): Promise<CustomerComponentInitialDataResult> {
        return this.apiService.post('nPreCredit', '/api/CustomerInfoComponent/FetchInitialByItemName', {
            applicationNr: applicationNr,
            customerIdApplicationItemCompoundName: customerIdApplicationItemCompoundName,
            customerBirthDateApplicationItemCompoundName: customerBirthDateApplicationItemCompoundName,
            backTarget: backTarget,
        });
    }

    async fetchCustomerComponentInitialDataByCustomerId(
        customerId: number,
        backTarget: string
    ): Promise<CustomerComponentInitialDataResult> {
        let r = await this.fetchCustomerComponentInitialDataByCustomerIdBulk([customerId], backTarget);
        return r[customerId];
    }

    async fetchCustomerComponentInitialDataByCustomerIdBulk(
        customerIds: number[],
        backTarget: string
    ): Promise<NumberDictionary<CustomerComponentInitialDataResult | null>> {
        let result: NumberDictionary<CustomerComponentInitialDataResult> = {};

        let fetchResult = await this.apiService.shared.fetchCustomerItemsBulkAsNumberDictionary(customerIds, [
            'firstName',
            'addressZipcode',
            'email',
            'sanction',
            'birthDate',
            'wasOnboardedExternally',
            'includeInFatcaExport',
            'companyName',
            'isCompany',
            'localIsPep',
            'localIsSanction',
        ]);

        let parseTriStateBoolean = (x: string) => (x === 'true' ? true : x === 'false' ? false : null);

        let getCustomerCardUrl = (customerId: number, forceLegacyUi: boolean) =>
            this.apiService.getUiGatewayUrl('nCustomer', 'Customer/CustomerCard', [
                ['customerId', customerId.toString()],
                ['backTarget', backTarget],
                ['forceLegacyUi', forceLegacyUi ? 'true' : null],
            ]);

        for (let customerId of customerIds) {
            let { properties } = fetchResult[customerId];
            if (properties) {
                let customerResult: CustomerComponentInitialDataResult = {
                    customerId: customerId,
                    firstName: properties['firstName'],
                    isSanctionRejected: properties['sanction'] == 'true',
                    wasOnboardedExternally: properties['wasOnboardedExternally'] == 'true',
                    includeInFatcaExport: parseTriStateBoolean('includeInFatcaExport'),
                    customerCardUrl: getCustomerCardUrl(customerId, false),
                    legacyCustomerCardUrl: getCustomerCardUrl(customerId, true),
                    customerFatcaCrsUrl: this.apiService.getUiGatewayUrl('nCustomer', 'Ui/KycManagement/FatcaCrs', [
                        ['customerId', customerId.toString()],
                        ['backTarget', backTarget],
                    ]),
                    birthDate: properties['birthDate'],
                    isMissingAddress: !properties['addressZipcode'],
                    isMissingEmail: !properties['email'],
                    isCompany: properties['isCompany'] == 'true',
                    companyName: properties['companyName'],
                    pepKycCustomerUrl: this.apiService.getUiGatewayUrl('nCustomer', 'Ui/KycManagement/Manage', [
                        ['customerId', customerId.toString()],
                        ['backTarget', backTarget],
                    ]),
                    localIsPep: parseTriStateBoolean('localIsPep'),
                    localIsSanction: parseTriStateBoolean('localIsSanction'),
                };
                result[customerId] = customerResult;
            } else {
                result[customerId] = null;
            }
        }

        return result;
    }

    getCustomerCardUrl(customerId: number, backTarget?: string) {
        return this.apiService.getUiGatewayUrl('nCustomer', 'Customer/CustomerCard', [
            ['customerId', customerId.toString()],
            ['backTarget', backTarget],
        ]);
    }

    //TODO: Call nCustomer directly
    fetchCustomerKycScreenStatus(customerId: number): Promise<{
        CustomerId: number;
        LatestScreeningDate: DateOnly;
    }> {
        return this.apiService.post('nPreCredit', '/api/Kyc/FetchCustomerScreeningStatus', {
            CustomerId: customerId,
        });
    }

    //TODO: Call nCustomer directly
    kycScreenCustomer(
        customerId: number,
        force: boolean
    ): Promise<{
        Success: boolean;
        Skipped: boolean;
        FailureCode: string;
    }> {
        return this.apiService.post('nPreCredit', '/api/Kyc/ScreenCustomer', {
            CustomerId: customerId,
            Force: force,
        });
    }
}

export interface CustomerComponentInitialDataResult {
    firstName: string;
    birthDate: string;
    customerId: number;
    isSanctionRejected: boolean;
    includeInFatcaExport: boolean;
    wasOnboardedExternally: boolean;
    customerCardUrl: string;
    legacyCustomerCardUrl: string;
    customerFatcaCrsUrl: string;
    pepKycCustomerUrl: string;
    isMissingAddress: boolean;
    isMissingEmail: boolean;
    isCompany: boolean;
    companyName: string;
    localIsPep: boolean;
    localIsSanction: boolean;
}
