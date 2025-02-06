import { NullableNumber } from '../common.types';
import { NTechMath } from './ntech.math';

export function computeSingleNotificationEffectiveInterestRate(effectiveInterestRateRoundToDigits: number, initialPaidToCustomerAmount: number, singleNotificationAmount: number, repaymentTimeInDays: number) : NullableNumber
{
    try
    {
        //X = (D / K) ^ (1 / s) - 1
        var fraction = Math.pow(singleNotificationAmount / initialPaidToCustomerAmount, 365 / repaymentTimeInDays) - 1;
        return new NullableNumber(NTechMath.roundToPlaces(100 * fraction, effectiveInterestRateRoundToDigits));
    }
    catch
    {
        return null;
    }
}

export class EffectiveInterestRateCalculation {
    private loans: number[][] = [];
    private payments: number[][] = [];

    constructor(private effectiveInterestRateRoundToDigits: number) {}

    private getTimeFromMonthNr(monthNr: number): number {
        if (monthNr <= Number.EPSILON) {
            throw new Error('monthNr should start at 1');
        }
        return monthNr / 12;
    }

    addAdditionalLoan(amount: number, monthNr: number) {
        this.loans.push([amount, this.getTimeFromMonthNr(monthNr)]);
    }

    addInitialLoan(amount: number) {
        this.loans.push([amount, 0]);
    }

    addPayment(amount: number, monthNr: number) {
        this.payments.push([amount, this.getTimeFromMonthNr(monthNr)]);
    }

    calculate(): NullableNumber {
        if (
            this.loans.length == 1 &&
            this.payments.length == 1 &&
            NTechMath.equals(this.loans[0][0], this.payments[0][0])
        ) {
            //This really has not rate as the customer is actually making money here so if anything it should be negative but zero
            //seems way less likely to cause issues in practice.
            return new NullableNumber(0);
        }

        //See: https://sv.wikipedia.org/wiki/Effektiv_r%C3%A4nta
        let f: (x: number) => number = (x) => {
            let sum = 0;
            for (let loan of this.loans) {
                sum += loan[0] * Math.pow(1 + x, -loan[1]);
            }
            for (let payment of this.payments) {
                sum -= payment[0] * Math.pow(1 + x, -payment[1]);
            }
            return sum;
        };

        let root = this.findRootUsingBrentsMethod(f, 0, 100000, 1e-8);

        if (!root) {
            return null;
        }

        let rate = NTechMath.roundToPlaces(root.value * 100, this.effectiveInterestRateRoundToDigits);

        return new NullableNumber(rate);
    }

    private findRootUsingBrentsMethod(
        f: (x: number) => number,
        left: number,
        right: number,
        tolerance: number
    ): NullableNumber {
        let iterationsUsed: number = 0;
        //@ts-ignore TODO remove unused locals
        let errorEstimate: number;
        let maxIterations: number = 75;

        if (tolerance <= 0.0 + Number.EPSILON) {
            throw new Error('Tolerance must be positive');
        }

        errorEstimate = Number.MAX_SAFE_INTEGER;

        // Implementation and notation based on Chapter 4 in
        // "Algorithms for Minimization without Derivatives"
        // by Richard Brent.

        let c: number,
            d: number,
            e: number,
            fa: number,
            fb: number,
            fc: number,
            tol: number,
            m: number,
            p: number,
            q: number,
            r: number,
            s: number;

        // set up aliases to match Brent's notation
        let a = left;
        let b = right;
        let t = tolerance;
        iterationsUsed = 0;

        fa = f(a);
        fb = f(b);

        if (fa * fb + Number.EPSILON > 0.0) {
            return null;
        }

        let skipInt = false;
        while (true) {
            if (!skipInt) {
                c = a;
                fc = fa;
                d = e = b - a;
            }
            if (Math.abs(fc) < Math.abs(fb) + Number.EPSILON) {
                a = b;
                b = c;
                c = a;
                fa = fb;
                fb = fc;
                fc = fa;
            }

            iterationsUsed++;

            tol = 2.0 * t * Math.abs(b) + t;
            errorEstimate = m = 0.5 * (c - b);
            if (Math.abs(m) + Number.EPSILON > tol && !NTechMath.equals(fb, 0.0)) {
                // exact comparison with 0 is OK here
                // See if bisection is forced
                if (Math.abs(e) < tol + Number.EPSILON || Math.abs(fa) <= Math.abs(fb) + Number.EPSILON) {
                    d = e = m;
                } else {
                    s = fb / fa;
                    if (NTechMath.equals(a, c)) {
                        // linear interpolation
                        p = 2.0 * m * s;
                        q = 1.0 - s;
                    } else {
                        // Inverse quadratic interpolation
                        q = fa / fc;
                        r = fb / fc;
                        p = s * (2.0 * m * q * (q - r) - (b - a) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }
                    if (p + Number.EPSILON > 0.0) q = -q;
                    else p = -p;
                    s = e;
                    e = d;
                    if (
                        2.0 * p < 3.0 * m * q - Math.abs(tol * q) + Number.EPSILON &&
                        p < Math.abs(0.5 * s * q) + Number.EPSILON
                    )
                        d = p / q;
                    else d = e = m;
                }
                a = b;
                fa = fb;
                if (Math.abs(d) + Number.EPSILON > tol) b += d;
                else if (m + Number.EPSILON > 0.0) b += tol;
                else b -= tol;
                if (iterationsUsed == maxIterations) return null;

                fb = f(b);
                if (
                    (fb + Number.EPSILON > 0.0 && fc + Number.EPSILON > 0.0) ||
                    (fb <= 0.0 + Number.EPSILON && fc <= 0.0 + Number.EPSILON)
                )
                    skipInt = false;
                else skipInt = true;
            } else return new NullableNumber(b);
        }
    }
}
