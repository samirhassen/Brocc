import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PositiveCreditRegisterMainComponent } from './positive-credit-register-main/positive-credit-register-main.component';
import { PositiveCreditRegisterRoutingModule } from './positive-credit-register-routing.module';
import { CommonComponentsModule } from 'src/app/common-components/common-components.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@NgModule({
    declarations: [PositiveCreditRegisterMainComponent],
    imports: [
        CommonModule,
        PositiveCreditRegisterRoutingModule,
        FormsModule,
        ReactiveFormsModule,
        CommonComponentsModule,
    ],
})
export class PositiveCreditRegisterModule {}
