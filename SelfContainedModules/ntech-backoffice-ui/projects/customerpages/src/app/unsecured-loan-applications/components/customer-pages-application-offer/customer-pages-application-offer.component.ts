import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { ComplexApplicationListRow } from 'src/app/common-services/complex-application-list';
import { StringDictionary } from 'src/app/common.types';
import { CustomerPagesEventService } from '../../../common-services/customerpages-event.service';
import { CustomerPagesApplicationsApiService } from '../../services/customer-pages-applications-api.service';
import { TaskToggleBlockInitialData } from '../task-toggle-block/task-toggle-block.component';
import { CustomerPagesApiService } from '../../../common-services/customer-pages-api.service';

@Component({
    selector: 'customer-pages-application-offer',
    templateUrl: './customer-pages-application-offer.component.html',
    styles: [],
})
export class CustomerPagesApplicationOfferComponent implements OnInit {
    constructor(
        private apiService: CustomerPagesApplicationsApiService,
        private eventService: CustomerPagesEventService,
        private generalApiService: CustomerPagesApiService
    ) {}

    @Input()
    public initialData: CustomerPagesApplicationOfferInitialData;

    public m: Model;

    ngOnInit(): void {}

    async ngOnChanges(_: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let customCosts = (await this.generalApiService.postLocal<{ code: string, text: string}[]>('api/embedded-customerpages/custom-costs', {})).map(x => ({
            code: x.code,
            firstNotificationCode: `firstNotificationCost_${x.code}_Amount`,
            text: x.text
        }));

        let offerItems = ComplexApplicationListRow.fromDictionary('temp', 1, this.initialData.offerItems);
        this.m = {
            applicationNr: this.initialData.applicationNr,
            offerItems: offerItems,
            firstNotificationCosts: offerItems.getUniqueItemNames().filter(x => x.startsWith('firstNotificationCost_') && x.endsWith('_Amount')).map(x => ({
                displayName: customCosts.find(y => y.firstNotificationCode ===  x)?.text ?? x,
                amount: offerItems.getUniqueItemInteger(x)
            })),
            isPossibleToDecide: this.initialData.isPossibleToDecide,
            toggleInitialData: {
                headerText: 'Lånedetaljer',
                isInitiallyExpanded: this.initialData.isPossibleToDecide,
                onExpandedToggled: null,
                toggleBlockId: null,
                isAccepted: offerItems.getUniqueItem('customerDecisionCode') == 'accepted',
                isRejected: offerItems.getUniqueItem('customerDecisionCode') == 'rejected',
            },
            hasRejectedOffer: offerItems.getUniqueItem('customerDecisionCode') == 'rejected',
        };
    }

    currencySymbol() {
        return 'kr';
    }

    acceptOffer(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.applicationNr;
        this.apiService.setCustomerCreditDecisionCode(applicationNr, 'accepted').then((_) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }

    rejectOffer(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.applicationNr;
        this.apiService.setCustomerCreditDecisionCode(applicationNr, 'rejected').then((_) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }
}

class Model {
    applicationNr: string;
    offerItems: ComplexApplicationListRow;
    firstNotificationCosts: { displayName: string, amount: number } []
    isPossibleToDecide: boolean;
    hasRejectedOffer: boolean;
    toggleInitialData: TaskToggleBlockInitialData;
}

export class CustomerPagesApplicationOfferInitialData {
    applicationNr: string;
    offerItems: StringDictionary;
    isPossibleToDecide: boolean;
}
