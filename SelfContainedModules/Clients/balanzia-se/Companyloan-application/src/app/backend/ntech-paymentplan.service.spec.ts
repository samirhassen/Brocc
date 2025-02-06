import { TestBed } from '@angular/core/testing';
import { NTechPaymentPlanService } from './ntech-paymentplan.service';
import { NTechMath } from './ntech.math';


describe('NTechValidationService', () => {
    beforeEach(() => TestBed.configureTestingModule({}));

    it('should be created', () => {
        const service: NTechPaymentPlanService = TestBed.get(NTechPaymentPlanService);
        expect(service).toBeTruthy();
    });

    it('should yield correct standard payment plan', () => {
        const service: NTechPaymentPlanService = TestBed.get(NTechPaymentPlanService);
        let pp = service.calculatePaymentPlanWithAnnuitiesFromRepaymentTime(15000, 36, 12.56, 150, 5)
        expect(pp.MonthlyCostExcludingFeesAmount).toBe(507.26)

        let pr = 6
        expect(pp.Payments[0].capital).toBeCloseTo(348.69, pr, 'first payment capital')
        expect(pp.Payments[1].capital).toBeCloseTo(352.34, pr, 'second payment capital')
        expect(NTechMath.sum(pp.Payments, x => x.capital)).toBeCloseTo(15150, pr, 'total capital')        
        expect(pp.Payments[pp.Payments.length - 1].capital).toBeCloseTo(501.95, pr, 'last payment capital')
        expect(pp.Payments[pp.Payments.length - 1].totalAmount).toBeCloseTo(512.20, pr, 'last payment total')
        expect(pp.TotalPaidAmount).toBeCloseTo(18441.30, pr, 'total paid amount')
        expect(pp.EffectiveInterstRatePercent.value).toBeCloseTo(14.87, pr, 'effective interest rate')
    });

});
