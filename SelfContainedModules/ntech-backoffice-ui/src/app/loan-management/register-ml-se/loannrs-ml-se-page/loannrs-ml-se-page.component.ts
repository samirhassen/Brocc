import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import * as moment from 'moment';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';

@Component({
    selector: 'app-loannrs-ml-se-page',
    templateUrl: './loannrs-ml-se-page.component.html',
    styles: [
    ]
})
export class LoannrsMlSePageComponent implements OnInit {
    constructor(private apiService: NtechApiService, private fb: UntypedFormBuilder) { }

    public m: Model;

    async ngOnInit() {
        let result = await this.apiService.post<{ recentCreditNrs: { creditNr: string, creditStartDate: string }[] }>('nCredit', 'Api/RecentlyGeneratedCreditNrs', { maxCount: 200 });
        let f = new FormsHelper(this.fb.group({
            'count': ['1', [Validators.required]]
        }));
        this.m = {
            form: f,
            sessionCreditNrs: [],
            recentCreditNrs: result.recentCreditNrs
        }
    }

    formatDate(date: string) {
        return moment(date).format('YYYY-MM-DD hh:mm');
    }

    async generateNrs(evt ?: Event) {
        evt?.preventDefault();
        let count = parseInt(this.m.form.getValue('count'));
        let result = await this.apiService.post<{ nrs: string[] }>('nCredit', 'Api/NewCreditNumbers', { count });
        this.m.sessionCreditNrs.push(...result.nrs);
    }
}

interface Model {
    form: FormsHelper
    sessionCreditNrs: string[],
    recentCreditNrs: {
        creditNr: string, creditStartDate: string
    }[]
}