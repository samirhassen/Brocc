import { Component, Input } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import {
    LoanStandardApplicationSearchHit,
    SharedApplicationApiService,
} from 'src/app/shared-application-components/services/shared-loan-application-api.service';

const PageSize: number = 25;

@Component({
    selector: 'applications-search',
    templateUrl: './applications-search.component.html',
    styles: [],
})
export class ApplicationsSearchComponent {
    constructor(private fb: UntypedFormBuilder, private router: Router) {}

    @Input() public initialData: ApplicationsSearchInitialData;
    public m: Model;

    ngOnInit(): void {
        this.gotoDataPage(0, true);
    }

    reset(evt?: Event) {
        evt?.preventDefault();
        this.m.form.getFormControl(null, 'omniSearchValue').setValue('');
        this.getDataPage(0, null, true);
    }

    private getDataPage(pageNr: number, evt?: Event, exclude?: boolean) {
        evt?.preventDefault();
        this.gotoDataPage(pageNr, exclude);
    }

    isSpecialSearchMode() {
        return !!this.m?.form?.getValue('omniSearchValue');
    }

    private navigateToApplication(applicationNr: string) {
        let routeStr = this.getRouterNavigationString();
        this.router.navigate([routeStr, applicationNr], { queryParams: { backTarget: this.m.targetToHere.getCode() } });
    }

    getRouterNavigationString(isCrossModuleTarget?: boolean) {
        if (this.initialData.isForMortgageLoans)
            return isCrossModuleTarget ? 'NewMortgageLoanApplications' : '/mortgage-loan-application/application/';
        else return isCrossModuleTarget ? 'NewUnsecuredLoanApplications' : '/unsecured-loan-application/application/';
    }

    private gotoDataPage(pageNr: number, exclude?: boolean) {
        this.initialData.apiService
            .fetchStandardWorkListDataPage(null, null, null, PageSize, pageNr, {
                includeListCounts: true,
                includeProviders: true,
                includeWorkflowModel: true,
            })
            .then((result) => {
                this.m = {
                    targetToHere: CrossModuleNavigationTarget.create(this.getRouterNavigationString(true), {}),
                    form: new FormsHelper(
                        this.fb.group({
                            omniSearchValue: ['', []],
                        })
                    ),
                    searchResult: {
                        items: exclude ? null : result.PageApplications,
                        pagingInitialData: {
                            totalNrOfPages: result.TotalNrOfPages,
                            currentPageNr: result.CurrentPageNr,
                            onGotoPage: (pageNr) => {
                                exclude ? null : this.gotoDataPage(pageNr);
                            },
                        },
                    },
                };
            });
    }

    search(evt?: Event) {
        evt?.preventDefault();

        if (!this.m || this.m.form.invalid()) {
            return; //To avoid the enter click handler bypassing the disabled buttons
        }

        this.initialData.apiService
            .searchForLoanStandardApplicationByOmniValue(this.m.form.getValue('omniSearchValue'), false)
            .then((x) => {
                if (x.Applications.length === 1) {
                    this.navigateToApplication(x.Applications[0].ApplicationNr);
                } else {
                    this.m.searchResult = {
                        items: x.Applications,
                        pagingInitialData: {
                            totalNrOfPages: 1,
                            currentPageNr: 0,
                            onGotoPage: (pageNr) => {
                                this.gotoDataPage(pageNr);
                            },
                        },
                    };
                }
            });
    }
}

class Model {
    targetToHere: CrossModuleNavigationTarget;
    form: FormsHelper;
    searchResult?: {
        items: LoanStandardApplicationSearchHit[];
        pagingInitialData: TablePagerInitialData;
    };
}

export class ApplicationsSearchInitialData {
    constructor(public isForMortgageLoans: boolean, public apiService: SharedApplicationApiService) {}
}
