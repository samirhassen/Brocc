import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Randomizer } from 'src/app/common-services/randomizer';
import { Dictionary, NumberDictionary, StringDictionary } from 'src/app/common.types';
import { CreateMortgageLoanStandardRequest } from '../pages/mortgage-standard/create-mortgage-loan/create-mortgageloan-standard-request';

@Injectable({
    providedIn: 'root',
})
export class TestPortalApiServiceService {
    constructor(public apiService: NtechApiService) {}

    private credit = 'nCredit';
    private precredit = 'nPreCredit';
    private test = 'nTest';
    private customer = 'nCustomer';

    getOrGenerateTestPerson(
        civicRegNr: string,
        addToCustomerModule: boolean
    ): Promise<GetOrGenerateTestPersonResponseModel> {
        return this.apiService.post(this.test, '/Api/TestPerson/GetOrGenerateSingle', {
            civicRegNr: civicRegNr,
            isAccepted: true,
            addToCustomerModule: addToCustomerModule,
        });
    }

    getOrGenerateTestCompany(addToCustomerModule: boolean): Promise<GetOrGenerateTestCompanyResponseModel> {
        return this.apiService.post(this.test, '/Api/Company/TestCompany/GetOrGenerate', {
            isAccepted: true,
            addToCustomerModule: addToCustomerModule,
        });
    }

    createUnsecuredLoanStandardApplication(request: any): Promise<CreateCustomApplicationResult> {
        return this.apiService.post(this.precredit, '/api/UnsecuredLoanStandard/Create-Application', request);
    }

    createMortgageLoanStandardApplication(request: any): Promise<CreateCustomApplicationResult> {
        return this.apiService.post(this.precredit, '/api/MortgageLoanStandard/Create-Application', request);
    }

    createOrUpdateTestPersons(persons: string[], clearCache: boolean) {
        return this.apiService.post(this.test, '/Api/TestPerson/CreateOrUpdate', {
            persons: persons,
            clearCache: clearCache,
        });
    }

    fetchApplicantsByApplicationNr(applicationNr: string): Promise<FetchApplicantResult> {
        return this.apiService.post(this.precredit, 'api/ApplicationInfo/FetchApplicants', {
            applicationNr: applicationNr,
        });
    }

    drawCreditNrs(amount: number): Promise<{ CreditNrs: string[] }> {
        return this.apiService.post(this.credit, 'Api/Credit/Generate-Reference-Numbers', { CreditNrCount: amount });
    }

    createUnsecuredStandardLoan(request: CreateUnsecuredLoanRequest): Promise<void> {
        return this.apiService.post(this.credit, 'Api/CreateCredit', request);
    }

    createStandardMortgageLoan(request: CreateMortgageLoanStandardRequest): Promise<{ CreditNr: string }> {
        return this.apiService.post(this.credit, '/Api/MortgageLoans/Create', request);
    }

    async createOrUpdateCustomer(
        CivicRegNr: string,
        EventType: string,
        items: { Value: string; Name: string; ForceUpdate: boolean }[]
    ): Promise<{ CustomerId: number }> {
        return this.apiService.post(this.customer, 'Api/PersonCustomer/CreateOrUpdate', {
            CivicRegNr: CivicRegNr,
            EventType: EventType,
            Properties: items,
        });
    }

    createMortgageLoanCollateral(request: { Properties: StringDictionary }): Promise<{ CollateralId: number }> {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/Create-Collateral', request);
    }

    fetchMortageLoanCollaterals(creditNrs: string[]): Promise<{
        Collaterals: {
            CollateralId: number;
            CreditNrs: string[];
            CollateralItems: Dictionary<{ StringValue: string }>;
        }[];
    }> {
        return this.apiService.post('nCredit', '/Api/MortgageLoans/Fetch-Collaterals', { creditNrs });
    }

    async fetchMortgageLoanFixedInterstRates(): Promise<{
        CurrentRates: {
            MonthCount: number;
            RatePercent: number;
        }[];
    }> {
        return this.apiService.post('nCredit', 'api/MortgageLoans/FixedInterest/Fetch-All-Current', {});
    }
}

export class CreateCustomApplicationResult {
    ApplicationNr: string;
}

export class GetOrGenerateTestPersonResponseModel {
    CivicRegNr: string;
    Properties: StringDictionary;
    WasGenerated: boolean;
    CustomerId?: number;
}

export class GetOrGenerateTestCompanyResponseModel {
    Orgnr: string;
    Properties: StringDictionary;
    WasGenerated: boolean;
    CustomerId?: number;
}

export class CreateApplicationRequest {
    public Applicants: ApplicantRequestModel[];
    public LoansToSettleAmount: number;
    public RequestedAmount: number;
    public RequestedRepaymentTimeInMonths: number;
    public RequestedRepaymentTimeInDays: number;
    public Meta: { ProviderName: string; CustomerExternalIpAddress: string, SkipInitialScoring: boolean };
    public BankDataShareApplicants: {
        ApplicantNr: number
        ProviderName : string
        ProviderSessionId : string
        IncomeAmount: number
        LtlAmount: number
        ProviderDataArchiveKey: string
    }[];
    public LoanObjective: string;
}

/**
 * Pre-populated with some random values.
 */
export class ApplicantRequestModel {
    constructor(values: StringDictionary, skipNameAndAddress: boolean) {
        this.CivicRegNr = values['civicRegNr'];
        if(!skipNameAndAddress) {
            this.FirstName = values['firstName'];
            this.LastName = values['lastName'];
            this.AddressStreet = values['addressStreet'];
            this.AddressZipcode = values['addressZipcode'];
            this.AddressCity = values['addressCity'];
        }
        this.Phone = values['phone'];
        this.Email = values['email'];
    }

    public AddressCity: string;
    public AddressStreet: string;
    public AddressZipcode: string;
    public CivicRegNr: string;
    public CivilStatus: string = Randomizer.anyOf(['single', 'co_habitant', 'married', 'divorced', 'widowed']);
    public ClaimsToBePep?: string = 'false';
    public ClaimsToHaveKfmDebt?: boolean = false;
    public Email: string;
    public EmployedSince?: string = '1994-12-10';
    public EmployerName: string = 'Företaget AB';
    public EmployerPhone: string = '010 111 222 333';
    public EmploymentStatus: string = Randomizer.anyOf([
        'early_retiree',
        'project_employee',
        'full_time',
        'hourly_employment',
        'part_time',
        'student',
        'pensioner',
        'unemployed',
        'probationary',
        'self_employed',
        'substitute',
    ]);
    public FirstName: string;
    public HasConsentedToCreditReport?: boolean = true;
    public HasConsentedToShareBankAccountData?: boolean = true;
    public HousingCostPerMonthAmount?: number = Randomizer.anyEvenNumberBetween(500, 8000, 500);
    public HousingType: string = Randomizer.anyOf(['condominium', 'house', 'rental', 'tenant']);
    public LastName: string;
    public MonthlyIncomeAmount?: number = Randomizer.anyEvenNumberBetween(18000, 45000, 1000);
    public NrOfChildren?: number = Randomizer.anyOf([0, 1, 2, 3, 4]);
    public Phone: string;
    public HasLegalOrFinancialGuardian: boolean = false;
    public ClaimsToBeGuarantor: boolean = false;
}

export class FetchApplicantResult {
    public ApplicationNr: string;
    public NrOfApplicants: number;
    public CustomerIdByApplicantNr: NumberDictionary<number>;
}

export class CreateUnsecuredLoanRequest {
    constructor() {}

    public ApplicationNr: string;
    public CreditNr: string;
    public Iban: string;
    public BankAccountNr: string;
    public AnnuityAmount: number; // decimal, från paymentplan
    public Applicants: { ApplicantNr: number; AgreementPdfArchiveKey: string; CustomerId: number }[];
    public CreditAmount: number; // RequestedAmount
    public NrOfApplicants: number;
    public MarginInterestRatePercent: number; // decimal
    public NotificationFee: number;
    public CapitalizedInitialFeeAmount: number;
    public DrawnFromLoanAmountInitialFeeAmount: number; // clientConfig.RequiredSetting("ntech.credit.initialfeedrawnfromloanamount.fixed"
    public ProviderName: string;
    public ProviderApplicationId: string; // new guid i API, hur? Vad?
    public CampaignCode: string; // null i API
    public SourceChannel: string; // null i API
}
