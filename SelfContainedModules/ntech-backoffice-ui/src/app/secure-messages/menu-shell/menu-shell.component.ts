import { Component, Input, OnInit, SimpleChanges } from '@angular/core';

@Component({
    selector: 'menu-shell',
    templateUrl: './menu-shell.component.html',
    styleUrls: [],
})
export class MenuShellComponent implements OnInit {
    @Input()
    public initialData: MenuShellInitialData = null;

    public m: Model = null;

    constructor() {}

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        this.m = {
            activeItemName: this.initialData.activeItemName,
        };
    }
}

export class MenuShellInitialData {
    activeItemName: string;
}

class Model {
    activeItemName: string;
}
