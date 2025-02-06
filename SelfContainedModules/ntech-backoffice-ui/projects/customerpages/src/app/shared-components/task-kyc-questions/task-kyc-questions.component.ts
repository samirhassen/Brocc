import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { parseTriStateBoolean, stringJoin } from 'src/app/common.types';
import * as i18nIsoCountries from 'i18n-iso-countries';
import { CustomerPagesConfigService } from '../../common-services/customer-pages-config.service';
import { UntypedFormBuilder, Validators } from '@angular/forms';

@Component({
    selector: 'task-kyc-questions',
    templateUrl: './task-kyc-questions.component.html',
    styles: [],
})
export class TaskKycQuestionsComponent implements OnInit {
    constructor(private config: CustomerPagesConfigService, private fb: UntypedFormBuilder) {}

    public m: Model;

    @Input()
    public initialData: TaskKycQuestionsComponentInitialData;

    ngOnInit(): void {
        i18nIsoCountries.registerLocale(require('i18n-iso-countries/langs/sv.json'));
    }

    public getCountryDisplayName(twoLetterIsoCode: string) {
        let lang: string;
        if (this.config.baseCountry() === 'SE') {
            lang = 'sv';
        } else {
            return twoLetterIsoCode;
        }
        let name = i18nIsoCountries.getName(twoLetterIsoCode, lang, { select: 'official' });
        return name || twoLetterIsoCode;
    }

    public getCountryDropdownOptions(): { Code: string; DisplayName: string }[] {
        let twoLetterIsoCodes = Object.keys(i18nIsoCountries.getAlpha2Codes());
        //Reorder to have the base country first in the list so most customers wont have to scroll
        twoLetterIsoCodes = [
            this.config.baseCountry(),
            ...twoLetterIsoCodes.filter((x) => x !== this.config.baseCountry()),
        ];
        return twoLetterIsoCodes.map((x) => {
            return {
                Code: x,
                DisplayName: this.getCountryDisplayName(x),
            };
        });
    }

    public getQuestionText(code: string) {
        switch (code) {
            case 'citizenCountries':
                return 'Vilka medborgarskap har du?';
            case 'taxCountries':
                return 'Vilka är dina skatterättsliga hemvister?';
            case 'isPep':
                return 'Är du en person i politisk utsatt ställning, nära familje-medlem eller medarbetare till en sådan person?';
            default:
                return code;
        }
    }

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let t = this.initialData;

        let m: Model = {};

        let isEditPossible = t.isPossibleToAnswer && !t.isAnswersApproved;
        if (isEditPossible) {
            m.edit = {
                form: new FormsHelper(this.fb.group({})),
                customers: [],
            };

            for (let customer of t.customers) {
                let codes = ['citizenCountries', 'isPep', 'taxCountries'];
                let answers = [];
                for (let code of codes) {
                    answers.push({
                        code: code,
                        answerText: customer.LatestQuestions?.find((x) => x.QuestionCode === code)?.AnswerText,
                    });
                }
                let applicantDisplayName =
                    customer.CustomerShortName + (customer.CustomerBirthDate ? `, ${customer.CustomerBirthDate}` : '');
                let applicant = new CustomerEditModel(
                    customer.CustomerId,
                    customer.ApplicantNr,
                    applicantDisplayName,
                    `citizenCountries${customer.CustomerId}`,
                    [this.config.baseCountry()],
                    `isPep${customer.CustomerId}`,
                    `taxCountries${customer.CustomerId}`,
                    [this.config.baseCountry()]
                );

                if (customer.LatestQuestions) {
                    applicant.citizenCountries = this.parseCommaSeparatedList(
                        customer.LatestQuestions.find((x) => x.QuestionCode === 'citizenCountries')?.AnswerCode
                    );
                    applicant.taxCountries = this.parseCommaSeparatedList(
                        customer.LatestQuestions.find((x) => x.QuestionCode === 'taxCountries')?.AnswerCode
                    );
                }

                m.edit.form.addControlIfNotExists(applicant.citizenCountriesFormControlName, '', []);
                m.edit.form.addControlIfNotExists(applicant.taxCountriesFormControlName, '', []);
                m.edit.form.addControlIfNotExists(
                    applicant.isPepFormControlName,
                    customer.LatestQuestions?.find((x) => x.QuestionCode === 'isPep')?.AnswerCode,
                    [Validators.required]
                );

                m.edit.form.form.valueChanges.subscribe((_) => {
                    let addCitizenCountryValue = m.edit.form.getValue(applicant.citizenCountriesFormControlName);
                    if (addCitizenCountryValue) {
                        if (applicant.citizenCountries.indexOf(addCitizenCountryValue) < 0) {
                            applicant.citizenCountries.push(addCitizenCountryValue);
                        }
                        m.edit.form.setValue(applicant.citizenCountriesFormControlName, '');
                    }
                    let addTaxCountryValue = m.edit.form.getValue(applicant.taxCountriesFormControlName);
                    if (addTaxCountryValue) {
                        if (applicant.taxCountries.indexOf(addTaxCountryValue) < 0) {
                            applicant.taxCountries.push(addTaxCountryValue);
                        }
                        m.edit.form.setValue(applicant.taxCountriesFormControlName, '');
                    }
                });
                m.edit.customers.push(applicant);
            }
        } else {
            m.view = {
                customers: [],
                isEditAllowed: t.isPossibleToAnswer && !t.isAnswersApproved,
                isConfirmAllowed: t.isPossibleToAnswer && !t.isAnswersApproved,
            };
            for (let customer of t.customers) {
                let answers = customer.LatestQuestions
                    ? customer.LatestQuestions.map((x) => {
                          return { questionText: x.QuestionText, answerText: x.AnswerText };
                      })
                    : [
                          { questionText: this.getQuestionText('isPep'), answerText: '' },
                          { questionText: this.getQuestionText('citizenCountries'), answerText: '' },
                          { questionText: this.getQuestionText('taxCountries'), answerText: '' },
                      ];
                m.view.customers.push({
                    displayName:
                        customer.CustomerShortName +
                        (customer.CustomerBirthDate ? `, ${customer.CustomerBirthDate}` : ''),
                    answers: answers,
                });

                if (!customer.LatestQuestions) m.view.isConfirmAllowed = false;
            }
        }

        this.m = m;
    }

    onSave(evt?: Event) {
        evt?.preventDefault();

        let customersWithAnswers: {
            customerId: number;
            applicantNr: number;
            answers: CustomerPagesKycQuestionAnswerModel[];
        }[] = [];

        this.m.edit.customers.forEach((customer) => {
            let answers = [];
            let isPep = parseTriStateBoolean(this.m.edit.form.getValue(customer.isPepFormControlName));
            answers.push({
                QuestionCode: 'isPep',
                AnswerCode: isPep === true ? 'true' : isPep === false ? 'false' : '',
                QuestionText: this.getQuestionText('isPep'),
                AnswerText: isPep === true ? 'Ja' : isPep === false ? 'Nej' : '',
            });
            answers.push({
                QuestionCode: 'citizenCountries',
                AnswerCode: stringJoin(',', customer.citizenCountries),
                QuestionText: this.getQuestionText('citizenCountries'),
                AnswerText: stringJoin(
                    ', ',
                    customer.citizenCountries.map((x) => this.getCountryDisplayName(x))
                ),
            });
            answers.push({
                QuestionCode: 'taxCountries',
                AnswerCode: stringJoin(',', customer.taxCountries),
                QuestionText: this.getQuestionText('taxCountries'),
                AnswerText: stringJoin(
                    ', ',
                    customer.taxCountries.map((x) => this.getCountryDisplayName(x))
                ),
            });
            customersWithAnswers.push({
                customerId: customer.customerId,
                applicantNr: customer.applicantNr,
                answers: answers,
            });
        });

        this.m.edit = null;

        this.initialData.onSave({ customersWithAnswers });
    }

    invalid() {
        return this.m.edit?.form.invalid() || this.m.edit?.customers.find((x) => x.invalid());
    }

    private parseCommaSeparatedList(value: string): string[] {
        if (!value) {
            return [];
        } else {
            return value.split(',').map((x) => x.trim());
        }
    }
}

class CustomerEditModel {
    constructor(
        public customerId: number,
        public applicantNr: number,
        public displayName: string,
        public citizenCountriesFormControlName: string,
        public citizenCountries: string[],
        public isPepFormControlName: string,
        public taxCountriesFormControlName: string,
        public taxCountries: string[]
    ) {}

    public isCitizenCountrySelected(isoCode: string) {
        return this.citizenCountries.indexOf(isoCode) >= 0;
    }

    public removeCitizenCountry(isoCode: string, evt?: Event) {
        evt?.preventDefault();

        let i = this.citizenCountries.indexOf(isoCode);
        if (i >= 0) {
            this.citizenCountries.splice(i, 1);
        }
    }

    public isTaxCountrySelected(isoCode: string) {
        return this.taxCountries.indexOf(isoCode) >= 0;
    }

    public removeTaxCountry(isoCode: string, evt?: Event) {
        evt?.preventDefault();

        let i = this.taxCountries.indexOf(isoCode);
        if (i >= 0) {
            this.taxCountries.splice(i, 1);
        }
    }

    invalid() {
        return this.citizenCountries.length === 0 || this.taxCountries.length === 0;
    }
}

class Model {
    view?: {
        isEditAllowed: boolean;
        isConfirmAllowed: boolean;
        customers: {
            displayName: string;
            answers: {
                questionText: string;
                answerText: string;
            }[];
        }[];
    };
    edit?: {
        form: FormsHelper;
        customers: CustomerEditModel[];
    };
}

export class TaskKycQuestionsComponentInitialData {
    applicationNr: string;
    isPossibleToAnswer: boolean;
    isAnswersApproved: boolean;
    customers: TaskKycQuestionsComponentCustomer[];
    onSave: (result: TaskKycQuestionsComponentSaveEventData) => void;
}

export interface TaskKycQuestionsComponentCustomer {
    CustomerId: number;
    ApplicantNr?: number;
    CustomerBirthDate: string;
    CustomerShortName: string;
    LatestKycQuestionsAnswerDate: string;
    LatestQuestions: CustomerPagesKycQuestionAnswerModel[];
}

export interface CustomerPagesKycQuestionAnswerModel {
    QuestionCode: string;
    AnswerCode: string;
    QuestionText: string;
    AnswerText: string;
}

export interface TaskKycQuestionsComponentSaveEventData {
    customersWithAnswers: { customerId: number; applicantNr: number; answers: CustomerPagesKycQuestionAnswerModel[] }[];
}
