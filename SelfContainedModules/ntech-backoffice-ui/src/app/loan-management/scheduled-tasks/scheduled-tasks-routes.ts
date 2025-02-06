import { Routes } from '@angular/router';
import { BackTargetResolverService } from 'src/app/common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from 'src/app/common-services/require-features.guard';
import { HighPermissionsGuard } from 'src/app/login/guards/high-permissions-guard';
import { AnnualStatementsComponent } from './annual-statements/annual-statements.component';
import { RemindersComponent } from './reminders/reminders.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { TerminationLettersComponent } from './termination-letters/termination-letters.component';
import { DailyKycScreenComponent } from './daily-kyc-screen/daily-kyc-screen.component';
import { BookKeepingComponent } from './book-keeping/book-keeping.component';
import { Cm1AmlExportComponent } from './cm1-aml-export/cm1-aml-export.component';
import { SatExportComponent } from './sat-export/sat-export.component';
import { TreasuryAmlExportComponent } from './treasury-aml-export/treasury-aml-export.component';
import { TrapetsAmlExportComponent } from './trapets-aml-export/trapets-aml-export.component';

export const scheduledTasksRoutes: Routes = [
    {
        path: 'scheduled-tasks',
        children: [
            {
                path: 'annual-statements',
                component: AnnualStatementsComponent,
                data: {
                    useFluidLayoutShell: true,
                    pageTitle: 'Credit annual statements',
                    requireAnyFeature: [
                        'ntech.feature.unsecuredloans.standard',
                        'ntech.feature.mortgageloans.standard',
                    ],
                },
            },
            {
                path: 'reminders',
                component: RemindersComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Reminders' },
            },
            {
                path: 'notifications',
                component: NotificationsComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Create notifications' },
            },
            {
                path: 'termination-letters',
                component: TerminationLettersComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Create termination letters' },
            },
            {
                path: 'daily-kyc-screen',
                component: DailyKycScreenComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'PEP/Sanction screening' },
            },
            {
                path: 'book-keeping',
                component: BookKeepingComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Bookkeeping files' },
            },
            {
                path: 'cm1-aml-export',
                component: Cm1AmlExportComponent,
                data: {
                    useFluidLayoutShell: true,
                    pageTitle: 'Cm1 AML Export',
                    requireAnyFeature: [ 'ntech.feature.Cm1aml.v1' ],
                },
            },
            {
                path: 'sat-export',
                component: SatExportComponent,
                data: {
                    useFluidLayoutShell: true,
                    pageTitle: 'SAT export',
                    requireClientCountry: 'FI',
                    requireAnyFeature: [ 'ntech.feature.unsecuredloans' ],
                },
            },
            {
                path: 'treasury-aml-export',
                component: TreasuryAmlExportComponent,
                data: {
                    useFluidLayoutShell: true,
                    pageTitle: 'Treasury AML Export',
                    requireAnyFeature: [ 'ntech.feature.Treasuryaml.v1' ],
                },
            },
            {
                path: 'trapets-aml-export',
                component: TrapetsAmlExportComponent,
                data: {
                    useFluidLayoutShell: true,
                    pageTitle: 'Trapets AML Export',
                    requireAnyFeature: [ 'ntech.feature.trapetsaml.v1' ],
                },
            },
        ],
        data: {},
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, HighPermissionsGuard],
    },
];
