import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { ViewAndEditComponentInitialData } from '../view-and-edit/view-and-edit.component';

@Component({
    selector: 'app-checkpoints-for-customer',
    templateUrl: './checkpoints-for-customer.component.html',
    styles: [
    ]
})
export class CheckpointsForCustomerComponent implements OnInit {
    constructor(private apiService: NtechApiService, private route: ActivatedRoute) { }
        public viewAndEditData ?: ViewAndEditComponentInitialData

    async ngOnInit() {
        let customerId: number = this.route.snapshot.params['customerId'];
        await this.reload(customerId);
    }

    private async reload(customerId: number) {
        let customerItems = (await this.apiService.shared.fetchCustomerItemsBulk([customerId], ['civicRegNr']))[customerId];
        this.viewAndEditData = {
            customerId: customerId,
            civicRegNr: customerItems ? customerItems['civicRegNr'] : null
        }
    }
}