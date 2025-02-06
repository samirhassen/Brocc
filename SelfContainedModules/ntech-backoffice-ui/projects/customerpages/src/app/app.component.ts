import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { setTheme } from 'ngx-bootstrap/utils';

setTheme('bs3'); //Ensure that the bootstrap 3 theme is used

@Component({
    selector: 'np-root',
    templateUrl: './app.component.html',
    styleUrls: [],
})
export class AppComponent {
    //@ts-ignore TODO remove unused locals
    constructor(private httpClient: HttpClient) {}

    ngOnInit(): void {}
}
