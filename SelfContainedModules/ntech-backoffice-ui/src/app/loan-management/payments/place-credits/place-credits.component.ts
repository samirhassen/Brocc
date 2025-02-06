import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { NTechApiErrorResponse, NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { NTechMath } from 'src/app/common-services/ntech.math';
import { PaymentOrderService, PaymentOrderUiItem } from 'src/app/common-services/payment-order-service';
import { NullableNumber } from 'src/app/common.types';

@Component({
    selector: 'place-credits',
    templateUrl: './place-credits.component.html',
    styleUrls: ['./place-credits.component.scss']
})
export class PlaceCreditsComponent {
    constructor(private apiService: NtechApiService, private toastr: ToastrService, private formBuilder: UntypedFormBuilder,
        private validationService: NTechValidationService, private paymentOrderService: PaymentOrderService,
        private config: ConfigService) { }

    @Input()
    public initialData: PlaceCreditsInitialData

    public m: Model;


    async ngOnChanges(_: SimpleChanges) {
        await this.reload();
    }

    async reload() {
        this.m = null;

        let initialData = this.initialData;

        if (!initialData) {
            return;
        }

        let paymentSettings = await this.apiService.shared.getCurrentSettingValues('paymentPlacement');
        let maxNotNotifiedCapitalWriteOffAmount =  parseInt(paymentSettings.SettingValues['maxUiNotNotifiedCapitalWriteOffAmount']);

        let result = await this.apiService.post<PlacementInstructionResult>('NTechHost', 'Api/Credit/PaymentPlacement/Compute-PlacementSuggestion', {
            creditNrs: initialData.creditNrs,
            paymentId: initialData.paymentId,
            onlyPlaceAgainstNotified: initialData.onlyPlaceAgainstNotified,
            onlyPlaceAgainstPaymentOrderItemUniqueId: initialData.onlyPlaceAgainstPaymentOrderItemUniqueId ? initialData.onlyPlaceAgainstPaymentOrderItemUniqueId : null,
            maxPlacedAmount: initialData.maxPlacedAmount?.value
        });
        let instruction = result.instruction;

        let paymentOrderUiItems = initialData.paymentOrderUiItems ?? await this.paymentOrderService.getPaymentOrderUiItems();

        let m: Model = {
            matchedCreditNrsText: initialData.creditNrs.join(', '),
            maxNotNotifiedCapitalWriteOffAmount: maxNotNotifiedCapitalWriteOffAmount,
            initialInstruction: JSON.parse(JSON.stringify(instruction)),
            paymentOrderUiItems: paymentOrderUiItems,
            unplacedAmount: instruction.leaveUnplacedAmount,
            allItems: [...instruction.notificationPlacementItems, ...instruction.notNotifiedPlacementItems],
            notifications: [],
            notNotified: {
                loans: [],
                editForm: new FormsHelper(this.formBuilder.group({
                    'editAmount': ['', [Validators.required, this.validationService.getPositiveDecimalValidator()]]
                })),
                editItem: null
            },
            isMortgageLoanSeClient: this.config.baseCountry() === 'SE' && this.config.isFeatureEnabled('ntech.feature.mortgageloans')
        }

        for(let item of instruction.notificationPlacementItems) {
            let n = m.notifications.find(x => x.dueDate === item.notificationDueDate && x.creditNr === item.creditNr);
            if(!n)  {
                m.notifications.push({
                    creditNr: item.creditNr,
                    dueDate: item.notificationDueDate,
                    items: [item],
                    isExpanded: false
                });
            } else {
                n.items.push(item);
            }
        }
        for(let item of instruction.notNotifiedPlacementItems) {
            let loan = m.notNotified.loans.find(x => x.creditNr === item.creditNr);
            if(!loan) {
                m.notNotified.loans.push({
                    creditNr: item.creditNr,
                    items: [item],
                    isExpanded: false
                });
            } else {
                loan.items.push(item);
            }
        }

        this.m = m
    }

    public currentSum(items: PaymentPlacementItem[]) {
        return items.map(x => x.amountCurrent).reduce((x, y) => x + y, 0);
    }

    public placedSum(items: PaymentPlacementItem[]) {
        return items.map(x => x.amountPlaced).reduce((x, y) => x + y, 0);
    }

    public writtenOffSum(items: PaymentPlacementItem[]) {
        return items.map(x => x.amountWrittenOff).reduce((x, y) => x + y, 0);
    }

    public remainingSum(items: PaymentPlacementItem[]) {
        return items.map(x => x.amountCurrent - x.amountPlaced - x.amountWrittenOff).reduce((x, y) => x + y, 0);
    }

    public writeOffNotificationItem(item : PaymentPlacementItem, writeOffType : 'remaining' | 'all', evt ?: Event) {
        evt?.preventDefault();
        if(writeOffType === 'all') {
            this.writeOffItemAll(item);
        } else if(writeOffType === 'remaining') {
            item.amountWrittenOff = this.remainingAmount(item);
        }
    }

    public moveNotificationItemToNotNotified(item : PaymentPlacementItem, moveType : 'remaining' | 'all', evt ?: Event) {
        evt?.preventDefault();

        if(moveType === 'all') {
            this.writeOffItemAll(item);
        } else if(moveType === 'remaining') {
            let targetItem = this.m.allItems.find(x => x.itemId === item.moveToUnplacedItemId);
            if(!targetItem) {
                this.toastr.error('Target item missing')
                return;
            }
            let remainingAmount = this.remainingAmount(item);
            item.amountWrittenOff += remainingAmount;
            targetItem.amountCurrent += remainingAmount;
        }
    }

    public remainingAmount(item: PaymentPlacementItem) {
        return item.amountCurrent - item.amountPlaced - item.amountWrittenOff;
    }

    public distributeUnplaced(evt ?: Event) {
        evt?.preventDefault();

        for(let item of this.m.allItems) {
            let remainingAmount = this.remainingAmount(item);
            if(remainingAmount > 0) {
                let placedAmount = Math.min(this.m.unplacedAmount, remainingAmount);
                item.amountPlaced += placedAmount;
                this.m.unplacedAmount -= placedAmount;
            }
            if(this.m.unplacedAmount <= 0) {
                return;
            }
        }
    }

    public reset(evt ?: Event) {
        evt?.preventDefault();
        this.reload();
    }

    public totalNotifiedCurrent() {
        return NTechMath.sum(this.m.allItems.filter(x => !!x.notificationId), x => x.amountCurrent);
    }

    public totalNotifiedPlaced() {
        return NTechMath.sum(this.m.allItems.filter(x => !!x.notificationId), x => x.amountPlaced);
    }

    public totalNotifiedWrittenOff() {
        return NTechMath.sum(this.m.allItems.filter(x => !!x.notificationId), x => x.amountWrittenOff);
    }

    public totalNotifiedRemaining() {
        return NTechMath.sum(this.m.allItems.filter(x => !!x.notificationId), x => this.remainingAmount(x));
    }

    public getMaxNotNotifiedWriteOffAmount(item: PaymentPlacementItem) {
        let maxWriteOffAmount = item.amountCurrent - item.amountPlaced;
        if(item.costTypeUniqueId === 'b_Capital') {
            //Capital also has a global limit besides not allowing the balance to go negative
            return Math.min(maxWriteOffAmount, this.m.maxNotNotifiedCapitalWriteOffAmount);
        } else {
            return maxWriteOffAmount
        }
    }

    public editNotNotifiedItem(item : PaymentPlacementItem, evt ?: Event) {
        evt.preventDefault();
        let nn = this.m.notNotified;

        if(item.itemType === 'PlaceOrWriteoff') {
            nn.editForm.setValue('editAmount', this.validationService.formatDecimalForEdit(item.amountWrittenOff));
        } else {
            nn.editForm.setValue('editAmount', this.validationService.formatDecimalForEdit(item.amountCurrent));
        }
        nn.editItem = item;
    }

    public isThisBeingEdited(item: PaymentPlacementItem) {
        return this.m.notNotified.editItem && this.m.notNotified.editItem.itemId === item.itemId;
    }

    public commitEdit(evt ?: Event) {
        evt?.preventDefault();
        let editItem = this.m.notNotified.editItem;

        let editAmount = this.validationService.parseDecimalOrNull(this.m.notNotified.editForm.getValue('editAmount'), true);
        if (editItem.itemType === 'PlaceOrWriteoff') {
            let writeOffAmount = editAmount;

            if(writeOffAmount > this.getMaxNotNotifiedWriteOffAmount(editItem)) {
                this.toastr.warning('Over max amount');
                return;
            }

            editItem.amountWrittenOff = writeOffAmount;
        } else {
            let placedAmount = editAmount;
            if(placedAmount < 0) {
                this.toastr.warning('Value cannot be negative');
                return;
            }

            if(placedAmount > editItem.amountPlaced + this.m.unplacedAmount) {
                this.toastr.warning('Not enough unplaced');
                return;
            }

            this.m.unplacedAmount += editItem.amountPlaced - placedAmount;
            editItem.amountPlaced = placedAmount;
            editItem.amountCurrent = placedAmount;
            editItem.amountWrittenOff = 0;
        }

        this.m.notNotified.editItem = null;
    }

    public cancelEdit(evt ?: Event) {
        evt?.preventDefault();

        this.m.notNotified.editItem = null;
    }

    public async placePayment(evt ?: Event) {
        evt?.preventDefault();

        let placeInstruction : PlacementInstruction = {
            initialPaymentAmount: this.m.initialInstruction.initialPaymentAmount,
            leaveUnplacedAmount: this.m.unplacedAmount,
            notificationPlacementItems: this.m.allItems.filter(x => !!x.notificationId),
            notNotifiedPlacementItems: this.m.allItems.filter(x => !x.notificationId)
        };

        this.m.isPlacing = true;
        setTimeout(() => {
            this.apiService.post<{ error: NTechApiErrorResponse }>('NTechHost', 'Api/Credit/PaymentPlacement/Place-PlacementSuggestion', {
                paymentId: this.initialData.paymentId,
                instruction: placeInstruction
            }, { handleNTechError: error => ({error}) } ).then(x => {
                if(x?.error) {
                    this.toastr.error(x.error.errorMessage);
                    this.m.isPlacing = false;
                } else {
                    document.location.href = this.apiService.getUiGatewayUrl('nCredit', 'Ui/UnplacedPayments/List');
                }
            })
        });
    }

    public itemText(item: PaymentPlacementItem) {
        if(item.costTypeUniqueId === 'b_SwedishRseAmount') {
            return 'RSE';
        } else {
            return this.m.paymentOrderUiItems.find(x => x.uniqueId === item.costTypeUniqueId)?.text ?? item.costTypeUniqueId;
        }
    }

    public placedAmount()  {
        return this.m.allItems.reduce((sum, x) => sum + x.amountPlaced, 0);
    }

    public isVisibleNotNotifiedItem(item: PaymentPlacementItem) {
        if(item.amountCurrent > 0) {
            return true;
        }
        if(item.costTypeUniqueId === 'b_SwedishRseAmount' && !item.notificationId  && this.m.isMortgageLoanSeClient) {
            return true;
        }
        if(item.costTypeUniqueId === 'b_Interest' && !item.notificationId) {
            return true;
        }
        return false;
    }

    public computedNotNotifiedCurrent(item: PaymentPlacementItem) {
        if(item.hasAmountCurrentComputed && item.amountCurrent !== item.amountCurrentComputed) {
            return item.amountCurrentComputed;
        } else {
            return null;
        }
    }

    private  writeOffItemAll(item: PaymentPlacementItem) {
        let moveToNotNotifiedAmount = item.amountCurrent - item.amountWrittenOff;
        let restoredUnplacedAmount = item.amountPlaced;
        if(item.itemType === 'MoveToUnplacedOrPlace' && moveToNotNotifiedAmount > 0) {
            let targetItem = this.m.allItems.find(x => x.itemId === item.moveToUnplacedItemId);
            if(!targetItem) {
                this.toastr.error('Target item missing')
                return;
            }
            targetItem.amountCurrent += moveToNotNotifiedAmount;
        }

        item.amountPlaced = 0;
        item.amountWrittenOff = item.amountCurrent;
        this.m.unplacedAmount += restoredUnplacedAmount;
    }
}

interface Model {
    matchedCreditNrsText: string
    isPlacing ?: boolean
    maxNotNotifiedCapitalWriteOffAmount: number
    initialInstruction: PlacementInstruction
    paymentOrderUiItems: PaymentOrderUiItem[]
    unplacedAmount: number
    allItems: PaymentPlacementItem[]
    notifications: NotificationModel[]
    notNotified: {
        loans: {
            creditNr: string
            items: PaymentPlacementItem[]
            isExpanded: boolean
        }[]
        editForm: FormsHelper,
        editItem: PaymentPlacementItem
    }
    isMortgageLoanSeClient: boolean
}

interface NotificationModel {
    creditNr: string
    dueDate: string
    isExpanded: boolean
    items: PaymentPlacementItem[]
}

export interface PlaceCreditsInitialData {
    creditNrs: string[]
    onlyPlaceAgainstNotified: boolean
    onlyPlaceAgainstPaymentOrderItemUniqueId : string
    maxPlacedAmount: NullableNumber
    paymentId: number,
    paymentOrderUiItems ?: PaymentOrderUiItem[]
}

export interface PaymentPlacementItem {
    amountCurrent: number
    itemId: number
    creditNr: string
    itemType: string
    amountPlaced: number
    amountWrittenOff: number
    notificationId: number
    notificationDueDate: string
    costTypeUniqueId: string
    moveToUnplacedItemId: number
    hasAmountCurrentComputed : boolean
    amountCurrentComputed ?: number
}

interface PlacementInstructionResult {
    instruction: PlacementInstruction
}
interface PlacementInstruction {
    initialPaymentAmount: number,
    leaveUnplacedAmount: number,
    notificationPlacementItems: PaymentPlacementItem[],
    notNotifiedPlacementItems: PaymentPlacementItem[]
}