import { Component, OnInit, Output } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { EventEmitter } from '@angular/core';

@Component({
    selector: 'customer-checkpoints-search',
    templateUrl: './search.component.html',
    styles: [
    ]
})
export class SearchComponent implements OnInit {
    constructor(private configService: ConfigService, private apiService: NtechApiService, private formBuilder: UntypedFormBuilder,
        private validationService: NTechValidationService) { }

    public m: Model;

    @Output()
    public search : EventEmitter<SearchCheckpointData> = new EventEmitter<SearchCheckpointData>();

    ngOnInit(): void {
        const searchForm = new FormsHelper(this.formBuilder.group({
            'searchText': ['', [Validators.required, this.validationService.getCivicRegNrValidator()]]
        }));
        const m: Model = {
            civicRegNrMask: '',
            searchForm: searchForm
        };
        if (this.configService.baseCountry() === 'FI') {
            m.civicRegNrMask = '(DDMMYYSNNNK)'
        } else if (this.configService.baseCountry() === 'SE') {
            m.civicRegNrMask = '(YYYYMMDDRRRC)'
        }
        this.m = m;
    }

    async onSearch(evt?: Event) {
        evt?.preventDefault();
        if (this.m.searchForm.invalid()) {
            return;
        }
        const civicRegNr = this.m.searchForm.getValue('searchText');

        const customerId = (await this.apiService.shared.fetchCustomerIdByCivicRegNr(civicRegNr)).CustomerId;

        this.search.emit({ 
            civicRegNr: civicRegNr,
            customerId: customerId
        });
    }

    resetSearch(evt?: Event) {
        evt?.preventDefault()
        this.m.searchForm.form.reset({
            searchText: ''
        })
    }
}

class Model {
    civicRegNrMask: string
    searchForm: FormsHelper
}

export interface SearchCheckpointData {
    customerId: number
    civicRegNr: string
}