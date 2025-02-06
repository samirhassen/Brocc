import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { KycAnswersEditorComponentInitialDataModel, KycQuestionAnswerModel, KycQuestionsSet } from 'projects/ntech-components/src/public-api';
import { BehaviorSubject } from 'rxjs';
import { CustomerPagesApiService } from '../../../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';

type Translations = { [index: string]: { [index: string]: string } }

const staticTranslations : Translations =  {
    'sessionCompleteText': {
        'sv': 'Frågorna är nu besvarade. Du kan stänga sidan.',
        'fi': 'Kysymykset on nyt vastattu. Voit sulkea sivun.'
    },
    'sessionCompleteRedirectText': {
        'sv': 'Frågorna är nu besvarande. Du kommer inom kort att skickas vidare.',
        'fi': 'Kysymykset on nyt vastattu. Sinut ohjataan pian eteenpäin.',
    },
    'redirectLinkText': {
        'sv': 'Nästa sida...',
        'fi': 'Seuraava sivu...'
    },
    'kyc': {
        'sv': 'Kundkännedom',
        'fi': 'Asiakkaan tuntemus'
    },
    'saveAnswers': {
        'sv': 'Skicka in svar',
        'fi': 'Lähetä vastaukset'
    },
    'backLinkText': {
        'sv': 'Tillbaka',
        'fi': 'Takaisin'
    }
}

@Component({
    selector: 'np-questions-session',
    templateUrl: './questions-session.component.html',
    styles: [
    ]
})
export class QuestionsSessionComponent implements OnInit {
    constructor(private route: ActivatedRoute, private apiService: CustomerPagesApiService, private config: CustomerPagesConfigService) { }

    async ngOnInit() {
        this.reload(this.route.snapshot.params['sessionId']);
    }

    public m: Model;

    public getStaticTranslation(name: string) {
        let t = staticTranslations[name];
        let s = t ? t[this.m.language] : null;
        return s ?? name;
    }

    private async reload(sessionId: string) {
        this.m = null;

        if(!sessionId) {
            return;
        }

        let session = await this.loadSession(sessionId);
        
         let m: Model = {
            sessionId: session.sessionId,
            language: session.language,
            isActive: false,
            isCompleted: false,
            allowBackToRedirectUrl: session.allowBackToRedirectUrl,
            redirectUrl: session.allowBackToRedirectUrl ? session.redirectUrl : null
        };

        if(session.isActive) {
            m.isActive = true;
            m.customers = session.customers.map(x => ({
                fullName: x.fullName,
                birthDate: x.birthDate,
                customerKey: x.customerKey,
                kycAnswersInitialData: {
                    language: new BehaviorSubject<string>(session.language),
                    currentQuestionsTemplate: session.questionsTemplate,
                    editState: new BehaviorSubject<{ isSavePossible: boolean, answers: KycQuestionAnswerModel[] }>(null),
                    getCountries: () => this.apiService.getIsoCountries(),
                    clientTwoLetterCountryIsoCode: this.config.baseCountry(),
                },
            }));
        } else if(session.isCompleted) {
            if(session.redirectUrl) {
                m.isRedirecting = true;
                setTimeout(() => {
                    document.location.href = session.redirectUrl;
                }, 1500);
            } else {
                m.isCompleted = true;
            }
        }

        this.m = m;
    }

    public isSavePossible() {
        return this.m.customers.every(x => x.kycAnswersInitialData.editState.value?.isSavePossible === true);
    }

    public async submitAnswers(evt ?: Event) {
        evt?.preventDefault();
        
        this.m.isSaving = true;

        let saveRequest : HandleAnswersRequest = {
            sessionId: this.m.sessionId,
            customerAnswers: this.m.customers.map(x => ({
                customerKey: x.customerKey,
                answers: x.kycAnswersInitialData.editState.value.answers
            }))
        }

        await this.handleAnswers(saveRequest);
        this.reload(this.m.sessionId);
    }

    private loadSession(sessionId: string) {
        return this.apiService.post<LoadSessionResponse>('NTechHost', 
            'Api/Customer/KycQuestionSession/LoadCustomerPagesSession', { sessionId }, {
                isAnonymous: true
            });
    }

    private handleAnswers(request: HandleAnswersRequest) {
        return this.apiService.post<{ wasCompleted: boolean }>('NTechHost', 
            'Api/Customer/KycQuestionSession/HandleAnswers', request, { 
                isAnonymous: true 
        });
    }
}

interface HandleAnswersRequest {
    sessionId: string,
    customerAnswers: {
        customerKey: string,
        answers: KycQuestionAnswerModel[]
    }[]
}

interface Model {
    sessionId?: string
    allowBackToRedirectUrl: boolean
    language: string
    isActive: boolean
    isCompleted: boolean
    isSaving?: boolean
    isRedirecting?: boolean
    redirectUrl?: string
    customers?: {
        fullName: string,
        birthDate: string,
        customerKey: string,
        kycAnswersInitialData?: KycAnswersEditorComponentInitialDataModel        
    }[]
}

interface LoadSessionResponse {
    sessionId: string
    language: string
    isExisting: boolean
    isActive: boolean
    isCompleted: boolean
    redirectUrl: string
    allowBackToRedirectUrl: boolean
    questionsTemplate: KycQuestionsSet
    customers: {
        fullName: string,
        birthDate: string,
        customerKey: string        
    }[]    
}
