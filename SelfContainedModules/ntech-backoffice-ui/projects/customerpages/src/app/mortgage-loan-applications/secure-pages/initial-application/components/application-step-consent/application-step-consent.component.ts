import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { CustomerPagesEventService } from 'projects/customerpages/src/app/common-services/customerpages-event.service';
import { BehaviorSubject, Subscription } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { MlApplicationModel } from '../../../../services/customer-pages-ml-api.service';
import {
    AutoFillInTestEventName,
    MlApplicationSessionModel,
    NextStepEventName,
} from '../../start-webapplication/start-webapplication.component';
import {
    CustomerPagesConfigService,
    NTechEnum,
} from 'projects/customerpages/src/app/common-services/customer-pages-config.service';

@Component({
    selector: 'application-step-consent',
    templateUrl: './application-step-consent.component.html',
    styles: [
        `
            .glyphicon.glyphicon-big {
                font-size: 50px;
            }
        `,
    ],
})
export class ApplicationStepConsentComponent {
    constructor(
        private fb: UntypedFormBuilder,
        private eventService: CustomerPagesEventService,
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

                let form = this.fb.group({
                    hasGivenConsent: [false, [Validators.required, Validators.requiredTrue]],
                });

                let helper = new FormsHelper(form);

                let enums = this.config.getEnums();
                this.config
                    .fetchMlWebappSettings()
                    .toPromise()
                    .then((res) => {
                        let m = new Model(helper, session, enums.OtherLoanTypes, enums.EmploymentStatuses);
                        m.personalDataPolicyUrl = res.MortgageLoanExternalApplicationSettings['personalDataPolicyUrl'];
                        this.m = m;
                    });
            })
        );
    }

    apply(evt?: Event) {
        evt?.preventDefault();
        //@ts-ignore TODO remove unused locals
        let f = this.m.form;
        let a = this.m.session.application;

        a.Applicants.forEach((a) => (a.HasConsentedToCreditReport = true));

        this.eventService.emitApplicationEvent(NextStepEventName, JSON.stringify({ session: this.m.session }));
    }

    private autoFillInTest() {
        //@ts-ignore TODO remove unused locals
        let f = this.m.form;
    }
}

class Model {
    constructor(
        public form: FormsHelper,
        public session: MlApplicationSessionModel,
        private loanTypes: NTechEnum[],
        private employmentTypes: NTechEnum[]
    ) {
        this.app = session.application;
    }

    // Reduce characters in ui.
    app: MlApplicationModel;
    personalDataPolicyUrl: string;

    // sum(MortgageLoansToSettle[*].CurrentDebtAmount) where ShouldBeSettled = true + ChangeExistingLoan.PaidToCustomerAmount
    getWishedToLoanAmount() {
        if (this.app.Purchase) {
            return this.app.Purchase.ObjectPriceAmount - this.app.Purchase.OwnSavingsAmount;
        } else {
            let currentDebt = this.app.ChangeExistingLoan.MortgageLoansToSettle.map((loan) =>
                loan.ShouldBeSettled ? loan.CurrentDebtAmount : 0
            ).reduce((amount) => {
                return amount;
            });
            return currentDebt + this.app.ChangeExistingLoan.PaidToCustomerAmount;
        }
    }

    getEstimatedValueAmount() {
        return this.app.Purchase?.ObjectPriceAmount ?? this.app.ChangeExistingLoan?.ObjectValueAmount;
    }

    getLoanType(typeCode: string) {
        return this.loanTypes.find((type) => type.Code === typeCode).DisplayName;
    }

    getEmploymentType(typeCode: string) {
        return this.employmentTypes.find((type) => type.Code === typeCode).DisplayName;
    }

    getObjectTypeText() {
        return this.app.ObjectTypeCode === 'seBrf' ? 'Bostadsrättsförening' : 'Fastighet';
    }
}
