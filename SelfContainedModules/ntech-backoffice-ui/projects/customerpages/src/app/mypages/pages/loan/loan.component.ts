import { Component, OnInit, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { MlSeAmortizationBasisModel } from 'projects/ntech-components/src/public-api';
import { createMortgagePropertyIdFromCollateralItems } from 'src/app/common-services/ml-standard-functions';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import {
    MyPagesMenuItemCode,
    MypagesShellComponentInitialData,
} from '../../components/mypages-shell/mypages-shell.component';
import { UnpaidNotificationsComponentInitialData } from '../../components/unpaid-notifications/unpaid-notifications.component';

import {
    CapitalTransactionHistory,
    CreditModel,
    InterestHistory,
    LoanAmortizationPlan,
    MyPagesApiService,
} from '../../services/mypages-api.service';
import { MlAmortizationPlanInitialData } from './ml-amortization-plan/ml-amortization-plan.component';

@Component({
    selector: 'np-loan',
    templateUrl: './loan.component.html',
    styleUrls: ['./loan.component.scss'],
})
export class LoanComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private apiService: MyPagesApiService,
        private modalService: BsModalService,
        private config: CustomerPagesConfigService
    ) {}

    public m: Model;
    public modalRef: BsModalRef;

    ngOnInit(): void {
        this.reload(this.route.snapshot.params['creditNr']);
    }

    private reload(creditNr: string) {
        this.m = null;

        this.apiService.fetchCredits(true).then((credits) => {
            let credit = credits.ActiveCredits.find((y) => y.CreditNr == creditNr);
            if (!credit) {
                this.m = new Model(creditNr, this.config.isNTechTest());
                this.m.errorMessage = 'Lånet finns inte';
                return;
            }
            this.apiService.fetchLoanAmortizationPlan(creditNr).then((amortizationPlan) => {
                let m = new Model(creditNr, this.config.isNTechTest());

                m.credit = credit;
                m.unpaidNotificationsInitialData = {
                    isOverview: false,
                    credits: [credit],
                };
                m.amortizationBasisSeData = amortizationPlan.AmortizationBasis;
                m.paidNotifications = credit.Notifications.filter((x) => !x.IsOpen).map((x) => {
                    return {
                        isVisible: false,
                        paidDate: x.ClosedDate,
                        initialAmount: x.InitialAmount,
                    };
                });

                m.showMorePaidNotifications();

                const isMl = this.config.isMortgageLoansStandardEnabled();
                m.amortizationPlan = amortizationPlan;
                m.amortizationBasisPdfUrl = isMl
                    ? '/mortgageloans/api/credit/fetch-amortizationbasis?creditNr=' + creditNr
                    : '';

                this.m = m;
            });
        });
    }

    public getMonthlyPayment() {
        let p = this.m.amortizationPlan;
        return p.UsesAnnuities ? p.AnnuityAmount : p.FixedMonthlyPaymentAmount;
    }

    public showInterestHistoryPopup(popupTemplate: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();

        this.apiService.fetchUnsecuredLoansInterestHistory(this.m.credit.CreditNr).then((x) => {
            this.m.popup = {
                title: 'Räntehistorik',
                interestHistory: x,
            };

            this.showPopup(popupTemplate);
        });
    }

    public showCapitalTransactionsPopup(popupTemplate: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();

        this.apiService.fetchCapitalTransactionHistory(this.m.credit.CreditNr).then((x) => {
            this.m.popup = {
                title: 'Kapitalskuld',
                capitalTransactionHistory: x,
            };

            this.showPopup(popupTemplate);
        });
    }

    public showAmortizationPlanPopup(popupTemplate: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();

        let isMl = this.config.isMortgageLoansStandardEnabled();
        this.m.popup = {
            title: 'Amorteringsplan',
            ulAmortizationPlan: isMl ? null : this.m.amortizationPlan,
            mlAmortizationPlanData: !isMl
                ? null
                : {
                      highlightCreditNr: this.m.credit.CreditNr,
                      amortizationPlan: this.m.amortizationPlan,
                      amortizationBasisSeData: this.m.amortizationBasisSeData,
                  },
        };

        this.showPopup(popupTemplate);
    }

    public formatFixedInterestMonths(monthCount: number) {
        return monthCount % 12 === 0 ? `${monthCount / 12} år` : `${monthCount} månader`;
    }

    public getMlPropertyId() {
        if (!this.m?.credit?.MortgageLoan) {
            return '';
        }
        return createMortgagePropertyIdFromCollateralItems(
            (x) => this.m.credit.MortgageLoan.CollateralStringItems[x],
            true
        );
    }

    private showPopup(popupTemplate: TemplateRef<any>) {
        this.modalRef = this.modalService.show(popupTemplate, { class: 'modal-xl', ignoreBackdropClick: true });
    }

    public getNextFutureNotificationAmount() {
        let futureNotification = this.m.amortizationPlan.AmortizationPlanItems.find(x => x.IsFutureItem && x.EventTypeCode === 'NewNotification');
        return futureNotification
            ? futureNotification.CapitalTransaction + futureNotification.InterestTransaction + futureNotification.InitialFeeTransaction + futureNotification.NotificationFeeTransaction
            : null;
    }

    public hasUnpaidNotifications() {
        return !!this.m.credit.Notifications.find(x => x.IsOpen);
    }
}

class Model {
    public shellInitialData: MypagesShellComponentInitialData;
    constructor(public creditNr: string, public isTest: boolean) {
        this.shellInitialData = {
            activeMenuItemCode: MyPagesMenuItemCode.Loans,
        };
    }

    public errorMessage: string;
    public credit: CreditModel;
    public amortizationBasisSeData: MlSeAmortizationBasisModel;
    public unpaidNotificationsInitialData: UnpaidNotificationsComponentInitialData;
    public paidNotifications: {
        isVisible: boolean;
        paidDate: string;
        initialAmount: number;
    }[];

    public popup: {
        title: string;
        interestHistory?: InterestHistory;
        capitalTransactionHistory?: CapitalTransactionHistory;
        ulAmortizationPlan?: LoanAmortizationPlan;
        mlAmortizationPlanData?: MlAmortizationPlanInitialData;
    };
    public amortizationPlan: LoanAmortizationPlan;
    public amortizationBasisPdfUrl?: string;

    public hasMorePaidNotifications() {
        return !!this.paidNotifications.find((x) => !x.isVisible);
    }

    public showMorePaidNotifications(evt?: Event) {
        evt?.preventDefault();

        let maxCountToShow = 5;
        for (let n of this.paidNotifications) {
            if (!n.isVisible) {
                n.isVisible = true;
                maxCountToShow = maxCountToShow - 1;
            }
            if (maxCountToShow === 0) {
                return;
            }
        }
    }
}
