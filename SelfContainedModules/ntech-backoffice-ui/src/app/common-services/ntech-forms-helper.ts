import {
    AbstractControl,
    AbstractControlOptions,
    AsyncValidatorFn,
    UntypedFormControl,
    UntypedFormGroup,
    ValidationErrors,
    ValidatorFn,
    Validators,
} from '@angular/forms';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { Dictionary } from '../common.types';

/**
 * Internal form handling using reactive forms.
 */
export class FormsHelper {
    constructor(public form: UntypedFormGroup) {}

    public getFormControl(groupName: string, controlName: string): AbstractControl {
        if (!this.form) {
            return null;
        }

        return groupName ? this.form?.get(groupName)?.get(controlName) : this.form?.get(controlName);
    }

    public hasFormControl(groupName: string, controlName: string) {
        return !!this.getFormControl(groupName, controlName);
    }

    public getValue(controlName: string) {
        let control = this.getFormControl(null, controlName);
        return control?.value;
    }

    public setValue(controlName: string, value: any) {
        let control = this.getFormControl(null, controlName);
        return control?.setValue(value);
    }

    public setValue2(groupName: string, controlName: string, value: any) {
        let control = this.getFormControl(groupName, controlName);
        return control?.setValue(value);
    }

    public getFormGroupValue(groupName: string, controlName: string) {
        let control = this.getFormControl(groupName, controlName);
        return control?.value;
    }

    public getFormGroupValue2(groupName1: string, groupName2: string, controlName: string) {
        let group2 = this.getFormControl(groupName1, groupName2);
        return group2?.get(controlName)?.value;
    }

    public hasError(controlName: string, groupName?: string) {
        let control = this.getFormControl(groupName, controlName);
        return control && control.invalid && control.dirty;
    }

    public hasAnyError(controlName: string, formValidationErrorName: string, groupName?: string) {
        let control = this.getFormControl(groupName, controlName);
        return this.hasError(controlName, groupName) || (this.hasNamedValidationError(formValidationErrorName) && control.dirty);
    }

    public hasErrorStrict(controlName: string, groupName?: string) {
        let control = this.getFormControl(groupName, controlName);
        return control && control.invalid;
    }

    public hasNamedValidationError(validatorName: string) {
        return this.form.errors && this.form.errors[validatorName] === true;
    }

    public ensureControlOnConditionOnly(
        shouldExist: boolean,
        controlName: string,
        getInitialValue: () => any,
        getValidators: () => ValidatorFn | ValidatorFn[] | AbstractControlOptions | null,
        getAsyncValidators?: () => AsyncValidatorFn | AsyncValidatorFn[] | null
    ) {
        if (!shouldExist && this.form.contains(controlName)) {
            this.form.removeControl(controlName);
        } else if (shouldExist) {
            this.addControlIfNotExists(
                controlName,
                getInitialValue(),
                getValidators(),
                getAsyncValidators ? getAsyncValidators() : null
            );
        }
    }

    public addControlIfNotExists(
        controlName: string,
        initialValue: any,
        validators: ValidatorFn | ValidatorFn[] | AbstractControlOptions | null,
        asyncValidators?: AsyncValidatorFn | AsyncValidatorFn[] | null
    ) {
        if (this.form.contains(controlName)) {
            return;
        }
        let control = new UntypedFormControl(initialValue, validators, asyncValidators ? asyncValidators : null);
        this.form.addControl(controlName, control);
    }

    public addGroupIfNotExists(
        groupName: string,
        controls: NewFormGroupControl[]
    ) {
        if (this.form.contains(groupName)) {
            return null;
        }
        let groupControls: Dictionary<UntypedFormControl> = {};
        for (let control of controls) {
            if (control) {
                //To allow conditionally skipping things with inline logic
                groupControls[control.controlName] = new UntypedFormControl(
                    control.initialValue,
                    control.validators,
                    control.asyncValidators
                );
            }
        }
        let group = new UntypedFormGroup(groupControls);
        this.form.addControl(groupName, group);
        return group;
    }

    setDisabled(groupName: string, controlName: string, isDisabled: boolean, emitEvent?: boolean) {
        let control = this.getFormControl(groupName, controlName);
        if (!control) {
            return;
        }
        //NOTE: The reason we care about emitEvent is that angular has a "feature" (that any sane person would consider a bug)
        //      where changing disabled triggers valueChange. This is supressed by emitEvent: false so we flip the default here from insane way to the sane way.
        if (isDisabled && !control.disabled) {
            control.disable({ emitEvent: !!emitEvent });
        } else if (!isDisabled && control.disabled) {
            control.enable({ emitEvent: !!emitEvent });
        }
    }

    invalid() {
        return this.form?.invalid;
    }

    setFormValidator(validate: (control: AbstractControl) => boolean, validatorName?: string) {
        this.setFormValidators([{ validate, validatorName }]);
    }

    setFormValidators(validators: { validate: (control: AbstractControl) => boolean; validatorName?: string }[]) {
        let actualValidator: ValidatorFn;
        if (validators.length > 1) {
            let actualValidators: ValidatorFn[] = [];
            for (let validator of validators) {
                actualValidators.push(FormsHelper.createFormValidator(validator.validate, validator.validatorName));
            }
            actualValidator = Validators.compose(actualValidators);
        } else {
            actualValidator = FormsHelper.createFormValidator(validators[0].validate, validators[0].validatorName);
        }
        this.form.setValidators(actualValidator);
        this.form.updateValueAndValidity();
    }

    public ensureGroupedControlOnConditionOnly(
        shouldExist: boolean,
        groupName: string,
        controlName: string,
        getInitialValue: () => any,
        getValidators: () => ValidatorFn | ValidatorFn[] | AbstractControlOptions | null,
        getAsyncValidators?: () => AsyncValidatorFn | AsyncValidatorFn[] | null
    ) {
        let group = this.form.controls[groupName] as UntypedFormGroup;
        if (!shouldExist && group?.contains(controlName)) {
            group.removeControl(controlName);
        } else if (shouldExist && group) {
            this.addGroupedControlIfNotExists(
                group,
                controlName,
                getInitialValue(),
                getValidators(),
                getAsyncValidators ? getAsyncValidators() : null
            );
        }
    }

    private addGroupedControlIfNotExists(
        group: UntypedFormGroup,
        controlName: string,
        initialValue: string,
        validators: ValidatorFn | ValidatorFn[] | AbstractControlOptions | null,
        asyncValidators?: AsyncValidatorFn | AsyncValidatorFn[] | null
    ) {
        if (group.contains(controlName)) {
            return;
        }
        let control = new UntypedFormControl(initialValue, validators, asyncValidators ? asyncValidators : null);
        group.addControl(controlName, control);
    }

    static createFormValidator(validate: (control: AbstractControl) => boolean, validatorName?: string): ValidatorFn {
        let validateActual: ValidatorFn = (x) => {
            let isValid = validate(x);
            if (isValid) {
                return null;
            } else {
                let errors: Dictionary<boolean> = {};
                errors[validatorName || 'formInvalid'] = true;
                return errors;
            }
        };
        return validateActual;
    }

    static loadSingleAttachedFileAsDataUrl(attachedFiles: FileList): Promise<{ dataUrl: string; filename: string }> {
        return new Promise<{ dataUrl: string; filename: string }>((resolve, reject) => {
            if (attachedFiles.length == 1) {
                let r = new FileReader();
                var f = attachedFiles[0];
                if (f.size > 10 * 1024 * 1024) {
                    reject('Attached file is too big!');
                }
                r.onloadend = (e) => {
                    let result = {
                        dataUrl: (<any>e.target).result,
                        filename: f.name,
                    };
                    resolve(result);
                };
                r.readAsDataURL(f);
            } else if (attachedFiles.length == 0) {
                reject('No agreement attached!');
            } else {
                reject('Multiple files have been attached. Please reload the page and only attach a single file.');
            }
        });
    }

    static createValidatorAsync(
        validatorName: string,
        isValid: (value: string) => Observable<boolean>
    ): (control: AbstractControl) => Observable<ValidationErrors> | null {
        return (x) => {
            let isMissing = x.value === '' || x.value === null || x.value === undefined;
            if (isMissing) {
                return of(null);
            }

            return isValid(x.value).pipe(
                map((y) => {
                    if (y) {
                        return null;
                    } else {
                        let err: Dictionary<any> = {};
                        err[validatorName] = 'invalid';
                        return err;
                    }
                })
            );
        };
    }
}

export interface NewFormGroupControl {
    controlName: string;
    initialValue: any;
    validators: ValidatorFn | ValidatorFn[] | AbstractControlOptions | null;
    asyncValidators?: AsyncValidatorFn | AsyncValidatorFn[] | null;
}