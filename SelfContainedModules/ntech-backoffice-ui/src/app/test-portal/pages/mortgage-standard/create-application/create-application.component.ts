import { Component, OnInit, ViewChild } from '@angular/core';
import * as moment from 'moment';
import { JsonEditorComponent, JsonEditorOptions } from 'ang-jsoneditor';
import { StringDictionary } from 'src/app/common.types';
import {
    ApplicantRequestModel,
    TestPortalApiServiceService,
} from 'src/app/test-portal/services/TestPortalApiService.service';
import { Randomizer } from 'src/app/common-services/randomizer';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { ToastrService } from 'ngx-toastr';

@Component({
    selector: 'app-create-application',
    templateUrl: './create-application.component.html',
    styles: [],
})
export class CreateMortgageLoanApplicationComponent implements OnInit {
    constructor(
        private apiService: TestPortalApiServiceService,
        private validationService: NTechValidationService,
        private toastr: ToastrService
    ) {}

    private ApplicationHistoryLocalStorageKeyName: string = 'NTech-MortgageLoanApplicationsHistoryV1';

    public model: Model;

    public jsonEditorOptions: JsonEditorOptions;
    @ViewChild(JsonEditorComponent, { static: false }) editor: JsonEditorComponent;

    ngOnInit(): void {
        this.reset();
    }

    reset() {
        this.model = {
            historyItems: this.getHistoryItems(),
            formState: 'Initial',
            mainApplicantMode: 'New',
            coApplicantMode: 'None',
        } as Model;
    }

    addToHistory(item: ApplicationHistoryItem) {
        let items = this.getHistoryItems();
        items.push(item);
        localStorage.setItem(this.ApplicationHistoryLocalStorageKeyName, JSON.stringify(items));
        this.model.historyItems = items;
    }

    getHistoryItems(): ApplicationHistoryItem[] {
        let itemsJson = localStorage.getItem(this.ApplicationHistoryLocalStorageKeyName);
        if (!itemsJson) {
            return [];
        }

        let items = JSON.parse(itemsJson) as ApplicationHistoryItem[];
        return items.sort((a: ApplicationHistoryItem, b: ApplicationHistoryItem) => {
            return <any>new Date(b.createdDate) - <any>new Date(a.createdDate);
        });
    }

    // Needs to be created once per editor since it cannot be shared.
    getJsonEditorOptions() {
        let options = new JsonEditorOptions();
        options.mode = 'tree';
        return options;
    }

    goToApplicants() {
        let validExisting = (mode: string, civicRegNr: string): boolean => {
            if (mode === 'Existing' && !this.validationService.isValidCivicNr(civicRegNr)) {
                return false;
            }
            return true;
        };
        // Mode existing must have a valid civicregnr, for both main and co.
        if (
            !validExisting(this.model.mainApplicantMode, this.model.mainApplicantCivicRegNr) ||
            !validExisting(this.model.coApplicantMode, this.model.coApplicantCivicRegNr)
        ) {
            this.toastr.error('Invalid civicregnr');
            return;
        }

        this.apiService.getOrGenerateTestPerson(this.model.mainApplicantCivicRegNr, false).then((mainRes) => {
            this.model.mainApplicantCivicRegNr = mainRes.CivicRegNr;
            this.model.mainApplicantData = mainRes.Properties;

            // Get or create coapplicant
            if (this.model.coApplicantMode !== 'None') {
                this.apiService.getOrGenerateTestPerson(this.model.coApplicantCivicRegNr, false).then((coRes) => {
                    this.model.coApplicantCivicRegNr = coRes.CivicRegNr;
                    this.model.coApplicantData = coRes.Properties;
                });
            }
        });

        this.model.formState = 'Applicants';
    }

    goToApplication() {
        this.model.formState = 'Application';
        let applicants: ApplicantRequestModel[] = [new ApplicantRequestModel(this.model.mainApplicantData, false)];
        if (this.model.coApplicantData) {
            applicants.push(new ApplicantRequestModel(this.model.coApplicantData, false));
        }

        let requestedAmount = Randomizer.anyEvenNumberBetween(30000, 150000, 1000);

        this.model.application = {
            objectTypeCode: 'seBrf',
            seBrfApartmentNr: 'LGH 1212',
            objectAddressStreet: 'Objektgatan 93',
            objectAddressZipcode: '939393',
            objectAddressCity: 'Objektstaden',
            objectAddressMunicipality: 'Objektkommunen',
            objectAddressCounty: 'SE',
            objectMonthlyFeeAmount: 1500,
            objectLivingArea: 120,
            objectOtherMonthlyCostsAmount: 300,
            incomingChildSupportAmount: 500,
            outgoingChildSupportAmount: 200,
            childBenefitAmount: 1600,
            householdChildren: [
                { ageInYears: 10, sharedCustody: true },
                { ageInYears: 15, sharedCustody: false },
            ],
            loansToSettle: [
                { currentDebtAmount: 15000, loanType: 'student', monthlyCostAmount: 500 },
                { currentDebtAmount: 150000, loanType: 'mortgage' },
            ],
            requestedAmount: requestedAmount,
            requestedRepaymentTimeInMonths: Randomizer.anyOf([12, 24, 36, 48, 60, 72, 84, 96, 108, 120]),
            applicants: applicants,
            meta: { ProviderName: 'self', CustomerExternalIpAddress: '127.0.0.1' },
        };

        this.setApplicationPurchaseOrChangeExistingLoan();
    }

    setApplicationPurchaseOrChangeExistingLoan() {
        let type = Randomizer.anyOf([1, 2, 3]);
        if (type === 1) {
            let objectPriceAmount = Randomizer.anyNumberBetween(5 * 100000, 50 * 100000);
            let ownSavingsAmountPercentBasis = Randomizer.anyOf([3, 8, 12, 14, 19, 23, 34]);

            this.model.application.purchase = {
                objectPriceAmount: objectPriceAmount,
                //Varying percentage of ObjectPriceAmount
                ownSavingsAmount: Math.round((ownSavingsAmountPercentBasis / 100) * objectPriceAmount),
            };
        } else {
            let objectValueAmount = Randomizer.anyNumberBetween(10 * 100000, 50 * 100000);
            //30 - 50% ltv
            let currentDebtAmount = Randomizer.anyNumberBetween(
                (30 / 100) * objectValueAmount,
                (50 / 100) * objectValueAmount
            );

            let ltv70DebtAmount = Math.floor(0.7 * objectValueAmount);
            let paidToCustomerAmount =
                type === 3
                    ? ltv70DebtAmount - currentDebtAmount //Loan up to 70% ltv
                    : Randomizer.anyOf([1, 2, 3]) === 1
                    ? 0
                    : Randomizer.anyNumberBetween(0, ltv70DebtAmount); // 1/3 has none, the rest has some

            this.model.application.changeExistingLoan = {
                objectValueAmount: objectValueAmount,
                paidToCustomerAmount: paidToCustomerAmount,
                mortgageLoansToSettle: [
                    {
                        currentDebtAmount: currentDebtAmount,
                        shouldBeSettled: type === 2,
                        bankName: 'Banken AB',
                        currentMonthlyAmortizationAmount: Math.round(Math.max(currentDebtAmount / 50, 500)),
                        interestRatePercent: 1.07,
                        loanNumber: 'BK 5567-1',
                    },
                ],
            };
        }
    }

    clearHistoryItems() {
        localStorage.removeItem(this.ApplicationHistoryLocalStorageKeyName);
        this.model.historyItems = [];
    }

    createApplication() {
        this.apiService.createMortgageLoanStandardApplication(this.model.application).then((x) => {
            let item = {
                applicationNr: x.ApplicationNr,
                applicants: this.model.application.applicants.map((x) => x.CivicRegNr),
                createdDate: moment().toDate(),
            } as ApplicationHistoryItem;
            this.addToHistory(item);
            this.reset();
        });
    }
}

export class Model {
    public formState: string;
    public historyItems: ApplicationHistoryItem[];
    public mainApplicantCivicRegNr: string;
    public mainApplicantData: StringDictionary;
    public coApplicantCivicRegNr: string;
    public coApplicantData: StringDictionary;
    public mainApplicantMode: string;
    public coApplicantMode: string;
    public application: CreateMortgageLoanApplicationRequest;
}

export class ApplicationHistoryItem {
    applicationNr: string;
    createdDate: Date;
    applicants: string[];
}

export class CreateMortgageLoanApplicationRequest {
    public objectTypeCode?: string;
    public seBrfApartmentNr?: string;
    public objectAddressStreet?: string;
    public objectAddressZipcode?: string;
    public objectAddressCity?: string;
    public objectAddressMunicipality?: string;
    public objectAddressCounty?: string;
    public objectMonthlyFeeAmount?: number;
    public objectLivingArea?: number;
    public objectOtherMonthlyCostsAmount?: number;
    public outgoingChildSupportAmount?: number;
    public incomingChildSupportAmount?: number;
    public childBenefitAmount?: number;
    public applicants: ApplicantRequestModel[];
    public nrOfHouseholdChildren?: number;
    public householdChildren?: ChildModel[];
    public loansToSettle?: LoanToSettleModel[];
    public requestedAmount?: number;
    public requestedRepaymentTimeInMonths?: number;
    public purchase?: PurchaseModel;
    public changeExistingLoan?: ChangeExistingLoanModel;
    public meta: { ProviderName: string; CustomerExternalIpAddress: string };
}

export class LoanToSettleModel {
    public currentDebtAmount?: number;
    public loanType?: string;
    public monthlyCostAmount?: number;
}

export class ChildModel {
    public exists?: boolean;
    public ageInYears?: number;
    public sharedCustody?: boolean;
}

export class PurchaseModel {
    public objectPriceAmount?: number;
    public ownSavingsAmount?: number;
}

export class ChangeExistingLoanModel {
    public objectValueAmount?: number;
    public paidToCustomerAmount?: number;
    public mortgageLoansToSettle?: MortgageLoansToSettleModel[];
}
export class MortgageLoansToSettleModel {
    public currentDebtAmount?: number;
    public shouldBeSettled?: boolean;
    public bankName?: string;
    public currentMonthlyAmortizationAmount?: number;
    public interestRatePercent?: number;
    public loanNumber?: string;
}
