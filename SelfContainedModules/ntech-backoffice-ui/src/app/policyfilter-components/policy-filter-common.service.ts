import { Injectable } from '@angular/core';
import { Dictionary } from '../common.types';

@Injectable({
    providedIn: 'root',
})
export class PolicyFilterCommonService {
    private rejectionReasons: Dictionary<PolicyFilterRejectionReason>;

    constructor() {
        let add = (code: string, displayName: string) =>
            (this.rejectionReasons[code] = {
                Code: code,
                DisplayName: displayName,
            });

        this.rejectionReasons = {};

        add('paymentRemark', 'Payment remark');
        add('minimumDemands', 'Minimum demands');
        add('priorHistory', 'Prior history');
        add('alreadyApplied', 'Already applied');
        add('requestedOffer', 'Requested offer');
        add('score', 'Score');
        add('address', 'Address');
    }

    //NOTE: These all return promise even though all the data is local since the next part will have these be dynamic.

    public async getRejectionReasonsList(): Promise<PolicyFilterRejectionReason[]> {
        let d = await this.getRejectionReasonsDictionary();
        return new Promise<PolicyFilterRejectionReason[]>((resolve) => {
            resolve(Object.keys(d).map((x) => d[x]));
        });
    }

    public getRejectionReasonsDictionary(): Promise<Dictionary<PolicyFilterRejectionReason>> {
        return new Promise<Dictionary<PolicyFilterRejectionReason>>((resolve) => {
            resolve(this.rejectionReasons);
        });
    }

    public getDefaultRejectionReasonName(): Promise<string> {
        return new Promise<string>((resolve) => resolve('minimumDemands'));
    }
}

export interface PolicyFilterRejectionReason {
    Code: string;
    DisplayName: string;
}
