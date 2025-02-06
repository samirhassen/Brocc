import { Component, Input, OnInit } from '@angular/core';

@Component({
    selector: 'jobrunner-task-result',
    templateUrl: './jobrunner-task-result.component.html',
    styles: [],
})
export class JobrunnerTaskResultComponent implements OnInit {
    constructor() {}

    @Input()
    public result: JobrunnerTaskResultModel;

    ngOnInit(): void {}
}

export interface JobrunnerTaskResultModel {
    warnings: string[];
    errors: string[];
    totalMilliseconds: number;
}
