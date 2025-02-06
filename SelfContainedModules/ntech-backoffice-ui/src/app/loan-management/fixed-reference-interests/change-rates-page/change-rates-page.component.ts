import { Component, OnInit } from '@angular/core';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { FixedRateService, RateServerModel } from '../../services/fixed-rate-service';
import { PendingChangeComponentInitialData } from '../pending-change/pending-change.component';
import { RateEditorComponentInitialData } from '../rate-editor/rate-editor.component';

@Component({
    selector: 'app-change-rates-page',
    templateUrl: './change-rates-page.component.html',
    styleUrls: ['./change-rates-page.component.scss'],
})
export class ChangeRatesPageComponent implements OnInit {
    constructor(private configService: ConfigService, private fixedRateService: FixedRateService) {}

    public m: Model;

    async ngOnInit() {
        await this.reload();
    }

    private async reload() {
        let result = await this.fixedRateService.getCurrentRates();

        let tf: TestFunctionsModel = null;
        if (this.configService.isNTechTest()) {
            tf = new TestFunctionsModel();
            if (result.PendingChange) {
                tf.addFunctionCall('Force approve', () => {
                    this.fixedRateService.commitCurrentChange(true).then(() => {
                        this.reload();
                    });
                });
            }
        }

        this.m = {
            serverRates: result.CurrentRates,
            pendingChangeInitialData: result.PendingChange
                ? {
                      activeChange: result.PendingChange,
                      currentRates: result.CurrentRates,
                      onCommit: async () => {
                          await this.fixedRateService.commitCurrentChange(false);
                          await this.reload();
                      },
                      onCancel: async () => {
                          await this.fixedRateService.cancelCurrentChange();
                          await this.reload();
                      },
                  }
                : null,
            testFunctions: tf,
        };
    }

    public parseMonthCount(nrOfMonths: number) {
        return this.fixedRateService.parseMonthCountShared(nrOfMonths);
    }

    public beginChange(evt?: Event) {
        evt?.preventDefault();
        this.m.editInitialData = {
            currentRates: this.m.serverRates,
            onInitiateChange: async (newRates) => {
                await this.fixedRateService.initiateChange(newRates);
                await this.reload();
            },
        };
    }
}

interface Model {
    serverRates: RateServerModel[];
    editInitialData?: RateEditorComponentInitialData;
    pendingChangeInitialData?: PendingChangeComponentInitialData;
    testFunctions: TestFunctionsModel;
}
