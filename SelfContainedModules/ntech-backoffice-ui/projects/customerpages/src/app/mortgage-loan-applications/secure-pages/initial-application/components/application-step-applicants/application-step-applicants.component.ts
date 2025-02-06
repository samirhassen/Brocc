import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { CustomerPagesValidationService } from 'projects/customerpages/src/app/common-services/customer-pages-validation.service';
import { CustomerPagesEventService } from 'projects/customerpages/src/app/common-services/customerpages-event.service';
import { BehaviorSubject, Subscription } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { CustomerPagesMortgageLoanApiService } from '../../../../services/customer-pages-ml-api.service';
import {
    AutoFillInTestEventName,
    MlApplicationSessionModel,
    NextStepEventName,
} from '../../start-webapplication/start-webapplication.component';
import { EmploymentFormService } from 'projects/customerpages/src/app/shared-components/services/employment-form.service';

@Component({
    selector: 'application-step-applicants',
    templateUrl: './application-step-applicants.component.html',
    styles: [],
})
export class ApplicationStepApplicantsComponent {
    constructor(
        private fb: UntypedFormBuilder,
        private eventService: CustomerPagesEventService,
        private validationService: CustomerPagesValidationService,
        private apiService: CustomerPagesMortgageLoanApiService,
        private employmentFormService: EmploymentFormService
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
                    mainApplicantEmail: ['', [Validators.required, this.validationService.getEmailValidator()]],
                    mainApplicantPhone: ['', [Validators.required, this.validationService.getPhoneValidator()]],
                    mainApplicantEmployment: ['', [Validators.required]],
                    mainApplicantIncomePerMonthAmount: [
                        '',
                        [Validators.required, this.validationService.getPositiveIntegerValidator()],
                    ],
                    hasCoApplicant: ['false', [Validators.required]],
                });

                let h = new FormsHelper(f);
                f.valueChanges.subscribe((x) => {
                    //Main applicant
                    h.ensureControlOnConditionOnly(
                        this.employmentFormService.isEmployedSinceEmploymentCode(
                            h.getValue('mainApplicantEmployment')
                        ),
                        'mainApplicantEmployedSince',
                        () => '',
                        () => [this.validationService.getDateOnlyValidator()]
                    );
                    h.ensureControlOnConditionOnly(
                        this.employmentFormService.isEmployedToEmploymentCode(h.getValue('mainApplicantEmployment')),
                        'mainApplicantEmployedTo',
                        () => '',
                        () => [this.validationService.getDateOnlyValidator()]
                    );
                    h.ensureControlOnConditionOnly(
                        this.employmentFormService.isEmployerEmploymentCode(h.getValue('mainApplicantEmployment')),
                        'mainApplicantEmployer',
                        () => '',
                        () => []
                    );
                    h.ensureControlOnConditionOnly(
                        this.employmentFormService.isEmployerEmploymentCode(h.getValue('mainApplicantEmployment')),
                        'mainApplicantEmployerPhone',
                        () => '',
                        () => [this.validationService.getPhoneValidator()]
                    );

                    //Co applicant
                    let hasCoApplicant = h.getValue('hasCoApplicant') === 'true';
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant,
                        'coApplicantCivicRegNr',
                        () => '',
                        () => [Validators.required, this.validationService.getCivicRegNrValidator()]
                    );
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant,
                        'coApplicantEmail',
                        () => '',
                        () => [Validators.required, this.validationService.getEmailValidator()]
                    );
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant,
                        'coApplicantPhone',
                        () => '',
                        () => [Validators.required, this.validationService.getPhoneValidator()]
                    );
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant,
                        'coApplicantEmployment',
                        () => '',
                        () => [Validators.required]
                    );
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant,
                        'coApplicantIncomePerMonthAmount',
                        () => '',
                        () => [Validators.required, this.validationService.getPositiveIntegerValidator()]
                    );
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant &&
                            this.employmentFormService.isEmployedSinceEmploymentCode(
                                h.getValue('coApplicantEmployment')
                            ),
                        'coApplicantEmployedSince',
                        () => '',
                        () => [this.validationService.getDateOnlyValidator()]
                    );
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant && this.employmentFormService.isEmployedToEmploymentCode(h.getValue('coApplicantEmployment')),
                        'coApplicantEmployedTo',
                        () => '',
                        () => [this.validationService.getDateOnlyValidator()]
                    );
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant && this.employmentFormService.isEmployerEmploymentCode(h.getValue('coApplicantEmployment')),
                        'coApplicantEmployer',
                        () => '',
                        () => []
                    );
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant && this.employmentFormService.isEmployerEmploymentCode(h.getValue('coApplicantEmployment')),
                        'coApplicantEmployerPhone',
                        () => '',
                        () => [this.validationService.getPhoneValidator()]
                    );
                    h.ensureControlOnConditionOnly(
                        hasCoApplicant,
                        'isCoApplicantPartOfTheHousehold',
                        () => 'true',
                        () => [Validators.required]
                    );
                });

                let employmentCodes = this.employmentFormService.getEmploymentStatuses();
                let m = new Model(h, x, employmentCodes);
                this.m = m;
            })
        );
    }

    apply(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.form;
        let a = this.m.session.application;

        a.Applicants[0].Email = f.getValue('mainApplicantEmail');
        a.Applicants[0].Phone = f.getValue('mainApplicantPhone');
        a.Applicants[0].IsPartOfTheHousehold = true;
        a.Applicants[0].IncomePerMonthAmount = this.validationService.parseIntegerOrNull(
            f.getValue('mainApplicantIncomePerMonthAmount')
        );
        a.Applicants[0].Employment = this.validationService.normalizeString(f.getValue('mainApplicantEmployment'));
        a.Applicants[0].EmployedSince = this.validationService.parseDateOnlyOrNull(
            f.getValue('mainApplicantEmployedSince')
        );
        a.Applicants[0].EmployedTo = this.validationService.parseDateOnlyOrNull(f.getValue('mainApplicantEmployedTo'));
        a.Applicants[0].Employer = this.validationService.normalizeString(f.getValue('mainApplicantEmployer'));
        a.Applicants[0].EmployerPhone = this.validationService.normalizeString(
            f.getValue('mainApplicantEmployerPhone')
        );

        let hasCoApplicant = f.getValue('hasCoApplicant') === 'true';
        if (hasCoApplicant) {
            if (a.Applicants.length < 2) {
                a.Applicants.push({
                    CivicRegNr: null,
                });
                a.Applicants[1].CivicRegNr = f.getValue('coApplicantCivicRegNr');
                a.Applicants[1].Email = f.getValue('coApplicantEmail');
                a.Applicants[1].Phone = f.getValue('coApplicantPhone');
                a.Applicants[1].IsPartOfTheHousehold = f.getValue('isCoApplicantPartOfTheHousehold') === 'true';
                a.Applicants[1].IncomePerMonthAmount = this.validationService.parseIntegerOrNull(
                    f.getValue('coApplicantIncomePerMonthAmount')
                );
                a.Applicants[1].Employment = this.validationService.normalizeString(
                    f.getValue('coApplicantEmployment')
                );
                a.Applicants[1].EmployedSince = this.validationService.parseDateOnlyOrNull(
                    f.getValue('coApplicantEmployedSince')
                );
                a.Applicants[1].EmployedTo = this.validationService.parseDateOnlyOrNull(
                    f.getValue('coApplicantEmployedTo')
                );
                a.Applicants[1].Employer = this.validationService.normalizeString(f.getValue('coApplicantEmployer'));
                a.Applicants[1].EmployerPhone = this.validationService.normalizeString(
                    f.getValue('coApplicantEmployerPhone')
                );
            }
        }
        this.eventService.emitApplicationEvent(NextStepEventName, JSON.stringify({ session: this.m.session }));
    }

    private autoFillInTest() {
        let f = this.m.form;

        this.apiService.generateTestPersons(2, true, true).then((x) => {
            let a1 = x.Persons[0].Properties;

            f.setValue('mainApplicantEmail', a1.email);
            f.setValue('mainApplicantPhone', a1.phone);
            f.setValue('mainApplicantIncomePerMonthAmount', '37500');

            f.setValue('mainApplicantEmployment', 'full_time');
            f.setValue('mainApplicantEmployedSince', '2017-03-15');
            f.setValue('mainApplicantEmployedTo', '2021-09-12');
            f.setValue('mainApplicantEmployer', 'Employer AB');
            f.setValue('mainApplicantEmployerPhone', '08 - 123 654');

            let a2 = x.Persons[1].Properties;
            f.setValue('hasCoApplicant', 'true');
            f.setValue('coApplicantCivicRegNr', a2.civicRegNr);
            f.setValue('coApplicantEmail', a2.email);
            f.setValue('coApplicantPhone', a2.phone);
            f.setValue('coApplicantIncomePerMonthAmount', '12000');
            f.setValue('isCoApplicantPartOfTheHousehold', 'true');

            f.setValue('coApplicantEmployment', 'student');
            f.setValue('coApplicantEmployedSince', '2015-04-21');
            f.setValue('coApplicantEmployedTo', '2021-12-19');
        });
    }
}

class Model {
    constructor(
        public form: FormsHelper,
        public session: MlApplicationSessionModel,
        public employmentCodes: { Code: string; DisplayName: string }[]
    ) {}
}
