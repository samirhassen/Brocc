import { Component, OnInit, TemplateRef } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { CreditCapitalTransaction, CreditDetails, CreditDetailsResult, CreditService } from '../credit.service';
import { CreditNumberEditorInitialData } from './credit-number-editor/credit-number-editor.component';

@Component({
    selector: 'app-credit-details-page',
    templateUrl: './credit-details-page.component.html',
    styles: [],
})
export class CreditDetailsPageComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private configService: ConfigService,
        private apiService: NtechApiService,
        private modalService: BsModalService
    ) {}

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('creditNr'));
        });
    }

    public c: Model;
    public modalRef: BsModalRef;

    private async reload(creditNr: string) {
        this.c = null;

        if (!creditNr) {
            let title = 'Credit details';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }
        this.eventService.setCustomPageTitle(`Credit ${creditNr}`, `Credit details ${creditNr}`);
        let result = await this.creditService.getCreditDetails(creditNr);

        let c: Model = {
            creditNr: creditNr,
            details: result.details,
            capitalTransactions: result.capitalTransactions,
        };

        this.setupCompanyLoans(c, result);

        this.c = c;
    }

    private setupCompanyLoans(c: Model, result: CreditDetailsResult) {
        if (!this.configService.isFeatureEnabled('ntech.feature.companyloans')) {
            return;
        }

        let isReadonly = result.details.currentStatusCode !== 'Normal';
        let createSave = (datedCreditValueCode: string, businessEventType: string) => {
            return (newValue: number) => {
                return this.creditService
                    .setDatedCreditValue(result.details.creditNr, datedCreditValueCode, businessEventType, newValue)
                    .then((x) => ({ newValue: x.NewValue }));
            };
        };
        c.companyLoans = {
            editableNumbers: [
                {
                    isReadOnly: isReadonly,
                    labelText: 'Application PD',
                    save: createSave('ApplicationProbabilityOfDefault', 'SetApplicationProbabilityOfDefault'),
                    initialValue: result.details.companyLoanRiskValues?.pd,
                },
                {
                    isReadOnly: isReadonly,
                    labelText: 'Application LGD',
                    save: createSave('ApplicationLossGivenDefault', 'SetApplicationLossGivenDefault'),
                    initialValue: result.details.companyLoanRiskValues?.lgd,
                },
            ],
        };
    }

    public showAmortizationPlan(popupTemplate: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();
        if (
            this.configService.isAnyFeatureEnabled([
                'ntech.feature.mortgageloans',
                'ntech.feature.mortgageloans.standard',
            ])
        ) {
            this.c.popup = {
                title: 'Amortization details',
                mortgageLoanAmortizationPlanCreditNr: this.c.creditNr,
            };
            this.modalRef = this.modalService.show(popupTemplate, { class: 'modal-xl', ignoreBackdropClick: true });
        } else {
            this.c.popup = {
                title: 'Amortization plan',
                creditAmortizationPlanCreditNr: this.c.creditNr,
            };
            this.modalRef = this.modalService.show(popupTemplate, { class: 'modal-xl', ignoreBackdropClick: true });
        }
    }

    public getBalanceDebugDetailsUrl() {
        return this.c
            ? this.apiService.getUiGatewayUrl(
                  'nCredit',
                  'api/Credit/BalanceDebugDetails?CreditNr=' + this.c.details.creditNr
              )
            : null;
    }

    public creditUrl(localUrl: string) {
        return this.apiService.getUiGatewayUrl('nCredit', localUrl);
    }

    public formatMortgageLoanInterestRebindMonthCount(monthCount: number) {
        if (!monthCount) {
            return '';
        }
        return monthCount % 12 === 0 ? `fixed ${monthCount / 12} years` : `fixed ${monthCount} months`;
    }
}

class Model {
    creditNr: string;
    details: CreditDetails;
    capitalTransactions: CreditCapitalTransaction[];
    popup?: {
        title: string;
        creditAmortizationPlanCreditNr?: string;
        mortgageLoanAmortizationPlanCreditNr?: string;
    };
    companyLoans?: {
        editableNumbers: CreditNumberEditorInitialData[];
    };
}
