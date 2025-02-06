import { OnChanges } from '@angular/core';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { WorkflowModel } from 'src/app/shared-application-components/services/shared-loan-application-api.service';
import { WorkflowStepHelper } from 'src/app/shared-application-components/services/workflow-helper';
import { StandardMortgageLoanApplicationModel } from '../services/mortgage-loan-application-model';

export interface WorkflowStepComponent extends OnChanges {
    initialData: WorkflowStepInitialData;
}

export class WorkflowStepInitialData {
    application: StandardMortgageLoanApplicationModel;
    applicationNavigationTarget: CrossModuleNavigationTarget;
    workflow: {
        model: WorkflowModel;
        step: WorkflowStepHelper;
    };
    testFunctions: TestFunctionsModel;
}
