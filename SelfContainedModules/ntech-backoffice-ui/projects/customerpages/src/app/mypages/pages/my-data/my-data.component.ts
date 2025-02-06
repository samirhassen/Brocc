import { Component, OnInit } from '@angular/core';
import {
    MyPagesMenuItemCode,
    MypagesShellComponentInitialData,
} from '../../components/mypages-shell/mypages-shell.component';
import { Router } from '@angular/router';

@Component({
    selector: 'mp-my-data',
    templateUrl: './my-data.component.html',
    styles: [],
})
export class MyDataComponent implements OnInit {
    constructor(private router: Router) {}

    public shellInitialData: MypagesShellComponentInitialData;

    ngOnInit() {
        this.shellInitialData = {
            activeMenuItemCode: MyPagesMenuItemCode.MyData,
        };
    }

    goToKycOverview() {
        this.router.navigateByUrl(`kyc/overview?fromTarget=StandardMyData&lang=sv`);
    }
}
