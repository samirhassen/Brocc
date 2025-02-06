import { Component, Input, OnInit } from '@angular/core';
import { TaskModel } from './customer-pages-application.component';

export type TaskDisplayState = 'normal' | 'focused' | 'hidden';

@Component({
    selector: 'customer-pages-application-tasks',
    template: `<div [class.frame]="displayState === 'focused'" *ngIf="support && displayState !== 'hidden'">
        <div class="task">
            <div class="pt-2">
                <h2 class="header-light" [ngClass]="{ 'text-center': displayState === 'focused' }">{{ title }}</h2>
                <ng-content select="[task-explanation]"></ng-content>

                <table *ngIf="tasks && tasks.length > 0" class="table table-separate">
                    <thead>
                        <tr>
                            <th class="col-xs-1"></th>
                            <th class="col-xs-10"></th>
                            <th class="col-xs-1"></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr
                            [ngClass]="getTaskRowClass(task)"
                            *ngFor="let task of tasks"
                            (click)="support.openTask(task, $event)"
                        >
                            <td>
                                <span
                                    [ngClass]="
                                        getIconClass(task.status.IsAccepted === true, task.status.IsAccepted === false)
                                    "
                                    class="custom-glyph ntech-status-icon"
                                ></span>
                            </td>
                            <td>{{ task.headerText }}</td>
                            <td class="text-right">
                                <span class="glyphicon-play glyphicon custom-glyph"></span>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>`,
    styleUrls: ['./customer-pages-application-tasks.scss'],
})
export class CustomerPagesApplicationTasksComponent implements OnInit {
    constructor() {}

    @Input()
    public displayState: TaskDisplayState;

    @Input()
    public tasks: TaskModel[];

    @Input()
    support: IApplicationTasksSupportFunctions;

    @Input()
    title: string;

    ngOnInit(): void {}

    getIconClass(isAccepted: boolean, isRejected: boolean) {
        let isOther = !isAccepted && !isRejected;
        return {
            'glyphicon-ok': isAccepted,
            'glyphicon-remove': isRejected,
            'glyphicon-minus': isOther,
            'glyphicon': true,
            'text-success': isAccepted,
            'text-danger': isRejected,
        };
    }

    getTaskRowClass(task: TaskModel) {
        return this.isTaskInactive(task) ? 'inactive-task-row' : 'active-task-row';
    }

    isTaskInactive(task: TaskModel) {        
        let status = task.status;
        if(task.isKycTask) {
            return !status.IsActive;
        } else {            
            return !status.IsActive && !(status.IsAccepted === true || status.IsAccepted === false);
        }        
    }
}

export interface IApplicationTasksSupportFunctions {
    openTask(task: TaskModel, evt?: Event): void;
}
