import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup, FormArray, FormControl, Form, AbstractControl } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { BeneficialOwnersModel } from 'src/app/backend/questions-model';

@Component({
  selector: 'q-beneficial-owners1',
  templateUrl: './q-beneficial-owners1.component.html',
  styleUrls: []
})
export class QBeneficialOwners1Component  extends QuestionsStep<BeneficialOwners1FormDataModel>  {
    //See this for how lists of controls work: https://alligator.io/angular/reactive-forms-formarray-dynamic-fields/
    protected createForm(formData: BeneficialOwners1FormDataModel): FormGroup {
        let f = this.fb.group({
            owners: this.fb.array([])
        })

        for(let o of formData.owners) {
            this.addOwnerControl(f.controls['owners'] as FormArray, o, null)
        }
        return f
    }

    public getOwnerControls() : FormArray{
        return <FormArray>this.form.controls['owners']
    }

    public addOwnerControl(owners: FormArray, initialValue: BeneficialOwners1ItemFormDataModel, evt: Event) {
        if(evt) {
            evt.preventDefault()
        }
        let ctr = this.fb.group({
            civicNr: [initialValue ? initialValue.civicNr : '', [Validators.required, this.validationService.getCivicNrValidator()] ],
            firstName: [initialValue ? initialValue.firstName : '', [Validators.required] ],
            lastName: [initialValue ? initialValue.lastName : '', [Validators.required] ],
            ownershipPercent: [initialValue ? initialValue.ownershipPercent: '', [Validators.required, this.validationService.getPositiveIntegerWithBoundsValidator(25, 100)] ],
        })
        owners.push(ctr)
        return ctr
    }

    public removeOwnerControl(g: FormGroup, index: number, evt?: Event) {
        if(evt) {
            evt.preventDefault()
        }
        const owners = this.getOwnerControls()
        owners.removeAt(index)
    }

    protected usesCustomFormPopulation() {
        return true
    }

    protected getIsForwardAllowed(formData: BeneficialOwners1FormDataModel): boolean {
        return this.form.valid && (formData.owners && formData.owners.length > 0)
    }

    protected getFormUpdateFromQuestions(): BeneficialOwners1FormDataModel {
        if(!this.questions.beneficialOwners) {
            return {
                owners: []
            }
        } else if(this.questions.beneficialOwners.hasBeneficialOwners1 && this.questions.beneficialOwners.beneficialOwners) {
            let owners: BeneficialOwners1ItemFormDataModel[] = []
            for(let o of BeneficialOwnersModel.filterOwners(true,  this.questions.beneficialOwners.beneficialOwners)) {
                owners.push({
                    civicNr: o.civicNr,
                    firstName: o.firstName,
                    lastName: o.lastName,
                    ownershipPercent: this.validationService.formatInteger(o.ownershipPercent)
                })
            }
            return {
                owners: owners
            }
        } else {
            return {
                owners: []
            }
        }
    }

    protected updateQuestionsFromForm(formData: BeneficialOwners1FormDataModel) {
        this.questions.beneficialOwners = {
            hasBeneficialOwners1: true,
            beneficialOwners: []
        }
        for(let o of formData.owners) {
            this.questions.beneficialOwners.beneficialOwners.push({
                civicNr: o.civicNr,
                firstName: o.firstName,
                lastName: o.lastName,
                ownershipPercent: this.validationService.parseInteger(o.ownershipPercent),
                isUsPerson: null,
                isPep: null
            })
        }
    }

    protected getStepName(): string {
        return 'q-beneficial-owners1'
    }

    protected getNextStep(): StepRouteModel {
        if(this.questions.beneficialOwners.beneficialOwners.length > 0) {
            return new StepRouteModel('usperson')
        } else {
            return new StepRouteModel('company-sector')
        }
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('beneficial-owners-option')
    }
}

export class BeneficialOwners1FormDataModel {
    owners?: BeneficialOwners1ItemFormDataModel[]
}

export class BeneficialOwners1ItemFormDataModel {
    civicNr?: string
    firstName?: string
    lastName?: string
    ownershipPercent?: string
}
