import { Component, OnInit, Inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService, API_SERVICE } from '../../backend/api-service';
import { StringDictionary } from '../../backend/common.types';
import { ConfigService } from 'src/app/backend/config.service';

@Component({
    selector: 'start-application',
    templateUrl: './start-application.component.html',
    styleUrls: []
})
export class StartApplicationComponent implements OnInit {

    constructor(private route: ActivatedRoute,
        private router: Router,
        @Inject(API_SERVICE) private apiService: ApiService,
        configService: ConfigService) {
        this.route.queryParams.subscribe(params => {
            let externalVariables: StringDictionary = configService.getQueryStringParameters()
            apiService.startApplication(externalVariables).subscribe(x => {
                this.apiService.navigateToRoute('calculator', x.id)
            })
        });
    }

    ngOnInit() {
    }

}
