import {
    Dictionary,
    getFromDictionary,
    getNumberDictionaryKeys,
    groupByNumber,
    groupByString,
    NumberDictionary,
    StringDictionary,
} from '../common.types';

export class ComplexApplicationList {
    constructor(public listName: string, flattendItems: FlattenedComplexApplicationListItem[]) {
        this.rows = {};
        let itemsByNr = groupByNumber(flattendItems, (x) => x.Nr);
        for (let nr of getNumberDictionaryKeys(itemsByNr)) {
            this.rows[nr] = new ComplexApplicationListRow(listName, nr, itemsByNr[nr]);
        }
    }

    private rows: NumberDictionary<ComplexApplicationListRow>;

    static createListsFromFlattenedItems(
        items: FlattenedComplexApplicationListItem[]
    ): Dictionary<ComplexApplicationList> {
        let itemsByListName = groupByString(items, (x) => x.ListName);
        let result: Dictionary<ComplexApplicationList> = {};
        for (let listName of Object.keys(itemsByListName)) {
            result[listName] = new ComplexApplicationList(listName, itemsByListName[listName]);
        }
        return result;
    }

    static createListFromFlattenedItems(
        listName: string,
        items: FlattenedComplexApplicationListItem[]
    ): ComplexApplicationList {
        let lists = ComplexApplicationList.createListsFromFlattenedItems(items);
        let count = Object.keys(lists).length;
        if (count == 0) {
            return new ComplexApplicationList(listName, []);
        } else if (count > 1) {
            throw new Error('Expected items from exactly one list');
        } else if (!lists[listName]) {
            throw new Error('Expected items from only the list ' + listName);
        } else {
            return lists[listName];
        }
    }

    public getRow(nr: number, emptyRowOnNotExists: boolean) {
        let actualRow = this.rows[nr];
        return actualRow
            ? actualRow
            : emptyRowOnNotExists
            ? new ComplexApplicationListRow(this.listName, nr, [])
            : null;
    }

    public getRowNumbers() {
        let rowNrs = getNumberDictionaryKeys<ComplexApplicationListRow>(this.rows);
        rowNrs.sort((a, b) => a - b);
        return rowNrs;
    }
}

export class ComplexApplicationListRow {
    constructor(public listName: string, public nr: number, flattendItems: FlattenedComplexApplicationListItem[]) {
        this.uniqueItems = {};
        this.repeatedItems = {};
        for (let item of flattendItems) {
            if (item.IsRepeatable) {
                if (!this.repeatedItems[item.ItemName]) {
                    this.repeatedItems[item.ItemName] = [];
                }
                this.repeatedItems[item.ItemName].push(item.ItemValue);
            } else {
                this.uniqueItems[item.ItemName] = item.ItemValue;
            }
        }
    }
    private uniqueItems: Dictionary<string>;
    private repeatedItems: Dictionary<string[]>;

    public getUniqueItemNames() {
        return Object.keys(this.uniqueItems ?? {});
    }

    public getUniqueItem(itemName: string) {
        return this.uniqueItems[itemName];
    }

    public getUniqueItemBoolean(itemName: string) {
        let rawValue = getFromDictionary(this.uniqueItems, itemName, null);
        if (rawValue === 'true') {
            return true;
        } else if (rawValue === 'false') {
            return false;
        } else {
            return null;
        }
    }

    public getUniqueItemInteger(itemName: string) {
        let rawValue = getFromDictionary(this.uniqueItems, itemName, null);
        if (rawValue == null) {
            return null;
        }
        return parseInt(rawValue);
    }

    public getUniqueItemDecimal(itemName: string) {
        let rawValue = getFromDictionary(this.uniqueItems, itemName, null);
        if (rawValue == null) {
            return null;
        }
        return parseFloat(rawValue);
    }

    public getRepeatedItems(itemName: string) {
        return this.repeatedItems[itemName] || [];
    }

    public static fromDictionary(listName: string, nr: number, uniqueValues: StringDictionary) {
        let items: FlattenedComplexApplicationListItem[] = uniqueValues
            ? Object.keys(uniqueValues).map((x) => {
                  return {
                      ListName: listName,
                      Nr: nr,
                      ItemName: x,
                      ItemValue: uniqueValues[x],
                      IsRepeatable: false,
                  } as FlattenedComplexApplicationListItem;
              })
            : [];
        return new ComplexApplicationListRow(listName, nr, items);
    }
}

export interface FlattenedComplexApplicationListItem {
    ListName: string;
    Nr: number;
    ItemName: string;
    ItemValue: string;
    IsRepeatable: boolean;
}
