import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import * as moment from 'moment';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { CustomerCardItemEditorComponentInitialData } from './customer-card-item-editor/customer-card-item-editor.component';

@Component({
    selector: 'app-customer-card',
    templateUrl: './customer-card.component.html',
    styles: [
    ]
})
export class CustomerCardComponent implements OnInit {
    constructor(private route: ActivatedRoute, private apiService: NtechApiService) { }

    public m: Model;

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(parseInt(params.get('customerId')), false);
        });
    }

    private async reload(customerId: number, includeSensitive: boolean) {        
        let result = await this.fetchContactInfo(customerId, includeSensitive, true);

        let items: ItemViewModel[] = []

        let isSensitiveItem = (name: string) => result.sensitiveItems.indexOf(name) >= 0

        let addItem = (name: string, displayLabelText: string, value: string, isEditable: boolean) => {
            let i = ItemViewModel.createEditableItemWithoutValue(name, displayLabelText)
            if (includeSensitive || !isSensitiveItem(name)) {
                i.value = value
                i.hasValue = true
            }
            i.isEditable = isEditable
            items.push(i)
        }

        let addSeparator = () => items.push(ItemViewModel.createSeparator())

        if (result.isCompanyBool) {
            addItem('companyName', 'Company name', result.companyName, true)
            addItem('orgnr', 'Orgnr', result.orgnr, false)
        } else {
            addItem('firstName', 'First name', result.firstName, true)
            addItem('lastName', 'Last name', result.lastName, true)
            addItem('birthDate', 'Birthdate', this.formatDate(result.birthDate), true)
            addItem('civicRegNr', 'Civic reg nr', result.civicRegNr, false)
        }

        addSeparator()
        addItem('addressStreet', 'Street', result.addressStreet, true)
        addItem('addressZipcode', 'Zipcode', result.addressZipcode, true)
        addItem('addressCity', 'City', result.addressCity, true)
        addItem('addressCountry', 'Country', result.addressCountry, true)
        addSeparator()
        addItem('email', 'Email', result.email, true)
        addItem('phone', 'Phone', result.phone, true)        
        
        this.m = {
            customerId: customerId,
            showSensitive: includeSensitive,
            items: items
        }
    }

    private async fetchContactInfo(customerId: number, includeSensitive: boolean, includeCivicRegNr: boolean) {
        //TODO: The wipe test function if there is time
        return this.apiService.post<FetchContactInfoResponse>('nCustomer', 'Api/ContactInfo/Fetch', { customerId, includeSensitive, includeCivicRegNr });
    }

    async showSensitive(evt ?: Event) {
        evt?.preventDefault();
        this.reload(this.m.customerId, true);
    }

    async editItem(item: ItemViewModel, evt ?: Event) {
        evt?.preventDefault();

        this.m.edit = {
            customerId: this.m.customerId,
            onClose: _ => {
                this.reload(this.m.customerId, this.m.showSensitive)
            },
            itemName: item.name,
            hideHeader: true
        };
    }
    
    private formatDate(d: string): string {
        if (d) {
            return moment(d).format('YYYY-MM-DD')
        } else {
            return null
        }
    }
}

interface Model {
    customerId: number
    showSensitive: boolean
    items: ItemViewModel[]
    edit?: CustomerCardItemEditorComponentInitialData
}

interface FetchContactInfoResponse {
    customerId: number
    isCompany: boolean
    firstName: string
    birthDate: string
    email: string
    phone: string
    sensitiveItems: string[]
    includeSensitive: boolean
    includeCivicRegNr: boolean
    isCompanyBool: boolean
    companyName: string
    orgnr: string
    lastName: string
    civicRegNr: string
    addressStreet: string
    addressZipcode: string
    addressCity: string
    addressCountry: string
}

export class ItemViewModel {
    name: string
    displayLabelText: string
    hasValue: boolean
    value: string
    isEditable: boolean
    isSeparator: boolean

    static createEditableItemWithoutValue(name: string, displayLabelText: string): ItemViewModel {
        let i: ItemViewModel = {
            name: name,
            displayLabelText: displayLabelText,
            isEditable: true,
            isSeparator: false,
            hasValue: false,
            value: null
        }
        return i
    }

    static createSeparator(): ItemViewModel {
        let i: ItemViewModel = {
            isSeparator: true,
            name: null, displayLabelText: null, hasValue: null, isEditable: null, value: null
        }
        return i
    }
}
