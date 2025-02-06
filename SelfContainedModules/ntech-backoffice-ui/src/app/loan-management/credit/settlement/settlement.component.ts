import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, ParamMap } from '@angular/router';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { CreditService, GetCreditSettlementInitialDataResult, SettlementSuggestionData } from '../credit.service';

@Component({
    selector: 'app-settlement',
    templateUrl: './settlement.component.html',
    styles: [],
})
export class SettlementComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private configService: ConfigService,
        private fb: UntypedFormBuilder,
        private validationService: NTechValidationService,
        private toastr: ToastrService,
        private apiService: NtechApiService
    ) {}

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('creditNr'));
        });
    }

    public m: Model;

    private async reload(creditNr: string) {
        this.m = null;

        if (!creditNr) {
            let title = 'Credit settlement';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }
        this.eventService.setCustomPageTitle(`Credit ${creditNr}`, `Credit settlement ${creditNr}`);

        let initialData = await this.creditService.getCreditSettlementInitialData(creditNr, true);
        let allowNotify = initialData.hasEmailProvider;

        let isMortgageLoansEnabled = this.configService.isFeatureEnabled('ntech.feature.mortgageloans');
        let isSweden = this.configService.baseCountry() === 'SE';
        let initialSettlementDate = this.configService
            .getCurrentDateAndTime()
            .add(isMortgageLoansEnabled ? 1 : 7, 'days')
            .format('YYYY-MM-DD');

        let formData : {[key: string] : any} = {
            settlementDate: [
                '',
                [
                    Validators.required,
                    this.validationService.getValidator('settlementDate', (x) => {
                        let newDate = this.validationService.parseDateOnlyOrNull(x);
                        if (newDate === null) {
                            return false;
                        }
                        let today = moment(
                            this.configService.getCurrentDateAndTime().format('YYYY-MM-DD'),
                            'YYYY-MM-DD',
                            true
                        );
                        if (moment(newDate, 'YYYY-MM-DD', true) < today) {
                            return false;
                        }
                        return true;
                    }),
                ],
            ],
            notifiedEmail: ['', [this.validationService.getEmailValidator()]],
            notify: [null, []],
        };

        if(isMortgageLoansEnabled && isSweden) {
            formData['swedishRseInterestRatePercent'] = ['', [Validators.required, this.validationService.getPositiveDecimalValidator()]]
        }

        let f = new FormsHelper(
            this.fb.group(formData)
        );

        let resetSettlementForm = () => {
            f.setValue('settlementDate', initialSettlementDate);
            f.setValue('notifiedEmail', allowNotify ? initialData.notificationEmail : '');
            f.setValue('notify', allowNotify);            
            if(f.hasFormControl(null, 'swedishRseInterestRatePercent')) {
                f.setValue('swedishRseInterestRatePercent', '');
            }
        };

        resetSettlementForm();

        f.setFormValidator((x) => {
            let notify = f.getValue('notify');
            if (!notify) {
                return true;
            }
            return !!f.getValue('notifiedEmail'); //Email required if notify
        }, 'form');

        this.m = {
            ...initialData,
            creditNr: creditNr,
            calculateModel: initialData.pendingOffer
                ? null
                : {
                      allowNotify: allowNotify,
                  },
            calculateForm: f,
            resetSettlementForm: resetSettlementForm,
        };
    }

    async calculate(evt?: Event) {
        evt?.preventDefault();
        
        let settlementDate = this.validationService.parseDateOnlyOrNull(
            this.m.calculateForm.getValue('settlementDate')
        );

        let hasRse = this.m.calculateForm.hasFormControl(null, 'swedishRseInterestRatePercent');
        let swedishRseInterestRatePercent = hasRse
            ?  this.validationService.parsePositiveDecimalOrNull(this.m.calculateForm.getValue('swedishRseInterestRatePercent'))
            : null;

        let suggestion = (
            await this.creditService.computeSettlementSuggestion(this.m.creditNr, settlementDate, swedishRseInterestRatePercent)
        )?.suggestion;

        this.m.suggestionModel = suggestion;
        if(suggestion.swedishRse) {
            this.m.rseEdit = {
                form: this.fb.group({
                    'swedishRseEstimatedAmount': [this.validationService.formatDecimalForEdit(suggestion.swedishRse.estimatedAmount), [Validators.required, this.validationService.getPositiveDecimalValidator()]]
                }),
                rseReportUrl: this.apiService.getUiGatewayUrl('nCredit', 'api/Reports/MortgageLoanSeRse?ComparisonInterestRatePercent', [
                    ['ComparisonInterestRatePercent', swedishRseInterestRatePercent.toString()],
                    ['CreditNr', this.m.creditNr],
                ])
            }
        }
    }

    isNotificationEmailBeingSent() {
        return (
            this.m &&
            this.m.calculateModel &&
            this.m.calculateModel.allowNotify &&
            this.m.calculateForm.getValue('notify') === true
        );
    }

    public getSwedishRseEstimatedAmount() {
        if(this.m.suggestionModel.swedishRse) {
            return this.validationService.parsePositiveDecimalOrNull(this.m.rseEdit.form.value.swedishRseEstimatedAmount)
        } else {
            return null;
        }
    }

    async createAndSendSuggestion(evt?: Event) {
        evt?.preventDefault();

        let notifiedEmail: string = null;
        if (this.isNotificationEmailBeingSent()) {
            notifiedEmail = this.m.calculateForm.getValue('notifiedEmail');
        }

        let swedishRseEstimatedAmount: number = this.getSwedishRseEstimatedAmount();;

        var creditNr = this.m.creditNr;
        let result = await this.creditService.createAndSendSettlementSuggestion(
            creditNr,
            this.m.suggestionModel.settlementDate,
            notifiedEmail,
            swedishRseEstimatedAmount,
            this.m.suggestionModel.swedishRse?.interestRatePercent
        );

        if (result.userWarningMessage) {
            this.toastr.warning(result.userWarningMessage);
        }

        this.m.calculateModel = null;
        this.m.suggestionModel = null;
        this.m.pendingOffer = result.pendingOffer;
        this.m.resetSettlementForm();
        this.eventService.signalReloadCreditComments(creditNr);
    }

    async cancel(evt?: Event) {
        evt?.preventDefault();

        var creditNr = this.m.creditNr;

        await this.creditService.cancelPendingSettlementSuggestion(
            this.m.pendingOffer.id);

        this.reload(creditNr);
        this.eventService.signalReloadCreditComments(creditNr);
    }

    public getSuggestedTotalSettlementBalance() {
        let s = this.m.suggestionModel;
        if(this.m.rseEdit) {
            return s.totalSettlementBalance - s.swedishRse.estimatedAmount + this.getSwedishRseEstimatedAmount();
        } else {
            return s.totalSettlementBalance;
        }
    }
}

interface Model extends GetCreditSettlementInitialDataResult {
    creditNr: string;
    calculateForm: FormsHelper;
    calculateModel?: {
        allowNotify: boolean;
    };
    suggestionModel?: SettlementSuggestionData;
    rseEdit ?: {
        form: UntypedFormGroup
        rseReportUrl: string
    }
    resetSettlementForm: () => void;
}
