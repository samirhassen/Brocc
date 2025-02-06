import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject, Subscription } from 'rxjs';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, getDictionaryValues } from 'src/app/common.types';
import { CreditDecisionEditorTabsComponentInitialData } from 'src/app/shared-application-components/components/credit-decision-editor-tabs/credit-decision-editor-tabs.component';
import { CreditDecisionRejectionEditorComponentInitialData } from 'src/app/shared-application-components/components/credit-decision-rejection-editor/credit-decision-rejection-editor.component';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';
import {
    MlCreditRecommendationModel,
    MortgageLoanApplicationApiService,
} from '../../services/mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';

@Component({
    selector: 'ml-credit-check-decision-editor',
    templateUrl: './ml-credit-check-decision-editor.component.html',
    styles: [],
})
export class MlCreditCheckDecisionEditorComponent {
    constructor(
        private validationService: NTechValidationService,
        private apiService: MortgageLoanApplicationApiService,
        private router: Router,
        private fb: UntypedFormBuilder,
        private toastr: ToastrService
    ) {}

    @Input()
    public initialData: MlCreditCheckDecisionEditorComponentInitialData;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        if (this?.m?.offer?.subscriptions) {
            for (let s of this?.m?.offer?.subscriptions) {
                s.unsubscribe();
            }
        }

        this.m = null;

        if (this.initialData == null) {
            return;
        }

        await this.init(this.initialData.application, this.initialData.recommendationTemporaryStorageKey);
    }

    private getMortgageLoansToSettleAmount(application: StandardMortgageLoanApplicationModel) {
        let mortgageLoansToSettle = application.getComplexApplicationList('MortgageLoansToSettle', true);
        let sum = 0;
        for (let rowNr of mortgageLoansToSettle.getRowNumbers()) {
            let row = mortgageLoansToSettle.getRow(rowNr, true);
            if (row.getUniqueItemBoolean('shouldBeSettled')) {
                sum += row.getUniqueItemInteger('currentDebtAmount');
            }
        }
        return sum;
    }

    private async init(application: StandardMortgageLoanApplicationModel, recommendationTemporaryStorageKey: string) {
        let isRejectTabActive = new BehaviorSubject<boolean>(false);
        let isCalculating = new BehaviorSubject<boolean>(false);

        let applicationRow = application.getComplexApplicationList('Application', true).getRow(1, true);
        let isPurchase = applicationRow.getUniqueItemBoolean('isPurchase');

        let offerForm = new FormsHelper(this.fb.group({}));
        let offerEditFields: Dictionary<EditblockFormFieldModel> = {};

        offerEditFields['objectPriceAmount'] = {
            getForm: () => offerForm,
            formControlName: 'objectPriceAmount',
            labelText: 'Purchase price',
            inEditMode: () => true,
            getOriginalValue: () => applicationRow.getUniqueItem('objectPriceAmount'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            conditional: {
                shouldExist: () => isPurchase,
            },
        };
        offerEditFields['paidToCustomerAmount'] = {
            getForm: () => offerForm,
            formControlName: 'paidToCustomerAmount',
            labelText: 'Additional loan amount',
            inEditMode: () => true,
            getOriginalValue: () => applicationRow.getUniqueItem('paidToCustomerAmount'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            conditional: {
                shouldExist: () => !isPurchase,
            },
        };
        offerEditFields['ownSavingsAmount'] = {
            getForm: () => offerForm,
            formControlName: 'ownSavingsAmount',
            labelText: 'Cash payment',
            inEditMode: () => true,
            getOriginalValue: () => applicationRow.getUniqueItem('ownSavingsAmount'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            conditional: {
                shouldExist: () => isPurchase,
            },
        };

        let model: Model = {
            rejectionEditorInitialData: {
                isActive: isRejectTabActive,
                isEditing: new BehaviorSubject<boolean>(false),
                onRejected: (x) => {
                    this.apiService
                        .setCurrentCreditDecision({
                            ApplicationNr: this.m.application.applicationNr,
                            WasAutomated: false,
                            SupressUserNotification: false,
                            Rejection: {
                                RejectionReasons: x.RejectionReasons,
                                OtherText: x.OtherText,
                            },
                            RecommendationTemporaryStorageKey: this.m.recommendationTemporaryStorageKey,
                        })
                        .then((x) => {
                            this.router.navigate([
                                '/mortgage-loan-application/application/',
                                this.m.application.applicationNr,
                            ]);
                        });
                },
                recommendation: this.initialData.recommendation,
            },
            application: application,
            recommendationTemporaryStorageKey: recommendationTemporaryStorageKey,
            tabsInitialData: {
                isCalculating: isCalculating,
                isRejectTabActive: isRejectTabActive,
                acceptTitle: 'Approve',
            },
            offer: {
                isPurchase: isPurchase,
                settlementAmount: this.getMortgageLoansToSettleAmount(application),
                form: offerForm,
                editFields: offerEditFields,
                subscriptions: EditblockFormFieldModel.setupForm(getDictionaryValues(offerEditFields), offerForm),
            },
            emailSettingName: this.initialData.isFinalCreditCheck
                ? 'finalCreditCheckApproveEmailTemplates'
                : 'initialCreditCheckApproveEmailTemplates',
        };

        this.registerTestFunctions(this.initialData.testFunctions);

        this.m = model;
    }

    getTotalAmount() {
        if (this.m.offer.isPurchase) {
            let objectPriceAmount = this.validationService.parseIntegerOrNull(
                this.m.offer.form.getValue('objectPriceAmount')
            );
            let ownSavingsAmount = this.validationService.parseIntegerOrNull(
                this.m.offer.form.getValue('ownSavingsAmount')
            );
            if (objectPriceAmount === null || ownSavingsAmount === null) {
                return null;
            }
            return this.m.offer.settlementAmount + objectPriceAmount - ownSavingsAmount;
        } else {
            let paidToCustomerAmount = this.validationService.parseIntegerOrNull(
                this.m.offer.form.getValue('paidToCustomerAmount')
            );
            if (paidToCustomerAmount === null) {
                return null;
            }
            return this.m.offer.settlementAmount + paidToCustomerAmount;
        }
    }

    accept(evt?: Event) {
        evt?.preventDefault();

        this.apiService
            .setCurrentCreditDecision({
                ApplicationNr: this.m.application.applicationNr,
                WasAutomated: false,
                SupressUserNotification: false,
                InitialOffer: {
                    PaidToCustomerAmount: this.validationService.parseIntegerOrNull(
                        this.m.offer.form.getValue('paidToCustomerAmount')
                    ),
                    OwnSavingsAmount: this.validationService.parseIntegerOrNull(
                        this.m.offer.form.getValue('ownSavingsAmount')
                    ),
                    IsPurchase: this.m.offer.isPurchase,
                    SettlementAmount: this.m.offer.settlementAmount,
                    ObjectPriceAmount: this.validationService.parseIntegerOrNull(
                        this.m.offer.form.getValue('objectPriceAmount')
                    ),
                },
                RecommendationTemporaryStorageKey: this.m.recommendationTemporaryStorageKey,
            })
            .then(
                (onSuccess) => {
                    this.router.navigate(['mortgage-loan-application/application/', this.m.application.applicationNr]);
                },
                (onError) => {
                    this.toastr.error(`Could not accept offer: ${onError.statusText}`);
                }
            );
    }

    isAcceptAllowed() {
        let totalAmount = this.getTotalAmount();
        if (totalAmount === null || totalAmount <= 0) {
            return false;
        }
        return true;
    }

    private registerTestFunctions(tf: TestFunctionsModel) {}
}

export class MlCreditCheckDecisionEditorComponentInitialData {
    isFinalCreditCheck: boolean;
    application: StandardMortgageLoanApplicationModel;
    testFunctions: TestFunctionsModel;
    recommendationTemporaryStorageKey: string;
    recommendation: MlCreditRecommendationModel;
}

class Model {
    application: StandardMortgageLoanApplicationModel;
    recommendationTemporaryStorageKey: string;
    rejectionEditorInitialData: CreditDecisionRejectionEditorComponentInitialData;
    tabsInitialData: CreditDecisionEditorTabsComponentInitialData;
    offer: {
        isPurchase: boolean;
        settlementAmount: number;
        form: FormsHelper;
        editFields: Dictionary<EditblockFormFieldModel>;
        subscriptions: Subscription[];
    };
    emailSettingName: string;
}
