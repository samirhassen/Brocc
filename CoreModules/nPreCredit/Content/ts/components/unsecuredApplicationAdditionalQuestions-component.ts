namespace UnsecuredApplicationAdditionalQuestionsComponentNs {

    export class UnsecuredApplicationAdditionalQuestionsController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', '$scope', 'ntechComponentService']
        constructor(private $http: ng.IHttpService,
            $q: ng.IQService,
            $scope: ng.IScope,
            public ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

            this.agreementSigned1FileUpload = new NtechAngularFileUpload.FileUploadHelper((<HTMLInputElement>document.getElementById('signedagreementfile1')),
                (<HTMLFormElement>document.getElementById('signedagreementfileform1')),
                $scope, $q);
            this.agreementSigned2FileUpload = new NtechAngularFileUpload.FileUploadHelper((<HTMLInputElement>document.getElementById('signedagreementfile2')),
                (<HTMLFormElement>document.getElementById('signedagreementfileform2')),
                $scope, $q);

            for (let applicantNr of [1, 2]) {
                this.agreementSignedFileUpload(applicantNr).addFileAttachedListener(filenames => {
                    if (!this.m) {
                        return
                    }
                    if (filenames.length == 0) {
                        this.m.agreementSigningStatus['applicant' + applicantNr].attachedFileName = null;
                    } else if (filenames.length == 1) {
                        this.m.agreementSigningStatus['applicant' + applicantNr].attachedFileName = filenames[0];
                    } else {
                        this.m.agreementSigningStatus['applicant' + applicantNr].attachedFileName = 'Error - multiple files selected!'
                    }
                });
            }
        }

        initialData: InitialData
        m: Model
        agreementSigned1FileUpload :NtechAngularFileUpload.FileUploadHelper
        agreementSigned2FileUpload :NtechAngularFileUpload.FileUploadHelper

        componentName(): string {
            return 'unsecuredApplicationAdditionalQuestions'
        }

        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }
            this.apiClient.fetchProviderInfo(this.initialData.applicationInfo.ProviderName).then(providerInfo => {
                this.apiClient.fetchUnsecuredLoanAdditionalQuestionsStatus(this.initialData.applicationInfo.ApplicationNr).then(x => {
                    this.m = {
                        provider: providerInfo,
                        agreementSigningStatus: x.AgreementSigningStatus,
                        additionalQuestionsStatus: x.AdditionalQuestionsStatus,
                        showMoreAgreementSigningOptions: false, //todo
                        userDirectLinkUrl: null,
                        consentAnswers: null
                    }
                })        
            })
        }

       

        agreementSignedFileUpload(applicantNr: number): NtechAngularFileUpload.FileUploadHelper {
            if (applicantNr === 1) {
                return this.agreementSigned1FileUpload
            } else if (applicantNr === 2) {
                return this.agreementSigned2FileUpload;
            } else {
                return null;
            }
        }

        headerClassFromStatus(status: string) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'

            return { 'text-success': isAccepted, 'text-danger': isRejected }
        }

        iconClassFromStatus(status: string) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'
            var isOther = !isAccepted && !isRejected
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected }
        }

        selectAttachedSignedAgreement(applicantNr, evt) {
            if (evt) {
                evt.preventDefault()
            }
            this.agreementSignedFileUpload(applicantNr).showFilePicker();
        }

        acceptAttachedSignedAgreement(applicantNr, evt) {
            if (evt) {
                evt.preventDefault()
            }

            this.agreementSignedFileUpload(applicantNr).loadSingleAttachedFileAsDataUrl().then(result => {
                this.$http({
                    method: 'POST',
                    url: '/CreditManagement/AddSignedAgreementDocument',
                    data: {
                        applicationNr: this.initialData.applicationInfo.ApplicationNr,
                        applicantNr: applicantNr,
                        attachedFileAsDataUrl: result.dataUrl,
                        attachedFileName: result.filename
                    }
                }).then((response: ng.IHttpResponse<IAttachSignedAgreementFileResult>) => {
                    this.m.agreementSigningStatus['applicant' + applicantNr].attachedFileName = null
                    var updatedAgreementSigningStatus = response.data.updatedAgreementSigningStatus
                    if (response.data.wasAgreementStatusChanged || response.data.wasCustomerCheckStatusChanged) {
                        //Could be made cleaner by updating all the sections that changed
                        location.reload()
                    } else if (updatedAgreementSigningStatus != null) {
                        this.m.agreementSigningStatus['applicant' + applicantNr] = updatedAgreementSigningStatus['applicant' + applicantNr]
                    }
                }, (response) => {
                    toastr.error(response.statusText, "Error");
                })
            }, err => {
                toastr.warning(err);
            })
        }

        cancelAttachedSignedAgreement(applicantNr, evt) {
            if (evt) {
                evt.preventDefault()
            }
            var s = this.m.agreementSigningStatus['applicant' + applicantNr]
            if (s) {
                s.attachedFileName = null
            }
            this.agreementSignedFileUpload(applicantNr).reset();
        }

        showDirectLink(evt) {
            this.$http({
                method: 'POST',
                url: '/CreditManagement/GetOrCreateApplicationWrapperLink',
                data: { applicationNr: initialData.ApplicationNr }
            }).then((response: ng.IHttpResponse<IGetOrCreateApplicationWrapperLinkResult>) => {
                this.m.userDirectLinkUrl = response.data.wrapperLink;
                (<any>$('#userDirectLinkDialog')).modal('show')
            }, (response) => {
            })
        }


        openConsentAnswersDialog() {
            this.apiClient.fetchConsentAnswers(this.initialData.applicationInfo.ApplicationNr).then(x => {
                if (x.ConsentItems.length < 1) {
                    this.m.consentAnswers = "No consent items.";
                    (<any>$('#consentAnswersDialog')).modal('show');
                    return;
                }

                let objArr = [];
                x.ConsentItems.forEach(x => {
                    objArr.push({
                        [x.GroupName]: JSON.parse(x.Item)
                    })
                })
                this.m.consentAnswers = JSON.stringify(objArr, null, 2);
                (<any>$('#consentAnswersDialog')).modal('show')
            })
        }

        canResetSigning() {
            if (!this.m || !this.m.agreementSigningStatus || !this.m.agreementSigningStatus.applicant1) {
                return false
            }
            if (!this.initialData.applicationInfo.IsActive) {
                return false
            }

            let s = this.m.agreementSigningStatus
            let statuses = [s.applicant1.status]
            if (s.applicant2) {
                statuses.push(s.applicant2.status)
            }
            for (let status of statuses) {
                if (status !== 'Success' && status !== 'NotSent') {
                    return true
                }
            }
            return false
        }

        resetSigning(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let applicationNr = this.initialData.applicationInfo.ApplicationNr
            this.apiClient.cancelUnsecuredLoanApplicationSignatureSession(applicationNr).then(_ => {
                this.signalReloadRequired()
            })
        }

        // To avoid onclick as inline-script due to CSP. 
        focusAndSelect(evt: any) {
            evt.currentTarget.focus();
            evt.currentTarget.select();
        }
    }

    export class UnsecuredApplicationAdditionalQuestionsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = UnsecuredApplicationAdditionalQuestionsController;
            this.templateUrl = 'unsecured-application-additional-questions.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
    }

    export class Model {
        provider: NTechPreCreditApi.ProviderInfoModel
        additionalQuestionsStatus: NTechPreCreditApi.UnsecuredLoanAdditionalQuestionsStatusModel
        agreementSigningStatus: NTechPreCreditApi.UnsecuredLoanAgreementSigningStatusModel
        showMoreAgreementSigningOptions: boolean
        userDirectLinkUrl: string
        consentAnswers: string
    }

    export interface IGetOrCreateApplicationWrapperLinkResult {
        wrapperLink: string
    }

    export interface ISendAdditionalQuestionsResult {
        failedMessage: string,
        newComment: any
    }

    export interface ISendAgreementSigningLinkResult {
        failedMessage: string,
        newComment: any,
        AgreementSigningStatus: any,
        AgreementStatus: string,
        forceReload: boolean
    }

    export interface IAttachSignedAgreementFileResult {
        updatedAgreementSigningStatus: string,
        wasCustomerCheckStatusChanged: boolean,
        wasAgreementStatusChanged: boolean
    }
}

angular.module('ntech.components').component('unsecuredApplicationAdditionalQuestions', new UnsecuredApplicationAdditionalQuestionsComponentNs.UnsecuredApplicationAdditionalQuestionsComponent())