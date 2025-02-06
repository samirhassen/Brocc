import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';

@Component({
  selector: 'q-employee-count',
  templateUrl: './q-employee-count.component.html',
  styleUrls: []
})
export class QEmployeeCountComponent  extends QuestionsStep<EmployeeCountFormDataModel>  {
    protected createForm(formData: EmployeeCountFormDataModel): FormGroup {        
        return this.fb.group({
            employeeCount: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ],
        })
    }

    protected getIsForwardAllowed(formData: EmployeeCountFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): EmployeeCountFormDataModel {        
        return {
            employeeCount: this.questions.employeeCount ? this.validationService.formatInteger(this.questions.employeeCount.employeeCount) : ''
        }
    }    

    protected updateQuestionsFromForm(formData: EmployeeCountFormDataModel) {
        this.questions.employeeCount = {
            employeeCount: this.validationService.parseInteger(formData.employeeCount)
        }
    }

    protected getStepName(): string {
        return 'q-employee-count'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('extra-payments-option')
    }

    protected getPreviousStep(): StepRouteModel {
        if(this.questions.currencyExchange && this.questions.currencyExchange.hasCurrencyExchange) {
            return new StepRouteModel('currency-exchange')
        } else {
            return new StepRouteModel('currency-exchange-option')
        }
    }
}

export class EmployeeCountFormDataModel {
    employeeCount: string
}
