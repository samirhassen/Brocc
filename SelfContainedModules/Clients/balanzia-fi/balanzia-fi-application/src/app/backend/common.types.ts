import * as moment from 'moment'

export type Dictionary<T> = { [index:string] : T };
export type StringDictionary = Dictionary<string>;

const InternalDateOnlyFormat = 'YYYY-MM-DD'
export class DateOnly {    
    constructor(public date: string) {}

    static fromDateString(date: string, dateFormat: string) {
        let m = moment(date, dateFormat, true)
        if(!m.isValid()) {
            throw new Error('Invalid date. Expected format: ' + dateFormat)
        }
        return new DateOnly(m.format(InternalDateOnlyFormat))
    }

    static format(d: DateOnly, dateFormat: string): string {
        let m = moment(d.date, InternalDateOnlyFormat, true)
        return m.format(dateFormat)
    }
}

export class NullableNumber {
    constructor(public value: number) {}
}