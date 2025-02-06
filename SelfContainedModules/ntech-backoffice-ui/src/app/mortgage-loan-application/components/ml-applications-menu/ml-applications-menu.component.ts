import { Component, Input, OnInit } from '@angular/core';

@Component({
    selector: 'ml-applications-menu',
    templateUrl: './ml-applications-menu.component.html',
    styles: [],
})
export class MlApplicationsMenuComponent implements OnInit {
    constructor() {}

    @Input()
    public currentTabName: string;

    ngOnInit(): void {}

    getNavPillsClass(tabName: string) {
        return { active: this.currentTabName === tabName };
    }
}
