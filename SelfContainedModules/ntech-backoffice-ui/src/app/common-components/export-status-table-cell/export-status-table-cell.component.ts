import { Component, Input, SimpleChanges } from '@angular/core';

@Component({
    selector: 'export-status-table-cell',
    templateUrl: './export-status-table-cell.component.html',
    styles: [],
})
export class ExportStatusTableCellComponent {
    constructor() {}

    @Input()
    public statusModelJson: string;

    @Input()
    public statusModel: ExportResultStatusStandardModel;

    m: ExportResultStatusStandardModel;

    ngOnChanges(changes: SimpleChanges) {
        if (this.statusModel) {
            this.m = this.statusModel;
        } else if (this.statusModelJson) {
            this.m = JSON.parse(this.statusModelJson);
        } else {
            this.m = null;
        }
    }
}

//Maps to OutgoingExportFileHeader.ExportResultStatusStandardModel
export interface ExportResultStatusStandardModel {
    status: string;
    errors: string[];
    warnings: string[];
    deliveryTimeInMs: number;
    deliveredToProfileName: string;
    providerName: string;
    deliveredToProfileNames: string[];
    failedProfileNames: string[];
}
