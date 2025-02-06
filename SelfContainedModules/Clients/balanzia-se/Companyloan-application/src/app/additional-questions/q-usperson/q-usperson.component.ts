import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup, FormArray, FormControl } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { StringDictionary, Dictionary } from 'src/app/backend/common.types';

@Component({
  selector: 'q-usperson',
  templateUrl: './q-usperson.component.html',
  styleUrls: []
})
export class QUsPersonComponent  extends QuestionsStep<UsPersonFormDataModel>  {
    //See this for how lists of controls work: https://alligator.io/angular/reactive-forms-formarray-dynamic-fields/
    protected createForm(formData: UsPersonFormDataModel): FormGroup {
        let f = this.fb.group({
            persons: this.fb.array([]),
            noUsPerson: [formData.noUsPerson, [] ]
        })

        const arr = f.controls['persons'] as FormArray
        for(let o of formData.persons) {
            let ctr = this.fb.group({
                civicNr: [o.civicNr, [] ],
                name: [o.name, [] ],
                isUsPerson: [o.isUsPerson, []]
            })
            arr.push(ctr)
        }

        return f
    }

    public getPersonsControls() : FormArray{
        return <FormArray>this.form.controls['persons']
    }

    protected usesCustomFormPopulation() {
        return true
    }

    protected getIsForwardAllowed(formData: UsPersonFormDataModel): boolean {
        return this.form.valid && (formData.noUsPerson === true || formData.persons && !!formData.persons.find(x => x.isUsPerson === true))
    }

    protected getFormUpdateFromQuestions(): UsPersonFormDataModel {
        let persons : UsPersonFormDataItemModel[] = []
        for(let c of this.questions.beneficialOwners.beneficialOwners) {
            persons.push({
                civicNr: c.civicNr,
                name: `${c.firstName} ${c.lastName}`,
                isUsPerson: c.isUsPerson
            })
        }
        return {
            noUsPerson:persons.length > 0 && !persons.find(x => x.isUsPerson !== false),
            persons: persons
        }
    }

    protected updateQuestionsFromForm(formData: UsPersonFormDataModel) {
        let usPersons : Dictionary<boolean>
        if(formData.noUsPerson) {
            usPersons = null
        } else {
            usPersons = {}
            for(let p of formData.persons) {
                usPersons[p.civicNr] = !!p.isUsPerson
            }
        }

        for(let c of this.questions.beneficialOwners.beneficialOwners) {
            c.isUsPerson = !!(usPersons && usPersons[c.civicNr])
        }
    }

    protected getStepName(): string {
        return 'q-usperson'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('pep')
    }

    protected getPreviousStep(): StepRouteModel {
        if(this.questions.beneficialOwners.hasBeneficialOwners1) {
            return new StepRouteModel('beneficial-owners1')
        } else {
            return new StepRouteModel('beneficial-owners-option')
        }
    }
}

export class UsPersonFormDataModel {
    persons?: UsPersonFormDataItemModel[]
    noUsPerson?: boolean
}

export class UsPersonFormDataItemModel {
    civicNr?: string
    name?: string
    isUsPerson?: boolean
}
