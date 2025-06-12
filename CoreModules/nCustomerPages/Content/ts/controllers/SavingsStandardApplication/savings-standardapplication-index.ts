var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms'])

ntech.angular.setupTranslation(app)

class SavingsAccountApplicationCtr {
    static readonly $inject = ['$scope', '$http', '$q', '$timeout', '$translate']

    apiClient: NTechCustomerPagesApi.ApiClient

    constructor(
        $scope: SavingsAccountApplicationNs.ILocalScope,
        private readonly $http: ng.IHttpService,
        private readonly $q: ng.IQService,
        private readonly $timeout: ng.ITimeoutService,
        $translate: any
    ) {
        this.apiClient = new NTechCustomerPagesApi.ApiClient(msg => toastr.error(msg), $http, $q)
        let initialDataTyped: SavingsAccountApplicationNs.IinitialDataTyped = initialData

        window.scope = $scope
        $scope.savingsAccountOverviewUrl = initialDataTyped.savingsAccountOverviewUrl
        $scope.isTest = initialDataTyped.isProduction === false
        $scope.loggedInCivicRegNr = initialDataTyped.civicRegNr
        $scope.fixedRateProducts = initialDataTyped.fixedInterestProducts;
        $scope.flexInterestRate = initialDataTyped.interestRatePercent;
        $scope.fixedRateProducts.sort((a, b) => a.termInMonths - b.termInMonths);

        $scope.selectedProduct = $scope.fixedRateProducts[0].id
        $scope.accountType = initialDataTyped.customerApplicationStatus === "CustomerHasAnActiveAccount" ? 'fixed' : 'flex';
        $scope.selectProduct = (id: string) => {
            $scope.selectedProduct = id;
            $scope.fixedRateProducts = angular.copy($scope.fixedRateProducts);
        }

        $scope.currentLanguage = () => {
            return $translate.use()
        }

        let getModeBasedOnStatus = (status: string) => {
            switch (status) {
                case 'CustomerIsAMinor':
                    return 'rejectedminor';
                case 'WaitingForClient':
                    return 'beingprocessed';
                case 'CustomerHasAnActiveAccount':
                    return 'hasactiveaccount';
                default:
                    return 'application';
            }
        }

        let isNullOrWhitespace = (input: any) => {
            if (typeof input === 'undefined' || input == null) return true;

            if (typeof input === 'string') {
                return input.trim().length < 1;
            } else {
                return false
            }
        }

        $scope.isValidIBAN = (value: any) => {
            if (isNullOrWhitespace(value))
                return true;

            return ntech.fi.isValidIBAN(value.replace(" ", ""))
        }

        $scope.isValidEmail = (value) => {
            //Just to help the user in case they mix up the fields. Not trying to ensure it's actually possible to send email here
            if (isNullOrWhitespace(value))
                return true

            const i = value.indexOf('@');
            return value.length >= 3 && i > 0 && i < (value.length - 1)
        }

        $scope.isValidPhoneNr = (value) => {
            if (isNullOrWhitespace(value))
                return true
            return !(/[a-z]/i.test(value))
        }

        $scope.mode = getModeBasedOnStatus(initialDataTyped.customerApplicationStatus)

        let isExistingCustomer: boolean = false
        let isTrustedInfoEditable: boolean = false

        if ($scope.mode == 'application' || $scope.mode == 'hasactiveaccount') {
            $scope.f = {}
            $scope.hasError = (n) => {
                return $scope.f.applicationform.$submitted && $scope.f.applicationform[n]?.$invalid;
            }

            if (initialDataTyped.existingCustomer) {
                $scope.namesViewModel = {
                    customerFirstName: initialDataTyped.existingCustomer.contact.customerFirstName,
                    customerLastName: initialDataTyped.existingCustomer.contact.customerLastName
                }
                $scope.addressViewModel = {
                    customerAddressStreet: initialDataTyped.existingCustomer.contact.customerAddressStreet,
                    customerAddressZipcode: initialDataTyped.existingCustomer.contact.customerAddressZipcode,
                    customerAddressCity: initialDataTyped.existingCustomer.contact.customerAddressCity
                }
                $scope.contactViewModel = {
                    customerEmail: initialDataTyped.existingCustomer.contact.customerEmail,
                    customerPhone: initialDataTyped.existingCustomer.contact.customerPhone
                }
                $scope.applicationEditModel = {}
                isExistingCustomer = true
            } else if (initialDataTyped.trustedSourceLookupCustomer?.contact) {
                $scope.namesViewModel = {
                    customerFirstName: initialDataTyped.trustedSourceLookupCustomer.contact.customerFirstName,
                    customerLastName: initialDataTyped.trustedSourceLookupCustomer.contact.customerLastName
                }
                $scope.addressViewModel = {
                    customerAddressStreet: initialDataTyped.trustedSourceLookupCustomer.contact.customerAddressStreet,
                    customerAddressZipcode: initialDataTyped.trustedSourceLookupCustomer.contact.customerAddressZipcode,
                    customerAddressCity: initialDataTyped.trustedSourceLookupCustomer.contact.customerAddressCity
                }
                $scope.contactEditModel = {}
                $scope.applicationEditModel = {}
            } else {
                $scope.namesEditModel = {}
                $scope.addressEditModel = {}
                $scope.contactEditModel = {}
                $scope.applicationEditModel = {}
                isTrustedInfoEditable = true
            }
        }

        $scope.cancel = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            $scope.isLoading = true
            $timeout(() => {
                document.location.href = initialDataTyped.cancelUrl
            })
        }

        $scope.apply = (_applicationModel: any, evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }

            if ($scope.f.applicationform.$invalid) {
                $scope.f.applicationform.$setSubmitted()
                return
            }
            $scope.isLoading = true

            let applicationItems: NTechCustomerPagesApi.ApplicationItem[] = []

            let addAllFieldsUsingFieldName = (ii: NTechCustomerPagesApi.ApplicationItem[], source: any) => {
                if (!source)
                    return
                angular.forEach(source, (v, k) => {
                    ii.push({Name: String(k), Value: v})
                })
            }

            if ($scope.namesEditModel) {
                addAllFieldsUsingFieldName(applicationItems, $scope.namesEditModel)
            }
            if ($scope.addressEditModel) {
                addAllFieldsUsingFieldName(applicationItems, $scope.addressEditModel)
            }
            if ($scope.contactEditModel) {
                addAllFieldsUsingFieldName(applicationItems, $scope.contactEditModel)
            }
            if ($scope.applicationEditModel) {
                addAllFieldsUsingFieldName(applicationItems, $scope.applicationEditModel)
            }

            switch ($scope.accountType) {
                case 'fixed':
                    applicationItems.push({Name: "savingsAccountTypeCode", Value: "FixedInterestAccount"});
                    applicationItems.push({Name: "fixedInterestProduct", Value: $scope.selectedProduct});
                    break;
                case 'flex':
                default:
                    applicationItems.push({Name: "savingsAccountTypeCode", Value: "StandardAccount"});
                    break;
            }

            let contactInfoLookupResultEncryptionKey = "";
            if (initialDataTyped?.trustedSourceLookupCustomer) {
                contactInfoLookupResultEncryptionKey = initialDataTyped.trustedSourceLookupCustomer.contactInfoLookupResultEncryptionKey
            }
            let application = {
                UserLanguage: $scope.currentLanguage(),
                ContactInfoLookupResultEncryptionKey: contactInfoLookupResultEncryptionKey,
                ApplicationItems: applicationItems,
                ExternalApplicationVariables: initialDataTyped.externalApplicationVariables
            }
            $scope.isLoading = true
            this.apiClient.savingsStandardApplicationApply(application).then(x => {
                if (!initialDataTyped.isProduction) {
                    localStorage.setItem(SavingsAccountApplicationNs.getTestApplicationStorageKey(isExistingCustomer, isTrustedInfoEditable), JSON.stringify({
                        namesEditModel: $scope.namesEditModel,
                        addressEditModel: $scope.addressEditModel,
                        contactEditModel: $scope.contactEditModel,
                        applicationEditModel: $scope.applicationEditModel
                    }))
                }
                $scope.isLoading = false
                document.location.href = x.questionsUrl
            })
        }

        if (initialDataTyped.isProduction === false) {
            $scope.loadLastSuccessfulTestApplication = function (evt) {
                if (evt) {
                    evt.preventDefault()
                }
                const last = localStorage.getItem(SavingsAccountApplicationNs.getTestApplicationStorageKey(isExistingCustomer, isTrustedInfoEditable));
                if (last) {
                    const a = JSON.parse(last);
                    if (a.namesEditModel) {
                        $scope.namesEditModel = a.namesEditModel
                    }
                    if (a.addressEditModel) {
                        $scope.addressEditModel = a.addressEditModel
                    }
                    if (a.contactEditModel) {
                        $scope.contactEditModel = a.contactEditModel
                    }
                    if (a.applicationEditModel) {
                        $scope.applicationEditModel = a.applicationEditModel
                    }
                }
            }
        }
    }
}

app.controller('savingsAccountApplicationCtr', SavingsAccountApplicationCtr);

module SavingsAccountApplicationNs {
    export function getTestApplicationStorageKey(isExistingCustomer: boolean, isTrustedInfoEditable: boolean) {
        let testCacheKeyPrefix: string = (isExistingCustomer ? 'e' : 'n') + (isTrustedInfoEditable ? 'c' : '_')
        return 'last_successful_application_' + testCacheKeyPrefix + '_v9'
    }

    export interface ILocalScope {
        selectProduct: (id: string) => void;
        fixedRateProducts: SavingsAccountApplicationNs.FixedRateProductModel[];
        flexInterestRate: number,
        currentLanguage: () => any
        savingsAccountOverviewUrl: string
        isTest: boolean
        loggedInCivicRegNr: string
        isValidIBAN: (value: any) => boolean
        isValidEmail: (value: any) => boolean
        isValidPhoneNr: (value: any) => boolean
        accountType: string
        selectedProduct: string
        mode: string
        f: any
        hasError: (value: any) => boolean
        namesViewModel: any
        contactViewModel: any
        addressViewModel: any
        applicationEditModel: any
        contactEditModel: any
        namesEditModel: any
        addressEditModel: any
        cancel: (evt: Event) => void
        loadLastSuccessfulTestApplication: (evt: Event) => void
        isLoading: boolean
        apply: (applicationModel: any, evt: Event) => void
    }

    export interface IinitialDataTyped {
        isProduction: boolean
        civicRegNr: string
        interestRatePercent: number,
        fixedInterestProducts: FixedRateProductModel[],
        existingCustomer: ExistingCustomerModel
        trustedSourceLookupCustomer: any
        translation: any
        customerApplicationStatus: string
        savingsAccountOverviewUrl: string
        cancelUrl: string
        externalApplicationVariables: NTechCustomerPagesApi.ApplicationItem[]
    }

    export interface FixedRateProductModel {
        id: string,
        name: string,
        interestRatePercent: number,
        termInMonths: number
    }

    export interface ExistingCustomerModel {
        contact: ExistingCustomerContactModel
    }

    export interface ExistingCustomerContactModel {
        customerAddressCity: string
        customerAddressStreet: string
        customerAddressZipcode: string
        customerAddressCountry: string
        customerFirstName: string
        customerLastName: string
        customerEmail: string
        customerPhone: string
    }
}