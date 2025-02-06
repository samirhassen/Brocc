import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';

@Component({
    selector: 'app-single-credit-function',
    templateUrl: './single-credit-function.component.html',
    styles: [
    ]
})
export class SingleCreditFunctionComponent implements OnInit {
    constructor(private formBuilder: UntypedFormBuilder, private apiService: NtechApiService, private toastr: ToastrService) { }

    public m: Model;

    async ngOnInit() {
        let form = new FormsHelper(
            this.formBuilder.group({
                'functionName': ['', [Validators.required]],
                'creditNr': ['', [Validators.required]]
            }));

        this.m = {
            f: form,
            error: null
        };
    }

    public async executeFunction(evt ?: Event) {
        evt?.preventDefault();

        let functionName = this.m.f.getValue('functionName');
        let creditNr = this.m.f.getValue('creditNr');
        try {
            if(functionName === 'removeMainApplicant' || functionName === 'removeCoApplicant') {
                await this.apiService.post('NTechHost', 'Api/Credit/CreditCustomer/Remove', {
                    applicantNr: functionName === 'removeMainApplicant' ? 1 : 2,
                    creditNr: creditNr
                });
                this.toastr.info('Ok');
            }
            this.m.f.setValue('creditNr', '');
            this.m.error = null;
        } catch(error: any) {
            if(error?.error?.errorMessage) {
                this.m.error = error?.error?.errorMessage;
            } else {
                this.m.error = error;
            }            
        }
    }

    public reset(evt ?: Event) {
        evt?.preventDefault();

        this.m.error = null;
    }
}

interface Model {
    f: FormsHelper,
    error: any
}
