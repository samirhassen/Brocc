import { Component } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'signature-failed',
  templateUrl: './signature-failed.component.html',
  styleUrls: []
})
export class SignatureFailedComponent {

    constructor() {
           
    }

    ngOnInit() {        

    }

    ngOnDestroy() {
        this.unsub()
    }

    subs: Subscription[] = []

    unsub() {
        if(this.subs) {
            for(let s of this.subs) {
                s.unsubscribe()
            }
            this.subs = []
        }
    }
}
