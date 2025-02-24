var NTechComponents;
(function (NTechComponents) {
    var CountsModel = /** @class */ (function () {
        function CountsModel() {
        }
        return CountsModel;
    }());
    NTechComponents.CountsModel = CountsModel;
    var NTechHttpTrafficCopService = /** @class */ (function () {
        function NTechHttpTrafficCopService() {
            //Using solution from: https://www.bennadel.com/blog/2777-monitoring-http-activity-with-http-interceptors-in-angularjs.htm
            this.total = {
                all: 0,
                get: 0,
                post: 0,
                delete: 0,
                put: 0,
                head: 0
            };
            this.pending = {
                all: 0,
                get: 0,
                post: 0,
                delete: 0,
                put: 0,
                head: 0
            };
            this.listeners = [];
        }
        NTechHttpTrafficCopService.prototype.addStateChangeListener = function (onStateChange) {
            this.listeners.push(onStateChange);
        };
        NTechHttpTrafficCopService.prototype.endRequest = function (httpMethod) {
            var httpMethodN = this.normalizedHttpMethod(httpMethod);
            this.pending.all--;
            this.pending[httpMethodN]--;
            // EDGE CASE: In the unlikely event that the interceptors were not
            // able to obtain the config object; or, the method was changed after
            // our interceptor reached it, there's a chance that our numbers will
            // be off. In such a case, we want to try to redistribute negative
            // counts onto other properties.
            if (this.pending[httpMethodN] < 0) {
                this.redistributePendingCounts(httpMethodN);
            }
            for (var _i = 0, _a = this.listeners; _i < _a.length; _i++) {
                var a = _a[_i];
                a();
            }
        };
        NTechHttpTrafficCopService.prototype.startRequest = function (httpMethod) {
            var httpMethodN = this.normalizedHttpMethod(httpMethod);
            this.total.all++;
            this.total[httpMethod]++;
            this.pending.all++;
            this.pending[httpMethod]++;
            for (var _i = 0, _a = this.listeners; _i < _a.length; _i++) {
                var a = _a[_i];
                a();
            }
        };
        NTechHttpTrafficCopService.prototype.normalizedHttpMethod = function (httpMethod) {
            var n = (httpMethod || "").toLowerCase();
            switch (n) {
                case "get":
                case "post":
                case "delete":
                case "put":
                case "head":
                    return (n);
            }
            return ("get");
        };
        // I attempt to redistribute an [unexpected] negative count to other
        // non-negative counts. The HTTP methods are iterated in likelihood of
        // execution. And, while this isn't an exact science, it will normalize
        // after all HTTP requests have finished processing.
        NTechHttpTrafficCopService.prototype.redistributePendingCounts = function (negativeMethod) {
            var overflow = Math.abs(this.pending[negativeMethod]);
            this.pending[negativeMethod] = 0;
            // List in likely order of precedence in the application.
            var methods = ["get", "post", "delete", "put", "head"];
            // Trickle the overflow across the list of methods.
            for (var i = 0; i < methods.length; i++) {
                var method = methods[i];
                if (overflow && this.pending[method]) {
                    this.pending[method] -= overflow;
                    if (this.pending[method] < 0) {
                        overflow = Math.abs(this.pending[method]);
                        this.pending[method] = 0;
                    }
                    else {
                        overflow = 0;
                    }
                }
            }
        };
        return NTechHttpTrafficCopService;
    }());
    NTechComponents.NTechHttpTrafficCopService = NTechHttpTrafficCopService;
})(NTechComponents || (NTechComponents = {}));
//# sourceMappingURL=ntech.httptrafficcopservice.js.map