import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { Dictionary } from 'src/app/common.types';
import {
    PolicyFilterDetailsDisplayItem,
    PolicyFilterEngineResult,
} from 'src/app/loan-policyfilters/services/policy-filters-apiservice';
import { MlCreditRecommendationModel } from '../../services/mortgage-loan-application-api.service';

@Component({
    selector: 'ml-application-policy-info',
    templateUrl: './ml-application-policy-info.component.html',
    styles: [],
})
export class MlApplicationPolicyInfoComponent implements OnInit {
    constructor() {}

    @Input()
    public initialData: MlApplicationPolicyInfoInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let r = this.initialData.recommendation;

        let detailPhases: {
            phaseDisplayName: string;
            isManualControlPhase: boolean;
            items: PolicyFilterDetailsDisplayItem[];
        }[] = null;
        if (r.PolicyFilterDetailsDisplayItems) {
            let detailPhasesByKey: Dictionary<{
                phaseDisplayName: string;
                isManualControlPhase: boolean;
                items: PolicyFilterDetailsDisplayItem[];
            }> = {};
            let phaseOrder: string[] = [];

            for (let item of r.PolicyFilterDetailsDisplayItems) {
                if (!detailPhasesByKey[item.PhaseName]) {
                    detailPhasesByKey[item.PhaseName] = {
                        phaseDisplayName: item.PhaseDisplayName,
                        isManualControlPhase: item.IsManualControlPhase,
                        items: [item],
                    };
                    phaseOrder.push(item.PhaseName);
                } else {
                    detailPhasesByKey[item.PhaseName].items.push(item);
                }
            }

            detailPhases = phaseOrder.map((x) => detailPhasesByKey[x]);
        }

        let m: Model = {
            policy: {
                statusCode: this.getPolicyFilterEngineResultStatusCode(r?.PolicyFilterResult),
                missingMessage: r?.PolicyFilterResult ? null : 'Not calculated',
                detailPhases: detailPhases,
            },
            lti: {
                missingMessage: r?.LoanToIncomeMissingReason,
                value: r?.LoanToIncome,
            },
            ltv: {
                missingMessage: r?.LoanToValueMissingReason,
                value: r?.LoanToValue * 100,
            },
            ltl: {
                value: r.LeftToLiveOn?.LtlAmount,
                missingMessage: r.LeftToLiveOn?.UndefinedReasonMessage,
                details: r.LeftToLiveOn?.UndefinedReasonMessage ? null : r.LeftToLiveOn,
            },
        };

        this.m = m;
    }

    getPolicyFilterEngineResultStatusCode(result: PolicyFilterEngineResult) {
        if (!result) {
            return null;
        }

        if (result.IsAcceptRecommended === true) {
            return result.IsManualControlRecommended === true ? 'manualControl' : 'accept';
        } else if (result.IsAcceptRecommended === false) {
            return 'reject';
        } else {
            return 'noDecision';
        }
    }
}

class Model {
    policy: {
        statusCode: string;
        missingMessage?: string;
        detailPhases?: {
            phaseDisplayName: string;
            isManualControlPhase: boolean;
            items: PolicyFilterDetailsDisplayItem[];
        }[];
    };
    lti: {
        value?: number;
        missingMessage?: string;
    };
    ltv: {
        value?: number;
        missingMessage?: string;
    };
    ltl: {
        value?: number;
        missingMessage?: string;
        details?: any;
    };
}

export class MlApplicationPolicyInfoInitialData {
    recommendation: MlCreditRecommendationModel;
}
