import { Component } from '@angular/core';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { StepRouteModel } from '../../backend/application-step';
import { Validators, FormGroup } from '@angular/forms';
import { DateOnly } from 'src/app/backend/common.types';
import * as moment from 'moment'

@Component({
  selector: 'employment-details',
  templateUrl: './employment-details.component.html',
  styleUrls: []
})
export class EmploymentDetailsComponent extends ApplicationApplicantStep<EmploymentDetailsFormDataModel> {
    usesEmployer() {
        let a = this.application.getApplicant(this.applicantNr)
        return a.employment === 'employment_fastanstalld' || a.employment === 'employment_visstidsanstalld' || a.employment === 'employment_foretagare'
    }

    employedSinceYears :number[] = []
    employedSinceMonths: string[][] = [
        ['01', 'jan'],
        ['02', 'feb'],
        ['03', 'mar'],
        ['04', 'apr'],
        ['05', 'may'],
        ['06', 'jun'],
        ['07', 'jul'],
        ['08', 'aug'],
        ['09', 'sep'],
        ['10', 'okt'],
        ['11', 'nov'],
        ['12', 'dec']
    ]

    protected afterNgOnInit() {
        this.employedSinceYears = []
        let currentYear = parseInt(moment().format('YYYY'))
        for(var i=0; i<10; ++i) {
            this.employedSinceYears.push(currentYear-i)            
        }
    }

    getEmployedSinceYearText(y: number) {
        if(this.employedSinceYears.length === 0 || y !== this.employedSinceYears[this.employedSinceYears.length - 1]) {
            return y.toString()
        }
        //Last item
        return  `${y}, ${y-1}, ...`
    }

    protected createForm(): import("@angular/forms").FormGroup {
        let employedSinceYearField = ['', [Validators.required]]
        let employedSinceMonthField = ['', [Validators.required]]
        if(this.usesEmployer()) {
            return this.fb.group({
                employedSinceYear: employedSinceYearField,
                employedSinceMonth: employedSinceMonthField,
                employer: ['', [Validators.required]]
            })
        } else {
            return this.fb.group({
                employedSinceYear: employedSinceYearField,
                employedSinceMonth: employedSinceMonthField
            })
        }
    }

    protected getIsForwardAllowed(formData: EmploymentDetailsFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): EmploymentDetailsFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)
        return a.employmentDetails && (!!a.employmentDetails.employer) === this.usesEmployer() 
            ? { 
                employedSinceYear: DateOnly.format(a.employmentDetails.employedSinceMonth, 'YYYY'), 
                employedSinceMonth: DateOnly.format(a.employmentDetails.employedSinceMonth, 'MM'), 
                employer: a.employmentDetails.employer } 
            : null
    }

    protected updateApplicationFromForm(formData: EmploymentDetailsFormDataModel) {
        let employedSinceMonth = DateOnly.fromDateString(`${formData.employedSinceYear}-${formData.employedSinceMonth}-01`, 'YYYY-MM-DD')
        if(this.usesEmployer()) {
            this.application.setDataEmploymentDetails(employedSinceMonth, this.applicantNr, formData.employer)
        } else {
            this.application.setDataEmploymentDetails(employedSinceMonth, this.applicantNr, null)
        }
    }

    protected getStepName(): string {
        return 'employment-details'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('income', this.applicantNr)
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('employment', this.applicantNr)
    }
}

class EmploymentDetailsFormDataModel {
    employedSinceYear: string
    employedSinceMonth: string    
    employer: string
}