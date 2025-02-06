import { Inject, Injectable } from '@angular/core';
import { ApiServiceBase, LoanPreviewRequestModel, LoanPreviewResponseModel } from './api-service';
import { SESSION_STORAGE, StorageService } from 'ngx-webstorage-service';
import { of, Observable } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Router } from '@angular/router';
import { CreateApplicationResponseModel } from './real-api.service';
import * as moment from 'moment'
import { CreateApplicationRequestModel } from './application-model.server';
import { NTechPaymentPlanService } from './ntech-paymentplan.service';
import { ConfigService } from './config.service';

@Injectable()
export class StandaloneTestApiService extends ApiServiceBase {

    constructor(@Inject(SESSION_STORAGE) storage: StorageService, router: Router, paymentPlanService: NTechPaymentPlanService, configService: ConfigService) {
        super(storage, router, paymentPlanService, configService);
    }

    createApplication(request: CreateApplicationRequestModel): Observable<CreateApplicationResponseModel> {
        let now = moment().format('YYYYMMDD-HHmmSS')
        return of({
            isFailed: false,
            redirectToUrl: null,
            failedUrl: null,
            applicationNr: `TEST-${now}`
        }).pipe(delay(2000))
    }
}
