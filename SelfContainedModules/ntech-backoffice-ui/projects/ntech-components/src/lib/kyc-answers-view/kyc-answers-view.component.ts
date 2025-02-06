import { Component, Input, SimpleChanges } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { KycQuestionAnswerModel } from '../kyc-answers-editor/kyc-answers-editor.component';

@Component({
    selector: 'kyc-answers-view',
    templateUrl: './kyc-answers-view.component.html',
    styleUrls: ['./kyc-answers-view.component.css'],
})
export class KycAnswersViewComponent {
    constructor() {}

    public m: Model;

    @Input()
    public initialData: KycAnswersViewInitialDataModel;

    public async onEdit(evt?: Event) {
        evt?.preventDefault();
        await this.initialData?.onEdit?.();
    }

    async ngOnChanges(_: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let i = this.initialData;

        this.m = {
            titleText: i.titleText ? i.titleText : null,
            isUpdateRequired: i.isUpdateRequired,
            nrOfDaysSinceAnswer: i.nrOfDaysSinceAnswer,
            answers: i.answers,
            hasAnswers: i.answers != null && i.answers.length > 0,
            showEditButton: !!i.onEdit,
        };
    }

    isEditDisabled() {
        return this.initialData?.isEditDisabled?.getValue() ?? false;
    }
}

interface Model {
    titleText: string;
    isUpdateRequired: boolean;
    nrOfDaysSinceAnswer: number;
    hasAnswers: boolean;
    answers: KycQuestionAnswerModel[];
    showEditButton: boolean;
}

export interface KycAnswersViewInitialDataModel {
    titleText: string;
    isUpdateRequired: boolean;
    nrOfDaysSinceAnswer: number;
    answers: KycQuestionAnswerModel[];
    onEdit?: () => Promise<void>;
    isEditDisabled?: BehaviorSubject<boolean>;
}
