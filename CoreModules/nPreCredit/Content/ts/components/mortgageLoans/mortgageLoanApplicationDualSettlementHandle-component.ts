namespace MortgageLoanApplicationDualSettlementHandleComponentNs {
    export class MortgageLoanApplicationDualSettlementHandleController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);

            this.ntechComponentService.subscribeToReloadRequired(() => {
                this.reload()
            })

            this.ntechComponentService.subscribeToNTechEvents(x => {
                if (x.eventName === AddRemoveListComponentNs.ChangeEventName && this.m && this.m.initial) {
                    let i = this.m.initial
                    let d = x.customData as AddRemoveListComponentNs.ChangeEvent
                    if (d && (d.eventCorrelationId === i.mainPaymentsInitialData.eventCorrelationId || d.eventCorrelationId === i.childPaymentsInitialData.eventCorrelationId)) {
                        //Could possibly be relaxed to only reload the summary
                        this.signalReloadRequired()
                    }
                }
            })
        }

        componentName(): string {
            return 'mortgageLoanApplicationDualSettlementHandle'
        }

        onChanges() {
            this.reload()
        }

        //First time we get to this step
        private reloadInitial(ai: NTechPreCreditApi.ApplicationInfoModel) {
            this.apiClient.initializeDualMortgageLoanSettlementPayments(ai.ApplicationNr).then(x => { this.signalReloadRequired() })
        }

        //Initial payments suggestions created from loans but no payment file created
        private reloadInitialized(ai: NTechPreCreditApi.ApplicationInfoModel, decision: NTechPreCreditApi.ItemBasedDecisionModel) {
            let createPaymentsInitialData = (headerText: string, listName: string): AddRemoveListComponentNs.InitialData => {
                return {
                    host: this.initialData,
                    ai: ai,
                    isEditAllowed: ai.IsActive && !ai.IsFinalDecisionMade,
                    headerText: headerText,
                    listName: listName,
                    itemNames: ['targetBankName', 'targetAccountIban', 'paymentAmount', 'messageToReceiver', 'paymentReference', 'isExpressPayment'],
                    eventCorrelationId: NTechComponents.generateUniqueId(6),
                    getViewDetailsUrl: null,
                    applicationEditorLabelSize: 3,
                    applicationEditorEnableChangeTracking: false
                }
            }

            let mainExpectedPaymentsAmount = this.parseDecimalOrNull(decision.UniqueItems['mainDirectToCustomerAmount']) 
                + this.parseDecimalOrNull(decision.UniqueItems['mainTotalSettlementAmount']) 
                + this.parseDecimalOrNull(decision.UniqueItems['mainPurchaseAmount'])

            let childExpectedPaymentsAmount = this.parseDecimalOrNull(decision.UniqueItems['childDirectToCustomerAmount']) 
                + this.parseDecimalOrNull(decision.UniqueItems['childTotalSettlementAmount'])

            this.getSummaryPaymentsAmounts().then(payments => {
                this.m = {
                    decision: decision,
                    initial: {
                        mainPaymentsInitialData: createPaymentsInitialData('Outgoing mortgage payments', 'MainSettlementPayments'),
                        childPaymentsInitialData: createPaymentsInitialData('Outgoing other loan payments', 'ChildSettlementPayments'),
                        summary: {
                            mainExpectedPaymentsAmount: mainExpectedPaymentsAmount,
                            mainActualPaymentsAmount: payments.mainPaymentsAmount,
                            mainDiffAmount: mainExpectedPaymentsAmount - payments.mainPaymentsAmount,
                            childExpectedPaymentsAmount: childExpectedPaymentsAmount,
                            childActualPaymentsAmount: payments.childPaymentsAmount,
                            childDiffAmount: childExpectedPaymentsAmount - payments.childPaymentsAmount
                        },
                        isGeneratePaymentsFileAllowed: ai.IsActive && !ai.IsFinalDecisionMade && (payments.mainPaymentsAmount > 0 || payments.childPaymentsAmount > 0)
                    },
                    pendingOrDone: null
                }

                this.setupInitializedTestFunctions()
            })
        }

        //Payment file created but the loan has not been created yet or loan has been created
        private reloadPendingOrDone(ai: NTechPreCreditApi.ApplicationInfoModel, decision: NTechPreCreditApi.ItemBasedDecisionModel, currentStepModel: WorkflowHelper.WorkflowStepModel, outgoingPaymentFileArchiveKey: string, outgoingPaymentFileCreationDate: string, loanCreationDate: string, isPending: boolean) {
            this.getOutgoingPayments().then(x => {
                let mainPaymentsSum = 0
                let childPaymentsSum = 0
                for (let p of x.payments) {
                    if (p.isMain) {
                        mainPaymentsSum += p.paymentAmount
                    } else {
                        childPaymentsSum += p.paymentAmount
                    }
                }
                let mainFeesSum = this.anSum(['mainInitialFeeAmount', 'mainValuationFeeAmount', 'mainDeedFeeAmount', 'mainMortgageApplicationFeeAmount'], decision)
                let childFeesSum = this.anSum(['childInitialFeeAmount'], decision)
                this.m = {
                    decision: decision,
                    initial: null,
                    pendingOrDone: {
                        isCancelAllowed: isPending && ai.IsActive && !ai.IsFinalDecisionMade && currentStepModel.areAllStepsAfterInitial(ai),
                        outgoingPaymentFileUrl: this.apiClient.getArchiveDocumentUrl(outgoingPaymentFileArchiveKey, { useOriginalFileName: true }),
                        payments: x.payments,
                        mainPaymentsSum: mainPaymentsSum,
                        childPaymentsSum: childPaymentsSum,
                        mainFeesSum: mainFeesSum,
                        childFeesSum: childFeesSum,
                        mainTotalSum: mainPaymentsSum + mainFeesSum,
                        childTotalSum: childPaymentsSum + childFeesSum,
                        isCreateLoanAllowed: ai.IsActive && !ai.IsFinalDecisionMade && currentStepModel.areAllStepsAfterInitial(ai),
                        isPending: isPending,
                        loanCreationDate: loanCreationDate,
                        outgoingPaymentFileCreationDate: outgoingPaymentFileCreationDate
                    }
                }
            })
        }

        private reload() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchApplicationInfo(this.initialData.applicationNr).then(ai => {
                let currentStepModel = WorkflowHelper.getStepModelByCustomData(this.initialData.workflowModel, x => x.IsSettlement === 'yes')
                let areAllStepBeforeThisAccepted = currentStepModel.areAllStepBeforeThisAccepted(ai)
                if (!areAllStepBeforeThisAccepted) {
                    return
                }

                let withDefault = (s: string, dv: string) => s === ApplicationDataSourceHelper.MissingItemReplacementValue ? dv : s

                this.apiClient.fetchCreditApplicationItemSimple(this.initialData.applicationNr, ['application.outgoingPaymentFileStatus', 'application.outgoingPaymentFileArchiveKey', 'application.outgoingPaymentFileCreationDate', 'application.loanCreationDate'], ApplicationDataSourceHelper.MissingItemReplacementValue).then(x => {
                    let outgoingPaymentFileStatus = withDefault(x['application.outgoingPaymentFileStatus'], 'initial')

                    if (outgoingPaymentFileStatus == 'initial') {
                        this.reloadInitial(ai)
                        return
                    }

                    this.apiClient.fetchItemBasedCreditDecision({
                        ApplicationNr: this.initialData.applicationNr,
                        MustBeCurrent: true,
                        MustBeAccepted: true,
                        MaxCount: 1
                    }).then(decisions => {
                        let decision = decisions.Decisions[0]
                        if (outgoingPaymentFileStatus == 'initialized') {
                            this.reloadInitialized(ai, decision)
                        } else if (outgoingPaymentFileStatus == 'pending' || outgoingPaymentFileStatus == 'done') {
                            let isPending = outgoingPaymentFileStatus == 'pending'
                            this.reloadPendingOrDone(ai, decision, currentStepModel,
                                withDefault(x['application.outgoingPaymentFileArchiveKey'], null),
                                withDefault(x['application.outgoingPaymentFileCreationDate'], null),
                                withDefault(x['application.loanCreationDate'], null), isPending)
                        }
                    })
                })
            })
        }

        private getSummaryPaymentsAmounts(): ng.IPromise<{ mainPaymentsAmount: number, childPaymentsAmount: number }> {
            return this.getOutgoingPayments().then(x => {
                let mainPaymentsAmount = 0
                let childPaymentsAmount = 0

                for (let p of x.payments) {
                    if (p.isMain) {
                        mainPaymentsAmount += p.paymentAmount
                    } else {
                        childPaymentsAmount += p.paymentAmount
                    }
                }

                return {
                    mainPaymentsAmount: mainPaymentsAmount,
                    childPaymentsAmount: childPaymentsAmount,
                }
            })
        }

        private getOutgoingPayments(): ng.IPromise<{ payments: OutgoingPaymentModel[] }> {
            let requestNames: string[] = []
            for (let n of ['targetBankName', 'targetAccountIban', 'paymentAmount', 'messageToReceiver', 'exists']) {
                for (let ln of ['Main', 'Child']) {
                    requestNames.push(`${ln}SettlementPayments#*#u#${n}`)
                }
            }

            return this.apiClient.fetchComplexApplicationListItemSimple(this.initialData.applicationNr, requestNames, ApplicationDataSourceHelper.MissingItemReplacementValue).then(x => {
                let d: NTechPreCreditApi.IStringDictionary<OutgoingPaymentModel> = {}
                for (let compoundName of Object.keys(x)) {
                    let n = ComplexApplicationListHelper.parseCompoundItemName(compoundName)
                    let isMain = n.listName == 'MainSettlementPayments'
                    let rowNr = parseInt(n.nr)
                    let dkey = `${isMain ? 'm' : 'c'}${rowNr}`
                    if (!d[dkey]) {
                        d[dkey] = new OutgoingPaymentModel(rowNr, isMain)
                    }
                    let p: OutgoingPaymentModel = d[dkey]
                    let value = x[compoundName]
                    if (n.itemName == 'targetBankName') {
                        p.targetBankName = value
                    } else if (n.itemName == 'targetAccountIban') {
                        p.targetAccountIban = value
                    } else if (n.itemName == 'paymentAmount') {
                        p.paymentAmount = this.parseDecimalOrNull(value)
                    } else if (n.itemName == 'messageToReceiver') {
                        p.messageToReceiver = value
                    }
                }
                let payments: OutgoingPaymentModel[] = []
                for (let k of Object.keys(d)) {
                    payments.push(d[k])
                }

                return { payments }
            })
        }

        a(name: string, decision?: NTechPreCreditApi.ItemBasedDecisionModel) {
            let d = decision
            if (!d) {
                if (!this.m || !this.m.decision || !this.m.decision.IsAccepted) {
                    return null
                }
                d = this.m.decision
            }

            return d.UniqueItems[name]
        }

        an(name: string, decision?: NTechPreCreditApi.ItemBasedDecisionModel) {
            return this.parseDecimalOrNull(this.a(name, decision))
        }

        anSum(names: string[], decision?: NTechPreCreditApi.ItemBasedDecisionModel) {
            let sum = 0
            for (let n of names) {
                sum += this.an(n, decision)
            }
            return sum
        }

        generatePaymentsFile(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.apiClient.createDualMortgageLoanSettlementPaymentsFile(this.initialData.applicationNr).then(x => {
                this.signalReloadRequired()
            })
        }

        cancelPending(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.setApplicationEditItemDataBatched(this.initialData.applicationNr, [{
                dataSourceName: 'CreditApplicationItem',
                isDelete: true,
                itemName: 'application.outgoingPaymentFileArchiveKey',
                newValue: null
            }, {
                dataSourceName: 'CreditApplicationItem',
                isDelete: true,
                itemName: 'application.outgoingPaymentFileCreationDate',
                newValue: null
            },
            {
                dataSourceName: 'CreditApplicationItem',
                isDelete: false,
                itemName: 'application.outgoingPaymentFileStatus',
                newValue: 'initialized'
            }]).then(x => {
                this.signalReloadRequired()
            })
        }

        createNewLoans(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.createDualMortgageLoan(this.initialData.applicationNr).then(x => {
                this.signalReloadRequired()
            })
        }

        private setupInitializedTestFunctions() {
            if (!this.initialData || !this.initialData.isTest || !this.initialData.testFunctions) {
                return
            }

            let tf = this.initialData.testFunctions
            let scope = tf.generateUniqueScopeName()
            tf.addFunctionCall(scope, 'Fill IBAN, BankName and Message', () => {
                if (!this.m || !this.m.initial) {
                    toastr.warning('Must be initial')
                    return
                }
                this.getOutgoingPayments().then(x => {
                    const RandomIban = 'FI2140538661913452' //Randomly generated iban
                    let edits: { dataSourceName: string, itemName: string, newValue: string, isDelete: boolean }[] = []
                    let getName = (p: OutgoingPaymentModel, itemName: string) =>
                        ComplexApplicationListHelper.getDataSourceItemName(p.isMain ? 'MainSettlementPayments' : 'ChildSettlementPayments', p.rowNr.toString(), itemName, ComplexApplicationListHelper.RepeatableCode.No)

                    for (let p of x.payments) {
                        if (!p.targetAccountIban) {
                            edits.push({ dataSourceName: ComplexApplicationListHelper.DataSourceName, itemName: getName(p, 'targetAccountIban'), newValue: RandomIban, isDelete: false })
                        }
                        if (!p.targetBankName) {
                            edits.push({ dataSourceName: ComplexApplicationListHelper.DataSourceName, itemName: getName(p, 'targetBankName'), newValue: `Bank ${p.isMain ? 'M' : 'C'}${p.rowNr}`, isDelete: false })
                        }
                        if (!p.messageToReceiver) {
                            edits.push({ dataSourceName: ComplexApplicationListHelper.DataSourceName, itemName: getName(p, 'messageToReceiver'), newValue: `Message ${p.isMain ? 'M' : 'C'}${p.rowNr}`, isDelete: false })
                        }
                        if (edits.length === 0) {
                            return
                        }
                        this.initialData.apiClient.setApplicationEditItemDataBatched(this.initialData.applicationNr, edits).then(x => {
                            this.signalReloadRequired()
                        })
                    }
                })
            })
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let context = {
                applicationNr: this.initialData.applicationNr
            }
            let t = NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication, context)
            NavigationTargetHelper.handleBack(t, this.apiClient, this.$q, context)
        }
    }

    export class Model {
        decision: NTechPreCreditApi.ItemBasedDecisionModel
        initial: InitialModel
        pendingOrDone: PendingOrDoneModel
    }

    export class PendingOrDoneModel {
        isCancelAllowed: boolean
        isPending: boolean
        outgoingPaymentFileUrl: string
        payments: OutgoingPaymentModel[]
        mainPaymentsSum: number
        childPaymentsSum: number
        mainFeesSum: number
        childFeesSum: number
        mainTotalSum: number
        childTotalSum: number
        isCreateLoanAllowed: boolean
        isConfirmChecked?: boolean
        outgoingPaymentFileCreationDate: string
        loanCreationDate: string
    }

    export class InitialModel {
        mainPaymentsInitialData: AddRemoveListComponentNs.InitialData
        childPaymentsInitialData: AddRemoveListComponentNs.InitialData
        summary: {
            mainExpectedPaymentsAmount: number
            mainActualPaymentsAmount: number
            mainDiffAmount: number
            childExpectedPaymentsAmount: number
            childActualPaymentsAmount: number
            childDiffAmount: number
        }
        isGeneratePaymentsFileAllowed: boolean
    }

    export class OutgoingPaymentModel {
        constructor(public rowNr: number, public isMain: boolean) {
        }

        targetBankName: string
        targetAccountIban: string
        paymentAmount: number
        messageToReceiver: string
    }

    export interface LocalInitialData {
        applicationNr: string
        workflowModel: WorkflowHelper.WorkflowServerModel
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {
    }

    export class MortgageLoanApplicationDualSettlementHandleComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualSettlementHandleController;
            this.template = `<div ng-if="$ctrl.m">

    <div class="pt-1 pb-2">
        <div class="pull-left"><a class="n-back" ng-click="$ctrl.onBack($event)" href="#"><span class="glyphicon glyphicon-arrow-left"></span></a></div>
        <h1 class="adjusted">Handle payments and settlement</h1>
    </div>

    ${this.initialTemplate()}
    ${this.pendingTemplate()}
</div>`
        }

        private initialDecisionTemplate = `<div>
                <h3 class="text-center">Mortgage loan</h3>
                <hr class="hr-section">
                <div class="pb-2">
                    <div class="form-horizontal">
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Initial fee</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('mainInitialFeeAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Valuation fee</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('mainValuationFeeAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Deed fee</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('mainDeedFeeAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Mortgage app. fee</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('mainMortgageApplicationFeeAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Purchase amount</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('mainPurchaseAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Payment to customer</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('mainDirectToCustomerAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Settlement amount</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('mainTotalSettlementAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Total amount</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('mainLoanAmount') | currency}}</div>
                        </div>
                    </div>
                </div>
                <h3 class="text-center">Loan with collateral</h3>
                <hr class="hr-section">
                <div>
                    <div class="form-horizontal">
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Initial fee</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('childInitialFeeAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Payment to customer</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('childDirectToCustomerAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Settlement amount</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('childTotalSettlementAmount') | currency}}</div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Total amount</label>
                            <div class="col-xs-6 form-control-static">{{$ctrl.an('childLoanAmount') | currency}}</div>
                        </div>
                    </div>
                </div>
            </div>`

        private initialTemplate(): string {
            let paymentSummaryTemplate = `<div class="pb-1">
                                    <table class="table">
                                        <thead>
                                            <tr>
                                                <th class="col-xs-2">&nbsp;</th>
                                                <th class="col-xs-3 text-right">Amount</th>
                                                <th class="col-xs-4 text-right">Outgoing payments</th>
                                                <th class="col-xs-3 text-right">Diff</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td>Total mortgage</td>
                                                <td class="text-right">{{$ctrl.m.initial.summary.mainExpectedPaymentsAmount | number:'2'}}</td>
                                                <td class="text-right">{{$ctrl.m.initial.summary.mainActualPaymentsAmount | number:'2'}}</td>
                                                <td class="text-right">{{$ctrl.m.initial.summary.mainDiffAmount | number:'2'}}</th>
                                            </tr>
                                            <tr>
                                                <td>Total loan with collateral</td>
                                                <td class="text-right">{{$ctrl.m.initial.summary.childExpectedPaymentsAmount | number:'2'}}</td>
                                                <td class="text-right">{{$ctrl.m.initial.summary.childActualPaymentsAmount | number:'2'}}</td>
                                                <td class="text-right">{{$ctrl.m.initial.summary.childDiffAmount | number:'2'}}</th>
                                            </tr>
                                        </tbody>
                                    </table></div>`

            let paymentsTemplate = `<div class="pb-1">
        <add-remove-list initial-data="$ctrl.m.initial.mainPaymentsInitialData"></add-remove-list>
    </div>
    <div>
        <add-remove-list initial-data="$ctrl.m.initial.childPaymentsInitialData"></add-remove-list>
    </div>`

            return `<div class="row pt-1 pb-2" ng-if="$ctrl.m.initial">
        <div class="col-sm-4">${this.initialDecisionTemplate}</div>
        <div class="col-sm-8">
            <div class="editblock">
                ${paymentSummaryTemplate}
                ${paymentsTemplate}
                <div class="pt-2 text-center" ng-if="$ctrl.m.initial.isGeneratePaymentsFileAllowed">
                    <a class="n-main-btn n-blue-btn" ng-click="$ctrl.generatePaymentsFile($event)">
                        Preview outgoing payments file
                    </a>
                </div>
            </div>
        </div>
    </div>`
        }

        private pendingTemplate(): string {
            return `<div class="row pt-1 pb-2" ng-if="$ctrl.m.pendingOrDone">
                <div class="col-sm-4">${this.initialDecisionTemplate}</div>
                <div class="col-sm-8 frame" style="background-color: rgb(250, 250, 250)!important">
                    <div class="pb-1 text-right" ng-if="$ctrl.m.pendingOrDone.isCancelAllowed">
                        <button class="n-main-btn n-white-btn" ng-click="$ctrl.cancelPending($event)">
                            Cancel
                        </button>
                    </div>

                    <div ng-repeat="isMain in [true, false]" class="{{isMain ? 'pt-1': 'pt-3'}}">
                        <h2>{{isMain ? 'Mortgage loan' : 'Loan with collateral'}}</h2>
                        <toggle-block header-text="'Outgoing payments'" floated-header-text="(isMain ? $ctrl.m.pendingOrDone.mainPaymentsSum : $ctrl.m.pendingOrDone.childPaymentsSum) | number:'2'">
                            <table class="table">
                                <thead><tr>
                                    <th>Bank</th>
                                    <th>IBAN</th>
                                    <th class="text-right">Amount</th>
                                </tr></thead>
                                <tbody><tr ng-repeat="p in $ctrl.m.pendingOrDone.payments | filter: { 'isMain': isMain }">
                                    <td>{{p.targetBankName}}</td>
                                    <td>{{p.targetAccountIban}}</td>
                                    <td class="text-right">{{p.paymentAmount | number:'2'}}</td>
                                </tr></tbody>
                            </table>
                        </toggle-block>

                        <toggle-block header-text="'Fees'" floated-header-text="(isMain ? $ctrl.m.pendingOrDone.mainFeesSum : $ctrl.m.pendingOrDone.childFeesSum) | number : '2'">
                            <table class="table">
                                <thead><tr>
                                    <th>Type</th>
                                    <th class="text-right">Amount</th>
                                </tr></thead>
                                <tbody>
                                    <tr ng-if="isMain">
                                        <td>Initial fee</td>
                                        <td class="text-right">{{$ctrl.an('mainInitialFeeAmount') | number:'2'}}</td>
                                    </tr>
                                    <tr ng-if="isMain">
                                        <td>Valuation fee</td>
                                        <td class="text-right">{{$ctrl.an('mainValuationFeeAmount') | number:'2'}}</td>
                                    </tr>
                                    <tr ng-if="isMain">
                                        <td>Deed fee</td>
                                        <td class="text-right">{{$ctrl.an('mainDeedFeeAmount') | number:'2'}}</td>
                                    </tr>
                                    <tr ng-if="isMain">
                                        <td>Mortgage app. fee</td>
                                        <td class="text-right">{{$ctrl.an('mainMortgageApplicationFeeAmount') | number:'2'}}</td>
                                    </tr>
                                    <tr ng-if="!isMain">
                                        <td>Initial fee</td>
                                        <td class="text-right">{{$ctrl.an('childInitialFeeAmount') | number:'2'}}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </toggle-block>
                        <div class="block">
                            <h2>&nbsp;<span class="pull-right"><b>{{(isMain ? $ctrl.m.pendingOrDone.mainTotalSum : $ctrl.m.pendingOrDone.childTotalSum) | number: '2'}}</b></span></h2>
                        </div>
                    </div>

                    <div class="pt-3 text-center" ng-if="$ctrl.m.pendingOrDone.isPending && $ctrl.m.pendingOrDone.outgoingPaymentFileUrl">
                        <a class="n-main-btn n-purple-btn" ng-href="{{$ctrl.m.pendingOrDone.outgoingPaymentFileUrl}}" target="_blank">
                            Download payment file <span class="glyphicon glyphicon-arrow-down"></span>
                        </a>
                    </div>

                    <div class="pt-3" ng-if="$ctrl.m.pendingOrDone.isCreateLoanAllowed">
                        <div class="form-horizontal">
                            <p class="text-center pt-3">Outgoing payment is delivered and confirmed?</p>
                            <div class="form-group">
                                <label class="col-xs-6 text-right">Yes</label>
                                <div class="col-xs-4"><input ng-model="$ctrl.m.pendingOrDone.isConfirmChecked" type="checkbox"></div>
                            </div>
                        </div>
                        <div class="text-center pt-3">
                            <button class="n-main-btn n-green-btn" ng-disabled="!$ctrl.m.pendingOrDone.isConfirmChecked" ng-click="$ctrl.createNewLoans($event)">Create loans</button>
                        </div>
                    </div>

                    <div class="pt-3" ng-if="!$ctrl.m.pendingOrDone.isPending">
                        <div class="form-horizontal">
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Outgoing payment file creation date</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.m.pendingOrDone.outgoingPaymentFileCreationDate | date:'short'}}</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Loan creation date</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.m.pendingOrDone.loanCreationDate | date:'short' }}</div>
                            </div>
                        </div>
                    </div>

                </div>
</div>
`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationDualSettlementHandle', new MortgageLoanApplicationDualSettlementHandleComponentNs.MortgageLoanApplicationDualSettlementHandleComponent())