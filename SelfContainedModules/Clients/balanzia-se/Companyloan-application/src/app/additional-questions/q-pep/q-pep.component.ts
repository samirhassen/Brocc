import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup, FormArray, FormControl } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { Dictionary } from 'src/app/backend/common.types';

@Component({
  selector: 'q-pep',
  templateUrl: './q-pep.component.html',
  styleUrls: []
})
export class QPepComponent  extends QuestionsStep<PepFormDataModel>  {
    protected createForm(formData: PepFormDataModel): FormGroup {
        let f = this.fb.group({
            persons: this.fb.array([]),
            noPep: [formData.noPep, [] ]
        })

        const arr = f.controls['persons'] as FormArray;
        for(let p of formData.persons) {
            let ctr = this.fb.group({
                civicNr: [p.civicNr, [] ],
                name: [p.name, [] ],
                isPep: [p.isPep, [] ]
            })
            arr.push(ctr);
        }

        return f
    }

    public getPersonsControls() : FormArray{
        return <FormArray>this.form.controls['persons']
    }

    protected getIsForwardAllowed(formData: PepFormDataModel): boolean {
        let stepAnswered = formData.noPep === true
            || (formData.persons && !!formData.persons.find(x => x.isPep));

        return this.form.valid && stepAnswered;
    }

    protected getFormUpdateFromQuestions(): PepFormDataModel {
        let persons: PepPersonFormDataModel[] = [];
        for(let own of this.questions.beneficialOwners.beneficialOwners) {
            persons.push({
                civicNr: own.civicNr,
                name: `${own.firstName} ${own.lastName}`,
                isPep: null
            })
        }
        return {
            noPep: persons.length > 0 && !persons.find(x => x.isPep !== false),
            persons: persons
        }
    }

    protected updateQuestionsFromForm(formData: PepFormDataModel) {
        let pepPersons : Dictionary<boolean>
        if(formData.noPep) {
            pepPersons = null
        } else {
            pepPersons = {}
            for(let p of formData.persons) {
                pepPersons[p.civicNr] = !!p.isPep
            }
        }

        for(let c of this.questions.beneficialOwners.beneficialOwners) {
            c.isPep = !!(pepPersons && pepPersons[c.civicNr])
        }
    }

    protected getStepName(): string {
        return 'q-pep'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('company-sector')
    }

    protected getPreviousStep(): StepRouteModel {
        let b = this.questions.beneficialOwners
        if(b && b.hasBeneficialOwners1 && b.beneficialOwners.length > 0)  {
            return new StepRouteModel('usperson')
        } else {
            return new StepRouteModel('beneficial-owners-option')
        }
    }
}

export class PepFormDataModel {
    noPep?: boolean;
    persons?: PepPersonFormDataModel[];
}

export class PepPersonFormDataModel {
    civicNr?: string;
    name?: string;
    isPep?: boolean;
}
