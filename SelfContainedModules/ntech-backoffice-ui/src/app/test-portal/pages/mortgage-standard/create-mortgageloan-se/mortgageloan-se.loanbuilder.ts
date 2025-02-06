import { ConfigService } from "src/app/common-services/config.service";
import { Randomizer } from "src/app/common-services/randomizer";
import { Dictionary } from "src/app/common.types";
import { MortgageLoanSeService, SeAmortizationBasis } from "./mortgageloan-se.service";
import { PaymentOrderUiItem } from "src/app/common-services/payment-order-service";

export const CustomCostFormPrefix = 'custom_'

export class MortgageLoanSeLoanBuilder {
    constructor(private apiService: MortgageLoanSeService, private currentFixedRates: {
        MonthCount: string;
        RatePercent: number;
    }[],
        private config: ConfigService,
        private getFormValue: (name: string) => string,
        private loansToBeAdded: Dictionary<string>[],
        private customPaymentOrderItems: PaymentOrderUiItem[]) {

    }

    private isDone = false;
    private directDebitBankAccountNr: string = null;

    public async createLoanRequestBasedOnForm() {
        if (this.isDone) {
            throw new Error('This is not a service it is a call once operation. Create a new instance each time')
        }
        this.isDone = true;

        let loansRequest: any = {};

        let applicantCustomerIds: number[] = [];

        if (this.getFormValue('applicant1CivicRegNr')) {
            applicantCustomerIds.push((await this.apiService.shared().fetchCustomerIdByCivicRegNr(this.getFormValue('applicant1CivicRegNr'))).CustomerId);
        } else {
            applicantCustomerIds.push((await this.getOrGenerateTestPersonAndSetDirectDebitAccount(null, true)).CustomerId);
        }

        if (this.getFormValue('nrOfApplicants') === '2') {
            if (this.getFormValue('applicant2CivicRegNr')) {
                applicantCustomerIds.push((await this.apiService.shared().fetchCustomerIdByCivicRegNr(this.getFormValue('applicant2CivicRegNr'))).CustomerId);
            } else {
                applicantCustomerIds.push((await this.getOrGenerateTestPersonAndSetDirectDebitAccount(null, true)).CustomerId);
            }
        }

        let creditNrs: string[] = (await this.apiService.testPortal().drawCreditNrs(this.loansToBeAdded.length))?.CreditNrs;

        const collateralType = this.getFormValue('collateralType');
        const isReusingCollateral = collateralType === 'reuse';

        let uiLoans = this.parseUiLoans(this.loansToBeAdded, creditNrs);

        const nrOfConsentingParties = parseInt(this.getFormValue('nrOfConsentingParties'));
        let consentingPartiesCustomerIds: number[] = [];

        if (nrOfConsentingParties > 0) {
            let consentingParty1CivicRegNr = this.getFormValue('consentingParty1CivicRegNr');
            if (consentingParty1CivicRegNr) {
                consentingPartiesCustomerIds.push((await this.apiService.shared().fetchCustomerIdByCivicRegNr(consentingParty1CivicRegNr)).CustomerId);
            } else {
                consentingPartiesCustomerIds.push((await this.getOrGenerateTestPersonAndSetDirectDebitAccount(null, true)).CustomerId);
            }
        }

        let mortgageLoanAgreementNr : string = this.getFormValue('mortgageLoanAgreementNr')
        if(mortgageLoanAgreementNr) {
            loansRequest.agreementNr = mortgageLoanAgreementNr
        }

        let loansToAdd: LoanToAddModel[];
        let amortizationBasisShared : SeAmortizationBasis;
        if (isReusingCollateral) {
            let { existingCollateralId, amortizationBasis, loansToAddOnReuse } = await this.handleReuseCollateral(uiLoans);
            loansRequest.existingCollateralId = existingCollateralId;
            loansRequest.amortizationBasis = amortizationBasis;
            loansToAdd = loansToAddOnReuse;
            amortizationBasisShared = amortizationBasis;
        } else {
            let { newCollateral, newCollateralLoansToAdd, amortizationBasis } = await this.handleNewCollateral(uiLoans);
            loansRequest.newCollateral = newCollateral;
            loansRequest.amortizationBasis = amortizationBasis;
            loansToAdd = newCollateralLoansToAdd;
            amortizationBasisShared = amortizationBasis;
        }

        if (!this.directDebitBankAccountNr) {
            //If we reused everything, generate a seprate test person for the bank account
            await this.getOrGenerateTestPersonAndSetDirectDebitAccount(null, false);
        }

        loansRequest.loans = loansToAdd.map(loanToAdd => {
            let basisLoan = amortizationBasisShared.loans.find(x => x.creditNr === loanToAdd.creditNr);
            return {
                monthlyFeeAmount: 20,
                fixedMonthlyAmortizationAmount: basisLoan.monthlyAmortizationAmount,
                activeDirectDebitAccount: {
                    bankAccountNrOwnerApplicantNr: 1,
                    bankAccountNr: this.directDebitBankAccountNr,
                    activeSinceDate: this.getFutureIsoDate(0)
                },
                loanAmount: loanToAdd.loanAmount,
                applicants: applicantCustomerIds.map((applicantCustomerId, applicantIndex) => ({
                    applicantNr: applicantIndex + 1,
                    customerId: applicantCustomerId,
                    agreementPdfArchiveKey: null,
                    ownershipPercent: applicantCustomerIds.length == 1 ? 100 : 50
                })),
                creditNr: loanToAdd.creditNr,
                providerName: "self",
                endDate: this.config.getCurrentDateAndTime().add(40, 'years').format('YYYY-MM-DD'),
                interestRebindMounthCount: loanToAdd.rebindingMonthCount,
                nextInterestRebindDate: this.config.getCurrentDateAndTime().add(loanToAdd.rebindingMonthCount, 'months').format('YYYY-MM-DD'),
                nominalInterestRatePercent: loanToAdd.marginInterestRatePercent,
                referenceInterestRate: loanToAdd.referenceInterestRate,
                consentingPartyCustomerIds: consentingPartiesCustomerIds,
                propertyOwnerCustomerIds: applicantCustomerIds,
                amortizationExceptionReasons: loanToAdd.amortizationExceptionReason ? [loanToAdd.amortizationExceptionReason] : null,
                amortizationExceptionUntilDate: loanToAdd.amortizationExceptionUntilDate,
                exceptionAmortizationAmount: loanToAdd.monthlyExceptionAmortizationAmount,
                firstNotificationCosts: loanToAdd.firstNotificationCosts
                /*
                Not current used:
                kycQuestionsJsonDocumentArchiveKey: null,
                documents: [],
                */
            }
        });

        return loansRequest;
    }

    public async createLoanBasedOnFormRequest(loansRequest: any) {
        return await this.apiService.createSwedishMortgageLoan(loansRequest);
    }

    public parseUiLoans(loansToBeAdded: Dictionary<string>[], creditNrs: string[]) {
        return loansToBeAdded.map((loanToAdd, loanToAddIndex) =>
            MortgageLoanSeLoanBuilder.parseUiLoan(loanToAdd, creditNrs[loanToAddIndex], this.currentFixedRates, this.customPaymentOrderItems));
    }

    public static parseUiLoan(loanToAdd: Dictionary<string>, creditNr: string, currentFixedRates: {
        MonthCount: string;
        RatePercent: number;
    }[], customCosts: PaymentOrderUiItem[]): UiLoanModel {
        const getLoanToAddItem = (name: string, loanToAdd: Dictionary<string>) => loanToAdd[name]?.trim() ?? '';
        const rebindingMonthCount = parseInt(getLoanToAddItem('rebindingMonthCount', loanToAdd));
        const referenceInterestRate = currentFixedRates.find(x => x.MonthCount === rebindingMonthCount.toString()).RatePercent;
        const loanAmount = parseInt(getLoanToAddItem('loanAmount', loanToAdd));
        const isUsingAlternateAmortizationRule = getLoanToAddItem('isUsingAlternateAmortizationRule', loanToAdd) === 'true';
        const amortizationException = getLoanToAddItem('amortizationException', loanToAdd);
        const maxLoanAmountRaw = (getLoanToAddItem('maxLoanAmount', loanToAdd) ?? '').trim();

        return {
            creditNr: creditNr,
            loanAmount: loanAmount,
            maxLoanAmount: maxLoanAmountRaw.length === 0 ? loanAmount : parseInt(maxLoanAmountRaw),
            marginInterestRatePercent: parseFloat(getLoanToAddItem('marginInterestRatePercent', loanToAdd).replace(',', '.')),
            referenceInterestRate: referenceInterestRate,
            rebindingMonthCount: rebindingMonthCount,
            isUsingAlternateAmortizationRule: isUsingAlternateAmortizationRule,
            amortizationException: amortizationException,
            firstNotificationCosts: customCosts.map(x => ({
                text: x.text,
                requestItem: {
                    costCode: x.orderItem.code,
                    costAmount: parseFloat(getLoanToAddItem(CustomCostFormPrefix + x.uniqueId, loanToAdd))
                }
            }))
        };
    }

    private getFutureIsoDate(nrOfDaysFromNow: number) {
        return this.config.getCurrentDateAndTime().add(nrOfDaysFromNow, 'day').format('YYYY-MM-DD')
    }

    private async handleReuseCollateral(uiLoans: UiLoanModel[]) {
        const reuseCollateralCreditNr = this.getFormValue('reuseCollateralCreditNr');
        let collateral = await this.apiService.testPortal().fetchMortageLoanCollaterals([reuseCollateralCreditNr]);
        let { amortizationBasis } = await this.apiService.getAmortizationBasisForExistingCredit(reuseCollateralCreditNr, true);
        if (amortizationBasis == null) {
            throw new Error('Missing amortization basis on collateral');
        }
        const existingCollateralId = collateral.Collaterals[0].CollateralId;

        //Add the new loans to the exist. This only works because we force all of these to the alternate rule
        let newAmortizationBasis = await this.apiService.appendAlternateRuleLoansToAmortizationBasis(
            amortizationBasis,
            uiLoans.map(x => ({
                creditNr: x.creditNr,
                loanAmount: x.loanAmount
            })), null);

        const loansToAdd = uiLoans.map(uiLoan => {
            let newBasisLoan = newAmortizationBasis.loans.find(x => x.creditNr === uiLoan.creditNr);
            return {
                creditNr: uiLoan.creditNr,
                loanAmount: uiLoan.loanAmount,
                maxLoanAmount: uiLoan.loanAmount,
                marginInterestRatePercent: uiLoan.marginInterestRatePercent,
                referenceInterestRate: uiLoan.referenceInterestRate,
                rebindingMonthCount: uiLoan.rebindingMonthCount,
                isUsingAlternateAmortizationRule: newBasisLoan.isUsingAlternateRule,
                monthlyAmortizationAmount: newBasisLoan.monthlyAmortizationAmount,
                monthlyExceptionAmortizationAmount: null,
                amortizationExceptionReason: null,
                amortizationExceptionUntilDate: null,
                firstNotificationCosts: uiLoan.firstNotificationCosts.map(x => x.requestItem)
            };
        });

        return {
            existingCollateralId,
            amortizationBasis: newAmortizationBasis,
            loansToAddOnReuse: loansToAdd
        }
    }

    private async handleNewCollateral(uiLoans: UiLoanModel[]) {
        const collateralType = this.getFormValue('collateralType');
        let isBrf = collateralType === 'newBrf';
        let collateralAddressSourceTestPerson = await this.getOrGenerateTestPersonAndSetDirectDebitAccount(null, false);
        let brfOrObjectIdSource = await this.apiService.testPortal().getOrGenerateTestCompany(false);
        const newCollateral: any = {
            isBrfApartment: isBrf,
            addressStreet: collateralAddressSourceTestPerson.Properties['addressStreet'],
            addressZipcode: collateralAddressSourceTestPerson.Properties['addressZipcode'],
            addressCity: collateralAddressSourceTestPerson.Properties['addressCity'],
            addressMunicipality: collateralAddressSourceTestPerson.Properties['addressCity']
        };
        if (isBrf) {
            newCollateral.brfOrgNr = brfOrObjectIdSource.Orgnr;
            newCollateral.brfName = brfOrObjectIdSource.Properties['companyName'];
            newCollateral.brfApartmentNr = 'S' + Randomizer.anyNumberBetween(50, 800);
            newCollateral.taxOfficeApartmentNr = Randomizer.anyNumberBetween(1101, 1199).toString();
        } else {
            newCollateral.objectId = brfOrObjectIdSource.Properties['addressStreet']
        }

        const loansToAdd : LoanToAddModel[] = uiLoans.map(uiLoan => {
            let monthlyExceptionAmortizationAmount: number = null;
            let amortizationExceptionReason: string = null;
            let amortizationExceptionUntilDate: string = null;
            if (uiLoan.amortizationException === 'twoYearsZero') {
                monthlyExceptionAmortizationAmount = 0;
                amortizationExceptionUntilDate = this.getFutureIsoDate(2 * 365);
                amortizationExceptionReason = 'Nyproduktion';
            }            
            return {
                creditNr: uiLoan.creditNr,
                loanAmount: uiLoan.loanAmount,
                maxLoanAmount: uiLoan.maxLoanAmount,
                marginInterestRatePercent: uiLoan.marginInterestRatePercent,
                referenceInterestRate: uiLoan.referenceInterestRate,
                rebindingMonthCount: uiLoan.rebindingMonthCount,
                isUsingAlternateAmortizationRule: uiLoan.isUsingAlternateAmortizationRule,
                monthlyExceptionAmortizationAmount: monthlyExceptionAmortizationAmount,
                amortizationExceptionReason: amortizationExceptionReason,
                amortizationExceptionUntilDate: amortizationExceptionUntilDate,
                firstNotificationCosts: uiLoan.firstNotificationCosts.map(x => x.requestItem)
            };
        });

        const totalLoanAmount = loansToAdd.map(x => x.maxLoanAmount).reduce((result, current) => result + current, 0);
        const totalNonAlternateLoanAmount = loansToAdd.filter(x => !x.isUsingAlternateAmortizationRule).map(x => x.maxLoanAmount).reduce((result, current) => result + current, 0);

        const amortizationRuleCode = this.getFormValue('newCollateralAmortizationRuleCode');
        const desiredLtv = parseInt(this.getFormValue('newCollateralLtvPercent')) / 100;
        const objectValueAmount = totalLoanAmount / desiredLtv;
        const desiredLti = parseInt(this.getFormValue('newCollateralLtiFraction'));
        const objectValuationAgeInMonths = parseInt(this.getFormValue('objectValuationAgeInMonths'));

        let amortizationPercent = 0;
        if (desiredLti > 4.5 && amortizationRuleCode == 'r201723') {
            amortizationPercent += 0.01;
        }
        if (desiredLtv > 0.5 && amortizationRuleCode !== 'none') {
            amortizationPercent += 0.01;
        }
        if (desiredLtv > 0.7 && amortizationRuleCode !== 'none') {
            amortizationPercent += 0.01;
        }

        const amortizationBasis : SeAmortizationBasis = {
            objectValueDate: this.getFutureIsoDate(-objectValuationAgeInMonths * 30),
            objectValue: Math.round(objectValueAmount),
            ltiFraction: desiredLti,
            ltvFraction: desiredLtv,
            currentCombinedYearlyIncomeAmount: Math.round(totalLoanAmount / desiredLti),
            otherMortageLoansAmount: 0,
            loans: loansToAdd.map(x => {
                return {
                    creditNr: x.creditNr,
                    currentCapitalBalanceAmount: x.loanAmount,
                    ruleCode: x.isUsingAlternateAmortizationRule ? 'r201723' : amortizationRuleCode,
                    maxCapitalBalanceAmount: x.maxLoanAmount,
                    isUsingAlternateRule: x.isUsingAlternateAmortizationRule,
                    monthlyAmortizationAmount: x.isUsingAlternateAmortizationRule 
                        ? Math.round(x.loanAmount / 10 / 12)
                        //Distribute the total required amount over all the loans that are not using the alternate rule according to their sizse
                        : Math.round((x.loanAmount / totalNonAlternateLoanAmount) * (totalNonAlternateLoanAmount * amortizationPercent / 12)),
                    amortizationExceptionReason: null,
                    amortizationExceptionUntilDate: null,
                    monthlyExceptionAmortizationAmount: null
                }
            })
        };

        return {
            newCollateral,
            newCollateralLoansToAdd: loansToAdd,
            amortizationBasis
        }
    }

    private async getOrGenerateTestPersonAndSetDirectDebitAccount(civicRegNr: string, addToCustomerModule: boolean) {
        let result = await this.apiService.testPortal().getOrGenerateTestPerson(civicRegNr, addToCustomerModule);
        if (!this.directDebitBankAccountNr) {
            this.directDebitBankAccountNr = result.Properties['bankAccountNr'];
        }
        return result;
    };
}

export interface UiLoanModel {
    creditNr: string
    loanAmount: number
    maxLoanAmount: number
    marginInterestRatePercent: number
    referenceInterestRate: number
    rebindingMonthCount: number
    isUsingAlternateAmortizationRule: boolean
    amortizationException: string
    firstNotificationCosts: { requestItem: { costCode: string, costAmount: number }, text: string }[]
}

interface LoanToAddModel {
    creditNr: string
    loanAmount: number
    maxLoanAmount: number
    marginInterestRatePercent: number
    referenceInterestRate: number
    rebindingMonthCount: number
    isUsingAlternateAmortizationRule: boolean
    monthlyExceptionAmortizationAmount: number
    amortizationExceptionReason: string
    amortizationExceptionUntilDate: string
    firstNotificationCosts: { costCode: string, costAmount: number }[]
}