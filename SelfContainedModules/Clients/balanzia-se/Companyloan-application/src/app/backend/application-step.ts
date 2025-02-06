import { ApplicationModel } from './application-model';
import { BehaviorSubject, Subscription } from 'rxjs';
import { FormGroup, FormBuilder } from '@angular/forms';
import { ActivatedRoute, Data } from '@angular/router';
import { Inject, OnInit } from '@angular/core';
import { API_SERVICE, ApiService } from './api-service';
import { NTechValidationService } from './ntech-validation.service';
import { ApplicationForwardBackService } from './application-forward-back.service';

export abstract class ApplicationStep<TFormDataModel> implements OnInit {
    application: ApplicationModel
    form: FormGroup

    constructor(private route: ActivatedRoute,
        @Inject(API_SERVICE) protected apiService: ApiService,
        protected fb: FormBuilder,
        protected validationService: NTechValidationService,
        protected forwardBackService: ApplicationForwardBackService) {
           
    }

    ngOnInit() {        
        this.subs.push(this.route.data.subscribe(x => {
            this.application = x.application
            this.onDataChanged(x)
            this.form = this.createForm()
            this.forwardBackService.isBackAllowed.next(!!this.getPreviousStep())
            this.subs.push(this.form.valueChanges.subscribe(x => {
                let f : TFormDataModel = x
                this.forwardBackService.isForwardAllowed.next(this.getIsForwardAllowed(f))
            }))
            
            this.form.reset()
            let i = this.getFormUpdateFromApplication()
            if(i) {
                this.form.patchValue(i)
                this.forwardBackService.isForwardAllowed.next(this.getIsForwardAllowed(i))
            } else {
                this.forwardBackService.isForwardAllowed.next(false)
            }
        }))

        this.subs.push(this.forwardBackService.onForward.subscribe(x => {
            this.forward()
        }))
        this.subs.push(this.forwardBackService.onBack.subscribe(x => {
            this.back()
        }))

        this.forwardBackService.isFinalStep.next(this.isFinalStep())
        this.afterNgOnInit()
    }

    ngOnDestroy() {
        this.unsub()
    }

    subs: Subscription[] = []

    unsub() {
        if(this.subs) {
            for(let s of this.subs) {
                s.unsubscribe()
            }
            this.subs = []
        }
    }

    protected abstract createForm() : FormGroup    
    protected abstract getIsForwardAllowed(formData: TFormDataModel) : boolean
    protected abstract getFormUpdateFromApplication() : TFormDataModel
    protected abstract updateApplicationFromForm(formData: TFormDataModel)
    protected abstract getStepName(): string
    protected abstract getNextStep(): StepRouteModel
    protected abstract getPreviousStep(): StepRouteModel
    protected onDataChanged(x: Data) { }
    protected isFinalStep(): boolean { return false }
    protected afterNgOnInit() {

    }

    protected onForward() {
        let f : TFormDataModel = this.form.value
        this.updateApplicationFromForm(f)
        this.apiService.saveApplication(this.application)
        let n = this.getNextStep()
        this.apiService.navigateToApplicationRoute(n.stepName, this.application.id, n.loanType)
    }

    forward() {
        this.onForward()
    }

    back() {
        let n = this.getPreviousStep()
        this.apiService.navigateToApplicationRoute(n.stepName, this.application.id, n.loanType)
    }
}

export class StepRouteModel {
    constructor(public stepName: string, public loanType? : string)  {}
}