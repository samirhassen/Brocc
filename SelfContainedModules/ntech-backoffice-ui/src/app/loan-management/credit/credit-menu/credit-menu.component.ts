import { Component, Input, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject } from 'rxjs';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { CreditAttentionStatus, CreditService, MenuItemModel } from '../credit.service';

@Component({
    selector: 'credit-menu',
    templateUrl: './credit-menu.component.html',
    styles: [],
})
export class CreditMenuComponent {
    constructor(
        private creditService: CreditService,
        private apiService: NtechApiService,
        private configService: ConfigService,
        private router: Router,
        private toastr: ToastrService
    ) {}

    @Input()
    public creditNr: string;

    @Input()
    public activeMenuItemCode: string;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        await this.reload(this.creditNr);
    }

    private async reload(creditNr: string) {
        this.m = null;

        if (!creditNr) {
            return;
        }
        let status = await this.creditService.fetchCreditAttentionStatus(creditNr);

        this.m = {
            status: status,
            testFunctions: createCreateTestFunctionsModel(
                this.configService,
                this.apiService,
                this.creditService,
                this.toastr,
                this.router,
                creditNr,
                () => this.reload(creditNr)
            ),
            isDisplayingSearchResult: new BehaviorSubject<boolean>(false),
            menuItems: this.creditService.getActiveMenuItems()
        };
    }

    getUrlOrRouterLink(code: string): { url?: string; routerLink?: string[] } {
        return {
            routerLink: ['/credit', code, this.creditNr],
        };
    }
}

class Model {
    status: CreditAttentionStatus;
    testFunctions: TestFunctionsModel;
    isDisplayingSearchResult: BehaviorSubject<boolean>;
    menuItems: MenuItemModel[];
}

export function createCreateTestFunctionsModel(
    configService: ConfigService,
    apiService: NtechApiService,
    creditService: CreditService,
    toastr: ToastrService,
    router: Router,
    creditNr: string,
    reload: () => void
) {
    if (!configService.isNTechTest()) {
        return null;
    }

    let gotoRandomCredit = (creditType: string) => {
        apiService
            .post<{ creditNr: string }>('nCredit', 'Api/Credit/TestFindRandom', {
                creditType,
            })
            .then(({ creditNr }) => {
                if (!creditNr) {
                    toastr.warning('No such credit exists');
                } else {
                    router.navigate(['/credit/details', creditNr]);
                }
            });
    };

    let f = new TestFunctionsModel();
    let addGotoCredit = (label: string, creditType: string) => {
        f.addFunctionCall(label, () => {
            gotoRandomCredit(creditType);
        });
    };
    addGotoCredit('Goto random credit - any', 'any');
    addGotoCredit('Goto random credit - no impairments', 'noimpairments');
    addGotoCredit('Goto random credit - on debt collection', 'debtcol');
    addGotoCredit('Goto random credit - with overdue notifications', 'overdue');
    addGotoCredit('Goto random credit - with a coapplicant', 'withcoapplicant');
    f.addLink('Test emails', apiService.getUiGatewayUrl('nCredit', 'Ui/TestLatestEmails/List'));

    if (creditNr && configService.baseCountry() === 'SE') {
        f.addFunctionCall('Inactivate termination letters', () => {
            creditService.inactivateTerminationLetters([creditNr])
                .then(({inactivatedOnCreditNrs}) => {
                    let wasInactivated = inactivatedOnCreditNrs?.length > 0;
                    if (wasInactivated) {
                        toastr.info('Letter inactivated');
                        reload();
                    } else {
                        toastr.warning('No termination letter inactivated');
                    }
                });
        });
    }

    return f;
}
