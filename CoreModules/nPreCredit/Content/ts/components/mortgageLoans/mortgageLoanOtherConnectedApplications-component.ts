namespace MortgageLoanOtherConnectedApplicationsCompactComponentNs {
    export class MortgageLoanOtherConnectedApplicationsCompactController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageLoanOtherConnectedApplicationsCompactComponentNsCompact'
        }

        onChanges() {
            this.reload()
        }

        private reload() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.loadModel().then(x => this.m = x)
        }

        private loadApplicationData(): ng.IPromise<{
            listCustomers: NTechPreCreditApi.INumberDictionary<string[]>,
            customerItems: NTechPreCreditApi.INumberDictionary<NTechPreCreditApi.IStringDictionary<string>>,
            customerIds : number[]
            ai2:NTechPreCreditApi.ApplicationInfoWithApplicantsModel
        }> {
            let rolesByCustomerId: NTechPreCreditApi.INumberDictionary<string[]> = {}
            return this.apiClient.fetchApplicationInfoWithApplicants(this.initialData.applicationNr).then(ai2 => {
                for (let applicantNr of Object.keys(ai2.CustomerIdByApplicantNr)) {
                    rolesByCustomerId[ai2.CustomerIdByApplicantNr[applicantNr]] = ['Applicant']
                }
                return ComplexApplicationListHelper.getAllCustomerIds(this.initialData.applicationNr, ['ApplicationObject'], this.apiClient, rolesByCustomerId).then(listCustomers => {
                    let customerIds = NTechPreCreditApi.getNumberDictionarKeys(listCustomers)
                    return this.apiClient.fetchCustomerItemsBulk(customerIds, ['firstName', 'birthDate']).then(customerItems => {
                        return { rolesByCustomerId, listCustomers, customerItems, customerIds, ai2 }
                    })
                })
            })
        }

        private loadModel(): ng.IPromise<Model> {
            return this.loadApplicationData().then(a => {
                let listCustomers = a.listCustomers
                let customerItems = a.customerItems
                let customerIds = a.customerIds
                let ai2 = a.ai2

                let firstNameAndBirthDateByCustomerId: NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string, customerId: number }> = {}
                for (let customerId of customerIds) {
                    firstNameAndBirthDateByCustomerId[customerId] = {
                        birthDate: customerItems[customerId]['birthDate'],
                        firstName: customerItems[customerId]['firstName'],
                        customerId: customerId
                    }
                }

                return this.apiClient.fetchotherApplicationsByCustomerId(customerIds, this.initialData.applicationNr, true).then(otherApps => {
                    let doForEachApplicantApplicationPair = (action: (app: NTechPreCreditApi.OtherApplicationsResponseApplicantModel, x: NTechPreCreditApi.OtherApplicationsResponseApplicationModel) => void) => {
                        otherApps.Applicants.forEach(app => {
                            app.Applications.forEach(x => {
                                action(app, x)
                            })
                        })
                    }

                    let applicationsNrsWithDupes: string[] = []
                    doForEachApplicantApplicationPair((_, x) => applicationsNrsWithDupes.push(x.ApplicationNr))

                    return this.apiClient.fetchApplicationInfoBulk(NTechLinq.distinct(applicationsNrsWithDupes)).then(applicationInfoByApplicationNr => {
                        let m : Model
                        m = {
                            customerIds: [],
                            rolesByCustomerId: [],
                            customerIdByApplicantNr: [],
                            firstNameAndBirthDateByCustomerId: [],
                            applicantInfo: null,
                            otherApplications: []
                        }

                        m.customerIds = customerIds;
                        m.rolesByCustomerId = listCustomers;
                        m.customerIdByApplicantNr = ai2.CustomerIdByApplicantNr;
                        m.firstNameAndBirthDateByCustomerId = firstNameAndBirthDateByCustomerId;

                        doForEachApplicantApplicationPair((app, x) => {
                            m.otherApplications.push({
                                "CustomerId": app.CustomerId,
                                "ApplicationDate": moment(applicationInfoByApplicationNr[x.ApplicationNr].ApplicationDate).format("YYYY-MM-DD"),
                                "ApplicationNr": x.ApplicationNr,
                                "IsActive": applicationInfoByApplicationNr[x.ApplicationNr].IsActive,
                                "Status": this.getApplicationStatus(applicationInfoByApplicationNr[x.ApplicationNr])
                            })
                        })

                        return m
                    })
                })
            })
        }

        getApplicationStatus(appInfo) {
            let stat = NTechPreCreditApi.ApplicationStatusItem;
            if (appInfo.IsCancelled)
                return stat.cancelled;
            if (appInfo.IsRejected)
                return stat.rejected;
            if (appInfo.IsFinalDecisionMade)
                return stat.finalDecisionMade;

            return null;
        }

        getToggleBlockText(data) {
            let applicationCount = this.m.otherApplications.filter(x => x["CustomerId"] == data.customerId).length;
            let applicantRole = this.m.rolesByCustomerId[data.customerId];
            let applicantRoleText = applicantRole[0] === "Applicant" ? "Applicant" : "Other";
            let txt = `${applicantRoleText}: ${data.firstName}, ${data.birthDate} (${applicationCount})`;
            return txt;
        }

        isCustomerHasOtherApplications(customerId) {
            return this.m.otherApplications.some(x => x["CustomerId"] === customerId);
        }
    }

    export class Model {
        customerIds: number[]
        rolesByCustomerId: NTechPreCreditApi.INumberDictionary<string[]>
        customerIdByApplicantNr: NTechPreCreditApi.INumberDictionary<number>
        firstNameAndBirthDateByCustomerId: NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string }>
        applicantInfo: string
        otherApplications: NTechPreCreditApi.OtherApplicationsResponseApplicantsInfoModel[]
    }

    export interface InitialData {
        applicationNr: string
    }


    export class MortgageLoanOtherConnectedApplicationsCompactComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanOtherConnectedApplicationsCompactController;
            this.template = `<div ng-if="$ctrl.m">
    <div class="otherApplicationsBlock" ng-repeat="cust in $ctrl.m.firstNameAndBirthDateByCustomerId track by $index">
        <toggle-block header-text="$ctrl.getToggleBlockText(cust)" >
            <table class="table" ng-if="$ctrl.isCustomerHasOtherApplications(cust.customerId)">
                <thead>
                    <tr> 
                        <th>Date</th>
                        <th class="text-right">Status</th>
                    </tr>
                </thead>
                <tbody>
                    <tr ng-repeat="x in $ctrl.m.otherApplications track by $index" ng-if="x.CustomerId === cust.customerId">
                        <td>                          
                            <a class="n-anchor" ng-class="{ 'inactive': x.IsActive !== true }"  target="_blank" ng-href="{{'/Ui/MortgageLoan/Application?applicationNr=' + x.ApplicationNr}}">
                            {{ x.ApplicationDate }} <span class="glyphicon glyphicon-new-window"></span>
                            </a>
                        </td>
                        <td class="text-right"> 
                            {{ x.Status ? x.Status : 'Active' }}
                        </td>
                    </tr>
                </tbody>
            </table>
        </toggle-block>
    </div>
 </div> `
        }
    }
}

angular.module('ntech.components').component('mortgageLoanOtherConnectedApplicationsCompact', new MortgageLoanOtherConnectedApplicationsCompactComponentNs.MortgageLoanOtherConnectedApplicationsCompactComponent())