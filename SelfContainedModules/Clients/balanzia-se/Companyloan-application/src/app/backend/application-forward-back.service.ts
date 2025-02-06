import { Injectable, EventEmitter } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ApplicationForwardBackService {
    isForwardAllowed: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false)
    isBackAllowed: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(true)
    isFinalStep: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false)
    isLoading: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false)

    onForward: EventEmitter<NavigationEvent> = new EventEmitter<NavigationEvent>()
    onBack: EventEmitter<NavigationEvent> = new EventEmitter<NavigationEvent>()

    constructor() { 

    }
}

export class NavigationEvent {
    sourceStepName: string
    isForward: boolean
    isBack: boolean
}
