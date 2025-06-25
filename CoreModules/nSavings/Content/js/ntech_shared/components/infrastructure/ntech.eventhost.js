var NTechComponents;
(function (NTechComponents) {
    class NTechEventHost {
        constructor() {
            this.subs = {};
        }
        signalEvent(e) {
            for (let id of Object.keys(this.subs)) {
                var f = this.subs[id];
                if (f) {
                    f(e);
                }
            }
        }
        subscribeToOneEventOnly(predicate, onEvent) {
            let removalId = this.subscribeToEvent(evt => {
                if (predicate(evt)) {
                    this.unSubscribeFromEvent(removalId);
                    onEvent();
                }
            });
        }
        subscribeToEvent(onEvent) {
            let removalId = this.createGuid();
            this.subs[removalId] = onEvent;
            return removalId;
        }
        unSubscribeFromEvent(removalId) {
            this.subs[removalId] = null;
        }
        createGuid() {
            let s4 = () => Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
        }
    }
    NTechComponents.NTechEventHost = NTechEventHost;
})(NTechComponents || (NTechComponents = {}));
