import { Component, Input, SimpleChanges } from '@angular/core';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { NumberDictionary } from 'src/app/common.types';
import {
    KycCustomerOnboardingStatusModel,
    SharedApplicationApiService,
} from '../../services/shared-loan-application-api.service';

@Component({
    selector: 'kyc-step-applicant-statuses',
    templateUrl: './kyc-step-applicant-statuses.component.html',
    styles: [],
})
export class KycStepApplicantStatusesComponent {
    constructor(private config: ConfigService, private eventService: NtechEventService) {}

    @Input()
    public initialData: KycStepApplicantStatusesComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let status = getApplicantsKycStatusModel(this.initialData.customerStatuses);

        this.m = {
            isPossibleToScreen: this.initialData.isStepActiveAndCurrent && !status.hasBeenScreened,
            hasBeenScreened: status.hasBeenScreened,
        };
    }

    getCustomerCardUrl(customerId: number) {
        return this.config.getServiceRegistry().createUrl('nCustomer', 'Customer/CustomerCard', [
            ['customerId', customerId.toString()],
            ['backTarget', this.initialData.applicationNavigationTarget?.getCode()],
        ]);
    }

    getKycCardUrl(customerId: number) {
        return this.config.getServiceRegistry().createUrl('nCustomer', 'Ui/KycManagement/Manage', [
            ['customerId', customerId.toString()],
            ['backTarget', this.initialData.applicationNavigationTarget?.getCode()],
        ]);
    }

    screen(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.applicationNr;
        let customerIds = this.initialData.customerStatuses.map((x) => x.CustomerId);
        this.initialData.apiService.kycScreenBatch(customerIds, this.config.getCurrentDateAndTime()).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }

    getRolesDisplayText(customer: KycCustomerOnboardingStatusModel) {
        if (!this.isMl() || !this.initialData) {
            return '';
        }
        let roles = this.initialData.allConnectedCustomerIdsWithRoles[customer.CustomerId].map((x) => {
            if (x == 'Applicant') return 'applicant';
            if (x == 'mortgageLoanPropertyOwner') return 'property owner';
            if (x == 'mortgageLoanConsentingParty') return 'consenting party';
            return x;
        });
        if (roles.length === 0) {
            return '';
        }
        return ` (${roles.join(', ')})`;
    }

    public isMl() {
        return this.config.isFeatureEnabled('ntech.feature.mortgageloans.standard');
    }
}

interface Model {
    isPossibleToScreen: boolean;
    hasBeenScreened: boolean;
}

export function getApplicantsKycStatusModel(
    customerStatuses: KycCustomerOnboardingStatusModel[]
): ApplicantsKycStatusModel {
    let result: ApplicantsKycStatusModel = {
        hasAnsweredQuestions: true,
        hasBeenScreened: true,
        anySanction: false,
        anyUnknown: false,
        anyWithoutNameAddressOrEmail: false,
        hasStatusThatAllowsApprove: false,
    };

    customerStatuses.forEach((status) => {
        if (!status.LatestKycQuestionsAnswerDate) {
            result.hasAnsweredQuestions = false;
        }
        if (!status.LatestScreeningDate) {
            result.hasBeenScreened = false;
        }
        if (status.IsSanction === true) {
            result.anySanction = true;
        }
        if (
            (status.IsSanction !== true && status.IsSanction !== false) ||
            (status.IsPep !== true && status.IsPep !== false)
        ) {
            result.anyUnknown = true;
        }

        if (!status.HasNameAndAddress) {
            result.anyWithoutNameAddressOrEmail = true;
        }
    });

    result.hasStatusThatAllowsApprove =
        result.hasBeenScreened && !result.anySanction && !result.anyUnknown && !result.anyWithoutNameAddressOrEmail;

    return result;
}

export interface ApplicantsKycStatusModel {
    hasAnsweredQuestions: boolean;
    hasBeenScreened: boolean;
    anySanction: boolean;
    anyUnknown: boolean;
    anyWithoutNameAddressOrEmail: boolean;
    hasStatusThatAllowsApprove: boolean;
}

export interface KycStepApplicantStatusesComponentInitialData {
    applicationNr: string;
    isStepActiveAndCurrent: boolean;
    customerStatuses: KycCustomerOnboardingStatusModel[];
    applicationNavigationTarget: CrossModuleNavigationTarget;
    apiService: SharedApplicationApiService;
    allConnectedCustomerIdsWithRoles: NumberDictionary<string[]>;
}
