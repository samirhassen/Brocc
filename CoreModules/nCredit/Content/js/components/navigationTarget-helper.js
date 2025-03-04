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
        else {
            return null;
        }
    }
    NavigationTargetHelper.createTargetFromComponentHostToHere = createTargetFromComponentHostToHere;
    function createCodeTarget(targetCode, context) {
        if (!context) {
            return { targetCode: targetCode };
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
        NavigationTargetCode["Credit"] = "Credit";
        NavigationTargetCode["UnplacedPayments"] = "UnplacedPayments";
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
            if (c == NavigationTargetCode.Credit && context && context.creditNr) {
                return getLocalModuleUrl('/Ui/Credit', [['creditNr', context.creditNr]]);
            }
            else if (c == NavigationTargetCode.UnplacedPayments) {
                return getLocalModuleUrl('/Ui/UnplacedPayments/List');
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
    function createCodeOrUrlFromInitialData(initialData, context) {
        var backUrl = initialData ? initialData.backUrl : null;
        var backTarget = initialData ? initialData.backTarget : null;
        return create(backUrl, backTarget, context);
    }
    NavigationTargetHelper.createCodeOrUrlFromInitialData = createCodeOrUrlFromInitialData;
    function handleBackWithInitialDataDefaults(initialData, apiClient, $q, context) {
        var t = createCodeOrUrlFromInitialData(initialData, context);
        return handleBack(t, apiClient, $q, context);
    }
    NavigationTargetHelper.handleBackWithInitialDataDefaults = handleBackWithInitialDataDefaults;
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
