var NTechComponents;
(function (NTechComponents) {
    var NTechComponentService = /** @class */ (function () {
        function NTechComponentService(trafficCop, ntechLog) {
            this.trafficCop = trafficCop;
            this.ntechLog = ntechLog;
            this.reloadEventHost = new NTechComponents.NTechEventHost();
            this.ntechEventHost = new NTechComponents.NTechEventHost();
        }
        NTechComponentService.prototype.isLoading = function () {
            return this.trafficCop.pending.all > 0;
        };
        NTechComponentService.prototype.signalReloadRequired = function (context) {
            this.reloadEventHost.signalEvent(context);
        };
        NTechComponentService.prototype.subscribeToReloadRequired = function (onReloadRequired) {
            return this.reloadEventHost.subscribeToEvent(onReloadRequired);
        };
        NTechComponentService.prototype.unSubscribeFromReloadRequired = function (removalId) {
            this.reloadEventHost.unSubscribeFromEvent(removalId);
        };
        NTechComponentService.prototype.subscribeToNTechEvents = function (onEvent) {
            return this.ntechEventHost.subscribeToEvent(onEvent);
        };
        NTechComponentService.prototype.emitNTechEvent = function (eventName, eventData) {
            this.ntechEventHost.signalEvent({ eventName: eventName, eventData: eventData });
        };
        NTechComponentService.prototype.emitNTechCustomDataEvent = function (eventName, customData) {
            this.ntechEventHost.signalEvent({ eventName: eventName, eventData: 'customData', customData: customData });
        };
        NTechComponentService.prototype.unSubscribeFromNTechEvents = function (removalId) {
            this.ntechEventHost.unSubscribeFromEvent(removalId);
        };
        NTechComponentService.prototype.setFocus = function (controlAlias) {
            this.emitNTechEvent('FocusControlByAlias', controlAlias);
        };
        NTechComponentService.prototype.createGuid = function () {
            var s4 = function () { return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1); };
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
        };
        //Based on https://github.com/neosmart/UrlBase64/blob/master/UrlBase64/UrlBase64.cs
        NTechComponentService.prototype.toUrlSafeBase64String = function (data) {
            var encoded = btoa(JSON.stringify(data)).replace('+', '-').replace('/', '_');
            while (encoded[encoded.length - 1] === '=') {
                encoded = encoded.substr(0, encoded.length - 1);
            }
            return encoded;
        };
        NTechComponentService.prototype.fromUrlSafeBase64String = function (data) {
            if (!data) {
                return null;
            }
            var decodeFirstPass = function () {
                var decoded = '';
                for (var _i = 0, data_1 = data; _i < data_1.length; _i++) {
                    var c = data_1[_i];
                    if (c === '_') {
                        decoded += '/';
                    }
                    else if (c === '-') {
                        decoded += '+';
                    }
                    else {
                        decoded += c;
                    }
                }
                switch (decoded.length % 4) {
                    case 2: return decoded + '==';
                    case 3: return decoded + '=';
                    default: return decoded;
                }
            };
            var d = decodeFirstPass();
            return JSON.parse(atob(d));
        };
        NTechComponentService.prototype.createCrossModuleNavigationTargetCode = function (targetName, targetContext) {
            if (targetName == null)
                return null;
            return "t-" + this.toUrlSafeBase64String({ targetName: targetName, targetContext: targetContext });
        };
        NTechComponentService.prototype.getQueryStringParameterByName = function (name) {
            name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
            var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"), results = regex.exec(window.location.search);
            return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
        };
        NTechComponentService.$inject = ['trafficCop', 'ntechLog'];
        return NTechComponentService;
    }());
    NTechComponents.NTechComponentService = NTechComponentService;
    var NTechEvent = /** @class */ (function () {
        function NTechEvent() {
        }
        return NTechEvent;
    }());
    NTechComponents.NTechEvent = NTechEvent;
    var ReloadRequiredContext = /** @class */ (function () {
        function ReloadRequiredContext() {
        }
        return ReloadRequiredContext;
    }());
    NTechComponents.ReloadRequiredContext = ReloadRequiredContext;
    var TestFunctionSet = /** @class */ (function () {
        function TestFunctionSet() {
            this.functions = [];
        }
        TestFunctionSet.prototype.add = function (title, execute) {
            if (!this.functions) {
                this.functions = [];
            }
            this.functions.push({ title: title, execute: execute });
        };
        TestFunctionSet.prototype.clear = function () {
            this.functions = [];
        };
        return TestFunctionSet;
    }());
    NTechComponents.TestFunctionSet = TestFunctionSet;
    var TestFunction = /** @class */ (function () {
        function TestFunction() {
        }
        return TestFunction;
    }());
    NTechComponents.TestFunction = TestFunction;
    var NTechApiClientBase = /** @class */ (function () {
        function NTechApiClientBase(onError, $http, $q) {
            this.onError = onError;
            this.$http = $http;
            this.$q = $q;
            this.activePostCount = 0;
            this.loggingContext = 'unknown';
        }
        NTechApiClientBase.prototype.post = function (url, data) {
            var _this = this;
            this.activePostCount++;
            var d = this.$q.defer();
            this.$http.post(url, data).then(function (result) {
                d.resolve(result.data);
            }, function (err) {
                if (_this.onError) {
                    _this.onError(err.statusText);
                }
                d.reject(err.statusText);
            }).finally(function () {
                _this.activePostCount--;
            });
            return d.promise;
        };
        NTechApiClientBase.prototype.isLoading = function () {
            return this.activePostCount > 0;
        };
        NTechApiClientBase.prototype.map = function (p, f) {
            var deferred = this.$q.defer();
            p.then(function (s) {
                deferred.resolve(f(s));
            }, function (e) {
                deferred.reject(e);
            });
            return deferred.promise;
        };
        return NTechApiClientBase;
    }());
    NTechComponents.NTechApiClientBase = NTechApiClientBase;
})(NTechComponents || (NTechComponents = {}));
//# sourceMappingURL=ntech.componentservice.js.map