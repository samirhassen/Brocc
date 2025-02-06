import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { NtechEventService, UpdateToggleBlock } from 'src/app/common-services/ntech-event.service';

@Component({
    selector: 'task-toggle-block',
    templateUrl: './task-toggle-block.component.html',
    styleUrls: ['./task-toggle-block.scss'],
})
export class TaskToggleBlockComponent implements OnInit {
    constructor(eventService: NtechEventService) {
        eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === UpdateToggleBlock && this.initialData?.toggleBlockId) {
                let eventData = x.customData as { toggleBlockId: string; newTitle: string };
                if (eventData?.toggleBlockId === this.initialData.toggleBlockId) {
                    this.m.headerText = eventData.newTitle;
                }
            }
        });
    }

    @Input() public initialData: TaskToggleBlockInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        this.m = {
            isExpanded: this.initialData.isInitiallyExpanded,
            headerText: this.initialData.headerText,
            status:
                this.initialData.isAccepted === true
                    ? 'accepted'
                    : this.initialData.isRejected === true
                    ? 'rejected'
                    : 'initial',
        };
    }

    toggleExpanded(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        this.m.isExpanded = !this.m.isExpanded;
        if (this.initialData && this.initialData.onExpandedToggled) {
            this.initialData.onExpandedToggled(this.m.isExpanded);
        }
    }
}

class Model {
    headerText: string;
    isExpanded: boolean;
    status: string;
}

export class TaskToggleBlockInitialData {
    headerText: string;
    isInitiallyExpanded?: boolean;
    onExpandedToggled?: (isExpanded: boolean) => void;
    toggleBlockId?: string;
    isRejected?: boolean;
    isAccepted?: boolean;
}
