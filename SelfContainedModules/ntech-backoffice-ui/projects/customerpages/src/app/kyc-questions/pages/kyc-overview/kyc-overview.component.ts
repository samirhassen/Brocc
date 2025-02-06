import { Component, OnInit } from '@angular/core';
import {
    KycAnswersEditorComponentInitialDataModel,
    KycAnswersViewInitialDataModel,
    KycCustomerStatus,
    KycQuestionsOverviewBase,
} from 'projects/ntech-components/src/public-api';
import { BehaviorSubject } from 'rxjs';
import { CustomerPagesApiService } from '../../../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import { KycOverviewService } from './kyc-overview.service';
import { parseQueryStringParameters } from 'src/app/common.types';

@Component({
    selector: 'np-kyc-overview',
    templateUrl: './kyc-overview.component.html',
    styleUrls: ['./kyc-overview.component.css'],
})
export class KycOverviewComponent extends KycQuestionsOverviewBase implements OnInit {
    constructor(
        private config: CustomerPagesConfigService,
        private apiService: KycOverviewService,
        private sharedApiService: CustomerPagesApiService
    ) {
        super();
    }

    public m: Model;

    async ngOnInit() {
        let fromTarget = this.config.getQueryStringParameters()['fromTarget'];

        if (!fromTarget) {
            const queryString = parseQueryStringParameters();
            fromTarget = queryString?.fromTarget;
        }

        let targetUrlPart = fromTarget ? `?targetName=${fromTarget}` : '';
        let language = this.config.getQueryStringParameters()['lang'];
        let hideBackUntilAnswered = this.config.getQueryStringParameters()['hbu'] === '1';

        this.setup(this.config.baseCountry(), language);

        let backUrl = targetUrlPart ? `/portal/navigate${targetUrlPart}` : null;

        this.reload(backUrl, hideBackUntilAnswered);
    }

    async reload(backUrl: string, hideBackUntilAnswered: boolean) {
        let customerStatus = await this.apiService.getCustomerStatus();
        this.reset(customerStatus, backUrl, hideBackUntilAnswered);
    }

    private reset(customerStatus: KycCustomerStatus, backUrl: string, hideBackUntilAnswered: boolean) {
        let m: Model = {
            isUpdateRequired: false,
            backUrl: backUrl,
            productTypes: [],
            hideBackUntilAnswered: hideBackUntilAnswered,
        };

        let isEditDisabled = new BehaviorSubject<boolean>(false);

        let setupProductType = (isSavings: boolean) => {
            let productType: ProductTypeModel = {
                titleTranslationKey: isSavings ? 'savingsTitle' : 'loanTitle',
                relations: customerStatus.activeRelations
                    .filter((x) => x.relationType.startsWith('Savings') === isSavings)
                    .map((relation) => {
                        let latestAnswers = customerStatus.latestAnswers?.find(
                            (x) => x.relationType == relation.relationType && x.relationId === relation.relationId
                        );
                        let relationModel: ProductRelationModel = {
                            relationType: relation.relationType,
                            relationId: relation.relationId,
                            isUpdateRequired: relation.isUpdateRequired,
                            answersViewData: {
                                titleText: relation.relationId,
                                isUpdateRequired: relation.isUpdateRequired,
                                nrOfDaysSinceAnswer: relation.nrOfDaysSinceAnswer,
                                answers: latestAnswers?.answers,
                                isEditDisabled: isEditDisabled,
                            },
                        };
                        let { currentQuestionsTemplate } = customerStatus.questionTemplates.activeProducts.find(
                            (x) => x.relationType === relation.relationType
                        );
                        relationModel.answersViewData.onEdit = async () => {
                            isEditDisabled.next(true);
                            relationModel.edit = {
                                data: {
                                    titleText: relation.relationId,
                                    language: this.language,
                                    currentQuestionsTemplate: currentQuestionsTemplate,
                                    onCancel: async () => {
                                        relationModel.edit = null;
                                        isEditDisabled.next(false);
                                    },
                                    onAnswersSubmitted: async (answers) => {
                                        let newCustomerStatus = await this.apiService.updateAnswers({
                                            relationId: relation.relationId,
                                            relationType: relation.relationType,
                                            answers: answers,
                                        });
                                        isEditDisabled.next(false);
                                        this.reset(newCustomerStatus, this.m.backUrl, this.m.hideBackUntilAnswered);
                                    },
                                    getCountries: () => this.sharedApiService.getIsoCountries(),
                                    clientTwoLetterCountryIsoCode: this.config.baseCountry(),
                                },
                            };
                        };
                        return relationModel;
                    }),
            };
            if (productType.relations.length > 0) {
                m.productTypes.push(productType);
            }
        };
        setupProductType(false);
        setupProductType(true);

        m.isUpdateRequired = m.productTypes.some((x) => x.relations.some((y) => y.isUpdateRequired));

        this.m = m;
    }
}

interface Model {
    backUrl: string;
    hideBackUntilAnswered: boolean;
    isUpdateRequired: boolean;
    productTypes: ProductTypeModel[];
}

interface ProductTypeModel {
    titleTranslationKey: string;
    relations: ProductRelationModel[];
}

interface ProductRelationModel {
    relationType: string;
    relationId: string;
    isUpdateRequired: boolean;
    answersViewData: KycAnswersViewInitialDataModel;
    edit?: {
        data: KycAnswersEditorComponentInitialDataModel;
    };
}
