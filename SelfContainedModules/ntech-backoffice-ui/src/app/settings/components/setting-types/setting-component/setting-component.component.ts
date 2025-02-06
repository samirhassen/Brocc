import { Component, Input, SimpleChanges } from '@angular/core';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { SettingModel, SettingTypeCode } from '../../../services/settings-model';

@Component({
    selector: 'setting-component',
    templateUrl: './setting-component.component.html',
    styles: [],
})
export class SettingComponentComponent {
    constructor() {}

    @Input()
    public initialData: SettingComponentInitialData;

    public m: Model;

    ngOnChanges(_: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        const setting = this.initialData.componentSetting;
        if (setting.Type !== SettingTypeCode.Component) {
            return;
        }

        this.m = {
            displayName: setting.DisplayName,
            componentName: setting.ComponentData.ComponentName,
        };
    }
}

export class SettingComponentInitialData {
    componentSetting: SettingModel;
    backTarget?: CrossModuleNavigationTarget;
}

class Model {
    displayName: string;
    componentName: string;
}
