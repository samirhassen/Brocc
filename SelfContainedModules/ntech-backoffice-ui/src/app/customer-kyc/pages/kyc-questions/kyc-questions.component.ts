import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import {
    KycAnswersEditorComponentInitialDataModel,
    KycAnswersModel,
    KycAnswersViewInitialDataModel,
    KycCustomerStatus,
    KycQuestionsOverviewBase,
} from 'projects/ntech-components/src/public-api';
import { BehaviorSubject } from 'rxjs';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { IsoCountriesService } from 'src/app/common-services/iso-countries.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { KycQuestionsService } from './kyc-questions.service';

@Component({
    selector: 'app-kyc-questions',
    templateUrl: './kyc-questions.component.html',
})
export class KycQuestionsComponent extends KycQuestionsOverviewBase implements OnInit {
    constructor(
        private apiService: KycQuestionsService,
        private route: ActivatedRoute,
        private config: ConfigService,
        private baseApiService: NtechApiService,
        private toastr: ToastrService,
        private countryService: IsoCountriesService
    ) {
        super();
    }

    public m: Model;

    async ngOnInit() {
        this.setup(this.config.baseCountry(), null);
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(parseInt(params.get('customerId')));
        });
    }

    async reload(customerId: number) {
        let customerStatus = await this.apiService.getCustomerStatus(customerId);
        this.reset(customerStatus, customerId);
    }

    private reset(customerStatus: KycCustomerStatus, customerId: number) {
        this.language.next('sv');
        let m: Model = {
            customerId: customerId,
            isUpdateRequired: false,
            productTypes: [],
            testFunctions: null,
        };

        let isEditDisabled = new BehaviorSubject<boolean>(false);

        let setupProductType = (isSavings: boolean) => {
            let productType: ProductTypeModel = {
                title: isSavings ? 'Savings accounts' : 'Loans',
                relations: customerStatus.activeRelations
                    .filter((x) => x.relationType.startsWith('Savings') === isSavings)
                    .map((relation) => {
                        let latestAnswers = customerStatus.latestAnswers?.find(
                            (x) => x.relationType == relation.relationType && x.relationId === relation.relationId
                        );
                        let relationModel: ProductRelationModel = {
                            historicalAnswers: customerStatus.historicalAnswers?.filter(
                                (x) => x.relationType === relation.relationType && x.relationId === relation.relationId
                            ),
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
                        relationModel.answersViewData.onEdit = async () => {
                            isEditDisabled.next(true);

                            let { currentQuestionsTemplate } = customerStatus.questionTemplates.activeProducts.find(
                                (x) => x.relationType === relation.relationType
                            );

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
                                        await this.apiService.updateAnswers({
                                            relationId: relation.relationId,
                                            relationType: relation.relationType,
                                            answers: answers,
                                            customerId: customerId,
                                        });
                                        isEditDisabled.next(false);
                                        this.reload(customerId);
                                    },
                                    getCountries: () => this.countryService.getIsoCountries(),
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

        this.setupTestFunctions(m);

        this.m = m;
    }

    private setupTestFunctions(m: Model) {
        if (!this.config.isNTechTest()) {
            return;
        }
        m.testFunctions = new TestFunctionsModel();
        if (this.config.isFeatureEnabled('feature.customerpages.kyc')) {
            m.testFunctions.addFunctionCall('Send Kyc reminders ', () => {
                this.baseApiService
                    .post<any>('nCustomer', 'Api/Kyc-Reminders/Send', { onlyConsiderCustomerIds: [this.m.customerId] })
                    .then((_) => {
                        this.toastr.success('Ok');
                    });
            });
        }
    }
}

interface Model {
    customerId: number;
    isUpdateRequired: boolean;
    productTypes: ProductTypeModel[];
    testFunctions: TestFunctionsModel;
}

interface ProductTypeModel {
    title: string;
    relations: ProductRelationModel[];
}

interface ProductRelationModel {
    relationType: string;
    relationId: string;
    isUpdateRequired: boolean;
    answersViewData: KycAnswersViewInitialDataModel;
    historicalAnswers: KycAnswersModel[];
    edit?: {
        data: KycAnswersEditorComponentInitialDataModel;
    };
}
