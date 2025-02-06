import { Component, OnInit } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary, groupByString } from 'src/app/common.types';
import { SettingGroupComponentInitialData } from '../../components/setting-group/setting-group.component';
import { SettingSingleComponentInitialData } from '../../components/setting-single/setting-single.component';
import { SettingsHelper } from '../../services/settings-model';

const LastSettingStorageKey = 'LastSetting';

function sortSettingsModels<T>(settings: T[], getDisplayName: (item: T) => string): T[] {
    return settings.sort((x, y) => getDisplayName(x).localeCompare(getDisplayName(y)));
}

@Component({
    selector: 'app-settings-list',
    templateUrl: './settings-list.component.html',
    styles: [],
})
export class SettingsListComponent implements OnInit {
    constructor(private config: ConfigService, private apiService: NtechApiService) {}

    public m: Model;

    ngOnInit(): void {
        let m: Model = {
            settings: [],
        };

        this.apiService.post('nBackOffice', 'api/embedded-backoffice/settings', {}).then((x: any) => {
            let helper = new SettingsHelper(x.Model);

            let enabledSettings = helper.model.Settings.filter((x) => helper.isSettingEnabled(x, this.config));
            
            let groupedSettings = groupByString(
                enabledSettings.filter((x) => x.UiGroupName),
                (x) => x.UiGroupName
            );

            //We do it this way to ensure that groups are added in the position of the first setting that is part of the group
            //to make the behaviour a bit predictable.
            let addedGroupNames: Dictionary<string> = {}; //Poor mans Set<string>
            for (let setting of enabledSettings) {
                if (setting.UiGroupName && !addedGroupNames[setting.UiGroupName]) {
                    m.settings.push({
                        code: setting.UiGroupName,
                        isGroup: true,
                        displayName: helper.getUiGroupDisplayName(setting.UiGroupName),
                        groupInitialData: {
                            settingsHelper: helper,
                            settings: groupedSettings[setting.UiGroupName],
                        },
                    });
                    addedGroupNames[setting.UiGroupName] = 'added';
                } else if (!setting.UiGroupName) {
                    m.settings.push({
                        code: setting.Code,
                        displayName: setting.DisplayName,
                        isGroup: false,
                        singleInitialData: {
                            setting: setting,
                            settingsHelper: helper,
                        },
                    });
                }
            }

            let lastSettingCode = localStorage.getItem(LastSettingStorageKey);
            if (lastSettingCode) {
                //Makes back navigation and testing smoother
                let lastSetting = m.settings.find((x) => x.code == lastSettingCode);
                if (lastSetting) {
                    m.activeSetting = lastSetting;
                }
            }

            m.settings = sortSettingsModels(m.settings, (x) => x.displayName);
            for (let groupSetting of m.settings.filter((x) => x.groupInitialData)) {
                groupSetting.groupInitialData.settings = sortSettingsModels(
                    groupSetting.groupInitialData.settings,
                    (x) => x.DisplayName
                );
            }

            this.m = m;
        });
    }

    pickSetting(s: SettingUiModel, evt?: Event) {
        evt?.preventDefault();

        this.m.activeSetting = s;
        localStorage.setItem(LastSettingStorageKey, s.code);
    }
}

class Model {
    settings: SettingUiModel[];
    activeSetting?: SettingUiModel;
}

class SettingUiModel {
    code: string;
    isGroup: boolean;
    displayName: string;
    singleInitialData?: SettingSingleComponentInitialData;
    groupInitialData?: SettingGroupComponentInitialData;
}
