import { Component, Input, SimpleChanges } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { ApplicantDataEditorInitialData } from 'src/app/shared-application-components/components/applicant-data-editor/applicant-data-editor.component';
import { HouseholdEconomyDataEditorInitialData } from 'src/app/shared-application-components/components/household-economy-data-editor/household-economy-data-editor.component';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';
import { ApplicationDataEditorInitialData } from '../application-data-editor/application-data-editor.component';

@Component({
    selector: 'application-basis',
    templateUrl: './application-basis.component.html',
    styles: [],
})
export class ApplicationBasisComponent {
    constructor(private apiService: UnsecuredLoanApplicationApiService) {}

    @Input()
    public initialData: ApplicationBasisComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;
        if (!this.initialData) {
            return;
        }
        this.reset();
    }

    getApplicantHeader = (applicantNr: number) => {
        const isMainApplicant = applicantNr === 1;
        const firstName = isMainApplicant
            ? this.m.mainApplicantInitialData.applicantInfo.FirstName
            : this.m.coApplicantInitialData.applicantInfo.FirstName;

        const birthDate = isMainApplicant
            ? this.m.mainApplicantInitialData.applicantInfo.BirthDate
            : this.m.coApplicantInitialData.applicantInfo.BirthDate;

        if (firstName !== null && firstName !== undefined && birthDate !== null && birthDate !== undefined)
            return `${firstName}, ${birthDate}`;

        if (firstName !== null && firstName !== undefined) return firstName;

        return isMainApplicant ? 'Main applicant' : 'Co applicant';
    };

    reset(): void {
        let isEditing = this.initialData.sharedIsEditing ?? new BehaviorSubject<boolean>(false);
        let model: Model = {
            application: this.initialData.application,
            sharedIsEditing: isEditing,
            applicationInitialData: {
                application: this.initialData.application,
                forceReadonly: this.initialData.forceReadonly,
                sharedIsEditing: isEditing,
            },
            mainApplicantInitialData: {
                application: this.initialData.application,
                applicantNr: 1,
                applicantInfo: this.initialData.application.applicantInfoByApplicantNr[1],
                forceReadonly: this.initialData.forceReadonly,
                sharedIsEditing: isEditing,
                apiService: this.apiService,
            },
            coApplicantInitialData:
                this.initialData.application.nrOfApplicants > 1
                    ? {
                          application: this.initialData.application,
                          applicantNr: 2,
                          applicantInfo: this.initialData.application.applicantInfoByApplicantNr[2],
                          forceReadonly: this.initialData.forceReadonly,
                          sharedIsEditing: isEditing,
                          apiService: this.apiService,
                      }
                    : null,
            householdEconomyInitialData: {
                application: this.initialData.application,
                forceReadonly: this.initialData.forceReadonly,
                sharedIsEditing: isEditing,
                apiService: this.apiService,
            },
        };

        this.m = model;
    }
}

export class ApplicationBasisComponentInitialData {
    application: StandardCreditApplicationModel;
    sharedIsEditing?: BehaviorSubject<boolean>;
    forceReadonly?: boolean;
}

class Model {
    application: StandardCreditApplicationModel;
    sharedIsEditing: BehaviorSubject<boolean>;
    mainApplicantInitialData: ApplicantDataEditorInitialData;
    coApplicantInitialData?: ApplicantDataEditorInitialData;
    applicationInitialData: ApplicationDataEditorInitialData;
    householdEconomyInitialData: HouseholdEconomyDataEditorInitialData;
}
