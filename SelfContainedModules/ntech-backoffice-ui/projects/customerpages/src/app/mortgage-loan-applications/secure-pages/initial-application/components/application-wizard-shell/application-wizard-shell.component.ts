import { Component, Input, SimpleChanges } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { MlApplicationSessionModel } from '../../start-webapplication/start-webapplication.component';

@Component({
    selector: 'application-wizard-shell',
    templateUrl: './application-wizard-shell.component.html',
    styles: [],
})
export class ApplicationWizardShellComponent {
    constructor() {}

    @Input()
    public initialData: ApplicationWizardShellInitialData;

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

        if (!this.initialData) {
            return;
        }

        let i = this.initialData;

        this.subs.push(
            i.session.subscribe((x) => {
                if (x) {
                    let civicRegNr = x?.application?.Applicants[0]?.CivicRegNr;
                    let firstName = x?.application?.Applicants[0]?.FirstName;
                    this.m = new Model(i.activeStepNr, firstName, civicRegNr, x);
                } else {
                    this.m = null;
                }
            })
        );
    }
}

export class ApplicationWizardShellInitialData {
    constructor(public activeStepNr: number, public session: Observable<MlApplicationSessionModel>) {}
}

class Model {
    constructor(
        public stepNr: number,
        public firstName: string,
        public civicRegNr: string,
        private session: MlApplicationSessionModel
    ) {}

    public isPurchase() {
        let a = this.session.application;
        return !!a.Purchase;
    }

    public isAdditionalLoan() {
        let a = this.session.application;
        return (
            !a.Purchase &&
            a.ChangeExistingLoan &&
            a.ChangeExistingLoan.MortgageLoansToSettle &&
            a.ChangeExistingLoan.MortgageLoansToSettle.length > 0 &&
            !a.ChangeExistingLoan.MortgageLoansToSettle[0].ShouldBeSettled
        );
    }

    public isMoveLoan() {
        let a = this.session.application;
        return (
            !a.Purchase &&
            a.ChangeExistingLoan &&
            a.ChangeExistingLoan.MortgageLoansToSettle &&
            a.ChangeExistingLoan.MortgageLoansToSettle.length > 0 &&
            a.ChangeExistingLoan.MortgageLoansToSettle[0].ShouldBeSettled
        );
    }
}
