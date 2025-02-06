import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerPagesConfigService, NTechEnum } from '../../../common-services/customer-pages-config.service';
import { PreApplicationStoredDataModel, UlStandardPreApplicationService } from '../../services/pre-application.service';
import { Dictionary } from 'src/app/common.types';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { CustomerpagesShellInitialData } from '../../../shared-components/customerpages-shell/customerpages-shell.component';
import { CustomerPagesValidationService } from '../../../common-services/customer-pages-validation.service';
import { Randomizer } from 'src/app/common-services/randomizer';

@Component({
    selector: 'np-pre-application-economy',
    templateUrl: './pre-application-economy.component.html',
    styles: [
    ]
})
export class PreApplicationEconomyComponent implements OnInit {
    constructor(private route: ActivatedRoute,
        private storageService: UlStandardPreApplicationService, private config: CustomerPagesConfigService,
        private router: Router, private formBuilder: UntypedFormBuilder, private validationService: CustomerPagesValidationService) { }

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
        this.m = null;
        this.errorMessage = null;

        let {isEnabled, settings} = await this.storageService.getUlStandardWebApplicationSettings();
        if(!isEnabled) {
            this.errorMessage = this.config.baseCountry() === 'SE' ? 'Ansökan inaktiv' : 'Application not active'
            return;
        }

        let initialApplication = this.storageService.load(preApplicationId);
        if(!initialApplication) {
            this.errorMessage = this.config.baseCountry() === 'SE' ? 'Ansökan finns inte' : 'No such application exists'
        }

        let formFields : Dictionary<any> = {
            'housing': ['', [Validators.required]],
            'housingCostPerMonthAmount': ['', [Validators.required, this.validationService.getPositiveIntegerValidator()]],
            'otherHouseholdFixedCostsAmount': ['', [Validators.required, this.validationService.getPositiveIntegerValidator()]],
            'internalChildCount': [0, []],
            'hasConsented': [false, []]
        };

        let f = new FormsHelper(this.formBuilder.group(formFields));

        let enums = this.config.getEnums();
        this.m = {
            form: f,
            childrenGroupNames: [],
            nextOtherLoanSuffix: 1,
            otherLoanGroupNames: [],
            isOtherLoansTouched: false,
            housingTypes: enums.HousingTypes,
            otherLoanTypes: enums.OtherLoanTypes,
            initialApplication: initialApplication,
            preApplicationId: preApplicationId,
            personalDataPolicyUrl: settings.personalDataPolicyUrl,
            isDataSharingEnabled: !!settings.dataSharing
        }

        f.form.valueChanges.subscribe(_ => {
            f.ensureControlOnConditionOnly(
                f.getValue('internalChildCount') > 0,
                'childBenefitAmount',
                () => '',
                () => [Validators.required, this.validationService.getPositiveIntegerValidator()]
            );
        });
    }

    public getHasOtherLoans() {
        if(this.m.otherLoanGroupNames.length === 0 && !this.m.isOtherLoansTouched) {
            return null;
        }
        return this.m.otherLoanGroupNames.length > 0;
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

    public async apply(evt ?: Event) {
        evt?.preventDefault();

        let f = this.m.form;
        let vs = this.validationService;

        let finalApplication : PreApplicationStoredDataModel = JSON.parse(JSON.stringify(this.m.initialApplication));
        finalApplication.childBenefitAmount = vs.parseIntegerOrNull(f.getValue('childBenefitAmount'));
        finalApplication.housingCostPerMonthAmount = vs.parseIntegerOrNull(f.getValue('housingCostPerMonthAmount'));
        finalApplication.otherHouseholdFixedCostsAmount = vs.parseIntegerOrNull(f.getValue('otherHouseholdFixedCostsAmount'));
        finalApplication.housing = f.getValue('housing');
        finalApplication.householdChildren = this.m.childrenGroupNames.map(groupName => ({
            ageInYears: vs.parseIntegerOrNull(f.getFormGroupValue(groupName, 'ageInYears')),
            sharedCustody: f.getFormGroupValue(groupName, 'sharedCustody') === 'true'
        }));
        finalApplication.loansToSettle = this.m.otherLoanGroupNames.map(groupName => ({
            loanType: f.getFormGroupValue(groupName, 'loanType'),
            monthlyCostAmount: vs.parseIntegerOrNull(f.getFormGroupValue(groupName, 'monthlyCostAmount')),
            currentDebtAmount: vs.parseIntegerOrNull(f.getFormGroupValue(groupName, 'currentDebtAmount'))
        }));
        for(let a of (finalApplication.applicants ?? [])) {
            a.hasConsentedToCreditReport = true;
        }

        this.storageService.save(this.m.preApplicationId, finalApplication);

        let shouldSendToDataShare = false;
        if(this.m.isDataSharingEnabled) {
            let scoringResult = await this.storageService.preScoreApplication(finalApplication);
            if(scoringResult.preScoreResultId) {
                finalApplication.preScoreResultId = scoringResult.preScoreResultId;
                this.storageService.save(this.m.preApplicationId, finalApplication)
            }
            shouldSendToDataShare = scoringResult.isAcceptRecommended;
        }

        if(shouldSendToDataShare) {
            this.router.navigate(['ul-webapplications/application-datashare/' + this.m.preApplicationId]);
        } else {
            let preApplicationId = this.m.preApplicationId;
            this.m = null;
            setTimeout(async () => {
                if(!this.storageService.exists(preApplicationId)) {
                    return; //Guard against backclicks, double clicks or other unforseen interactions
                }
                let createResult = await this.storageService.createApplication(finalApplication);
                this.storageService.delete(preApplicationId);
                this.router.navigate(['ul-webapplications/application-received/' + createResult.ApplicationNr]);
            }, 0);
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

    private takeOtherLoanSuffix() {
        let toReturn = this.m.nextOtherLoanSuffix;
        this.m.nextOtherLoanSuffix += 1;
        return toReturn;
    }

    public addOtherLoan() {
        this.m.isOtherLoansTouched = true;
        let newGroupName = this.addOtherLoanFormGroup(this.m.form, this.takeOtherLoanSuffix());
        // To protect against label/input triggering change twice from radiobuttons.
        if (newGroupName !== null) this.m.otherLoanGroupNames.push(newGroupName);
    }

    public removeOtherLoan(removeAll: boolean, groupName?: string) {
        this.m.isOtherLoansTouched = true;
        if (removeAll) {
            for(let groupName of this.m.otherLoanGroupNames) {
                this.m.form.form.removeControl(groupName);
                this.m.otherLoanGroupNames = [];
            }
        } else if (groupName !== null) {
            this.m.otherLoanGroupNames.splice(this.m.otherLoanGroupNames.indexOf(groupName), 1);
            this.m.form.form.removeControl(groupName);
        }
    }

    public setHasOtherLoans(hasOtherLoans: boolean, evt ?: Event) {
        evt.preventDefault();
        this.m.isOtherLoansTouched = true;
        if(hasOtherLoans && this.m.otherLoanGroupNames.length === 0) {
            this.addOtherLoan();
        } else if(!hasOtherLoans && this.m.otherLoanGroupNames.length > 0) {
            this.removeOtherLoan(true, null);
        }
    }

    private setupTestFunctions() {
        this.shellData.test = {
            functionCalls: [{
                displayText: 'Autofill',
                execute: async () => {
                    let f = this.m.form;
                    f.setValue('housing', this.m.housingTypes[0].Code);
                    f.setValue('housingCostPerMonthAmount', '5000');
                    f.setValue('otherHouseholdFixedCostsAmount', '700');

                    if(this.m.childrenGroupNames.length > 0) {
                        while(this.m.childrenGroupNames.length > 0) {
                            this.removeChild();
                        }
                    }
                    let nrOfChildren = Randomizer.anyOf([0, 0, 0, 1, 2]);
                    [...Array(nrOfChildren)].forEach((_) => this.addChild());
                    this.m.childrenGroupNames.forEach(groupName => {
                        f.setValue2(groupName, 'ageInYears', Randomizer.anyOf(['1', '3', '5', '7', '9']));
                        f.setValue2(groupName, 'sharedCustody', Randomizer.anyOf(['true', 'false']));
                    });
                    f.setValue('childBenefitAmount', (this.m.childrenGroupNames.length * 100).toString());

                    if(this.m.otherLoanGroupNames.length > 0) {
                        this.removeOtherLoan(true, null);
                    }
                    let nrOfOtherLoans = Randomizer.anyOf([0, 1, 2]);
                    [...Array(nrOfOtherLoans)].forEach((_) => this.addOtherLoan());
                    this.m.otherLoanGroupNames.map((groupName, _) => {
                        f.setValue2(
                            groupName,
                            'currentDebtAmount',
                            Randomizer.anyEvenNumberBetween(15000, 200000, 1000).toString()
                        );
                        f.setValue2(groupName, 'monthlyCostAmount', Randomizer.anyEvenNumberBetween(500, 4500, 100).toString());
                        f.setValue2(groupName, 'loanType', Randomizer.anyOf(this.m.otherLoanTypes.map(x => x.Code)));
                    });

                    f.setValue('hasConsented', true);
                }
            }]
        }
    }
}

interface Model {
    preApplicationId: string
    initialApplication: PreApplicationStoredDataModel
    form: FormsHelper
    personalDataPolicyUrl: string
    isOtherLoansTouched: boolean
    housingTypes: NTechEnum[]
    otherLoanTypes: NTechEnum[]
    childrenGroupNames: string[]
    otherLoanGroupNames: string[]
    nextOtherLoanSuffix: number
    isDataSharingEnabled: boolean
}