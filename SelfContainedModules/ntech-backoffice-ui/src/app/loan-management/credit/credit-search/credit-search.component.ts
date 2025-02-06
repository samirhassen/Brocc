import { Component, Input, OnInit } from '@angular/core';
import { Router, NavigationExtras } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { CreditService, SearchCreditHit } from '../credit.service';

@Component({
    selector: 'credit-search',
    templateUrl: './credit-search.component.html',
    styles: [],
})
export class CreditSearchComponent implements OnInit {
    constructor(private configService: ConfigService, private router: Router, private creditService: CreditService) {}

    public m: Model;

    @Input()
    public isDisplayingSearchResult: BehaviorSubject<boolean>;

    ngOnInit(): void {
        let m: Model = {
            omniSearchValue: '',
            civicRegNrMask: '',
            searchHits: null,
        };

        if (this.configService.baseCountry() === 'FI') {
            m.civicRegNrMask = '(DDMMYYSNNNK)';
        } else if (this.configService.baseCountry() === 'SE') {
            m.civicRegNrMask = '(YYYYMMDDRRRC)';
        }

        this.m = m;

        this.synchIsDisplayingSearchResult();
    }

    private synchIsDisplayingSearchResult() {
        if (!this.isDisplayingSearchResult) {
            return;
        }

        if (!this.m) {
            this.isDisplayingSearchResult.next(false);
        } else {
            this.isDisplayingSearchResult.next(!!this.m.searchHits);
        }
    }

    async searchCredit(evt?: Event) {
        evt?.preventDefault();

        let { hits } = await this.creditService.searchCredit(this.m.omniSearchValue);

        if (hits.length === 1) {
            let link = this.getCreditLink(hits[0].creditNr);
            this.router.navigate(link.commands, link.extras);
            this.m.searchHits = null;
        } else {
            this.m.searchHits = hits;
        }

        this.synchIsDisplayingSearchResult();
    }

    onSearchTextChanged(newText: string) {
        if (!newText && this.m?.searchHits) {
            this.m.searchHits = null;
            this.synchIsDisplayingSearchResult();
        }
    }

    resetSearch(evt?: Event) {
        evt?.preventDefault();
        this.m.omniSearchValue = null;
        this.onSearchTextChanged('');
    }

    getCreditLink(creditNr: string): {
        commands: any[];
        extras: NavigationExtras;
    } {
        return {
            commands: ['/credit/details', creditNr],
            extras: {
                queryParams: {
                    backTarget: CrossModuleNavigationTarget.create('CreditSearch', {}),
                },
            },
        };
    }
}

interface Model {
    omniSearchValue: string;
    civicRegNrMask: string;
    searchHits: SearchCreditHit[];
}
