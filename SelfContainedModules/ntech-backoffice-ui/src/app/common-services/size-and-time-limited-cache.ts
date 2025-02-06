import { Dictionary } from '../common.types';

export class SizeAndTimeLimitedCache {
    /*
    This should really be like a red black tree or such ordered by expirationTimeMs
    so items that are replaced all the time dont get randomly evicted when the size cap is hit.

    Could be added if this turns out to be an issue
    */
    private initialInsertionOrderedEntries: CacheItem[];
    private entryByKey: Dictionary<CacheItem>;
    constructor(private maxCount: number, private maxAgeInMinutes: number) {
        this.initialInsertionOrderedEntries = [];
        this.entryByKey = {};
    }

    public get<TValue>(key: string): TValue {
        let entry = this.entryByKey[key];
        if (!entry || this.isExpired(entry.expirationTimeMs)) {
            return null;
        }
        return entry.value;
    }

    public set<TValue>(key: string, value: TValue) {
        let newExpirationTime = new Date().getTime() + this.maxAgeInMinutes * 60 * 1000;
        if (this.entryByKey[key]) {
            //Replace previous entry
            let currentEntry = this.entryByKey[key];
            currentEntry.expirationTimeMs = newExpirationTime;
            currentEntry.value = value;
        } else {
            //Add a new entry
            let entry: CacheItem = { key: key, value: value, expirationTimeMs: newExpirationTime };
            while (
                this.initialInsertionOrderedEntries.length >= this.maxCount &&
                this.initialInsertionOrderedEntries.length > 0
            ) {
                let oldest = this.initialInsertionOrderedEntries[0];
                this.entryByKey[oldest.key] = null;
                this.initialInsertionOrderedEntries.splice(0, 1);
            }
            this.initialInsertionOrderedEntries.push(entry);
            this.entryByKey[entry.key] = entry;
        }
    }

    private isExpired(expirationTimeMs: number) {
        return expirationTimeMs < new Date().getTime();
    }
}

interface CacheItem {
    key: string;
    expirationTimeMs: number;
    value: any;
}
