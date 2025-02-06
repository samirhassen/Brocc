import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'np-application-created',
    templateUrl: './application-created.component.html',
    styles: [],
})
export class ApplicationCreatedComponent implements OnInit {
    constructor(private route: ActivatedRoute) {}

    public m: Model;

    ngOnInit(): void {
        this.m = {
            applicationNr: this.route.snapshot.params['applicationNr'],
        };
    }
}

class Model {
    applicationNr: string;
}
