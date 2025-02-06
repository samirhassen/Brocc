import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'task-page',
    templateUrl: './task-page.component.html',
    styleUrls: ['./task-page.component.scss'],
})
export class TaskPageComponent {
    constructor() {}

    @Input()
    public headerText: string;

    @Output()
    public onClose: EventEmitter<void> = new EventEmitter();

    @Input()
    public isStandalonePage: boolean;

    public close(evt: Event) {
        evt.preventDefault();
        this.onClose.emit();
    }
}
