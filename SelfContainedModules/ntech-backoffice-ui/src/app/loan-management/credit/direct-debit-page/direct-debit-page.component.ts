import { Component } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import {
    CreditDirectDebitDetailsApplicantModel,
    CreditDirectDebitDetailsModel,
    CreditService,
    DirectDebitEventModel,
    ValidateBankAccountNrResult,
    ValidateBankAccountNrResultAccount,
} from '../credit.service';

@Component({
    selector: 'app-direct-debit-page',
    templateUrl: './direct-debit-page.component.html',
    styles: [],
})
export class DirectDebitPageComponent {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private toastr: ToastrService
    ) {}

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('creditNr'));
        });
    }

    public m: LocalModel;

    private async reload(creditNr: string) {
        this.m = null;

        if (!creditNr) {
            let title = 'Credit collateral';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }
        this.eventService.setCustomPageTitle(`Credit ${creditNr}`, `Credit collateral ${creditNr}`);

        let details = await this.creditService.fetchCreditDirectDebitDetails(creditNr, null, false);

        //let targetToHere = CrossModuleNavigationTarget.create('CreditMortgageloanStandardCollateral', { creditNr: creditNr })

        this.m = new LocalModel(details.Details, details.Events);
        this.m.scheduleChanges = [];
    }

    beginInternalEdit(evt?: Event) {
        evt?.preventDefault();

        if (!this.m) return;

        this.m.internalDirectDebitEdit = new DdEditModel(this.m.ddView);
    }

    cancelInternalEdit(evt: Event) {
        if (evt) evt.preventDefault();

        this.m.internalDirectDebitEdit = null;
    }

    removeDirectDebitSchedulation(evt: Event) {
        if (evt) evt.preventDefault();

        if (!this.m || !this.m.ssView) return;

        let paymentNr =
            this.m.ssView.PendingSchedulation.Status == 'PendingCancellation'
                ? this.m.ssView.Schedulation.AccountOwner.StandardPaymentNr
                : this.m.ssView.PendingSchedulation.AccountOwner.StandardPaymentNr;
        if (!paymentNr) {
            this.toastr.error('Missing payment number.');
            return;
        }

        this.creditService.removeDirectDebitSchedulation(this.m.creditNr, paymentNr).then((_) => {
            this.signalReloadRequired();
        });
    }

    commitInternalEdit(evt: Event) {
        if (evt) evt.preventDefault();

        if (!this.m) return;

        let v = this.m.ddView;
        if (!v.AccountOwner || !v.AccountOwner.ApplicantNr) {
            this.toastr.error('Account owner missing.');
            return;
        }
        if (!v.BankAccount || !v.BankAccount.NormalizedNr) {
            this.toastr.error('Bank account number missing.');
            return;
        }

        this.creditService
            .updateDirectDebitCheckStatus(
                this.m.creditNr,
                this.m.ddEdit.Status,
                v.BankAccount.NormalizedNr,
                v.AccountOwner.ApplicantNr
            )
            .then((_) => {
                this.signalReloadRequired();
            });
    }

    getBankAccountDisplayText(bankAccount?: ValidateBankAccountNrResultAccount, includeBankName?: boolean) {
        if (bankAccount === null || bankAccount.AccountNr === null || bankAccount.ClearingNr === null) return '-';

        const bankAccountText = `${bankAccount.ClearingNr}${bankAccount.AccountNr}`;

        if (includeBankName) return `${bankAccountText}, ${bankAccount.BankName ?? '-'}`;

        return bankAccountText;
    }

    async initiateActivation(evt: Event, isChangeActivated?: boolean) {
        if (evt) evt.preventDefault;

        if (!this.m || !this.m.ssEdit) return;

        let schedulation = this.m.ssEdit.PendingSchedulation;
        try {
            if (schedulation.Status === 'Change') await this.initiateScheduledChange(isChangeActivated);
            else if (schedulation.Status == 'Active') await this.initiateScheduledActivation(isChangeActivated);
            else if (schedulation.Status === 'NotActive') await this.initiateScheduledCancellation(isChangeActivated);
            else {
                this.toastr.error('Schedulation status error.');
                return;
            }
        } catch (e) {
            this.toastr.error((e as any)?.statusText ?? 'Failed');
        }
    }

    async initiateScheduledActivation(isChangeActivated: boolean) {
        let schedulation = this.m.ssEdit.PendingSchedulation;

        if (!schedulation.AccountOwnerApplicantNr) {
            this.toastr.error('Account owner missing.');
            return;
        }

        if (!schedulation.BankAccountNr) {
            this.toastr.error('Bank account number missing.');
            return;
        }

        let applicant = this.m.getPersonNameAndDateModelByApplicantNr(parseInt(schedulation.AccountOwnerApplicantNr));

        await this.creditService.scheduleDirectDebitActivation(
            isChangeActivated !== null ? isChangeActivated : false,
            this.m.creditNr,
            schedulation.BankAccountNr,
            applicant.StandardPaymentNr,
            applicant.ApplicantNr,
            applicant.CustomerId
        );

        this.signalReloadRequired();
    }

    async initiateScheduledCancellation(isChangeActivated: boolean) {
        if (!this.m.ssView.Schedulation.AccountOwner || !this.m.ssView.Schedulation.AccountOwner.StandardPaymentNr) {
            this.toastr.error('Missing payment number.');
            return;
        }

        await this.creditService.scheduleDirectDebitCancellation(
            this.m.creditNr,
            isChangeActivated !== null ? isChangeActivated : false,
            this.m.ssView.Schedulation.AccountOwner.StandardPaymentNr
        );

        this.signalReloadRequired();
    }

    async initiateScheduledChange(isChangeActivated: boolean) {
        let schedulation = this.m.ssEdit.PendingSchedulation;

        if (!schedulation.AccountOwnerApplicantNr) {
            this.toastr.error('Account owner missing.');
            return;
        }

        if (!schedulation.BankAccountNr) {
            this.toastr.error('Bank account number missing.');
            return;
        }

        let applicant = this.m.getPersonNameAndDateModelByApplicantNr(parseInt(schedulation.AccountOwnerApplicantNr));
        let currentStatus = this.m.ssView.Schedulation.Status;
        await this.creditService.scheduleDirectDebitChange(
            currentStatus,
            isChangeActivated !== null ? isChangeActivated : false,
            this.m.creditNr,
            schedulation.BankAccountNr,
            applicant.StandardPaymentNr,
            parseInt(schedulation.AccountOwnerApplicantNr),
            applicant.CustomerId
        );
        this.signalReloadRequired();
    }

    changeActivationConfirm(evt?: Event) {
        if (evt) evt.preventDefault();

        if (!this.m || !this.m.ssView) return;

        if (this.m.ssView.PendingSchedulation.Status === 'PendingChange') this.activateSchedulationChange();
        else if (this.m.ssView.PendingSchedulation.Status === 'PendingActivation')
            this.activateSchedulationActivation();
        else if (this.m.ssView.PendingSchedulation.Status === 'PendingCancellation') {
            this.creditService
                .scheduleDirectDebitCancellation(
                    this.m.creditNr,
                    true,
                    this.m.ssView.Schedulation.AccountOwner.StandardPaymentNr
                )
                .then((x) => {
                    this.signalReloadRequired();
                });
        } else {
            this.toastr.error('Invalid schedulation status.');
            return;
        }
    }

    activateSchedulationChange() {
        let schedulation = this.m.ssView;

        if (
            !schedulation.PendingSchedulation.AccountOwner ||
            !schedulation.PendingSchedulation.AccountOwner.ApplicantNr
        ) {
            this.toastr.error('Missing account owner.');
            return;
        }

        let applicant = this.m.getPersonNameAndDateModelByApplicantNr(
            schedulation.PendingSchedulation.AccountOwner.ApplicantNr
        );
        this.creditService
            .scheduleDirectDebitChange(
                this.m.ssView.Schedulation.Status,
                true,
                this.m.creditNr,
                schedulation.PendingSchedulation.BankAccount.NormalizedNr,
                applicant.StandardPaymentNr,
                applicant.ApplicantNr,
                applicant.CustomerId
            )
            .then((result) => {
                this.signalReloadRequired();
            });
    }

    activateSchedulationActivation() {
        let schedulation = this.m.ssView;

        if (
            !schedulation.PendingSchedulation.AccountOwner ||
            !schedulation.PendingSchedulation.AccountOwner.ApplicantNr
        ) {
            this.toastr.error('Missing account owner.');
            return;
        }

        let applicant = this.m.getPersonNameAndDateModelByApplicantNr(
            schedulation.PendingSchedulation.AccountOwner.ApplicantNr
        );
        this.creditService
            .scheduleDirectDebitActivation(
                true,
                this.m.creditNr,
                schedulation.PendingSchedulation.BankAccount === null
                    ? null
                    : schedulation.PendingSchedulation.BankAccount.NormalizedNr,
                applicant.StandardPaymentNr,
                applicant.ApplicantNr,
                applicant.CustomerId
            )
            .then((_) => {
                this.signalReloadRequired();
            });
    }

    private signalReloadRequired() {
        this.reload(this.m.creditNr);
    }

    onBankAccountEdited(newValueEvent: Event) {
        if (!this.m || !this.m.ssEdit.PendingSchedulation) {
            return;
        }

        this.m.ssEdit.PendingSchedulation.BankAccountValidationResult = null;

        let newValue = (newValueEvent.currentTarget as any)?.value;

        if (!newValue) {
            return;
        }

        this.creditService.validateBankAccountNr(newValue).then((result) => {
            this.m.ssEdit.PendingSchedulation.BankAccountValidationResult = result;
        });
    }

    onAccountOwnerApplicantNrEdited() {
        if (!this.m || !this.m.ssEdit.PendingSchedulation) {
            return;
        }

        this.m.ssEdit.PendingSchedulation.WasAccountOwnerApplicantNrRecentlyChanged = true;
        setTimeout(() => {
            this.m.ssEdit.PendingSchedulation.WasAccountOwnerApplicantNrRecentlyChanged = false;
        }, 400);
    }

    toggleScheduleChange(evt?: Event) {
        if (evt) evt.preventDefault();

        if (!this.m) return;

        let m = this.m;
        if (m.scheduleChanges) {
            m.scheduleChanges = null;
        } else {
            m.scheduleChanges = [];
        }
    }

    toggleEvents(evt: Event) {
        if (evt) evt.preventDefault();

        if (!this.m) {
            return;
        }
        let m = this.m;
        if (m.events) {
            m.events = null;
        } else {
            this.creditService.fetchCreditDirectEvents(this.m.creditNr).then((result) => {
                m.events = result;
            });
        }
    }

    isSchedulationStatusPending(excludePending: boolean) {
        if (!excludePending) {
            if (
                this.m.ssView.PendingSchedulation.Status === 'PendingActivation' ||
                this.m.ssView.PendingSchedulation.Status === 'PendingCancellation' ||
                this.m.ssView.PendingSchedulation.Status === 'PendingChange'
            )
                return true;
        } else if (excludePending) {
            if (
                this.m.ssView.PendingSchedulation.Status !== 'PendingActivation' &&
                this.m.ssView.PendingSchedulation.Status !== 'PendingCancellation' &&
                this.m.ssView.PendingSchedulation.Status !== 'PendingChange'
            )
                return true;
        }

        return false;
    }

    getStatusText(status: string) {
        let txt = '';
        switch (status) {
            case 'Active':
            case 'Activation':
                txt = 'Active';
                break;
            case 'NotActive':
            case 'Cancellation':
                txt = 'Not Active';
                break;
            case 'PendingActivation':
                txt = 'Pending Activation';
                break;
            case 'PendingCancellation':
                txt = 'Pending Cancellation';
                break;
            case 'PendingChange':
                txt = 'Pending Change';
                break;
            default:
                txt = status;
                break;
        }
        return txt;
    }
}

class LocalModel {
    constructor(d: CreditDirectDebitDetailsModel, events: DirectDebitEventModel[]) {
        let ownersByApplicantNr = new Dictionary<number, CreditDirectDebitDetailsApplicantModel>();
        for (let a of d.Applicants) {
            ownersByApplicantNr.add(a.ApplicantNr, a);
        }

        this.ddView = this.createDdViewModel(d, ownersByApplicantNr);
        this.ssView = this.createSsViewModel(d, ownersByApplicantNr);

        //This crazy init was added when porting from angularjs. It was always utterly broken, angularjs just happily ignores that and magics up an empty PendingSchedulation on use
        this.ssEdit = {
            PendingSchedulation: {
                Status: null,
                AccountOwnerApplicantNr: null,
                WasAccountOwnerApplicantNrRecentlyChanged: null,
                BankAccountNr: null,
                BankAccountValidationResult: null,
            },
            Schedulation: null,
        };
        this.ddEdit = {
            Status: null,
            AccountOwnerApplicantNr: null,
            WasAccountOwnerApplicantNrRecentlyChanged: null,
            BankAccountNr: null,
            BankAccountValidationResult: null,
        };
        this.allOwnersOrdered = d.Applicants;
        this.allOwnersByApplicantNr = ownersByApplicantNr;
        this.creditNr = d.CreditNr;
        this.events = events;
    }
    public creditNr: string;
    allOwnersByApplicantNr: Dictionary<number, CreditDirectDebitDetailsApplicantModel>;
    ddView: DdViewModel;
    ddEdit: DdEditModel;
    internalDirectDebitEdit: DdEditModel;

    //schedulation status
    ssView: SsViewModel;
    ssEdit: { PendingSchedulation: SsEditModel; Schedulation: SsEditModel };

    allOwnersOrdered: CreditDirectDebitDetailsApplicantModel[];
    events: DirectDebitEventModel[];
    scheduleChanges: [];

    getPersonNameAndDateModelByApplicantNr(applicantNr: number) {
        if (!applicantNr) return undefined;

        return this.allOwnersByApplicantNr.getValue(applicantNr);
    }

    private createDdViewModel(
        d: CreditDirectDebitDetailsModel,
        ownersByApplicantNr: Dictionary<number, CreditDirectDebitDetailsApplicantModel>
    ): DdViewModel {
        let v = new DdViewModel();

        v.IsEditAllowed = true;
        v.InternalDirectDebitCheckStatus = d.IsActive ? 'Active' : 'NotActive';
        v.BankAccount = d.BankAccount == null ? null : d.BankAccount.IsValid ? d.BankAccount.ValidAccount : null;

        if (d.AccountOwnerApplicantNr) {
            v.AccountOwner = ownersByApplicantNr.getValue(d.AccountOwnerApplicantNr);
        }

        return v;
    }

    private createSsViewModel(
        d: CreditDirectDebitDetailsModel,
        ownersByApplicantNr: Dictionary<number, CreditDirectDebitDetailsApplicantModel>
    ): SsViewModel {
        let v = new SsViewModel();
        v.PendingSchedulation = {
            Status:
                d.SchedulationChangesModel.PendingSchedulationDetails.SchedulationOperation === null
                    ? null
                    : d.SchedulationChangesModel.PendingSchedulationDetails.SchedulationOperation,
            AccountOwner:
                d.SchedulationChangesModel.PendingSchedulationDetails.AccountOwnerApplicantNr == null
                    ? null
                    : ownersByApplicantNr.getValue(
                          d.SchedulationChangesModel.PendingSchedulationDetails.AccountOwnerApplicantNr
                      ),
            BankAccount:
                d.SchedulationChangesModel.PendingSchedulationDetails.BankAccount == null
                    ? null
                    : d.SchedulationChangesModel.PendingSchedulationDetails.BankAccount.ValidAccount,
        };

        v.Schedulation = {
            Status:
                d.SchedulationChangesModel.SchedulationDetails.SchedulationOperation === null
                    ? null
                    : d.SchedulationChangesModel.SchedulationDetails.SchedulationOperation,
            AccountOwner:
                d.SchedulationChangesModel.SchedulationDetails.AccountOwnerApplicantNr == null
                    ? null
                    : ownersByApplicantNr.getValue(
                          d.SchedulationChangesModel.SchedulationDetails.AccountOwnerApplicantNr
                      ),
            BankAccount:
                d.SchedulationChangesModel.SchedulationDetails.BankAccount == null
                    ? null
                    : d.SchedulationChangesModel.SchedulationDetails.BankAccount.ValidAccount,
        };

        return v;
    }
}

class DdViewModel {
    InternalDirectDebitCheckStatus: string;
    IsEditAllowed: boolean;
    AccountOwner: CreditDirectDebitDetailsApplicantModel;
    BankAccount: ValidateBankAccountNrResultAccount;
}

class DdEditModel {
    constructor(v: DdViewModel) {
        if (v.AccountOwner) {
            this.AccountOwnerApplicantNr = v.AccountOwner.ApplicantNr.toString();
        }
        if (v.BankAccount) {
            this.BankAccountNr = v.BankAccount.NormalizedNr;
        }
        if (v.InternalDirectDebitCheckStatus) this.Status = v.InternalDirectDebitCheckStatus;
    }
    AccountOwnerApplicantNr: string;
    BankAccountNr: string;
    Status: string;
    BankAccountValidationResult: ValidateBankAccountNrResult;
    WasAccountOwnerApplicantNrRecentlyChanged: boolean;
}

class SsViewModel {
    PendingSchedulation: {
        Status: string;
        AccountOwner: CreditDirectDebitDetailsApplicantModel;
        BankAccount: ValidateBankAccountNrResultAccount;
    };
    Schedulation: {
        Status: string;
        AccountOwner: CreditDirectDebitDetailsApplicantModel;
        BankAccount: ValidateBankAccountNrResultAccount;
    };
}

class SsEditModel {
    constructor(v: SsViewModel) {
        if (v.PendingSchedulation.AccountOwner) {
            this.AccountOwnerApplicantNr = v.PendingSchedulation.AccountOwner.ApplicantNr.toString();
        }
        if (v.PendingSchedulation.BankAccount) {
            this.BankAccountNr = v.PendingSchedulation.BankAccount.NormalizedNr;
        }
        if (v.PendingSchedulation.Status) {
            this.Status = v.PendingSchedulation.Status;
        }
    }
    AccountOwnerApplicantNr: string;
    BankAccountNr: string;
    Status: string;

    BankAccountValidationResult: ValidateBankAccountNrResult;
    WasAccountOwnerApplicantNrRecentlyChanged: boolean;
}

class Dictionary<T extends number | string, U> {
    private _keys: T[] = [];
    private _values: U[] = [];

    private undefinedKeyErrorMessage: string = 'Key is either undefined, null or an empty string.';

    private isEitherUndefinedNullOrStringEmpty(object: any): boolean {
        return typeof object === 'undefined' || object === null || object.toString() === '';
    }

    private checkKeyAndPerformAction(
        action: { (key: T, value?: U): void | U | boolean },
        key: T,
        value?: U
    ): void | U | boolean {
        if (this.isEitherUndefinedNullOrStringEmpty(key)) {
            throw new Error(this.undefinedKeyErrorMessage);
        }

        return action(key, value);
    }

    public add(key: T, value: U): void {
        var addAction = (key: T, value: U): void => {
            if (this.containsKey(key)) {
                throw new Error('An element with the same key already exists in the dictionary.');
            }

            this._keys.push(key);
            this._values.push(value);
        };

        this.checkKeyAndPerformAction(addAction, key, value);
    }

    public remove(key: T): boolean {
        var removeAction = (key: T): boolean => {
            if (!this.containsKey(key)) {
                return false;
            }

            var index = this._keys.indexOf(key);
            this._keys.splice(index, 1);
            this._values.splice(index, 1);

            return true;
        };

        return <boolean>this.checkKeyAndPerformAction(removeAction, key);
    }

    public getValue(key: T): U {
        var getValueAction = (key: T): U => {
            if (!this.containsKey(key)) {
                return null;
            }

            var index = this._keys.indexOf(key);
            return this._values[index];
        };

        return <U>this.checkKeyAndPerformAction(getValueAction, key);
    }

    public containsKey(key: T): boolean {
        var containsKeyAction = (key: T): boolean => {
            if (this._keys.indexOf(key) === -1) {
                return false;
            }
            return true;
        };

        return <boolean>this.checkKeyAndPerformAction(containsKeyAction, key);
    }

    public changeValueForKey(key: T, newValue: U): void {
        var changeValueForKeyAction = (key: T, newValue: U): void => {
            if (!this.containsKey(key)) {
                throw new Error('In the dictionary there is no element with the given key.');
            }

            var index = this._keys.indexOf(key);
            this._values[index] = newValue;
        };

        this.checkKeyAndPerformAction(changeValueForKeyAction, key, newValue);
    }

    public keys(): T[] {
        return this._keys;
    }

    public values(): U[] {
        return this._values;
    }

    public count(): number {
        return this._values.length;
    }
}
