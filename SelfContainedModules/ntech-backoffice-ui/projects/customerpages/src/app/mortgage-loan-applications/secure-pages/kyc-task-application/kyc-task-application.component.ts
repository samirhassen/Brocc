import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerPagesApiService } from '../../../common-services/customer-pages-api.service';
import {
    CustomerPagesKycQuestionAnswerModel,
    TaskKycQuestionsComponentInitialData,
} from '../../../shared-components/task-kyc-questions/task-kyc-questions.component';

@Component({
    selector: 'np-kyc-task-application',
    templateUrl: './kyc-task-application.component.html',
    styles: [],
})
export class KycTaskApplicationComponent implements OnInit {
    constructor(private route: ActivatedRoute, private router: Router, private apiService: CustomerPagesApiService) {}

    public m: Model;

    ngOnInit(): void {
        this.reload(this.route.snapshot.params['applicationNr']);
    }

    closeTask() {
        this.router.navigate(['mortgage-loan-applications/secure/application', this.m.applicationNr]);
    }

    private async reload(applicationNr: string) {
        let result = await this.apiService.post<KeyStatusServerResponse>(
            'nPreCredit',
            'api/MortgageLoanStandard/CustomerPages/Fetch-Application-KycStatus',
            {
                applicationNr: applicationNr,
            }
        );

        if (!result?.IsActive) {
            this.m = {
                applicationNr: applicationNr,
            };
            return;
        }

        let m: Model = {
            applicationNr: applicationNr,
            kycInitialData: {
                applicationNr: applicationNr,
                isPossibleToAnswer: result.IsPossibleToAnswer,
                isAnswersApproved: result.IsAnswersApproved,
                customers: [],
                onSave: (x) => {
                    let request = {
                        ApplicationNr: applicationNr,
                        Customers: x.customersWithAnswers.map((y) => ({
                            CustomerId: y.customerId,
                            Answers: y.answers,
                        })),
                    };
                    this.apiService
                        .post('nPreCredit', 'api/MortgageLoanStandard/CustomerPages/Answer-Kyc-Questions', request)
                        .then((_) => {
                            this.closeTask();
                        });
                },
            },
        };

        for (let customer of result.Customers) {
            m.kycInitialData.customers.push({
                ApplicantNr: customer.ApplicantNr,
                CustomerId: customer.CustomerId,
                CustomerBirthDate: customer.CustomerBirthDate,
                CustomerShortName: customer.CustomerShortName,
                LatestKycQuestionsAnswerDate: customer.LatestKycQuestionsAnswerDate,
                LatestQuestions: customer.LatestQuestions,
            });
        }

        this.m = m;
    }
}

interface Model {
    applicationNr: string;
    kycInitialData?: TaskKycQuestionsComponentInitialData;
}

interface KeyStatusServerResponse {
    ApplicationNr: string;
    IsActive: boolean;
    IsAccepted: boolean;
    IsPossibleToAnswer: boolean;
    IsAnswersApproved: boolean;
    Customers: {
        CustomerId: number;
        ApplicantNr: number;
        CustomerBirthDate: string;
        CustomerShortName: string;
        LatestKycQuestionsAnswerDate: string;
        LatestQuestions: CustomerPagesKycQuestionAnswerModel[];
    }[];
}
