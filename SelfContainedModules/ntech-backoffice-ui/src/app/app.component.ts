import { Component } from '@angular/core';
import { setTheme } from 'ngx-bootstrap/utils';

setTheme('bs3'); //Ensure that the bootstrap 3 theme is used

@Component({
    selector: 'ui-root',
    templateUrl: './app.component.html',
    styles: [],
})
export class AppComponent {
    title = 'ntech-backoffice-ui';
}
