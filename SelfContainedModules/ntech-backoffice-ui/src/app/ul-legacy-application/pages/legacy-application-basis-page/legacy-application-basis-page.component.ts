import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary, StringDictionary } from 'src/app/common.types';

@Component({
    selector: 'app-legacy-application-basis-page',
    templateUrl: './legacy-application-basis-page.component.html',
    styles: [
    ]
})
export class LegacyApplicationBasisPageComponent implements OnInit {
    constructor(private route: ActivatedRoute, private apiService: NtechApiService) { }

    public m: Model;

    async ngOnInit() {       
        this.reload(this.route.snapshot.params['applicationNr']);
    }

    private async reload(applicationNr: string) {
        this.m = null;

        let result = await this.apiService.post<any>('NTechHost', 'Api/PreCredit/ApplicationBasisData/Fetch', { 
            applicationNr
        });
        this.m = {
            hasCoApplicant: result.application.nrOfApplicants > 1,
            data: result
        }        
    }

    appliedForRepaymentTimeInMonths() {
        if(!this.m) {
            return null;
        }
        let a = this.applicationData();
        if (a.repaymentTimeInMonths) {
            return a.repaymentTimeInMonths;
        } else if (a.repaymentTimeInYears) {
            return (12 * parseInt(a.repaymentTimeInYears)).toString();
        } else {
            return '<None>';
        }        
    }

    applicationData() {
        return this.m.data.application.groupedItems.application;
    }
}

interface Model {
    hasCoApplicant: boolean
    data: LegacyApplicationResponse
}

export interface LegacyApplicationResponse {
    applicationNr: string
    isEditAllowed: boolean
    changedCreditApplicationItems: { groupName: string, itemName: string }[]
    application: {
        nrOfApplicants: number
        groupedItems: {
            application: {
                scoringVersion: string
                amount: string
                repaymentTimeInMonths: string
                repaymentTimeInYears: string
                campaignCode: string
                loansToSettleAmount: string
            }
            applicant1 : StringDictionary
            applicant2 ?: StringDictionary
        }
    },
    translations : Dictionary<StringDictionary>
}