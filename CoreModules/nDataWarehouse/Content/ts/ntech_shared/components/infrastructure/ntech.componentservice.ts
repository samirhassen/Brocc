namespace NTechComponents {
    export class NTechComponentService {
        private reloadEventHost = new NTechEventHost<ReloadRequiredContext>()
        private ntechEventHost = new NTechEventHost<NTechEvent>()

        static $inject = ['trafficCop', 'ntechLog']
        constructor(private trafficCop: NTechHttpTrafficCopService, public ntechLog: NTechLoggingService) {

        }

        isLoading() {
            return this.trafficCop.pending.all > 0
        }

        signalReloadRequired(context: ReloadRequiredContext) {
            this.reloadEventHost.signalEvent(context)
        }

        subscribeToReloadRequired(onReloadRequired: ((context: ReloadRequiredContext) => void)): string {
            return this.reloadEventHost.subscribeToEvent(onReloadRequired)
        }

        unSubscribeFromReloadRequired(removalId: string) {
            this.reloadEventHost.unSubscribeFromEvent(removalId)
        }

        subscribeToNTechEvents(onEvent: ((evt: NTechEvent) => void)): string {
            return this.ntechEventHost.subscribeToEvent(onEvent);
        }

        emitNTechEvent(eventName: string, eventData: string) {
            this.ntechEventHost.signalEvent({ eventName: eventName, eventData: eventData })
        }

        emitNTechCustomDataEvent<T>(eventName: string, customData: T) {
            this.ntechEventHost.signalEvent({ eventName: eventName, eventData: 'customData', customData: customData })
        }

        unSubscribeFromNTechEvents(removalId: string) {
            this.ntechEventHost.unSubscribeFromEvent(removalId)
        }

        setFocus(controlAlias: string) {
            this.emitNTechEvent('FocusControlByAlias', controlAlias)
        }

        private createGuid() {
            let s4 = () => Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
        }

        //Based on https://github.com/neosmart/UrlBase64/blob/master/UrlBase64/UrlBase64.cs

        toUrlSafeBase64String<T>(data: T): string {
            let encoded = btoa(JSON.stringify(data)).replace('+', '-').replace('/', '_')
            while(encoded[encoded.length - 1] === '=') {
                encoded = encoded.substr(0, encoded.length - 1)
            }        
            return encoded;
        }

        fromUrlSafeBase64String<T>(data: string): T {
            if(!data) {
                return null
            }
            let decodeFirstPass = () =>
            {
                let decoded = ''
                for(let c of data) {
                    if(c === '_') {
                        decoded += '/'
                    } else if(c === '-') {
                        decoded += '+'
                    } else {
                        decoded += c
                    }
                }
                switch(decoded.length % 4) {
                    case 2: return decoded + '=='
                    case 3: return decoded + '='
                    default: return decoded
                }
            }

            let d = decodeFirstPass()
            return JSON.parse(atob(d))
        }

        createCrossModuleNavigationTargetCode(targetName: string, targetContext: { [key: string]: string} ) : string {
            if (targetName == null)
                return null

            return "t-" + this.toUrlSafeBase64String({ targetName, targetContext })
        }

        getQueryStringParameterByName(name: string): string {
            name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
            var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
                results = regex.exec(window.location.search);
            return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
        }
    }

    export class NTechEvent {
        eventName: string
        eventData: string
        customData?: any
    }

    export class ReloadRequiredContext {
        sourceComponentName: string
    }


    export class TestFunctionSet {
        constructor() {

        }
        functions: TestFunction[] = []

        add(title: string, execute: ((evt: Event) => void)) {
            if (!this.functions) {
                this.functions = []
            }
            this.functions.push({ title: title, execute: execute })
        }

        clear() {
            this.functions = []
        }
    }

    export class TestFunction {
        title: string
        execute: (evt: Event) => void
    }

    export abstract class NTechApiClientBase {
        constructor(private onError: ((errorMessage: string) => void),
            protected $http: ng.IHttpService,
            protected $q: ng.IQService) {

        }

        private activePostCount: number = 0
        public loggingContext: string = 'unknown'

        protected post<TRequest, TResult>(url: string, data: TRequest): ng.IPromise<TResult> {
            this.activePostCount++;
            let d: ng.IDeferred<TResult> = this.$q.defer()
            this.$http.post(url, data).then((result: ng.IHttpResponse<TResult>) => {
                d.resolve(result.data)
            }, err => {
                if (this.onError) {
                    this.onError(err.statusText)
                }
                d.reject(err.statusText)
            }).finally(() => {
                this.activePostCount--;
            })
            return d.promise
        }

        public isLoading() {
            return this.activePostCount > 0;
        }

        public map<TSource, TTarget>(p: ng.IPromise<TSource>, f: (s: TSource) => TTarget): ng.IPromise<TTarget> {
            let deferred = this.$q.defer<TTarget>()
            p.then(s => {
                deferred.resolve(f(s))
            }, e => {
                deferred.reject(e)
            })
            return deferred.promise
        }
    }
}