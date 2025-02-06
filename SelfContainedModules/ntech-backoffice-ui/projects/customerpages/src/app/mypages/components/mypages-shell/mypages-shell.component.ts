import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { BehaviorSubject } from 'rxjs';
import { CustomerPagesApiService } from '../../../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import { CustomerMessagesHelper } from '../../../shared-components/customer-messages-helper';

export enum MyPagesMenuItemCode {
    Overview = 'overview',
    Loans = 'loans',
    UnsecuredLoanApplications = 'unsecuredLoanApplications',
    MortgageLoanApplications = 'mortgageLoanApplications',
    SavingsAccounts = 'savingsAccounts',
    Documents = 'documents',
    SecureMessage = 'secureMessages',
    MyData = 'myData',
}

@Component({
    selector: 'mypages-shell',
    templateUrl: './mypages-shell.component.html',
    styles: [],
})
export class MypagesShellComponent implements OnInit {
    constructor(
        public config: CustomerPagesConfigService,
        private titleService: Title,
        private sharedApiService: CustomerPagesApiService
    ) {}

    @Input()
    public initialData: MypagesShellComponentInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.titleService.setTitle('Mina sidor');

        this.m = null;

        if (!this.initialData) {
            return;
        }

        let m = new Model();

        m.hasNoCustomerRelation = this.initialData.hasNoCustomerRelation;
        m.addMenuItem(MyPagesMenuItemCode.Overview, 'Översikt', ['/my/overview']);
        if (
            this.config.isAnyFeatureEnabled([
                'ntech.feature.unsecuredloans.standard',
                'ntech.feature.mortgageloans.standard',
            ])
        ) {
            m.addMenuItem(MyPagesMenuItemCode.Loans, 'Lån', ['/my/loans']);
        }
        if (this.config.isAnyFeatureEnabled(['ntech.feature.savingsstandard'])) {
            m.addTestMenuItem(MyPagesMenuItemCode.SavingsAccounts, 'Sparkonton');
        }
        m.addMenuItem(MyPagesMenuItemCode.Documents, 'Dokument', ['/my/documents']);
        m.addMenuItem(MyPagesMenuItemCode.SecureMessage, 'Meddelanden', ['/my/messages']);
        m.addMenuItem(MyPagesMenuItemCode.MyData, 'Mina uppgifter', ['/my/data']);

        m.setActiveMenuItem(this.initialData.activeMenuItemCode);

        this.refreshUnreadMessagesCount(m).then((_) => {
            this.m = m;
        });
    }

    private refreshUnreadMessagesCount(m: Model) {
        if (this.initialData.activeMenuItemCode == MyPagesMenuItemCode.SecureMessage) {
            //No reason to load this for the messages page
            //since we will mark everything as read as soon as the customer enters the page
            return new Promise<void>((resolve) => {
                m.unreadMessagesCount.next(0);
                resolve();
            });
        } else {
            let messagesHelper = new CustomerMessagesHelper(null, this.config, this.sharedApiService);
            return messagesHelper.getSecureMessagesUnreadByCustomerCount().then((x) => {
                m.unreadMessagesCount.next(x.UnreadCount);
            });
        }
    }

    toggleMobileMenu(evt?: Event) {
        evt?.preventDefault();
        this.m.isMobileMenuVisible = !this.m.isMobileMenuVisible;
    }

    logOut(evt?: Event) {
        evt?.preventDefault();

        document.location.href = this.config.config()?.LogoutUrl;
    }
}

export class MypagesShellComponentInitialData {
    activeMenuItemCode: MyPagesMenuItemCode;
    hasNoCustomerRelation?: boolean;
}

class Model {
    constructor() {
        this.menuItems = [];
        this.isMobileMenuVisible = false;
        this.unreadMessagesCount = new BehaviorSubject<number>(0);
    }

    public unreadMessagesCount: BehaviorSubject<number>;

    menuItems: MenuItemModel[];
    isMobileMenuVisible: boolean;
    hasNoCustomerRelation: boolean;

    addMenuItem(code: MyPagesMenuItemCode, displayName: string, routerLink: string[]) {
        let i = new MenuItemModel(code, displayName, routerLink, false);
        this.menuItems.push(i);
        return i;
    }

    addTestMenuItem(code: MyPagesMenuItemCode, displayName: string) {
        let routerLink = [`/my/menu-test/${code}`];
        return this.addMenuItem(code, displayName, routerLink);
    }

    setActiveMenuItem(code: MyPagesMenuItemCode) {
        for (let m of this.menuItems) {
            m.isActive = m.code === code;
        }
    }
}

class MenuItemModel {
    constructor(
        public code: MyPagesMenuItemCode,
        public codeDisplayName: string,
        public routerLink: string[],
        public isActive: boolean
    ) {}

    public isVisible(hideMessages: boolean, onlyMessages: boolean) {
        if (hideMessages && this.code == 'secureMessages') {
            return false;
        }
        if (onlyMessages && this.code != 'secureMessages') {
            return false;
        }
        return true;
    }
}
