import { Component, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary } from 'src/app/common.types';

@Component({
    selector: 'bookkeeping-rules-edit',
    templateUrl: './bookkeeping-rules-edit.component.html',
    styleUrls: ['./bookkeeping-rules-edit.component.css'],
})
export class BookkeepingRulesEditComponent {
    constructor(
        private apiClient: NtechApiService,
        private fb: UntypedFormBuilder,
        private configService: ConfigService,
        private validationService: NTechValidationService,
        private toastr: ToastrService
    ) {}

    async ngOnInit(): Promise<void> {
        this.m = null;

        this.reload();
    }

    ngOnChanges(_: SimpleChanges) {
        this.m = null;

        this.reload();
    }

    public m: Model;

    private async reload() {
        this.m = null;

        const rules = await this.apiClient.shared.fetchBookkeepingRules();
        const m: Model = {
            accountNames: rules.allAccountNames,
            accountNrByAccountName: rules.accountNrByAccountName,
            allConnections: rules.allConnections,
            ruleRows: <BookkeepingRuleDescriptionTableRow[]>rules.ruleRows,
            exportCode: null,
            importText: null,
            backUrl: null, //todo backurl
            isTest: this.configService.isNTechTest(),
            previewUrl: this.apiClient.getUiGatewayUrl('nCredit', '/Api/Reports/BookKeepingPreview', []),
            importExportForm: new FormsHelper(this.fb.group({})),
        };
        const code: BookKeepingCode = {
            accountNames: m.accountNames,
            accountNrByAccountName: m.accountNrByAccountName,
        };
        m.exportCode = `B_${btoa(JSON.stringify(code))}_B`;

        const accountNames = m.accountNrByAccountName;

        const form = new FormsHelper(this.fb.group({}));
        for (let accountName in accountNames) {
            form.addControlIfNotExists(accountName, accountNames[accountName], [
                Validators.required,
                this.validationService.getPositiveIntegerValidator(),
            ]);
        }

        m.edit = {
            isEdit: false,
            accountNrByAccountName: accountNames,
            form: form,
        };

        m.importExportForm.addControlIfNotExists('importText', '', []);

        this.m = m;
    }

    beginEdit(evt?: Event): void {
        evt?.preventDefault();

        if (!this.m.edit) {
            return;
        }

        this.m.edit.isEdit = true;
    }

    cancelEdit(evt?: Event): void {
        evt?.preventDefault();

        if (!this.m.edit) {
            return;
        }

        this.m.edit.isEdit = false;
        this.m.importExportForm.setValue('importText', '');
    }

    async commitEdit(evt?: Event) {
        evt?.preventDefault();

        if (!this.m.edit?.form) {
            return;
        }

        if (this.m.edit.form.invalid()) {
            this.toastr.error('Invalid values');
            return;
        }

        for (let accountName of this.m.accountNames) {
            const editedAccountName = this.m.edit.form.getValue(accountName);
            if (editedAccountName !== this.m.accountNrByAccountName[accountName]) {
                await this.keyValueStoreSet(accountName, 'BookKeepingAccountNrsV1', editedAccountName);
            }
        }

        this.reload();
    }

    hasConnection(row: BookkeepingRuleDescriptionTableRow, connectionName: string): boolean {
        return row && row.Connections && row.Connections.indexOf(connectionName) >= 0;
    }

    getRowAccountNr(
        row: BookkeepingRuleDescriptionTableRow,
        isCredit: boolean
    ): {
        isEdited: boolean;
        currentValue: string;
    } {
        let result = {
            isEdited: false,
            currentValue: '',
        };

        if (!row || !this.m) {
            return result;
        }

        result.currentValue = isCredit ? row.CreditAccountNr : row.DebetAccountNr;

        const accountName: string = isCredit ? row.CreditAccountName : row.DebetAccountName;
        if (!this.m.edit.isEdit || !accountName) {
            return result;
        }

        result.isEdited = this.m.accountNrByAccountName[accountName] !== this.m.edit.form.getValue(accountName);
        if (this.m.edit.form.invalid()) {
            result.currentValue = '-';
        } else {
            result.currentValue = this.m.edit.form.getValue(accountName);
        }

        return result;
    }

    onImportTextChanged(): void {
        if (!this.m.importExportForm) {
            return;
        }

        const importText = this.m.importExportForm.getValue('importText');

        if (!this.m || this.m.edit?.isEdit || !importText || importText.length < 5) {
            return;
        }

        if (importText.substr(0, 2) !== 'B_' || importText.substr(importText.length - 2, 2) !== '_B') {
            return;
        }

        const code: BookKeepingCode = JSON.parse(atob(importText.substr(2, importText.length - 4)));
        const missingAccountNamesInImport: string[] = [];
        const extraAccountNamesInImport: string[] = [];

        this.beginEdit();

        for (let accountName of this.m.accountNames) {
            const importedAccountNr = code.accountNrByAccountName[accountName];
            if (importedAccountNr) {
                this.m.edit.form.setValue(accountName, importedAccountNr);
            } else {
                missingAccountNamesInImport.push(accountName);
            }
        }

        for (let accountName of code.accountNames) {
            if (this.m.accountNames.indexOf(accountName) < 0) {
                extraAccountNamesInImport.push(accountName);
            }
        }

        if (missingAccountNamesInImport.length > 0 || extraAccountNamesInImport.length > 0) {
            const stringJoin = (a: string[]) => {
                let result = '';
                for (let s of a) {
                    if (result.length > 0) {
                        result += ', ';
                    }
                    result += s;
                }
            };
            let warningMessage = '';
            if (missingAccountNamesInImport.length > 0) {
                warningMessage += `These account names are in the import but not here: ${stringJoin(
                    missingAccountNamesInImport
                )}`;
            }
            if (extraAccountNamesInImport.length > 0) {
                warningMessage += `These account names are here but not in the import: ${stringJoin(
                    extraAccountNamesInImport
                )}`;
            }

            this.toastr.warning(warningMessage);
        }
    }

    //NOTE: We keep this out of credit-service as we dont want to promote the use of legacy things
    async keyValueStoreSet(accountName: string, keySpace: string, value: string) {
        let rawModel = (
            await this.apiClient.post<{ Value: string }>('nCredit', 'Api/KeyValueStore/Set', {
                Key: accountName,
                KeySpace: keySpace,
                Value: value,
            })
        )?.Value;

        return rawModel;
    }
}

interface BookKeepingCode {
    accountNames: string[];
    accountNrByAccountName: Dictionary<string>;
}

interface Model {
    edit?: {
        isEdit: boolean;
        accountNrByAccountName: Dictionary<string>;
        form: FormsHelper;
    };
    importExportForm: FormsHelper;
    accountNames: string[];
    accountNrByAccountName: Dictionary<string>;
    allConnections: string[];
    ruleRows: BookkeepingRuleDescriptionTableRow[];
    importText: string;
    exportCode: string;
    backUrl: string;
    isTest: boolean;
    previewUrl: string;
}

interface BookkeepingRuleDescriptionTableRow {
    EventName: string;
    LedgerAccountName: string;
    Connections: string[];
    DebetAccountNr: string;
    DebetAccountName: string;
    CreditAccountNr: string;
    CreditAccountName: string;
    Filter: string;
}
