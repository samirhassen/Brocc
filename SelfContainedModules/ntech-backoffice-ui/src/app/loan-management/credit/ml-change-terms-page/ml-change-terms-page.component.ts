import { Component, ElementRef, ViewChild } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, ParamMap } from '@angular/router';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { FileInputEventTarget } from 'src/app/common.types';
import { FixedRateService, RateServerModel } from '../../services/fixed-rate-service';
import {
    CreditService,
    MlGetCreditTermChangesResult,
    MlTermsChangeData,
} from '../credit.service';
import { MlChangeTermsSignatureService } from './ml-change-terms-signature.service';

const AlmostZero = 0.000001;

@Component({
    selector: 'app-ml-change-terms-page',
    templateUrl: './ml-change-terms-page.component.html',
    styles: [],
})
export class MlChangeTermsPageComponent {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private generalApiService: NtechApiService,
        private fixedRateService: FixedRateService,
        private fb: UntypedFormBuilder,
        private validationService: NTechValidationService,
        private toastr: ToastrService,
        private configService: ConfigService
    ) {}

    @ViewChild('fileInput')
    fileInput: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputForm')
    fileInputForm: ElementRef<HTMLFormElement>;

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('creditNr'));
        });
    }

    public m: Model;

    private async reload(creditNr: string) {
        this.m = null;

        if (!creditNr) {
            const title = 'Credit change terms';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }

        this.eventService.setCustomPageTitle(`Credit ${creditNr}`, `Credit change terms ${creditNr}`);

        let result = await this.creditService.getMlTermChangesInitialData(creditNr);

        let currentRates = (await this.fixedRateService.getCurrentRates()).CurrentRates;
        let signatureService = new MlChangeTermsSignatureService(this.generalApiService);
        let signedAgreement = this.getSignedAgreement(result);

        this.m = {
            ...result,
            currentRates: currentRates,
            agreementDocument: signedAgreement,
            unsignedAgreement: result.pendingTerms ? {
                DocumentArchiveKey: result.pendingTerms?.Signature?.UnsignedDocumentKey,
                signatureCustomers: signedAgreement ? null : (await signatureService.getSignatureCustomers(result.pendingTerms.ActiveSignatureSessionKey))
            } : null,
            creditNr: creditNr,
            calculateForm: this.getCalculateForm(
                result.minAllowedMarginInterestRate,
                result.maxAllowedMarginInterestRate,
                result.currentTerms?.interestBoundUntil
            ),
        };
    }

    getCalculateForm(
        resultMinAllowedMarginInterestRate: number,
        resultMaxAllowedMarginInterestRate: number,
        currentInterestBoundToDate: Date
    ) {
        const minAllowedMarginInterestRate = resultMinAllowedMarginInterestRate ?? -300;
        const maxAllowedMarginInterestRate = resultMaxAllowedMarginInterestRate ?? 300;

        let form = new FormsHelper(
            this.fb.group({
                calcMarginInterestRatePercent: [
                    '',
                    [
                        Validators.required,
                        this.validationService.getValidator('calcMarginInterestRatePercent', (x) => {
                            if (!this.validationService.isValidDecimal(x, true)) {
                                return false;
                            }
                            let newInterestRate = this.validationService.parseDecimalOrNull(x, true);
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
                calcFixedTime: [
                    '',
                    [
                        Validators.required,
                        this.validationService.getValidator('calcFixedTime', (x) => {
                            let newCount = this.validationService.parseIntegerOrNull(x, false);
                            if (newCount === null) {
                                return false;
                            }
                            return !(newCount < 1 || newCount > 1200);
                        }),
                    ],
                ],
                calcFromDate: [
                    this.getInterestBoundUntilOrToday(currentInterestBoundToDate),
                    [
                        Validators.required,
                        this.validationService.getValidator('calcFromDate', (x) => {
                            let newFromDate = this.validationService.parseDateOnlyOrNull(x);
                            if (newFromDate === null) {
                                return false;
                            }

                            if (!this.getIsValidFromDate(newFromDate, currentInterestBoundToDate)) {
                                return false;
                            }

                            return newFromDate !== null;
                        }),
                    ],
                ],
            })
        );

        form.setFormValidator((x) => {
            let newInterestRate = this.validationService.parseDecimalOrNull(
                form.getValue('calcMarginInterestRatePercent'),
                true
            );
            let newCount = this.validationService.parseIntegerOrNull(form.getValue('calcFixedTime'), false);
            if (newInterestRate === null || newCount === null) {
                //Meaning this validation is done by the individual validators
                return true;
            }

            if (!this.m?.currentRates) {
                return false;
            }

            let referenceRatePercent = this.m.currentRates.find((x) => x.MonthCount === newCount).RatePercent;

            return referenceRatePercent + newInterestRate > AlmostZero;
        }, 'timeInterestCombination');

        return form;
    }

    getSignedAgreement(result: MlGetCreditTermChangesResult) {
        if (!result.pendingTerms) {
            return null;
        }

        let signature = result.pendingTerms?.Signature;
        if(!signature?.SignedDocumentKey) {
            return null;
        }
        
        return {
                  isEditAllowed: false,
                  isEditing: false,
                  unsignedAgreement: {
                      DocumentArchiveKey: signature.UnsignedDocumentKey
                  },
                  signedAgreement: {
                      DocumentArchiveKey: signature.SignedDocumentKey
                  },
                  attachedFile: {
                      name: 'test',
                      dataUrl: this.getArchiveDocumentUrl(signature.SignedDocumentKey),
                  },
              };
    }

    displayAsYearsText(months: number): string {
        if (!months) {
            return '';
        }

        if (months < 12) {
            return `${months} months`;
        }

        const yearPostfix = months < 13 ? 'year' : 'years';
        return `${months / 12} ${yearPostfix}`;
    }

    getArchiveDocumentUrl(archiveKey: string) {
        return this.generalApiService.getArchiveDocumentUrl(archiveKey, true);
    }

    isAcceptPendingChangeAllowed() {
        return (
            this.m.pendingTerms &&
            !this.m.pendingTerms?.ScheduledDate &&
            this.m.pendingTerms?.Signature?.SignedDocumentKey
        );
    }

    async computeNewTerms(evt?: Event) {
        evt?.preventDefault();

        const values = this.getComputeValues();

        try {
            let result = await this.creditService.mlComputeNewCreditTermChanges(
                this.m.creditNr,
                values.fixedMonthsCount,
                values.marginInterest,
                values.interestBoundFrom,
                values.referenceInterest
            );
            this.m.computedNewTerms = result?.newTerms;
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    async cancelPendingTermsChange(evt?: Event) {
        evt?.preventDefault();

        try {
            const signedDocumentKey = this.m.pendingTerms?.Signature?.SignedDocumentKey;
            if (signedDocumentKey) {
                await this.creditService.mlRemoveAttachedChangeTermsAgreeement(this.m.pendingTerms.Id, signedDocumentKey);
            }
            await this.creditService.mlCancelPendingCreditTermChange(this.m.pendingTerms.Id);
            this.m.pendingTerms = null;
            this.m.computedNewTerms = null;
            await this.reload(this.m.creditNr);
            this.eventService.signalReloadCreditComments(this.m.creditNr);
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    async schedulePendingTermsChange(evt?: Event) {
        evt?.preventDefault();
        try {
            await this.creditService.mlSchedulePendingCreditTermChange(this.m.pendingTerms.Id);
            const creditNr = this.m.creditNr;
            await this.reload(creditNr);
            this.eventService.signalReloadCreditComments(creditNr);
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    async startCreditTermsChange(evt?: Event) {
        evt?.preventDefault();
        try {
            let m = this.m;
            let result = await this.creditService.mlStartCreditTermsChange(
                m.creditNr,
                m.computedNewTerms.NewInterestRebindMonthCount,
                m.computedNewTerms.MarginInterest,
                m.computedNewTerms.InterestBoundFrom,
                m.computedNewTerms.ReferenceInterest
            );
            if (result.userWarningMessage) {
                this.toastr.warning(result.userWarningMessage);
            }            
            this.reload(m.creditNr);
            this.eventService.signalReloadCreditComments(m.creditNr);
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    selectFileToAttach(evt: Event) {
        evt?.preventDefault();

        this.m.attachContext = 'attachSignedAgreement';
        this.fileInput.nativeElement.click();
    }

    async onFileAttached(evt: Event) {
        let target: FileInputEventTarget = (evt as any).target;
        try {
            let x = await FormsHelper.loadSingleAttachedFileAsDataUrl(target.files);
            let attachedFile = {
                name: x.filename,
                dataUrl: x.dataUrl,
            };
            if (this.m.attachContext === 'attachSignedAgreement') {
                this.m.agreementDocument = {
                    isEditAllowed: true,
                    isEditing: false,
                    signedAgreement: {
                        DocumentArchiveKey: '',
                    },
                    attachedFile: attachedFile,
                    isPendingRemoval: false,
                };
            }
    
            await this.creditService
                .mlAttachChangeTermsAgreeement(this.m.pendingTerms.Id, x.dataUrl, x.filename);

            this.m.attachContext = null;
            this.fileInputForm.nativeElement.reset();                

            this.reload(this.m.creditNr);
        } catch(err : any) {
            this.toastr.error(err);
            this.m.attachContext = null;
            this.fileInputForm.nativeElement.reset();
        }
    }

    attachEditDocument(evt?: Event) {
        evt?.preventDefault();

        this.m.attachContext = 'attachSignedAgreement';
        this.fileInput.nativeElement.click();
    }

    async removeEditDocument(evt?: Event) {
        evt?.preventDefault();
        try {
            let signedDocumentKey = this.m.pendingTerms?.Signature?.SignedDocumentKey;
            await this.creditService.mlRemoveAttachedChangeTermsAgreeement(this.m.pendingTerms.Id, signedDocumentKey);
            this.reload(this.m.creditNr);
        } catch (e) {
            this.toastr.error('Could not remove signature document.', 'Error');
        }
    }

    getDocumentUrl(archiveKey: string) {
        return this.generalApiService.getArchiveDocumentUrl(archiveKey);
    }

    setDateToTomorrow(evt?: Event) {
        evt?.preventDefault();

        const tomorrow = moment(this.configService.getCurrentDateAndTime().add(1, 'days'));
        return this.m?.calculateForm.setValue('calcFromDate', moment(tomorrow).format('YYYY-MM-DD'));
    }

    getInterestBoundUntilOrToday(currentInterestBoundToDate: Date) {
        return moment(currentInterestBoundToDate) > moment(this.configService.getCurrentDateAndTime())
            ? moment(currentInterestBoundToDate).format('YYYY-MM-DD')
            : moment(this.configService.getCurrentDateAndTime().add(1, 'days')).format('YYYY-MM-DD');
    }

    getIsValidFromDate(newFromDate: string, currentInterestBoundToDate: Date) {
        if (moment(newFromDate) < moment(this.configService.getCurrentDateAndTime())) {
            return false;
        }

        if (
            moment(currentInterestBoundToDate) > moment(this.configService.getCurrentDateAndTime()) &&
            moment(newFromDate) > moment(currentInterestBoundToDate)
        ) {
            return false;
        }

        return true;
    }

    getComputeValues(): {
        fixedMonthsCount: number;
        marginInterest: number;
        interestBoundFrom: string;
        referenceInterest: number;
    } {
        const marginInterest = this.validationService.parseDecimalOrNull(
            this.m.calculateForm.getValue('calcMarginInterestRatePercent'),
            true
        );

        const fixedMonthsCount = this.validationService.parseIntegerOrNull(
            this.m.calculateForm.getValue('calcFixedTime')
        );

        const interestBoundFrom = this.validationService.parseDateOnlyOrNull(
            this.m.calculateForm.getValue('calcFromDate')
        );

        const refInterest = this.m.currentRates.filter((x) => x.MonthCount === fixedMonthsCount);
        const referenceInterest = refInterest[0] !== null ? refInterest[0].RatePercent : 0;

        return { fixedMonthsCount, marginInterest, interestBoundFrom, referenceInterest };
    }

    getIconClass(isAccepted: boolean, isRejected: boolean) {
        let isOther = !isAccepted && !isRejected;
        return {
            'glyphicon-ok': isAccepted,
            'glyphicon-remove': isRejected,
            'glyphicon-minus': isOther,
            'glyphicon': true,
            'text-success': isAccepted,
            'text-danger': isRejected,
        };
    }

    copyToClipboard(value: string, evt ?: Event) {
        evt?.preventDefault();
        navigator.clipboard
            .writeText(value)
            .then(() => {
                this.toastr.info('Link copied to clipboard');
            })
            .catch((e) => {
                this.toastr.error('Failed to copy the link to the clipboard.');
            });        
    }
}

interface Model extends MlGetCreditTermChangesResult {
    creditNr: string;
    currentRates: RateServerModel[];
    calculateForm: FormsHelper;
    computedNewTerms?: MlTermsChangeData;
    attachContext?: string;
    agreementDocument?: {
        isEditAllowed: boolean;
        isEditing: boolean;
        signedAgreement: {
            DocumentArchiveKey: string;
        };
        attachedFile?: {
            name: string;
            dataUrl: string;
        };
        isPendingRemoval?: boolean;
    };
    unsignedAgreement?: {
        DocumentArchiveKey: string;
        signatureCustomers: { fullName: string, hasSigned: boolean, signatureUrl: string }[]
    };
}
