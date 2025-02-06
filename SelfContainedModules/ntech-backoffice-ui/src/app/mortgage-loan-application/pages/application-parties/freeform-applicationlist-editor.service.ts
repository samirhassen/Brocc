import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary, getNumberDictionaryKeys, NumberDictionary } from 'src/app/common.types';

@Injectable({
    providedIn: 'root',
})
export class FreeFormApplicationListEditorService {
    constructor(private apiService: NtechApiService) {}

    public setSingleRowList(applicationNr: string, listName: string, completeRow: Dictionary<string>) {
        return this.edit(
            applicationNr,
            Object.keys(completeRow).map((itemName) => ({
                listName: listName,
                nr: 1,
                itemName: itemName,
                itemValue: completeRow[itemName],
            })),
            true
        );
    }

    public setMultiRowList(applicationNr: string, listName: string, rows: NumberDictionary<Dictionary<string>>) {
        let changes: Change[] = [];
        for (let rowNr of getNumberDictionaryKeys(rows)) {
            changes.push(
                ...Object.keys(rows[rowNr]).map((itemName) => ({
                    listName: listName,
                    nr: rowNr,
                    itemName: itemName,
                    itemValue: rows[rowNr][itemName],
                }))
            );
        }
        return this.edit(applicationNr, changes, true);
    }

    //Note: This only works for a few select lists. Do NOT changet this to include lists like Application or Applicant
    //      or any other used for workflow logic. Only do this for lists where it's ok that the user can manipulate all the values.
    private edit(applicationNr: string, changes: Change[], removeOtherItemsInLists: boolean) {
        return this.apiService.post('nPreCredit', 'api/MortgageLoanStandard/Set-FreeEdit-Application-List', {
            applicationNr,
            changes,
            removeOtherItemsInLists,
        });
    }
}

interface Change {
    listName: string;
    nr: number;
    itemName: string;
    itemValue: string;
}
