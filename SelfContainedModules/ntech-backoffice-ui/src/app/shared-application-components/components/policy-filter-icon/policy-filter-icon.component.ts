import { Component, Input } from '@angular/core';

@Component({
    selector: 'policy-filter-icon',
    templateUrl: './policy-filter-icon.component.html',
    styles: [],
})
export class PolicyFilterIconComponent {
    constructor() {}

    @Input()
    public statusCode: string;
}
