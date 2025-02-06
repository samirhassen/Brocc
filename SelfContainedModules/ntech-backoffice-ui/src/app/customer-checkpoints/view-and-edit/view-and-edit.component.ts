import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, distinct, NumberDictionary } from 'src/app/common.types';
import { CheckpointCode, CustomerCheckpointService, StateModel } from '../customer-checkpoint.service';

@Component({
    selector: 'view-and-edit-checkpoint',
    templateUrl: './view-and-edit.component.html',
    styles: [
    ]
})
export class ViewAndEditComponent {
    constructor(private apiService: NtechApiService, private formBuilder: UntypedFormBuilder,
        private validationService: NTechValidationService, private customerCheckpointService: CustomerCheckpointService) { }

    @Input()
    public initialData: ViewAndEditComponentInitialData;

    public m: Model;


    async ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if(!this.initialData) {
            return;
        }

        await this.reload(this.initialData.customerId, this.initialData.civicRegNr);
    }    

    private async reload(customerId: number, civicRegNr ?: string) {
        const result = await this.customerCheckpointService.fetchStateAndHistoryForCustomer(customerId);

        let userIds : number[] = []
        if(result.currentState) {
            userIds.push(result.currentState.stateBy)
        }
        if(result.historyStates) {
            userIds.push(...result.historyStates.map(x => x.stateBy))
        }

        let userDisplayNamesByUserId = await this.apiService.shared.getUserDisplayNames(distinct(userIds));
        this.m = {
            activeCodes: this.customerCheckpointService.getActiveCheckpointCodes(),
            customerId: customerId,
            view : {
                civicRegNr: civicRegNr ? civicRegNr : null,
                currentState: result.currentState,
                historyStates: result.historyStates ?? [],
                clearTextReasons: {},
                isHistoricalReasonUnlocked: {},
                userDisplayNamesByUserId : userDisplayNamesByUserId
            }
        }
    }


    async ensureReasonText(id: number) {
        if (this.m.view.clearTextReasons[id]) {
            return;
        }
        let { reasonText } = await this.customerCheckpointService.fetchReasonText(id);
        this.m.view.clearTextReasons[id] = reasonText;
    }

    async unlockCurrentReason(evt?: Event) {
        evt?.preventDefault();

        await this.ensureReasonText(this.m.view.currentState.id);

        this.m.view.isCurrentClearTextReasonUnlocked = true;
    }

    async unlockHistoricalReason(id: number, evt?: Event) {
        evt?.preventDefault();

        await this.ensureReasonText(id);

        this.m.view.isHistoricalReasonUnlocked[id] = true;
    }

    async beginEdit(evt?: Event) {
        evt?.preventDefault();

        const currentState = this.m.view.currentState;
        let clearTextReason = ''

        let hasCodes = currentState?.codes?.length > 0;
        if (hasCodes) {
            await this.ensureReasonText(currentState.id);
            clearTextReason = this.m.view.clearTextReasons[currentState.id];
        }

        let controls: Dictionary<any> = {
            'clearTextReason': [clearTextReason, []]            
        };

        for(let activeCode of this.m.activeCodes) {
            let isCodeEnabled = (currentState?.codes ?? []).indexOf(activeCode.code) >= 0;
            controls[activeCode.code] = [isCodeEnabled, []];
        }

        const editForm = new FormsHelper(this.formBuilder.group(controls));

        editForm.setFormValidator(_ => {
            if (!this.isAnyEditCodeEnabled()) {
                return true;
            }

            return !this.validationService.isNullOrWhitespace(editForm.getValue('clearTextReason'));
        })

        this.m.edit = {
            form: editForm
        }
    }

    async commitEdit(evt?: Event) {
        evt?.preventDefault();

        let form = this.m.edit.form;
        let customerId = this.m.customerId;
        let newCodes : string[] = [];

        for(let activeCode of this.m.activeCodes) {
            if(form.getValue(activeCode.code) === true) {
                newCodes.push(activeCode.code);
            }
        }
        
        const reasonText = newCodes.length > 0 ? form.getValue('clearTextReason') : null;

        await this.customerCheckpointService.setCheckpointState(customerId, newCodes, reasonText);

        this.reload(customerId)
    }

    cancelEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.edit = null;
    }

    getUserDisplayName(h: StateModel) {
        return this.m.view.userDisplayNamesByUserId[h.stateBy];
    }

    isCodeEnabled(state: StateModel, code: string) {
        return (state?.codes ?? []).indexOf(code) >= 0;
    }

    isAnyEditCodeEnabled() {
        if(!this.m?.edit) {
            return false;
        }
        for(let activeCode of this.m.activeCodes) {
            if(this.m.edit.form.getValue(activeCode.code) === true) {
                return true;
            }
        }
        return false;
    }

    public nullToEmptyArray<T>(items: T[]) {
        if(!items) {
            return [];
        }
        return items;
    }

    public getCodeDisplayName(code: string) {
        let activeCode = this.m?.activeCodes?.find(x => x.code === code);
        return activeCode?.displayName ?? code;
    }
}

class Model {
    customerId: number
    activeCodes : CheckpointCode[]
    view?: {
        civicRegNr: string
        isCurrentClearTextReasonUnlocked?: boolean
        currentState?: StateModel
        historyStates?: StateModel[]
        clearTextReasons: NumberDictionary<string>
        isHistoricalReasonUnlocked: NumberDictionary<boolean>
        userDisplayNamesByUserId: NumberDictionary<string>
    }
    edit?: {
        form: FormsHelper
    }
}

export interface ViewAndEditComponentInitialData {
    customerId: number
    civicRegNr ?: string
}