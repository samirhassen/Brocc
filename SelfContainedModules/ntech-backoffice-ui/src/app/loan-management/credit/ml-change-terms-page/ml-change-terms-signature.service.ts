import { NtechApiService } from "src/app/common-services/ntech-api.service";
import { NumberDictionary, getNumberDictionaryKeys } from "src/app/common.types";

export class MlChangeTermsSignatureService {
    constructor(private apiService: NtechApiService) {

    }

    public async getSignatureCustomers(sessionId: string) : Promise<{ fullName: string, hasSigned: boolean, signatureUrl: string }[]> {
        try {
            if(!sessionId) {
                return null;
            }

            let session = (await this.apiService.post<{
                Session: CommonElectronicIdSignatureSession
            }>('nCustomer', 'api/ElectronicSignatures/Get-Session', { sessionId }))?.Session;
    
            let customerIdBySignerNr : NumberDictionary<number> = JSON.parse(session.CustomData['customerIdBySignerNr']);
            let signerNrs = getNumberDictionaryKeys(session.SigningCustomersBySignerNr);
            let customerIds = signerNrs.map(x => customerIdBySignerNr[x])
            let customerDataByCustomerId = await this.apiService.shared.fetchCustomerItemsBulkAsNumberDictionary(customerIds, ['firstName', 'lastName']);
    
            return signerNrs.map(signerNr => {
                let customerId = customerIdBySignerNr[signerNr];
                let customerData = customerDataByCustomerId[customerId]?.properties ?? {};
                let signingCustomer = session.SigningCustomersBySignerNr[signerNr];
                return {
                    fullName: customerData['firstName'] + ' ' + customerData['lastName'],
                    hasSigned: !!signingCustomer.SignedDateUtc,
                    signatureUrl: signingCustomer.SignedDateUtc ? null : signingCustomer.SignatureUrl
                }
            });
        } catch(error: any) {
            return null;
        }
    }
}

type Dictionary<K extends keyof any, T> = Record<K, T>;

interface CommonElectronicIdSignatureSession {
    SigningCustomersBySignerNr: Dictionary<number, SigningCustomer>;
    CustomData: Dictionary<string, string> | null;
}

interface SigningCustomer {
    SignerNr: number;
    SignedDateUtc: Date | null;
    SignatureUrl: string | null;
    CustomData: Dictionary<string, string> | null;
}