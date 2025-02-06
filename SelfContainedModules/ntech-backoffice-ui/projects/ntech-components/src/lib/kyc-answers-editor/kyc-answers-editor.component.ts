import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, ValidationErrors, Validators } from '@angular/forms';
import { BehaviorSubject } from 'rxjs';
import { Dictionary } from 'src/app/common.types';

@Component({
    selector: 'kyc-answers-editor',
    templateUrl: './kyc-answers-editor.component.html',
    styleUrls: ['./kyc-answers-editor.component.css'],
})
export class KycAnswersEditorComponent implements OnInit {
    constructor(private formBuilder: UntypedFormBuilder) {}

    ngOnInit(): void {}

    @Input()
    public initialData: KycAnswersEditorComponentInitialDataModel;

    public m: Model;

    async ngOnChanges(_: SimpleChanges) {
        this.m = null;

        let i = this.initialData;

        if (!i) {
            return;
        }

        await this.reset(i?.currentQuestionsTemplate);
    }

    private async reset(currentQuestionsTemplate: KycQuestionsSet) {
        if (!currentQuestionsTemplate) {
            return;
        }

        let m: Model = {
            relation: null,
            isCancelEnabled: !!this.initialData.onCancel,
            isSubmitEnabled: !!this.initialData.onAnswersSubmitted,
            staticTranslations: {
                saveAnswers: {
                    sv: 'Skicka in svar',
                    fi: 'Lähetä vastaukset',
                },
                yes: {
                    sv: 'Ja',
                    fi: 'Kyllä',
                },
                no: {
                    sv: 'Nej',
                    fi: 'Ei',
                },
                unsecuredLoan: {
                    sv: 'Lån utan säkerhet',
                    fi: 'Vakuudeton luotto',
                },
                savingsAccount: {
                    sv: 'Sparkonto',
                    fi: 'Säästötili',
                },
                selectCountry: {
                    sv: 'Välj land',
                    fi: 'Valitse maa',
                },
            },
            countryOptions: null,
        };

        if (currentQuestionsTemplate.questions.findIndex((x) => x.type == 'yesNoWithCountryOptions')) {
            m.countryOptions = await this.loadAndReorderCountries();
        }

        let f: { [index: string]: any } = {};
        let formValidators: ((x: UntypedFormGroup) => string)[] = [];
        for (let question of currentQuestionsTemplate.questions) {
            if (question.type === 'dropdown') {
                f[question.key] = ['', [Validators.required]];
            } else if (this.isYesNoQuestion(question)) {
                f[question.key] = ['', [Validators.required]];
                if (question.type === 'yesNoWithOptions') {
                    for (let option of question.options) {
                        f[this.getQuestionOptionFormKey(question, option)] = [false, []];
                    }
                    formValidators.push((x) => {
                        let isYes = x.value[question.key] === 'yes';
                        if (!isYes) {
                            return null;
                        }
                        for (let option of question.options) {
                            if (x.value[this.getQuestionOptionFormKey(question, option)] === true) {
                                return null;
                            }
                        }
                        return question.key;
                    });
                } else if (question.type === 'yesNoWithCountryOptions') {
                    f[question.optionsKey] = [[], []];
                    f['__country_select__' + question.optionsKey] = ['', []];
                    formValidators.push((x) => {
                        let isYes = x.value[question.key] === 'yes';
                        if (!isYes) {
                            return null;
                        }
                        let selectedCountries: string[] = x.value[question.optionsKey];
                        return selectedCountries.length > 0 ? null : question.key;
                    });
                }
            }
        }
        let form = this.formBuilder.group(f);
        form.setValidators((x) => {
            let errors = formValidators.map((validate) => validate(form)).filter((x) => !!x);
            if (errors.length > 0) {
                let result: ValidationErrors = {};
                for (let error of errors) {
                    result[error] = true;
                }
                return result;
            } else {
                return null;
            }
        });

        m.relation = {
            questionsVersion: currentQuestionsTemplate.version,
            questions: currentQuestionsTemplate.questions,
            form: form,
        };

        m.titleText = this.initialData.titleText ? this.initialData.titleText : null;

        this.m = m;

        //NOTE: Keep this after assingment to this.m or refactor parseAnswers to not use this.m
        if (this.initialData.editState) {
            this.m.relation.form.valueChanges.subscribe((_: any) => {
                this.initialData.editState.next(this.parseAnswers());
            });
        }
    }

    public isYesNoQuestion(question: KycQuestion) {
        return (
            question.type === 'yesNoWithOptions' ||
            question.type === 'yesNo' ||
            question.type === 'yesNoWithCountryOptions'
        );
    }

    private parseAnswers(): { isSavePossible: boolean; answers: KycQuestionAnswerModel[] } {
        let answers: KycQuestionAnswerModel[] = [];

        if (this.m.relation.form.invalid) {
            return { isSavePossible: false, answers: null };
        }

        for (let question of this.m.relation.questions) {
            let v = (x: string) => this.m.relation.form.value[x];
            if (question.type === 'dropdown') {
                let value = v(question.key);
                let selectedOption = question.options.find((x) => x.value === value);
                answers.push({
                    questionCode: question.key,
                    answerCode: value,
                    answerText: this.getTranslation(selectedOption.translations),
                    questionText: this.getTranslation(question.headerTranslations),
                });
            } else if (
                question.type === 'yesNoWithOptions' ||
                question.type === 'yesNo' ||
                question.type === 'yesNoWithCountryOptions'
            ) {
                let isYes = v(question.key) == 'yes';
                if (isYes) {
                    answers.push({
                        questionCode: question.key,
                        answerCode: 'true',
                        answerText: this.getStaticTranslation('yes'),
                        questionText: this.getTranslation(question.headerTranslations),
                    });
                    if (question.type === 'yesNoWithOptions') {
                        let optionCodes: string[] = [];
                        let optionTexts: string[] = [];
                        for (let option of question.options) {
                            if (v(this.getQuestionOptionFormKey(question, option)) === true) {
                                optionCodes.push(option.value);
                                optionTexts.push(this.getTranslation(option.translations));
                            }
                        }
                        answers.push({
                            questionCode: question.optionsKey,
                            answerCode: optionCodes.join(', '),
                            answerText: optionTexts.join(', '),
                            questionText:
                                this.getTranslation(question.headerTranslations) +
                                ' - ' +
                                this.getTranslation(question.optionsHeaderTranslations),
                        });
                    } else if (question.type === 'yesNoWithCountryOptions') {
                        let optionCodes: string[] = [];
                        let optionTexts: string[] = [];
                        for (let countryCode of v(question.optionsKey)) {
                            optionCodes.push(countryCode);
                            optionTexts.push(this.getCountryDisplayName(countryCode));
                        }
                        answers.push({
                            questionCode: question.optionsKey,
                            answerCode: optionCodes.join(', '),
                            answerText: optionTexts.join(', '),
                            questionText:
                                this.getTranslation(question.headerTranslations) +
                                ' - ' +
                                this.getTranslation(question.optionsHeaderTranslations),
                        });
                    }
                } else {
                    answers.push({
                        questionCode: question.key,
                        answerCode: 'false',
                        answerText: this.getStaticTranslation('no'),
                        questionText: this.getTranslation(question.headerTranslations),
                    });
                }
            }
        }

        return {
            isSavePossible: true,
            answers: answers,
        };
    }

    onCountryOptionChanged(question: KycQuestion, countryCode: string) {
        let currentValue: string[] = this.m.relation.form.value[question.optionsKey];
        let patch: Dictionary<any> = {};
        if (currentValue.indexOf(countryCode) < 0) {
            currentValue.push(countryCode);
            patch[question.optionsKey] = currentValue;
        }

        patch['__country_select__' + question.optionsKey] = '';
        this.m.relation.form.patchValue(patch);
    }

    getCountryDisplayName(code: string) {
        return this.m.countryOptions.find((x) => x.code == code).displayName;
    }

    removeCountryOption(question: KycQuestion, countryCode: string, evt?: Event) {
        evt?.preventDefault();
        let currentValue: string[] = this.m.relation.form.value[question.optionsKey];
        let index = currentValue.indexOf(countryCode);
        if (index >= 0) {
            currentValue.splice(index, 1);
            this.m.relation.form.patchValue({
                [question.optionsKey]: currentValue,
            });
        }
    }

    countriesExceptChosen(question: KycQuestion, countryOptions: { code: string; displayName: string }[]) {
        let chosenCountries: string[] = this.m.relation.form.value[question.optionsKey];
        return countryOptions.filter((x) => chosenCountries.indexOf(x.code) < 0);
    }

    async submitAnswers(evt?: Event) {
        evt?.preventDefault();

        let { answers } = this.parseAnswers();

        await this.initialData.onAnswersSubmitted(answers);
    }

    async cancel(evt?: Event) {
        evt?.preventDefault();
        await this.initialData.onCancel();
    }

    getQuestionOptionFormKey(
        question: KycQuestion,
        option: { value: string; translations: { [index: string]: string } }
    ) {
        return `${question.key}_${option.value}`;
    }

    getStaticTranslation(key: string) {
        return this.getTranslation(this.m.staticTranslations[key]) || key;
    }

    getTranslation(translations: { [index: string]: string }) {
        if (!translations) {
            return '';
        }
        let preferredTranslation = translations[this.initialData.language.value];
        if (preferredTranslation) {
            return preferredTranslation;
        } else {
            let availableTranslations = Object.keys(translations);
            if (availableTranslations.length > 0) {
                return translations[availableTranslations[0]];
            } else {
                return '';
            }
        }
    }

    private async loadAndReorderCountries() {
        let countries = await this.initialData.getCountries();
        return getOrderedCountryDropdownOptionsFromIsoCountries(
            countries,
            this.initialData.clientTwoLetterCountryIsoCode,
            this.initialData.language.value
        );
    }
}

interface Model {
    titleText?: string;
    staticTranslations: { [index: string]: { [index: string]: string } };
    relation: RelationModel;
    isSubmitEnabled: boolean;
    isCancelEnabled: boolean;
    countryOptions: { code: string; displayName: string }[];
}

interface RelationModel {
    questionsVersion: string;
    questions: KycQuestion[];
    form: UntypedFormGroup;
}

export interface KycAnswersEditorComponentInitialDataModel {
    language: BehaviorSubject<string>;
    currentQuestionsTemplate: KycQuestionsSet;
    getCountries: () => Promise<IsoCountry[]>;
    clientTwoLetterCountryIsoCode: string;
    onCancel?: () => Promise<void>;
    onAnswersSubmitted?: (answers: KycQuestionAnswerModel[]) => Promise<void>;
    titleText?: string;
    editState?: BehaviorSubject<{ isSavePossible: boolean; answers: KycQuestionAnswerModel[] }>;
}

export interface KycQuestion {
    type: string;
    key: string;
    headerTranslations?: { [index: string]: string };
    options?: {
        value: string;
        translations: { [index: string]: string };
    }[];
    optionsKey?: string;
    optionsHeaderTranslations?: { [index: string]: string };
}

export interface KycQuestionAnswerModel {
    questionCode: string;
    answerCode: string;
    questionText: string;
    answerText: string;
}

export interface KycAnswersModel {
    relationType: string;
    relationId: string;
    answerDate: string;
    answers: KycQuestionAnswerModel[];
}

export interface KycCustomerStatus {
    activeRelations: {
        relationType: string;
        relationId: string;
        isUpdateRequired: boolean;
        nrOfDaysSinceAnswer: number;
    }[];
    questionTemplates: {
        activeProducts: {
            relationType: string;
            currentQuestionsTemplate: KycQuestionsSet;
            historicalModels: {
                id: number;
                date: string;
            }[];
        }[];
    };
    latestAnswers: KycAnswersModel[];
    historicalAnswers: KycAnswersModel[];
}

export interface KycQuestionsSet {
    version: string;
    questions: KycQuestion[];
}

export interface KycQuestion {
    type: string;
    key: string;
    headerTranslations?: { [index: string]: string };
    options?: {
        value: string;
        translations: { [index: string]: string };
    }[];
    optionsKey?: string;
    optionsHeaderTranslations?: { [index: string]: string };
}

/*
Order countries by display name but move the client's country to the top of the list.
*/
export function getOrderedCountryDropdownOptions<T>(
    countries: T[],
    clientTwoLetterCountryIsoCode: string,
    getDisplayName: (value: T) => string,
    getCountryIsoCode: (value: T) => string
): DropdownOption[] {
    let defaultChoiceCountryCodes = [clientTwoLetterCountryIsoCode];
    if (clientTwoLetterCountryIsoCode == 'FI') {
        defaultChoiceCountryCodes.push('SE');
    }
    let orderedCountries = orderByStr(
        countries.map((x) => ({
            code: getCountryIsoCode(x),
            displayName: getDisplayName(x),
        })),
        (x) => x.displayName
    );

    return [
        ...orderedCountries.filter((x) => defaultChoiceCountryCodes.includes(x.code)),
        ...orderedCountries.filter((x) => !defaultChoiceCountryCodes.includes(x.code)),
    ];
}

export function getOrderedCountryDropdownOptionsFromIsoCountries(
    countries: IsoCountry[],
    clientTwoLetterCountryIsoCode: string,
    userLanguage: string
): DropdownOption[] {
    return getOrderedCountryDropdownOptions(
        countries,
        clientTwoLetterCountryIsoCode,
        (x) => x.translatedNameByLang2[userLanguage] ?? x.commonName,
        (x) => x.iso2Name
    );
}

function orderByStr<T>(source: T[], getSortKey: (value: T) => string): T[] {
    return [...source].sort((x, y) => getSortKey(x).localeCompare(getSortKey(y)));
}

export interface DropdownOption {
    code: string;
    displayName: string;
}

export interface IsoCountry {
    commonName: string;
    nativeName: string;
    iso2Name: string;
    iso3Name: string;
    translatedNameByLang2: { [index: string]: string };
}
