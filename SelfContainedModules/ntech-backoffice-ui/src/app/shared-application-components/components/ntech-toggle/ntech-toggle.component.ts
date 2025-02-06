import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'ntech-toggle',
    templateUrl: './ntech-toggle.component.html',
    styles: [],
})
export class NtechToggleComponent {
    constructor() {}

    @Input()
    public isToggled: boolean;

    @Input()
    public isDisabled: boolean;

    @Output()
    public requestToggle: EventEmitter<void> = new EventEmitter<void>();

    public toggle(evt?: Event) {
        evt?.preventDefault();

        if (this.isDisabled) {
            return;
        }

        this.requestToggle.emit();
    }
}
