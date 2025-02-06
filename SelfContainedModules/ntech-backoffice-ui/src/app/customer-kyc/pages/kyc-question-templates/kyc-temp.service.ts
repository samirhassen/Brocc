import { orderBy } from "src/app/common.types";

const isLoanActive = true;
const isSavingsActive = true;

export class TempQuestionService {
    private idCounter = 1;

    constructor() {
        this.storedModels = [];
        this.activeProductNames = [];
        if(isLoanActive) {            
            this.storedModels.push({
                relationType: 'Credit_UnsecuredLoan',
                json: JSON.stringify(exampleLoanQuestions),
                date: '2022-12-10',
                id: this.getId()
            });
            this.activeProductNames.push('Credit_UnsecuredLoan');
        }
        if(isSavingsActive) {
            this.storedModels.push({
                relationType: 'SavingsAccount_StandardAccount',
                json: JSON.stringify(exampleSavingsQuestions),
                date: '2022-12-10',
                id: this.getId()
            });
            this.activeProductNames.push('SavingsAccount_StandardAccount');
        }        
    }

    private getId() {
        return this.idCounter++;
    }

    private activeProductNames: string[];
    private storedModels : StoredModel[];

    public getInitialData(): Promise<KycQuestionsTemplateInitialData> {
        return new Promise<KycQuestionsTemplateInitialData>(resolve => {
            let result : KycQuestionsTemplateInitialData = {
                activeProducts: []
            }
            for(let activeProductName of this.activeProductNames) {
                let productModels = orderBy(this.storedModels.filter(m => m.relationType == activeProductName), x => -x.id);
                result.activeProducts.push({
                    relationType: activeProductName,
                    currentQuestionsModelJson: productModels && productModels.length > 0 ? productModels[0]?.json : null,
                    historicalModels: productModels.map(x => ({
                            id: x.id,
                            date: x.date,
                    }))
                });
            }
            resolve(result);
        });
    }

    public saveQuestions(relationType: string, questionsModelJson: string): Promise<{ id: number }> {
        return new Promise<{ id: number }>(resolve => {
            let newId = this.getId();
            this.storedModels.push({
                relationType: relationType,
                json: questionsModelJson,
                date: new Date().toISOString().split('T')[0],
                id: newId
            });
            resolve({ id: newId});
        });
    }

    public validateQuestionsModel(questionsModelJson: string): Promise<{ isValid: boolean, validationFailedMessage: string }> {
        return new Promise<{ isValid: boolean, validationFailedMessage: string }>(resolve => {
            resolve({ isValid: false, validationFailedMessage: 'Invalid model' });
        });
    }

    public getQuestionsJson(id: number): Promise<string> {
        return new Promise<string>(resolve => {
            let model = this.storedModels.find(x => x.id == id);
            resolve(model?.json);
        });
    }
}

export interface KycQuestionsTemplateInitialData {
    activeProducts: {
        relationType: string
        currentQuestionsModelJson: string
        historicalModels: {
            id: number,
            date: string
        }[]
    }[]
}

interface StoredModel {
    id: number
    relationType: string
    date: string
    json: string
}


const exampleLoanQuestions = {
    "questions" : [{
            "type" : "dropdown",
            "key": "loan_purpose",
            "headerTranslations":{
                "sv": "Vad ska pengarna användas till?",
                "fi": "Rahojen käyttötarkoitus?" 
            },
            "options": [{
                    "value": "consumption",
                    "translations": {
                        "sv": "Konsumtion",
                        "fi": "Kulutus" 
                    }
                }, {
                    "value": "investment",
                    "translations": {
                        "sv": "Investering",
                        "fi": "Sijoitus" 
                    }
                }, {
                    "value": "relative",
                    "translations": {
                        "sv": "Till närstående",
                        "fi": "Lähimmäisille" 
                    }
                },{
                    "value": "other",
                    "translations": {
                        "sv": "Annat",
                        "fi": "Muu" 
                    }
                }
            ]
        }, {
            "type" : "dropdown",
            "key": "loan_whosmoney",
            "headerTranslations": {
                "sv": "Vems pengar kommer användas för återbetalning?",
                "fi": "Kenen varoja tullaan käyttämään takaisinmaksussa?"
            },
            "options": [{
                    "value": "own",
                    "translations": {
                        "sv": "Egna pengar",
                        "fi": "Omia rahoja" 
                    }
                }, {
                    "value": "others",
                    "translations": {
                        "sv": "Annans pengar",
                        "fi": "Jonkun muun rahoja" 
                    }
                }
            ]
        }, {
            "type" : "dropdown",
            "key": "loan_paymentfrequency",
            "headerTranslations": {
                "sv": "Hur ofta kommer avbetalningarna göras?",
                "fi": "Kuinka usein lyhennyksiä tehdään?"
            },
            "options": [{
                    "value": "onschedule",
                    "translations": {
                        "sv": "Enligt amorteringsplan",
                        "fi": "Lyhennyssuunnitelman mukaisesti" 
                    }
                }, {
                    "value": "withextrapayments",
                    "translations": {
                        "sv": "Med extraamorteringar",
                        "fi": "Ylimääräisiä lyhennyksiä" 
                    }
                }
            ]
        }, {
            "type": "yesNoWithOptions",
            "key": "ispep",
            "optionsKey": "pepRoles",
            "headerTranslations": {
                "sv": "Har du en hög politisk befattning inom staten, är en nära släktning eller medarbetare till en sådan person?",
                "fi": "Oletko poliittisesti vaikutusvaltainen henkilö tai tällaisen perheenjäsen tai läheinen yhtiökumppani?"
            },
            "optionsHeaderTranslations": {
                "sv": "Är du, någon i din familj, eller närstående, en person som har eller har haft någon av följande roller?",
                "fi": "Oletko sinä, tai läheinen perheenjäsenesi, henkilö joka on ollut seuraavassa asemassa?"
            },
            "options": [
                {
                    "value": "governmentofficial",
                    "translations": {
                        "sv": "Stats- eller regeringschef, minister eller vice/biträdande minister.",
                        "fi": "Valtionpäämies, hallitusten päämies, ministeri tai vara- ja apulaisministeri."
                    }
                }, {
                    "value": "memberofparliament",
                    "translations": {
                        "sv": "Parlamentsledamot",
                        "fi": "Kansanedustaja."
                    }
                }, {
                    "value": "supremecourtjudge",
                    "translations": {
                        "sv": "Domare i högsta domstolen, konstitutionell domstol eller liknande befattning.",
                        "fi": "Korkeimman oikeuden, perustuslakituomioistuimien tai vastaavan jäsen."
                    }
                }, {
                    "value": "economicofficial",
                    "translations": {
                        "sv": "Högre tjänsteman vid revisionsmyndighet eller ledamot i centralbanks styrande organ.",
                        "fi": "Tilintarkastustuomioistuinten tai keskuspankkien johtokunnan jäsen."
                    }
                }, {
                    "value": "diplomantordefense",
                    "translations": {
                        "sv": "Ambassadör, beskickningschef eller hög officer i försvarsmakten.",
                        "fi": "Suurlähettiläs, asiainhoitaja tai puolustusvoimien korkea-arvoinen upseeri."
                    }
                }, {
                    "value": "stateownedcompany",
                    "translations": {
                        "sv": "VD eller styrelseledamot i statsägt företag.",
                        "fi": "Valtion omistamien yritysten hallinto-, johto tai valvontaelimen jäsen."
                    }
                }, {
                    "value": "internationalagency",
                    "translations": {
                        "sv": "Ledande befattning i mellanstatlig organisation eller medlem i dess högsta ledning.",
                        "fi": "Johtava asema kansainvälisessä järjestössä (esim. YK, NATO) tai se sen johtokunnan jäsen."
                    }
                }
            ]
        }, {
            "type": "yesNo",
            "key": "hasOtherTaxOrCitizenCountry",
            "headerTranslations": {
                "sv": "Är du skattepliktig eller medborgare i något annat land än Finland?",
                "fi": "Onko sinulla verovelvollisuutta tai kansalaisuutta muussa maassa kuin Suomessa?"
            }
        }
    ]
};

const exampleSavingsQuestions ={
    "questions" : [{
        "type" : "dropdown",
        "key": "nrdepositsperyearrangeestimate",
        "headerTranslations": {
            "sv": "Hur ofta kommer insättningar att göras per år?",
            "fi": "Kuinka usein talletuksia tehdään vuosittain?" 
        },
        "options": [
            {
                "value": "0_10",
                "translations": {
                    "sv": "Färre än 10 gånger",
                    "fi": "Vähemmän kuin 10 kertaa" 
                }
            }, {
                "value": "10_50",
                "translations": {
                    "sv": "10 - 50 gånger",
                    "fi": "10 - 50 kertaa" 
                }
            }, {
                "value": "50_100",
                "translations": {
                    "sv": "50 - 100 gånger",
                    "fi": "50 - 100 kertaa" 
                }
            }, {
                "value": "100_inf",
                "translations": {
                    "sv": "Fler än 100 gånger",
                    "fi": "Enemmän kuin 100 kertaa" 
                }
            }
        ]
    }, {
        "type" : "dropdown",
        "key": "savingshorizonestimate",
        "headerTranslations": {
            "sv": "Hur länge har du tänkt spara?",
            "fi": "Kuinka pitkä on suunniteltu säästämisaikasi?" 
        },
        "options": [
            {
                "value": "morethanfiveyears",
                "translations": {
                    "sv": "Långsiktigt (mer än 5 år)",
                    "fi": "Pitkä (yli 5 vuotta)" 
                }
            }, {
                "value": "onetofiveyears",
                "translations": {
                    "sv": "Medellångt (1 - 5 år)",
                    "fi": "Keskipitkä (1-5 vuotta)" 
                }
            }, {
                "value": "lessthanoneyear",
                "translations": {
                    "sv": "Kort sikt (kortare än 1 år)",
                    "fi": "Lyhyt (vähemmän kuin 1 vuosi)" 
                }
            }
        ]
    }, {
        "type" : "dropdown",
        "key": "initialdepositrangeestimate",
        "headerTranslations": {
            "sv": "Vilket ungefärligt värde kommer du att överföra i samband med öppnandet?",
            "fi": "Kuinka paljon arviolta talletat tilin avaamisen yhteydessä?" 
        },
        "options": [
            {
                "value": "0_100",
                "translations": {
                    "sv": "Mindre än 100 €",
                    "fi": "Vähemmän kuin 100 €" 
                }
            }, {
                "value": "100_1000",
                "translations": {
                    "sv": "100 € - 1 000 €",
                    "fi": "100 € - 1 000 €" 
                }
            }, {
                "value": "1000_10000",
                "translations": {
                    "sv": "1 000 € - 10 000 €",
                    "fi": "1 000 € - 10 000 €" 
                }
            }, {
                "value": "10000_50000",
                "translations": {
                    "sv": "10 000 € - 50 000 €",
                    "fi": "10 000 € - 50 000 €" 
                }
            }, {
                "value": "50000_80000",
                "translations": {
                    "sv": "50 000 € - 80 000 €",
                    "fi": "50 000 € - 80 000 €" 
                }
            }, {
                "value": "80000_max",
                "translations": {
                    "sv": "Mer än 80 000 €",
                    "fi": "Enemmän kuin 80 000 €" 
                }
            }
        ]
    }, {
        "type" : "dropdown",
        "key": "mainoccupation",
        "headerTranslations": {
            "sv": "Vilken är din huvudsakliga sysselsättning?",
            "fi": "Mikä on pääasiallinen toimenkuvasi?" 
        },
        "options": [
            {
                "value": "manager",
                "translations": {
                    "sv": "Chefsjobb",
                    "fi": "Johtaja" 
                }
            }, {
                "value": "specialist",
                "translations": {
                    "sv": "Specialistjobb",
                    "fi": "Erityisasiantuntija" 
                }
            }, {
                "value": "expert",
                "translations": {
                    "sv": "Expertjobb",
                    "fi": "Asiantuntija" 
                }
            }, {
                "value": "administration",
                "translations": {
                    "sv": "Administrativt arbete och kundtjänst",
                    "fi": "Toimisto- ja asiakaspalvelu" 
                }
            }, {
                "value": "sales",
                "translations": {
                    "sv": "Service- och säljarbete",
                    "fi": "Palvelu- ja myyntityöntekijät" 
                }
            }, {
                "value": "agriculture",
                "translations": {
                    "sv": "Jord- och skogsbruk etc.",
                    "fi": "Maanviljelijät, metsätyöntekijät ym." 
                }
            }, {
                "value": "construction",
                "translations": {
                    "sv": "Bygg- och produktionsarbete",
                    "fi": "Rakennus-, korjaus- ja valmistustyöntekijät" 
                }
            }, {
                "value": "transport",
                "translations": {
                    "sv": "Process- och transport",
                    "fi": "Prosessi- ja kuljetustyöntekijät" 
                }
            }, {
                "value": "military",
                "translations": {
                    "sv": "Militär",
                    "fi": "Sotilaat" 
                }
            }, {
                "value": "other",
                "translations": {
                    "sv": "Andra",
                    "fi": "Muu" 
                }
            }
        ]
    }, {
        "type" : "dropdown",
        "key": "purpose",
        "headerTranslations": {
            "sv": "Vad är det huvudsakliga syftet med ditt sparande?",
            "fi": "Mikä on säästämisen pääasiallinen tarkoitus?" 
        },
        "options": [
            {
                "value": "pension",
                "translations": {
                    "sv": "Pension",
                    "fi": "Eläke" 
                }
            }, {
                "value": "relative",
                "translations": {
                    "sv": "Till närstående, till exempel barn",
                    "fi": "Lähiomaiselle, esimerkiksi lapsille" 
                }
            }, {
                "value": "currencyexchange",
                "translations": {
                    "sv": "Valutaväxling",
                    "fi": "Valuutanvaihto" 
                }
            }, {
                "value": "longtermsavings",
                "translations": {
                    "sv": "ValLångsiktigt sparandeutaväxling",
                    "fi": "Pitkäaikainen säästäminen" 
                }
            }, {
                "value": "shorttermsavings",
                "translations": {
                    "sv": "Kortsiktigt sparande",
                    "fi": "Lyhytaikainen säästäminen" 
                }
            }, {
                "value": "trading",
                "translations": {
                    "sv": "Trading",
                    "fi": "Kaupankäynti" 
                }
            }, {
                "value": "transactionaccount",
                "translations": {
                    "sv": "Transaktionskonto",
                    "fi": "Käyttelytili" 
                }
            }
        ]
    }, {
        "type" : "dropdown",
        "key": "sourceoffunds",
        "headerTranslations": {
            "sv": "Varifrån kommer de pengar som du sätter in på kontot huvudsakligen ifrån?",
            "fi": "Mistä tallettamasi rahat pääsääntöisesti tulevat?"
        },
        "options": [{
                "value": "salaryorpension",
                "translations": {
                    "sv": "Lön eller pension",
                    "fi": "Palkka tai eläke" 
                }
            }, {
                "value": "inheritanceorgift",
                "translations": {
                    "sv": "Arv eller gåva",
                    "fi": "Perintö tai lahja" 
                }
            }, {
                "value": "othersale",
                "translations": {
                    "sv": "Annan försäljning, exempelvis bil- eller båtförsäljning",
                    "fi": "Muu myynti, esimerkiksi auto- tai venemyynti" 
                }
            }, {
                "value": "insurancepayout",
                "translations": {
                    "sv": "Försäkringsutbetalning",
                    "fi": "Vakuutuskorvaus" 
                }
            }, {
                "value": "owncompany",
                "translations": {
                    "sv": "Egen rörelse, lön eller utdelning",
                    "fi": "Oma yritys, palkka tai osinko" 
                }
            }, {
                "value": "realestatesale",
                "translations": {
                    "sv": "Bostads- eller fastighetsförsäljning",
                    "fi": "Asunnon tai kiinteistön myynti" 
                }
            }, {
                "value": "financialinstrumentsale",
                "translations": {
                    "sv": "Vinst från värdepappershandel",
                    "fi": "Voitto arvopaperikaupasta" 
                }
            }, {
                "value": "welfarepayout",
                "translations": {
                    "sv": "Bidrag eller arbetslöshetsersättning",
                    "fi": "Sosiaalietuus, esim. työttömyysetuus" 
                }
            }, {
                "value": "savingsaccount",
                "translations": {
                    "sv": "Sparande",
                    "fi": "Säästäminen" 
                }
            }, {
                "value": "owncompanysale",
                "translations": {
                    "sv": "Företagsförsäljning",
                    "fi": "Yritysmyynti" 
                }
            }, {
                "value": "gambling",
                "translations": {
                    "sv": "Lotteri eller spel",
                    "fi": "Lotto tai muut pelit" 
                }
            }
        ]
    }, {
            "type": "yesNoWithOptions",
            "key": "ispep",
            "optionsKey": "pep_roles",
            "headerTranslations": {
                "sv": "Har du en hög politisk befattning inom staten, är en nära släktning eller medarbetare till en sådan person?",
                "fi": "Oletko poliittisesti vaikutusvaltainen henkilö tai tällaisen perheenjäsen tai läheinen yhtiökumppani?"
            },
            "optionsHeaderTranslations": {
                "sv": "Är du, någon i din familj, eller närstående, en person som har eller har haft någon av följande roller?",
                "fi": "Oletko sinä, tai läheinen perheenjäsenesi, henkilö joka on ollut seuraavassa asemassa?"
            },
            "options": [
                {
                    "value": "governmentofficial",
                    "translations": {
                        "sv": "Stats- eller regeringschef, minister eller vice/biträdande minister.",
                        "fi": "Valtionpäämies, hallitusten päämies, ministeri tai vara- ja apulaisministeri."
                    }
                }, {
                    "value": "memberofparliament",
                    "translations": {
                        "sv": "Parlamentsledamot",
                        "fi": "Kansanedustaja."
                    }
                }, {
                    "value": "supremecourtjudge",
                    "translations": {
                        "sv": "Domare i högsta domstolen, konstitutionell domstol eller liknande befattning.",
                        "fi": "Korkeimman oikeuden, perustuslakituomioistuimien tai vastaavan jäsen."
                    }
                }, {
                    "value": "economicofficial",
                    "translations": {
                        "sv": "Högre tjänsteman vid revisionsmyndighet eller ledamot i centralbanks styrande organ.",
                        "fi": "Tilintarkastustuomioistuinten tai keskuspankkien johtokunnan jäsen."
                    }
                }, {
                    "value": "diplomantordefense",
                    "translations": {
                        "sv": "Ambassadör, beskickningschef eller hög officer i försvarsmakten.",
                        "fi": "Suurlähettiläs, asiainhoitaja tai puolustusvoimien korkea-arvoinen upseeri."
                    }
                }, {
                    "value": "stateownedcompany",
                    "translations": {
                        "sv": "VD eller styrelseledamot i statsägt företag.",
                        "fi": "Valtion omistamien yritysten hallinto-, johto tai valvontaelimen jäsen."
                    }
                }, {
                    "value": "internationalagency",
                    "translations": {
                        "sv": "Ledande befattning i mellanstatlig organisation eller medlem i dess högsta ledning.",
                        "fi": "Johtava asema kansainvälisessä järjestössä (esim. YK, NATO) tai se sen johtokunnan jäsen."
                    }
                }
            ]
        }, {
            "type": "yesNo",
            "key": "hasOtherTaxOrCitizenCountry",
            "headerTranslations": {
                "sv": "Är du skattepliktig eller medborgare i något annat land än Finland?",
                "fi": "Onko sinulla verovelvollisuutta tai kansalaisuutta muussa maassa kuin Suomessa?"
            }
        }
    ]
};