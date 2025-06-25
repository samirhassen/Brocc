var NTechComponents;
(function (NTechComponents) {
    class NTechLocalStorageService {
        constructor($window) {
            this.$window = $window;
        }
        getUserContainer(namespace, userId, version) {
            return new NTechLocalStorageContainer(namespace + '_u' + userId, version, this.$window);
        }
        getSharedContainer(namespace, version) {
            return new NTechLocalStorageContainer(namespace, version, this.$window);
        }
    }
    NTechLocalStorageService.$inject = ['$window'];
    NTechComponents.NTechLocalStorageService = NTechLocalStorageService;
    class NTechLocalStorageContainer {
        constructor(namespace, version, $window) {
            this.namespace = namespace;
            this.version = version;
            this.$window = $window;
        }
        getEpochNow() {
            return new Date().getTime();
        }
        set(key, value, expirationMinutes) {
            let epochNow = this.getEpochNow();
            this.setInternal(this.getInternalKey(key), {
                version: this.version,
                creationEpoch: epochNow,
                expirationEpoch: expirationMinutes ? epochNow + expirationMinutes * 60000 : null,
                value: value
            });
        }
        get(key) {
            let item = this.getInternal(this.getInternalKey(key));
            if (item) {
                return item.value;
            }
            else {
                return null;
            }
        }
        whenExists(key, action) {
            let item = this.getInternal(this.getInternalKey(key)); //Called instead of get to allow null values to trigger the action
            if (item) {
                action(item.value);
            }
        }
        getInternalKey(key) {
            return this.namespace + '_' + key;
        }
        setInternal(key, item) {
            if (!this.$window.localStorage) {
                return;
            }
            this.$window.localStorage[key] = JSON.stringify(item);
        }
        getInternal(key) {
            if (!this.$window.localStorage) {
                return null;
            }
            let value = this.$window.localStorage[key];
            if (!value) {
                return null;
            }
            let item = JSON.parse(value);
            if (!item) {
                return null;
            }
            if (item.version !== this.version) {
                return null;
            }
            let epochNow = this.getEpochNow();
            if (item.expirationEpoch && item.expirationEpoch < epochNow) {
                return null;
            }
            return item;
        }
    }
    NTechComponents.NTechLocalStorageContainer = NTechLocalStorageContainer;
})(NTechComponents || (NTechComponents = {}));
