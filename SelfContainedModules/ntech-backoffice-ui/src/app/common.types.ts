export type Dictionary<T> = { [index: string]: T };
export type StringDictionary = Dictionary<string>;
export type NumberDictionary<T> = { [key: number]: T };

export function getOrAddToDictionary<T extends object>(source: Dictionary<T>, key: string, create: () => T): T {
    if (!source[key]) {
        source[key] = create();
    }
    return source[key];
}

export function getFromDictionary<T>(source: Dictionary<T>, key: string, defaultValue: T): T {
    if (source[key]) {
        return source[key];
    } else {
        return defaultValue;
    }
}

export function distinct<TInput>(i: TInput[]): TInput[] {
    if (!i) {
        return null;
    }
    return i.filter((item, i, ar) => ar.indexOf(item) === i);
}

export function groupByString<TInput>(source: TInput[], getKey: (x: TInput) => string): Dictionary<TInput[]> {
    let d: Dictionary<TInput[]> = {};
    for (let item of source || []) {
        let key = getKey(item);
        if (!d[key]) {
            d[key] = [];
        }
        d[key].push(item);
    }
    return d;
}

export function groupByNumber<TInput>(source: TInput[], getKey: (x: TInput) => number): NumberDictionary<TInput[]> {
    let d: Dictionary<TInput[]> = {};
    for (let item of source || []) {
        let key = getKey(item);
        if (!d[key]) {
            d[key] = [];
        }
        d[key].push(item);
    }
    return d;
}

export function getNumberDictionaryKeys<TInput>(source: NumberDictionary<TInput>): number[] {
    let result: number[] = [];
    if (!source) {
        result;
    }
    for (let key of Object.keys(source)) {
        result.push(parseInt(key));
    }
    return result;
}

export function getNumberDictionaryValues<TInput>(source: NumberDictionary<TInput>): TInput[] {
    let result: TInput[] = [];
    if (!source) {
        result;
    }
    for (let key of getNumberDictionaryKeys(source)) {
        result.push(source[key]);
    }
    return result;
}

export function getDictionaryValues<TInput>(source: Dictionary<TInput>): TInput[] {
    let result: TInput[] = [];
    if (!source) {
        result;
    }
    for (let key of Object.keys(source)) {
        result.push(source[key]);
    }
    return result;
}

export function orderBy<T>(source: T[], getSortKey: (value: T) => number): T[] {
    return [...source].sort((x, y) => getSortKey(x) - getSortKey(y));
}

export function orderByStr<T>(source: T[], getSortKey: (value: T) => string): T[] {
    return [...source].sort((x, y) => getSortKey(x).localeCompare(getSortKey(y)));
}

export class NullableNumber {
    constructor(public value: number) {}
}

class UniqueIdGenerator {
    alphabet = '23456789abdegjkmnpqrvwxyz';
    generateUniqueId(length: number) {
        var rtn = '';
        for (var i = 0; i < length; i++) {
            rtn += this.alphabet.charAt(Math.floor(Math.random() * this.alphabet.length));
        }
        return rtn;
    }
}
let uniqueIdGenerator = new UniqueIdGenerator();
export function generateUniqueId(length: number) {
    return uniqueIdGenerator.generateUniqueId(length);
}

export type FileInputEventTarget = EventTarget & { files: FileList };

//Based on https://github.com/neosmart/UrlBase64/blob/master/UrlBase64/UrlBase64.cs

export function toUrlSafeBase64String<T>(data: T): string {
    let encoded = btoa(JSON.stringify(data)).replace('+', '-').replace('/', '_');
    while (encoded[encoded.length - 1] === '=') {
        encoded = encoded.substr(0, encoded.length - 1);
    }
    return encoded;
}

export function fromUrlSafeBase64String<T>(data: string): T {
    if (!data) {
        return null;
    }
    let decodeFirstPass = () => {
        let decoded = '';
        for (let c of data) {
            if (c === '_') {
                decoded += '/';
            } else if (c === '-') {
                decoded += '+';
            } else {
                decoded += c;
            }
        }
        switch (decoded.length % 4) {
            case 2:
                return decoded + '==';
            case 3:
                return decoded + '=';
            default:
                return decoded;
        }
    };

    let d = decodeFirstPass();
    return JSON.parse(atob(d));
}

export function parseQueryStringParameters(): StringDictionary {
    let d: StringDictionary = {};
    let query = window.location.search.substring(1);
    let vars = query.split('&');
    if (vars && vars.length > 0 && vars[0].trim().length > 0) {
        for (var i = 0; i < vars.length; i++) {
            var pair = vars[i].split('=');
            d[decodeURIComponent(pair[0])] = decodeURIComponent(pair[1]);
        }
    }
    return d;
}

export function normalizeStringSlashes(input: string, leading: boolean | null, trailing: boolean | null) {
    let result = input;
    if (result.startsWith('/') && leading === false) {
        result = result.substr(1);
    }
    if (result.endsWith('/') && trailing === false) {
        result = result.substr(0, result.length - 1);
    }
    if (!result.startsWith('/') && leading === true) {
        result = '/' + result;
    }
    if (!result.endsWith('/') && trailing === true) {
        result = result + '/';
    }
    return result;
}

export function parseTriStateBoolean(value: string): boolean | null {
    return value === 'true' ? true : value === 'false' ? false : null;
}

export function stringJoin(separator: string, values: string[]) {
    let s = '';
    for (let value of values || []) {
        if (s.length > 0) {
            s += separator;
        }
        s += value;
    }
    return s;
}

let bankAccountNrTypesFallback: Dictionary<string> = {
    BankAccountSe: 'Regular account',
    BankGiroSe: 'Bankgiro',
    PlusGiroSe: 'Plusgiro',
    IBANFi: 'Finnish IBAN',
};
let bankAccountNrTypesLang: Dictionary<Dictionary<string>> = {
    sv: {
        BankAccountSe: 'Vanligt bankkonto',
        BankGiroSe: 'Bankgiro',
        PlusGiroSe: 'Plusgiro',
    },
};
export function getBankAccountTypeDropdownOptions(
    baseCountry: string,
    includeEmptyOption: boolean,
    language?: string,
    excludeRegularAccounts?: boolean
) {
    let types: { Code: string; DisplayName: string }[] = [];
    if (includeEmptyOption) {
        types.push({ Code: '', DisplayName: '' });
    }
    if (baseCountry == 'SE') {
        types.push({ Code: 'BankGiroSe', DisplayName: '' });
        types.push({ Code: 'PlusGiroSe', DisplayName: '' });
        if (!excludeRegularAccounts) types.push({ Code: 'BankAccountSe', DisplayName: '' });
    } else if (baseCountry == 'FI') {
        types.push({ Code: 'IBANFi', DisplayName: '' });
    }

    for (let t of types) {
        let langOverride = language ? bankAccountNrTypesLang[language] : null;
        t.DisplayName = (langOverride ? langOverride[t.Code] : null) || bankAccountNrTypesFallback[t.Code];
    }

    return types;
}

export interface DateOnly {
    yearMonthDay: number;
}

export function getDateOnlyParts(d: DateOnly): { year: number; month: number; day: number } | null {
    if (!d) {
        return null;
    }
    let ymd = d.yearMonthDay; //yyyymmdd
    let day = ymd % 100;
    ymd = (ymd - day) / 100; //yyyymm
    let month = ymd % 100;
    let year = (ymd - month) / 100; //yyyy
    return { year, month, day };
}

export function dateOnlyToIsoDate(d: DateOnly): string | null {
    let parts = getDateOnlyParts(d);
    if (!parts) return null;
    return `${parts.year}-${String(parts.month).padStart(2, '0')}-${String(parts.day).padStart(2, '0')}`;
}

// https://stackoverflow.com/questions/12931828/convert-returned-json-object-properties-to-lower-first-camelcase
export function toCamelCase(o: any) {
    let newO: any;
    let origKey: string;
    let newKey: string;
    let value: any;
    if (o instanceof Array) {
        return o.map(function (value) {
            if (typeof value === "object") {
                value = toCamelCase(value)
            }
            return value
        })
    } else {
        newO = {}
        for (origKey in o) {
            if (o.hasOwnProperty(origKey)) {
                newKey = (origKey.charAt(0).toLowerCase() + origKey.slice(1) || origKey).toString()
                value = o[origKey]
                if (value instanceof Array || (value !== null && value.constructor === Object)) {
                    value = toCamelCase(value)
                }
                newO[newKey] = value
            }
        }
    }
    return newO
}

//https://stackoverflow.com/a/5480605/353526
export function camelCaseRevier(key: string, value: any) {    
    if (value && typeof value === 'object' && !Array.isArray(value)) {
        let replacement: any = {};
        for (var k in value) {
            if (Object.hasOwnProperty.call(value, k)) {
                replacement[k && k.charAt(0).toLowerCase() + k.substring(1)] = value[k];
            }
        }
        return replacement;
    }
    return value;
  }