import { Component, Input, OnInit } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'scheduled-tasks-menu',
    templateUrl: './scheduled-tasks-menu.component.html',
    styles: [],
})
export class ScheduledTasksMenuComponent implements OnInit {
    constructor(private apiService: NtechApiService) {}

    public enabledTasks: TaskModel[];

    @Input()
    public currentTaskName: string;

    ngOnInit(): void {
        this.apiService.post<TaskModel[]>('nCredit', 'Api/ScheduledTasks/Fetch-AllTasks', {}).then((allTasks) => {
            this.enabledTasks = allTasks.filter((x) => x.IsEnabled);
        });
    }
}

interface TaskModel {
    TaskDisplayName: string;
    IsEnabled: boolean;
    TaskName: string;
    AbsoluteUrl: string;
}
