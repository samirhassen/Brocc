import { Component, OnInit } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'app-errors',
    templateUrl: './errors.component.html',
    styles: [],
})
export class ErrorsComponent implements OnInit {
    constructor(private apiService: NtechApiService) {}

    public m: Model;

    ngOnInit(): void {
        let m: Model = {
            nextPage: 0,
            errors: [],
        };
        this.loadMore(m).then((_) => {
            this.m = m;
        });
    }

    loadMore(m: Model) {
        let page = m.nextPage;
        return this.apiService.post<Error[]>('NTechHost', 'Api/SystemLog/Fetch-Latest-Errors', { page: page }).then((x) => {
            for (let error of x) {
                m.errors.push(error);
            }
            m.nextPage = page + 1;
        });
    }

    showFullText(s: string, evt?: Event) {
        evt?.preventDefault();

        this.m.fullText = s;
    }

    closeFullText(evt?: Event) {
        evt?.preventDefault();

        this.m.fullText = null;
    }

    fetchErrors(evt?: Event) {
        evt?.preventDefault();

        this.loadMore(this.m);
    }
}

class Model {
    nextPage: number;
    errors: Error[];
    fullText?: string;
}

export interface Error {
    eventDate: string;
    serviceName: string;
    requestUri: string;
    remoteIp: string;
    message: string;
    exceptionMessage: string;
}
