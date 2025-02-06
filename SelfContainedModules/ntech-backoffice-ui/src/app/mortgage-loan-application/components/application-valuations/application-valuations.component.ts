import { Component, Input, SimpleChanges, TemplateRef, ViewChild } from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { MortgageLoanApplicationApiService } from '../../services/mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';
import { UcbvValuationBuyerComponentInitialData } from '../ucbv-valuation-buyer/ucbv-valuation-buyer.component';
import { PropertyValuation, UcBvValuationProcess } from '../ucbv-valuation-buyer/ucbv-valuation-process';
import { ApplicationValuationPreviewComponentInitialData } from './application-valuation-preview/application-valuation-preview.component';

@Component({
    selector: 'application-valuations',
    templateUrl: './application-valuations.component.html',
    styles: [],
})
export class ApplicationValuationsComponent {
    constructor(
        private modalService: BsModalService,
        private apiService: MortgageLoanApplicationApiService,
        private eventService: NtechEventService
    ) {}

    @ViewChild('buyNewModal', { static: true }) buyNewModal: TemplateRef<any>;
    public buyNewModalRef: BsModalRef;

    @ViewChild('showPreview', { static: true }) showPreviewModal: TemplateRef<any>;
    public showPreviewModelRef: BsModalRef;

    @Input()
    public initialData: ApplicationValuationsComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let m: Model = {
            isActiveApplication: true,
            valuations: [],
        };

        let mortgageObjectValuationList = this.initialData.application.getComplexApplicationList(
            'MortgageObjectValuation',
            true
        );

        for (let rowNr of mortgageObjectValuationList.getRowNumbers().reverse()) {
            let mortgageObjectValuation = mortgageObjectValuationList.getRow(rowNr, true);

            let v: ValuationModel = {
                date: mortgageObjectValuation.getUniqueItem('CreationDate'),
                valuationAmount: mortgageObjectValuation.getUniqueItemInteger('ValuationAmount'),
                valuationPdfArchiveKey: mortgageObjectValuation.getUniqueItem('ValuationPdfArchiveKey'),
                entityName: mortgageObjectValuation.getUniqueItem('EntityName'),
                sourceRawDataArchiveKey: mortgageObjectValuation.getUniqueItem('SourceRawDataArchiveKey'),
            };
            if (mortgageObjectValuation.getUniqueItem('SourceType') === 'UcBvLgh') {
                v.apartment = {
                    arsredovisningPdfArchiveKey: mortgageObjectValuation.getUniqueItem(
                        'SeBrfArsredovisningPdfArchiveKey'
                    ),
                    apartmentArea: mortgageObjectValuation.getUniqueItemInteger('ApartmentArea'),
                    apartmentNr: mortgageObjectValuation.getUniqueItem('SeTaxOfficeApartmentNr'),
                };
            }
            if (mortgageObjectValuation.getUniqueItem('SourceType') === 'UcBvHouse') {
                v.house = {
                    inskrivningJsonArchiveKey: mortgageObjectValuation.getUniqueItem('InskrivningJsonArchiveKey'),
                };
            }
            m.valuations.push(v);
        }

        this.m = m;
    }

    buyNew(evt?: Event) {
        evt?.preventDefault();

        let p = new UcBvValuationProcess(this.apiService.api(), this.initialData.application);

        let onValuationAccepted = (valuation: PropertyValuation) => {
            this.apiService.addPropertyValuation(valuation).then((_) => {
                this.buyNewModalRef?.hide();
                this.eventService.signalReloadApplication(this.initialData.application.applicationNr);
            });
        };

        p.getValuation().then((x) => {
            if (x.resultCode === 'success') {
                onValuationAccepted(x.valuation);
            } else {
                this.m.previewInitialData = null;
                this.m.buyNew = {
                    initialData: {
                        application: this.initialData.application,
                        initialResult: x,
                        valuationProcess: p,
                        onValuationAccepted: onValuationAccepted,
                    },
                };
                this.buyNewModalRef = this.modalService.show(this.buyNewModal, {
                    class: 'modal-xl',
                    ignoreBackdropClick: true,
                });
            }
        });
    }

    showValuation(valuation: ValuationModel, evt?: Event) {
        evt?.preventDefault();

        this.m.buyNew = null;

        this.m.previewInitialData = {
            valuation: valuation,
        };

        this.showPreviewModelRef = this.modalService.show(this.showPreviewModal, {
            class: 'modal-xl',
            ignoreBackdropClick: true,
        });
    }
}

class Model {
    isActiveApplication: boolean;
    valuations: ValuationModel[];
    buyNew?: {
        initialData: UcbvValuationBuyerComponentInitialData;
    };
    previewInitialData?: ApplicationValuationPreviewComponentInitialData;
}

export class ApplicationValuationsComponentInitialData {
    application: StandardMortgageLoanApplicationModel;
}

export interface ValuationModel {
    date: string;
    valuationAmount: number;
    valuationPdfArchiveKey: string;
    sourceRawDataArchiveKey: string;
    house?: {
        inskrivningJsonArchiveKey: string;
    };
    apartment?: {
        apartmentNr: string;
        apartmentArea: number;
        arsredovisningPdfArchiveKey: string;
    };
    entityName: string;
}
