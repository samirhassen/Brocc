import { Component, Input, SimpleChanges } from '@angular/core';
import {
    KycAnswersModel,
    KycAnswersViewInitialDataModel,
    KycQuestionAnswerModel,
} from 'projects/ntech-components/src/public-api';
import { BehaviorSubject } from 'rxjs';

@Component({
    selector: 'kyc-history',
    templateUrl: './kyc-history.component.html',
})
export class KycHistoryComponent {
    constructor() {}

    public m: Model;

    @Input()
    public initialData: KycAnswersModel[];

    async ngOnChanges(_: SimpleChanges) {
        this.m = {
            historicalAnswers: this.initialData,
        };
    }

    getViewData(answers: KycQuestionAnswerModel[]): KycAnswersViewInitialDataModel {
        return {
            titleText: '',
            isUpdateRequired: false,
            nrOfDaysSinceAnswer: 0,
            answers: answers,
            isEditDisabled: new BehaviorSubject<boolean>(true),
        };
    }
}

interface Model {
    historicalAnswers: KycAnswersModel[];
}
