import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Component({
    selector: 'edit-form',
    templateUrl: './edit-form.component.html',
    styles: [],
})
export class EditFormComponent implements OnInit {
    constructor() {}

    @Input()
    public initialData: EditFormInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let i = this.initialData;

        this.m = {
            isEditAllowed: i.isEditAllowed,
            inEditMode: i.inEditMode,
            isEditing: i.sharedIsEditing ?? new BehaviorSubject<boolean>(false),
        };
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();

        let m = this.m;

        if (this.initialData.onBeginEdit) {
            this.initialData.onBeginEdit();
        }

        m.inEditMode.next(true);
        m.isEditing.next(true);
    }

    onCancel(evt?: Event) {
        evt?.preventDefault();

        let m = this.m;

        if (this.initialData.onCancel) {
            this.initialData.onCancel();
        }

        m.inEditMode.next(false);
        m.isEditing.next(false);
    }

    onSave(evt?: Event) {
        evt?.preventDefault();

        let m = this.m;
        this.initialData.onSave().then((x) => {
            if (x?.removeEditModeAfter) {
                m.inEditMode.next(false);
                m.isEditing.next(false);
            }
        });
    }

    isInvalid() {
        return this.initialData && this.initialData.isInvalid();
    }
}

class Model {
    inEditMode: BehaviorSubject<boolean>;
    isEditAllowed: boolean;
    isEditing: BehaviorSubject<boolean>;
}

export class EditFormInitialData {
    isEditAllowed: boolean;
    onBeginEdit?: () => void;
    onSave: () => Promise<{ removeEditModeAfter?: boolean }>;
    onCancel?: () => void;
    sharedIsEditing?: BehaviorSubject<boolean>; //Used to synchronize editing across multiple components
    inEditMode: BehaviorSubject<boolean>; //If this specific component is in edit mode
    isInvalid: () => boolean;
}
