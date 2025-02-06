import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { distinct } from 'src/app/common.types';
import { CreditDocumentModel, CreditService } from '../credit.service';

@Component({
    selector: 'app-documents-page',
    templateUrl: './documents-page.component.html',
    styles: [],
})
export class DocumentsPageComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private configService: ConfigService,
        private apiService: NtechApiService
    ) {}

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('creditNr'));
        });
    }

    public m: Model;

    private async reload(creditNr: string) {
        this.m = null;

        if (!creditNr) {
            let title = 'Credit documents';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }
        this.eventService.setCustomPageTitle(`Credit ${creditNr}`, `Credit documents ${creditNr}`);

        let documents = await this.creditService.fetchCreditDocuments(creditNr, true, true);
        let customerIdsToLoad = distinct(documents.filter((x) => !!x.CustomerId).map((x) => x.CustomerId));
        let customersData = await this.apiService.shared.fetchCustomerItemsBulk(customerIdsToLoad, [
            'isCompany',
            'companyName',
            'firstName',
        ]);

        let isCompanyLoansEnabled = this.configService.isFeatureEnabled('ntech.feature.companyloans');

        let localDocuments: LocalCreditDocumentModel[] = [];
        for (let d of documents) {
            let customerDisplayName: string = null;
            if (d.CustomerId && customersData[d.CustomerId]) {
                let customerData = customersData[d.CustomerId];
                let isCompany = customerData['isCompany'] === 'true';
                customerDisplayName = isCompany ? customerData['companyName'] : customerData['firstName'];
            }
            localDocuments.push(
                new LocalCreditDocumentModel(
                    isCompanyLoansEnabled,
                    d,
                    customerDisplayName,
                    this.apiService.getArchiveDocumentUrl(d.ArchiveKey)
                )
            );
        }

        this.m = {
            creditNr: creditNr,
            documents: localDocuments,
        };
    }
}

interface Model {
    creditNr: string;
    documents: LocalCreditDocumentModel[];
}

class LocalCreditDocumentModel {
    constructor(
        private isCompanyLoansEnabled: boolean,
        public document: CreditDocumentModel,
        public customerDisplayName: string,
        public downloadUrl: string
    ) {}

    getDisplayName() {
        let targetText = this.getDocumentTargetText(this.document);
        let documentTypeDisplayName = this.getDocumentTypeDisplayName(this.document);
        return targetText ? `${documentTypeDisplayName} ${targetText}` : documentTypeDisplayName;
    }

    private getDocumentTargetText(d: CreditDocumentModel) {
        if (d.ApplicantNr) {
            if (this.isCompanyLoansEnabled) {
                //Applicant 1 is the company here and there are never more applicants so the concept is not so useful for company loans
                return `for ${this.customerDisplayName}`;
            } else {
                return `for applicant ${d.ApplicantNr}`;
            }
        } else if (d.CustomerId) {
            return `for ${this.customerDisplayName}`;
        }

        return '';
    }

    private getDocumentTypeDisplayName(d: CreditDocumentModel) {
        if (d.DocumentType === 'InitialAgreement') {
            return 'Signed agreement';
        } else if (d.DocumentType === 'MortgageLoanDenuntiation') {
            return 'Denuntiation';
        } else if (d.DocumentType === 'ProofOfIdentity') {
            return 'Proof of identity';
        } else if (d.DocumentType === 'MortgageLoanCustomerAmortizationPlan') {
            return 'Amortization basis';
        } else if (d.DocumentType === 'MortgageLoanLagenhetsutdrag') {
            return 'L\u00e4genhetsutdrag';
        } else if (d.DocumentType === 'ProxyAuthorization') {
            return 'Fullmakt';
        } else if (d.DocumentType === 'ApplicationFreeform') {
            return `Application document '${d.FileName}'`;
        } else if (d.DocumentType === 'DirectDebitConsent') {
            return 'Medgivande om autogiro';
        } else if (d.DocumentType === 'Extra_AnnualStatement') {
            return '\u00c5rsbesked - ' + d.ExtraDocumentData;
        } else {
            return d.DocumentType;
        }
    }
}
