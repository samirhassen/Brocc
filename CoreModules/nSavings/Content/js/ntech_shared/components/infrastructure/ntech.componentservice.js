var NTechComponents;
(function (NTechComponents) {
    class NTechComponentService {
        constructor(trafficCop, ntechLog) {
            this.trafficCop = trafficCop;
            this.ntechLog = ntechLog;
            this.reloadEventHost = new NTechComponents.NTechEventHost();
            this.ntechEventHost = new NTechComponents.NTechEventHost();
        }
        isLoading() {
            return this.trafficCop.pending.all > 0;
        }
        signalReloadRequired(context) {
            this.reloadEventHost.signalEvent(context);
        }
        subscribeToReloadRequired(onReloadRequired) {
            return this.reloadEventHost.subscribeToEvent(onReloadRequired);
        }
        unSubscribeFromReloadRequired(removalId) {
            this.reloadEventHost.unSubscribeFromEvent(removalId);
        }
        subscribeToNTechEvents(onEvent) {
            return this.ntechEventHost.subscribeToEvent(onEvent);
        }
        emitNTechEvent(eventName, eventData) {
            this.ntechEventHost.signalEvent({ eventName: eventName, eventData: eventData });
        }
        emitNTechCustomDataEvent(eventName, customData) {
            this.ntechEventHost.signalEvent({ eventName: eventName, eventData: 'customData', customData: customData });
        }
        unSubscribeFromNTechEvents(removalId) {
            this.ntechEventHost.unSubscribeFromEvent(removalId);
        }
        setFocus(controlAlias) {
            this.emitNTechEvent('FocusControlByAlias', controlAlias);
        }
        createGuid() {
            let s4 = () => Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
        }
        //Based on https://github.com/neosmart/UrlBase64/blob/master/UrlBase64/UrlBase64.cs
        toUrlSafeBase64String(data) {
            let encoded = btoa(JSON.stringify(data)).replace('+', '-').replace('/', '_');
            while (encoded[encoded.length - 1] === '=') {
                encoded = encoded.substr(0, encoded.length - 1);
            }
            return encoded;
        }
        fromUrlSafeBase64String(data) {
            if (!data) {
                return null;
            }
            let decodeFirstPass = () => {
                let decoded = '';
                for (let c of data) {
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
            let d = decodeFirstPass();
            return JSON.parse(atob(d));
        }
        createCrossModuleNavigationTargetCode(targetName, targetContext) {
            if (targetName == null)
                return null;
            return "t-" + this.toUrlSafeBase64String({ targetName, targetContext });
        }
        getQueryStringParameterByName(name) {
            name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
            var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"), results = regex.exec(window.location.search);
            return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
        }
    }
    NTechComponentService.$inject = ['trafficCop', 'ntechLog'];
    NTechComponents.NTechComponentService = NTechComponentService;
    class NTechEvent {
    }
    NTechComponents.NTechEvent = NTechEvent;
    class ReloadRequiredContext {
    }
    NTechComponents.ReloadRequiredContext = ReloadRequiredContext;
    class TestFunctionSet {
        constructor() {
            this.functions = [];
        }
        add(title, execute) {
            if (!this.functions) {
                this.functions = [];
            }
            this.functions.push({ title: title, execute: execute });
        }
        clear() {
            this.functions = [];
        }
    }
    NTechComponents.TestFunctionSet = TestFunctionSet;
    class TestFunction {
    }
    NTechComponents.TestFunction = TestFunction;
    class NTechApiClientBase {
        constructor(onError, $http, $q) {
            this.onError = onError;
            this.$http = $http;
            this.$q = $q;
            this.activePostCount = 0;
            this.loggingContext = 'unknown';
        }
        post(url, data) {
            this.activePostCount++;
            let d = this.$q.defer();
            this.$http.post(url, data).then((result) => {
                d.resolve(result.data);
            }, err => {
                if (this.onError) {
                    this.onError(err.statusText);
                }
                d.reject(err.statusText);
            }).finally(() => {
                this.activePostCount--;
            });
            return d.promise;
        }
        isLoading() {
            return this.activePostCount > 0;
        }
        map(p, f) {
            let deferred = this.$q.defer();
            p.then(s => {
                deferred.resolve(f(s));
            }, e => {
                deferred.reject(e);
            });
            return deferred.promise;
        }
    }
    NTechComponents.NTechApiClientBase = NTechApiClientBase;
})(NTechComponents || (NTechComponents = {}));
