var NTechComponents;
(function (NTechComponents) {
    var NTechLocalStorageService = /** @class */ (function () {
        function NTechLocalStorageService($window) {
            this.$window = $window;
        }
        NTechLocalStorageService.prototype.getUserContainer = function (namespace, userId, version) {
            return new NTechLocalStorageContainer(namespace + '_u' + userId, version, this.$window);
        };
        NTechLocalStorageService.prototype.getSharedContainer = function (namespace, version) {
            return new NTechLocalStorageContainer(namespace, version, this.$window);
        };
        NTechLocalStorageService.$inject = ['$window'];
        return NTechLocalStorageService;
    }());
    NTechComponents.NTechLocalStorageService = NTechLocalStorageService;
    var NTechLocalStorageContainer = /** @class */ (function () {
        function NTechLocalStorageContainer(namespace, version, $window) {
            this.namespace = namespace;
            this.version = version;
            this.$window = $window;
        }
        NTechLocalStorageContainer.prototype.getEpochNow = function () {
            return new Date().getTime();
        };
        NTechLocalStorageContainer.prototype.set = function (key, value, expirationMinutes) {
            var epochNow = this.getEpochNow();
            this.setInternal(this.getInternalKey(key), {
                version: this.version,
                creationEpoch: epochNow,
                expirationEpoch: expirationMinutes ? epochNow + expirationMinutes * 60000 : null,
                value: value
            });
        };
        NTechLocalStorageContainer.prototype.get = function (key) {
            var item = this.getInternal(this.getInternalKey(key));
            if (item) {
                return item.value;
            }
            else {
                return null;
            }
        };
        NTechLocalStorageContainer.prototype.whenExists = function (key, action) {
            var item = this.getInternal(this.getInternalKey(key)); //Called instead of get to allow null values to trigger the action
            if (item) {
                action(item.value);
            }
        };
        NTechLocalStorageContainer.prototype.getInternalKey = function (key) {
            return this.namespace + '_' + key;
        };
        NTechLocalStorageContainer.prototype.setInternal = function (key, item) {
            if (!this.$window.localStorage) {
                return;
            }
            this.$window.localStorage[key] = JSON.stringify(item);
        };
        NTechLocalStorageContainer.prototype.getInternal = function (key) {
            if (!this.$window.localStorage) {
                return null;
            }
            var value = this.$window.localStorage[key];
            if (!value) {
                return null;
            }
            var item = JSON.parse(value);
            if (!item) {
                return null;
            }
            if (item.version !== this.version) {
                return null;
            }
            var epochNow = this.getEpochNow();
            if (item.expirationEpoch && item.expirationEpoch < epochNow) {
                return null;
            }
            return item;
        };
        return NTechLocalStorageContainer;
    }());
    NTechComponents.NTechLocalStorageContainer = NTechLocalStorageContainer;
})(NTechComponents || (NTechComponents = {}));
//# sourceMappingURL=ntech.localstorageservice.js.map