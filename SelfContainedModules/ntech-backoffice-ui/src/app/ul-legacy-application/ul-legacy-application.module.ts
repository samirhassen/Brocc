import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LegacyApplicationBasisPageComponent } from './pages/legacy-application-basis-page/legacy-application-basis-page.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { UlLegacyApplicationRoutingModule } from './ul-legacy-application-routing.module';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { LegacyAppNewFieldComponent } from './components/legacy-app-new-field/legacy-app-new-field.component';

@NgModule({
    declarations: [
        LegacyApplicationBasisPageComponent,
        LegacyAppNewFieldComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        UlLegacyApplicationRoutingModule,
        CommonComponentsModule
    ]
})
export class UlLegacyApplicationModule { }
