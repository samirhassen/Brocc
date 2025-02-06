import { BehaviorSubject } from 'rxjs';
import { KycQuestion } from '../public-api';

type Translations = { [index: string]: { [index: string]: string } };
export abstract class KycQuestionsOverviewBase {
    constructor() {
        this.language = new BehaviorSubject<string>('sv');
    }

    public language: BehaviorSubject<string>;

    public languages = [
        {
            code: 'sv',
            name: 'Svenska',
        },
        {
            code: 'fi',
            name: 'Suomi',
        },
    ];

    protected setup(clientCountry: string, language?: string) {
        if (clientCountry === 'FI') {
            this.languages = [
                {
                    code: 'fi',
                    name: 'Suomi',
                },
                {
                    code: 'sv',
                    name: 'Svenska',
                },
            ];
        } else {
            this.languages = [
                {
                    code: 'sv',
                    name: 'Svenska',
                },
            ];
        }
        if (language && this.languages.find((x) => x.code === language)) {
            this.language.next(language);
        } else {
            this.language.next(this.languages[0].code);
        }
    }

    public staticTranslations: Translations = {
        explanationText: {
            sv: 'För att få behålla dig som kund måste vi ha uppdaterad information om dig och ditt engagemang. Därför är det viktigt att du besvarar och uppdaterar alla frågor med aktuell information.',
            fi: 'Jotta voimme säilyttää sinut asiakkaanamme, meidän täytyy olla päivitettyä tietoa sinusta ja sitoutumisestasi. Siksi on tärkeää, että vastaat ja päivität kaikki kysymykset ajantasaisilla tiedoilla.',
        },
        explanationTextUpdate: {
            sv: 'Du behöver uppdatera din KYC-information (Know your customer). Det är viktigt för oss att ha aktuell information om dig och hur du använder våra tjänster, så att vi kan ge dig bästa möjliga kundupplevelse och uppfylla våra lagstadgade skyldigheter.',
            fi: 'Asiakkaan tuntemisen tietosi (KYC - Know your customer) tulisi päivittää. Meille on tärkeää tuntea sinut ja kuinka käytät palveluitamme, jotta voimme palvella sinua parhaalla mahdollisella tavalla ja voidaksemme täyttää lain meille asettamat velvollisuudet.',
        },
        language: {
            sv: 'Språk',
            fi: 'Kieli',
        },
        back: {
            sv: 'Tillbaka',
            fi: 'Takaisin',
        },
        loanTitle: {
            sv: 'Lån',
            fi: 'Luotto',
        },
        savingsTitle: {
            sv: 'Sparkonton',
            fi: 'Säästötili',
        },
        kyc: {
            sv: 'Kundkännedom',
            fi: 'Asiakkaan tuntemus',
        },
    };

    public getRelationTypeTranslationKey(relationType: string) {
        if (relationType === 'Credit_UnsecuredLoan') {
            return 'unsecuredLoan';
        } else if (relationType === 'SavingsAccount_StandardAccount') {
            return 'savingsAccount';
        } else {
            return relationType;
        }
    }

    public getQuestionOptionFormKey(
        question: KycQuestion,
        option: { value: string; translations: { [index: string]: string } }
    ) {
        return `${question.key}_${option.value}`;
    }

    public getTranslation(translations: { [index: string]: string }) {
        if (!translations) {
            return '';
        }
        let preferredTranslation = translations[this.language.value];
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

    getStaticTranslation(key: string) {
        return this.getTranslation(this.staticTranslations[key]) || key;
    }

    onLanguageChanged(evt: Event) {
        this.language.next((evt as any).target.value);
    }
}
