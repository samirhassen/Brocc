import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, ValidatorFn } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject, Subscription } from 'rxjs';
import { ComplexApplicationListRow } from 'src/app/common-services/complex-application-list';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, NumberDictionary } from 'src/app/common.types';
import { EditFormInitialData } from 'src/app/shared-application-components/components/edit-form/edit-form.component';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';
import { MortgageLoanApplicationApiService } from '../../services/mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';
import { FreeFormApplicationListEditorService } from './freeform-applicationlist-editor.service';

@Component({
    selector: 'application-parties',
    templateUrl: './application-parties.component.html',
    styleUrls: ['./application-parties.component.scss'],
})
export class ApplicationPartiesComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private apiService: MortgageLoanApplicationApiService,
        private fb: UntypedFormBuilder,
        private listEditorService: FreeFormApplicationListEditorService,
        private validationService: NTechValidationService
    ) {}

    public m: Model;

    async ngOnInit() {
        let applicationNr = this.route.snapshot.params['applicationNr'] as string;
        await this.reload(applicationNr);
    }

    private async reload(applicationNr: string) {
        let oldM = this.m;
        this.m = null;
        let applicationModel = await this.apiService.fetchApplicationInitialData(applicationNr);
        if (applicationModel === 'noSuchApplicationExists') {
            return;
        }
        let isReadOnly = !applicationModel.applicationInfo.IsActive;
        let m: Model = {
            applicationNr: applicationNr,
            isReadOnly: isReadOnly,
        };

        let sharedIsEditing = new BehaviorSubject<boolean>(false);

        this.setupBrokerForm(m, sharedIsEditing, applicationModel, oldM?.broker?.subs);
        this.setupBrfAdminForm(m, sharedIsEditing, applicationModel, oldM?.brfAdmin?.subs);
        this.setupCreditorForm(m, sharedIsEditing, applicationModel, oldM?.creditor?.subs);

        this.m = m;
    }

    private setupBrokerForm(
        m: Model,
        sharedIsEditing: BehaviorSubject<boolean>,
        application: StandardMortgageLoanApplicationModel,
        oldSubs: Subscription[]
    ) {
        if (oldSubs) {
            for (let sub of oldSubs) {
                sub.unsubscribe();
            }
        }

        let listName = 'BrokerParty';
        let brokerPartyRow = application.getComplexApplicationList(listName, true).getRow(1, true);

        let f = new FormsHelper(this.fb.group({}));

        m.broker = {
            editFormInitialData: {
                isEditAllowed: !m.isReadOnly,
                sharedIsEditing: sharedIsEditing,
                onSave: async () => {
                    let newValues: Dictionary<string> = {};
                    for (let f of this.m.broker.fields) {
                        newValues[f.formControlName] = this.m.broker.form.getValue(f.formControlName);
                    }
                    await this.listEditorService.setSingleRowList(this.m.applicationNr, listName, newValues);
                    this.reload(this.m.applicationNr);
                    return { removeEditModeAfter: false };
                },
                onCancel: () => {
                    this.reload(this.m.applicationNr);
                },
                inEditMode: new BehaviorSubject<boolean>(false),
                isInvalid: () => f.invalid(),
            },
            fields: [],
            form: f,
            subs: [],
        };
        let addSimpleField = (name: string, label: string, validators?: ValidatorFn[]) => {
            m.broker.fields.push({
                getForm: () => f,
                formControlName: name,
                labelText: label,
                inEditMode: () => m.broker.editFormInitialData.inEditMode.value,
                getOriginalValue: () => brokerPartyRow.getUniqueItem(name),
                getValidators: () => [...(validators || [])],
                labelColCount: 4,
            });
        };

        addSimpleField('companyName', 'Company name');
        addSimpleField('contactPersonName', 'Contact person name');
        addSimpleField('addressStreet', 'Street');
        addSimpleField('addressZipcode', 'Zip');
        addSimpleField('addressCity', 'City');
        addSimpleField('email', 'Email', [this.validationService.getEmailValidator()]);
        addSimpleField('phone', 'Phone', [this.validationService.getPhoneValidator()]);

        m.broker.subs = EditblockFormFieldModel.setupForm(m.broker.fields, f);
    }

    private setupBrfAdminForm(
        m: Model,
        sharedIsEditing: BehaviorSubject<boolean>,
        application: StandardMortgageLoanApplicationModel,
        oldSubs: Subscription[]
    ) {
        if (oldSubs) {
            for (let sub of oldSubs) {
                sub.unsubscribe();
            }
        }
        let objectTypeCode = application
            .getComplexApplicationList('Application', true)
            .getRow(1, true)
            .getUniqueItem('objectTypeCode');

        if (objectTypeCode !== 'seBrf') {
            return;
        }

        let listName = 'BrfAdminParty';
        let brfAdminRow = application.getComplexApplicationList(listName, true).getRow(1, true);

        let f = new FormsHelper(this.fb.group({}));

        m.brfAdmin = {
            editFormInitialData: {
                isEditAllowed: !m.isReadOnly,
                sharedIsEditing: sharedIsEditing,
                onSave: async () => {
                    let newValues: Dictionary<string> = {};
                    for (let f of this.m.brfAdmin.fields) {
                        newValues[f.formControlName] = this.m.brfAdmin.form.getValue(f.formControlName);
                    }
                    await this.listEditorService.setSingleRowList(this.m.applicationNr, listName, newValues);
                    this.reload(this.m.applicationNr);
                    return { removeEditModeAfter: false };
                },
                onCancel: () => {
                    this.reload(this.m.applicationNr);
                },
                inEditMode: new BehaviorSubject<boolean>(false),
                isInvalid: () => f.invalid(),
            },
            fields: [],
            form: f,
            subs: [],
        };
        let addSimpleField = (name: string, label: string, validators?: ValidatorFn[]) => {
            m.brfAdmin.fields.push({
                getForm: () => f,
                formControlName: name,
                labelText: label,
                inEditMode: () => m.brfAdmin.editFormInitialData.inEditMode.value,
                getOriginalValue: () => brfAdminRow.getUniqueItem(name),
                getValidators: () => [...(validators || [])],
                labelColCount: 4,
            });
        };

        addSimpleField('companyName', 'Company name');
        addSimpleField('contactPersonName', 'Contact person name');
        addSimpleField('addressStreet', 'Street');
        addSimpleField('addressZipcode', 'Zip');
        addSimpleField('addressCity', 'City');
        addSimpleField('email', 'Email', [this.validationService.getEmailValidator()]);
        addSimpleField('phone', 'Phone', [this.validationService.getPhoneValidator()]);

        m.brfAdmin.subs = EditblockFormFieldModel.setupForm(m.brfAdmin.fields, f);
    }

    private setupCreditorForm(
        m: Model,
        sharedIsEditing: BehaviorSubject<boolean>,
        application: StandardMortgageLoanApplicationModel,
        oldSubs: Subscription[]
    ) {
        if (oldSubs) {
            for (let sub of oldSubs) {
                sub.unsubscribe();
            }
        }
        let listName = 'CreditorParty';
        let list = application.getComplexApplicationList(listName, true);

        let f = new FormsHelper(this.fb.group({}));

        m.creditor = {
            editFormInitialData: {
                isEditAllowed: !m.isReadOnly,
                sharedIsEditing: sharedIsEditing,
                onSave: async () => {
                    let newValues: NumberDictionary<Dictionary<string>> = {};
                    for (let row of this.m.creditor.rows) {
                        let rowNr = row.rowNr;
                        newValues[rowNr] = {};
                        for (let field of row.fields) {
                            let name = field.formControlName.substring(
                                0,
                                field.formControlName.length - rowNr.toString().length
                            );
                            newValues[rowNr][name] = f.getValue(field.formControlName);
                        }
                        newValues[rowNr]['exists'] = 'true'; //To ensure empty rows are preserved
                    }
                    await this.listEditorService.setMultiRowList(this.m.applicationNr, listName, newValues);
                    await this.reload(this.m.applicationNr);
                    return { removeEditModeAfter: false };
                },
                onCancel: () => {
                    this.reload(this.m.applicationNr);
                },
                inEditMode: new BehaviorSubject<boolean>(false),
                isInvalid: () => f.invalid(),
            },
            rows: [],
            form: f,
            subs: [],
        };

        for (let rowNr of list.getRowNumbers()) {
            this.addCreditor(m, list.getRow(rowNr, true), null);
        }
    }

    addCreditor(m: Model, initialValue: ComplexApplicationListRow | null, evt: Event | null) {
        evt?.preventDefault();

        let rows = m.creditor.rows;

        let rowNr = initialValue ? initialValue.nr : rows.length == 0 ? 1 : rows[rows.length - 1].rowNr + 1;
        let creditor = m.creditor;
        let fields: EditblockFormFieldModel[] = [];
        let addSimpleField = (name: string, label: string, validators?: ValidatorFn[]) => {
            fields.push({
                getForm: () => creditor.form,
                formControlName: name + rowNr,
                labelText: label,
                inEditMode: () => creditor.editFormInitialData.inEditMode.value,
                getOriginalValue: () => initialValue?.getUniqueItem(name),
                getValidators: () => [...(validators || [])],
                labelColCount: 4,
            });
        };

        addSimpleField('companyName', 'Company name');
        addSimpleField('contactPersonName', 'Contact person name');
        addSimpleField('addressStreet', 'Street');
        addSimpleField('addressZipcode', 'Zip');
        addSimpleField('addressCity', 'City');
        addSimpleField('email', 'Email', [this.validationService.getEmailValidator()]);
        addSimpleField('phone', 'Phone', [this.validationService.getPhoneValidator()]);

        m.creditor.rows.push({
            rowNr: rowNr,
            fields: fields,
        });

        for (let sub of m.creditor.subs) {
            sub.unsubscribe();
        }
        let allFields: EditblockFormFieldModel[] = [];
        for (let row of m.creditor.rows) {
            allFields.push(...row.fields);
        }
        m.creditor.subs = EditblockFormFieldModel.setupForm(allFields, m.creditor.form);
    }

    removeCreditor(rowNr: number, evt?: Event) {
        evt?.preventDefault();

        let c = this.m.creditor;
        let i = c.rows.findIndex((x) => x.rowNr === rowNr);
        this.m.creditor.rows.splice(i, 1);
    }
}

class Model {
    applicationNr: string;
    isReadOnly: boolean;
    broker?: {
        editFormInitialData: EditFormInitialData;
        fields: EditblockFormFieldModel[];
        form: FormsHelper;
        subs: Subscription[];
    };
    brfAdmin?: {
        editFormInitialData: EditFormInitialData;
        fields: EditblockFormFieldModel[];
        form: FormsHelper;
        subs: Subscription[];
    };
    creditor?: {
        editFormInitialData: EditFormInitialData;
        rows: {
            rowNr: number;
            fields: EditblockFormFieldModel[];
        }[];
        form: FormsHelper;
        subs: Subscription[];
    };
}
