import { Component, Input, OnInit } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'view-questions-template',
    templateUrl: './view-questions-template.component.html',
    styles: [],
})
export class ViewQuestionsTemplateComponent implements OnInit {
    constructor(private config: ConfigService) {}

    @Input()
    public modelJson: string;

    public m: ModelJson;

    ngOnInit(): void {
        if (!this.modelJson) return;

        this.tryParseJson();
    }

    tryParseJson() {
        const obj: ModelJson = JSON.parse(this.modelJson);

        this.m = {
            questions: obj.questions,
        };
    }

    getUiLanguage(): string {
        return this.config.baseCountry();
    }
}

interface ModelJson {
    questions: Question[];
}

interface Question {
    type: string;
    key: string;
    headerTranslations: {
        sv: string;
        fi: string;
    };
    optionsKey: string | null;
    optionsHeaderTranslations: {
        sv: string;
        fi: string;
    } | null;
    options: {
        value: string;
        translations: {
            sv: string;
            fi: string;
        };
    }[];
}
