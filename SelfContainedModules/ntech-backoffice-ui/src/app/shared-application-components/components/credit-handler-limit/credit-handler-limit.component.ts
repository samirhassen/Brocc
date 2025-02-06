import { Component } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'credit-handler-limit',
    templateUrl: './credit-handler-limit.component.html',
})
export class CreditHandlerLimitComponent {
    constructor(private apiService: NtechApiService) {}
    public m: Model;
    public nameFilter: any;

    async ngOnInit(): Promise<void> {
        const creditHandlerLimits = await this.apiService.post<{ users: User[]; levels: Level[] }>(
            'nPreCredit',
            'api/CreditHandlerLimit/Get',
            {}
        );

        this.m = {
            users: creditHandlerLimits.users,
            levels: creditHandlerLimits.levels,
        };
    }

    beginEdit = (user: User, evt?: MouseEvent): void => {
        evt?.preventDefault();

        user.edit = {
            LimitLevel: user.LimitLevel.toString(),
            IsOverrideAllowed: user.IsOverrideAllowed,
        };
    };

    cancelEdit = (user: User, evt?: MouseEvent): void => {
        evt?.preventDefault();
        user.edit = undefined;
    };

    async saveEdit(user: User, evt?: MouseEvent): Promise<void> {
        evt?.preventDefault();

        await this.apiService.post('nPreCredit', 'api/CreditHandlerLimit/Edit', {
            userId: user.UserId,
            limitLevel: user.edit.LimitLevel,
            isOverrideAllowed: user.edit.IsOverrideAllowed,
        });

        user.LimitLevel = parseInt(user.edit.LimitLevel, 10);
        (user.IsOverrideAllowed = user.edit.IsOverrideAllowed), (user.edit = null);
    }

    isEditingAny(): boolean {
        let isEdit: boolean = false;
        this.m.users.forEach((x) => {
            if (x.edit != null) {
                isEdit = true;
                return;
            }
        });

        return isEdit;
    }
}

interface Model {
    users: User[];
    levels: Level[];
}

interface User {
    DisplayName: string;
    LimitLevel: number;
    IsOverrideAllowed: boolean;
    UserId: number;
    edit?: {
        LimitLevel: string;
        IsOverrideAllowed: boolean;
    };
}

interface Level {
    LimitLevel: number;
    MaxAmount: number;
}
