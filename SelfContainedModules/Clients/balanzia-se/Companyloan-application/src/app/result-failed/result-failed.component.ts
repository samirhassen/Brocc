import { Component, OnInit } from '@angular/core';
import { Router, ActivationEnd } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

@Component({
    selector: 'result-failed',
    templateUrl: './result-failed.component.html',
    styleUrls: []
})
export class ResultFailedComponent {
    failureCode: BehaviorSubject<string> = new BehaviorSubject<string>(null)

    constructor(private router: Router) {
        router.events.subscribe(x => {
            if (x instanceof ActivationEnd) {
                let y: ActivationEnd = x
                this.failureCode.next(y.snapshot.data.failureCode ? y.snapshot.data.failureCode : null)
            }
        })
    }
}
