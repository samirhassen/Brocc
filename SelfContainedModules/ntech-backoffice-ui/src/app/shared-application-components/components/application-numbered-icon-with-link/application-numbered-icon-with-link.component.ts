import { Component, Input } from '@angular/core';

@Component({
    selector: 'application-numbered-icon-with-link',
    templateUrl: './application-numbered-icon-with-link.component.html',
    styles: [
        `
            .the-icon {
                display: initial;
                background-color: #e0ebfc;
                color: #1d69c4;
                border-radius: 12px;
                position: relative;
                padding: 3px 5px 2px 5px;
                margin-right: 20px;
            }

            .the-icon .the-icon-number {
                position: absolute;
                background-color: #ffccd9;
                padding: 2px 4px 1px 4px;
                border-radius: 7px;
                font-size: 12px;
                line-height: 12px;
                color: #d4496a;
                font-weight: bold;
                top: -3px;
                right: -7px;
            }
        `,
    ],
})
export class ApplicationNumberedIconWithLinkComponent {
    constructor() {}

    @Input()
    public iconClass: string;

    @Input()
    public iconNumber: number;

    @Input()
    public linkText: string;

    @Input()
    public linkRoute: string[];

    @Input()
    public onNavigate: () => void;

    public navigate(evt?: Event) {
        evt?.preventDefault();

        this?.onNavigate();
    }
}
