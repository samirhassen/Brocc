import { Component, Input, OnInit, SimpleChanges, TemplateRef, ViewChild } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, distinct, generateUniqueId, StringDictionary } from 'src/app/common.types';
import { PolicyFilterCommonService } from '../policy-filter-common.service';
import { AddStaticParameterModel, PolicyfilterRulesetHelper } from './policyfilter-ruleset-helper';

@Component({
    selector: 'policyfilter-ruleset',
    templateUrl: './policyfilter-ruleset.component.html',
    styles: [],
})
export class PolicyfilterRulesetComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private modalService: BsModalService,
        private toastr: ToastrService,
        private validationSevice: NTechValidationService,
        private policyService: PolicyFilterCommonService
    ) {}

    @Input()
    public initialData: PolicyfilterRulesetInitialData;

    @ViewChild('addRuleModalTemplate', { static: true })
    addRuleModalTemplate: TemplateRef<any>;

    public addRuleModalRef: BsModalRef;

    public m: Model;

    ngOnInit(): void {}

    async ngOnChanges(changes: SimpleChanges) {
        await this.reload(this.initialData.ruleSet, false, this.initialData.ruleSetName);
    }

    async reload(ruleSet: PolicyfilterRuleSet, initialEditMode: boolean, ruleSetName: string) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let ruleSetClone = PolicyfilterRulesetHelper.copyAndNormalizeRuleset(ruleSet);

        let form = new FormsHelper(this.fb.group({}));
        form.addControlIfNotExists('ruleSetName', ruleSetName, [Validators.required]);
        form.addControlIfNotExists('importRulesetString', null, null);

        let addableRules = PolicyfilterRulesetHelper.createAddableRules(
            this.initialData.allRules,
            this.validationSevice
        );

        let rejectionReasons = await this.policyService.getRejectionReasonsList();

        let allRejectionReasons: StringDictionary = {};
        for (let r of addableRules.map((x) => x.rule.DefaultRejectionReasonName)) {
            allRejectionReasons[r] = r; //ensure that even old removed reasons are accessible if there are rules left that use them
        }
        for (let r of rejectionReasons) {
            allRejectionReasons[r.Code] = r.DisplayName;
        }

        let m: Model = {
            isEditAllowed: this.initialData.slotName?.toLowerCase().trim() === 'pending',
            inEditMode: initialEditMode,
            form: form,
            current: null,
            currentSlotName: this.initialData.slotName,
            addableRules: [],
            allPhases: [
                { Code: 'Internal', DisplayName: 'Internal' },
                { Code: 'External', DisplayName: 'External' },
                { Code: 'ManualControl', DisplayName: 'Manual control' },
            ],
            allRejectionReasons: Object.keys(allRejectionReasons).map((x) => ({
                Code: x,
                DisplayName: allRejectionReasons[x],
            })),
        };

        m.addableRules = PolicyfilterRulesetHelper.createAddableRules(this.initialData.allRules, this.validationSevice);

        let defaultRejectionReasonName = await this.policyService.getDefaultRejectionReasonName();
        let getDefaultRejectionReasonByRuleName = (name: string) => {
            return (
                m.addableRules.find((x) => x.rule.RuleName == name)?.rule?.DefaultRejectionReasonName ??
                defaultRejectionReasonName
            );
        };

        this.format(ruleSetClone, getDefaultRejectionReasonByRuleName).then((x) => {
            m.current = {
                ruleSet: ruleSetClone,
                ruleSetName: ruleSetName,
                internal: x.internal,
                external: x.external,
                manualControl: x.manualControl,
            };
            this.m = m;
        });
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.inEditMode = true;
    }

    async onCancel(evt?: Event) {
        evt?.preventDefault();
        await this.reload(this.initialData.ruleSet, false, this.initialData.ruleSetName);
    }

    onSave(evt?: Event) {
        evt?.preventDefault();
        let ruleSet = this.getCurrentRuleset();
        let ruleSetName = this.m.form.getValue('ruleSetName');
        let newRuleSetName = ruleSetName !== this.initialData.ruleSetName ? ruleSetName : null;

        this.m = null;
        this.initialData.onSave({ editedRuleSet: ruleSet, newRuleSetName }).then((x) => {
            if (x.showPageAfter) {
                this.reload(ruleSet, false, ruleSetName);
            }
        });
    }

    getRejectionReasonDisplayName(name: string) {
        return this.m?.allRejectionReasons?.find((x) => x.Code === name)?.DisplayName || name;
    }

    private getCurrentRuleset(): PolicyfilterRuleSet {
        let importRulesetString = this.m.form.getValue('importRulesetString');

        if (!importRulesetString) return this.m.current.ruleSet;
        else {
            if (
                this.m.current.external.rules.length >= 1 ||
                this.m.current.internal.rules.length >= 1 ||
                this.m.current.manualControl.rules.length >= 1
            )
                throw this.handleInvalidImportError(false, true);

            if (
                importRulesetString.substr(0, 2) !== 'S_' ||
                importRulesetString.substr(importRulesetString.length - 2, 2) !== '_S'
            )
                throw this.handleInvalidImportError(true);

            try {
                let importedPolicyFilter: PolicyfilterRuleSet = JSON.parse(
                    atob(importRulesetString.substr(2, importRulesetString.length - 4))
                );
                this.removeUnaddableRules(importedPolicyFilter);
                return importedPolicyFilter;
            } catch {
                throw this.handleInvalidImportError();
            }
        }
    }

    private handleInvalidImportError(isInvalidImportError?: boolean, isExistingRulesError?: boolean) {
        if (isInvalidImportError) {
            this.toastr.error('Invalid import text');
            return 'Invalid import text';
        } else if (isExistingRulesError) {
            this.toastr.error('Not allowed: remove all existing rules first.');
            return 'Not allowed: remove all existing rules first.';
        } else {
            this.toastr.error('Error when importing rules');
            return 'Error when importing rules';
        }
    }

    private removeUnaddableRules(importedPolicyFilter: PolicyfilterRuleSet) {
        //Imported rules that do not exist in addableRules are skipped
        [
            importedPolicyFilter.ExternalRules,
            importedPolicyFilter.InternalRules,
            importedPolicyFilter.ManualControlOnAcceptedRules,
        ].forEach((ruleSet) => {
            if (!ruleSet) return;
            ruleSet.forEach((rule, i) => {
                if (this.m.addableRules.filter((x) => x.rule.RuleName === rule.RuleName).length < 1) {
                    this.toastr.warning(`Imported Rule '${rule.RuleName}' does not exist and is skipped.`);
                    ruleSet.splice(i, 1);
                }
            });
        });
    }

    invalid() {
        return this?.m?.form?.invalid() === true;
    }

    getSlotDisplayName(slotName: string) {
        if (!slotName) {
            return 'Inactive';
        } else if (slotName == 'A') {
            return this.initialData.isAbTestingActive ? slotName : 'Active';
        } else {
            return slotName;
        }
    }

    addRule(
        ruleRow: { rule: PolicyFilterRuleUiModel; setupAdd: (m: AddStaticParameterModel) => Promise<void> },
        evt?: Event
    ) {
        evt?.preventDefault();

        this.m.add = PolicyfilterRulesetHelper.createAddStaticParameterModel(ruleRow, this.fb);

        this.addRuleModalRef = this.modalService.show(this.addRuleModalTemplate, {
            class: 'modal-lg',
            ignoreBackdropClick: true,
        });
    }

    removeRule(rule: ModelRuleRow) {
        this.reload(rule.removeRow(), true, this.initialData.ruleSetName);
    }

    commitRule(evt?: Event) {
        evt?.preventDefault();

        let phaseName = this.m.add.form.getValue('phaseName');
        let ruleName = this.m.add.rule.RuleName;

        let isManualControl = phaseName == 'ManualControl';

        let ruleWithParameter: PolicyfilterRuleAndStaticParameterValues = {
            RuleName: ruleName,
            StaticParameterValues: {
                StoredValues: {},
            },
            RejectionReasonName: isManualControl ? null : this.m.add.form.getValue('rejectionReasonName'),
        };

        if (this.m.add.addToRuleWithValues) {
            this.m.add.addToRuleWithValues(ruleWithParameter);
        }

        let ruleSet = PolicyfilterRulesetHelper.copyAndNormalizeRuleset(this.m.current.ruleSet);

        if (phaseName == 'External') {
            ruleSet.ExternalRules.push(ruleWithParameter);
        } else if (phaseName == 'Internal') {
            ruleSet.InternalRules.push(ruleWithParameter);
        } else if (phaseName == 'ManualControl') {
            ruleSet.ManualControlOnAcceptedRules.push(ruleWithParameter);
        } else {
            this.toastr.warning('No phase named ' + phaseName + ' exists');
            this.addRuleModalRef?.hide();
            return;
        }

        this.addRuleModalRef?.hide();
        this.reload(ruleSet, true, this.m.form.getValue('ruleSetName'));
    }

    private async format(
        ruleSet: PolicyfilterRuleSet,
        getDefaultRejectionReasonByRuleName: (name: string) => string
    ): Promise<{
        internal: ModelPhase;
        external: ModelPhase;
        manualControl: ModelPhase;
    }> {
        let result: {
            internal: ModelPhase;
            external: ModelPhase;
            manualControl: ModelPhase;
        } = {
            internal: null,
            external: null,
            manualControl: null,
        };
        let rulesByKey: Dictionary<PolicyfilterRuleAndStaticParameterValues> = {};
        let allModuleRows: ModelRuleRow[] = [];

        let setupPhase = (
            getPhaseRules: (rs: PolicyfilterRuleSet) => PolicyfilterRuleAndStaticParameterValues[],
            displayName: string,
            isManualControl: boolean
        ) => {
            let rejectionReasonNames: string[] = isManualControl ? null : [];
            let rules = getPhaseRules(ruleSet).map((x, index) => {
                let rejectionReasonName = isManualControl
                    ? null
                    : x.RejectionReasonName ?? getDefaultRejectionReasonByRuleName(x.RuleName);
                let key = generateUniqueId(10);
                rulesByKey[key] = x;
                let moduleRow: ModelRuleRow = {
                    key: key,
                    ruleAndStaticParameterValues: x,
                    display: null,
                    removeRow: () => {
                        let clone = PolicyfilterRulesetHelper.copyAndNormalizeRuleset(ruleSet);
                        let phaseRules = getPhaseRules(clone);
                        phaseRules.splice(index, 1);
                        return clone;
                    },
                    rejectionReasonName: rejectionReasonName,
                    hasActiveRejectionReason: false,
                };
                rejectionReasonNames?.push(rejectionReasonName);
                allModuleRows.push(moduleRow);
                return moduleRow;
            });
            let phase: ModelPhase = {
                displayName: displayName,
                rules: rules,
                activeRejectionReasonName: null,
                rejectionReasonNames: isManualControl ? null : distinct(rejectionReasonNames),
            };
            return phase;
        };

        result.internal = setupPhase((x) => x.InternalRules, 'Internal', false);
        result.external = setupPhase((x) => x.ExternalRules, 'External', false);
        result.manualControl = setupPhase((x) => x.ManualControlOnAcceptedRules, 'Manual control', true);

        let x = await this.initialData.getDisplayFormattedRules(rulesByKey);

        for (let moduleRow of allModuleRows) {
            moduleRow.display = x.DisplayFormattedRulesByKey[moduleRow.key];
        }

        return result;
    }
}

class Model {
    isEditAllowed: boolean;
    inEditMode: boolean;
    form: FormsHelper;
    current: RuleSetDisplayModel;
    currentSlotName: string;
    addableRules: { rule: PolicyFilterRuleUiModel; setupAdd: (m: AddStaticParameterModel) => Promise<void> }[];
    add?: AddStaticParameterModel;
    allPhases: { Code: string; DisplayName: string }[];
    allRejectionReasons: { Code: string; DisplayName: string }[];
}
export class ModelPhase {
    displayName: string;
    rules: ModelRuleRow[];
    activeRejectionReasonName: string;
    rejectionReasonNames: string[];
}
export class ModelRuleRow {
    key: string;
    ruleAndStaticParameterValues: PolicyfilterRuleAndStaticParameterValues;
    display: DisplayFormattedPolicyFilterRulesetRule;
    rejectionReasonName: string;
    hasActiveRejectionReason: boolean;
    removeRow: () => PolicyfilterRuleSet;
}

export interface RuleSetDisplayModel {
    ruleSetName: string;
    ruleSet: PolicyfilterRuleSet;
    internal: ModelPhase;
    external: ModelPhase;
    manualControl: ModelPhase;
}

export interface PolicyfilterRuleSet {
    InternalRules: PolicyfilterRuleAndStaticParameterValues[];
    ExternalRules: PolicyfilterRuleAndStaticParameterValues[];
    ManualControlOnAcceptedRules: PolicyfilterRuleAndStaticParameterValues[];
}

export interface DisplayFormattedPolicyFilterRulesetRule {
    RuleName: string;
    RuleDisplayName: string;
    Description: string;
    StaticParametersDisplayWithNames: string;
    StaticParametersDisplayWithoutNames: string;
}

export interface PolicyfilterRuleAndStaticParameterValues {
    RuleName: string;
    StaticParameterValues: PolicyfilterStaticParameterSet;
    RejectionReasonName: string;
}

export interface PolicyfilterStaticParameterSet {
    StoredValues: Dictionary<string>;
}

export interface PolicyFilterRuleUiModel {
    RuleName: string;
    RuleDisplayName: string;
    Description: string;
    StaticParameters: PolicyFilterRuleStaticParameterUiModel[];
    DefaultRejectionReasonName: string;
}

export interface PolicyFilterRuleStaticParameterUiModel {
    Name: string;
    IsList: boolean;
    TypeCode: string;
    Options: { Code: string; DisplayName: string }[];
}

export class PolicyfilterRulesetInitialData {
    isAbTestingActive: boolean;
    ruleSet: PolicyfilterRuleSet;
    ruleSetName: string;
    slotName: string;
    onSave: (data: {
        editedRuleSet: PolicyfilterRuleSet;
        newRuleSetName: string;
    }) => Promise<{ showPageAfter: boolean }>;
    allRules: PolicyFilterRuleUiModel[];
    getDisplayFormattedRules: (
        rulesByKey: Dictionary<PolicyfilterRuleAndStaticParameterValues>
    ) => Promise<{ DisplayFormattedRulesByKey: Dictionary<DisplayFormattedPolicyFilterRulesetRule> }>;
}
