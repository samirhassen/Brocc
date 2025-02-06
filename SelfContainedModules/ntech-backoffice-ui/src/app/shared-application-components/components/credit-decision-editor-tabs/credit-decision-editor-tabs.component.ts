import { Component, Input } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Component({
    selector: 'credit-decision-editor-tabs',
    templateUrl: './credit-decision-editor-tabs.component.html',
    styles: [],
})
export class CreditDecisionEditorTabsComponent {
    constructor() {}

    @Input()
    public initialData: CreditDecisionEditorTabsComponentInitialData;

    setAcceptRejectMode(isRejectTabActive: boolean, evt?: Event) {
        evt?.preventDefault();

        this.initialData.isRejectTabActive.next(isRejectTabActive);
    }
}

export class CreditDecisionEditorTabsComponentInitialData {
    isCalculating: BehaviorSubject<boolean>;
    isRejectTabActive: BehaviorSubject<boolean>;
    acceptTitle: string;
}
