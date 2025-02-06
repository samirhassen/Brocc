import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';

/*
Valuation process that is automated as far as possible but can stop at any point for user input
*/
export class UcBvValuationProcess {
    constructor(private apiService: NtechApiService, private application: StandardMortgageLoanApplicationModel) {}

    public async getValuation(overrides?: ValuationOverrides): Promise<{
        resultCode:
            | 'error'
            | 'requireObjectType'
            | 'requireAddress'
            | 'requireObjectChoice'
            | 'requireApartmentNrChoice'
            | 'success';
        errorMessage?: string;
        requireObjectChoice?: {
            objects: { Id: string; Name: string }[];
        };
        requireApartmentNrChoice?: {
            foreningName: string;
            seTaxOfficeApartmentNrs: string[];
        };
        valuation?: PropertyValuation;
    }> {
        try {
            let applicationAddress = this.getApplicationAddress();
            let isApartment = this.getIsApartment(overrides?.isApartment);

            if (isApartment === null) {
                return {
                    resultCode: 'requireObjectType',
                };
            }

            let street = overrides?.address?.street ?? applicationAddress.street;
            let zipCode = overrides?.address?.zipCode ?? applicationAddress.zipCode;
            let city = overrides?.address?.city ?? applicationAddress.city;
            let municipality = overrides?.address?.municipality ?? applicationAddress.municipality;

            if (!zipCode || !city) {
                return {
                    resultCode: 'requireAddress',
                };
            }

            let seTaxOfficeApartmentNr = overrides?.seTaxOfficeApartmentNr ?? applicationAddress.seTaxOfficeApartmentNr;
            let objectId = overrides?.objectId;

            if (!objectId) {
                let findAddressResult: any = await this.apiService.post('nCreditReport', 'Api/UcBvSe/SokAddress', {
                    adress: street,
                    postnr: zipCode,
                    postort: city,
                    kommun: municipality,
                });
                this.ensureSuccess(findAddressResult);
                if (findAddressResult.Data.length > 1) {
                    return {
                        resultCode: 'requireObjectChoice',
                        requireObjectChoice: {
                            objects: findAddressResult.Data,
                        },
                    };
                } else if (findAddressResult.Data.length === 0) {
                    return {
                        resultCode: 'error',
                        errorMessage: 'Hittade inget matchande objekt',
                    };
                }
                objectId = findAddressResult.Data[0].Id;
            }

            let getObjectResult: any = await this.apiService.post('nCreditReport', 'Api/UcBvSe/HamtaObjektInfo2', {
                id: objectId,
            });
            this.ensureSuccess(getObjectResult);
            if (isApartment && getObjectResult.Data.Objekttyp !== 'BR') {
                return {
                    resultCode: 'error',
                    errorMessage: 'Objektet är inte en bostadsrätt',
                };
            }
            if (!isApartment && getObjectResult.Data.Objekttyp !== 'SH') {
                return {
                    resultCode: 'error',
                    errorMessage: 'Objektet är inte ett småhus',
                };
            }

            let entityName: string;
            let valuation: PropertyValuation;

            if (isApartment) {
                let seTaxOfficeApartmentNrs: string[] = getObjectResult.Data.Lagenheter.map((y: any) => y.Lghnr);
                entityName = getObjectResult.Data.Forening;
                if (!seTaxOfficeApartmentNr || !seTaxOfficeApartmentNrs.find((x) => x === seTaxOfficeApartmentNr)) {
                    return {
                        resultCode: 'requireApartmentNrChoice',
                        requireApartmentNrChoice: {
                            foreningName: entityName,
                            seTaxOfficeApartmentNrs: seTaxOfficeApartmentNrs,
                        },
                    };
                }

                let apartmentResult: any = await this.apiService.post('nCreditReport', 'Api/UcBvSe/HamtaLagenhet', {
                    id: objectId,
                    lghNr: seTaxOfficeApartmentNr,
                });

                this.ensureSuccess(apartmentResult);
                let area = apartmentResult.Data.Boarea;
                let valuationResult: any = await this.apiService.post(
                    'nCreditReport',
                    'Api/UcBvSe/VarderaBostadsratt',
                    {
                        id: objectId,
                        lghNr: seTaxOfficeApartmentNr,
                        area: area,
                        includeValuationPdf: true,
                        includeSeBrfArsredovisningPdf: true,
                    }
                );

                valuation = {
                    ApplicationNr: this.application.applicationNr,
                    UcBvObjectId: objectId,
                    UcBvTransId: valuationResult.TransId,
                    ValuationAmount: valuationResult.Data.Varde,
                    UcBvValuationPdfArchiveKey: valuationResult.ValuationPdfArchiveKey,
                    SourceType: 'UcBvLgh',
                    ApartmentArea: area,
                    SeTaxOfficeApartmentNr: seTaxOfficeApartmentNr,
                    UcBvRawJsonData: valuationResult.RawFullJson,
                    EntityName: entityName,
                    SeBrfArsredovisningPdfArchiveKey: valuationResult.SeBrfArsredovisningPdfArchiveKey,
                    InskrivningJsonArchiveKey: null,
                };
            } else {
                entityName = getObjectResult.Data.Fastighet;
                let valuationResult: any = await this.apiService.post('nCreditReport', 'Api/UcBvSe/VarderaSmahus', {
                    id: objectId,
                    includeValuationPdf: true,
                    includeInskrivningJson: true,
                });

                this.ensureSuccess(valuationResult);

                valuation = {
                    ApplicationNr: this.application.applicationNr,
                    UcBvObjectId: objectId,
                    UcBvTransId: valuationResult.TransId,
                    ValuationAmount: valuationResult.Data.Varde,
                    UcBvValuationPdfArchiveKey: valuationResult.ValuationPdfArchiveKey,
                    SourceType: 'UcBvHouse',
                    ApartmentArea: null,
                    SeTaxOfficeApartmentNr: null,
                    UcBvRawJsonData: valuationResult.RawFullJson,
                    EntityName: entityName,
                    SeBrfArsredovisningPdfArchiveKey: null,
                    InskrivningJsonArchiveKey: valuationResult.InskrivningJsonArchiveKey,
                };
            }

            return {
                resultCode: 'success',
                valuation: valuation,
            };
        } catch (e: any) {
            if (e.resultCode && e.errorMessage) {
                return e;
            } else {
                throw e;
            }
        }
    }

    public getApplicationAddress() {
        let applicationRow = this.application.getComplexApplicationList('Application', true).getRow(1, true);
        return {
            street: applicationRow.getUniqueItem('objectAddressStreet'),
            zipCode: applicationRow.getUniqueItem('objectAddressZipcode'),
            city: applicationRow.getUniqueItem('objectAddressCity'),
            municipality: applicationRow.getUniqueItem('objectAddressMunicipality'),
            seTaxOfficeApartmentNr: applicationRow.getUniqueItem('seTaxOfficeApartmentNr'),
        };
    }

    private ensureSuccess(ucApiResult: { Felkod: number; Felmeddelande: string }) {
        if (ucApiResult?.Felkod !== 0) {
            throw {
                resultCode: 'error',
                errorMessage: ucApiResult?.Felmeddelande ?? 'Unknown error',
            };
        }
    }

    private getIsApartment(isApartmentOverride?: boolean): boolean | null {
        let applicationRow = this.application.getComplexApplicationList('Application', true).getRow(1, true);
        if (isApartmentOverride === true || isApartmentOverride === false) {
            return isApartmentOverride;
        }
        let objectTypeCode = applicationRow.getUniqueItem('objectTypeCode');
        if (objectTypeCode === 'seBrf') {
            return true;
        } else if (objectTypeCode === 'seFastighet') {
            return false;
        }
        return null;
    }
}

export interface PropertyValuation {
    ApplicationNr: string;
    UcBvObjectId: string;
    UcBvTransId: string;
    ValuationAmount: number;
    UcBvValuationPdfArchiveKey: string;
    UcBvRawJsonData: string;
    SourceType: string;
    ApartmentArea: number;
    SeTaxOfficeApartmentNr: string;
    EntityName: string;
    SeBrfArsredovisningPdfArchiveKey: string;
    InskrivningJsonArchiveKey: string;
}

export interface GetValuationResult {
    resultCode:
        | 'error'
        | 'requireObjectType'
        | 'requireAddress'
        | 'requireObjectChoice'
        | 'requireApartmentNrChoice'
        | 'success';
    errorMessage?: string;
    requireObjectChoice?: {
        objects: { Id: string; Name: string }[];
    };
    requireApartmentNrChoice?: {
        foreningName: string;
        seTaxOfficeApartmentNrs: string[];
    };
    valuation?: PropertyValuation;
}

export interface ValuationOverrides {
    isApartment?: boolean;
    address?: {
        street?: string;
        zipCode?: string;
        city?: string;
        municipality?: string;
    };
    objectId?: string;
    seTaxOfficeApartmentNr?: string;
}
