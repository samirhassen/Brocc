import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { NtechEventService, UpdateToggleBlock } from 'src/app/common-services/ntech-event.service';

@Component({
    selector: 'toggle-block',
    templateUrl: './toggle-block.component.html',
    styleUrls: [ './toggle-block.component.scss' ]
})
export class ToggleBlockComponent implements OnInit {
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

    @Input() public initialData: ToggleBlockInitialData;

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
            useTransparentBackground: this.initialData.useTransparentBackground,
            useFixedBorder: this.initialData.useFixedBorder,
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
    useFixedBorder: boolean;
    useTransparentBackground: boolean;
}

export class ToggleBlockInitialData {
    headerText: string;
    isInitiallyExpanded?: boolean;
    useTransparentBackground?: boolean;
    useFixedBorder?: boolean;
    onExpandedToggled?: (isExpanded: boolean) => void;
    toggleBlockId?: string;
}
