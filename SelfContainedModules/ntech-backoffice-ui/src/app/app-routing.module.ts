import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NotAuthorizedComponent } from './common-components/not-authorized/not-authorized.component';
import { NotFoundComponent } from './common-components/not-found/not-found.component';
import { LoginCompleteComponent, LoginCompleteGuard } from './login/login-complete/login-complete.component';

const routes: Routes = [
    { path: 'login-complete', component: LoginCompleteComponent, canActivate: [LoginCompleteGuard] },
    { path: 'not-found', component: NotFoundComponent },
    { path: 'not-authorized', component: NotAuthorizedComponent },
];

@NgModule({
    imports: [RouterModule.forRoot(routes, { enableTracing: false, onSameUrlNavigation: 'reload' })],
    exports: [RouterModule],
})
export class AppRoutingModule {}
