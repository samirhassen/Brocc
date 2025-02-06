namespace MortgageLoanApplicationDualSignAgreementComponentNs {
    export class MortgageLoanApplicationDualSignAgreementController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData
        m: Model
        linkDialogId: string

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);

            this.linkDialogId = modalDialogService.generateDialogId()
        }

        componentName(): string {
            return 'mortgageLoanApplicationDualSignAgreement'
        }

        onChanges() {
            this.reload()
        }

        private reload() {
            this.m = null

            if (!this.initialData || !this.initialData.applicationInfo) {
                return
            }

            let ai = this.initialData.applicationInfo

            let wf = this.initialData.workflowModel

            let init = (
                customersWithRoles: MortgageLoanDualCustomerRoleHelperNs.ApplicationCustomerRolesResponse,
                lockedAgreement: NTechPreCreditApi.LockedAgreementModel,
                aia: NTechPreCreditApi.ApplicationInfoWithApplicantsModel,
                docs: NTechPreCreditApi.ApplicationDocument[]
            ) => {
                if (!wf.areAllStepBeforeThisAccepted(ai)) {
                    this.m = null
                    return
                }

                this.apiClient.fetchDualAgreementSignatureStatus(ai.ApplicationNr).then(s => {
                    let i = new ApplicationDocumentsComponentNs.InitialData(ai)
                    i.onDocumentsAddedOrRemoved = (areAllDocumentsAdded) => {
                        this.reload()
                    }
                    i.forceReadonly = wf.isStatusAccepted(ai)
                    this.m = {
                        isApproveAllowed: false,
                        documentsInitialData: i, //Allow attaching manually even before this step has been reached in case they send it out by hand
                        customers: [],
                        isPendingSignatures: s.IsPendingSignatures,
                        currentSignatureLink: null
                    }

                    let isAnyMissingDocuments = false

                    for (let customerId of customersWithRoles.customerIds) {
                        let firstName = customersWithRoles.firstNameAndBirthDateByCustomerId[customerId]['firstName']
                        let birthDate = customersWithRoles.firstNameAndBirthDateByCustomerId[customerId]['birthDate']

                        //---- Document -----
                        let roles = ''
                        for (let roleName of customersWithRoles.rolesByCustomerId[customerId]) {
                            if (roles.length > 0) {
                                roles += ', '
                            }
                            roles += roleName
                        }
                        let title = `${firstName}, ${birthDate} (${roles})`
                        let applicantNr: number = null
                        if (aia.CustomerIdByApplicantNr[1] == customerId) {
                            applicantNr = 1
                        } else if (ai.NrOfApplicants > 1 && aia.CustomerIdByApplicantNr[2] == customerId) {
                            applicantNr = 2
                        }
                        //NOTE: If you add applicantNr here make sure it's also included when the document is saved from the callback or these wont match
                        i.addComplexDocument('SignedAgreement', title, null, customerId, null)

                        let signedAgreementDocument = NTechLinq.first(docs, x => x.CustomerId === customerId)

                        if (!signedAgreementDocument) {
                            isAnyMissingDocuments = true
                        }

                        //--Customer---
                        let archiveKey = lockedAgreement && lockedAgreement.UnsignedAgreementArchiveKeyByCustomerId ? lockedAgreement.UnsignedAgreementArchiveKeyByCustomerId[customerId] : null
                        let unsignedAgreementUrl = archiveKey ? `/CreditManagement/ArchiveDocument?key=${archiveKey}` : null
                        this.m.customers.push({
                            customerId: customerId,
                            firstName: firstName,
                            birthDate: birthDate,
                            roleNames: customersWithRoles.rolesByCustomerId[customerId],
                            unsignedAgreementUrl: unsignedAgreementUrl,
                            signedAgreement: signedAgreementDocument ? { date: signedAgreementDocument.DocumentDate, url: `/CreditManagement/ArchiveDocument?key=${signedAgreementDocument.DocumentArchiveKey}` } : null,
                            signatureToken: s.IsPendingSignatures ? s.SignatureTokenByCustomerId[customerId] : null
                        })
                    }

                    this.m.isApproveAllowed = ai.IsActive && ai.HasLockedAgreement && !isAnyMissingDocuments
                        && wf.areAllStepBeforeThisAccepted(ai)
                        && wf.isStatusInitial(ai)

                    if (this.initialData.isTest) {
                        let tf = this.initialData.testFunctions
                        tf.addFunctionCall(tf.generateUniqueScopeName(), 'Add mock signed agreements', () => {
                            //Small pdf from https://stackoverflow.com/questions/17279712/what-is-the-smallest-possible-valid-pdf
                            let promises = []
                            for (let c of this.m.customers) {
                                promises.push(this.apiClient.addApplicationDocument(this.initialData.applicationInfo.ApplicationNr, 'SignedAgreement', null,
                                    tf.generateTestPdfDataUrl(`Signed agreement on ${this.initialData.applicationInfo.ApplicationNr} for ${c.firstName}, ${c.birthDate}`),
                                    `SignedAgreement${c.customerId}.pdf`, c.customerId, null))
                            }
                            this.$q.all(promises).then(x => {
                                this.signalReloadRequired()
                            })
                        })
                    }
                })
            }

            this.apiClient.getLockedAgreement(ai.ApplicationNr).then(lockedAgreement => {
                this.apiClient.fetchApplicationInfoWithApplicants(ai.ApplicationNr).then(aia => {
                    MortgageLoanDualCustomerRoleHelperNs.getApplicationCustomerRolesByCustomerId(ai.ApplicationNr, this.apiClient).then(x => {
                        this.apiClient.fetchApplicationDocuments(ai.ApplicationNr, ['SignedAgreement']).then(docs => {
                            init(x, lockedAgreement.LockedAgreement, aia, docs)
                        })
                    })
                })
            })
        }

        showSignatureLink(c: CustomerModel, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.m.currentSignatureLink = null
            this.apiClient.getUserModuleUrl('nCustomerPages', '/a/#/token-login', {
                t: `st_${c.signatureToken}`
            }).then(x => {
                this.m.currentSignatureLink = {
                    linkTitle: `Signature link for ${c.firstName}, ${c.birthDate}`,
                    linkUrl: x.UrlExternal
                }
                this.modalDialogService.openDialog(this.linkDialogId)
            })
        }

        approve(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.setMortgageApplicationWorkflowStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.workflowModel.currentStep.Name, 'Accepted', 'Agreements signed').then(y => {
                this.signalReloadRequired()
            })
        }

        glyphIconClassFromBoolean(isAccepted: boolean, isRejected: boolean) {
            return ApplicationStatusBlockComponentNs.getIconClass(isAccepted, isRejected)
        }

        // To avoid onclick as inline-script due to CSP. 
        focusAndSelect(evt: any) {
            evt.currentTarget.focus();
            evt.currentTarget.select();
        }
    }

    export class Model {
        isApproveAllowed: boolean
        documentsInitialData: ApplicationDocumentsComponentNs.InitialData
        customers: CustomerModel[]
        currentSignatureLink: {
            linkUrl: string,
            linkTitle: string
        }
        isPendingSignatures: boolean
    }

    export class CustomerModel {
        customerId: number
        firstName: string
        birthDate: string
        roleNames: string[]
        signatureToken: string
        unsignedAgreementUrl: string
        signedAgreement: {
            date: Date,
            url: string
        }
    }

    export class MortgageLoanApplicationDualSignAgreementCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualSignAgreementController;
            this.template = `<div class="container" ng-if="$ctrl.m">

<div class="row" ng-if="$ctrl.m.customers">
    <table class="table col-sm-12">
        <tbody>
            <tr ng-repeat="c in $ctrl.m.customers">
                <td class="col-xs-5">{{c.firstName}}, {{c.birthDate}} (<span ng-repeat="r in c.roleNames" class="comma">{{r}}</span>)</td>
                <td class="col-xs-1"><span class="glyphicon" ng-class="{{$ctrl.glyphIconClassFromBoolean(!!c.signedAgreement, false)}}"></span></td>
                <td class="col-xs-3 text-right" ng-if="!c.signedAgreement">
                    <a ng-if="c.unsignedAgreementUrl" ng-href="{{c.unsignedAgreementUrl}}" target="_blank" class="n-direct-btn n-purple-btn">UNSIGNED <span class="glyphicon glyphicon-save"></span></a>
                    <span ng-if="!c.unsignedAgreementUrl">Missing</span>
                </td>
                <td class="col-xs-3 text-right" ng-if="!c.signedAgreement">
                    <button ng-if="$ctrl.m.isPendingSignatures" ng-click="$ctrl.showSignatureLink(c, $event)" class="n-direct-btn n-turquoise-btn">Signature link <span class="glyphicon glyphicon-resize-full"></span></button>
               </td>
                <td class="col-xs-6 text-right" colspan="2" ng-if="c.signedAgreement">
                    <spa >Signed {{c.signedAgreement.date | date:'short'}}</span>
                </td>
            </tr>
        </tbody>
    </table>

    <modal-dialog dialog-title="$ctrl.m.currentSignatureLink.linkTitle" dialog-id="$ctrl.linkDialogId">
        <div class="modal-body">
            <textarea ng-if="$ctrl.m.currentSignatureLink" rows="1" style="width:100%;resize: none" ng-click="$ctrl.focusAndSelect($event)" readonly="readonly">{{$ctrl.m.currentSignatureLink.linkUrl}}</textarea>
        </div>
    </modal-dialog>
</div>

<div class="row">
    <application-documents initial-data="$ctrl.m.documentsInitialData">

    </application-documents>
</div>

<div class="row" ng-if="$ctrl.m.isApproveAllowed">
    <div class="text-center pt-3">
        <button class="n-main-btn n-green-btn" ng-click="$ctrl.approve($event)">
            Approve
        </button>
    </div>
</div>

</div>`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationDualSignAgreement', new MortgageLoanApplicationDualSignAgreementComponentNs.MortgageLoanApplicationDualSignAgreementCheckComponent())