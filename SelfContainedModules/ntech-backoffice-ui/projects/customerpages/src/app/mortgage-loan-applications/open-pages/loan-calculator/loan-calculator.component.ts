import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import * as moment from 'moment';
import { Dictionary, generateUniqueId } from 'src/app/common.types';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import { CustomerPagesValidationService } from '../../../common-services/customer-pages-validation.service';
import { CustomerPagesEventService } from '../../../common-services/customerpages-event.service';
import {
    getMlApplicationSessionModelStorageKey,
    MlApplicationSessionModel,
} from '../../secure-pages/initial-application/start-webapplication/start-webapplication.component';
import { CalculatorAdditionalLoanData } from './calculator-tab-additionalloan/calculator-tab-additionalloan.component';
import { CalculatorMoveData } from './calculator-tab-move/calculator-tab-move.component';
import { CalculatorPurchaseData } from './calculator-tab-purchase/calculator-tab-purchase.component';

@Component({
    selector: 'np-loan-calculator',
    templateUrl: './loan-calculator.component.html',
    styles: [
        `
            #summary {
                background-color: #023365;
                color: white;
                border-radius: 6px;
                padding: 12px;
                margin-top: 30px;
            }
            div.notice {
                color: red;
            }
        `,
    ],
})
export class LoanCalculatorComponent implements OnInit {
    constructor(
        private configService: CustomerPagesConfigService,
        private changeDetector: ChangeDetectorRef,
        private eventService: CustomerPagesEventService,
        private validationService: CustomerPagesValidationService
    ) {}

    public m: Model;

    ngOnInit(): void {
        this.configService
            .fetchMlWebappSettings()
            .toPromise()
            .then((x) => {
                let settings = new MortgageLoanWebappSettings(
                    x.MortgageLoanExternalApplicationSettings,
                    this.validationService
                );
                let startTab = settings.IsPurchaseTabActive
                    ? 'purchase'
                    : settings.IsAdditionalLoanTabActive
                    ? 'additionalloan'
                    : settings.IsMoveTabActive
                    ? 'move'
                    : null;

                this.m = new Model(startTab, settings);
                this.calculateSummary();
            });
    }

    apply(evt?: Event) {
        evt?.preventDefault();
        let sessionId = generateUniqueId(10);
        let session: MlApplicationSessionModel = {
            sessionId: sessionId,
            creationDate: moment().toISOString(),
            application: {},
        };
        let m = this.m;
        if (m.currentTabName === 'purchase') {
            session.application.Purchase = {
                ObjectPriceAmount: m.purchaseData.objectPriceAmount,
                OwnSavingsAmount: m.purchaseData.ownSavingsAmount,
            };
        } else if (m.currentTabName === 'move') {
            session.application.ChangeExistingLoan = {
                ObjectValueAmount: m.moveData.objectValueAmount,
                PaidToCustomerAmount: m.moveData.paidToCustomerAmount,
                MortgageLoansToSettle: [
                    {
                        ShouldBeSettled: true,
                        CurrentDebtAmount: m.moveData.currentDebtAmount,
                    },
                ],
            };
        } else if (m.currentTabName === 'additionalloan') {
            session.application.ChangeExistingLoan = {
                ObjectValueAmount: m.additionalLoanData.objectValueAmount,
                PaidToCustomerAmount: m.additionalLoanData.paidToCustomerAmount,
                MortgageLoansToSettle: [
                    {
                        ShouldBeSettled: false,
                        CurrentDebtAmount: m.additionalLoanData.currentDebtAmount,
                    },
                ],
            };
        }
        localStorage.setItem(getMlApplicationSessionModelStorageKey(sessionId), JSON.stringify(session));
        let loginUrl = this.configService.getLoginUrl('ContinueMortgageLoanApplication', sessionId);

        this.eventService.isLoading.next(true);
        document.location.href = loginUrl;
    }

    private calculateSummary() {
        let wishToBorrowAmount = this.calculateWishToBorrowAmount();
        let loanToValue = this.calculateLtv(wishToBorrowAmount);
        let amortizationRatePercent = this.calculateAmortizationRatePercent(loanToValue);

        let exampleInterest = this.m.settings.ExampleInterestRatePercent; // Get from ClientConfiguration
        let interestPayment = (exampleInterest * wishToBorrowAmount) / 100 / 12;
        let amortizationPayment = (amortizationRatePercent * wishToBorrowAmount) / 100 / 12;
        let monthlyCost = interestPayment + amortizationPayment;

        this.m.summary = {
            wishToBorrowAmount: wishToBorrowAmount,
            amortizationRatePercent: amortizationRatePercent,
            exampleInterestRate: exampleInterest,
            loanToValueAmount: loanToValue,
            monthlyCostAmount: monthlyCost,
        };
    }

    private calculateAmortizationRatePercent(ltv: number) {
        if (ltv > 70) return 2;
        if (ltv > 50 && ltv <= 70) return 1;
        else return 0;
    }

    private calculateWishToBorrowAmount(): number {
        if (this.m?.purchaseData) {
            return (this.m?.purchaseData?.objectPriceAmount || 0) - (this.m?.purchaseData?.ownSavingsAmount || 0);
        } else if (this.m?.moveData) {
            return (this.m?.moveData?.currentDebtAmount || 0) + (this.m?.moveData?.paidToCustomerAmount || 0);
        } else {
            return this.m?.additionalLoanData?.paidToCustomerAmount || 0;
        }
    }

    private calculateLtv(wishToBorrowAmount: number): number {
        let purch = this.m?.purchaseData;
        let addi = this.m?.additionalLoanData;
        let move = this.m?.moveData;

        if (purch) {
            return 100 * (wishToBorrowAmount / (purch?.objectPriceAmount || 0));
        } else if (addi) {
            return 100 * ((wishToBorrowAmount + (addi?.currentDebtAmount || 0)) / (addi?.objectValueAmount || 0));
        } else {
            return 100 * (wishToBorrowAmount / (move?.objectValueAmount || 0));
        }
    }

    onPurchaseDataChanged(data: CalculatorPurchaseData) {
        this.m.purchaseData = data;
        this.m.additionalLoanData = null;
        this.m.moveData = null;
        this.calculateSummary();
        this.changeDetector.detectChanges();
    }

    onCalculatorMoveDataChanged(data: CalculatorMoveData) {
        this.m.moveData = data;
        this.m.purchaseData = null;
        this.m.additionalLoanData = null;
        this.calculateSummary();
        this.changeDetector.detectChanges();
    }

    onCalculatorAdditionalLoanDataChanged(data: CalculatorAdditionalLoanData) {
        this.m.additionalLoanData = data;
        this.m.purchaseData = null;
        this.m.moveData = null;
        this.calculateSummary();
        this.changeDetector.detectChanges();
    }

    setActiveTab(tabName: string, evt?: Event) {
        evt?.preventDefault();

        this.m.currentTabName = tabName;
    }
}

class Model {
    constructor(public currentTabName: string, public settings: MortgageLoanWebappSettings) {}

    purchaseData?: CalculatorPurchaseData;
    moveData?: CalculatorMoveData;
    additionalLoanData?: CalculatorAdditionalLoanData;
    summary?: Summary;

    isValid() {
        let validLtv = this.summary?.loanToValueAmount <= this.settings.MaxLoanToValuePercent;
        let validTabData =
            (this.currentTabName === 'purchase' && this.purchaseData?.isValid) ||
            (this.currentTabName === 'move' && this.moveData?.isValid) ||
            (this.currentTabName === 'additionalloan' && this.additionalLoanData?.isValid);

        return {
            valid: validTabData && validLtv,
            loanToValueAmountIsValod: validLtv,
        };
    }
}

class Summary {
    wishToBorrowAmount: number;
    monthlyCostAmount: number;
    exampleInterestRate: number;
    loanToValueAmount: number;
    amortizationRatePercent: number;
}

export class MortgageLoanWebappSettings {
    constructor(settings: Dictionary<string>, validationService: CustomerPagesValidationService) {
        this.ExampleInterestRatePercent = validationService.parsePositiveDecimalOrNull(
            settings['exampleInterestRatePercent']
        );
        this.IsPurchaseTabActive = settings['isPurchaseTabActive'] == 'true';
        this.IsMoveTabActive = settings['isMoveTabActive'] == 'true';
        this.IsAdditionalLoanTabActive = settings['isAdditionalLoanTabActive'] == 'true';
        this.MaxLoanToValuePercent = validationService.parseInteger(settings['maxLoanToValuePercent']);

        this.MaxCurrentMortgageLoanAmount = validationService.parseInteger(settings['maxCurrentMortgageLoanAmount']);
        this.MinCurrentMortgageLoanAmount = validationService.parseInteger(settings['minCurrentMortgageLoanAmount']);
        this.MaxEstimatedValue = validationService.parseInteger(settings['maxEstimatedValue']);
        this.MinEstimatedValue = validationService.parseInteger(settings['minEstimatedValue']);
        this.MaxCashAmount = validationService.parseInteger(settings['maxCashAmount']);
        this.MinCashAmount = validationService.parseInteger(settings['minCashAmount']);
        this.MaxAdditionalLoanAmount = validationService.parseInteger(settings['maxAdditionalLoanAmount']);
        this.MinAdditionalLoanAmount = validationService.parseInteger(settings['minAdditionalLoanAmount']);
    }

    public ExampleInterestRatePercent: number;
    public IsPurchaseTabActive: boolean;
    public IsMoveTabActive: boolean;
    public IsAdditionalLoanTabActive: boolean;
    public MaxLoanToValuePercent: number;

    public MaxCurrentMortgageLoanAmount: number;
    public MinCurrentMortgageLoanAmount: number;
    public MaxEstimatedValue: number;
    public MinEstimatedValue: number;
    public MaxCashAmount: number;
    public MinCashAmount: number;
    public MaxAdditionalLoanAmount: number;
    public MinAdditionalLoanAmount: number;
}
