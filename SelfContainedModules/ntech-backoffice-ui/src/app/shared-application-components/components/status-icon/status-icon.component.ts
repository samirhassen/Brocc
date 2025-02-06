import { Component, Input } from '@angular/core';

@Component({
    selector: 'status-icon',
    templateUrl: './status-icon.component.html',
    styles: [],
})
export class StatusIconComponent {
    constructor() {}

    @Input()
    public isAccepted: boolean;

    @Input()
    public isRejected: boolean;

    getIconClass() {
        var isOther = !this.isAccepted && !this.isRejected;
        return {
            'glyphicon-ok': this.isAccepted,
            'glyphicon-remove': this.isRejected,
            'glyphicon-minus': isOther,
            'glyphicon': true,
            'text-success': this.isAccepted,
            'text-danger': this.isRejected,
        };
    }
}
