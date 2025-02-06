import { Component, OnInit } from '@angular/core';
import { SearchCheckpointData } from '../search/search.component';
import { ViewAndEditComponentInitialData } from '../view-and-edit/view-and-edit.component';

@Component({
    selector: 'app-checkpoints-main',
    templateUrl: './checkpoints-main.component.html',
    styles: [
    ]
})
export class CheckpointsMainComponent implements OnInit {
    constructor() { }

    ngOnInit(): void {
    }

    public viewAndEditData ?: ViewAndEditComponentInitialData

    onSearch(evt: SearchCheckpointData) {
        this.viewAndEditData = {
            customerId: evt.customerId,
            civicRegNr: evt.civicRegNr
        }
    }
}
