import { Component, OnInit, Inject, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { API_SERVICE, ApiService, OfferModel } from '../../backend/api-service';
import { ApplicationModel } from "../../backend/application-model";
import { ProgressBarHelper, ProgressModel } from '../../backend/progressbar-helper';
import { forkJoin, combineLatest, BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators';
import { NullableNumber } from '../../backend/common.types';
import { ConfigService } from 'src/app/backend/config.service';

@Component({
    selector: 'calculator',
    templateUrl: './calculator.component.html',
    styleUrls: ['./calculator.component.css']
})
export class CalculatorComponent {
    application : ApplicationModel
    offer: OfferModel
    request: LoanRequestedModel
    isApplyAllowed: BehaviorSubject<boolean>
    isLegalInterestCeilingEnabled: BehaviorSubject<boolean>

    @ViewChild('loanAmountProgressBar', { static: false })loanAmountProgressBar : ElementRef<HTMLElement>
    @ViewChild('repaymentTimeProgressBar', { static: false })repaymentTimeProgressBar : ElementRef<HTMLElement>

    constructor(private route: ActivatedRoute,
        private router: Router,
        @Inject(API_SERVICE) private apiService: ApiService,
        configService: ConfigService) {

            this.isApplyAllowed = new BehaviorSubject<boolean>(false)
            this.isLegalInterestCeilingEnabled = new BehaviorSubject<boolean>(configService.isLegalInterestCeilingEnabled())
            this.route.data.subscribe(x => {
                this.application = null
                this.offer = null
                this.request = null
                this.isApplyAllowed.next(false)                

                let a : ApplicationModel  = x.application
                this.application = a

                if(this.application.offer) {
                    this.setupRequest(this.application.offer.LoanAmount, this.application.offer.RepaymentTimeInYears)
                } else {
                    this.apiService.calculateLoanPreview({ RequestedLoanAmount: null, RequestedRepaymentTimeInYears: null }).subscribe(x => {
                        this.setupRequest(x.RequestedLoanAmount, x.RequestedRepaymentTimeInYears)
                    })
                }
            })
    }

    private setupRequest(requestedLoanAmount: number, requestedRepaymentTimeInYears: number) {
        let loanLimits = this.apiService.getLoanLimits()
        this.request = {
            RepaymentTimeInYears: new ProgressBarHelper(1, loanLimits.MinimumRepaymentTimeInYears, loanLimits.MaximumRepaymentTimeInYears, requestedRepaymentTimeInYears),
            LoanAmount: new ProgressBarHelper(1000, loanLimits.MinimumLoanAmount, loanLimits.MaximumLoanAmount, requestedLoanAmount),
        }
        combineLatest([this.request.LoanAmount.current, this.request.RepaymentTimeInYears.current]).subscribe(r => {
              this.recalculate(r[0], r[1])                          
          })        
    }

    apply(evt: Event) {
        if(evt) {
            evt.preventDefault()
        }
        if(!this.isApplyAllowed.value) {
            return
        }
        this.application.setDataCalculator(this.offer)
        this.apiService.saveApplication(this.application)
        this.apiService.navigateToRoute('nr-of-applicants', this.application.id)
    }

    private recalculate(loanAmount: ProgressModel, repaymentTime: ProgressModel) {
        if(!this.request) {
            this.offer = null
            return
        }
        this.apiService.calculateLoanPreview({RequestedLoanAmount: loanAmount.currentValue ? new NullableNumber(loanAmount.currentValue) : null, RequestedRepaymentTimeInYears: repaymentTime.currentValue ? new NullableNumber(repaymentTime.currentValue) : null  }).subscribe(x => {
            this.offer = x.Offer
            this.isApplyAllowed.next(!!x.Offer)
        })
    }

    onClickLoanAmount(evt: MouseEvent) {
        evt.preventDefault()
        if(!this.loanAmountProgressBar || !this.loanAmountProgressBar.nativeElement || !this.request) {
            return
        }
        this.request.LoanAmount.handleClickEvent(evt, this.loanAmountProgressBar.nativeElement)
    }

    onClickRepaymentTime(evt: MouseEvent) {
        evt.preventDefault()
        if(!this.repaymentTimeProgressBar || !this.repaymentTimeProgressBar.nativeElement || !this.request) {
            return
        }
        this.request.RepaymentTimeInYears.handleClickEvent(evt, this.repaymentTimeProgressBar.nativeElement)
    }    
}

export class LoanRequestedModel {    
    LoanAmount: ProgressBarHelper
    RepaymentTimeInYears: ProgressBarHelper
}