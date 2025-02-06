import { Component } from '@angular/core';
import { ApplicationForwardBackService } from '../backend/application-forward-back.service';

@Component({
    selector: 'forward-back-buttons',
    templateUrl: './forward-back-buttons.component.html',
    styleUrls: ['./forward-back-buttons.component.css']
})
export class ForwardBackButtonsComponent {
    constructor(public forwardBackService: ApplicationForwardBackService) { 
   
    }

    forward(evt: Event) {
        if(evt) {
            evt.preventDefault()
        }
        if(!this.forwardBackService.isForwardAllowed.value) {
            return
        }
        this.forwardBackService.onForward.emit()
    } 
}
