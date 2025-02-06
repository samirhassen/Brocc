import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { MlSeAmortizationBasisComponent } from './ml-se-amortization-basis/ml-se-amortization-basis.component';
import { KycAnswersViewComponent } from './kyc-answers-view/kyc-answers-view.component';
import { KycAnswersEditorComponent } from './kyc-answers-editor/kyc-answers-editor.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { KreditzDatashareIframeComponent } from './kreditz-datashare-iframe/kreditz-datashare-iframe.component';

@NgModule({
  declarations: [
    MlSeAmortizationBasisComponent,
    KycAnswersViewComponent,
    KycAnswersEditorComponent,
    KreditzDatashareIframeComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
  ],
  exports: [
    MlSeAmortizationBasisComponent,
    KycAnswersViewComponent,
    KycAnswersEditorComponent,
    KreditzDatashareIframeComponent
  ]
})
export class NtechComponentsModule { }
