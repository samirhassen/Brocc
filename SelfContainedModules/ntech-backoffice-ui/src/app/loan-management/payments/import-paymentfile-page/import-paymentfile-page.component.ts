import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { FileInputEventTarget } from 'src/app/common.types';

@Component({
    selector: 'app-import-paymentfile-page',
    templateUrl: './import-paymentfile-page.component.html',
    styles: [
    ]
})
export class ImportPaymentfilePageComponent implements OnInit {
    constructor(private config: ConfigService, private toastr: ToastrService, private formBuilder: UntypedFormBuilder,
        private apiService: NtechApiService) { }

    public m: Model;

    @ViewChild('fileInput')
    fileInput: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputForm')
    fileInputForm: ElementRef<HTMLFormElement>;    

    async ngOnInit() {
        let m: Model = {
            successMessage: null,
            candidateFile: null,
            fileForm: null,
            fileFormats: [],
            baseCountry: this.config.baseCountry()
        }

        if(m.baseCountry === 'SE') {
            m.fileFormats.push({ value: 'bgmax', text: 'BgMax' });
            m.fileFormats.push({ value: 'autogiro', text: 'Autogiro' });
        } else if(m.baseCountry === 'FI') {
            m.fileFormats.push({ value: 'camt.054.001.02', text: 'BankToCustomerDebitCreditNotificationV02 (camt.054.001.02)' });
        }
        m.fileForm = new FormsHelper(this.formBuilder.group({
            'fileFormatName': [m.fileFormats[0].value, [Validators.required]]
        }));
        
        this.m = m;
    }

    public selectFile(evt ?: Event) {
        evt?.preventDefault();
        this.fileInput.nativeElement.click();
    }

    async importFile(evt ?: Event) {
        evt?.preventDefault();
        let c = this.m.candidateFile;
        try {
            let result = await this.apiService.post<{ message: string }>('NTechHost', 'Api/Credit/PaymentPlacement/Import-PaymentFile', {
                fileAsDataUrl: c.dataUrl,
                fileName: c.filename,
                fileFormatName: c.fileFormatName,
                overrideDuplicateCheck: c.form.getValue('forceImport') === true,
                overrideIbanCheck: c.form.getValue('forceImportIban') === true
            });
            this.m.candidateFile = null;
            this.m.successMessage = result.message;
        } catch(error: any) {
            this.m.successMessage = null;
            this.toastr.error(error?.toString());
        }
    }

    public onFileAttached(evt: Event) {
        let target: FileInputEventTarget = (evt as any).target;
        FormsHelper.loadSingleAttachedFileAsDataUrl(target.files).then(            
            (x) => {
                let fileFormatName = this.m.fileForm.getValue('fileFormatName');
                this.apiService.post<GetDataResult>('NTechHost', 'Api/Credit/PaymentPlacement/PaymentFile-Data', {
                    fileAsDataUrl: x.dataUrl,
                    fileName: x.filename,
                    fileFormatName: this.m.fileForm.getValue('fileFormatName')
                }).then(response => {
                    this.fileInputForm.nativeElement.reset();
                    let form = new FormsHelper(this.formBuilder.group({
                        'forceImport': [false, [Validators.required]],
                        'forceImportIban': [false, [Validators.required]]
                    }));
                    this.m.candidateFile = {
                        fileFormatName: fileFormatName,
                        dataUrl: x.dataUrl,
                        filename: x.filename,
                        data: response,
                        form: form
                    }
                });
            },
            (x) => {
                this.toastr.error(x);
                this.m.successMessage = null;
                this.fileInputForm.nativeElement.reset();
            }
        );
    }

    isImportAllowed() {
        let c = this.m.candidateFile;
        if(c.data.hasBeenImported && c.form.getValue('forceImport') !== true) {
            return false;
        }
        if(c.data.hasUnexpectedBankAccountNrs && c.form.getValue('forceImportIban') !== true) {
            return false;
        }        
        return true;
    }
}

interface Model {
    baseCountry: string
    successMessage: string
    candidateFile: {
        fileFormatName: string
        filename: string
        dataUrl: string
        data: GetDataResult
        form: FormsHelper
    }
    fileForm: FormsHelper
    fileFormats: { text: string, value: string }[]
}

interface GetDataResult {
    hasUnexpectedBankAccountNrs: boolean
    fileCreationDate: string
    externalId: string
    includedBankAccountNrs: string
    expectedBankAccountNr: string
    totalPaymentCount: number
    totalPaymentSum: number
    hasBeenImported: boolean
}
