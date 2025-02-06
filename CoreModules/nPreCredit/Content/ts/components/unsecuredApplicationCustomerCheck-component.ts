namespace UnsecuredApplicationCustomerCheckComponentNs {
    export class UnsecuredApplicationCustomerCheckController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'unsecuredApplicationCustomerCheck'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let crossModuleNavigationTargetToHere = NavigationTargetHelper.createCrossModule(
                'UnsecuredLoanApplication', 
                { applicationNr: this.initialData.applicationNr })

            this.apiClient.fetchApplicationInfoWithApplicants(this.initialData.applicationNr).then(x => {
                let m: Model = {
                    IsKycScreenAllowed: x.Info.IsActive,
                    Applicants: []
                }
                for (var applicantNr = 1; applicantNr <= x.Info.NrOfApplicants; applicantNr++) {
                    m.Applicants.push({
                        ApplicantNr: applicantNr,
                        CustomerId: x.CustomerIdByApplicantNr[applicantNr],
                        PepKyc: null,
                        Fatca: null,
                        Name: null,
                        Email: null,
                        Address: null,
                        PepSanctionState: null
                    })
                    this.m = m;
                }

                for (let a of m.Applicants) {
                    this.apiClient.fetchCustomerKycScreenStatus(a.CustomerId).then(x => {
                        a.PepKyc = {
                            LatestScreeningDate: x.LatestScreeningDate
                        }
                    })
                    this.apiClient.fetchCustomerComponentInitialData(this.initialData.applicationNr, a.ApplicantNr, crossModuleNavigationTargetToHere.targetCode).then(x => {
                        a.Fatca = {
                            IncludeInFatcaExport: x.includeInFatcaExport,
                            CustomerFatcaCrsUrl: x.customerFatcaCrsUrl
                        }
                        a.Name = {
                            IsMissingName: !x.firstName,
                            CustomerCardUrl: x.customerCardUrl
                        }
                        a.Address = {
                            IsMissingAddress: x.isMissingAddress,
                            CustomerCardUrl: x.customerCardUrl
                        }
                        a.Email = {
                            IsMissingEmail: x.isMissingEmail,
                            CustomerCardUrl: x.customerCardUrl
                        }
                        a.PepSanctionState = {
                            PepKycCustomerUrl: x.pepKycCustomerUrl,
                            IsAccepted: (x.localIsPep === true || x.localIsPep === false) && x.localIsSanction === false,
                            IsRejected: x.localIsSanction === true
                        }
                    })
                }
            })
        }

        iconClass(isAccepted: boolean, isRejected: boolean) {
            if (isAccepted) {
                return 'glyphicon glyphicon-ok text-success custom-glyph'
            } else if (isRejected) {
                return 'glyphicon glyphicon-remove text-danger custom-glyph'
            } else {
                return 'glyphicon glyphicon-minus custom-glyph'
            }
        }

        iconClassEmailAddressName(a: ApplicantModel) {
            let isAccepted = true
            if (a.Email && a.Email.IsMissingEmail) {
                isAccepted = false
            }
            if (a.Address && a.Address.IsMissingAddress) {
                isAccepted = false
            }
            if (a.Name && a.Name.IsMissingName) {
                isAccepted = false
            }
            return this.iconClass(isAccepted, false)
        }

        kycScreenNow(applicant: ApplicantModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.kycScreenCustomer(applicant.CustomerId, false).then(x => {
                if (!x.Success) {
                    toastr.warning('Screening failed: ' + x.FailureCode)
                } else if (x.Skipped) {
                    toastr.info('Customer has already been screened')
                } else {
                    this.signalReloadRequired()
                }
            })
        }
    }

    export class UnsecuredApplicationCustomerCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = UnsecuredApplicationCustomerCheckController;
            this.templateUrl = 'unsecured-application-customer-check.html';
        }
    }

    export class InitialData {
        applicationNr: string
    }

    export class Model {
        IsKycScreenAllowed: boolean
        Applicants: ApplicantModel[]
    }

    export class ApplicantModel {
        ApplicantNr: number
        CustomerId: number
        PepKyc: PepKycScreenApplicantModel
        Fatca: FatcaModel
        Name: NameModel
        Email: EmailModel
        Address: AddressModel
        PepSanctionState: {
            PepKycCustomerUrl: string
            IsAccepted: boolean
            IsRejected: boolean
        }
    }

    export class PepKycScreenApplicantModel {
        LatestScreeningDate: NTechDates.DateOnly
    }

    export class FatcaModel {
        IncludeInFatcaExport: boolean
        CustomerFatcaCrsUrl: string
    }

    export class NameModel {
        IsMissingName: boolean
        CustomerCardUrl: string
    }

    export class AddressModel {
        IsMissingAddress: boolean
        CustomerCardUrl: string
    }

    export class EmailModel {
        IsMissingEmail: boolean
        CustomerCardUrl: string
    }
}

angular.module('ntech.components').component('unsecuredApplicationCustomerCheck', new UnsecuredApplicationCustomerCheckComponentNs.UnsecuredApplicationCustomerCheckComponent())