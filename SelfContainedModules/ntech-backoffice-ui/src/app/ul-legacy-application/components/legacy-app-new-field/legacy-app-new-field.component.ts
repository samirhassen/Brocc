import { Component, Input, OnInit } from '@angular/core';
import { LegacyApplicationResponse } from '../../pages/legacy-application-basis-page/legacy-application-basis-page.component';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'legacy-app-new-field',
    templateUrl: './legacy-app-new-field.component.html',
    styles: [ ]
})
export class LegacyAppNewFieldComponent implements OnInit {    
    constructor(private apiService: NtechApiService, private configService: ConfigService) { }

    @Input()
    public extraClasses: string;

    @Input()
    public labelText: string;

    @Input()
    public group: string

    @Input()
    public name: string

    @Input()
    public type: string    

    @Input()
    public data: LegacyApplicationResponse;

    ngOnInit(): void {
    }

    mainClass() {
        return ('form-group ' + (this.extraClasses ?? '')).trim();
    }

    currentValue() {
        let a = this.data.application.groupedItems as any;
        return a[this.group][this.name];
    }

    changeUrl(isInsert: boolean) {
        return this.apiService.getUiGatewayUrl('nPreCredit', 'CreditApplicationEdit/EditValue',[
            ['mode', isInsert ? 'insert' : 'edit'],
            ['applicationNr', this.data.applicationNr],
            ['groupName', this.group],
            ['name', this.name],
            ['backTarget', 'UllApplicationBasis'],
        ])
    }

    isValueMissing() {
        return this.currentValue() === undefined;
    }

    isValueChanged() {
        let changedItems = this.data.changedCreditApplicationItems ?? [];
        return !!changedItems.find(x => x.groupName === this.group && x.itemName === this.name);
    }

    translate(name: string) {
        let lang = this.configService.userLanguage();
        let trs = this.data.translations[lang] ?? this.data.translations['en']
        return trs[name] ?? name;
    }

    isEditAllowed() {
        return this.data.isEditAllowed;
    }
}
