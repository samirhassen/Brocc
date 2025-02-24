var NavigationTargetHelper;
(function (NavigationTargetHelper) {
    function createUrlTarget(url) {
        return { targetUrl: url };
    }
    NavigationTargetHelper.createUrlTarget = createUrlTarget;
    function createTargetFromComponentHostToHere(i) {
        if (i.navigationTargetCodeToHere) {
            return createCodeTarget(i.navigationTargetCodeToHere);
        }
        else if (i.urlToHere) {
            return createUrlTarget(i.urlToHere);
        }
        else {
            return null;
        }
    }
    NavigationTargetHelper.createTargetFromComponentHostToHere = createTargetFromComponentHostToHere;
    function createCodeTarget(targetCode, context) {
        if (!context) {
            return { targetCode: targetCode };
        }
        else if (targetCode === NavigationTargetCode.MortgageLoanEditCollateral && context.listNr) {
            return { targetCode: targetCode + '_' + context.listNr };
        }
        else {
            return { targetCode: targetCode };
        }
    }
    NavigationTargetHelper.createCodeTarget = createCodeTarget;
    function create(backUrl, targetCode, context) {
        if (targetCode) {
            return createCodeTarget(targetCode, context);
        }
        else if (backUrl) {
            return createUrlTarget(backUrl);
        }
        else {
            return null;
        }
    }
    NavigationTargetHelper.create = create;
    function createCodeOrUrlFromInitialData(initialData, context, defaultTarget) {
        var backUrl = initialData ? initialData.backUrl : null;
        var backTarget = initialData ? initialData.backTarget : null;
        if (!backTarget && !backUrl) {
            backTarget = defaultTarget;
        }
        return create(backUrl, backTarget, context);
    }
    NavigationTargetHelper.createCodeOrUrlFromInitialData = createCodeOrUrlFromInitialData;
    function createCrossModule(targetName, targetContext) {
        var code = NTechNavigationTarget.createCrossModuleNavigationTargetCode(targetName, targetContext);
        return create(null, code, null);
    }
    NavigationTargetHelper.createCrossModule = createCrossModule;
    function handleBackWithInitialDataDefaults(initialData, apiClient, $q, context, defaultTarget) {
        var t = createCodeOrUrlFromInitialData(initialData, context, defaultTarget);
        return handleBack(t, apiClient, $q, context);
    }
    NavigationTargetHelper.handleBackWithInitialDataDefaults = handleBackWithInitialDataDefaults;
    function AppendBackNavigationToUrl(url, target) {
        if (!target) {
            return url;
        }
        if (!target.targetCode && !target.targetUrl) {
            return url;
        }
        if (url.indexOf('backUrl') > 0) {
            return url;
        }
        if (url.indexOf('backTarget') > 0) {
            return url;
        }
        var newUrl = url + (url.indexOf('?') < 0 ? '?' : '&');
        if (target.targetCode) {
            newUrl += "backTarget=".concat(decodeURIComponent(encodeURIComponent(target.targetCode)));
        }
        else {
            newUrl += "backUrl=".concat(decodeURIComponent(encodeURIComponent(target.targetUrl)));
        }
        return newUrl;
    }
    NavigationTargetHelper.AppendBackNavigationToUrl = AppendBackNavigationToUrl;
    var CodeOrUrl = /** @class */ (function () {
        function CodeOrUrl() {
        }
        return CodeOrUrl;
    }());
    NavigationTargetHelper.CodeOrUrl = CodeOrUrl;
    var NavigationTargetCode;
    (function (NavigationTargetCode) {
        NavigationTargetCode["MortgageLoanCreditCheckNewInitial"] = "MortgageLoanCreditCheckNewInitial";
        NavigationTargetCode["MortgageLoanCreditCheckNewFinal"] = "MortgageLoanCreditCheckNewFinal";
        NavigationTargetCode["MortgageLoanCreditCheckViewInitial"] = "MortgageLoanCreditCheckViewInitial";
        NavigationTargetCode["MortgageLoanCreditCheckViewFinal"] = "MortgageLoanCreditCheckViewFinal";
        NavigationTargetCode["MortgageLoanApplication"] = "MortgageLoanApplication";
        NavigationTargetCode["MortgageLoanSearch"] = "MortgageLoanSearch";
        NavigationTargetCode["MortgageLoanCreateLeadWorkList"] = "MortgageLoanCreateLeadWorkList";
        NavigationTargetCode["MortgageLoanEditCollateral"] = "MortgageLoanEditCollateral";
        NavigationTargetCode["MortgageLoanHandleSettlement"] = "MortgageLoanHandleSettlement";
        NavigationTargetCode["UnsecuredLoanCreditCheckNewInitial"] = "UnsecuredLoanCreditCheckNewInitial";
        NavigationTargetCode["UnsecuredLoanCreditCheckViewInitial"] = "UnsecuredLoanCreditCheckViewInitial";
        NavigationTargetCode["UnsecuredLoanApplication"] = "UnsecuredLoanApplication";
        NavigationTargetCode["UnsecuredLoanApplications"] = "UnsecuredLoanApplications";
        NavigationTargetCode["MortgageLoanLead"] = "MortgageLoanLead";
        NavigationTargetCode["MortgageLoanApplications"] = "MortgageLoanApplications";
        NavigationTargetCode["CompanyLoanSearch"] = "CompanyLoanSearch";
        NavigationTargetCode["UllApplicationBasis"] = "UllApplicationBasis";
    })(NavigationTargetCode = NavigationTargetHelper.NavigationTargetCode || (NavigationTargetHelper.NavigationTargetCode = {}));
    function resolveNavigationUrl(codeOrUrl, context) {
        if (!codeOrUrl) {
            return null;
        }
        if (codeOrUrl.targetCode) {
            var c = codeOrUrl.targetCode;
            if (c.length > 2 && c.substr(0, 2) === 't-') {
                return getLocalModuleUrl('/Ui/Gateway/nBackOffice/Ui/CrossModuleNavigate', [['targetCode', codeOrUrl.targetCode]]);
            }
            if (c === NavigationTargetCode.MortgageLoanCreditCheckNewInitial || c === NavigationTargetCode.MortgageLoanCreditCheckNewFinal) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr");
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/NewCreditCheck', [['applicationNr', context.applicationNr], ['scoringWorkflowStepName', c === NavigationTargetCode.MortgageLoanCreditCheckNewFinal ? 'FinalCreditCheck' : 'InitialCreditCheck']]);
            }
            if (c === NavigationTargetCode.MortgageLoanCreditCheckViewInitial || c === NavigationTargetCode.MortgageLoanCreditCheckViewFinal) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr");
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/ViewCreditCheckDetails', [['applicationNr', context.applicationNr], ['scoringWorkflowStepName', c === NavigationTargetCode.MortgageLoanCreditCheckViewFinal ? 'FinalCreditCheck' : 'InitialCreditCheck']]);
            }
            else if (c === NavigationTargetCode.MortgageLoanApplication) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr");
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/Application', [['applicationNr', context.applicationNr]]);
            }
            else if (c === NavigationTargetCode.MortgageLoanLead) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr");
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/Lead', [['applicationNr', context.applicationNr], ['workListId', context.workListId]]);
            }
            else if (c === NavigationTargetCode.MortgageLoanSearch) {
                return getLocalModuleUrl('/Ui/MortgageLoan/Search', [['tabName', 'search']]);
            }
            else if (c == NavigationTargetCode.MortgageLoanCreateLeadWorkList) {
                return getLocalModuleUrl('/Ui/MortgageLoan/Search', [['tabName', 'createWorkList']]);
            }
            else if (c == NavigationTargetCode.MortgageLoanApplications) {
                return getLocalModuleUrl('/Ui/MortgageLoan/Search', [['tabName', 'workList']]);
            }
            else if (c === NavigationTargetCode.MortgageLoanEditCollateral || startsWith(c, NavigationTargetCode.MortgageLoanEditCollateral + '_')) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr");
                }
                var listNr = context.listNr;
                if (!listNr && c.length > NavigationTargetCode.MortgageLoanEditCollateral.length) {
                    listNr = c.substring(NavigationTargetCode.MortgageLoanEditCollateral.length + 1);
                }
                else {
                    throw new Error("Missing listNr");
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/Edit-Collateral', [['applicationNr', context.applicationNr], ['listNr', listNr]]);
            }
            else if (c === NavigationTargetCode.MortgageLoanHandleSettlement) {
                return getLocalModuleUrl('/Ui/MortgageLoan/Handle-Settlement', [['applicationNr', context.applicationNr]]);
            }
            else if (c === NavigationTargetCode.UnsecuredLoanCreditCheckNewInitial) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr");
                }
                return getLocalModuleUrl('/CreditCheck/New', [['applicationNr', context.applicationNr]]);
            }
            else if (c === NavigationTargetCode.UnsecuredLoanCreditCheckViewInitial) {
                if (!context || !context.creditDecisionId) {
                    throw new Error("Missing creditDecisionId");
                }
                return getLocalModuleUrl('/CreditCheck/View', [['id', context.creditDecisionId]]);
            }
            else if (c === NavigationTargetCode.UnsecuredLoanApplication) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr");
                }
                return getLocalModuleUrl('/CreditManagement/CreditApplication', [['applicationNr', context.applicationNr]]);
            }
            else if (c == NavigationTargetCode.UnsecuredLoanApplications) {
                return getLocalModuleUrl('/CreditManagement/CreditApplications');
            }
            else if (c == NavigationTargetCode.CompanyLoanSearch) {
                return getLocalModuleUrl('/Ui/CompanyLoan/Search');
            }
            else if (c === NavigationTargetCode.UllApplicationBasis) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr");
                }
                return getLocalModuleUrl("/Ui/Gateway/nBackOffice/s/loan-application/application-basis/".concat(context.applicationNr));
            }
            else {
                throw new Error("Invalid navigation target");
            }
        }
        if (initialData && initialData.disableBackUrlSupport) {
            return null;
        }
        else {
            return codeOrUrl.targetUrl;
        }
    }
    NavigationTargetHelper.resolveNavigationUrl = resolveNavigationUrl;
    function handleBack(codeOrUrl, apiClient, $q, context) {
        var getUrl;
        var url = resolveNavigationUrl(codeOrUrl, context);
        if (!url) {
            getUrl = apiClient.getUserModuleUrl('nBackOffice', '/').then(function (x) { return x.Url; });
        }
        else {
            var deferred = $q.defer();
            deferred.resolve(url);
            getUrl = deferred.promise;
        }
        return getUrl.then(function (x) {
            document.location.href = x;
        });
    }
    NavigationTargetHelper.handleBack = handleBack;
    function tryNavigateTo(code, context) {
        var url = resolveNavigationUrl(createCodeTarget(code, context), context);
        if (!url) {
            return false;
        }
        else {
            document.location.href = url;
            return true;
        }
    }
    NavigationTargetHelper.tryNavigateTo = tryNavigateTo;
    //TODO: Share with controllerbase
    function startsWith(s, prefix) {
        if (!s) {
            return false;
        }
        return s.substring(0, prefix.length) === prefix;
    }
    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null)
            return true;
        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        }
        else {
            return false;
        }
    }
    function getLocalModuleUrl(moduleLocalPath, queryStringParameters) {
        if (moduleLocalPath[0] === '/') {
            moduleLocalPath = moduleLocalPath.substring(1);
        }
        var p = "/".concat(moduleLocalPath);
        if (queryStringParameters) {
            var s = moduleLocalPath.indexOf('?') >= 0 ? '&' : '?';
            for (var _i = 0, queryStringParameters_1 = queryStringParameters; _i < queryStringParameters_1.length; _i++) {
                var q = queryStringParameters_1[_i];
                if (!isNullOrWhitespace(q[1])) {
                    p += "".concat(s).concat(q[0], "=").concat(encodeURIComponent(decodeURIComponent(q[1])));
                    s = '&';
                }
            }
        }
        return p;
    }
})(NavigationTargetHelper || (NavigationTargetHelper = {}));
