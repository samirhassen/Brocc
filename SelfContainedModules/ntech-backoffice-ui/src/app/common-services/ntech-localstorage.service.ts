import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class NTechLocalStorageService {
    constructor() {}

    getUserContainer(namespace: string, userId: number, version: string): NTechLocalStorageContainer {
        return new NTechLocalStorageContainer(namespace + '_u' + userId, version);
    }

    getSharedContainer(namespace: string, version: string): NTechLocalStorageContainer {
        return new NTechLocalStorageContainer(namespace, version);
    }
}

export class NTechLocalStorageContainer {
    constructor(private namespace: string, private version: string) {}

    private getEpochNow(): number {
        return new Date().getTime();
    }

    set<T>(key: string, value: T, expirationMinutes?: number) {
        let epochNow = this.getEpochNow();
        this.setInternal(this.getInternalKey(key), {
            version: this.version,
            creationEpoch: epochNow,
            expirationEpoch: expirationMinutes ? epochNow + expirationMinutes * 60000 : null,
            value: value,
        });
    }

    get<T>(key: string): T {
        let item = this.getInternal<T>(this.getInternalKey(key));
        if (item) {
            return item.value;
        } else {
            return null;
        }
    }

    getWithDefault<T>(key: string, defaultValueFactory: () => T) {
        let item = this.getInternal<T>(this.getInternalKey(key));
        if (item) {
            return item.value;
        } else {
            return defaultValueFactory();
        }
    }

    whenExists<T>(key: string, action: (value: T) => void) {
        let item = this.getInternal<T>(this.getInternalKey(key)); //Called instead of get to allow null values to trigger the action
        if (item) {
            action(item.value);
        }
    }

    private getInternalKey(key: string) {
        return this.namespace + '_' + key;
    }

    private setInternal<T>(key: string, item: IStoredItem<T>) {
        if (!localStorage) {
            return;
        }
        localStorage[key] = JSON.stringify(item);
    }

    private getInternal<T>(key: string): IStoredItem<T> {
        if (!localStorage) {
            return null;
        }
        let value = localStorage[key];
        if (!value) {
            return null;
        }
        let item = JSON.parse(value) as IStoredItem<T>;
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

interface IStoredItem<T> {
    version: string;
    creationEpoch: number;
    expirationEpoch?: number;
    value: T;
}
