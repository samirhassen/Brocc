import { Component, OnInit } from '@angular/core';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { LoanDefaultManagementApiService, TerminationLetter } from '../../services/loan-default-management-api.service';
import { DatePipe } from '@angular/common';

@Component({
    selector: 'termination-letter-management',
    templateUrl: './termination-letter.component.html',
    styleUrls: ['./termination-letter.component.scss'],
})
export class TerminationLetterComponent implements OnInit {
    constructor(
        private apiService: LoanDefaultManagementApiService,
        public toastr: ToastrService,
        private config: ConfigService,
        private datePipe: DatePipe
    ) {}

    public m: Model;

    ngOnInit(): void {
        let model: Model = {
            searchValue: null,
            currentPageNr: null,
            totalNrOfPages: null,
            terminationLetters: null,
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

    public async postponeOrResume(letter: TerminationLetter, shouldPostpone: boolean, evt?: Event) {
        evt?.preventDefault();
        let creditNr = letter.CreditNr;
        try {
            if(shouldPostpone) {
                await this.apiService.postponeTerminationLetters(creditNr);
            } else {
                await this.apiService.resumeTerminationLetters(creditNr);
            }
            this.toastr.info(
                shouldPostpone ? `Termination letter for ${creditNr} postponed` : 'Termination letter resumed'
            );
            this.gotoPage(this.m.currentPageNr, this.m.searchValue);
        } catch(error: any) {
            this.toastr.error('Something went wrong', error);
        }
    }

    public gotoPage(pageNr: number, searchValue?: string) {
        let pageSize = 50;
        this.apiService.getPageTerminationLetters(pageSize, pageNr, searchValue).then(
            (onSuccess) => {
                this.m.currentPageNr = onSuccess.CurrentPageNr;
                this.m.totalNrOfPages = onSuccess.TotalNrOfPages;
                this.m.terminationLetters = onSuccess.Page.map(x => ({
                    ...x,
                    stateTexts: this.getStateTexts(x)
                }));
            },
            (_) => {
                this.toastr.error('Could not load termination letters');
            }
        );
    }

    private getStateTexts(letter: TerminationLetter) : string[] {
        let stateTexts: string [] = []
        if(letter.IsEligableForTerminationLetter) {
            stateTexts.push('Will receive letter.');
        }
        if(!letter.IsEligableForTerminationLetter && letter.ActivePostponedUntilDate) {
            stateTexts.push(`Postponed until ${this.datePipe.transform(letter.ActivePostponedUntilDate, 'shortDate')}.`)
        }
        if(!letter.IsEligableForTerminationLetter && letter.ActiveTerminationLetterDueDate) {
            stateTexts.push(`Has active letter due ${this.datePipe.transform(letter.ActiveTerminationLetterDueDate, 'shortDate')}.`);
        }
        if(stateTexts.length === 0 && letter.TerminationCandidateDate) {
            if(this.config.getCurrentDateAndTime().startOf('day') < moment(letter.TerminationCandidateDate)) {
                stateTexts.push(`Can receive letter from ${this.datePipe.transform(letter.TerminationCandidateDate, 'shortDate')}`)
            }
        }
        return stateTexts;
    }
}

export class Model {
    public searchValue: string;
    public currentPageNr?: number;
    public totalNrOfPages?: number;
    public terminationLetters: TerminationLetterLocal[];
}

interface TerminationLetterLocal extends TerminationLetter {
    stateTexts: string[]
}
