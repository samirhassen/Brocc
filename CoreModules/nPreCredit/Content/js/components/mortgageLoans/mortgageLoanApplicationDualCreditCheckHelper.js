var MortgageLoanApplicationDualCreditCheckSharedNs;
(function (MortgageLoanApplicationDualCreditCheckSharedNs) {
    MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName = 'CurrentMortgageLoans';
    MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName = 'CurrentOtherLoans';
    MortgageLoanApplicationDualCreditCheckSharedNs.FieldNames = ['bankName', 'loanTotalAmount', 'loanMonthlyAmount', 'loanShouldBeSettled', 'loanApplicant1IsParty', 'loanApplicant2IsParty'];
    function getDecisionBasisHtmlTemplate(isNew) {
        return "<div ng-if=\"$ctrl.m.b\">\n    <h2 class=\"custom-header\">Decision basis</h2>\n    <hr class=\"hr-section\" />\n\n    <div class=\"row\">\n        <div class=\"col-xs-8\">\n            <div class=\"editblock\">\n                <div class=\"row pb-3\">\n                    <div class=\"col-xs-6\">\n                        <application-editor initial-data=\"$ctrl.m.b.applicationBasisFields\"></application-editor>\n                    </div>\n                </div>\n\n                <div class=\"row\">\n                    <div class=\"col-xs-6\">\n                        <h2 class=\"custom-header text-center\">{{$ctrl.m.b.applicant1DetailInfo}}</h2>\n                        <hr class=\"hr-section\" />\n                    </div>\n                    <div class=\"col-xs-6\">\n                        <h2 class=\"custom-header text-center\" ng-if=\"$ctrl.m.b.hasCoApplicant\">{{$ctrl.m.b.applicant2DetailInfo}}</h2>\n                        <hr ng-if=\"$ctrl.m.b.hasCoApplicant\" class=\"hr-section\" />\n                    </div>\n                </div>\n                <div class=\"row pb-3\">\n                    <div class=\"col-xs-6\">\n                        <application-editor initial-data=\"$ctrl.m.b.applicant1BasisFields\"></application-editor>\n                    </div>\n                    <div class=\"col-xs-6\">\n                        <application-editor ng-if=\"$ctrl.m.b.hasCoApplicant\" initial-data=\"$ctrl.m.b.applicant2BasisFields\"></application-editor>\n                    </div>\n                </div>\n                <div>\n                    <div class=\"pb-2\">\n                        <h3>Current mortgage loans</h3>\n                        <hr class=\"hr-section\" />\n\n                        <button ng-if=\"$ctrl.m.b.isEditAllowed\" class=\"n-direct-btn n-green-btn\" ng-click=\"$ctrl.m.b.addExistingLoan(true, $event)\" ng-if=\"$ctrl.m.b.isEditAllowed\">Add</button>\n                        <hr class=\"hr-section dotted\" />\n\n                        <div ng-repeat=\"c in $ctrl.m.b.currentMortgageLoans\">\n                            <div class=\"row\">\n                                <div class=\"col-xs-6\">\n                                    <application-editor initial-data=\"c.d\"></application-editor>\n                                </div>\n                                <div class=\"col-xs-6\">\n                                    <div class=\"text-right\">\n                                        <button ng-if=\"$ctrl.m.b.isEditAllowed\" class=\"n-icon-btn n-red-btn\" ng-click=\"$ctrl.m.b.deleteExistingLoan(true, c.nr, $event)\"><span class=\"glyphicon glyphicon-minus\"></span></button>\n                                    </div>\n                                </div>\n                            </div>\n                            <hr class=\"hr-section dotted\" />\n                        </div>\n\n                    </div>\n\n                    <div class=\"pb-2\">\n                        <h3>Current consumer loans</h3>\n                        <hr class=\"hr-section\" />\n\n                        <button ng-if=\"$ctrl.m.b.isEditAllowed\" class=\"n-direct-btn n-green-btn\" ng-click=\"$ctrl.m.b.addExistingLoan(false, $event)\" ng-if=\"$ctrl.m.b.isEditAllowed\">Add</button>\n                        <hr class=\"hr-section dotted\" />\n\n                        <div ng-repeat=\"c in $ctrl.m.b.additionalCurrentOtherLoans\">\n                            <div class=\"row\">\n                                <div class=\"col-xs-6\">\n                                    <application-editor initial-data=\"c.d\"></application-editor>\n                                </div>\n                                <div class=\"col-xs-6\">\n                                    <div class=\"text-right\">\n                                        <button ng-if=\"$ctrl.m.b.isEditAllowed\" class=\"n-icon-btn n-red-btn\" ng-click=\"$ctrl.m.b.deleteExistingLoan(false, c.nr, $event)\"><span class=\"glyphicon glyphicon-minus\"></span></button>\n                                    </div>\n                                </div>\n                            </div>\n                            <hr class=\"hr-section dotted\" />\n                        </div>\n                    </div>\n                </div>\n            </div>\n        </div>\n        <div class=\"col-xs-4\">\n            <h2 class=\"custom-header text-center\">Other applications</h2>\n            <hr class=\"hr-section\" />\n            <mortgage-loan-other-connected-applications-compact initial-data=\"$ctrl.m.b.otherApplicationsData\"></mortgage-loan-other-connected-applications-compact>\n            <h2 class=\"custom-header text-center pt-3\">Object</h2>\n            <hr class=\"hr-section\" />\n            <mortgage-loan-dual-collateral-compact initial-data=\"$ctrl.m.b.objectCollateralData\"></mortgage-loan-dual-collateral-compact>\n            <h2 class=\"custom-header text-center pt-3\">Other</h2>\n            <hr class=\"hr-section\" />\n            <mortgage-loan-dual-collateral-compact initial-data=\"$ctrl.m.b.otherCollateralData\"></mortgage-loan-dual-collateral-compact>\n            <h2 class=\"custom-header text-center pt-3\">External Credit Report</h2>\n            <hr class=\"hr-section\" />\n            <list-and-buy-credit-reports-for-customer initial-data=\"$ctrl.m.customerCreditReports[0]\"></list-and-buy-credit-reports-for-customer>            \n            <list-and-buy-credit-reports-for-customer ng-if=\"$ctrl.m.b.hasCoApplicant\" initial-data=\"$ctrl.m.customerCreditReports[1]\"></list-and-buy-credit-reports-for-customer>\n        </div>\n    </div>\n\n</div>";
    }
    MortgageLoanApplicationDualCreditCheckSharedNs.getDecisionBasisHtmlTemplate = getDecisionBasisHtmlTemplate;
    var DecisionBasisModel = /** @class */ (function () {
        function DecisionBasisModel(addExistingLoan, deleteExistingLoan, additionalCurrentOtherLoans, currentMortgageLoans, isEditAllowed) {
            this.addExistingLoan = addExistingLoan;
            this.deleteExistingLoan = deleteExistingLoan;
            this.additionalCurrentOtherLoans = additionalCurrentOtherLoans;
            this.currentMortgageLoans = currentMortgageLoans;
            this.isEditAllowed = isEditAllowed;
        }
        return DecisionBasisModel;
    }());
    MortgageLoanApplicationDualCreditCheckSharedNs.DecisionBasisModel = DecisionBasisModel;
    function createLoansInitialData(isViewComponent, sourceComponent, apiClient, $q, loansListName, nr, aiModel, backTarget) {
        var isEditAllowed = !isViewComponent && aiModel.IsActive && !aiModel.IsFinalDecisionMade && !aiModel.HasLockedAgreement;
        return ApplicationEditorComponentNs.createInitialData(aiModel.ApplicationNr, aiModel.ApplicationType, backTarget, apiClient, $q, function (x) {
            for (var _i = 0, FieldNames_1 = MortgageLoanApplicationDualCreditCheckSharedNs.FieldNames; _i < FieldNames_1.length; _i++) {
                var fieldName = FieldNames_1[_i];
                x.addComplexApplicationListItem(ComplexApplicationListHelper.getDataSourceItemName(loansListName, nr.toString(), fieldName, ComplexApplicationListHelper.RepeatableCode.No), isViewComponent);
            }
        }, {
            isInPlaceEditAllowed: isEditAllowed,
            afterInPlaceEditsCommited: function () {
                //Milder alternative is to reload ltv data. Also do the same in object in this case
                sourceComponent.signalReloadRequired();
            }
        });
    }
    function initializeSharedDecisionModel(m, hasCoApplicant, aiModel, backTarget, apiClient, $q, isInPlaceEditAllowed, afterInPlaceEditsCommited, backTargetCode, isReadOnly, applicantDataByApplicantNr) {
        var editorOpts = {
            isInPlaceEditAllowed: isInPlaceEditAllowed,
            afterInPlaceEditsCommited: afterInPlaceEditsCommited
        };
        var createEditor = function (setup) { return ApplicationEditorComponentNs.createInitialData(aiModel.ApplicationNr, aiModel.ApplicationType, backTarget, apiClient, $q, setup, editorOpts); };
        var createApplicantBasisFields = function (applicantNr) { return createEditor(function (x) {
            for (var _i = 0, _a = ['hasApprovedCreditCheck', 'isFirstTimeBuyer', 'employment', 'employedSince', 'employer', 'profession', 'employedTo', 'marriage', 'monthlyIncomeSalaryAmount', 'monthlyIncomePensionAmount', 'monthlyIncomeCapitalAmount', 'monthlyIncomeBenefitsAmount', 'monthlyIncomeOtherAmount', 'childrenMinorCount', 'childrenAdultCount', 'costOfLivingRent', 'costOfLivingFees']; _i < _a.length; _i++) {
                var name_1 = _a[_i];
                x.addDataSourceItem('CreditApplicationItem', "applicant".concat(applicantNr, ".").concat(name_1), isReadOnly, true);
            }
            return x;
        }); };
        var applicationBasisFields = createEditor(function (x) {
            x.addDataSourceItem('CreditApplicationItem', 'application.mortgageLoanApplicationType', true, true);
            x.addDataSourceItem('CreditApplicationItem', 'application.requestedLoanAmount', isReadOnly, true);
            x.addDataSourceItem('CreditApplicationItem', 'application.existingMortgageLoanAmount', isReadOnly, true);
            x.addDataSourceItem('CreditApplicationItem', 'application.leftToLiveOn', isReadOnly, true);
            x.addDataSourceItem('CreditApplicationItem', 'application.requestedDueDay', isReadOnly, true);
            x.addDataSourceItem('CreditApplicationItem', 'application.wasHandledByBroker', isReadOnly, true);
            x.addDataSourceItem('CreditApplicationItem', 'application.requestedContactDateAndTime', isReadOnly, true);
            x.addDataSourceItem('CreditApplicationItem', 'application.ownSavingsAmount', isReadOnly, true);
            x.addDataSourceItem('CreditApplicationItem', 'application.consumerBankAccountIban', isReadOnly, true);
        });
        var applicant1BasisFields = createApplicantBasisFields(1);
        var applicant2BasisFields = aiModel.NrOfApplicants > 1 ? createApplicantBasisFields(2) : null;
        var otherApplicationsData = {
            applicationNr: aiModel.ApplicationNr
        };
        var objectCollateralData = {
            applicationNr: aiModel.ApplicationNr,
            onlyMainCollateral: true,
            onlyOtherCollaterals: false,
            allowDelete: false,
            allowViewDetails: true,
            viewDetailsUrlTargetCode: backTargetCode
        };
        var otherCollateralData = {
            applicationNr: aiModel.ApplicationNr,
            onlyMainCollateral: false,
            onlyOtherCollaterals: true,
            allowDelete: false,
            allowViewDetails: true,
            viewDetailsUrlTargetCode: backTargetCode
        };
        m.hasCoApplicant = hasCoApplicant;
        m.applicationBasisFields = applicationBasisFields;
        m.applicant1BasisFields = applicant1BasisFields;
        m.applicant2BasisFields = applicant2BasisFields;
        m.applicant1DetailInfo = "".concat(applicantDataByApplicantNr[1].firstName, ", ").concat(applicantDataByApplicantNr[1].birthDate);
        m.applicant2DetailInfo = hasCoApplicant ? "".concat(applicantDataByApplicantNr[2].firstName, ", ").concat(applicantDataByApplicantNr[2].birthDate) : '';
        m.otherApplicationsData = otherApplicationsData;
        m.otherCollateralData = otherCollateralData;
        m.objectCollateralData = objectCollateralData;
    }
    MortgageLoanApplicationDualCreditCheckSharedNs.initializeSharedDecisionModel = initializeSharedDecisionModel;
    function createDecisionBasisModel(isViewComponent, sourceComponent, apiClient, $q, aiModel, hasCoApplicant, isEditAllowed, backTarget, mortgageLoanNrs, otherLoanNrs, isFinal, applicantDataByApplicantNr) {
        var backTargetCode = isFinal
            ? (isViewComponent ? NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckViewFinal : NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckNewFinal)
            : (isViewComponent ? NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckViewInitial : NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckNewInitial);
        var currentMortgageLoans = [];
        var additionalCurrentOtherLoans = [];
        for (var _i = 0, mortgageLoanNrs_1 = mortgageLoanNrs; _i < mortgageLoanNrs_1.length; _i++) {
            var nr = mortgageLoanNrs_1[_i];
            currentMortgageLoans.push({ d: createLoansInitialData(isViewComponent, sourceComponent, apiClient, $q, MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName, nr, aiModel, backTarget), nr: nr });
        }
        for (var _a = 0, otherLoanNrs_1 = otherLoanNrs; _a < otherLoanNrs_1.length; _a++) {
            var nr = otherLoanNrs_1[_a];
            additionalCurrentOtherLoans.push({ d: createLoansInitialData(isViewComponent, sourceComponent, apiClient, $q, MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName, nr, aiModel, backTarget), nr: nr });
        }
        var m = new MortgageLoanApplicationDualCreditCheckSharedNs.DecisionBasisModel(function (isMortgageLoan, evt) {
            if (evt) {
                evt.preventDefault();
            }
            var currentItems = isMortgageLoan ? currentMortgageLoans : additionalCurrentOtherLoans;
            var currentListName = isMortgageLoan ? MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName : MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName;
            var currentMax = 0;
            for (var _i = 0, currentItems_1 = currentItems; _i < currentItems_1.length; _i++) {
                var c = currentItems_1[_i];
                currentMax = Math.max(c.nr, currentMax);
            }
            var newNr = currentMax + 1;
            apiClient.fetchApplicationInfo(aiModel.ApplicationNr).then(function (ai) {
                var itemName = ComplexApplicationListHelper.getDataSourceItemName(currentListName, newNr.toString(), 'exists', ComplexApplicationListHelper.RepeatableCode.No);
                return apiClient.setApplicationEditItemData(aiModel.ApplicationNr, 'ComplexApplicationList', itemName, 'true', false).then(function (x) {
                    var nr = currentMax + 1;
                    currentItems.push({
                        d: createLoansInitialData(isViewComponent, sourceComponent, apiClient, $q, currentListName, nr, ai, backTarget),
                        nr: nr
                    });
                });
            });
        }, function (isMortgageLoan, nr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            var currentListName = isMortgageLoan ? MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName : MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName;
            ComplexApplicationListHelper.deleteRow(aiModel.ApplicationNr, currentListName, nr, apiClient).then(function (x) {
                sourceComponent.signalReloadRequired();
            });
        }, additionalCurrentOtherLoans, currentMortgageLoans, isEditAllowed);
        var isInPlaceEditAllowed = !isViewComponent && aiModel.IsActive && !aiModel.IsFinalDecisionMade && !aiModel.HasLockedAgreement;
        initializeSharedDecisionModel(m, hasCoApplicant, aiModel, backTarget, apiClient, $q, isInPlaceEditAllowed, function () {
            sourceComponent.signalReloadRequired();
        }, backTargetCode, isViewComponent, applicantDataByApplicantNr);
        return m;
    }
    MortgageLoanApplicationDualCreditCheckSharedNs.createDecisionBasisModel = createDecisionBasisModel;
    function getLtvBasisAndLoanListNrs(sourceComponent, applicationNr, apiClient) {
        var listNames = [MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName, MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName];
        var itemNames = getItemNamesArr(listNames);
        return apiClient.fetchCreditApplicationItemComplex(applicationNr, itemNames, ApplicationDataSourceHelper.MissingItemReplacementValue).then(function (x) {
            var mortgageLoansToSettleAmount = 0;
            var otherLoansToSettleAmount = 0;
            var valuationAmount = [];
            var statValuationAmount = [];
            var priceAmount = [];
            var mortgageLoanNrs = [];
            var otherLoanNrs = [];
            var securityElsewhereAmount = [];
            var housingCompanyLoans = [];
            for (var _i = 0, _a = Object.keys(x); _i < _a.length; _i++) {
                var compoundName = _a[_i];
                var n = ComplexApplicationListHelper.parseCompoundItemName(compoundName);
                var value = x[compoundName];
                if (n.itemName == 'loanTotalAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    if (n.listName === MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName) {
                        mortgageLoansToSettleAmount += sourceComponent.parseDecimalOrNull(value);
                    }
                    else if (n.listName === MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName) {
                        otherLoansToSettleAmount += sourceComponent.parseDecimalOrNull(value);
                    }
                }
                else if (n.itemName == 'valuationAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    valuationAmount.push({ key: sourceComponent.parseDecimalOrNull(n.nr), value: sourceComponent.parseDecimalOrNull(value) });
                }
                else if (n.itemName == 'statValuationAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    statValuationAmount.push({ key: sourceComponent.parseDecimalOrNull(n.nr), value: sourceComponent.parseDecimalOrNull(value) });
                }
                else if (n.itemName == 'priceAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    priceAmount.push({ key: sourceComponent.parseDecimalOrNull(n.nr), value: sourceComponent.parseDecimalOrNull(value) });
                }
                else if (n.itemName == 'securityElsewhereAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    securityElsewhereAmount.push(sourceComponent.parseDecimalOrNull(value));
                }
                else if (n.itemName == 'housingCompanyLoans' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    housingCompanyLoans.push(sourceComponent.parseDecimalOrNull(value));
                }
                else if (n.itemName === 'exists' && value === 'true') {
                    if (n.listName === MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName) {
                        mortgageLoanNrs.push(parseInt(n.nr));
                    }
                    else if (n.listName === MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName) {
                        otherLoanNrs.push(parseInt(n.nr));
                    }
                }
            }
            return {
                valuationAmount: valuationAmount,
                statValuationAmount: statValuationAmount,
                priceAmount: priceAmount,
                mortgageLoanNrs: mortgageLoanNrs,
                otherLoanNrs: otherLoanNrs,
                mortgageLoansToSettleAmount: mortgageLoansToSettleAmount,
                otherLoansToSettleAmount: otherLoansToSettleAmount,
                securityElsewhereAmount: securityElsewhereAmount,
                housingCompanyLoans: housingCompanyLoans
            };
        });
    }
    MortgageLoanApplicationDualCreditCheckSharedNs.getLtvBasisAndLoanListNrs = getLtvBasisAndLoanListNrs;
    function getItemNamesArr(listNames) {
        var itemNames = ["ApplicationObject#*#u#valuationAmount"];
        itemNames.push("ApplicationObject#*#u#statValuationAmount");
        itemNames.push("ApplicationObject#*#u#priceAmount");
        itemNames.push("ApplicationObject#*#u#securityElsewhereAmount");
        itemNames.push("ApplicationObject#*#u#housingCompanyLoans");
        for (var _i = 0, listNames_1 = listNames; _i < listNames_1.length; _i++) {
            var listName = listNames_1[_i];
            itemNames.push("".concat(listName, "#*#*#loanTotalAmount"));
            itemNames.push("".concat(listName, "#*#*#loanShouldBeSettled"));
            itemNames.push("".concat(listName, "#*#u#exists"));
        }
        return itemNames;
    }
    function getCustomerCreditHistoryByApplicationNr(applicationNr, apiClient) {
        return apiClient.fetchApplicationInfoWithApplicants(applicationNr)
            .then(function (x) {
            var customerIds = Object.keys(x.CustomerIdByApplicantNr).map(function (i) { return x.CustomerIdByApplicantNr[i]; });
            return apiClient.fetchCreditHistoryByCustomerId(customerIds)
                .then(function (res) {
                var credits = [];
                for (var _i = 0, _a = res["credits"]; _i < _a.length; _i++) {
                    var credit = _a[_i];
                    credits.push({
                        CreditNr: credit.CreditNr,
                        CapitalBalance: credit.CapitalBalance
                    });
                }
                return credits;
            });
        });
    }
    MortgageLoanApplicationDualCreditCheckSharedNs.getCustomerCreditHistoryByApplicationNr = getCustomerCreditHistoryByApplicationNr;
    function getApplicantDataByApplicantNr(applicationNr, hasCoApplicant, apiClient) {
        var applicantNrs = (hasCoApplicant ? [1, 2] : [1]);
        var names = [];
        for (var _i = 0, applicantNrs_1 = applicantNrs; _i < applicantNrs_1.length; _i++) {
            var applicantNr = applicantNrs_1[_i];
            names.push("a".concat(applicantNr, ".firstName"));
            names.push("a".concat(applicantNr, ".birthDate"));
        }
        return apiClient.fetchApplicationDataSourceItems(applicationNr, [{
                DataSourceName: 'CustomerCardItem',
                MissingItemReplacementValue: '-',
                ReplaceIfMissing: true,
                Names: names,
                ErrorIfMissing: false,
                IncludeEditorModel: false,
                IncludeIsChanged: false
            }]).then(function (x) {
            var result = NTechPreCreditApi.FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items);
            var d = {};
            for (var _i = 0, applicantNrs_2 = applicantNrs; _i < applicantNrs_2.length; _i++) {
                var applicantNr = applicantNrs_2[_i];
                d[applicantNr] = {
                    firstName: result["a".concat(applicantNr, ".firstName")],
                    birthDate: result["a".concat(applicantNr, ".birthDate")],
                };
            }
            return d;
        });
    }
    MortgageLoanApplicationDualCreditCheckSharedNs.getApplicantDataByApplicantNr = getApplicantDataByApplicantNr;
})(MortgageLoanApplicationDualCreditCheckSharedNs || (MortgageLoanApplicationDualCreditCheckSharedNs = {}));
