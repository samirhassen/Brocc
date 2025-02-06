import { Component, Input, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { DateOnly, dateOnlyToIsoDate } from 'src/app/common.types';
import { CustomerComponentInitialDataResult, CustomerInfoService } from './customer-info.service';

@Component({
    selector: 'customer-info',
    templateUrl: './customer-info.component.html',
    styles: [],
})
export class CustomerInfoComponent {
    @Input()
    public initialData?: CustomerInfoInitialData;
    public model?: Model;

    constructor(
        private apiService: NtechApiService,
        private toastr: ToastrService,
        private customerService: CustomerInfoService,
        private router: Router
    ) {}

    async ngOnChanges(changes: SimpleChanges) {
        this.model = null;

        if (this.initialData == null) {
            return;
        }

        let c = 0;
        if (this.initialData.applicantNr) c += 1;
        if (this.initialData.customerIdCompoundItemName) c += 1;
        if (this.initialData.customerId) c += 1;
        if (c !== 1) {
            throw new Error('Exactly one of applicantNr, customerIdCompoundItemName and customerId must be set');
        }
        let init = (result: CustomerComponentInitialDataResult) => {
            this.model = {
                customer: result,
                hideDetailsLink: this.initialData.hideDetailsLink,
                showKycBlock: this.initialData.showKycBlock,
            };
        };

        if (this.initialData.preLoadedCustomer) {
            init(this.initialData.preLoadedCustomer);
        } else if (this.initialData.applicantNr) {
            let result = await this.customerService.fetchCustomerComponentInitialData(
                this.initialData.applicationNr,
                this.initialData.applicantNr,
                this.initialData.backTarget?.getCode()
            );
            init(result);
        } else if (this.initialData.customerIdCompoundItemName) {
            let result = await this.customerService.fetchCustomerComponentInitialDataByItemCompoundName(
                this.initialData.applicationNr,
                this.initialData.customerIdCompoundItemName,
                this.initialData.birthDateCompoundItemName,
                this.initialData.backTarget?.getCode()
            );
            init(result);
        } else {
            let result = await this.customerService.fetchCustomerComponentInitialDataByCustomerId(
                this.initialData.customerId,
                this.initialData.backTarget?.getCode()
            );
            init(result);
        }
    }

    async toggleContactInfo(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        if (!this.model?.contactInfo) {
            let itemNames = ['addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email'];
            if (this.isCompany()) {
                itemNames.push('companyName');
            } else {
                itemNames.push('firstName');
                itemNames.push('lastName');
            }

            let items = await this.customerService.fetchCustomerItems(this.model.customer.customerId, itemNames);
            this.model.contactInfo = {
                isOpen: true,
                firstName: items['firstName'],
                lastName: items['lastName'],
                addressStreet: items['addressStreet'],
                addressCity: items['addressCity'],
                addressZipcode: items['addressZipcode'],
                addressCountry: items['addressCountry'],
                phone: items['phone'],
                email: items['email'],
                companyName: items['companyName'],
            };
        } else {
            this.model.contactInfo.isOpen = !this.model.contactInfo.isOpen;
        }
    }

    async togglePepKycInfo(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        if (!this.model.pepKycInfo) {
            let result = await this.customerService.fetchCustomerKycScreenStatus(this.model.customer.customerId);
            this.model.pepKycInfo = {
                latestScreeningDate: result.LatestScreeningDate,
                isOpen: true,
            };
        } else {
            this.model.pepKycInfo.isOpen = !this.model.pepKycInfo.isOpen;
        }
    }

    async doKycScreen(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        if (!this.model?.pepKycInfo) {
            return;
        }
        let x = await this.customerService.kycScreenCustomer(this.model.customer.customerId, false);
        if (!x.Success) {
            this.toastr.warning('Screening failed: ' + x.FailureCode);
        } else if (x.Skipped) {
            this.toastr.info('Customer has already been screened');
        } else {
            if (this.initialData.onkycscreendone != null) {
                if (!this.initialData.onkycscreendone(this.model.customer.customerId)) {
                    return;
                }
            }
            this.model.pepKycInfo = null;
            this.togglePepKycInfo(null);
        }
    }

    formatmissing(i: any) {
        if (!i) {
            return '-';
        } else {
            return i;
        }
    }

    formatPhoneNr(nr: string) {
        //TODO: libphonne + country from ConfigService
        return nr;
    }

    async unlockCivicRegNr(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        let nrName = this.isCompany() ? 'orgnr' : 'civicRegNr';
        let result = await this.customerService.fetchCustomerItems(this.model.customer.customerId, [nrName]);
        this.model.civicRegNr = result[nrName];
    }

    async unlockAmlRiskClass(evt?: Event) {
        evt?.preventDefault();
        let result = await this.customerService.fetchCustomerItems(this.model.customer.customerId, ['amlRiskClass']);
        this.model.amlRiskClass = result['amlRiskClass'] ?? '-';
    }

    isCompany() {
        if (!this.model?.customer) {
            return null;
        }
        return this.model.customer.isCompany;
    }

    getPepSanctionUrl() {
        if (!this.model?.customer) {
            return null;
        }

        return this.apiService.getUiGatewayUrl('nCustomer', 'Ui/KycManagement/Manage', [
            ['customerId', this.model.customer.customerId.toString()],
            ['backTarget', this.initialData?.backTarget?.getCode()],
        ]);
    }

    getFatcaCrsUrl() {
        if (!this.model?.customer) {
            return null;
        }

        return this.apiService.getUiGatewayUrl('nCustomer', 'Ui/KycManagement/FatcaCrs', [
            ['customerId', this.model.customer.customerId.toString()],
            ['backTarget', this.initialData?.backTarget?.getCode()],
        ]);
    }

    navigateToKycQuestions(evt?: Event): void {
        evt?.preventDefault();

        if (!this.model?.customer) {
            return null;
        }

        this.router.navigate([`/customer-kyc/questions/${this.model.customer.customerId}`], {
            queryParams: {
                backTarget: this.initialData?.backTarget?.getCode(),
            },
        });
    }

    toDate(d: DateOnly) {
        return d ? dateOnlyToIsoDate(d) : '';
    }
}

class Model {
    hideDetailsLink?: boolean;
    showKycBlock: boolean;
    public customer?: CustomerComponentInitialDataResult;
    public civicRegNr?: string;
    public contactInfo?: ContactInfo;
    public pepKycInfo?: PepKycInfo;
    public amlRiskClass?: string;
}

export class CustomerInfoInitialData {
    applicationNr?: string;
    backTarget?: CrossModuleNavigationTarget;
    showKycBlock?: boolean;
    onkycscreendone?: (customerId: number) => boolean;
    applicantNr?: number;
    customerIdCompoundItemName?: string; //Examples: applicant1.customerId or application.companyCustomerId
    birthDateCompoundItemName?: string;
    customerId?: number;
    hideDetailsLink?: boolean;
    linksOnTheLeft?: boolean;
    showPepSanctionLink?: boolean;
    showFatcaCrsLink?: boolean;
    showKycQuestionsLink?: boolean;
    showAmlRisk?: boolean;
    preLoadedCustomer?: CustomerComponentInitialDataResult;
}

class ContactInfo {
    public isOpen: boolean;
    public firstName: string;
    public lastName: string;
    public addressStreet: string;
    public addressZipcode: string;
    public addressCity: string;
    public addressCountry: string;
    public phone: string;
    public email: string;
    public companyName: string;
}

class PepKycInfo {
    public isOpen: boolean;
    latestScreeningDate: DateOnly;
}
