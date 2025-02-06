import { Component, Input, OnInit, TemplateRef } from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';

@Component({
    selector: 'application-policy-info-wrapper',
    templateUrl: './application-policy-info-wrapper.component.html',
    styles: [],
})
export class ApplicationPolicyInfoWrapperComponent implements OnInit {
    constructor(private modalService: BsModalService) {}

    public detailsModalRef: BsModalRef;

    @Input()
    public isVisible: boolean;

    ngOnInit(): void {}

    showDetails(detailsModalTemplate: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();
        this.detailsModalRef = this.modalService.show(detailsModalTemplate, {
            class: 'modal-xl',
            ignoreBackdropClick: true,
        });
    }
}
