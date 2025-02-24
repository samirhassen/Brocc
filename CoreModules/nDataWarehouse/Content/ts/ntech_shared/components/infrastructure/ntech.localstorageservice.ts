namespace NTechComponents {
    export class NTechLocalStorageService {
        static $inject = ['$window']
        constructor(private $window: ng.IWindowService) {
            
        }

        getUserContainer(namespace: string, userId: number, version: string) : NTechLocalStorageContainer {
            return new NTechLocalStorageContainer(namespace + '_u' + userId, version, this.$window)
        }

        getSharedContainer(namespace: string, version: string) : NTechLocalStorageContainer {
            return new NTechLocalStorageContainer(namespace, version, this.$window)
        }
    }

    export class NTechLocalStorageContainer {
        constructor(private namespace: string, private version: string, private $window: ng.IWindowService) {

        }

        private getEpochNow() : number {
            return new Date().getTime()
        }

        set<T>(key: string, value: T, expirationMinutes?: number) {
            let epochNow = this.getEpochNow()
            this.setInternal(this.getInternalKey(key), {
                version: this.version,
                creationEpoch: epochNow,
                expirationEpoch: expirationMinutes ? epochNow + expirationMinutes*60000 : null,
                value: value
            })
        }

        get<T>(key: string): T {
            let item = this.getInternal<T>(this.getInternalKey(key))
            if(item) {
                return item.value
            } else {
                return null
            }
        }

        whenExists<T>(key: string, action: (value: T) => void) {
            let item = this.getInternal<T>(this.getInternalKey(key)) //Called instead of get to allow null values to trigger the action
            if (item) {
                action(item.value)
            }
        }        

        private getInternalKey(key: string) {
            return this.namespace + '_' + key
        }

        private setInternal<T>(key: string, item: IStoredItem<T>)   {
            if(!this.$window.localStorage) {
                return
            }
            this.$window.localStorage[key] = JSON.stringify(item)
        }

        private getInternal<T>(key: string) : IStoredItem<T> {
            if(!this.$window.localStorage) {
                return null
            }
            let value = this.$window.localStorage[key]
            if(!value) {
                return null
            }
            let item = JSON.parse(value) as IStoredItem<T>
            if(!item) {
                return null
            }
            if(item.version !== this.version) {
                return null
            }
            let epochNow = this.getEpochNow()
            if(item.expirationEpoch && item.expirationEpoch < epochNow) {
                return null
            }
            return item
        }
    }

    interface IStoredItem<T> {
        version: string,
        creationEpoch: number
        expirationEpoch?: number
        value: T        
    }
}