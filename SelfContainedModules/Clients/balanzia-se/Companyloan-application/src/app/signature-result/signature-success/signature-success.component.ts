import { Component } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'signature-success',
  templateUrl: './signature-success.component.html',
  styleUrls: []
})
export class SignatureSuccessComponent {

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
