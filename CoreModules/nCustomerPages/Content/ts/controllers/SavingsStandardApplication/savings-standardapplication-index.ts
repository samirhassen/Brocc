var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms'])

ntech.angular.setupTranslation(app)

class SavingsAccountApplicationCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout', '$translate']

    apiClient: NTechCustomerPagesApi.ApiClient
    
    constructor(
        $scope: SavingsAccountApplicationNs.ILocalScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService,
        $translate: any
    ) {
        this.apiClient = new NTechCustomerPagesApi.ApiClient(msg => toastr.error(msg), $http, $q)
        let initialDataTyped: SavingsAccountApplicationNs.IinitialDataTyped = initialData

        window.scope = $scope
        $scope.savingsAccountOverviewUrl = initialDataTyped.savingsAccountOverviewUrl
        $scope.isTest = initialDataTyped.isProduction === false
        $scope.loggedInCivicRegNr = initialDataTyped.civicRegNr

        $scope.currentLanguage = () => {
            return $translate.use()
        }

        let getModeBasedOnStatus = (status: string) => {
            if (status === 'CustomerIsAMinor') {
                return 'rejectedminor'
            } else if (status === 'WaitingForClient') {
                return 'beingprocessed'
            } else if (status === 'CustomerHasAnActiveAccount') {
                return 'hasactiveaccount'
            } else {
                return 'application'
            }
        }

        let isNullOrWhitespace = (input : any) => {
            if (typeof input === 'undefined' || input == null) return true;

            if ($.type(input) === 'string') {
                return $.trim(input).length < 1;
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

            var i = value.indexOf('@')
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

        if ($scope.mode == 'application') {
            $scope.f = {}
            $scope.hasError = (n) => {
                return $scope.f.applicationform.$submitted && $scope.f.applicationform[n] && $scope.f.applicationform[n].$invalid
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
            } else if (initialDataTyped.trustedSourceLookupCustomer && initialDataTyped.trustedSourceLookupCustomer.contact) {
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
        $scope.apply = (applicationModel: any, evt: Event) => {
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
                    ii.push({ Name: k, Value: v })
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

            var contactInfoLookupResultEncryptionKey = ""
            if (initialDataTyped && initialDataTyped.trustedSourceLookupCustomer) {
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
                var last = localStorage.getItem(SavingsAccountApplicationNs.getTestApplicationStorageKey(isExistingCustomer, isTrustedInfoEditable))
                if (last) {
                    var a = JSON.parse(last)
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
        currentLanguage: () => any
        savingsAccountOverviewUrl: string
        isTest: boolean
        loggedInCivicRegNr: string
        isValidIBAN: (value: any) => boolean
        isValidEmail: (value: any) => boolean
        isValidPhoneNr: (value: any) => boolean
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
        interestRatePercent : number
        existingCustomer: ExistingCustomerModel
        trustedSourceLookupCustomer : any
        translation : any
        customerApplicationStatus: string
        savingsAccountOverviewUrl: string
        cancelUrl: string
        externalApplicationVariables: NTechCustomerPagesApi.ApplicationItem[]
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