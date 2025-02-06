import { Component, Input, OnInit, TemplateRef } from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { StandardApplicationModelBase } from 'src/app/shared-application-components/services/standard-application-base';

@Component({
    selector: 'customer-link-button',
    templateUrl: './customer-link-button.component.html',
    styles: [],
})
export class CustomerLinkButtonComponent implements OnInit {
    constructor(private modalService: BsModalService) {}

    public customerLinkDialogRef?: BsModalRef;

    @Input()
    public application: StandardApplicationModelBase;

    ngOnInit(): void {}

    openCustomerLinkDialog(dialogRef: TemplateRef<any>, ev?: Event) {
        this.customerLinkDialogRef = this.modalService.show(dialogRef, { ignoreBackdropClick: true });
    }

    onCustomerLinkTextFocused(customerLinkTextElement: HTMLTextAreaElement) {
        customerLinkTextElement?.select();
    }
}
