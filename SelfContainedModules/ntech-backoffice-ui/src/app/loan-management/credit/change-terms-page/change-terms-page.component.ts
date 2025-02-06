import { Component } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { CreditService, GetCreditTermChangesResult, TermsChangeData } from '../credit.service';

@Component({
    selector: 'app-change-terms-page',
    templateUrl: './change-terms-page.component.html',
    styles: [],
})
export class ChangeTermsPageComponent {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private configService: ConfigService,
        private apiService: NtechApiService,
        private fb: UntypedFormBuilder,
        private validationService: NTechValidationService,
        private toastr: ToastrService
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
            let title = 'Credit change terms';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }
        this.eventService.setCustomPageTitle(`Credit ${creditNr}`, `Credit change terms ${creditNr}`);

        let result = await this.creditService.getTermChangesInitialData(creditNr);
        if(result.isSingleRepaymentLoan || result.creditStatus !== 'Normal') {
            this.m = ({
                creditNr: creditNr,
                disabledMessage: result.isSingleRepaymentLoan
                ? 'Terms change are not supported on single repayment loans'
                : 'Credit is not active'
            } as any);
            return;
        }

        let minAllowedMarginInterestRate = result.minAllowedMarginInterestRate
            ? result.minAllowedMarginInterestRate
            : 0.00001;
        let maxAllowedMarginInterestRate = result.maxAllowedMarginInterestRate
            ? result.maxAllowedMarginInterestRate
            : 300;
        let f = new FormsHelper(
            this.fb.group({
                calcMarginInterestRatePercent: [
                    '',
                    [
                        Validators.required,
                        this.validationService.getValidator('calcMarginInterestRatePercent', (x) => {
                            if (!this.validationService.isValidPositiveDecimal(x)) {
                                return false;
                            }
                            let newInterestRate = this.validationService.parseDecimalOrNull(x, false);
                            if (newInterestRate === null) {
                                return false;
                            }
                            return !(
                                newInterestRate < minAllowedMarginInterestRate ||
                                newInterestRate > maxAllowedMarginInterestRate
                            );
                        }),
                    ],
                ],
                calcNrOfRemainingPayments: [
                    '',
                    [
                        Validators.required,
                        this.validationService.getValidator('calcNrOfRemainingPayments', (x) => {
                            let newCount = this.validationService.parseIntegerOrNull(x, false);
                            if (newCount === null) {
                                return false;
                            }
                            return !(newCount < 1 || newCount > 1200);
                        }),
                    ],
                ],
            })
        );

        this.m = {
            ...result,
            disabledMessage: null,
            creditNr: creditNr,
            calculateForm: f,
            hasEmailProvider: this.configService.hasEmailProvider(),
        };
    }

    getArchiveDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey, true);
    }

    isAcceptPendingChangeAllowed() {
        if (!(this.m.pendingTerms && this.m.pendingTerms.Signatures)) {
            return false;
        }

        var haveAllApplicantsSigned = true;
        for (let s of this.m.pendingTerms.Signatures) {
            if (!s.SignedDocumentKey) {
                haveAllApplicantsSigned = false;
            }
        }
        return this.m.pendingTerms.Signatures.length > 0 && haveAllApplicantsSigned;
    }

    async computeNewTerms(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.calculateForm;

        let calcMarginInterestRatePercent = this.validationService.parseDecimalOrNull(
            f.getValue('calcMarginInterestRatePercent'),
            false
        );
        let calcNrOfRemainingPayments = this.validationService.parseIntegerOrNull(
            f.getValue('calcNrOfRemainingPayments')
        );

        try {
            let result = await this.creditService.computeNewCreditTermChanges(
                this.m.creditNr,
                calcNrOfRemainingPayments,
                calcMarginInterestRatePercent
            );
            this.m.computedNewTerms = result?.newTerms;
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    async cancelPendingTermsChange(evt?: Event) {
        evt?.preventDefault();

        try {
            await this.creditService.cancelPendingCreditTermChange(this.m.pendingTerms.Id);
            this.m.pendingTerms = null;
            this.m.computedNewTerms = null;
            this.eventService.signalReloadCreditComments(this.m.creditNr);
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    async acceptPendingTermsChange(evt?: Event) {
        evt?.preventDefault();
        try {
            await this.creditService.acceptPendingCreditTermChange(this.m.pendingTerms.Id);
            let creditNr = this.m.creditNr;
            this.reload(creditNr);
            this.eventService.signalReloadCreditComments(creditNr);
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    async sendNewTerms(evt?: Event) {
        evt?.preventDefault();

        try {
            let m = this.m;
            let terms = m.computedNewTerms;
            let result = await this.creditService.sendNewCreditTermChange(
                m.creditNr,
                terms.NrOfRemainingPayments,
                terms.MarginInterestRatePercent
            );
            m.pendingTerms = result.pendingTerms;
            m.computedNewTerms = null;
            if (result.userWarningMessage) {
                this.toastr.warning(result.userWarningMessage);
            }
            this.eventService.signalReloadCreditComments(this.m.creditNr);
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    copySignatureLink(signatureApplicant: { SignatureUrl: string }, evt?: Event) {
        evt?.preventDefault();
        navigator.clipboard.writeText(signatureApplicant.SignatureUrl).then(() => {
            this.toastr.info('Signature link copied to clipboard');
        });
    }
}

interface Model extends GetCreditTermChangesResult {
    disabledMessage: string;
    creditNr: string;
    calculateForm: FormsHelper;
    hasEmailProvider: boolean;
    computedNewTerms?: TermsChangeData;
}
