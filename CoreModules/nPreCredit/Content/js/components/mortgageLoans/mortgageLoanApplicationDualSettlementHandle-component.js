var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var MortgageLoanApplicationDualSettlementHandleComponentNs;
(function (MortgageLoanApplicationDualSettlementHandleComponentNs) {
    var MortgageLoanApplicationDualSettlementHandleController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationDualSettlementHandleController, _super);
        function MortgageLoanApplicationDualSettlementHandleController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            _this.ntechComponentService.subscribeToReloadRequired(function () {
                _this.reload();
            });
            _this.ntechComponentService.subscribeToNTechEvents(function (x) {
                if (x.eventName === AddRemoveListComponentNs.ChangeEventName && _this.m && _this.m.initial) {
                    var i = _this.m.initial;
                    var d = x.customData;
                    if (d && (d.eventCorrelationId === i.mainPaymentsInitialData.eventCorrelationId || d.eventCorrelationId === i.childPaymentsInitialData.eventCorrelationId)) {
                        //Could possibly be relaxed to only reload the summary
                        _this.signalReloadRequired();
                    }
                }
            });
            return _this;
        }
        MortgageLoanApplicationDualSettlementHandleController.prototype.componentName = function () {
            return 'mortgageLoanApplicationDualSettlementHandle';
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.onChanges = function () {
            this.reload();
        };
        //First time we get to this step
        MortgageLoanApplicationDualSettlementHandleController.prototype.reloadInitial = function (ai) {
            var _this = this;
            this.apiClient.initializeDualMortgageLoanSettlementPayments(ai.ApplicationNr).then(function (x) { _this.signalReloadRequired(); });
        };
        //Initial payments suggestions created from loans but no payment file created
        MortgageLoanApplicationDualSettlementHandleController.prototype.reloadInitialized = function (ai, decision) {
            var _this = this;
            var createPaymentsInitialData = function (headerText, listName) {
                return {
                    host: _this.initialData,
                    ai: ai,
                    isEditAllowed: ai.IsActive && !ai.IsFinalDecisionMade,
                    headerText: headerText,
                    listName: listName,
                    itemNames: ['targetBankName', 'targetAccountIban', 'paymentAmount', 'messageToReceiver', 'paymentReference', 'isExpressPayment'],
                    eventCorrelationId: NTechComponents.generateUniqueId(6),
                    getViewDetailsUrl: null,
                    applicationEditorLabelSize: 3,
                    applicationEditorEnableChangeTracking: false
                };
            };
            var mainExpectedPaymentsAmount = this.parseDecimalOrNull(decision.UniqueItems['mainDirectToCustomerAmount'])
                + this.parseDecimalOrNull(decision.UniqueItems['mainTotalSettlementAmount'])
                + this.parseDecimalOrNull(decision.UniqueItems['mainPurchaseAmount']);
            var childExpectedPaymentsAmount = this.parseDecimalOrNull(decision.UniqueItems['childDirectToCustomerAmount'])
                + this.parseDecimalOrNull(decision.UniqueItems['childTotalSettlementAmount']);
            this.getSummaryPaymentsAmounts().then(function (payments) {
                _this.m = {
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
                };
                _this.setupInitializedTestFunctions();
            });
        };
        //Payment file created but the loan has not been created yet or loan has been created
        MortgageLoanApplicationDualSettlementHandleController.prototype.reloadPendingOrDone = function (ai, decision, currentStepModel, outgoingPaymentFileArchiveKey, outgoingPaymentFileCreationDate, loanCreationDate, isPending) {
            var _this = this;
            this.getOutgoingPayments().then(function (x) {
                var mainPaymentsSum = 0;
                var childPaymentsSum = 0;
                for (var _i = 0, _a = x.payments; _i < _a.length; _i++) {
                    var p = _a[_i];
                    if (p.isMain) {
                        mainPaymentsSum += p.paymentAmount;
                    }
                    else {
                        childPaymentsSum += p.paymentAmount;
                    }
                }
                var mainFeesSum = _this.anSum(['mainInitialFeeAmount', 'mainValuationFeeAmount', 'mainDeedFeeAmount', 'mainMortgageApplicationFeeAmount'], decision);
                var childFeesSum = _this.anSum(['childInitialFeeAmount'], decision);
                _this.m = {
                    decision: decision,
                    initial: null,
                    pendingOrDone: {
                        isCancelAllowed: isPending && ai.IsActive && !ai.IsFinalDecisionMade && currentStepModel.areAllStepsAfterInitial(ai),
                        outgoingPaymentFileUrl: _this.apiClient.getArchiveDocumentUrl(outgoingPaymentFileArchiveKey, { useOriginalFileName: true }),
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
                };
            });
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.reload = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchApplicationInfo(this.initialData.applicationNr).then(function (ai) {
                var currentStepModel = WorkflowHelper.getStepModelByCustomData(_this.initialData.workflowModel, function (x) { return x.IsSettlement === 'yes'; });
                var areAllStepBeforeThisAccepted = currentStepModel.areAllStepBeforeThisAccepted(ai);
                if (!areAllStepBeforeThisAccepted) {
                    return;
                }
                var withDefault = function (s, dv) { return s === ApplicationDataSourceHelper.MissingItemReplacementValue ? dv : s; };
                _this.apiClient.fetchCreditApplicationItemSimple(_this.initialData.applicationNr, ['application.outgoingPaymentFileStatus', 'application.outgoingPaymentFileArchiveKey', 'application.outgoingPaymentFileCreationDate', 'application.loanCreationDate'], ApplicationDataSourceHelper.MissingItemReplacementValue).then(function (x) {
                    var outgoingPaymentFileStatus = withDefault(x['application.outgoingPaymentFileStatus'], 'initial');
                    if (outgoingPaymentFileStatus == 'initial') {
                        _this.reloadInitial(ai);
                        return;
                    }
                    _this.apiClient.fetchItemBasedCreditDecision({
                        ApplicationNr: _this.initialData.applicationNr,
                        MustBeCurrent: true,
                        MustBeAccepted: true,
                        MaxCount: 1
                    }).then(function (decisions) {
                        var decision = decisions.Decisions[0];
                        if (outgoingPaymentFileStatus == 'initialized') {
                            _this.reloadInitialized(ai, decision);
                        }
                        else if (outgoingPaymentFileStatus == 'pending' || outgoingPaymentFileStatus == 'done') {
                            var isPending = outgoingPaymentFileStatus == 'pending';
                            _this.reloadPendingOrDone(ai, decision, currentStepModel, withDefault(x['application.outgoingPaymentFileArchiveKey'], null), withDefault(x['application.outgoingPaymentFileCreationDate'], null), withDefault(x['application.loanCreationDate'], null), isPending);
                        }
                    });
                });
            });
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.getSummaryPaymentsAmounts = function () {
            return this.getOutgoingPayments().then(function (x) {
                var mainPaymentsAmount = 0;
                var childPaymentsAmount = 0;
                for (var _i = 0, _a = x.payments; _i < _a.length; _i++) {
                    var p = _a[_i];
                    if (p.isMain) {
                        mainPaymentsAmount += p.paymentAmount;
                    }
                    else {
                        childPaymentsAmount += p.paymentAmount;
                    }
                }
                return {
                    mainPaymentsAmount: mainPaymentsAmount,
                    childPaymentsAmount: childPaymentsAmount,
                };
            });
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.getOutgoingPayments = function () {
            var _this = this;
            var requestNames = [];
            for (var _i = 0, _a = ['targetBankName', 'targetAccountIban', 'paymentAmount', 'messageToReceiver', 'exists']; _i < _a.length; _i++) {
                var n = _a[_i];
                for (var _b = 0, _c = ['Main', 'Child']; _b < _c.length; _b++) {
                    var ln = _c[_b];
                    requestNames.push("".concat(ln, "SettlementPayments#*#u#").concat(n));
                }
            }
            return this.apiClient.fetchComplexApplicationListItemSimple(this.initialData.applicationNr, requestNames, ApplicationDataSourceHelper.MissingItemReplacementValue).then(function (x) {
                var d = {};
                for (var _i = 0, _a = Object.keys(x); _i < _a.length; _i++) {
                    var compoundName = _a[_i];
                    var n = ComplexApplicationListHelper.parseCompoundItemName(compoundName);
                    var isMain = n.listName == 'MainSettlementPayments';
                    var rowNr = parseInt(n.nr);
                    var dkey = "".concat(isMain ? 'm' : 'c').concat(rowNr);
                    if (!d[dkey]) {
                        d[dkey] = new OutgoingPaymentModel(rowNr, isMain);
                    }
                    var p = d[dkey];
                    var value = x[compoundName];
                    if (n.itemName == 'targetBankName') {
                        p.targetBankName = value;
                    }
                    else if (n.itemName == 'targetAccountIban') {
                        p.targetAccountIban = value;
                    }
                    else if (n.itemName == 'paymentAmount') {
                        p.paymentAmount = _this.parseDecimalOrNull(value);
                    }
                    else if (n.itemName == 'messageToReceiver') {
                        p.messageToReceiver = value;
                    }
                }
                var payments = [];
                for (var _b = 0, _c = Object.keys(d); _b < _c.length; _b++) {
                    var k = _c[_b];
                    payments.push(d[k]);
                }
                return { payments: payments };
            });
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.a = function (name, decision) {
            var d = decision;
            if (!d) {
                if (!this.m || !this.m.decision || !this.m.decision.IsAccepted) {
                    return null;
                }
                d = this.m.decision;
            }
            return d.UniqueItems[name];
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.an = function (name, decision) {
            return this.parseDecimalOrNull(this.a(name, decision));
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.anSum = function (names, decision) {
            var sum = 0;
            for (var _i = 0, names_1 = names; _i < names_1.length; _i++) {
                var n = names_1[_i];
                sum += this.an(n, decision);
            }
            return sum;
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.generatePaymentsFile = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.createDualMortgageLoanSettlementPaymentsFile(this.initialData.applicationNr).then(function (x) {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.cancelPending = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
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
                }]).then(function (x) {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.createNewLoans = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.createDualMortgageLoan(this.initialData.applicationNr).then(function (x) {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.setupInitializedTestFunctions = function () {
            var _this = this;
            if (!this.initialData || !this.initialData.isTest || !this.initialData.testFunctions) {
                return;
            }
            var tf = this.initialData.testFunctions;
            var scope = tf.generateUniqueScopeName();
            tf.addFunctionCall(scope, 'Fill IBAN, BankName and Message', function () {
                if (!_this.m || !_this.m.initial) {
                    toastr.warning('Must be initial');
                    return;
                }
                _this.getOutgoingPayments().then(function (x) {
                    var RandomIban = 'FI2140538661913452'; //Randomly generated iban
                    var edits = [];
                    var getName = function (p, itemName) {
                        return ComplexApplicationListHelper.getDataSourceItemName(p.isMain ? 'MainSettlementPayments' : 'ChildSettlementPayments', p.rowNr.toString(), itemName, ComplexApplicationListHelper.RepeatableCode.No);
                    };
                    for (var _i = 0, _a = x.payments; _i < _a.length; _i++) {
                        var p = _a[_i];
                        if (!p.targetAccountIban) {
                            edits.push({ dataSourceName: ComplexApplicationListHelper.DataSourceName, itemName: getName(p, 'targetAccountIban'), newValue: RandomIban, isDelete: false });
                        }
                        if (!p.targetBankName) {
                            edits.push({ dataSourceName: ComplexApplicationListHelper.DataSourceName, itemName: getName(p, 'targetBankName'), newValue: "Bank ".concat(p.isMain ? 'M' : 'C').concat(p.rowNr), isDelete: false });
                        }
                        if (!p.messageToReceiver) {
                            edits.push({ dataSourceName: ComplexApplicationListHelper.DataSourceName, itemName: getName(p, 'messageToReceiver'), newValue: "Message ".concat(p.isMain ? 'M' : 'C').concat(p.rowNr), isDelete: false });
                        }
                        if (edits.length === 0) {
                            return;
                        }
                        _this.initialData.apiClient.setApplicationEditItemDataBatched(_this.initialData.applicationNr, edits).then(function (x) {
                            _this.signalReloadRequired();
                        });
                    }
                });
            });
        };
        MortgageLoanApplicationDualSettlementHandleController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            var context = {
                applicationNr: this.initialData.applicationNr
            };
            var t = NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication, context);
            NavigationTargetHelper.handleBack(t, this.apiClient, this.$q, context);
        };
        MortgageLoanApplicationDualSettlementHandleController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanApplicationDualSettlementHandleController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationDualSettlementHandleComponentNs.MortgageLoanApplicationDualSettlementHandleController = MortgageLoanApplicationDualSettlementHandleController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationDualSettlementHandleComponentNs.Model = Model;
    var PendingOrDoneModel = /** @class */ (function () {
        function PendingOrDoneModel() {
        }
        return PendingOrDoneModel;
    }());
    MortgageLoanApplicationDualSettlementHandleComponentNs.PendingOrDoneModel = PendingOrDoneModel;
    var InitialModel = /** @class */ (function () {
        function InitialModel() {
        }
        return InitialModel;
    }());
    MortgageLoanApplicationDualSettlementHandleComponentNs.InitialModel = InitialModel;
    var OutgoingPaymentModel = /** @class */ (function () {
        function OutgoingPaymentModel(rowNr, isMain) {
            this.rowNr = rowNr;
            this.isMain = isMain;
        }
        return OutgoingPaymentModel;
    }());
    MortgageLoanApplicationDualSettlementHandleComponentNs.OutgoingPaymentModel = OutgoingPaymentModel;
    var MortgageLoanApplicationDualSettlementHandleComponent = /** @class */ (function () {
        function MortgageLoanApplicationDualSettlementHandleComponent() {
            this.initialDecisionTemplate = "<div>\n                <h3 class=\"text-center\">Mortgage loan</h3>\n                <hr class=\"hr-section\">\n                <div class=\"pb-2\">\n                    <div class=\"form-horizontal\">\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Initial fee</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainInitialFeeAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Valuation fee</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainValuationFeeAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Deed fee</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainDeedFeeAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Mortgage app. fee</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainMortgageApplicationFeeAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Purchase amount</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainPurchaseAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Payment to customer</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainDirectToCustomerAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Settlement amount</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainTotalSettlementAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Total amount</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainLoanAmount') | currency}}</div>\n                        </div>\n                    </div>\n                </div>\n                <h3 class=\"text-center\">Loan with collateral</h3>\n                <hr class=\"hr-section\">\n                <div>\n                    <div class=\"form-horizontal\">\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Initial fee</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('childInitialFeeAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Payment to customer</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('childDirectToCustomerAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Settlement amount</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('childTotalSettlementAmount') | currency}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Total amount</label>\n                            <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('childLoanAmount') | currency}}</div>\n                        </div>\n                    </div>\n                </div>\n            </div>";
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualSettlementHandleController;
            this.template = "<div ng-if=\"$ctrl.m\">\n\n    <div class=\"pt-1 pb-2\">\n        <div class=\"pull-left\"><a class=\"n-back\" ng-click=\"$ctrl.onBack($event)\" href=\"#\"><span class=\"glyphicon glyphicon-arrow-left\"></span></a></div>\n        <h1 class=\"adjusted\">Handle payments and settlement</h1>\n    </div>\n\n    ".concat(this.initialTemplate(), "\n    ").concat(this.pendingTemplate(), "\n</div>");
        }
        MortgageLoanApplicationDualSettlementHandleComponent.prototype.initialTemplate = function () {
            var paymentSummaryTemplate = "<div class=\"pb-1\">\n                                    <table class=\"table\">\n                                        <thead>\n                                            <tr>\n                                                <th class=\"col-xs-2\">&nbsp;</th>\n                                                <th class=\"col-xs-3 text-right\">Amount</th>\n                                                <th class=\"col-xs-4 text-right\">Outgoing payments</th>\n                                                <th class=\"col-xs-3 text-right\">Diff</th>\n                                            </tr>\n                                        </thead>\n                                        <tbody>\n                                            <tr>\n                                                <td>Total mortgage</td>\n                                                <td class=\"text-right\">{{$ctrl.m.initial.summary.mainExpectedPaymentsAmount | number:'2'}}</td>\n                                                <td class=\"text-right\">{{$ctrl.m.initial.summary.mainActualPaymentsAmount | number:'2'}}</td>\n                                                <td class=\"text-right\">{{$ctrl.m.initial.summary.mainDiffAmount | number:'2'}}</th>\n                                            </tr>\n                                            <tr>\n                                                <td>Total loan with collateral</td>\n                                                <td class=\"text-right\">{{$ctrl.m.initial.summary.childExpectedPaymentsAmount | number:'2'}}</td>\n                                                <td class=\"text-right\">{{$ctrl.m.initial.summary.childActualPaymentsAmount | number:'2'}}</td>\n                                                <td class=\"text-right\">{{$ctrl.m.initial.summary.childDiffAmount | number:'2'}}</th>\n                                            </tr>\n                                        </tbody>\n                                    </table></div>";
            var paymentsTemplate = "<div class=\"pb-1\">\n        <add-remove-list initial-data=\"$ctrl.m.initial.mainPaymentsInitialData\"></add-remove-list>\n    </div>\n    <div>\n        <add-remove-list initial-data=\"$ctrl.m.initial.childPaymentsInitialData\"></add-remove-list>\n    </div>";
            return "<div class=\"row pt-1 pb-2\" ng-if=\"$ctrl.m.initial\">\n        <div class=\"col-sm-4\">".concat(this.initialDecisionTemplate, "</div>\n        <div class=\"col-sm-8\">\n            <div class=\"editblock\">\n                ").concat(paymentSummaryTemplate, "\n                ").concat(paymentsTemplate, "\n                <div class=\"pt-2 text-center\" ng-if=\"$ctrl.m.initial.isGeneratePaymentsFileAllowed\">\n                    <a class=\"n-main-btn n-blue-btn\" ng-click=\"$ctrl.generatePaymentsFile($event)\">\n                        Preview outgoing payments file\n                    </a>\n                </div>\n            </div>\n        </div>\n    </div>");
        };
        MortgageLoanApplicationDualSettlementHandleComponent.prototype.pendingTemplate = function () {
            return "<div class=\"row pt-1 pb-2\" ng-if=\"$ctrl.m.pendingOrDone\">\n                <div class=\"col-sm-4\">".concat(this.initialDecisionTemplate, "</div>\n                <div class=\"col-sm-8 frame\" style=\"background-color: rgb(250, 250, 250)!important\">\n                    <div class=\"pb-1 text-right\" ng-if=\"$ctrl.m.pendingOrDone.isCancelAllowed\">\n                        <button class=\"n-main-btn n-white-btn\" ng-click=\"$ctrl.cancelPending($event)\">\n                            Cancel\n                        </button>\n                    </div>\n\n                    <div ng-repeat=\"isMain in [true, false]\" class=\"{{isMain ? 'pt-1': 'pt-3'}}\">\n                        <h2>{{isMain ? 'Mortgage loan' : 'Loan with collateral'}}</h2>\n                        <toggle-block header-text=\"'Outgoing payments'\" floated-header-text=\"(isMain ? $ctrl.m.pendingOrDone.mainPaymentsSum : $ctrl.m.pendingOrDone.childPaymentsSum) | number:'2'\">\n                            <table class=\"table\">\n                                <thead><tr>\n                                    <th>Bank</th>\n                                    <th>IBAN</th>\n                                    <th class=\"text-right\">Amount</th>\n                                </tr></thead>\n                                <tbody><tr ng-repeat=\"p in $ctrl.m.pendingOrDone.payments | filter: { 'isMain': isMain }\">\n                                    <td>{{p.targetBankName}}</td>\n                                    <td>{{p.targetAccountIban}}</td>\n                                    <td class=\"text-right\">{{p.paymentAmount | number:'2'}}</td>\n                                </tr></tbody>\n                            </table>\n                        </toggle-block>\n\n                        <toggle-block header-text=\"'Fees'\" floated-header-text=\"(isMain ? $ctrl.m.pendingOrDone.mainFeesSum : $ctrl.m.pendingOrDone.childFeesSum) | number : '2'\">\n                            <table class=\"table\">\n                                <thead><tr>\n                                    <th>Type</th>\n                                    <th class=\"text-right\">Amount</th>\n                                </tr></thead>\n                                <tbody>\n                                    <tr ng-if=\"isMain\">\n                                        <td>Initial fee</td>\n                                        <td class=\"text-right\">{{$ctrl.an('mainInitialFeeAmount') | number:'2'}}</td>\n                                    </tr>\n                                    <tr ng-if=\"isMain\">\n                                        <td>Valuation fee</td>\n                                        <td class=\"text-right\">{{$ctrl.an('mainValuationFeeAmount') | number:'2'}}</td>\n                                    </tr>\n                                    <tr ng-if=\"isMain\">\n                                        <td>Deed fee</td>\n                                        <td class=\"text-right\">{{$ctrl.an('mainDeedFeeAmount') | number:'2'}}</td>\n                                    </tr>\n                                    <tr ng-if=\"isMain\">\n                                        <td>Mortgage app. fee</td>\n                                        <td class=\"text-right\">{{$ctrl.an('mainMortgageApplicationFeeAmount') | number:'2'}}</td>\n                                    </tr>\n                                    <tr ng-if=\"!isMain\">\n                                        <td>Initial fee</td>\n                                        <td class=\"text-right\">{{$ctrl.an('childInitialFeeAmount') | number:'2'}}</td>\n                                    </tr>\n                                </tbody>\n                            </table>\n                        </toggle-block>\n                        <div class=\"block\">\n                            <h2>&nbsp;<span class=\"pull-right\"><b>{{(isMain ? $ctrl.m.pendingOrDone.mainTotalSum : $ctrl.m.pendingOrDone.childTotalSum) | number: '2'}}</b></span></h2>\n                        </div>\n                    </div>\n\n                    <div class=\"pt-3 text-center\" ng-if=\"$ctrl.m.pendingOrDone.isPending && $ctrl.m.pendingOrDone.outgoingPaymentFileUrl\">\n                        <a class=\"n-main-btn n-purple-btn\" ng-href=\"{{$ctrl.m.pendingOrDone.outgoingPaymentFileUrl}}\" target=\"_blank\">\n                            Download payment file <span class=\"glyphicon glyphicon-arrow-down\"></span>\n                        </a>\n                    </div>\n\n                    <div class=\"pt-3\" ng-if=\"$ctrl.m.pendingOrDone.isCreateLoanAllowed\">\n                        <div class=\"form-horizontal\">\n                            <p class=\"text-center pt-3\">Outgoing payment is delivered and confirmed?</p>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 text-right\">Yes</label>\n                                <div class=\"col-xs-4\"><input ng-model=\"$ctrl.m.pendingOrDone.isConfirmChecked\" type=\"checkbox\"></div>\n                            </div>\n                        </div>\n                        <div class=\"text-center pt-3\">\n                            <button class=\"n-main-btn n-green-btn\" ng-disabled=\"!$ctrl.m.pendingOrDone.isConfirmChecked\" ng-click=\"$ctrl.createNewLoans($event)\">Create loans</button>\n                        </div>\n                    </div>\n\n                    <div class=\"pt-3\" ng-if=\"!$ctrl.m.pendingOrDone.isPending\">\n                        <div class=\"form-horizontal\">\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Outgoing payment file creation date</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.m.pendingOrDone.outgoingPaymentFileCreationDate | date:'short'}}</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Loan creation date</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.m.pendingOrDone.loanCreationDate | date:'short' }}</div>\n                            </div>\n                        </div>\n                    </div>\n\n                </div>\n</div>\n");
        };
        return MortgageLoanApplicationDualSettlementHandleComponent;
    }());
    MortgageLoanApplicationDualSettlementHandleComponentNs.MortgageLoanApplicationDualSettlementHandleComponent = MortgageLoanApplicationDualSettlementHandleComponent;
})(MortgageLoanApplicationDualSettlementHandleComponentNs || (MortgageLoanApplicationDualSettlementHandleComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationDualSettlementHandle', new MortgageLoanApplicationDualSettlementHandleComponentNs.MortgageLoanApplicationDualSettlementHandleComponent());
