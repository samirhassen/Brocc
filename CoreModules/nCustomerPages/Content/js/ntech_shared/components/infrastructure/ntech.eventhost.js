var NTechComponents;
(function (NTechComponents) {
    var NTechEventHost = /** @class */ (function () {
        function NTechEventHost() {
            this.subs = {};
        }
        NTechEventHost.prototype.signalEvent = function (e) {
            for (var _i = 0, _a = Object.keys(this.subs); _i < _a.length; _i++) {
                var id = _a[_i];
                var f = this.subs[id];
                if (f) {
                    f(e);
                }
            }
        };
        NTechEventHost.prototype.subscribeToOneEventOnly = function (predicate, onEvent) {
            var _this = this;
            var removalId = this.subscribeToEvent(function (evt) {
                if (predicate(evt)) {
                    _this.unSubscribeFromEvent(removalId);
                    onEvent();
                }
            });
        };
        NTechEventHost.prototype.subscribeToEvent = function (onEvent) {
            var removalId = this.createGuid();
            this.subs[removalId] = onEvent;
            return removalId;
        };
        NTechEventHost.prototype.unSubscribeFromEvent = function (removalId) {
            this.subs[removalId] = null;
        };
        NTechEventHost.prototype.createGuid = function () {
            var s4 = function () { return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1); };
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
        };
        return NTechEventHost;
    }());
    NTechComponents.NTechEventHost = NTechEventHost;
})(NTechComponents || (NTechComponents = {}));
//# sourceMappingURL=ntech.eventhost.js.map