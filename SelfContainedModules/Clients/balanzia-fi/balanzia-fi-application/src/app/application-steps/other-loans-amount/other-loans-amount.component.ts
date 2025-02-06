import { Component, Inject, ViewChild, ElementRef } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { NullableNumber } from '../../backend/common.types';
import { Data, ActivatedRoute } from '@angular/router';
import { API_SERVICE, ApiService } from '../../backend/api-service';
import { NTechValidationService } from '../../backend/ntech-validation.service';
import { ApplicationForwardBackService } from '../../backend/application-forward-back.service';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'other-loans-amount',
  templateUrl: './other-loans-amount.component.html',
  styleUrls: []
})
export class OtherLoansAmountComponent extends ApplicationApplicantStep<OtherLoansAmountFormDataModel> {
    private loanType: string

    @ViewChild('totalAmountInput', { static: true }) totalAmountInput: ElementRef<HTMLInputElement>

    constructor(route: ActivatedRoute,
        @Inject(API_SERVICE) apiService: ApiService,
        fb: FormBuilder,
        validationService: NTechValidationService,
        forwardBackService: ApplicationForwardBackService, 
        translateService: TranslateService) {
            super(route, apiService, fb, validationService, forwardBackService, translateService
                )
    }

    protected onDataChanged(x: Data) { 
        super.onDataChanged(x)
        this.loanType = x.loanType
    }

    getTrKey(name: string) {
        return `loanTypes.${this.loanType}.${name}`
    }

    protected createForm(): FormGroup {
        return this.fb.group({
            totalAmount: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ],
            monthAmount: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }
    
    protected getIsForwardAllowed(formData: OtherLoansAmountFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): OtherLoansAmountFormDataModel {        
        let a = this.application.getApplicant(this.applicantNr)
        let amt = a.otherLoansAmounts ? a.otherLoansAmounts[this.loanType]: null        
        return amt ? { 
            totalAmount: this.validationService.formatInteger(amt.totalAmount.value), 
            monthAmount: this.validationService.formatInteger(amt.monthAmount.value)
        } : null
    }

    protected updateApplicationFromForm(formData: OtherLoansAmountFormDataModel) {
        this.application.setDataOtherLoansAmount(
            this.loanType,
             new NullableNumber(this.validationService.parseInteger(formData.totalAmount)), 
             new NullableNumber(this.validationService.parseInteger(formData.monthAmount)), 
             this.applicantNr)
    }

    protected getStepName(): string {
        return 'other-loans-amount'
    }

    protected getNextStep(): StepRouteModel {
        let a = this.getApplicant()
        let loanTypes = a.otherLoansOptions
        let i = loanTypes.indexOf(this.loanType)
        if(i === (loanTypes.length - 1)) {
            //Last type
            if(this.applicantNr === 1 && this.application.nrOfApplicants && this.application.nrOfApplicants.value === 2) {
                return new StepRouteModel('ssn', 2)
            } else {
                return new StepRouteModel('consolidation-option')
            }
        } else {
            return new StepRouteModel('other-loans-amount', this.applicantNr, loanTypes[i+1])
        }
    }

    protected getPreviousStep(): StepRouteModel {
        let a = this.getApplicant()
        let loanTypes = a.otherLoansOptions
        let i = loanTypes.indexOf(this.loanType)
        if(i === 0) {
            //First type
            return new StepRouteModel('other-loans-options', this.applicantNr)
        } else {
            return new StepRouteModel('other-loans-amount', this.applicantNr, loanTypes[i-1])
        }
    }
}

class OtherLoansAmountFormDataModel {
    totalAmount: string
    monthAmount: string
}