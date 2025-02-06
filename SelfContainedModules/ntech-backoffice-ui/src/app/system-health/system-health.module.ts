import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SystemHealthRoutingModule } from './system-health-routing.module';
import { ErrorsComponent } from './pages/errors/errors.component';

@NgModule({
    declarations: [ErrorsComponent],
    imports: [CommonModule, SystemHealthRoutingModule],
})
export class SystemHealthModule {}
