import { UntypedFormBuilder, ValidatorFn, Validators } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import {
    PolicyfilterRuleAndStaticParameterValues,
    PolicyfilterRuleSet,
    PolicyFilterRuleUiModel,
} from './policyfilter-ruleset.component';

/*
This class is a somewhat weak attempt at making PolicyfilterRulesetComponent less massive.
With more time it might be better to break it down into several actual components.
 */
export class PolicyfilterRulesetHelper {
    public static copyAndNormalizeRuleset(item: PolicyfilterRuleSet): PolicyfilterRuleSet {
        let copy = JSON.parse(JSON.stringify(item));
        if (!copy.InternalRules) {
            copy.InternalRules = [];
        }
        if (!copy.ExternalRules) {
            copy.ExternalRules = [];
        }
        if (!copy.ManualControlOnAcceptedRules) {
            copy.ManualControlOnAcceptedRules = [];
        }
        return copy;
    }

    public static createAddStaticParameterModel(
        ruleRow: { rule: PolicyFilterRuleUiModel; setupAdd: (m: AddStaticParameterModel) => Promise<void> },
        fb: UntypedFormBuilder
    ) {
        let form = new FormsHelper(fb.group({}));

        form.addControlIfNotExists('phaseName', '', [Validators.required]);
        form.addControlIfNotExists('rejectionReasonName', ruleRow.rule.DefaultRejectionReasonName, [
            Validators.required,
        ]);

        let addModel: AddStaticParameterModel = {
            rule: ruleRow.rule,
            form: form,
            addToRuleWithValues: null,
        };
        if (ruleRow.setupAdd) {
            ruleRow.setupAdd(addModel);
        }

        return addModel;
    }

    public static createAddableRules(
        allRules: PolicyFilterRuleUiModel[],
        validationSevice: NTechValidationService
    ): { rule: PolicyFilterRuleUiModel; setupAdd: (m: AddStaticParameterModel) => Promise<void> }[] {
        let addableRules: { rule: PolicyFilterRuleUiModel; setupAdd: (m: AddStaticParameterModel) => Promise<void> }[] =
            [];

        for (let rule of allRules) {
            if (!rule.StaticParameters || rule.StaticParameters.length === 0) {
                //No parameters
                addableRules.push({ rule: rule, setupAdd: null });
            } else if (rule.StaticParameters.length === 1 && !rule.StaticParameters[0].IsList) {
                //Single value parameter
                addableRules.push({
                    rule: rule,
                    setupAdd: (x) => {
                        return new Promise<void>((resolve) => {
                            let p = rule.StaticParameters[0];

                            let extraValidator: ValidatorFn = null;
                            if (p.TypeCode === 'Int') {
                                extraValidator = validationSevice.getIntegerWithBoundsValidator(null, null);
                                x.addToRuleWithValues = (r) =>
                                    (r.StaticParameterValues.StoredValues[p.Name] = validationSevice
                                        .parseInteger(x.form.getValue('singleNonListParameterValue'))
                                        .toString());
                            } else if (p.TypeCode === 'Decimal') {
                                extraValidator = validationSevice.getDecimalValidator();
                                x.addToRuleWithValues = (r) =>
                                    (r.StaticParameterValues.StoredValues[p.Name] = validationSevice
                                        .parseDecimalOrNull(x.form.getValue('singleNonListParameterValue'), true)
                                        .toString()
                                        .replace(',', '.'));
                            } else if (p.TypeCode === 'Percent') {
                                extraValidator = validationSevice.getPositiveDecimalValidator();
                                x.addToRuleWithValues = (r) =>
                                    (r.StaticParameterValues.StoredValues[p.Name] = validationSevice
                                        .parsePositiveDecimalOrNull(x.form.getValue('singleNonListParameterValue'))
                                        .toString()
                                        .replace(',', '.'));
                            } else {
                                x.addToRuleWithValues = (r) =>
                                    (r.StaticParameterValues.StoredValues[p.Name] =
                                        x.form.getValue('singleNonListParameterValue'));
                            }

                            x.form.addControlIfNotExists(
                                'singleNonListParameterValue',
                                '',
                                extraValidator ? [Validators.required, extraValidator] : [Validators.required]
                            );
                            x.singleNonListParameter = {
                                name: p.Name,
                                typeCode: p.TypeCode,
                            };
                            resolve();
                        });
                    },
                });
            } else if (
                rule.StaticParameters.length === 1 &&
                rule.StaticParameters[0].IsList &&
                rule.StaticParameters[0].Options &&
                rule.StaticParameters[0].TypeCode === 'String'
            ) {
                //Single dropdown parameter
                addableRules.push({
                    rule: rule,
                    setupAdd: (x) => {
                        return new Promise<void>((resolve) => {
                            let p = rule.StaticParameters[0];

                            x.form.addControlIfNotExists('singleListParameter', '', []);
                            let selectedOptions: { Code: string; DisplayName: string }[] = [];
                            x.form.setFormValidator((x) => {
                                return selectedOptions.length > 0;
                            }, 'hasAtLeastOneValue');

                            x.addToRuleWithValues = (r) =>
                                (r.StaticParameterValues.StoredValues[p.Name] = JSON.stringify(
                                    selectedOptions.map((y) => y.Code)
                                ));

                            x.singleListParameter = {
                                name: p.Name,
                                options: p.Options,
                                selectedOptions: selectedOptions,
                                onAdd: () => {
                                    let code = x.form.getValue('singleListParameter');
                                    if (!code) {
                                        return;
                                    }
                                    let option = p.Options.find((y) => y.Code === code);
                                    if (selectedOptions.findIndex((y) => y.Code === code) < 0) {
                                        selectedOptions.push(option);
                                    }
                                    x.form.setValue('singleListParameter', '');
                                },
                                onRemove: (option, evt) => {
                                    evt.preventDefault();
                                    let i = selectedOptions.findIndex((y) => y.Code === option.Code);
                                    if (i >= 0) {
                                        selectedOptions.splice(i, 1);
                                    }
                                    x.form.form.updateValueAndValidity();
                                },
                            };
                            resolve();
                        });
                    },
                });
            }
        }
        return addableRules;
    }
}

export class AddStaticParameterModel {
    rule: PolicyFilterRuleUiModel;
    form: FormsHelper;
    addToRuleWithValues: (ruleWithValues: PolicyfilterRuleAndStaticParameterValues) => void;
    singleNonListParameter?: {
        name: string;
        typeCode: string;
    };
    singleListParameter?: {
        name: string;
        options: { Code: string; DisplayName: string }[];
        selectedOptions: { Code: string; DisplayName: string }[];
        onAdd: () => void;
        onRemove: (option: { Code: string; DisplayName: string }, evt: Event) => void;
    };
}
