import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'result-success',
  templateUrl: './result-success.component.html',
  styleUrls: []
})
export class ResultSuccessComponent {
    applicationNr: string

    constructor(private route: ActivatedRoute) {
        route.data.subscribe(x => {
            this.applicationNr = x.applicationNr
        })
    }
}
