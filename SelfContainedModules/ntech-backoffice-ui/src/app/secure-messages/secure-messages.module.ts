import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SecureMessagesListComponent } from './secure-messages-list/secure-messages-list.component';
import { SecureMessagesRoutingModule } from './secure-messages-routing.module';
import { MenuShellComponent } from './menu-shell/menu-shell.component';
import { SecureMessagesChannelComponent } from './secure-messages-channel/secure-messages-channel.component';
import { FormsModule } from '@angular/forms';
import { SecureMessagesCreateComponent } from './secure-messages-create/secure-messages-create.component';
import { CommonComponentsModule } from '../common-components/common-components.module';

//quill
import { QuillModule } from 'ngx-quill';

@NgModule({
    declarations: [
        SecureMessagesListComponent,
        MenuShellComponent,
        SecureMessagesChannelComponent,
        SecureMessagesCreateComponent,
    ],
    imports: [CommonModule, SecureMessagesRoutingModule, FormsModule, CommonComponentsModule, QuillModule.forRoot()],
})
export class SecureMessagesModule {}
