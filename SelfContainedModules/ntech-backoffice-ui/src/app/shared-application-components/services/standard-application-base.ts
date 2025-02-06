import {
    ComplexApplicationList,
    FlattenedComplexApplicationListItem,
} from 'src/app/common-services/complex-application-list';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { NTechMath } from 'src/app/common-services/ntech.math';
import { Dictionary, NumberDictionary } from 'src/app/common.types';
import { ApplicationInfoModel, WorkflowModel } from './shared-loan-application-api.service';

export abstract class StandardApplicationModelBase {
    constructor(
        public readonly applicationNr: string,
        public readonly nrOfApplicants: number,
        public readonly applicationInfo: ApplicationInfoModel,
        public readonly customerIdByApplicantNr: NumberDictionary<number>,
        public readonly workflow: {
            ApplicationVersion: number;
            Model: WorkflowModel;
        },
        private complexApplicationLists: Dictionary<ComplexApplicationList>,
        private enums: StandardLoanApplicationEnumsModel,
        private customerPagesApplicationsUrl: string
    ) {}

    public getCivilStatuses() {
        return this.enums.CivilStatuses;
    }

    public getEmploymentStatuses() {
        return this.enums.EmploymentStatuses;
    }

    public getHousingTypes() {
        return this.enums.HousingTypes;
    }

    public getOtherLoanTypes() {
        return this.enums.OtherLoanTypes;
    }

    public getApplicantNrs() {
        let n = [];
        for (var applicantNr = 1; applicantNr <= this.nrOfApplicants; applicantNr++) {
            n.push(applicantNr);
        }
        return n;
    }

    public static isEmployerEmploymentCode(code: string) {
        let codes = [
            'full_time',
            'project_employee',
            'hourly_employment',
            'part_time',
            'probationary',
            'self_employed',
            'substitute',
        ];
        return NTechMath.isAnyOf(code, codes);
    }

    public static isEmployedSinceEmploymentCode(code: string, validationService: NTechValidationService) {
        return !(NTechMath.isAnyOf(code, ['unemployed']) || validationService.isNullOrWhitespace(code));
    }

    public static isEmployedToEmploymentCode(code: string) {
        return NTechMath.isAnyOf(code, ['project_employee', 'probationary']);
    }

    public getComplexApplicationList(listName: string, emptyListOnNotExists: boolean): ComplexApplicationList {
        let actualList = this.complexApplicationLists[listName];
        return actualList ? actualList : emptyListOnNotExists ? new ComplexApplicationList(listName, []) : null;
    }

    public getComplexApplicationListNames() {
        return Object.keys(this.complexApplicationLists);
    }

    getHouseholdChildren(): StandardLoanApplicationChildModel[] {
        let childrenList = this.getComplexApplicationList('HouseholdChildren', true);
        let children: StandardLoanApplicationChildModel[] = [];
        for (let childRowNr of childrenList.getRowNumbers()) {
            let row = childrenList.getRow(childRowNr, false);
            row.getUniqueItem;
            children.push({
                complexListRowNr: childRowNr,
                sharedCustody: row.getUniqueItemBoolean('sharedCustody'),
                ageInYears: row.getUniqueItemInteger('ageInYears'),
            });
        }
        return children;
    }

    getOtherLoans(): StandardLoanApplicationOtherLoanModel[] {
        let otherLoansList = this.getComplexApplicationList('LoansToSettle', true);
        let loans: StandardLoanApplicationOtherLoanModel[] = [];
        for (let loanRowNr of otherLoansList.getRowNumbers()) {
            let row = otherLoansList.getRow(loanRowNr, false);
            loans.push({
                complexListRowNr: loanRowNr,
                loanType: row.getUniqueItem('loanType'),
                currentDebtAmount: row.getUniqueItemInteger('currentDebtAmount'),
                monthlyCostAmount: row.getUniqueItemInteger('monthlyCostAmount'),
                currentInterestRatePercent: row.getUniqueItemDecimal('currentInterestRatePercent'),
                shouldBeSettled: row.getUniqueItemBoolean('shouldBeSettled'),
                bankAccountNrType: row.getUniqueItem('bankAccountNrType'),
                bankAccountNr: row.getUniqueItem('bankAccountNr'),
                settlementPaymentReference: row.getUniqueItem('settlementPaymentReference'),
                settlementPaymentMessage: row.getUniqueItem('settlementPaymentMessage'),
            });
        }
        return loans;
    }

    getCustomerPagesApplicationsUrl() {
        return this.customerPagesApplicationsUrl;
    }
}

export interface StandardApplicationInitialDataModelBase {
    ApplicationNr: string;
    ApplicationInfo: ApplicationInfoModel;
    ApplicantInfoByApplicantNr: NumberDictionary<ApplicantInfoModel>;
    NrOfApplicants: number;
    CustomerIdByApplicantNr: NumberDictionary<number>;
    AllConnectedCustomerIdsWithRoles: NumberDictionary<string[]>;
    ApplicationWorkflowVersion: number;
    CurrentWorkflowModel: WorkflowModel;
    Documents: StandardLoanApplicationDocumentModel[];
    ComplexListItems: FlattenedComplexApplicationListItem[];
    Enums: StandardLoanApplicationEnumsModel;
    CustomerPagesApplicationsUrl: string;
}

export class ApplicantInfoModel {
    CustomerId: number;
    FirstName: string;
    LastName: string;
    BirthDate: string;
    Email: string;
    AddressStreet: string;
    AddressCity: string;
    AddressZipcode: string;
    AddressCountry: string;
}

export interface StandardLoanApplicationEnumsModel {
    CivilStatuses: { Code: string; DisplayName: string }[];
    EmploymentStatuses: { Code: string; DisplayName: string }[];
    HousingTypes: { Code: string; DisplayName: string }[];
    OtherLoanTypes: { Code: string; DisplayName: string }[];
}

export interface StandardLoanApplicationDocumentModel {
    Id: number;
    DocumentType: string;
    ApplicantNr: number;
    CustomerId: number;
    DocumentArchiveKey: string;
    DocumentSubType: string;
}

export class StandardLoanApplicationChildModel {
    complexListRowNr: number;
    ageInYears: number;
    sharedCustody: boolean;
}

export class StandardLoanApplicationOtherLoanModel {
    complexListRowNr?: number;
    loanType?: string;
    currentDebtAmount?: number;
    monthlyCostAmount?: number;
    currentInterestRatePercent?: number;
    shouldBeSettled?: boolean;
    bankAccountNrType?: string;
    bankAccountNr?: string;
    settlementPaymentReference?: string;
    settlementPaymentMessage?: string;
}
