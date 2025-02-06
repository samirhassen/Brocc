import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary, NumberDictionary } from 'src/app/common.types';
import { PolicyFilterRuleSetListItemModel } from 'src/app/policyfilter-components/policyfilter-ruleset-list/policyfilter-ruleset-list.component';
import {
    DisplayFormattedPolicyFilterRulesetRule,
    PolicyfilterRuleAndStaticParameterValues,
    PolicyfilterRuleSet,
    PolicyFilterRuleUiModel,
} from 'src/app/policyfilter-components/policyfilter-ruleset/policyfilter-ruleset.component';

@Injectable({
    providedIn: 'root',
})
export class PolicyFiltersApiService {
    constructor(private apiService: NtechApiService) {}

    addTestPolicyFilterRuleSet(skipIfExists: boolean, overrideSlotName?: string): Promise<{ WasCreated: boolean }> {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/PolicyFilters/Create-TestSet', {
            skipIfExists,
            overrideSlotName,
        });
    }

    createOrGetPendingPolicyFilterSet(name?: string): Promise<{ WasCreated: boolean; Id: number }> {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/PolicyFilters/CreateOrEdit-Set', {
            NewPending: {
                name: name,
                useGeneratedName: !name ? true : false,
            },
        });
    }

    editPolicyFilterSet(
        id: number,
        updatedRuleSet: PolicyfilterRuleSet,
        updatedRuleSetName: string
    ): Promise<{ WasCreated: boolean; Id: number }> {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/PolicyFilters/CreateOrEdit-Set', {
            UpdateExisting: {
                id: id,
                ruleSet: updatedRuleSet,
                updatedName: updatedRuleSetName,
            },
        });
    }

    fetchPolicyFilterRuleSets(
        includeAllRules: boolean
    ): Promise<{ RuleSets: PolicyFilterRuleSetListItemModel[]; AllRules: PolicyFilterRuleUiModel[] }> {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/PolicyFilters/Fetch-RuleSets', { includeAllRules });
    }

    formatPolicyFilterRulesetForDisplay(
        rulesByKey: Dictionary<PolicyfilterRuleAndStaticParameterValues>
    ): Promise<{ DisplayFormattedRulesByKey: Dictionary<DisplayFormattedPolicyFilterRulesetRule> }> {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/PolicyFilters/Format-Rules-For-Display', {
            rulesByKey,
        });
    }

    changePolicyFilterSlot(rulesetId: number, slotName: string): Promise<{ MovedToInactiveId?: number }> {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/PolicyFilters/Change-Slot', {
            id: rulesetId,
            slotName,
        });
    }
}

export class RunFraudControlResult {
    FraudControls: {
        CheckName: string;
        Values: string[];
        HasMatch: boolean;
        IsApproved: boolean;
    }[];
}

export interface PolicyFilterCreditRecommendationModel {
    LeftToLiveOn: CreditRecommendationLtlResult;
    PolicyFilterResult: PolicyFilterEngineResult;
    PolicyFilterDetailsDisplayItems: PolicyFilterDetailsDisplayItem[];
}

export interface CreditRecommendationLtlResult {
    LtlAmount: number;
    DisplayLtlAmount: string;
    DisplaySummaryHeader: string;
    DisplaySummaryFooter: string;
    UndefinedReasonMessage: string;
    Groups: {
        Code: string;
        ContributionAmount: number;
        DisplayGroupHeader: string;
        DisplayContributionHeader: string;
        DisplayContributionAmount: string;
        ContributionItems: {
            ItemCode: string;
            ContributionAmount: number;
            DisplayLabel: string;
            DisplayContributionAmount: number;
        }[];
        InformationItems: {
            ItemCode: string;
            Value: number;
            DisplayLabel: string;
            DisplayValue: string;
        }[];
    }[];
}

export interface PolicyFilterDetailsDisplayItem {
    RuleName: string;
    RuleDisplayName: string;
    ForApplicantNr?: number;
    StaticParametersDisplayText: string;
    VariablesDisplayText: string;
    IsRejectedByRule?: boolean;
    IsSkipped: boolean;
    PhaseDisplayName: string;
    PhaseName: string;
    IsManualControlPhase: boolean;
}

export interface PolicyFilterEngineResult {
    InternalResult: PolicyFilterEnginePhaseResult;
    ExternalResult: PolicyFilterEnginePhaseResult;
    ManualControlResult: PolicyFilterEnginePhaseResult;
    VariableSet: PolicyFilterVariableSet;
    IsAcceptRecommended?: boolean;
    IsManualControlRecommended?: boolean;
    RejectionReasonNames?: string[];
}

export interface PolicyFilterEnginePhaseResult {
    RuleResults: PolicyFilterRuleResult[];
}

export interface PolicyFilterRuleResult {
    RuleName: string;
    ForApplicantNr?: number;
    StaticParameters: PolicyFilterStaticParameterSet;
    IsRejectedByRule?: boolean;
    IsSkipped: boolean;
    IsMissingApplicationLevelVariable: boolean;
    IsMissingApplicantLevelVariable: boolean;
    MissingVariableName: string;
    MissingApplicantLevelApplicantNrs: string[];
}

export interface PolicyFilterVariableSet {
    NrOfApplicants: number;
    ApplicationValues: Dictionary<string>;
    ApplicantValues: NumberDictionary<Dictionary<string>>;
}

export interface PolicyFilterStaticParameterSet {
    StoredValues: Dictionary<string>;
}
