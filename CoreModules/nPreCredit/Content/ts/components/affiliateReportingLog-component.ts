namespace AffiliateReportingLogComponentNs {

    export class AffiliateReportingLogController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model
        dialogId: string


        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$timeout']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService,
            private $timeout: ng.ITimeoutService) {
            super(ntechComponentService, $http, $q);
            this.dialogId = modalDialogService.generateDialogId()
        }

        componentName(): string {
            return 'affiliateReportingLog'
        }

        onChanges() {
            if (this.m != null) {
                this.modalDialogService.closeDialog(this.dialogId)
            }
            this.m = {
                hasIntegration: null
            }
        }

        refresh() {
            let parseJson = (s: string) => {
                try {
                    return JSON.parse(s)
                } catch (ex) {
                    return null
                }
            }
            this.apiClient.fetchAllAffiliateReportingEventsForApplication(this.initialData.applicationNr, true).then(x => {
                this.m.hasIntegration = x.AffiliateMetadata.HasDispatcher
                if (this.m.hasIntegration) {
                    this.m.events = x.Events
                    for (let e of this.m.events) {
                        e.EventDataJson = parseJson(e.EventData)
                        for (let i of e.Items) {
                            (i as ExtendedItemModel).OutgoingRequestBodyJson = parseJson(i.OutgoingRequestBody);
                            (i as ExtendedItemModel).OutgoingResponseBodyJson = parseJson(i.OutgoingResponseBody);
                        }
                    }
                } else {
                    this.m.events = null
                }
                this.modalDialogService.openDialog(this.dialogId)
            })
        }

        showLog(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.refresh()
        }

        resendEvent(e: NTechPreCreditApi.AffiliateReportingEventModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.resendAffiliateReportingEvent(e.Id).then(() => {
                e.ProcessedStatus = 'Pending'
                e.ProcessedDate = null
            })
        }
    }

    export class AffiliateReportingLogComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = AffiliateReportingLogController;
            this.templateUrl = 'affiliate-reporting-log.html';
        }
    }

    export class InitialData {
        applicationNr: string
    }

    export class Model {
        hasIntegration: boolean
        events?: ExtendedEventModel[]
    }

    export interface ExtendedEventModel extends NTechPreCreditApi.AffiliateReportingEventModel {
        EventDataJson?: any
    }

    export interface ExtendedItemModel extends NTechPreCreditApi.AffiliateReportingEventItemModel {
        OutgoingRequestBodyJson?: any
        OutgoingResponseBodyJson?: any
    }
}

angular.module('ntech.components').component('affiliateReportingLog', new AffiliateReportingLogComponentNs.AffiliateReportingLogComponent())