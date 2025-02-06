import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ListComponent } from './list/list.component';
import { GenerateComponent } from './generate/generate.component';
import { KeyComponent } from './key/key.component';
import { ApiKeysRoutingModule } from './api-keys-routing.module';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { ReactiveFormsModule } from '@angular/forms';

@NgModule({
    declarations: [ListComponent, GenerateComponent, KeyComponent],
    imports: [CommonModule, ApiKeysRoutingModule, CommonComponentsModule, ReactiveFormsModule],
})
export class ApiKeysModule {}
