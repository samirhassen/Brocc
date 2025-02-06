import { Component, OnInit } from '@angular/core';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import {
    DebtCollectionModel,
    LoanDefaultManagementApiService,
} from '../../services/loan-default-management-api.service';
import { DatePipe } from '@angular/common';

@Component({
    selector: 'debt-collection-management',
    templateUrl: './debt-collection.component.html',
    styleUrls: ['./debt-collection.component.scss'],
})
export class DebtCollectionComponent implements OnInit {
    constructor(
        private apiService: LoanDefaultManagementApiService,
        public toastr: ToastrService,
        private config: ConfigService,
        private datePipe: DatePipe
    ) {}

    public m: Model;
    private nrOfDaysToPostpone = 20;

    ngOnInit(): void {
        let model: Model = {
            searchValue: null,
            currentPageNr: null,
            totalNrOfPages: null,
            debtCollections: null,
        };

        this.m = model;
        // Initial load.
        this.gotoPage(0, null);
    }

    public clearSearch(evt: Event) {
        evt?.preventDefault();

        this.m.searchValue = null;
        this.gotoPage(0);
    }

    public postponeOrResume(dc: DebtCollectionModel, shouldPostpone: boolean, evt?: Event) {
        evt?.preventDefault();
        let currentDate = moment(this.config.getCurrentDateAndTime());
        let candidateDate = moment(dc.WithGraceLatestEligableTerminationLetterDueDate);
        let postponeUntilDate = moment.max([candidateDate, currentDate]).add(this.nrOfDaysToPostpone, 'day').format('YYYY-MM-DD');
        this.apiService.postponeOrResumeDebtCollections(dc.CreditNr, shouldPostpone ? postponeUntilDate : null).then(
            (_) => {
                this.toastr.info(
                    shouldPostpone ? `Debt collection for ${dc.CreditNr} postponed` : 'Debt collection resumed'
                );
                this.gotoPage(this.m.currentPageNr, this.m.searchValue);
            },
            (onError) => {
                this.toastr.error('Something went wrong', onError);
            }
        );
    }

    public gotoPage(pageNr: number, searchValue?: string) {
        let pageSize = 50;
        this.apiService.getPageDebtCollection(pageSize, pageNr, searchValue).then(
            (onSuccess) => {
                this.m.currentPageNr = onSuccess.CurrentPageNr;
                this.m.totalNrOfPages = onSuccess.TotalNrOfPages;
                this.m.debtCollections = onSuccess.Page.map(x => ({
                    ...x,
                    stateTexts: this.getStateTexts(x)
                }));
            },
            (_) => {
                this.toastr.error('Could not load debt collections.');
            }
        );
    }

    private getStateTexts(dc: DebtCollectionModel) {
        let stateTexts: string[] = [];
        if(dc.IsEligableForDebtCollectionExport) {
            stateTexts.push('Will be sent to debt collection');
        }
        if(!dc.IsEligableForDebtCollectionExport && dc.ActivePostponedUntilDate) {
            stateTexts.push(`Postponed until ${this.datePipe.transform(dc.ActivePostponedUntilDate, 'shortDate')}`);
        }
        if(stateTexts.length === 0 && dc.WithGraceLatestEligableTerminationLetterDueDate) {
            if(this.config.getCurrentDateAndTime().startOf('day') < moment(dc.WithGraceLatestEligableTerminationLetterDueDate)) {
                stateTexts.push(`Can be sent from ${this.datePipe.transform(dc.WithGraceLatestEligableTerminationLetterDueDate, 'shortDate')}`)
            }
        }
        return stateTexts;
    }
}

export class Model {
    public searchValue: string;
    public currentPageNr?: number;
    public totalNrOfPages?: number;
    public debtCollections: DebtCollectionModelLocal[];
}

interface DebtCollectionModelLocal extends DebtCollectionModel {
    stateTexts: string[]
}
