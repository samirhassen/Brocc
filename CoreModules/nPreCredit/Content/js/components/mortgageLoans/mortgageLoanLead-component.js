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
var MortgageLoanLeadComponentNs;
(function (MortgageLoanLeadComponentNs) {
    var MortgageLoanLeadController = /** @class */ (function (_super) {
        __extends(MortgageLoanLeadController, _super);
        function MortgageLoanLeadController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        MortgageLoanLeadController.prototype.componentName = function () {
            return 'mortgageLoanLead';
        };
        MortgageLoanLeadController.prototype.onBack = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var after = function () {
                var target;
                if (_this.m && _this.m.isWorkListMode) {
                    target = NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreateLeadWorkList);
                }
                else {
                    target = NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanSearch);
                }
                NavigationTargetHelper.handleBack(target, _this.apiClient, _this.$q, {
                    applicationNr: _this.m && _this.m.applicationNr ? _this.m.applicationNr : null,
                    workListId: _this.m && _this.m.workListStatus ? _this.m.workListStatus.WorkListHeaderId.toString() : null
                });
            };
            if (this.m.isWorkListMode) {
                //Try to replace beforing returning
                var ws = this.m.workListStatus;
                this.apiClient.tryCompleteOrReplaceMortgageLoanWorkListItem(ws.WorkListHeaderId, ws.ItemId, true).then(function (x) {
                    after();
                });
            }
            else {
                after();
            }
        };
        MortgageLoanLeadController.prototype.onChanges = function () {
            this.reload();
        };
        MortgageLoanLeadController.prototype.reload = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var i = this.initialData;
            var isWorkListMode = !!i.workListApplicationNr;
            var applicationNr = isWorkListMode ? i.workListApplicationNr : i.leadOnlyApplicationNr;
            if (!applicationNr) {
                return;
            }
            this.apiClient.fetchApplicationInfo(applicationNr).then(function (ai) {
                LeadDataHelper.fetch(applicationNr, _this.apiClient).then(function (lead) {
                    _this.apiClient.fetchApplicationAssignedHandlers({ applicationNr: applicationNr, returnPossibleHandlers: true, returnAssignedHandlers: false }).then(function (handlers) {
                        MortgageLoanApplicationDualCreditCheckSharedNs.getApplicantDataByApplicantNr(ai.ApplicationNr, ai.NrOfApplicants > 1, _this.apiClient).then(function (applicantDataByApplicantNr) {
                            if (isWorkListMode) {
                                _this.apiClient.fetchMortgageLoanWorkListItemStatus(parseInt(_this.initialData.workListId), _this.initialData.workListApplicationNr).then(function (workListStatus) {
                                    if (workListStatus.IsTakenByCurrentUser) {
                                        _this.m = _this.createModel(ai, lead, workListStatus, applicantDataByApplicantNr, handlers.PossibleHandlers);
                                    }
                                    else {
                                        toastr.warning('Lead is not taken by the current user in this worklist, ignoring the worklist');
                                        _this.m = _this.createModel(ai, lead, null, applicantDataByApplicantNr, handlers.PossibleHandlers);
                                    }
                                });
                            }
                            else {
                                _this.m = _this.createModel(ai, lead, null, applicantDataByApplicantNr, handlers.PossibleHandlers);
                            }
                        });
                    });
                });
            });
        };
        MortgageLoanLeadController.prototype.createModel = function (ai, leadData, workListStatus, applicantDataByApplicantNr, assignableHandlers) {
            return {
                isActiveLead: ai.IsLead && ai.IsActive,
                lead: leadData,
                isWorkListMode: !!workListStatus,
                applicationNr: ai.ApplicationNr,
                workListStatus: workListStatus,
                assignableHandlers: assignableHandlers,
                assignedHandlerUserId: null,
                providerDisplayName: ai.ProviderDisplayName,
                b: this.createDecisionModel(ai, workListStatus ? workListStatus.WorkListHeaderId.toString() : null, applicantDataByApplicantNr),
                commentsInitialData: {
                    applicationInfo: ai,
                    hideAdditionalInfoToggle: true,
                    reloadPageOnWaitingForAdditionalInformation: false
                },
                tabs: {
                    activeName: 'qualifiedLead',
                    tryLaterOptions: NTechLinq.select(TryLaterDaysOptions, function (x) { return x.toString(); }),
                    selectedTryLaterDays: this.inferNextTryLaterDays(leadData),
                    rejectModel: RejectModel.create(this.initialData.rejectionReasonToDisplayNameMapping)
                }
            };
        };
        MortgageLoanLeadController.prototype.inferNextTryLaterDays = function (d) {
            //Idea is to "take the next one" from TryLaterDaysOptions so for 5->6 we take 7 and so on until we reach the highest one and then stop there
            var tryLaterDays = d.tryLaterDays();
            for (var _i = 0, TryLaterDaysOptions_1 = TryLaterDaysOptions; _i < TryLaterDaysOptions_1.length; _i++) {
                var tryLaterLimit = TryLaterDaysOptions_1[_i];
                if (tryLaterDays < tryLaterLimit) {
                    return tryLaterLimit.toString();
                }
            }
            return tryLaterDays.toString();
        };
        MortgageLoanLeadController.prototype.createCrossModuleNavigationTargetToHere = function (applicationNr, workListId) {
            var code;
            if (workListId) {
                code = this.ntechComponentService.createCrossModuleNavigationTargetCode('MortgageLoanApplicationWorkListLead', { applicationNr: applicationNr, workListId: workListId });
            }
            else {
                code = code = this.ntechComponentService.createCrossModuleNavigationTargetCode('MortgageLoanApplicationLead', { applicationNr: applicationNr });
            }
            return NavigationTargetHelper.createCodeTarget(code, null);
        };
        MortgageLoanLeadController.prototype.createDecisionModel = function (x, workListId, applicantDataByApplicantNr) {
            var b = new DecisionBasisModel();
            var targetToHere = this.createCrossModuleNavigationTargetToHere(x.ApplicationNr, workListId);
            var isInPlaceEditAllowed = true;
            var isReadOnly = false;
            var afterInPlaceEditCommited = function () {
            };
            MortgageLoanApplicationDualCreditCheckSharedNs.initializeSharedDecisionModel(b, x.NrOfApplicants > 1, x, targetToHere, this.apiClient, this.$q, isInPlaceEditAllowed, afterInPlaceEditCommited, targetToHere.targetCode, isReadOnly, applicantDataByApplicantNr);
            var createCustomerInfoInitialData = function (applicantNr) {
                var d = {
                    applicationNr: x.ApplicationNr,
                    applicantNr: applicantNr,
                    customerIdCompoundItemName: null,
                    backTarget: targetToHere.targetCode
                };
                return d;
            };
            b.applicationCustomerInfo1InitialData = createCustomerInfoInitialData(1);
            if (x.NrOfApplicants > 1) {
                b.applicationCustomerInfo2InitialData = createCustomerInfoInitialData(2);
            }
            return b;
        };
        MortgageLoanLeadController.prototype.skipItem = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.afterComplete(null);
        };
        MortgageLoanLeadController.prototype.goToApplication = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication, { applicationNr: this.m.applicationNr });
        };
        MortgageLoanLeadController.prototype.setActiveTab = function (tabName, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.tabs.activeName = tabName;
        };
        MortgageLoanLeadController.prototype.changeToQualifiedLead = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.tryComplateMortgageLoanLead(this.m.applicationNr, ChangeToQualifiedLeadCode, null, null, null).then(function (x) {
                if (!x.WasChangedToQualifiedLead) {
                    toastr.warning('Could not change to qualified lead');
                    return;
                }
                _this.apiClient.setApplicationAssignedHandlers(_this.m.applicationNr, _this.m.assignedHandlerUserId ? [_this.m.assignedHandlerUserId] : null, null).then(function () {
                    _this.afterComplete(ChangeToQualifiedLeadCode);
                });
            });
        };
        MortgageLoanLeadController.prototype.cancelLead = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.tryComplateMortgageLoanLead(this.m.applicationNr, CancelCode, null, null, null).then(function (x) {
                if (!x.WasCancelled) {
                    toastr.warning('Could not cancel lead');
                    return;
                }
                _this.afterComplete(CancelCode);
            });
        };
        MortgageLoanLeadController.prototype.tryLater = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var tryLaterDays = parseInt(this.m.tabs.selectedTryLaterDays);
            this.apiClient.tryComplateMortgageLoanLead(this.m.applicationNr, TryLaterCode, null, null, tryLaterDays).then(function (x) {
                if (!x.WasTryLaterScheduled) {
                    toastr.warning('Could not schedule try later');
                    return;
                }
                _this.afterComplete(TryLaterCode);
            });
        };
        MortgageLoanLeadController.prototype.reject = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var r = this.m.tabs.rejectModel.getSelectedReasons();
            this.apiClient.tryComplateMortgageLoanLead(this.m.applicationNr, RejectCode, r.reasonCodes, r.otherReasonText, null).then(function (x) {
                if (!x.WasRejected) {
                    toastr.warning('Could not reject');
                    return;
                }
                _this.afterComplete(RejectCode);
            });
        };
        MortgageLoanLeadController.prototype.afterComplete = function (completionCode) {
            var _this = this;
            if (this.m.isWorkListMode) {
                //Complete and take a new one
                var ws_1 = this.m.workListStatus;
                this.apiClient.tryCompleteOrReplaceMortgageLoanWorkListItem(ws_1.WorkListHeaderId, ws_1.ItemId, false).then(function (x) {
                    if (!x.WasCompleted) {
                        toastr.warning('Could not complete worklist item');
                        return;
                    }
                    _this.apiClient.tryTakeMortgageLoanWorkListItem(ws_1.WorkListHeaderId).then(function (y) {
                        if (!y.WasItemTaken) {
                            NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreateLeadWorkList, null);
                        }
                        else {
                            NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanLead, {
                                applicationNr: y.TakenItemId,
                                workListId: ws_1.WorkListHeaderId.toString()
                            });
                        }
                    });
                });
            }
            else {
                //TODO: Sending back to search ... could alternatively reload the page but will get wierd for things like try later
                NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanSearch, null);
            }
        };
        MortgageLoanLeadController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanLeadController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanLeadComponentNs.MortgageLoanLeadController = MortgageLoanLeadController;
    var ChangeToQualifiedLeadCode = 'ChangeToQualifiedLead';
    var CancelCode = 'Cancel';
    var TryLaterCode = 'TryLater';
    var RejectCode = 'Reject';
    var TryLaterDaysOptions = [0, 1, 2, 4, 7, 14];
    var MortgageLoanLeadComponent = /** @class */ (function () {
        function MortgageLoanLeadComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanLeadController;
            var decisionBasisTemplate = "<div>\n    <h2 class=\"custom-header\">Decision basis</h2>\n    <hr class=\"hr-section\" />\n\n    <div class=\"row\">\n        <div class=\"col-xs-8\">\n            <div class=\"editblock\">\n                <div class=\"row pb-3\">\n                    <div class=\"col-xs-6\">\n                        <application-editor initial-data=\"$ctrl.m.b.applicationBasisFields\"></application-editor>\n                    </div>\n                </div>\n\n                <div class=\"row\">\n                    <div class=\"col-xs-6\">\n                        <h2 class=\"custom-header text-center\">{{$ctrl.m.b.applicant1DetailInfo}}</h2>\n                        <hr class=\"hr-section\" />\n                    </div>\n                    <div class=\"col-xs-6\">\n                        <h2 class=\"custom-header text-center\" ng-if=\"$ctrl.m.b.hasCoApplicant\">{{$ctrl.m.b.applicant2DetailInfo}}</h2>\n                        <hr ng-if=\"$ctrl.m.b.hasCoApplicant\" class=\"hr-section\" />\n                    </div>\n                </div>\n                <div class=\"row pb-3\">\n                    <div class=\"col-xs-6\">\n                        <application-editor initial-data=\"$ctrl.m.b.applicant1BasisFields\"></application-editor>\n                    </div>\n                    <div class=\"col-xs-6\">\n                        <application-editor ng-if=\"$ctrl.m.b.hasCoApplicant\" initial-data=\"$ctrl.m.b.applicant2BasisFields\"></application-editor>\n                    </div>\n                </div>\n            </div>\n        </div>\n        <div class=\"col-xs-4\">\n            <div class=\"pb-1\">\n                <application-customerinfo initial-data=\"$ctrl.m.b.applicationCustomerInfo1InitialData\"></application-customerinfo>\n            </div>\n            <hr class=\"hr-section dotted\" />\n            <div ng-if=\"$ctrl.m.b.applicationCustomerInfo2InitialData\" class=\"pb-3\">\n                <application-customerinfo initial-data=\"$ctrl.m.b.applicationCustomerInfo2InitialData\"></application-customerinfo>\n            </div>\n            <hr ng-if=\"$ctrl.m.b.applicationCustomerInfo2InitialData\" class=\"hr-section dotted\" />\n            <h2 class=\"custom-header text-center\">Other Applications</h2>\n            <hr class=\"hr-section\" />\n            <mortgage-loan-other-connected-applications-compact initial-data=\"$ctrl.m.b.otherApplicationsData\"></mortgage-loan-other-connected-applications-compact>\n            <h2 class=\"custom-header text-center\">Object</h2>\n            <hr class=\"hr-section\" />\n            <mortgage-loan-dual-collateral-compact initial-data=\"$ctrl.m.b.objectCollateralData\"></mortgage-loan-dual-collateral-compact>\n            <h2 class=\"custom-header text-center\">Other</h2>\n            <hr class=\"hr-section\" />\n            <mortgage-loan-dual-collateral-compact initial-data=\"$ctrl.m.b.otherCollateralData\"></mortgage-loan-dual-collateral-compact>\n        </div>\n    </div>\n\n</div>";
            var sharedNotActiveLeadTemplate = "\n                <p ng-if=\"$ctrl.m.lead.wasAccepted()\" class=\"text-center\">Converted to a <a href=\"#\" ng-click=\"$ctrl.goToApplication($event)\">qualified lead</a> on {{$ctrl.m.lead.acceptedDate() | date:shortDate}}</p>\n                              \n                <div ng-if=\"$ctrl.m.lead.wasCancelled()\">\n                    <div class=\"form-horizontal\">\n                        <div class=\"form-group\">\n                            <label class=\"control-label col-xs-6\">Cancelled on</label>\n                            <div class=\"form-control-static col-xs-6\">{{$ctrl.m.lead.cancelledDate() | date:shortDate}}</div>\n                        </div>\n                    </div>\n                </div>\n\n                <div ng-if=\"$ctrl.m.lead.wasRejected()\">\n                    <div class=\"form-horizontal\">\n                        <div class=\"form-group\">\n                            <label class=\"control-label col-xs-6\">Rejected on</label>\n                            <div class=\"form-control-static col-xs-6\">{{$ctrl.m.lead.rejectedDate() | date:shortDate}}</div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"control-label col-xs-6\">Reasons</label>\n                            <div class=\"form-control-static col-xs-6\"><span ng-repeat=\"r in $ctrl.m.lead.displayRejectionReasons($ctrl.initialData.rejectionReasonToDisplayNameMapping)\" class=\"comma\">{{r}}</span></div>\n                        </div>\n                    </div>                    \n                </div>                \n                \n                <p ng-if=\"!$ctrl.m.lead.isLead() && !$ctrl.m.lead.wasAccepted()\">Not a lead</p>";
            var sharedTemplate = "<div ng-show=\"$ctrl.m.isActiveLead\"><div class=\"row\">\n        <div class=\"col-xs-10 col-sm-offset-1\">\n            <div class=\"row\" ng-init=\"tabs=[['Qualified Lead', 'qualifiedLead'], ['Reject', 'reject'], ['Try later', 'tryLater'], ['Cancel', 'cancel']]\">\n                <div class=\"{{$first ? 'col-sm-offset-2 ' : ''}}col-xs-2\" ng-repeat=\"t in tabs\">\n                    <span ng-click=\"$ctrl.setActiveTab(t[1], $event)\" type=\"button\" class=\"btn\" ng-class=\"{ disabled : false, 'decision-form-active-btn' : $ctrl.m.tabs.activeName === t[1], 'decision-form-inactive-btn' : $ctrl.m.tabs.activeName !== t[1] }\">\n                        {{t[0]}}\n                    </span>\n                </div>\n            </div>\n\n            <form class=\"decision-form\" name=\"qualifiedLeadForm\" bootstrap-validation=\"'parent'\" novalidate ng-show=\"$ctrl.m.tabs.activeName == 'qualifiedLead'\">\n                \n                <div class=\"form-horizontal\">\n                    <div class=\"form-group\">\n                        <label class=\"col-xs-6 control-label\">Qualified lead</label>\n                        <div class=\"col-xs-6\"><div class=\"checkbox\"><input type=\"checkbox\" ng-model=\"isQualifiedLeadChecked\"></div></div>\n                    </div>\n                    <div class=\"form-group\">\n                        <label for=\"assignedHandler\" class=\"col-xs-6 control-label\">Assign handler</label>\n                        <div class=\"col-xs-4\">\n                            <select id=\"assignedHandler\" class=\"form-control\" ng-model=\"$ctrl.m.assignedHandlerUserId\" ng-options=\"handler.UserId as handler.UserDisplayName for handler in $ctrl.m.assignableHandlers\">\n                            <option value=\"\" selected=\"true\">None</option>\n                            </select>\n                        </div>                  \n                    </div>\n                </div>                    \n                <div class=\"text-center pt-3\">\n                    <button type=\"button\" class=\"n-main-btn n-green-btn\" ng-disabled=\"!isQualifiedLeadChecked\" ng-click=\"$ctrl.changeToQualifiedLead($event)\">Next</button>\n                </div>\n            </form>\n\n            <form class=\"form-horizontal decision-form\" name=\"rejectForm\" bootstrap-validation=\"'parent'\" novalidate ng-show=\"$ctrl.m.tabs.activeName == 'reject'\">\n                <h4 class=\"text-center\">Rejection reasons</h4>\n                <div class=\"row\">\n                    <div class=\"col-sm-6 col-md-6\">\n                        <div class=\"form-group\" ng-repeat=\"b in $ctrl.m.tabs.rejectModel.rejectModelCheckboxesCol1\">\n                            <label class=\"col-md-8 control-label\">{{b.displayName}}</label>\n                            <div class=\"col-md-4\"><div class=\"checkbox\"><input type=\"checkbox\" ng-model=\"$ctrl.m.tabs.rejectModel.reasons[b.reason]\"></div></div>\n                        </div>\n                    </div>\n                    <div class=\"col-sm-6 col-md-6\">\n                        <div class=\"form-group\" ng-repeat=\"b in $ctrl.m.tabs.rejectModel.rejectModelCheckboxesCol2\">\n                            <label class=\"col-md-6 control-label\">{{b.displayName}}</label>\n                            <div class=\"col-md-4\"><div class=\"checkbox\"><input type=\"checkbox\" ng-model=\"$ctrl.m.tabs.rejectModel.reasons[b.reason]\"></div></div>\n                        </div>\n                    </div>\n                </div>\n                <div class=\"form-group\">\n                    <label class=\"col-md-4 control-label\">Other</label>\n                    <div class=\"col-md-6\"><input type=\"text\" class=\"form-control\" ng-model=\"$ctrl.m.tabs.rejectModel.otherReason\"></div>\n                </div>\n                <div class=\"text-center pt-3\">\n                    <button type=\"button\" class=\"n-main-btn n-red-btn\" ng-disabled=\"!$ctrl.m.tabs.rejectModel.anyRejectionReasonGiven()\" ng-click=\"$ctrl.reject($event)\">Reject</button>\n                </div>\n            </form>\n            <form class=\"form-horizontal decision-form\" name=\"tryLaterForm\" bootstrap-validation=\"'parent'\" novalidate ng-show=\"$ctrl.m.tabs.activeName == 'tryLater'\">\n                <div class=\"row\">\n                    <div class=\"col-sm-offset-2 col-sm-6 col-md-6\">\n                        <div class=\"form-group\">\n                            <label class=\"col-md-8 control-label\">Try again after</label>\n                            <div class=\"col-md-4\">\n                                <select class=\"form-control\" ng-model=\"$ctrl.m.tabs.selectedTryLaterDays\">\n                                    <option ng-repeat=\"d in $ctrl.m.tabs.tryLaterOptions\" value=\"{{d}}\">{{d}} days</option>\n                                </select>\n                            </div>\n                        </div>\n                    </div>\n                </div>\n                <div class=\"text-center pt-3\">\n                    <button type=\"button\" class=\"n-main-btn n-green-btn\" ng-click=\"$ctrl.tryLater($event)\">Next</button>\n                </div>\n            </form>\n            <form class=\"form-horizontal decision-form\" name=\"cancelForm\" bootstrap-validation=\"'parent'\" novalidate ng-show=\"$ctrl.m.tabs.activeName == 'cancel'\">\n                <div class=\"row\">\n                    <div class=\"col-sm-offset-2 col-sm-6 col-md-6\">\n                        <div class=\"form-group\">\n                            <label class=\"col-md-8 control-label\">Cancel</label>\n                            <div class=\"col-md-4\"><div class=\"checkbox\"><input type=\"checkbox\" ng-model=\"isCancelChecked\"></div></div>\n                        </div>\n                    </div>\n                </div>\n                <div class=\"text-center pt-3\">\n                    <button type=\"button\" class=\"n-main-btn n-green-btn\" ng-disabled=\"!isCancelChecked\" ng-click=\"$ctrl.cancelLead($event)\">Next</button>\n                </div>\n            </form>\n        </div>\n    </div>\n\n    ".concat(decisionBasisTemplate, "\n    </div>\n\n    <div ng-hide=\"$ctrl.m.isActiveLead\">\n        ").concat(sharedNotActiveLeadTemplate, "\n   </div>\n\n    <div class=\"pt-3\"><application-comments initial-data=\"$ctrl.m.commentsInitialData\"></application-comments></div>");
            var standAloneTemplate = "<div class=\"pt-1 pb-2\">\n        <div class=\"pull-left\"><a class=\"n-back\" href=\"#\" ng-click=\"$ctrl.onBack($event)\"><span class=\"glyphicon glyphicon-arrow-left\"></span></a></div>\n        <h1 class=\"adjusted\">Lead {{$ctrl.m.applicationNr}} \n            <span class=\"adjusted-subtitle\"> {{$ctrl.m.providerDisplayName}}</span> \n        </h1>\n    </div>\n    \n    ".concat(sharedTemplate);
            var workListTemplate = "<div class=\"pt-1 pb-2\">\n        <div class=\"row\">\n            <div class=\"col-xs-1\"><a class=\"n-back\" href=\"#\" ng-click=\"$ctrl.onBack($event)\"><span class=\"glyphicon glyphicon-arrow-left\"></span></a></div>\n            <div class=\"col-xs-3\">\n                <div class=\"text-center worklist-counter\">\n                    <label>{{$ctrl.m.providerDisplayName}}</label>\n                    <p>{{$ctrl.m.workListStatus.CurrentUserActiveItemId}}</p>\n                </div>\n            </div>\n            <div class=\"col-xs-2\">\n                <div class=\"text-center worklist-counter\">\n                    <label>Selection</label>\n                    <p>Leads</p>\n                </div>\n            </div>\n            <div class=\"col-xs-2\">\n                <div class=\"text-center worklist-counter\">\n                    <label>My count</label>\n                    <p>{{$ctrl.m.workListStatus.TakeOrCompletedByCurrentUserCount}}</p>\n                </div>\n            </div>\n            <div class=\"col-xs-2\">\n                <div class=\"text-center worklist-counter\">\n                    <label>Total</label>\n                    <p>{{$ctrl.m.workListStatus.TakenCount + $ctrl.m.workListStatus.CompletedCount}}/{{$ctrl.m.workListStatus.TotalCount}}\n                    </p>\n                </div>\n            </div>\n            <div class=\"col-xs-2 text-right pt-1\">\n                <button ng-click=\"$ctrl.skipItem($event)\" type=\"button\"\n                    class=\"n-main-btn n-blue-btn\">Skip <span\n                        class=\"glyphicon glyphicon-arrow-right\"></span></button>\n            </div>\n        </div>\n        <div style=\"border-bottom: 2px solid #2d7fc1;padding-top:5px;\"></div>\n    </div>\n\n    ".concat(sharedTemplate);
            this.template = "<div ng-if=\"$ctrl.m && $ctrl.m.isWorkListMode === false\">".concat(standAloneTemplate, "</div>\n                             <div ng-if=\"$ctrl.m && $ctrl.m.isWorkListMode === true\">").concat(workListTemplate, "</div>");
        }
        return MortgageLoanLeadComponent;
    }());
    MortgageLoanLeadComponentNs.MortgageLoanLeadComponent = MortgageLoanLeadComponent;
    var LeadDataHelper = /** @class */ (function () {
        function LeadDataHelper(leadData) {
            this.leadData = leadData;
        }
        LeadDataHelper.prototype.isLead = function () {
            return this.leadData.getOptionalUniqueValue(1, 'IsLead') === 'true';
        };
        LeadDataHelper.prototype.wasAccepted = function () {
            return this.leadData.getOptionalUniqueValue(1, 'WasAccepted') === 'true';
        };
        LeadDataHelper.prototype.acceptedDate = function () {
            var d = this.leadData.getOptionalUniqueValue(1, 'AcceptedDate');
            if (!d) {
                return null;
            }
            return moment(d).toDate();
        };
        LeadDataHelper.prototype.wasCancelled = function () {
            return this.leadData.getOptionalUniqueValue(1, 'WasCancelled') === 'true';
        };
        LeadDataHelper.prototype.cancelledDate = function () {
            var d = this.leadData.getOptionalUniqueValue(1, 'CancelledDate');
            if (!d) {
                return null;
            }
            return moment(d).toDate();
        };
        LeadDataHelper.prototype.wasRejected = function () {
            return this.leadData.getOptionalUniqueValue(1, 'WasRejected') === 'true';
        };
        LeadDataHelper.prototype.rejectedDate = function () {
            var d = this.leadData.getOptionalUniqueValue(1, 'RejectedDate');
            if (!d) {
                return null;
            }
            return moment(d).toDate();
        };
        LeadDataHelper.prototype.displayRejectionReasons = function (rejectionReasonToDisplayNameMapping) {
            var rejectionReasons = this.leadData.getOptionalRepeatingValue(1, 'RejectionReasons');
            if (!rejectionReasons) {
                return null;
            }
            var displayRejectionReasons = [];
            for (var _i = 0, rejectionReasons_1 = rejectionReasons; _i < rejectionReasons_1.length; _i++) {
                var r = rejectionReasons_1[_i];
                if (r === 'other') {
                    displayRejectionReasons.push('other: ' + this.leadData.getOptionalUniqueValue(1, 'OtherRejectionReasonText'));
                }
                else {
                    displayRejectionReasons.push(rejectionReasonToDisplayNameMapping[r] ? rejectionReasonToDisplayNameMapping[r] : r);
                }
            }
            return displayRejectionReasons;
        };
        LeadDataHelper.prototype.tryLaterDays = function () {
            var d = this.leadData.getOptionalUniqueValue(1, 'TryLaterDays');
            if (d) {
                return parseInt(d);
            }
            else {
                return 0;
            }
        };
        LeadDataHelper.fetch = function (applicationNr, apiClient) {
            return ComplexApplicationListHelper.fetch(applicationNr, 'Lead', apiClient, ['IsLead', 'WasAccepted', 'AcceptedDate', 'TryLaterDays', 'WasCancelled', 'CancelledDate', 'WasRejected', 'RejectedDate', 'OtherRejectionReasonText'], ['RejectionReasons']).then(function (leadData) {
                return new LeadDataHelper(leadData);
            });
        };
        LeadDataHelper.ComplexListItemNamesUsed = ['IsLead', 'WasAccepted', 'TryLaterDays'];
        return LeadDataHelper;
    }());
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanLeadComponentNs.Model = Model;
    var RejectModel = /** @class */ (function () {
        function RejectModel() {
            this.otherReason = '',
                this.reasons = {};
            this.rejectModelCheckboxesCol1 = [];
            this.rejectModelCheckboxesCol2 = [];
        }
        RejectModel.create = function (rejectionReasonToDisplayNameMapping) {
            var r = new RejectModel();
            for (var _i = 0, _a = Object.keys(rejectionReasonToDisplayNameMapping); _i < _a.length; _i++) {
                var reasonName = _a[_i];
                var displayName = rejectionReasonToDisplayNameMapping[reasonName];
                if (r.rejectModelCheckboxesCol1.length > r.rejectModelCheckboxesCol2.length) {
                    r.rejectModelCheckboxesCol2.push(new RejectionCheckboxModel(reasonName, displayName));
                }
                else {
                    r.rejectModelCheckboxesCol1.push(new RejectionCheckboxModel(reasonName, displayName));
                }
            }
            return r;
        };
        RejectModel.prototype.getSelectedReasons = function () {
            var reasonCodes = [];
            for (var _i = 0, _a = Object.keys(this.reasons); _i < _a.length; _i++) {
                var key = _a[_i];
                if (this.reasons[key] === true) {
                    reasonCodes.push(key);
                }
            }
            if (this.otherReason) {
                reasonCodes.push('other');
            }
            return { reasonCodes: reasonCodes, otherReasonText: this.otherReason ? this.otherReason : null };
        };
        RejectModel.prototype.anyRejectionReasonGiven = function () {
            return this.getSelectedReasons().reasonCodes.length > 0;
        };
        return RejectModel;
    }());
    MortgageLoanLeadComponentNs.RejectModel = RejectModel;
    var RejectionCheckboxModel = /** @class */ (function () {
        function RejectionCheckboxModel(reason, displayName) {
            this.reason = reason;
            this.displayName = displayName;
        }
        return RejectionCheckboxModel;
    }());
    MortgageLoanLeadComponentNs.RejectionCheckboxModel = RejectionCheckboxModel;
    var DecisionBasisModel = /** @class */ (function () {
        function DecisionBasisModel() {
        }
        return DecisionBasisModel;
    }());
    MortgageLoanLeadComponentNs.DecisionBasisModel = DecisionBasisModel;
})(MortgageLoanLeadComponentNs || (MortgageLoanLeadComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanLead', new MortgageLoanLeadComponentNs.MortgageLoanLeadComponent());
