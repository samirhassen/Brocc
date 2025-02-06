import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CustomerpagesShellInitialData } from '../../../shared-components/customerpages-shell/customerpages-shell.component';

@Component({
    selector: 'np-pre-application-received',
    templateUrl: './pre-application-received.component.html',
    styleUrls: ['./pre-application-received.component.scss']
})
export class PreApplicationReceivedComponent implements OnInit {
    constructor(private route: ActivatedRoute) { }

    public shellData: CustomerpagesShellInitialData = {
        logoRouterLink: null,
        skipBodyLayout: false,
        wideNavigation: false
    };

    public m: Model;

    ngOnInit(): void {
        let applicationNr = this.route.snapshot.params['applicationNr'];
        if(applicationNr) {
            this.m = {
                applicationNr: applicationNr
            };
        }
    }
}

interface Model {
    applicationNr: string
}
