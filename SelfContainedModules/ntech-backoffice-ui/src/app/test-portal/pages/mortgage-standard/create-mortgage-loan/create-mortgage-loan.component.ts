import { Component, OnInit } from '@angular/core';
import { JsonEditorOptions } from 'ang-jsoneditor';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import {
    NTechPaymentPlanService,
    RepaymentTimePaymentPlanRequest,
} from 'src/app/common-services/ntech-paymentplan.service';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Randomizer } from 'src/app/common-services/randomizer';
import { StringDictionary } from 'src/app/common.types';
import { TestPortalApiServiceService } from 'src/app/test-portal/services/TestPortalApiService.service';
import { Applicant, CreateMortgageLoanStandardRequest } from './create-mortgageloan-standard-request';

@Component({
    selector: 'create-mortgage-loan',
    templateUrl: './create-mortgage-loan.component.html',
})
export class CreateMortgageLoanComponent implements OnInit {
    constructor(
        private apiService: TestPortalApiServiceService,
        private validationService: NTechValidationService,
        private toastr: ToastrService,
        private config: ConfigService,
        private paymentPlanService: NTechPaymentPlanService
    ) {}

    public model: Model;
    private LoanHistoryLocalStorageKeyName: string = 'NTech-MortgageLoansStandardHistoryV1';
    public ClientSetting: {
        useFixedAnnuity: boolean;
    };
    public amortizationExceptionDropdown: string = 'No';

    async ngOnInit() {
        this.ClientSetting = {
            useFixedAnnuity: true,
        };
        await this.reset();
    }

    async reset() {
        let rates = await this.apiService.fetchMortgageLoanFixedInterstRates();
        this.model = {
            historyItems: this.getHistoryItems(),
            formState: 'Initial',
            mainApplicantMode: 'New',
            coApplicantMode: 'None',
            providerName: 'self',
            amortizationRule: 'r201723',
            ltvPercent: '45',
            repaymentTimeMonths: Randomizer.anyOf([120, 180, 240, 300, 360, 420, 480]), // 10-40 years,
            actualAmortizationAmount: Randomizer.anyEvenNumberBetween(1000, 5000, 100),
            amortizationException: {
                hasException: false,
                toDate: moment(this.config.getCurrentDateAndTime()).add(1, 'year').format('yyyy-MM-DD'),
                amount: 0,
            },
            reuseCollateralCreditNr: '',
            referenceRates: rates.CurrentRates,
            fixedReferenceInterestRateMonthCount: rates.CurrentRates[0].MonthCount.toString(),
            marginInterestRatePercent: '0',
        } as Model;
    }

    // Needs to be created once per editor since it cannot be shared.
    getJsonEditorOptions() {
        let options = new JsonEditorOptions();
        options.modes = ['code', 'text', 'tree', 'view'];
        return options;
    }

    addToHistory(item: LoanHistoryItem) {
        let items = this.getHistoryItems();
        items.push(item);
        localStorage.setItem(this.LoanHistoryLocalStorageKeyName, JSON.stringify(items));
        this.model.historyItems = items;
    }

    getHistoryItems(): LoanHistoryItem[] {
        let itemsJson = localStorage.getItem(this.LoanHistoryLocalStorageKeyName);
        if (!itemsJson) {
            return [];
        }

        let items = JSON.parse(itemsJson) as LoanHistoryItem[];
        return items.sort((a: LoanHistoryItem, b: LoanHistoryItem) => {
            return <any>new Date(b.createdDate) - <any>new Date(a.createdDate);
        });
    }

    clearHistoryItems() {
        localStorage.removeItem(this.LoanHistoryLocalStorageKeyName);
        this.model.historyItems = [];
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

    private async getPropertyAndParties(reuseCreditNr: string, applicantCustomerIds: number[]) {
        let collateralId: number;
        if (reuseCreditNr) {
            let result = await this.apiService.fetchMortageLoanCollaterals([reuseCreditNr]);
            collateralId = result.Collaterals[0].CollateralId;
        } else {
            let collateralAddressSourceTestPerson = await this.apiService.getOrGenerateTestPerson(null, false);
            let objectTypeCode = Randomizer.anyOf(['seFastighet', 'seBrf']);
            let props: StringDictionary = {};
            props['objectTypeCode'] = objectTypeCode;
            if (objectTypeCode === 'seBrf') {
                let brfCompany = await this.apiService.getOrGenerateTestCompany(false);
                props['seBrfOrgNr'] = brfCompany.Orgnr;
                props['seBrfName'] = brfCompany.Properties['companyName'];
                props['seBrfApartmentNr'] = 'S' + Randomizer.anyNumberBetween(50, 800);
                props['seTaxOfficeApartmentNr'] = Randomizer.anyNumberBetween(1101, 1199).toString();
            } else {
                let objectIdPerson = await this.apiService.getOrGenerateTestPerson(null, false);
                props['objectId'] = objectIdPerson.Properties['addressStreet'];
            }
            props['objectAddressStreet'] = collateralAddressSourceTestPerson.Properties['addressStreet'];
            props['objectAddressZipcode'] = collateralAddressSourceTestPerson.Properties['addressZipcode'];
            props['objectAddressCity'] = collateralAddressSourceTestPerson.Properties['addressCity'];
            props['objectAddressMunicipality'] = collateralAddressSourceTestPerson.Properties['addressCity'];

            let result = await this.apiService.createMortgageLoanCollateral({ Properties: props });
            collateralId = result.CollateralId;
        }
        let extraConsentPerson = await this.apiService.getOrGenerateTestPerson(null, true);
        return {
            ConsentingPartyCustomerIds: [...applicantCustomerIds, extraConsentPerson.CustomerId],
            PropertyOwnerCustomerIds: applicantCustomerIds,
            CollateralId: collateralId,
        };
    }

    private populateCustomerItems(values: StringDictionary) {
        let items: { Value: string; Name: string; ForceUpdate: boolean }[] = [];

        items.push({ Value: values['firstName'], Name: 'firstName', ForceUpdate: true });
        items.push({ Value: values['lastName'], Name: 'lastName', ForceUpdate: true });
        items.push({ Value: values['email'], Name: 'email', ForceUpdate: true });
        items.push({ Value: values['phone'], Name: 'phone', ForceUpdate: true });
        items.push({ Value: values['addressStreet'], Name: 'addressStreet', ForceUpdate: true });
        items.push({ Value: values['addressZipcode'], Name: 'addressZipcode', ForceUpdate: true });
        items.push({ Value: values['addressCity'], Name: 'addressCity', ForceUpdate: true });
        items.push({ Value: values['addressCountry'], Name: 'addressCountry', ForceUpdate: true });

        return items;
    }

    // Use async here so we can wait for one or two createcustomer-calls in the easiest way.
    async goToLoan() {
        let currentDate = this.config.getCurrentDateAndTime();

        let onSuccess = await this.apiService.createOrUpdateCustomer(
            this.model.mainApplicantCivicRegNr,
            'CreateCustomerManually',
            this.populateCustomerItems(this.model.mainApplicantData)
        );

        let mainCustomerId = onSuccess.CustomerId;

        let loanAmount = Randomizer.anyEvenNumberBetween(750000, 2000000, 10000);

        let applicants: Applicant[] = [
            {
                ApplicantNr: 1,
                CustomerId: mainCustomerId,
                OwnershipPercent: 50, // Set what here? Backend will set 100 if only one customer atm.
            } as Applicant,
        ];

        if (this.model.coApplicantMode !== 'None') {
            let co = await this.apiService.createOrUpdateCustomer(
                this.model.coApplicantCivicRegNr,
                'CreateCustomerManually',
                this.populateCustomerItems(this.model.coApplicantData)
            );

            applicants.push({
                ApplicantNr: 2,
                CustomerId: co.CustomerId,
                OwnershipPercent: 50, // Set what here? Backend will set 100 if only one customer atm.
            } as Applicant);
        }

        let propertyAndParties = await this.getPropertyAndParties(
            this.model.reuseCollateralCreditNr,
            applicants.map((x) => x.CustomerId)
        );

        let marginInterestRatePercent =
            this.validationService.parseDecimalOrNull(this.model.marginInterestRatePercent, true) ?? 0;
        let fixedReferenceInterestRateMonthCount = parseInt(this.model.fixedReferenceInterestRateMonthCount);
        let referenceInterstRatePercent = this.model.referenceRates.find(
            (x) => x.MonthCount === fixedReferenceInterestRateMonthCount
        ).RatePercent;

        let ltvFraction = parseInt(this.model.ltvPercent) / 100;

        let request: CreateMortgageLoanStandardRequest = {
            MonthlyFeeAmount: 0, // Go 0 for Hemdel or customized for other clients as well?
            NominalInterestRatePercent: marginInterestRatePercent,
            Applicants: applicants,
            NrOfApplicants: applicants.length,
            ProviderName: this.model.providerName,
            LoanAmount: loanAmount,
            AmortizationRule: this.model.amortizationRule,
            AmortizationBasisLoanAmount: loanAmount,
            AmortizationBasisObjectValue: Math.round(loanAmount / ltvFraction),
            AmortizationBasisDate: currentDate.format('yyyy-MM-DD'),
            DebtIncomeRatioBasisAmount: 1000,
            CurrentCombinedYearlyIncomeAmount: 500000,
            ProviderApplicationId: null,
            EndDate: moment(currentDate).add(this.model.repaymentTimeMonths, 'month').format('yyyy-MM-DD'),

            // Set all other fields here so they will be visible in the jsoneditor.
            ActiveDirectDebitAccount: null,
            AmortizationExceptionReasons: null,
            ApplicationNr: null,
            CapitalizedInitialFees: null,
            Collaterals: null, //Not in use for standard
            CreditNr: null, // will be set later, before request is being sent in, set null temp here.
            Documents: null,
            DrawnFromLoanAmountInitialFees: null,
            IsForNonPropertyUse: false,
            KycQuestionsJsonDocumentArchiveKey: null,
            LoanAmountParts: null,
            MainCreditCreditNr: null,
            SettlementDate: currentDate.format('yyyy-MM-DD'), // Not nullable in backend.
            SharedOcrPaymentReference: null,
            ActualAmortizationAmount: null,
            AmortizationExceptionUntilDate: null,
            AmortizationFreeUntilDate: null,
            AnnuityAmount: null,
            CurrentObjectValue: null,
            CurrentObjectValueDate: null,
            ExceptionAmortizationAmount: null,
            HistoricalStartDate: null,
            NextInterestRebindDate: currentDate
                .add(fixedReferenceInterestRateMonthCount, 'months')
                .format('yyyy-MM-DD'),
            InterestRebindMounthCount: fixedReferenceInterestRateMonthCount,
            NotificationDueDay: null,
            ReferenceInterestRate: referenceInterstRatePercent,
            RequiredAlternateAmortizationAmount: null,
            ...propertyAndParties,
        };

        if (this.model.amortizationException.hasException) {
            if (Math.abs(this.model.amortizationException.amount) < 0.000001) {
                request.AmortizationFreeUntilDate = this.model.amortizationException.toDate;
            } else {
                request.AmortizationExceptionUntilDate = this.model.amortizationException.toDate;
                request.ExceptionAmortizationAmount = this.model.amortizationException.amount;
                request.AmortizationExceptionReasons = ['Nyproduktion'];
            }
        }
        if (this.ClientSetting.useFixedAnnuity) request.ActualAmortizationAmount = this.model.actualAmortizationAmount;
        else
            request.AnnuityAmount = this.getAnnuityAmount(
                0,
                marginInterestRatePercent,
                referenceInterstRatePercent,
                loanAmount,
                this.model.repaymentTimeMonths
            );

        this.model.loanData = JSON.stringify(request);
        this.model.createLoanRequest = request;
        this.model.formState = 'CustomizeLoan';
    }

    private getAnnuityAmount(
        notificationFee: number,
        marginInterestRatePercent: number,
        referenceInterstRatePercent: number,
        requestedAmount: number,
        repaymentTime: number
    ): number {
        let request: RepaymentTimePaymentPlanRequest = {
            loansToSettleAmount: 0,
            paidToCustomerAmount: requestedAmount,
            repaymentTimeInMonths: repaymentTime,
            marginInterestRatePercent: marginInterestRatePercent,
            referenceInterestRatePercent: referenceInterstRatePercent,
            initialFeeWithheldAmount: 0,
            initialFeeCapitalizedAmount: 0,
            notificationFee: notificationFee,
        };

        let paymentPlan = this.paymentPlanService.calculatePlanWithAnnuitiesFromRepaymentTime(request);
        return paymentPlan.MonthlyCostExcludingFeesAmount;
    }

    createLoan() {
        this.apiService.drawCreditNrs(1).then((creditNrResult) => {
            let creditNr = creditNrResult.CreditNrs[0];
            this.model.createLoanRequest.CreditNr = creditNr;

            let applicants: string[] = [this.model.mainApplicantCivicRegNr];
            if (this.model.coApplicantMode != 'None') applicants.push(this.model.coApplicantCivicRegNr);

            this.apiService.createStandardMortgageLoan(this.model.createLoanRequest).then(
                (onSuccess) => {
                    this.toastr.success(`Standard mortgageloan ${onSuccess.CreditNr} created!`);
                    this.addToHistory({
                        applicationNr: '-',
                        createdDate: moment().toDate(),
                        creditNr: creditNr,
                        applicants: applicants,
                        urlToCredit: this.config
                            .getServiceRegistry()
                            .createUrl('nCredit', 'Ui/Credit', [['creditNr', creditNr]]),
                    });
                    this.reset();
                },
                (onError) => this.toastr.error('Could not create standard mortgage loan, check System Health')
            );
        });
    }

    public getRateDescription(rate: { MonthCount: number; RatePercent: number }) {
        let monthCount = rate.MonthCount;
        let time = monthCount % 12 === 0 ? `${monthCount / 12} år` : `${monthCount} månader`;
        return `${time} (${rate.RatePercent.toLocaleString('sv-SE')}%)`;
    }
}

export class Model {
    public formState: string;
    public historyItems: LoanHistoryItem[];

    public providerName: string;

    public ltvPercent: string;
    public mainApplicantCivicRegNr: string;
    public mainApplicantData: StringDictionary;
    public coApplicantCivicRegNr: string;
    public coApplicantData: StringDictionary;
    public mainApplicantMode: string;
    public coApplicantMode: string;
    public amortizationRule: string;
    public repaymentTimeMonths: number;
    public actualAmortizationAmount: number;
    public reuseCollateralCreditNr: string;
    public amortizationException: {
        hasException: boolean;
        toDate: string;
        amount: number;
    };
    public referenceRates: {
        MonthCount: number;
        RatePercent: number;
    }[];

    public applicationDetails: {
        applicationNr: string;
        requestedAmount: number;
        applicants: string[];
        repaymentTimeInMonths: number;
    };
    public createLoanRequest: CreateMortgageLoanStandardRequest;
    public loanData: string;
    public fixedReferenceInterestRateMonthCount: string;
    public marginInterestRatePercent: string;
}

export class LoanHistoryItem {
    applicationNr: string;
    creditNr: string;
    urlToCredit: string;
    createdDate: Date;
    applicants: string[];
}
