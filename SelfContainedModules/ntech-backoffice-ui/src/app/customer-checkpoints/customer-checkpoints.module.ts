import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CheckpointsMainComponent } from './checkpoints-main/checkpoints-main.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { CustomerCheckpointsRoutingModule } from './customer-checkpoints-routing.module';
import { SearchComponent } from './search/search.component';
import { ViewAndEditComponent } from './view-and-edit/view-and-edit.component';
import { CheckpointsForCustomerComponent } from './checkpoints-for-customer/checkpoints-for-customer.component';

@NgModule({
    declarations: [CheckpointsMainComponent, SearchComponent, ViewAndEditComponent, CheckpointsForCustomerComponent],
    imports: [CommonModule, CustomerCheckpointsRoutingModule, FormsModule, ReactiveFormsModule, CommonComponentsModule],
})
export class CustomerCheckpointsModule {}
