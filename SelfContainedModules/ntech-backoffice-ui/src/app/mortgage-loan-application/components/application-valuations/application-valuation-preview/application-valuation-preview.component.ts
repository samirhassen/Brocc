import { Component, Input, SimpleChanges } from '@angular/core';
import { MortgageLoanApplicationApiService } from 'src/app/mortgage-loan-application/services/mortgage-loan-application-api.service';
import { ValuationModel } from '../application-valuations.component';

@Component({
    selector: 'application-valuation-preview',
    templateUrl: './application-valuation-preview.component.html',
    styles: [],
})
export class ApplicationValuationPreviewComponent {
    constructor(private apiService: MortgageLoanApplicationApiService) {}

    @Input()
    public initialData: ApplicationValuationPreviewComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let valuation = this.initialData.valuation;

        this.loadValuationAndInskrivningData(
            valuation.sourceRawDataArchiveKey,
            valuation.house?.inskrivningJsonArchiveKey
        ).then((x) => {
            this.m = {
                valuation: valuation,
                inskrivningData: x.inskrivningData,
                valuationData: x.valuationData,
                inskrivningAgare: x.inskrivningData ? this.createAgareModel(x.inskrivningData) : null,
                brfSignal: valuation.apartment ? this.createBrfSignalWarnings(x.valuationData) : null,
            };
        });
    }

    public getArchiveDocumentUrl(archiveKey: string) {
        return archiveKey ? this.apiService.api().getArchiveDocumentUrl(archiveKey, true) : null;
    }

    public joinStrings(strings: string[]) {
        return strings && strings.length > 0 ? strings.join(', ') : '-';
    }

    private async loadValuationAndInskrivningData(
        valuationArchiveKey: string,
        inskrivningArchiveKey: string
    ): Promise<{
        valuationData: any;
        inskrivningData: any;
    }> {
        let valuationData: any = null;
        let inskrivningData: any = null;
        if (valuationArchiveKey) {
            let valuationBlob = await this.apiService.shared().downloadArchiveDocumentData(valuationArchiveKey);
            let valuationText = await valuationBlob.text();
            valuationData = JSON.parse(valuationText);
        }
        if (inskrivningArchiveKey) {
            let inskrivningBlob = await this.apiService.shared().downloadArchiveDocumentData(inskrivningArchiveKey);
            let inskrivningText = await inskrivningBlob.text();
            inskrivningData = JSON.parse(inskrivningText);
        }
        return {
            valuationData,
            inskrivningData,
        };
    }

    private createAgareModel(inskrivningData: any): HouseAgare[] {
        let inskrivningAgare: HouseAgare[] = [];
        let x = inskrivningData;
        for (let agande of x.Agande) {
            let agare = agande.Agare;
            let namn: string;
            let isPerson: boolean;
            let adress: string;
            if (agare.Person) {
                namn = agare.fornamn + ' ' + agare.efternamn;
                isPerson = true;
                if (agare.Person.SarskildAdress || agare.Person.Utlandsadress) {
                    adress = 'Varning: Särskild eller utlandsadress. Se rådata';
                } else if (agare.Person.Adress) {
                    let adr = agare.Person.Adress;
                    adress = [adr.coAdress, adr.utdelningsadress1, adr.utdelningsadress2, adr.postnummer, adr.postort]
                        .filter((x) => !!x)
                        .join(' ');
                } else {
                    adress = 'Saknas';
                }
            } else {
                namn = agare.organisationsnamn;
                isPerson = false;
                if (agare.Organisation.Utlandsadress) {
                    adress = 'Varning: Utlandsadress. Se rådata';
                } else if (agare.Organisation.Adress) {
                    let adr = agare.Organisation.Adress;
                    adress = [adr.coAdress, adr.utdelningsadress1, adr.utdelningsadress2, adr.postnummer, adr.postort]
                        .filter((x) => !!x)
                        .join(' ');
                } else {
                    adress = 'Saknas';
                }
            }
            inskrivningAgare.push({
                namn: namn,
                isPerson: isPerson,
                orgOrCivicNr: agare.IDnummer,
                address: adress,
                andelTaljare: parseInt(agande.BeviljadAndel.taljare),
                andelNamnare: parseInt(agande.BeviljadAndel.namnare),
            });
        }
        return inskrivningAgare;
    }

    private createBrfSignalWarnings(valuationData: any): { year: number; warnings: string[] } {
        let warnings: string[] = [];
        let brfSignal = valuationData?.Data?.Brfsignal;
        if (brfSignal?.Belaning === 2) {
            warnings.push('Belåning');
        }
        if (brfSignal?.Likviditet === 2) {
            warnings.push('Likviditet');
        }
        if (brfSignal?.Rantekanslighet === 2) {
            warnings.push('Räntekänslighet');
        }
        if (brfSignal?.Sjalvforsorjningsgrad === 2) {
            warnings.push('Självförsorjningsgrad');
        }
        return brfSignal
            ? {
                  year: brfSignal?.Ar,
                  warnings: warnings,
              }
            : null;
    }
}

class Model {
    valuation: ValuationModel;
    valuationData: any;
    inskrivningData?: any;
    inskrivningAgare?: HouseAgare[];
    brfSignal?: { year: number; warnings: string[] };
}

interface HouseAgare {
    namn: string;
    isPerson: boolean;
    orgOrCivicNr: string;
    address: string;
    andelTaljare: number;
    andelNamnare: number;
}

export class ApplicationValuationPreviewComponentInitialData {
    valuation: ValuationModel;
}
