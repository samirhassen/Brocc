import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary } from 'src/app/common.types';
import {
    StandardApplicationModelBase,
    StandardLoanApplicationEnumsModel,
} from 'src/app/shared-application-components/services/standard-application-base';

export function fillInRandomTestApplication(
    useCoApplicant: boolean,
    t: TestFunctionsModel,
    f: FormsHelper,
    support: ITestFunctionsSupport
) {
    let nrOfApplicants = useCoApplicant ? 2 : 1; // 1/3 applications have 2 applicants
    let isAccepted = t.getRandomInt(1, 3) < 3; // 1/3 rejected
    let isNameKnown = t.getRandomInt(1, 3) === 1;
    let isAddressKnown = isNameKnown ? t.getRandomInt(1, 2) == 1 : false;
    let isConsentCollected = t.getRandomInt(1, 4) === 1;

    support.baseApiService
        .post('nTest', 'Api/TestPerson/GetOrGenerate', {
            persons: nrOfApplicants === 1 ? [{ isAccepted }] : [{ isAccepted }, { isAccepted }],
            useCommonAddress: t.getRandomInt(1, 2) == 1, // 1/2 have the same address
        })
        .then((x: any) => {
            support.baseApiService
                .post('nTest', 'Api/Company/TestCompany/GetOrGenerateBulk', {
                    count: nrOfApplicants,
                    isAccepted: true,
                    addToCustomerModule: false,
                })
                .then((companyResult: any) => {
                    f.setValue('providerName', t.pickRandom(Object.keys(support.staticData.ProviderDisplayNameByName)));
                    let requestedLoanAmount = t.getRandomInt(20000, 150000);
                    f.setValue('requestedLoanAmount', requestedLoanAmount.toString());
                    f.setValue(
                        'requestedRepaymentTimeInMonths',
                        (6 + 12 * t.getRandomInt(0, 20)).toString().toString()
                    );
                    f.setValue('hasCoApplicant', nrOfApplicants === 2 ? 'true' : 'false');
                    for (let applicantNr of nrOfApplicants === 1 ? [1] : [1, 2]) {
                        let prefix = applicantNr === 1 ? support.mainApplicantPrefix : support.coApplicantPrefix;
                        let generatedPerson = x.Persons[applicantNr - 1].Properties;
                        f.setValue(`${prefix}CivicRegNr`, generatedPerson.civicRegNr);
                        f.setValue(`${prefix}Email`, generatedPerson.email);
                        f.setValue(`${prefix}Phone`, generatedPerson.phone);
                        if (t.getRandomInt(1, 5) === 5) {
                            //Mostly this info will not be available
                            f.setValue(`${prefix}ClaimsToBePep`, t.getRandomInt(1, 4) == 1 ? 'true' : 'false');
                        } else {
                            f.setValue(`${prefix}ClaimsToBePep`, '');
                        }
                        if (isNameKnown) {
                            f.setValue(`${prefix}FirstName`, generatedPerson.firstName);
                            f.setValue(`${prefix}LastName`, generatedPerson.lastName);
                        } else {
                            f.setValue(`${prefix}FirstName`, '');
                            f.setValue(`${prefix}LastName`, '');
                        }
                        if (isAddressKnown) {
                            f.setValue(`${prefix}AddressStreet`, generatedPerson.addressStreet);
                            f.setValue(`${prefix}AddressZipcode`, generatedPerson.addressZipcode);
                            f.setValue(`${prefix}AddressCity`, generatedPerson.addressCity);
                        } else {
                            f.setValue(`${prefix}AddressStreet`, '');
                            f.setValue(`${prefix}AddressZipcode`, '');
                            f.setValue(`${prefix}AddressCity`, '');
                        }

                        f.setValue(
                            `${prefix}HasConsentedToCreditReport`,
                            isConsentCollected ? (t.getRandomInt(1, 10) === 1 ? 'false' : 'true') : ''
                        );
                        f.setValue(
                            `${prefix}HasConsentedToShareBankAccountData`,
                            isConsentCollected ? (t.getRandomInt(1, 4) === 1 ? 'true' : 'false') : ''
                        );
                        f.setValue(
                            `${prefix}Marriage`,
                            t.getRandomInt(1, 5) == 1 ? '' : t.pickRandom(support.staticData.Enums.CivilStatuses).Code
                        );
                        f.setValue(
                            `${prefix}IncomePerMonthAmount`,
                            t.getRandomInt(1, 5) == 1 ? '' : t.getRandomInt(10000, 60000).toString()
                        );

                        //Employment
                        let employment = '';
                        let employerName = '';
                        let employerPhone = '';
                        let employedSince = '';
                        let employedTo = '';
                        if (t.getRandomInt(1, 5) < 5) {
                            // 1/5 we dont get any info
                            employment = t.pickRandom(support.staticData.Enums.EmploymentStatuses).Code;
                            employerName = StandardApplicationModelBase.isEmployerEmploymentCode(employment)
                                ? companyResult.Companies[applicantNr - 1].Properties['companyName']
                                : '';
                            employerPhone = StandardApplicationModelBase.isEmployerEmploymentCode(employment)
                                ? companyResult.Companies[applicantNr - 1].Properties['phone']
                                : '';
                            if (
                                StandardApplicationModelBase.isEmployedSinceEmploymentCode(
                                    employment,
                                    support.validationService
                                ) &&
                                StandardApplicationModelBase.isEmployedToEmploymentCode(employment)
                            ) {
                                //Current thinking is that this can only be since is past and to is future as in the middle of a project
                                //It cant be currently unemployed but have a future project so both are in the future
                                employedSince = t
                                    .getRandomHistoricalDate(support.config.getCurrentDateAndTime(), 15, 10 * 365)
                                    .format('YYYY-MM-DD');
                                employedTo = t
                                    .getRandomFutureDate(support.config.getCurrentDateAndTime(), 15, 365)
                                    .format('YYYY-MM-DD');
                            } else if (
                                StandardApplicationModelBase.isEmployedSinceEmploymentCode(
                                    employment,
                                    support.validationService
                                )
                            ) {
                                employedSince = t
                                    .getRandomHistoricalDate(support.config.getCurrentDateAndTime(), 15, 10 * 365)
                                    .format('YYYY-MM-DD');
                            } else if (StandardApplicationModelBase.isEmployedToEmploymentCode(employment)) {
                                employedTo = t
                                    .getRandomFutureDate(support.config.getCurrentDateAndTime(), 15, 365)
                                    .format('YYYY-MM-DD');
                            }
                        }

                        f.setValue(`${prefix}Employment`, employment);
                        f.setValue(`${prefix}Employer`, employerName);
                        f.setValue(`${prefix}EmployerPhone`, employerPhone);
                        f.setValue(`${prefix}EmployedSince`, employedSince);
                        f.setValue(`${prefix}EmployedTo`, employedTo);

                        if (employment === 'unemployed') {
                            f.setValue(`${prefix}IncomePerMonthAmount`, '0');
                        }
                    }

                    f.setValue('nrOfChildren', t.pickRandom(['', '0', '1', '2', '3', '4']));
                    f.setValue('housingType', t.pickRandom(support.staticData.Enums.HousingTypes).Code);
                    f.setValue(
                        'housingCostPerMonthAmount',
                        t.getRandomInt(1, 3) === 1 ? '' : t.getRandomInt(0, 8000).toString()
                    );

                    //Economy
                    support.clearOtherLoans();
                    if (t.getRandomInt(1, 3) < 3) {
                        let settlementAmount = t.getRandomInt(5000, requestedLoanAmount);
                        let prefix = support.addOtherLoanRowReturningNewPrefix();
                        f.setValue(`${prefix}LoanType`, 'unknown');
                        f.setValue(`${prefix}CurrentDebtAmount`, settlementAmount.toString());
                        f.setValue(`${prefix}ShouldBeSettled`, 'true');
                    }
                });
        });
}

//Used to pass references to test function setup
export interface ITestFunctionsSupport {
    addOtherLoanRowReturningNewPrefix(): string;
    clearOtherLoans(): void;
    staticData: { Enums: StandardLoanApplicationEnumsModel; ProviderDisplayNameByName: Dictionary<string> };
    mainApplicantPrefix: string;
    coApplicantPrefix: string;
    config: ConfigService;
    baseApiService: NtechApiService;
    validationService: NTechValidationService;
}
