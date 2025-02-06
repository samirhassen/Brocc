import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { FileInputEventTarget } from 'src/app/common.types';
import { LoadAmortizationBasisSeResult } from '../../credit/ml-amortization-se.service';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'app-register-ml-se-page',
    templateUrl: './register-ml-se-page.component.html',
    styles: [
    ]
})
export class RegisterMlSePageComponent implements OnInit {
    constructor(private toastr: ToastrService, private apiService: NtechApiService, private config: ConfigService) { }

    public m: Model;

    @ViewChild('fileInputLoans')
    fileInputLoans: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputFormLoans')
    fileInputFormLoans: ElementRef<HTMLFormElement>;

    @ViewChild('fileInputAgreement')
    fileInputAgreement: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputFormAgreement')
    fileInputFormAgreement: ElementRef<HTMLFormElement>;

    async ngOnInit() {
        this.reload(false);
    }

    public async reload(deletePendingAgreement: boolean, evt ?: Event) {
        evt?.preventDefault();

        if(deletePendingAgreement && this.m?.preview?.previewAgreementArchiveKey) {
            await this.apiService.post('nDocument', 'Archive/Delete', { 
                key: this.m.preview.previewAgreementArchiveKey
            });
        }
        
        this.m = {};
    }

    async selectAndLoadLoans(evt: Event) {
        evt?.preventDefault();
        this.fileInputLoans.nativeElement.click();
    }

    async selectAndLoadAgreement(evt: Event) {
        evt?.preventDefault();
        this.fileInputAgreement.nativeElement.click();
    }

    public async importPreview() {
        let y: any = await this.apiService.post('NTechHost', 'Api/Credit/SeMortgageLoans/Import-Excel-File', {
            base64EncodedExcelFile: this.apiService.shared.parseDataUrl(this.m.attachedLoansFile.dataUrl).base64Data,
            fileName: this.m.attachedLoansFile.name,
            isPreviewOnly: true,
            agreementFileName: this.m.attachedAgreementFile?.name,
            agreementFileBase64Data: this.m.attachedAgreementFile 
                ? this.apiService.shared.parseDataUrl(this.m.attachedAgreementFile.dataUrl).base64Data
                : null
        });

        this.m.preview = {
            amortBasisPreview: (y.amorteringsunderlagPreview && y.createPreview?.amortizationBasis) ? {
                amorteringsunderlag: y.amorteringsunderlagPreview,
                amortizationBasis: y.createPreview.amortizationBasis,
                propertyId: null,
                propertyIdWithLabel: '<preview>',
                balanceDate: this.config.getCurrentDateAndTime().format('YYYY-MM-DD'),
                collateralId: null
            } : null,
            errors: y.errors ?? [],
            warnings: y.warnings ?? [],
            previewAgreementArchiveKey: y.previewAgreementArchiveKey
        }
        if (this.m.preview.errors.length === 0) {
            this.m.preview.createLoansRequest = y.createPreview;
            this.m.preview.createCustomersRequests = y.customersPreview;
        }
        if (y.rawDataPoints) {
            let d: { groupName: string, values: { key: string, value: string }[] }[] = []
            for (let groupName of Object.keys(y.rawDataPoints)) {
                let group = y.rawDataPoints[groupName];
                d.push({
                    groupName: groupName,
                    values: Object.keys(group).map(x => ({
                        key: x,
                        value: group[x]
                    }))
                });
            }
            this.m.preview.rawDataPoints = d;
        }
        this.m.isCreateAllowed = y.isCreateAllowed;

    }

    public async importAndCreateLoans(evt?: Event) {
        evt?.preventDefault();
        let { base64Data } = this.apiService.shared.parseDataUrl(this.m.attachedLoansFile.dataUrl);
        let y: any = await this.apiService.post('NTechHost', 'Api/Credit/SeMortgageLoans/Import-Excel-File', {
            base64EncodedExcelFile: base64Data,
            fileName: this.m.attachedLoansFile.name,
            isPreviewOnly: false,
            agreementArchiveKey: this.m.preview.previewAgreementArchiveKey
        });

        this.m = {
            loansCreated: y.loansCreated
        }
    }

    async onLoansFileAttached(evt: Event) {
        let target: FileInputEventTarget = (evt as any).target;
        FormsHelper.loadSingleAttachedFileAsDataUrl(target.files).then(
            (x) => {
                if (!x.filename.endsWith('.xlsx')) {
                    this.toastr.warning('Loans file must be an .xlsx file');
                    this.fileInputFormLoans.nativeElement.reset();
                    return;
                }
                let dataUrl = x.dataUrl;
                this.m.attachedLoansFile = {
                    name: x.filename,
                    dataUrl: dataUrl,
                };
                this.fileInputFormLoans.nativeElement.reset();
                this.importPreview();
            },
            (x) => {
                this.toastr.error(x);
                this.fileInputFormLoans.nativeElement.reset();
            }
        );
    }

    async onAgreementFileAttached(evt: Event) {
        let target: FileInputEventTarget = (evt as any).target;
        FormsHelper.loadSingleAttachedFileAsDataUrl(target.files).then(
            (x) => {
                if (!x.filename.endsWith('.pdf')) {
                    this.toastr.warning('Agreement file must be a pdf');
                    this.fileInputFormAgreement.nativeElement.reset();
                    return;
                }
                this.m.attachedAgreementFile = {
                    dataUrl: x.dataUrl,
                    name: x.filename
                };
            },
            (x) => {
                this.toastr.error(x);
                this.fileInputFormAgreement.nativeElement.reset();
            }
        );
    }
}

interface Model {
    isCreateAllowed?: boolean
    attachedLoansFile?: {
        name: string,
        dataUrl: string
    }
    attachedAgreementFile?: {
        name: string,
        dataUrl: string
    }
    preview?: {
        previewAgreementArchiveKey?: string
        amortBasisPreview?: LoadAmortizationBasisSeResult,
        errors: string[],
        warnings: string[],
        createLoansRequest?: any,
        createCustomersRequests?: any[]
        rawDataPoints?: { groupName: string, values: { key: string, value: string }[] }[]
    }
    loansCreated?: {
        creditNrs: string[]
        collateralId: number
    }
}
