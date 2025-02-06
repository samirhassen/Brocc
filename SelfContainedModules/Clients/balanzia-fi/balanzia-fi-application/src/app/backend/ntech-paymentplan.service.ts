import { OfferModel } from './api-service';
import { NTechMath } from './ntech.math';
import { Injectable } from '@angular/core';
import { NullableNumber } from './common.types';
import { EffectiveInterestRateCalculation } from './effective-interest-rate.calculator';

@Injectable({
    providedIn: 'root'
})
export class NTechPaymentPlanService {
    constructor() { }

    annuityRoundToDigits: number = 2
    paymentRoundToDigits: number = 2
    monthCountCapEvenIfNotFullyPaid: NullableNumber = new NullableNumber(30 * 12)
    lastMonthCarryOverStrategyPercentOfAnnuity: NullableNumber = new NullableNumber(10)
    lastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount = new NullableNumber(10)
    lastMonthCarryOverStrategyFixedAmount: NullableNumber = null
    effectiveInterestRateRoundToDigits: number = 2

    calculatePaymentPlanWithAnnuitiesFromRepaymentTime(loanAmount: number,
        repaymentTimeInMonths: number,
        interestRatePercent: number,
        capitalizedInitialFeeAmount: number,
        notificationFee: number): PaymentPlan {

        let initialCapitalDebtAmount = this.computeInitialCapitalDebtAmount(loanAmount, capitalizedInitialFeeAmount)
        let annuityAmount = NTechMath.roundToPlaces(this.computeAnnuity(interestRatePercent, initialCapitalDebtAmount, repaymentTimeInMonths), this.annuityRoundToDigits)

        let payments = this.computeMonthlyPayments(annuityAmount, initialCapitalDebtAmount, interestRatePercent, notificationFee, null)        

        return {
            LoanAmount: loanAmount,
            InitialFeeAmount: capitalizedInitialFeeAmount,
            MonthlyFeeAmount: notificationFee,
            MonthlyCostExcludingFeesAmount: annuityAmount,
            MonthlyCostIncludingFeesAmount: annuityAmount + notificationFee,
            RepaymentTimeInMonths: repaymentTimeInMonths,
            RepaymentTimeInYears: Math.ceil(repaymentTimeInMonths / 12),
            NominalInterestRatePercent: interestRatePercent,
            TotalPaidAmount: NTechMath.sum(payments, x => x.totalAmount),
            EffectiveInterstRatePercent: this.calculateEffectiveInterestRatePercent(loanAmount, payments),
            Payments: payments
        }
    }

    private computeInitialCapitalDebtAmount(loanAmount: number, capitalizedInitialFeeAmount: number) {
        return loanAmount + capitalizedInitialFeeAmount
    }

    private computeAnnuity(interestRatePercent: number, initialCapitalDebtAmount: number, repaymentTimeInMonths: number): number {
        if (interestRatePercent < 0.00001) {
            return initialCapitalDebtAmount / repaymentTimeInMonths
        }

        let r = interestRatePercent / 100 / 12;

        let pv = initialCapitalDebtAmount
        let n = repaymentTimeInMonths

        let result = (r * pv / (1 - Math.pow(1 + r, -n)))

        return result
    }

    private getActualAmortizationAmountForMonth(monthNr: number, standardAmortizationAmount: number, remainingCapitalAmount: number): number {
        return Math.min(standardAmortizationAmount, remainingCapitalAmount);
    }

    private shouldCarryOverRemainingCapitalAmount(remainingCapitalAmount: number, annuityAmount?: NullableNumber, fixedMonthlyCapitalAmount?: NullableNumber): boolean {
        if (remainingCapitalAmount == 0)
            return false;

        if ((!annuityAmount && !fixedMonthlyCapitalAmount) || (annuityAmount && fixedMonthlyCapitalAmount))
            throw new Error('Must supply exactly one of annuityAmount and fixedMonthlyCapitalAmount')

        if (annuityAmount) {
            if (this.lastMonthCarryOverStrategyFixedAmount || this.lastMonthCarryOverStrategyPercentOfAnnuity) {
                if (this.lastMonthCarryOverStrategyFixedAmount && this.lastMonthCarryOverStrategyPercentOfAnnuity) {
                    throw new Error('Not implemented')
                }

                let lastMonthCarryOverLimit = this.lastMonthCarryOverStrategyPercentOfAnnuity
                    ? (this.lastMonthCarryOverStrategyPercentOfAnnuity.value / 100 * annuityAmount.value)
                    : this.lastMonthCarryOverStrategyFixedAmount.value

                return remainingCapitalAmount < lastMonthCarryOverLimit
            }
            else {
                return false
            }
        }
        else if (fixedMonthlyCapitalAmount) {
            if (this.lastMonthCarryOverStrategyFixedAmount || this.lastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount) {
                if (this.lastMonthCarryOverStrategyFixedAmount && this.lastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount) {
                    throw new Error('Not implemented')
                }

                let lastMonthCarryOverLimit = this.lastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount
                    ? (this.lastMonthCarryOverStrategyPercentOfFixedMonthlyCapitalAmount.value / 100 * fixedMonthlyCapitalAmount.value)
                    : this.lastMonthCarryOverStrategyFixedAmount.value

                return (remainingCapitalAmount < lastMonthCarryOverLimit)
            }
            else {
                return false
            }
        }
        else {
            throw new Error('Not implemented')
        }
    }

    private computeMonthlyPayments(annuityAmount: number, initialCapitalDebtAmount: number, interestRatePercent: number, notficationFeeAmount: number, initialFeePaidOnFirstNotification?: NullableNumber) {
        let paymentPlan: MonthlyPayment[] = []

        let first = true;
        let remainingAmount = initialCapitalDebtAmount;
        let monthNr = 0;

        while (remainingAmount > 0) {
            monthNr++;

            let interest = NTechMath.roundToPlaces(remainingAmount * interestRatePercent / 100 / 12, this.paymentRoundToDigits)
            let amortization: number = this.getActualAmortizationAmountForMonth(monthNr, annuityAmount - interest, remainingAmount)
            if (amortization < 0) {
                amortization = 0
            }

            remainingAmount -= amortization;
            var shouldCarryOverAmount = this.shouldCarryOverRemainingCapitalAmount(remainingAmount, new NullableNumber(annuityAmount), null);
            if (shouldCarryOverAmount || (this.monthCountCapEvenIfNotFullyPaid && this.monthCountCapEvenIfNotFullyPaid.value == monthNr)) {
                amortization += remainingAmount;
                remainingAmount = 0;
            }

            let p: MonthlyPayment = {
                monthNr: monthNr,
                capital: amortization,
                interest: interest,
                initialFee: first ? (initialFeePaidOnFirstNotification ? initialFeePaidOnFirstNotification.value : 0) : 0,
                monthlyFee: notficationFeeAmount,
                totalAmount: null
            }

            p.totalAmount = p.capital + p.interest + p.initialFee + p.monthlyFee

            paymentPlan.push(p)
            first = false;

            if (monthNr == 500 * 12) {
                throw new Error('Will never be paid with current terms')
            }
        }
        return paymentPlan
    }

    private calculateEffectiveInterestRatePercent(initialPaidToCustomerAmount: number, payments: MonthlyPayment[]) {
        let c = new EffectiveInterestRateCalculation(this.effectiveInterestRateRoundToDigits)

        c.addInitialLoan(initialPaidToCustomerAmount)
        for(let p of payments) {
            c.addPayment(p.totalAmount, p.monthNr)
        }

        return c.calculate()
    }
}

export class MonthlyPayment {
    monthNr: number
    monthlyFee: number
    initialFee: number
    interest: number
    capital: number
    totalAmount: number
}

export class PaymentPlan {
    LoanAmount: number
    RepaymentTimeInMonths: number
    RepaymentTimeInYears: number
    MonthlyCostIncludingFeesAmount: number
    MonthlyCostExcludingFeesAmount: number
    MonthlyFeeAmount: number
    InitialFeeAmount: number
    NominalInterestRatePercent: number
    EffectiveInterstRatePercent: NullableNumber
    TotalPaidAmount: number
    Payments: MonthlyPayment[]
}