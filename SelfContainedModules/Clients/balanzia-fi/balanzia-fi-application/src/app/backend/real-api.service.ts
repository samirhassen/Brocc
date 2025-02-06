import { Injectable, Inject } from '@angular/core';
import { ApiService, ApiServiceBase, LoanPreviewResponseModel, LoanPreviewRequestModel } from './api-service';
import { SESSION_STORAGE, StorageService } from 'ngx-webstorage-service';
import { Observable, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { Router } from '@angular/router';
import { CreateApplicationRequestModel } from './application-model.server';
import { NTechPaymentPlanService } from './ntech-paymentplan.service';
import { ConfigService } from './config.service';

@Injectable()
export class RealApiService extends ApiServiceBase {
    constructor(@Inject(SESSION_STORAGE) storage: StorageService, router: Router, private httpClient: HttpClient, paymentPlanService: NTechPaymentPlanService, configService: ConfigService) {
        super(storage, router, paymentPlanService, configService);
    }

    createApplication(request: CreateApplicationRequestModel): Observable<CreateApplicationResponseModel> {
        return this.httpClient.post<CreateApplicationResponseModel>('/api/application/create', request)
    }
}

export class CreateApplicationResponseModel {
    isFailed: boolean
    redirectToUrl: string
    failedUrl: string
    applicationNr: string
}