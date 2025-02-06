import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { BehaviorSubject } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { Dictionary, NumberDictionary, orderBy } from 'src/app/common.types';
import { PolicyFilterCreditRecommendationModel } from 'src/app/loan-policyfilters/services/policy-filters-apiservice';
import { PolicyFilterCommonService } from 'src/app/policyfilter-components/policy-filter-common.service';

@Component({
    selector: 'credit-decision-rejection-editor',
    templateUrl: './credit-decision-rejection-editor.component.html',
    styles: [],
})
export class CreditDecisionRejectionEditorComponent {
    constructor(private fb: UntypedFormBuilder, private policyService: PolicyFilterCommonService) {}

    @Input()
    public initialData: CreditDecisionRejectionEditorComponentInitialData;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let rejectionReasons = await this.policyService.getRejectionReasonsList();

        let countPerColumn: NumberDictionary<number> = {};
        let rejectionReasonColumnAffinity: Dictionary<number> = {};

        let addReasonToColumn = (reason: string, column: number) => {
            if (rejectionReasonColumnAffinity[reason]) {
                return; //So we dont overwrite the preselected ones
            }
            rejectionReasonColumnAffinity[reason] = column;
            if (countPerColumn[column] === undefined) {
                countPerColumn[column] = 1;
            } else {
                countPerColumn[column] = countPerColumn[column] + 1;
            }
        };

        addReasonToColumn('paymentRemark', 1);
        addReasonToColumn('priorHistory', 1);
        addReasonToColumn('minimumDemands', 2);
        addReasonToColumn('alreadyApplied', 2);

        for (let rejectionReason of rejectionReasons) {
            let nextColumn = orderBy([1, 2, 3], (x) => (countPerColumn[x] === undefined ? 0 : countPerColumn[x]))[0];
            addReasonToColumn(rejectionReason.Code, nextColumn);
        }

        let model: Model = {
            rejectForm: null,
            rejectionReasons: [],
        };

        for (let rejectionReason of rejectionReasons) {
            let i = rejectionReasonColumnAffinity[rejectionReason.Code];
            model.rejectionReasons.push(
                new RejectionReasonViewModel(rejectionReason.DisplayName, rejectionReason.Code, i)
            );
        }

        let rejectControls: Dictionary<any> = {};
        rejectControls['rejectionReasonOtherText'] = ['', []];

        let recommendation = this.initialData.recommendation?.PolicyFilterResult;
        let initialReasons = recommendation?.IsAcceptRecommended === false ? recommendation.RejectionReasonNames : null;

        for (let reason of model.rejectionReasons) {
            let isInitiallySelected = initialReasons ? initialReasons.indexOf(reason.reasonCode) >= 0 : false;
            rejectControls[reason.formControlName()] = [isInitiallySelected, []];
        }
        model.rejectForm = new FormsHelper(this.fb.group(rejectControls));

        this.m = model;
    }

    getRejectionReasonColumn(columnNr: number) {
        return this.m.rejectionReasons.filter((x) => x.columnAffinity === columnNr);
    }

    anyRejectionReasonGiven() {
        return this.parseRejectedByReasons()?.RejectionReasons?.length > 0;
    }

    parseRejectedByReasons() {
        if (!this.m) {
            return null;
        }

        let result = {
            OtherText: null as string,
            RejectionReasons: [] as { Code: string; DisplayName: string }[],
        };

        for (let reason of this.m.rejectionReasons) {
            if (reason.isChecked(this.m.rejectForm)) {
                result.RejectionReasons.push({ Code: reason.reasonCode, DisplayName: reason.displayName });
            }
        }
        let otherText = this.m.rejectForm.getValue('rejectionReasonOtherText');
        if (otherText) {
            result.OtherText = otherText;
            result.RejectionReasons.push({ Code: 'other', DisplayName: 'Other ' + otherText });
        }

        return result;
    }

    reject(evt?: Event) {
        evt.preventDefault();

        let rejectedBy = this.parseRejectedByReasons();
        this.initialData.onRejected(rejectedBy);
    }
}

class Model {
    rejectForm: FormsHelper;
    rejectionReasons: RejectionReasonViewModel[];
}

export class CreditDecisionRejectionEditorComponentInitialData {
    isEditing: BehaviorSubject<boolean>;
    isActive: BehaviorSubject<boolean>;
    recommendation?: PolicyFilterCreditRecommendationModel;
    onRejected: (reason: RejectionReasonParsed) => void;
}

interface RejectionReasonParsed {
    OtherText: string;
    RejectionReasons: { Code: string; DisplayName: string }[];
}

class RejectionReasonViewModel {
    constructor(public displayName: string, public reasonCode: string, public columnAffinity: number) {}

    formControlName() {
        return `isRejectedForReason_${this.reasonCode}`;
    }

    isChecked(form: FormsHelper): boolean {
        return form.getValue(this.formControlName());
    }
}
