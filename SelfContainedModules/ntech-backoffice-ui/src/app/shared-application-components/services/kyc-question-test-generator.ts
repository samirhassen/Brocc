import * as moment from 'moment';
import { NTechMath } from 'src/app/common-services/ntech.math';
import { getNumberDictionaryKeys, NumberDictionary, stringJoin } from 'src/app/common.types';
import { CustomerKycQuestionsSet } from './shared-loan-application-api.service';

export function createRandomizedKycQuestionAnswersForApplicants(
    customerIdByApplicantNr: NumberDictionary<number>,
    now: moment.Moment,
    clientCountry: string
): CustomerKycQuestionsSet[] {
    let questionSets: CustomerKycQuestionsSet[] = [];
    let countries = ['SE', 'FI', 'NO', 'US', 'DK', 'DE'].filter((x) => x != clientCountry);
    let applicantNrs = getNumberDictionaryKeys(customerIdByApplicantNr);

    for (let applicantNr of applicantNrs) {
        let customerId = customerIdByApplicantNr[applicantNr];
        let questions = new CustomerKycQuestionsSet(now.toISOString(), customerId);

        //33% chance to have two citizen countries
        let citizenCountries =
            NTechMath.getRandomIntInclusive(1, 3) === 1
                ? [clientCountry, countries[NTechMath.getRandomIntInclusive(0, countries.length - 1)]]
                : [clientCountry];
        questions.addQuestion(
            'citizenCountries',
            stringJoin(',', citizenCountries),
            'Vilka är dina medborgarskap?',
            stringJoin(',', citizenCountries)
        );

        //20% chance to be pep
        let isPep = NTechMath.getRandomIntInclusive(1, 5) === 1;
        questions.addQuestion(
            'isPep',
            isPep ? 'true' : 'false',
            'Har du en hög politisk befattning inom staten, är en nära släkting eller medarbetare till en sådan person?',
            isPep ? 'Ja' : 'Nej'
        );

        //33% chance to have two tax countries
        let taxCountries =
            NTechMath.getRandomIntInclusive(1, 3) === 1
                ? [clientCountry, countries[NTechMath.getRandomIntInclusive(0, countries.length - 1)]]
                : [clientCountry];
        questions.addQuestion(
            'taxCountries',
            stringJoin(',', taxCountries),
            'Vilka är dina skatterättsliga hemvister?',
            stringJoin(',', taxCountries)
        );

        questionSets.push(questions);
    }

    return questionSets;
}
