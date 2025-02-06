import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PreApplicationStoredDataModel, UlStandardPreApplicationService } from '../../services/pre-application.service';
import { CustomerPagesConfigService, NTechEnum } from '../../../common-services/customer-pages-config.service';
import { CustomerpagesShellInitialData } from '../../../shared-components/customerpages-shell/customerpages-shell.component';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { CustomerPagesValidationService } from '../../../common-services/customer-pages-validation.service';
import { Dictionary } from 'src/app/common.types';
import { EmploymentFormService } from '../../../shared-components/services/employment-form.service';

@Component({
    selector: 'np-pre-application-applicants',
    templateUrl: './pre-application-applicants.component.html',
    styles: [
    ]
})
export class PreApplicationApplicantsComponent implements OnInit {
    constructor(private route: ActivatedRoute, private storageService: UlStandardPreApplicationService,
        private config: CustomerPagesConfigService, private router: Router, private formBuilder: UntypedFormBuilder,
        private validationService: CustomerPagesValidationService, private employmentFormService: EmploymentFormService) { }

    public shellData: CustomerpagesShellInitialData = {
        logoRouterLink: null,
        skipBodyLayout: false,
        wideNavigation: false
    };
    public m: Model
    public errorMessage: string

    async ngOnInit() {
        this.setupTestFunctions();
        await this.reload(this.route.snapshot.params['preApplicationId']);
    }

    private async reload(preApplicationId: string) {
        this.errorMessage = null;
        this.m = null;

        let {isEnabled, settings} = await this.storageService.getUlStandardWebApplicationSettings();
        if(!isEnabled) {
            this.errorMessage = this.config.baseCountry() === 'SE' ? 'Ansökan inaktiv' : 'Application not active'
            return;
        }


        let loanObjectives = settings?.loanObjectives ?? [];

        let application = this.storageService.load(preApplicationId);
        if(!application) {
            this.errorMessage = this.config.baseCountry() === 'SE' ? 'Ansökan finns inte' : 'No such application exists'
        }

        let formFields: Dictionary<any> = {
            'mainApplicantCivicRegNr': ['', [this.validationService.getCivicRegNrValidator({ require12DigitCivicRegNrSe: true }), Validators.required]],
            'mainApplicantEmail': ['', [this.validationService.getEmailValidator(), Validators.required]],
            'mainApplicantPhone': ['', [this.validationService.getPhoneValidator(), Validators.required]],
            'mainApplicantIncomePerMonthAmount': ['', [this.validationService.getPositiveIntegerValidator(), Validators.required]],
            'mainApplicantEmployment': ['', [Validators.required]],
            'mainApplicantHasLegalOrFinancialGuardian': ['', [Validators.required]],
            'mainApplicantClaimsToBeGuarantor': ['', [Validators.required]],
        }

        if(loanObjectives?.length > 0) {
            formFields['loanObjective'] = ['', [Validators.required]]
        }
        let form = new FormsHelper(this.formBuilder.group(formFields));
        form.form.valueChanges.subscribe((x) => {
            form.ensureControlOnConditionOnly(
                this.employmentFormService.isEmployedSinceEmploymentCode(
                    form.getValue('mainApplicantEmployment')
                ),
                'mainApplicantEmployedSince',
                () => '',
                () => [this.validationService.getDateOnlyValidator(), Validators.required]
            );
            form.ensureControlOnConditionOnly(
                this.employmentFormService.isEmployedToEmploymentCode(form.getValue('mainApplicantEmployment')),
                'mainApplicantEmployedTo',
                () => '',
                () => [this.validationService.getDateOnlyValidator(), Validators.required]
            );
            form.ensureControlOnConditionOnly(
                this.employmentFormService.isEmployerEmploymentCode(form.getValue('mainApplicantEmployment')),
                'mainApplicantEmployer',
                () => '',
                () => [Validators.required]
            );
            form.ensureControlOnConditionOnly(
                this.employmentFormService.isEmployerEmploymentCode(form.getValue('mainApplicantEmployment')),
                'mainApplicantEmployerPhone',
                () => '',
                () => [this.validationService.getPhoneValidator(), Validators.required]
            );
        });

        this.m = {
            form: form,
            preApplicationId: preApplicationId,
            application: application,
            loanObjectives: loanObjectives,
            employmentCodes: this.employmentFormService.getEmploymentStatuses()
        }
    }

    public async apply(evt ?: Event) {
        evt?.preventDefault();
        let application = this.m.application;
        let vs = this.validationService;
        let f = this.m.form;
        application.loanObjective = f.getValue('loanObjective');
        application.applicants = [{
            civicRegNr: f.getValue('mainApplicantCivicRegNr'),
            phone:  f.getValue('mainApplicantPhone'),
            email: f.getValue('mainApplicantEmail'),
            incomePerMonthAmount: vs.parseIntegerOrNull(f.getValue('mainApplicantIncomePerMonthAmount')),
            employment: vs.normalizeString(f.getValue('mainApplicantEmployment')),
            employer: vs.normalizeString(f.getValue('mainApplicantEmployer')),
            employerPhone: vs.normalizeString(f.getValue('mainApplicantEmployerPhone')),
            employedSince: vs.parseDateOnlyOrNull(f.getValue('mainApplicantEmployedSince')),
            employedTo: vs.parseDateOnlyOrNull(f.getValue('mainApplicantEmployedTo')),
            hasLegalOrFinancialGuardian: f.getValue('mainApplicantHasLegalOrFinancialGuardian') === 'yes',
            claimsToBeGuarantor: f.getValue('mainApplicantClaimsToBeGuarantor') === 'yes'
        }];
        await this.storageService.save(this.m.preApplicationId, application);
        this.router.navigate(['ul-webapplications/application-economy/' + this.m.preApplicationId]);
    }

    private setupTestFunctions() {
        this.shellData.test = {
            functionCalls: [{
                displayText: 'Autofill',
                execute: async () => {
                    let testPersons = await this.storageService.generateTestPersons(1, true, true);
                    let a1 = testPersons.Persons[0].Properties;
                    let f = this.m.form;
                    f.setValue('mainApplicantCivicRegNr', a1.civicRegNr);
                    f.setValue('mainApplicantEmail', a1.email);
                    f.setValue('mainApplicantPhone', a1.phone);
                    f.setValue('mainApplicantIncomePerMonthAmount', '37500');
                    if(this.m.loanObjectives.length > 0) {
                        f.setValue('loanObjective', this.m.loanObjectives[0]);
                    }
                    f.setValue('mainApplicantEmployment', 'full_time');
                    f.setValue('mainApplicantEmployedSince', '2017-03-15');
                    f.setValue('mainApplicantEmployedTo', '2021-09-12');
                    f.setValue('mainApplicantEmployer', 'Employer AB');
                    f.setValue('mainApplicantEmployerPhone', '08 - 123 654');
                    f.setValue('mainApplicantHasLegalOrFinancialGuardian', 'no');
                    f.setValue('mainApplicantClaimsToBeGuarantor', 'no');
                }
            }]
        }
    }
}

interface Model {
    preApplicationId: string
    application: PreApplicationStoredDataModel
    form: FormsHelper
    loanObjectives: string[]
    employmentCodes: NTechEnum[]
}