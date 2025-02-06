import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'q-bankaccount',
  templateUrl: './q-bankaccount.component.html',
  styleUrls: []
})
export class QBankAccountComponent  extends QuestionsStep<BankAccountFormDataModel>  {
    protected createForm(formData: BankAccountFormDataModel): FormGroup {
        let invalidValidators = [Validators.required, this.validationService.getAlwaysInvalidValidator()]

        let initialBankAccountNrType = formData ? formData.bankAccountNrType : ''
        let form = this.fb.group({
            bankAccountNrType: [initialBankAccountNrType, [Validators.required] ],
            bankAccountNr: [formData ? formData.bankAccountNr : '']
        })

        const ctr = form.get('bankAccountNr')

        let setupBankAccountNrValidators = x => {
            if(!x) {
                ctr.setValidators(invalidValidators)
                ctr.setAsyncValidators(null)
            } else {
                ctr.setValidators([Validators.required])
                ctr.setAsyncValidators([this.validationService.getBankAccountNrValidator(x, this.apiService)])
            }            
        }

        setupBankAccountNrValidators(initialBankAccountNrType)

        this.subs.push(form.get('bankAccountNrType').valueChanges.subscribe(x => {
            ctr.setValue('')
            setupBankAccountNrValidators(x)
        }))

        return form
    }

    protected usesCustomFormPopulation() {
        return true
    }

    protected getIsForwardAllowed(formData: BankAccountFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): BankAccountFormDataModel {
        return {
            bankAccountNr: this.questions.bankAccountNr,
            bankAccountNrType: this.questions.bankAccountNrType
        }
    }

    protected updateQuestionsFromForm(formData: BankAccountFormDataModel) {
        this.questions.bankAccountNr = formData.bankAccountNr
        this.questions.bankAccountNrType = formData.bankAccountNrType
    }

    protected getStepName(): string {
        return 'q-bankaccount'
    }

    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('consent')
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('payment-source')
    }
}

export class BankAccountFormDataModel {
    bankAccountNr: string
    bankAccountNrType: string
}
