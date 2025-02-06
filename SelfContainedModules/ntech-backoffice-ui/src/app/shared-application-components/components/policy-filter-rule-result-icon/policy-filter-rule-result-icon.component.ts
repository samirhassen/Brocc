import { Component, Input } from '@angular/core';

@Component({
    selector: 'policy-filter-rule-result-icon',
    templateUrl: './policy-filter-rule-result-icon.component.html',
    styles: [],
})
export class PolicyFilterRuleResultIconComponent {
    @Input()
    public policy: {
        statusCode: string;
        missingMessage?: string;
    };
    constructor() {}
}
