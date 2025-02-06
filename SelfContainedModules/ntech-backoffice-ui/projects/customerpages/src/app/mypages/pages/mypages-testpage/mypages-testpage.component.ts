import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MypagesShellComponentInitialData } from '../../components/mypages-shell/mypages-shell.component';

@Component({
    selector: 'np-mypages-testpage',
    templateUrl: './mypages-testpage.component.html',
    styles: [],
})
export class MypagesTestpageComponent implements OnInit {
    constructor(private route: ActivatedRoute) {}

    public shellInitialData: MypagesShellComponentInitialData;

    ngOnInit(): void {
        this.route.params.subscribe((x) => {
            this.shellInitialData = {
                activeMenuItemCode: x['menuCode'],
            };
        });
    }
}
