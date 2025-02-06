import { Component, Input, SimpleChanges } from '@angular/core';
import { BankAccountNrValidationValidAccountResult } from 'src/app/common-services/bank-account-nr-validation.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'repayment',
    templateUrl: './repayment.component.html',
    styles: [
    ]
})
export class RepaymentComponent {
    constructor(private apiService: NtechApiService) { }

    @Input()
    public initialData : RepaymentInitialData;

    public m: Model;

    ngOnChanges(_ : SimpleChanges) {
        this.m = null;

        let i = this.initialData;

        if(!i) {
            return;
        }
        
        this.m = {
            isRepaying: false,
            repaymentAmount: i.repaymentAmount,
            customerName: i.repayToName,
            leaveUnplacedAmount: i.unplacedAmount - i.repaymentAmount,
            validBankAccountNr: {
                bankAccountNrType: i.repayToBankAccount.BankAccountNrType,
                bankName: i.repayToBankAccount.BankName,
                displayValue: i.repayToBankAccount.DisplayNr
            }
        }
    }

    async repay(evt ?: Event) {
        evt?.preventDefault();
        this.m.isRepaying = true;
        setTimeout(() => {
            let i = this.initialData;
            this.apiService.post('NTechHost', 'Api/Credit/PaymentPlacement/Repay-UnplacedPayment', {
                paymentId: i.paymentId,
                customerName: i.repayToName,
                repaymentAmount: i.repaymentAmount,
                leaveUnplacedAmount: this.m.leaveUnplacedAmount,
                bankAccountNrType: i.repayToBankAccount.BankAccountNrType,
                bankAccountNr: i.repayToBankAccount.NormalizedNr
            }).then(_ => {
                document.location.href = this.apiService.getUiGatewayUrl('nCredit', 'Ui/UnplacedPayments/List');
            })
        }, 0);
    }
}

interface Model {
    repaymentAmount: number
    leaveUnplacedAmount: number
    customerName: string
    validBankAccountNr: {
        bankAccountNrType: string
        bankName: string
        displayValue: string
    }
    isRepaying: boolean
}

export interface RepaymentInitialData {
    repayToBankAccount: BankAccountNrValidationValidAccountResult
    unplacedAmount: number
    repaymentAmount: number
    repayToName: string
    paymentId: number
}