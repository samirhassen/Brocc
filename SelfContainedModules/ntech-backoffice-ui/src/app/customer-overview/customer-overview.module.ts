import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { CustomerOverviewRoutingModule } from './customer-overview-routing.module';
import { SearchComponent } from './search/search.component';
import { CustomerComponent } from './customer/customer.component';
import { CustomerCardComponent } from './customer-card/customer-card.component';
import { CustomerCardItemEditorComponent } from './customer-card/customer-card-item-editor/customer-card-item-editor.component';

@NgModule({
    declarations: [SearchComponent, CustomerComponent, CustomerCardComponent, CustomerCardItemEditorComponent],
    imports: [CommonModule, CustomerOverviewRoutingModule, ReactiveFormsModule, CommonComponentsModule],
})
export class CustomerOverviewModule {}
