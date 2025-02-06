import { Dictionary, NumberDictionary, StringDictionary } from '../common.types';
import { NtechApiService } from './ntech-api.service';

export class SharedApisService {
    constructor(private apiService: NtechApiService) {}

    private bulkFetchPropertiesByCustomerIds(
        customerIds: number[],
        itemNames: string[]
    ): Promise<{
        customers: {
            CustomerId: number;
            Properties: { Name: string; Value: string }[];
        }[];
    }> {
        return this.apiService.post('nCustomer', '/Customer/BulkFetchPropertiesByCustomerIds', {
            customerIds: customerIds,
            propertyNames: itemNames,
        });
    }

    fetchCustomerItems(customerId: number, itemNames: string[]): Promise<StringDictionary> {
        return this.fetchCustomerItemsBulk([customerId], itemNames).then((x) => {
            return x[customerId];
        });
    }

    fetchCustomerItemsBulk(customerIds: number[], itemNames: string[]): Promise<NumberDictionary<StringDictionary>> {
        return this.bulkFetchPropertiesByCustomerIds(customerIds, itemNames).then((x) => {
            let r: NumberDictionary<StringDictionary> = {};
            if (x.customers) {
                for (let c of x.customers) {
                    let cd: StringDictionary = {};
                    for (let p of c.Properties) {
                        cd[p.Name] = p.Value;
                    }
                    r[c.CustomerId] = cd;
                }
            }
            return r;
        });
    }

    async fetchCustomerItemsBulkAsNumberDictionary(
        customerIds: number[],
        itemNames: string[]
    ): Promise<
        NumberDictionary<{
            properties: StringDictionary;
        }>
    > {
        let fetchResult = await this.bulkFetchPropertiesByCustomerIds(customerIds, itemNames);

        let r: NumberDictionary<{
            properties: StringDictionary;
        }> = {};

        if (!fetchResult.customers) {
            return r;
        }

        for (let c of fetchResult.customers) {
            let cd: StringDictionary = {};
            for (let p of c.Properties) {
                cd[p.Name] = p.Value;
            }
            r[c.CustomerId] = {
                properties: cd,
            };
        }
        return r;
    }

    fetchCustomerIdByCivicRegNr(civicRegNr: string): Promise<{ CustomerId?: number }> {
        return this.apiService.post('nCustomer', 'api/CustomerIdByCivicRegNr', { civicRegNr });
    }

    fetchCustomerIdByOrgnr(orgnr: string): Promise<{ CustomerId?: number }> {
        return this.apiService.post('nCustomer', 'api/CustomerIdByOrgnr', { orgnr });
    }

    downloadArchiveDocumentData(archiveKey: string) {
        return this.apiService.download('nDocument', 'Archive/Download', { key: archiveKey });
    }

    async storeArchiveDocument(mimeType: string, fileName: string, base64EncodedFileData: string) {
        let {key} = await this.apiService.post<{ key: string }>('nDocument', 'Archive/Store', { mimeType, fileName, base64EncodedFileData }, { forceCamelCase: true});
        return key;
    }

    parseDataUrl(dataUrl: string) : { mimeType: string, base64Data: string } {
        //data:<mimeType>[;charset];base64,<base64data>
        let d = dataUrl.substring('data:'.length); //<mimeType>[;charset];base64,<base64data>
        let mimeType = d.substring(0, d.indexOf(';'));
        let base64Data = d.substring(d.indexOf(',') + 1);
        return { mimeType, base64Data };
    }

    resolveCrossModuleNavigationTarget(
        targetCode: string
    ): Promise<{ Url: string; LocalEmbeddedBackofficeUrl?: string }> {
        return this.apiService.post('nBackOffice', 'Api/CrossModuleNavigate/Resolve', { targetCode });
    }

    getCurrentSettingValues(settingCode: string): Promise<{ SettingValues: Dictionary<string> }> {
        return this.apiService.post('nCustomer', 'api/Settings/LoadValues', { settingCode });
    }

    fetchBookkeepingRules(): Promise<{
        ruleRows: any[];
        allConnections: string[];
        allAccountNames: string[];
        accountNrByAccountName: Dictionary<string>;
    }> {
        return this.apiService.post('nCredit', '/Api/Bookkeeping/RulesAsJson', {});
    }

    async getUserDisplayNames(userIds: number[]): Promise<NumberDictionary<string>> {
        let displayNames = await this.apiService.post<
            {
                UserId: number;
                DisplayName: string;
            }[]
        >('nUser', 'User/GetDisplayNamesAndUserIds', { userIds });

        let result: NumberDictionary<string> = {};

        for (let displayName of displayNames) {
            result[displayName.UserId] = displayName.DisplayName;
        }

        for (let userId of userIds) {
            if (!result[userId]) {
                result[userId] = `User ${userId}`;
            }
        }

        return result;
    }
}
