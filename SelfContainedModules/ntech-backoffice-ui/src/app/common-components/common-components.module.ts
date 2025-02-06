import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LayoutShellComponent } from './layout-shell/layout-shell.component';
import { CustomerInfoComponent } from './customer-info/customer-info.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { HtmlPreviewComponent } from './html-preview/html-preview.component';
import { SafeHtmlPipe } from './pipes/safe-html.pipe';
import { NotAuthorizedComponent } from './not-authorized/not-authorized.component';
import { RouterModule } from '@angular/router';
import { OnEnterDirective } from './directives/on-enter.directive';
import { TablePagerComponent } from './table-pager/table-pager.component';
import { TestFunctionsPopupComponent } from './test-functions-popup/test-functions-popup.component';
import { ToggleBlockComponent } from './toggle-block/toggle-block.component';
import { SendEmailLogoComponent } from './send-email-logo/send-email-logo.component';
import { ScheduledTasksMenuComponent } from './scheduled-tasks-menu/scheduled-tasks-menu.component';
import { JobrunnerTaskResultComponent } from './jobrunner-task-result/jobrunner-task-result.component';
import { ExportStatusTableCellComponent } from './export-status-table-cell/export-status-table-cell.component';
import { AiSearchPopupComponent } from './ai-search-popup/ai-search-popup.component';

@NgModule({
    declarations: [
        LayoutShellComponent,
        CustomerInfoComponent,
        NotFoundComponent,
        HtmlPreviewComponent,
        SafeHtmlPipe,
        NotAuthorizedComponent,
        OnEnterDirective,
        TablePagerComponent,
        TestFunctionsPopupComponent,
        ToggleBlockComponent,
        SendEmailLogoComponent,
        ScheduledTasksMenuComponent,
        JobrunnerTaskResultComponent,
        ExportStatusTableCellComponent,
        AiSearchPopupComponent,
    ],
    imports: [CommonModule, RouterModule],
    exports: [
        LayoutShellComponent,
        CustomerInfoComponent,
        NotFoundComponent,
        HtmlPreviewComponent,
        SafeHtmlPipe,
        NotAuthorizedComponent,
        OnEnterDirective,
        TablePagerComponent,
        TestFunctionsPopupComponent,
        ToggleBlockComponent,
        SendEmailLogoComponent,
        ScheduledTasksMenuComponent,
        JobrunnerTaskResultComponent,
        ExportStatusTableCellComponent,
    ],
})
export class CommonComponentsModule {}
