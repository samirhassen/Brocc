import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { Dictionary } from 'src/app/common.types';
import {
    CreditRecommendationLtlResult,
    PolicyFilterDetailsDisplayItem,
    PolicyFilterEngineResult,
} from 'src/app/loan-policyfilters/services/policy-filters-apiservice';
import { CreditRecommendationModel, PreScoreRecommendationModel } from '../../services/unsecured-loan-application-api.service';

@Component({
    selector: 'application-policy-info',
    templateUrl: './application-policy-info.component.html',
    styles: [],
})
export class ApplicationPolicyInfoComponent implements OnInit {
    constructor() {}

    @Input()
    public initialData: ApplicationPolicyInfoInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let r = this.initialData.recommendation;
        let preScoreRecommendation = this.initialData.preScoreRecommendation;

        let m: Model = {
            dbr: {
                value: r.DebtBurdenRatio,
                missingMessage: r.DebtBurdenRatioMissingReasonMessage,
            },
            ltl: {
                value: r.LeftToLiveOn?.LtlAmount,
                missingMessage: r.LeftToLiveOn?.UndefinedReasonMessage,
                details: r.LeftToLiveOn?.UndefinedReasonMessage ? null : r.LeftToLiveOn,
            },
            policy: {
                statusCode: this.getPolicyFilterEngineResultStatusCode(r?.PolicyFilterResult),
                missingMessage: r?.PolicyFilterResult ? null : 'Not calculated',
                detailPhases: this.generateDetailPhases(r),
            },
            pd: {
                value: r.ProbabilityOfDefaultPercent,
                missingMessage: r.ProbabilityOfDefaultMissingReasonMessage ? '-' : null,
            },
            preScore: preScoreRecommendation && preScoreRecommendation.PolicyFilterResult ? {
                statusCode: this.getPolicyFilterEngineResultStatusCode(preScoreRecommendation?.PolicyFilterResult),
                missingMessage: preScoreRecommendation?.PolicyFilterResult ? null : 'Not calculated',
                detailPhases: this.generateDetailPhases(preScoreRecommendation)
            } : null
        };

        this.m = m;
    }

    private generateDetailPhases(r : { PolicyFilterDetailsDisplayItems : PolicyFilterDetailsDisplayItem[]}){
        let detailPhases: {
            phaseDisplayName: string;
            isManualControlPhase: boolean;
            items: PolicyFilterDetailsDisplayItem[];
        }[] = null;

        if (!r?.PolicyFilterDetailsDisplayItems) {
            return detailPhases;
        }

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
        return detailPhases;
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
    dbr: {
        value?: number;
        missingMessage?: string;
    };
    ltl: {
        value?: number;
        missingMessage?: string;
        details?: CreditRecommendationLtlResult;
    };
    pd: {
        value?: number;
        missingMessage?: string;
    };
    policy: {
        statusCode: string;
        missingMessage?: string;
        detailPhases?: {
            phaseDisplayName: string;
            isManualControlPhase: boolean;
            items: PolicyFilterDetailsDisplayItem[];
        }[];
    };
    preScore ?: {
        statusCode: string
        missingMessage ?: string
        detailPhases?: {
            phaseDisplayName: string;
            isManualControlPhase: boolean;
            items: PolicyFilterDetailsDisplayItem[];
        }[];
    }
}

export class ApplicationPolicyInfoInitialData {
    recommendation: CreditRecommendationModel;
    preScoreRecommendation : PreScoreRecommendationModel;
}
