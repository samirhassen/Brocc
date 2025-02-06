import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { combineLatest } from 'rxjs';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'app-show-report',
    templateUrl: './show-report.component.html',
    styles: [
    ]
})
export class ShowReportComponent implements OnInit {
    constructor(private route: ActivatedRoute, private configService: ConfigService, private apiService: NtechApiService,
        private sanitizer: DomSanitizer) { }
        
    public m: Model = null;    
    
    @ViewChild('downloadAnchor') downloadAnchor: ElementRef;
    
    async ngOnInit() {
        let pr = this.route.pathFromRoot;
        //Example: s/test/show-report/NTechHost/Api/Credit/Report/MortageLoan/AnnexTwo-Excel?kitten=2
        combineLatest([
            pr[pr.length - 2].url, //example: [show-report, NTechHost]
            pr[pr.length - 1].url] //example: [Api, Credit, Report, MortageLoan, AnnexTwo-Excel]
        ).subscribe(async ([parentUrl, url]) => {
            let moduleName = parentUrl[parentUrl.length - 1].path;
            let inModulePath = url.join('/');

            try {
                let query = '';
                let queryParams = this.configService.getQueryStringParameters() ?? {};
                let queryParamKeys = Object.keys(queryParams);
                for(var i=0; i<queryParamKeys.length; i++) {
                    query += (i == 0 ? '?' : '&') + `${encodeURIComponent(queryParamKeys[i])}=${encodeURIComponent(queryParams[queryParamKeys[i]])}`
                }
                
                let result = await this.apiService.download(moduleName, inModulePath + query, null, true);
                this.m = {
                    report: {
                        objectUrl: this.sanitizer.bypassSecurityTrustUrl(window.URL.createObjectURL(result)),
                        rawData: result
                    },
                    error: null
                }

                setTimeout(() => {
                    this.downloadAnchor?.nativeElement?.click();
                }, 200);
            } catch(err: any) {
                this.m = {
                    report: null,
                    error: err
                }
            }            
        });
    }
}

interface Model {
    report: {
        objectUrl: SafeUrl
        rawData: Blob
    }
    error : any
}