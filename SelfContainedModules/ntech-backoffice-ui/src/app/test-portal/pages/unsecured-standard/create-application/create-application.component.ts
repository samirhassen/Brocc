import { Component, OnInit, ViewChild } from '@angular/core';
import * as moment from 'moment';
import { JsonEditorComponent, JsonEditorOptions } from 'ang-jsoneditor';
import { StringDictionary, generateUniqueId } from 'src/app/common.types';
import {
    ApplicantRequestModel,
    CreateApplicationRequest,
    TestPortalApiServiceService,
} from 'src/app/test-portal/services/TestPortalApiService.service';
import { Randomizer } from 'src/app/common-services/randomizer';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'app-create-application',
    templateUrl: './create-application.component.html',
    styles: [],
})
export class CreateApplicationComponent implements OnInit {
    constructor(
        private apiService: TestPortalApiServiceService,
        private validationService: NTechValidationService,
        private toastr: ToastrService,
        private config: ConfigService
    ) {}

    private ApplicationHistoryLocalStorageKeyName: string = 'NTech-UnsecuredApplicationsHistoryV1';

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

    // Bug in backend Update-method that saves all properties except CivicRegnr & CivicRegNrCountry, leading to them missing
    // Don't call atm.
    updateTestPersons() {
        let personsToUpdate: string[] = [];
        if (this.model.mainApplicantMode === 'New') {
            personsToUpdate.push(JSON.stringify(this.model.mainApplicantData));
        }
        if (this.model.coApplicantMode === 'New') {
            personsToUpdate.push(JSON.stringify(this.model.coApplicantData));
        }

        if (personsToUpdate.length > 0) {
            this.apiService.createOrUpdateTestPersons(personsToUpdate, true).then((x) => {});
        }
    }

    goToApplication() {
        this.model.formState = 'Application';
        let applicants: ApplicantRequestModel[] = [new ApplicantRequestModel(this.model.mainApplicantData, true)];
        if (this.model.coApplicantData) {
            applicants.push(new ApplicantRequestModel(this.model.coApplicantData, true));
        }

        let requestedAmount = Randomizer.anyEvenNumberBetween(30000, 150000, 1000);
        let isRepaymentTimeDays: boolean = Randomizer.anyOf([true, false]);

        let request : CreateApplicationRequest = {
            LoansToSettleAmount: Randomizer.anyEvenNumberBetween(0, requestedAmount, 1000),
            RequestedAmount: requestedAmount,
            RequestedRepaymentTimeInMonths: isRepaymentTimeDays ? null : Randomizer.anyNumberBetween(2, 6),
            RequestedRepaymentTimeInDays: isRepaymentTimeDays ? Randomizer.anyNumberBetween(10, 29) : null,
            Applicants: applicants,
            Meta: { ProviderName: 'self', CustomerExternalIpAddress: '127.0.0.1', SkipInitialScoring: true },
            BankDataShareApplicants: [],
            LoanObjective: 'Övrig konsumtion'
        };

        if(this.config.isFeatureEnabled('ntech.feature.unsecuredloans.datasharing')) {
            let applicantNr = 1;
            for(let applicant of applicants) {
                let providerSessionId = 'kzt_' + generateUniqueId(25);
                request.BankDataShareApplicants.push({
                    ApplicantNr: applicantNr,
                    ProviderName : 'kreditz',
                    ProviderSessionId : providerSessionId,
                    IncomeAmount: applicant.MonthlyIncomeAmount && applicant.MonthlyIncomeAmount > 18000
                        ? Randomizer.anyEvenNumberBetween(applicant.MonthlyIncomeAmount - 10000, applicant.MonthlyIncomeAmount + 10000, 1)
                        : Randomizer.anyEvenNumberBetween(18000, 45000, 1),
                    LtlAmount: Randomizer.anyOf([1, -1]) * Randomizer.anyEvenNumberBetween(0, 3000, 1),
                    ProviderDataArchiveKey: null
                })
                applicantNr++;
            }
        }

        this.model.creditReports = request.Applicants.map((applicant, i) => {
            let applicantData = i === 0 ? this.model.mainApplicantData : this.model.coApplicantData;

            return  {
                'templateAccepted': 'true',
                'templateManualAttention': 'false',
                'personstatus': 'normal',
                'firstName': applicantData['firstName'],
                'lastName': applicantData['lastName'],
                'addressStreet': applicantData['addressStreet'],
                'addressZipcode': applicantData['addressZipcode'],
                'addressCity': applicantData['addressCity'],
                'hasDomesticAddress': 'true',
                'hasPosteRestanteAddress': 'false',
                'hasPostBoxAddress': 'false',
                'nrOfPaymentRemarks': '0',
                'hasPaymentRemark': 'false',
                'hasGuardian': 'false',
                'hasKfmBalance': 'false',
                'hasSwedishSkuldsanering': 'false',
                'latestIncomeYear': ((applicant.MonthlyIncomeAmount ?? 40000) * 12).toFixed(0),
                'latestIncomePerYear': ((applicant.MonthlyIncomeAmount ?? 40000) * 12).toFixed(0),
                'scoreValue': '75',
                'riskValue': '25',
                'hasSpecialAddress': 'false'
            };
        });

        this.model.application = request;
    }

    clearHistoryItems() {
        localStorage.removeItem(this.ApplicationHistoryLocalStorageKeyName);
        this.model.historyItems = [];
    }

    async createApplication() {
        try {
            let result = await this.apiService.createUnsecuredLoanStandardApplication(this.model.application);
            let item = {
                applicationNr: result.ApplicationNr,
                applicants: this.model.application.Applicants.map((x) => x.CivicRegNr),
                createdDate: moment().toDate(),
            } as ApplicationHistoryItem;
            this.addToHistory(item);


            for(var i=0; i < this.model.application.Applicants.length; i++) {
                let applicant = this.model.application.Applicants[i];
                let customerId = (await this.apiService.apiService.shared.fetchCustomerIdByCivicRegNr(applicant.CivicRegNr)).CustomerId;
                await this.apiService.apiService.post('nCreditReport', 'CreditReport/InjectPersonTestReport', {
                    ProviderName: 'CreditSafeSe', //Can be any provider that exists
                    ReasonType: 'CreditApplication',
                    ReasonData: result.ApplicationNr,
                    CivicRegNr: applicant.CivicRegNr,
                    CustomerId: customerId,
                    CreditReportItems: this.model.creditReports[i]
                });
            }

            this.reset();
        } catch(e: any) {
            this.toastr.error('Create application failed');
        }
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
    public application: CreateApplicationRequest;
    public creditReports: StringDictionary[]
}

export class ApplicationHistoryItem {
    applicationNr: string;
    createdDate: Date;
    applicants: string[];
}
