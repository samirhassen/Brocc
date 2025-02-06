import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { CustomerPagesValidationService } from 'projects/customerpages/src/app/common-services/customer-pages-validation.service';
import { CustomerPagesEventService } from 'projects/customerpages/src/app/common-services/customerpages-event.service';
import { BehaviorSubject, Subscription } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import {
    AutoFillInTestEventName,
    MlApplicationSessionModel,
    NextStepEventName,
} from '../../start-webapplication/start-webapplication.component';

@Component({
    selector: 'application-step-object',
    templateUrl: './application-step-object.component.html',
    styles: [],
})
export class ApplicationStepObjectComponent {
    constructor(
        private fb: UntypedFormBuilder,
        private eventService: CustomerPagesEventService,
        private validationService: CustomerPagesValidationService
    ) {}

    @Input()
    public session: BehaviorSubject<MlApplicationSessionModel>;

    public m: Model;
    private subs: Subscription[];

    ngOnChanges(changes: SimpleChanges) {
        if (this.subs) {
            for (let sub of this.subs) {
                sub.unsubscribe();
            }
        }
        this.subs = [];

        this.m = null;

        if (!this.session) {
            return;
        }

        this.subs.push(
            this.eventService.applicationEvents.subscribe((x) => {
                if (x.eventCode === AutoFillInTestEventName) {
                    this.autoFillInTest();
                }
            })
        );

        this.subs.push(
            this.session.subscribe((x) => {
                if (!x) {
                    this.m = null;
                    return;
                }

                let f = this.fb.group({
                    objectAddressStreet: ['', [Validators.required]],
                    objectAddressZipcode: ['', [Validators.required]],
                    objectAddressCity: ['', [Validators.required]],
                    objectAddressMunicipality: ['', [Validators.required]],
                    objectOtherMonthlyCostsAmount: ['', [this.validationService.getPositiveIntegerValidator()]],
                    objectLivingArea: ['', [this.validationService.getPositiveIntegerValidator()]],
                    isObjectApartment: ['', [Validators.required]],
                });

                let h = new FormsHelper(f);
                f.valueChanges.subscribe((x) => {
                    let isObjectApartment = h.getValue('isObjectApartment') === 'true';
                    h.ensureControlOnConditionOnly(
                        isObjectApartment,
                        'apartmentNr',
                        () => '',
                        () => [Validators.required]
                    );
                    h.ensureControlOnConditionOnly(
                        isObjectApartment,
                        'objectMonthlyFeeAmount',
                        () => '',
                        () => [this.validationService.getPositiveIntegerValidator()]
                    );
                });

                let isPurchase = !!x.application.Purchase;

                let m = new Model(x, h, [], 1, !!x.application.Purchase, null, this.validationService);

                let shouldMortgageLoansBeSettled: boolean = true;
                if (!isPurchase && x.application.ChangeExistingLoan?.MortgageLoansToSettle) {
                    for (let loan of x.application.ChangeExistingLoan.MortgageLoansToSettle) {
                        shouldMortgageLoansBeSettled = loan.ShouldBeSettled;
                        m.addLoan(
                            {
                                bankName: loan.BankName,
                                currentDebtAmount: this.validationService.formatIntegerForEdit(loan.CurrentDebtAmount),
                                interestRatePercent: this.validationService.formatPositiveDecimalForEdit(
                                    loan.InterestRatePercent
                                ),
                                currentMonthlyAmortizationAmount: this.validationService.formatIntegerForEdit(
                                    loan.CurrentMonthlyAmortizationAmount
                                ),
                            },
                            null
                        );
                    }
                }

                m.shouldMortgageLoansBeSettled = shouldMortgageLoansBeSettled;

                this.m = m;
            })
        );
    }

    apply(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.form;
        let a = this.m.session.application;
        a.ObjectAddressStreet = f.getValue('objectAddressStreet');
        a.ObjectAddressZipcode = f.getValue('objectAddressZipcode');
        a.ObjectAddressCity = f.getValue('objectAddressCity');
        a.ObjectAddressMunicipality = f.getValue('objectAddressMunicipality');
        a.ObjectOtherMonthlyCostsAmount = this.validationService.parseIntegerOrNull(
            f.getValue('objectOtherMonthlyCostsAmount')
        );
        a.ObjectLivingArea = this.validationService.parseIntegerOrNull(f.getValue('objectLivingArea'));
        let isObjectApartment = f.getValue('isObjectApartment') === 'true';
        if (isObjectApartment) {
            a.ObjectTypeCode = 'seBrf';
            a.ObjectMonthlyFeeAmount = this.validationService.parseIntegerOrNull(f.getValue('objectMonthlyFeeAmount'));
            a.SeBrfApartmentNr = f.getValue('apartmentNr');
        } else {
            a.ObjectTypeCode = 'seFastighet';
            a.ObjectMonthlyFeeAmount = null;
        }
        if (!this.m.isPurchase) {
            let mortgageLoansToSettle = [];
            for (let suffix of this.m.loanSuffixes) {
                mortgageLoansToSettle.push({
                    BankName: f.getValue('bankName' + suffix),
                    ShouldBeSettled: this.m.shouldMortgageLoansBeSettled,
                    CurrentMonthlyAmortizationAmount: this.validationService.parseIntegerOrNull(
                        f.getValue('currentMonthlyAmortizationAmount' + suffix)
                    ),
                    InterestRatePercent: this.validationService.parseDecimalOrNull(
                        f.getValue('interestRatePercent' + suffix),
                        false
                    ),
                    CurrentDebtAmount: this.validationService.parseIntegerOrNull(
                        f.getValue('currentDebtAmount' + suffix)
                    ),
                });
            }
            a.ChangeExistingLoan.MortgageLoansToSettle = mortgageLoansToSettle;
        }

        this.eventService.emitApplicationEvent(NextStepEventName, JSON.stringify({ session: this.m.session }));
    }

    private autoFillInTest() {
        let f = this.m.form;
        f.setValue('objectAddressStreet', 'Gatan 1');
        f.setValue('objectAddressZipcode', '111 11');
        f.setValue('objectAddressCity', 'Staden');
        f.setValue('objectAddressMunicipality', 'Kommunen');
        f.setValue('objectOtherMonthlyCostsAmount', '500');
        f.setValue('objectLivingArea', '53');
        f.setValue('isObjectApartment', 'true');
        f.setValue('apartmentNr', 'L1233');
        f.setValue('objectMonthlyFeeAmount', '2700');

        if (this.m.loanSuffixes.length === 0) {
            this.m.addLoan(
                {
                    bankName: 'Banken',
                    currentMonthlyAmortizationAmount: '750',
                    interestRatePercent: '1,5',
                    currentDebtAmount: '155000',
                },
                null
            );
        }
    }
}

class Model {
    constructor(
        public session: MlApplicationSessionModel,
        public form: FormsHelper,
        public loanSuffixes: string[],
        public nextLoanSuffix: number,
        public isPurchase: boolean,
        public shouldMortgageLoansBeSettled: boolean,
        private validationService: CustomerPagesValidationService
    ) {}

    addLoan(
        initialValues: {
            bankName: string;
            currentMonthlyAmortizationAmount: string;
            interestRatePercent: string;
            currentDebtAmount: string;
        },
        evt: Event
    ) {
        evt?.preventDefault();

        let suffix = this.nextLoanSuffix.toString();

        this.nextLoanSuffix = this.nextLoanSuffix + 1;

        let f = this.form;
        f.addControlIfNotExists('bankName' + suffix, initialValues?.bankName, []);
        f.addControlIfNotExists(
            'currentMonthlyAmortizationAmount' + suffix,
            initialValues?.currentMonthlyAmortizationAmount,
            [this.validationService.getPositiveIntegerValidator()]
        );
        f.addControlIfNotExists('interestRatePercent' + suffix, initialValues?.interestRatePercent, [
            this.validationService.getPositiveDecimalValidator(),
        ]);
        f.addControlIfNotExists('currentDebtAmount' + suffix, initialValues?.currentDebtAmount, [
            Validators.required,
            this.validationService.getPositiveIntegerValidator(),
        ]);

        this.loanSuffixes.push(suffix);

        return suffix;
    }

    removeLoan(suffix: string, evt?: Event) {
        evt?.preventDefault();
        this.loanSuffixes.splice(this.loanSuffixes.indexOf(suffix), 1);

        let f = this.form;
        f.form.removeControl('bankName' + suffix);
        f.form.removeControl('currentMonthlyAmortizationAmount' + suffix);
        f.form.removeControl('interestRatePercent' + suffix);
        f.form.removeControl('currentDebtAmount' + suffix);
    }
}
