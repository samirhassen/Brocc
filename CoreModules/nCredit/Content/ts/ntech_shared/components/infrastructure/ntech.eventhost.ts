namespace NTechComponents {
    export class NTechEventHost<TEvent> {
        private subs: { [index: string]: (context: TEvent) => void } = {}
 
        signalEvent(e: TEvent) {
            for (let id of Object.keys(this.subs)) {
                var f = this.subs[id]
                if(f) {
                    f(e);
                }                
            }
        }

        subscribeToOneEventOnly(predicate: (evt: TEvent) => boolean, onEvent: () => void) {
            let removalId = this.subscribeToEvent(evt => {
                if (predicate(evt)) {
                    this.unSubscribeFromEvent(removalId)
                    onEvent()
                }
            })
        }        
 
        subscribeToEvent(onEvent: ((context: TEvent) => void)) : string {
            let removalId = this.createGuid()
            this.subs[removalId] = onEvent
            return removalId
        }
 
        unSubscribeFromEvent(removalId: string) {
            this.subs[removalId] = null
        }

        private createGuid() {
            let s4 = () => Math.floor((1 + Math.random()) * 0x10000)
                    .toString(16)
                    .substring(1);
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
        }
    }
}