import { Injectable } from '@angular/core';
import { CustomerPagesApiService } from '../../common-services/customer-pages-api.service';
import { SharedApplicationBasicInfoModel } from '../../shared-components/applications-list/applications-list.component';

@Injectable({
    providedIn: 'root',
})
export class CustomerPagesMortgageLoanApiService {
    constructor(private apiService: CustomerPagesApiService) {}

    public shared() {
        return this.apiService;
    }

    public createApplication(application: MlApplicationModel): Promise<{
        ApplicationNr: string;
    }> {
        return this.apiService.postLocal('/api/embedded-customerpages/create-mortgageloan-application', application);
    }

    /*
    A person has properties like: civicRegNr, email, phone
     */
    public generateTestPersons(
        nrOfApplicants: number,
        isAccepted: boolean,
        useCommonAddress: boolean
    ): Promise<{
        Persons: { Properties: any }[];
    }> {
        return this.apiService.post('nTest', 'Api/TestPerson/GetOrGenerate', {
            persons: nrOfApplicants === 1 ? [{ isAccepted }] : [{ isAccepted }, { isAccepted }],
            useCommonAddress: useCommonAddress,
        });
    }

    public fetchApplications(): Promise<{
        Applications: SharedApplicationBasicInfoModel[];
    }> {
        return this.apiService.post('nPreCredit', 'api/MortgageLoanStandard/CustomerPages/Fetch-Applications', {});
    }

    public fetchApplication(applicationNr: string): Promise<{ Application: ExtendedMlApplicationModel }> {
        return this.apiService.post('nPreCredit', 'api/MortgageLoanStandard/CustomerPages/Fetch-Application', {
            applicationNr,
        });
    }
}

export interface ExtendedMlApplicationModel extends SharedApplicationBasicInfoModel {
    LatestAcceptedDecision?: {
        IsFinal: boolean;
        LoanAmount: number;
    };
    ObjectSummary?: {
        IsApartment: boolean;
        AddressStreet: string;
        AddressZipCode: string;
        AddressCity: string;
        AddressMunicipality: string;
        AddressApartmentNr: string;
        AddressSeTaxOfficeApartmentNr: string;
    };
    IsKycTaskActive: boolean;
    IsKycTaskApproved: boolean;
}

export interface MlApplicationModel {
    Applicants?: {
        CivicRegNr: string;
        FirstName?: string;
        LastName?: string;
        Email?: string;
        Phone?: string;
        IsPartOfTheHousehold?: boolean;
        IncomePerMonthAmount?: number;
        Employment?: string;
        EmployedSince?: string;
        EmployedTo?: string;
        Employer?: string;
        EmployerPhone?: string;
        HasConsentedToCreditReport?: boolean;
    }[];
    Purchase?: {
        ObjectPriceAmount: number;
        OwnSavingsAmount: number;
    };
    ChangeExistingLoan?: {
        ObjectValueAmount: number;
        PaidToCustomerAmount: number;
        MortgageLoansToSettle: {
            BankName?: string;
            InterestRatePercent?: number;
            CurrentDebtAmount: number;
            CurrentMonthlyAmortizationAmount?: number;
            ShouldBeSettled: boolean;
        }[];
    };
    ObjectAddressStreet?: string;
    ObjectAddressZipcode?: string;
    ObjectAddressCity?: string;
    ObjectAddressMunicipality?: string;

    ObjectOtherMonthlyCostsAmount?: number;
    ObjectLivingArea?: number;
    ObjectTypeCode?: string;
    ObjectMonthlyFeeAmount?: number;

    SeBrfApartmentNr?: string;

    OutgoingChildSupportAmount?: number;
    IncomingChildSupportAmount?: number;
    ChildBenefitAmount?: number;

    HouseholdChildren?: {
        Exists?: boolean;
        AgeInYears?: number;
        SharedCustody?: boolean;
    }[];

    LoansToSettle?: {
        CurrentDebtAmount: number;
        LoanType: string; // typeof enum CreditStandardOtherLoanType
        MonthlyCostAmount?: number;
    }[];
}
