import { Component, Input, OnInit, ViewEncapsulation } from '@angular/core';

@Component({
    selector: 'html-preview',
    encapsulation: ViewEncapsulation.ShadowDom, //Prevent css rules from external html leaking out into the rest of the application
    template: `<div class="iframe-container">
        <div [innerHTML]="content | safeHtml"></div>
    </div>`,
    styles: [],
})
export class HtmlPreviewComponent implements OnInit {
    constructor() {}

    @Input()
    content: string;

    ngOnInit(): void {}
}
