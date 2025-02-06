import { Component, Input, SimpleChanges } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { KycTemplateService } from '../../services/kyc-template.service';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'edit-questions-template',
    templateUrl: './edit-questions-template.component.html',
    styles: ['.last-button { margin-right: 49px; }'],
})
export class EditQuestionsTemplateComponent {
    constructor(private apiService: KycTemplateService, private config: ConfigService, private toastr: ToastrService) {}

    @Input()
    public initialData: EditQuestionsTemplateComponentInitialData;

    public m: Model;

    public supportedQuestionTypes: { name: string; displayName: string }[] = [
        { name: 'yesNo', displayName: 'Yes / no question' },
        { name: 'yesNoWithOptions', displayName: 'Yes / no question with options' },
        { name: 'yesNoWithCountryOptions', displayName: 'Yes / no question with countries' },
        { name: 'dropdown', displayName: 'Dropdown question' },
    ];

    ngOnChanges(_: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        this.m = {
            isEditOrderMode: false,
            questions: null,
            version: null,
            selectedTypeToAdd: 'yesNo',
            isFi: this.config.baseCountry() === 'FI',
        };

        this.setJsonQuestions();
    }

    setJsonQuestions() {
        if (!this.initialData.initialModelJson) {
            return;
        }

        const obj: JsonModel = JSON.parse(this.initialData.initialModelJson);

        this.m.questions = obj.questions;
        this.m.version = obj.version;
    }

    getQuestionsWithType(type: string): Question[] {
        if (this.supportedQuestionTypes.map((q) => q.name === type) === null) {
            this.toastr.error(`Unsupported question with type: '${type}'. Contact support.`);
        }

        return this.m.questions.filter((x) => x.type === type);
    }

    removeQuestion(q: Question) {
        const index = this.m.questions.indexOf(q, 0);
        if (index > -1) {
            this.m.questions.splice(index, 1);
        }
    }

    onAddNewQuestion(evt?: Event) {
        evt?.preventDefault();

        const q: Question = {
            type: this.m.selectedTypeToAdd,
            key: '',
            headerTranslations: this.m.isFi ? { fi: '', sv: '' } : { sv: '' },
            optionsKey: '',
            optionsHeaderTranslations: this.m.isFi ? { fi: '', sv: '' } : { sv: '' },
            options: null,
        };

        if (!this.m.questions?.length) {
            this.m.questions = [q];
            return;
        }

        this.m.questions.push(q);
    }

    addOption(q: Question) {
        const option = {
            value: '',
            translations: this.m.isFi
                ? {
                      sv: '',
                      fi: '',
                  }
                : { sv: '' },
        };

        if (!q.options?.length) {
            q.options = [option];
            return;
        }

        q.options.push(option);
    }

    deleteOption(q: Question, index: number) {
        q.options.splice(index, 1);
    }

    async onContinueToChangeQuestionOrder(evt?: Event) {
        evt?.preventDefault();

        const validatedKycQuestionsTemplate = await this.getValidatedKycQuestionTemplate();
        if (validatedKycQuestionsTemplate) {
            this.m.isEditOrderMode = true;
        }
    }

    onChangeOrder(q: Question, isMoveUp: boolean) {
        const fromIndex = this.m.questions.indexOf(q);
        const toIndex = isMoveUp ? fromIndex - 1 : fromIndex + 1;

        const element = this.m.questions[fromIndex];
        this.m.questions.splice(fromIndex, 1);
        this.m.questions.splice(toIndex, 0, element);
    }

    async cancelEdit(evt?: Event) {
        evt?.preventDefault();
        await this.initialData.onCancel();
    }

    async commitEdit(evt?: Event) {
        evt?.preventDefault();

        const validatedKycQuestionTemplate = await this.getValidatedKycQuestionTemplate();
        if (validatedKycQuestionTemplate) {
            await this.initialData.onSave(validatedKycQuestionTemplate);
        }
    }

    async getValidatedKycQuestionTemplate(): Promise<string | null> {
        const newQuestions = {
            version: this.m.version,
            questions: this.m.questions,
        };

        const newQuestionsJson = JSON.stringify(newQuestions);

        const { isValid, validationErrorMessage } = await this.apiService.validateKycQuestionTemplateModelData(
            newQuestionsJson
        );

        if (!isValid) {
            this.toastr.warning(validationErrorMessage);
            return null;
        }

        return newQuestionsJson;
    }

    col() {
        return {
            'col-xs-3': this.m.isFi,
            'col-xs-6': !this.m.isFi,
        };
    }

    showUpButton(q: Question) {
        return this.m.questions.indexOf(q) !== 0;
    }

    showDownButton(q: Question) {
        return this.m.questions.indexOf(q) !== this.m.questions.length - 1;
    }

    isLastQuestion(q: Question) {
        return this.m.questions.indexOf(q) === this.m.questions.length - 1;
    }
}

interface Model {
    isEditOrderMode: boolean;
    questions: Question[];
    version: string;
    isFi: boolean;
    selectedTypeToAdd: string;
}

interface JsonModel {
    version: string;
    questions: Question[];
}

export class EditQuestionsTemplateComponentInitialData {
    initialModelJson?: string;
    onSave: (modelJson: string) => Promise<void>;
    onCancel: () => Promise<void>;
}

interface Question {
    type: string;
    key: string;
    headerTranslations: {
        sv: string;
        fi?: string;
    };
    optionsKey: string | null;
    optionsHeaderTranslations: {
        sv: string;
        fi?: string;
    } | null;
    options: {
        value: string;
        translations: {
            sv: string;
            fi?: string;
        };
    }[];
}
