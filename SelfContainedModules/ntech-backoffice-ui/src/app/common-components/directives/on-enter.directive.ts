import { Directive, EventEmitter, HostListener, OnInit, Output } from '@angular/core';

/*
Usage:
<input onEnter (onEnterClick)="search()" type="text" ...

Note, we would like to use the selector onEnterClick and let that be the output also so just this would be enough:
<input (onEnterClick)="search()" type="text" ...

Could not get that to work though even though is seems like it should be possible.
*/
@Directive({
    selector: '[onEnter]',
})
export class OnEnterDirective implements OnInit {
    constructor() {}

    ngOnInit() {}

    @Output() onEnterClick = new EventEmitter();

    @HostListener('keyup', ['$event'])
    keyUpEvent(event: KeyboardEvent) {
        this.handleEnter(event, () => this.onEnterClick.emit());
    }

    @HostListener('keydown', ['$event'])
    keyDownEvent(event: KeyboardEvent) {
        this.handleEnter(event);
    }

    private handleEnter(event: KeyboardEvent, after?: () => void) {
        if (!event) {
            return;
        }
        if (event.key != 'Enter') {
            return;
        }
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        if (after) {
            after();
        }
    }
}
