import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ManualCreditreportsRoutingModule } from './manual-creditreports-routing.module';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { BuyManualCreditreportComponent } from './buy-manual-creditreport/buy-manual-creditreport.component';
import { ManualCreditReportsApiService } from './services/manual-creditreports-api.service';
import { ReactiveFormsModule } from '@angular/forms';
import { ListCreditreportsComponent } from './list-creditreports/list-creditreports.component';
import { SearchCreditreportsComponent } from './search-creditreports/search-creditreports.component';

@NgModule({
    providers: [ManualCreditReportsApiService],
    declarations: [BuyManualCreditreportComponent, ListCreditreportsComponent, SearchCreditreportsComponent],
    imports: [CommonModule, ReactiveFormsModule, ManualCreditreportsRoutingModule, CommonComponentsModule],
})
export class ManualCreditreportsModule {}
