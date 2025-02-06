import { Component, Input, SimpleChanges } from '@angular/core';
import { SettingModel, SettingsHelper } from '../../services/settings-model';
import { SettingSingleComponentInitialData } from '../setting-single/setting-single.component';

@Component({
    selector: 'setting-group',
    templateUrl: './setting-group.component.html',
    styles: [],
})
export class SettingGroupComponent {
    constructor() {}

    @Input()
    public initialData: SettingGroupComponentInitialData;

    m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let m: Model = {
            settings: [],
        };

        for (let setting of this.initialData.settings) {
            m.settings.push({
                displayName: setting.DisplayName,
                initialData: {
                    setting: setting,
                    settingsHelper: this.initialData.settingsHelper,
                },
            });
        }

        this.m = m;
    }
}

export class SettingGroupComponentInitialData {
    settingsHelper: SettingsHelper;
    settings: SettingModel[];
}

class Model {
    settings: { displayName: string; initialData: SettingSingleComponentInitialData }[];
}
