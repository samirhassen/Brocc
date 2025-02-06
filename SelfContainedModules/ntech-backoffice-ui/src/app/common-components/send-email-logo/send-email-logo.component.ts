import { Component, Input, SimpleChanges } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'send-email-logo',
    templateUrl: './send-email-logo.component.html',
    styles: [],
})
export class SendEmailLogoComponent {
    constructor(private config: ConfigService, private apiService: NtechApiService) {}

    @Input()
    public emailSettingName: string;

    public willSendEmail: boolean;

    ngOnInit(): void {
        this.willSendEmail = this.config.hasEmailProvider();
    }

    async ngOnChanges(changes: SimpleChanges) {
        if (this.emailSettingName && this.config.hasEmailProvider()) {
            let emailSettings = await this.apiService.shared.getCurrentSettingValues(this.emailSettingName);
            this.willSendEmail = emailSettings.SettingValues['isEnabled'] === 'true';
        } else {
            this.willSendEmail = this.config.hasEmailProvider();
        }
    }
}
