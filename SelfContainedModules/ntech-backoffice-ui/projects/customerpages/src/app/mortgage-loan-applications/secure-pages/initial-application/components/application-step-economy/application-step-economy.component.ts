import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import {
    CustomerPagesConfigService,
    NTechEnum,
} from 'projects/customerpages/src/app/common-services/customer-pages-config.service';
import { CustomerPagesValidationService } from 'projects/customerpages/src/app/common-services/customer-pages-validation.service';
import { CustomerPagesEventService } from 'projects/customerpages/src/app/common-services/customerpages-event.service';
import { BehaviorSubject, Subscription } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { Randomizer } from 'src/app/common-services/randomizer';
import {
    AutoFillInTestEventName,
    MlApplicationSessionModel,
    NextStepEventName,
} from '../../start-webapplication/start-webapplication.component';

@Component({
    selector: 'application-step-economy',
    templateUrl: './application-step-economy.component.html',
    styles: [],
})
export class ApplicationStepEconomyComponent {
    constructor(
        private fb: UntypedFormBuilder,
        private eventService: CustomerPagesEventService,
        private validationService: CustomerPagesValidationService,
        private config: CustomerPagesConfigService
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
            this.session.subscribe((session) => {
                if (!session) {
                    this.m = null;
                    return;
                }

                // These will always exist.
                let baseForm = this.fb.group({
                    hasOtherMortgageLoans: [null, [Validators.required]],
                    hasOtherNonMortgageLoans: [null, [Validators.required]],
                    internalChildCount: [0, []],
                });

                let baseHelper = new FormsHelper(baseForm);
                baseForm.valueChanges.subscribe((x) => {
                    // Add some fields based on other values on the form.
                    baseHelper.ensureControlOnConditionOnly(
                        baseHelper.getValue('internalChildCount') > 0,
                        'childBenefitAmount',
                        () => '',
                        () => [Validators.required, this.validationService.getPositiveIntegerValidator()]
                    );
                    baseHelper.ensureControlOnConditionOnly(
                        baseHelper.getValue('internalChildCount') > 0,
                        'sharedCustodyAndUpkeep',
                        () => false,
                        () => []
                    );
                    baseHelper.ensureControlOnConditionOnly(
                        baseHelper.getValue('sharedCustodyAndUpkeep') === true,
                        'outgoingChildSupportAmount',
                        () => '',
                        () => [Validators.required, this.validationService.getPositiveIntegerValidator()]
                    );
                    baseHelper.ensureControlOnConditionOnly(
                        baseHelper.getValue('sharedCustodyAndUpkeep') === true,
                        'incomingChildSupportAmount',
                        () => '',
                        () => [Validators.required, this.validationService.getPositiveIntegerValidator()]
                    );

                    let hasOtherMortgageLoans = baseHelper.getValue('hasOtherMortgageLoans');
                    if (hasOtherMortgageLoans === 'true' && this.m.mortgageLoanGroupNames?.length === 0) {
                        let newGroupName = this.addMortgageLoanFormGroup(this.m.form, 1);
                        if (newGroupName !== null) this.m.mortgageLoanGroupNames.push(newGroupName);
                        this.m.takeMortgageLoanSuffix();
                    } else if (hasOtherMortgageLoans === 'false') {
                        this.removeMortgageLoan(true);
                    }

                    let hasOtherNonMortgageLoans = baseHelper.getValue('hasOtherNonMortgageLoans');
                    if (hasOtherNonMortgageLoans === 'true' && this.m.otherLoanGroupNames.length === 0) {
                        let newGroupName = this.addOtherLoanFormGroup(this.m.form, 1);
                        if (newGroupName !== null) this.m.otherLoanGroupNames.push(newGroupName);
                        this.m.takeNonMortgageLoanSuffix();
                    } else if (hasOtherNonMortgageLoans === 'false') {
                        this.removeOtherLoan(true);
                    }
                });
                let loanTypes = this.config.getEnums().OtherLoanTypes;
                let m = new Model(baseHelper, session, loanTypes);
                this.m = m;
            })
        );
    }

    private addMortgageLoanFormGroup(form: FormsHelper, suffix: number): string {
        let formGroupName = 'mortgageLoanGroup' + suffix.toString();
        let addedOrNull = form.addGroupIfNotExists(formGroupName, [
            {
                controlName: 'loanType',
                initialValue: 'mortgage',
                validators: [],
            },
            {
                controlName: 'monthlyCostAmount',
                initialValue: null,
                validators: [this.validationService.getPositiveIntegerValidator()],
            },
            {
                controlName: 'currentDebtAmount',
                initialValue: null,
                validators: [Validators.required, this.validationService.getPositiveIntegerValidator()],
            },
        ]);
        return addedOrNull ? formGroupName : null;
    }

    public addMortgageLoan() {
        let newGroupName = this.addMortgageLoanFormGroup(this.m.form, this.m.takeMortgageLoanSuffix());
        // To protect against label/input triggering change twice from radiobuttons.
        if (newGroupName !== null) this.m.mortgageLoanGroupNames.push(newGroupName);
    }

    public removeMortgageLoan(removeAll: boolean, groupName?: string) {
        let removeSingle = (group: string) => {
            this.m.mortgageLoanGroupNames.splice(this.m.mortgageLoanGroupNames.indexOf(group), 1);
            this.m.form.form.removeControl(group);
        };

        if (removeAll) {
            this.m.mortgageLoanGroupNames.every((x) => removeSingle(x));
        } else if (groupName !== null) {
            removeSingle(groupName);
        }
    }

    private addOtherLoanFormGroup(form: FormsHelper, suffix: number): string {
        let formGroupName = 'otherLoanGroup' + suffix.toString();
        let addedOrNull = form.addGroupIfNotExists(formGroupName, [
            {
                controlName: 'loanType',
                initialValue: null,
                validators: [],
            },
            {
                controlName: 'monthlyCostAmount',
                initialValue: null,
                validators: [this.validationService.getPositiveIntegerValidator()],
            },
            {
                controlName: 'currentDebtAmount',
                initialValue: null,
                validators: [Validators.required, this.validationService.getPositiveIntegerValidator()],
            },
        ]);
        return addedOrNull ? formGroupName : null;
    }

    public addOtherLoan() {
        let newGroupName = this.addOtherLoanFormGroup(this.m.form, this.m.takeNonMortgageLoanSuffix());
        // To protect against label/input triggering change twice from radiobuttons.
        if (newGroupName !== null) this.m.otherLoanGroupNames.push(newGroupName);
    }

    public removeOtherLoan(removeAll: boolean, groupName?: string) {
        let removeSingle = (group: string) => {
            this.m.otherLoanGroupNames.splice(this.m.otherLoanGroupNames.indexOf(group), 1);
            this.m.form.form.removeControl(group);
        };

        if (removeAll) {
            this.m.otherLoanGroupNames.forEach(x => removeSingle(x));
        } else if (groupName !== null) {
            removeSingle(groupName);
        }
    }

    private addChildFormGroup(form: FormsHelper, index: number): string {
        let formGroupName = 'childGroup' + (index + 1).toString();
        form.setValue('internalChildCount', form.getValue('internalChildCount') + 1);
        form.addGroupIfNotExists(formGroupName, [
            {
                controlName: 'ageInYears',
                initialValue: null,
                validators: [this.validationService.getPositiveIntegerWithBoundsValidator(0, 200)],
            },
            {
                controlName: 'sharedCustody',
                initialValue: null,
                validators: [],
            },
        ]);
        return formGroupName;
    }

    apply(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.form;
        let a = this.m.session.application;
        let val = this.validationService;

        a.ChildBenefitAmount = val.parseIntegerOrNull(f.getValue('childBenefitAmount'));
        a.OutgoingChildSupportAmount = val.parseIntegerOrNull(f.getValue('outgoingChildSupportAmount'));
        a.IncomingChildSupportAmount = val.parseIntegerOrNull(f.getValue('incomingChildSupportAmount'));

        if (this.m.childrenGroupNames.length > 0) {
            let children = [];
            for (let group of this.m.childrenGroupNames) {
                children.push({
                    Exists: true,
                    AgeInYears: val.parseIntegerOrNull(f.getFormGroupValue(group, 'ageInYears')),
                    SharedCustody: f.getFormGroupValue(group, 'sharedCustody') === 'true',
                });
            }
            a.HouseholdChildren = children;
        }

        let otherLoans = [];
        if (this.m.mortgageLoanGroupNames.length > 0) {
            for (let group of this.m.mortgageLoanGroupNames) {
                otherLoans.push({
                    LoanType: 'mortgage',
                    MonthlyCostAmount: val.parseIntegerOrNull(f.getFormGroupValue(group, 'monthlyCostAmount')),
                    CurrentDebtAmount: val.parseIntegerOrNull(f.getFormGroupValue(group, 'currentDebtAmount')),
                });
            }
        }

        if (this.m.otherLoanGroupNames.length > 0) {
            for (let group of this.m.otherLoanGroupNames) {
                otherLoans.push({
                    LoanType: f.getFormGroupValue(group, 'loanType'),
                    MonthlyCostAmount: val.parseIntegerOrNull(f.getFormGroupValue(group, 'monthlyCostAmount')),
                    CurrentDebtAmount: val.parseIntegerOrNull(f.getFormGroupValue(group, 'currentDebtAmount')),
                });
            }
        }

        a.LoansToSettle = otherLoans;

        this.eventService.emitApplicationEvent(NextStepEventName, JSON.stringify({ session: this.m.session }));
    }

    addChild() {
        let currentCount = this.m.childrenGroupNames?.length ?? 0;
        let newGroupName = this.addChildFormGroup(this.m.form, currentCount);
        this.m.childrenGroupNames.push(newGroupName);
    }

    removeChild() {
        let indexToRemove = this.m.childrenGroupNames.length - 1;
        this.m.form.setValue('internalChildCount', indexToRemove.toString());
        let groupNameToRemove = this.m.childrenGroupNames[indexToRemove];
        this.m.childrenGroupNames.splice(indexToRemove, 1);
        this.m.form.form.removeControl(groupNameToRemove);
    }

    private autoFillInTest() {
        let f = this.m.form;

        let nrOfChildren = Randomizer.anyOf([0, 0, 0, 1, 2]);
        if (nrOfChildren > 0) {
            [...Array(nrOfChildren)].forEach((_) => this.addChild());
            let sharedCustody = Randomizer.anyOf([true, false]);
            this.m.childrenGroupNames.map((groupName, _) => {
                f.setValue2(groupName, 'ageInYears', Randomizer.anyOf(['1', '3', '5', '7', '9']));
                f.setValue2(groupName, 'sharedCustody', Randomizer.anyOf(['true', 'false']));
            });

            f.setValue('childBenefitAmount', Randomizer.anyOf(['500', '1000', '1500']));

            if (sharedCustody) {
                f.setValue('sharedCustodyAndUpkeep', sharedCustody);
                f.setValue('outgoingChildSupportAmount', Randomizer.anyOf(['500', '1000', '1500']));
                f.setValue('incomingChildSupportAmount', Randomizer.anyOf(['500', '1000', '1500']));
            }
        }

        let nrOfMortgageLoans = Randomizer.anyOf([0, 0, 0, 1, 2]);
        if (nrOfMortgageLoans > 0) {
            [...Array(nrOfMortgageLoans)].forEach((_) => this.addMortgageLoan());
            this.m.mortgageLoanGroupNames.map((groupName, _) => {
                f.setValue2(
                    groupName,
                    'currentDebtAmount',
                    Randomizer.anyEvenNumberBetween(100000, 2500000, 10000).toString()
                );
                f.setValue2(
                    groupName,
                    'monthlyCostAmount',
                    Randomizer.anyEvenNumberBetween(1500, 8000, 250).toString()
                );
            });
        }
        f.setValue('hasOtherMortgageLoans', nrOfMortgageLoans > 0 ? 'true' : 'false');

        let nrNonMortgageLoans = Randomizer.anyOf([0, 1, 2]);
        if (nrNonMortgageLoans > 0) {
            [...Array(nrNonMortgageLoans)].forEach((_) => this.addOtherLoan());
            this.m.otherLoanGroupNames.map((groupName, _) => {
                f.setValue2(
                    groupName,
                    'currentDebtAmount',
                    Randomizer.anyEvenNumberBetween(15000, 200000, 1000).toString()
                );
                f.setValue2(groupName, 'monthlyCostAmount', Randomizer.anyEvenNumberBetween(500, 4500, 100).toString());
                f.setValue2(groupName, 'loanType', Randomizer.anyOf(this.m.otherLoanTypes().map((val, _) => val.Code)));
            });
        }
        f.setValue('hasOtherNonMortgageLoans', nrNonMortgageLoans > 0 ? 'true' : 'false');
    }
}

class Model {
    constructor(public form: FormsHelper, public session: MlApplicationSessionModel, private loanTypes: NTechEnum[]) {
        this.childrenGroupNames = [];
        this.mortgageLoanGroupNames = [];
        this.otherLoanGroupNames = [];
        this.nextNonMortgageLoanSuffix = 1;
        this.nextMortgageLoanSuffix = 1;
    }

    otherLoanTypes = () => this.loanTypes.filter((type) => type.Code !== 'mortgage');

    childrenGroupNames: string[];
    mortgageLoanGroupNames: string[];
    otherLoanGroupNames: string[];

    private nextNonMortgageLoanSuffix: number;
    private nextMortgageLoanSuffix: number;

    takeNonMortgageLoanSuffix() {
        let toReturn = this.nextNonMortgageLoanSuffix;
        this.nextNonMortgageLoanSuffix += 1;
        return toReturn;
    }
    takeMortgageLoanSuffix() {
        let toReturn = this.nextMortgageLoanSuffix;
        this.nextMortgageLoanSuffix += 1;
        return toReturn;
    }
}
