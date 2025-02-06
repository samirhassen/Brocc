import { Component, OnInit } from '@angular/core';
import * as moment from 'moment';
import { createMortgagePropertyIdFromCollateralItems } from 'src/app/common-services/ml-standard-functions';
import { getDictionaryValues, NumberDictionary } from 'src/app/common.types';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import {
    MyPagesMenuItemCode,
    MypagesShellComponentInitialData,
} from '../../components/mypages-shell/mypages-shell.component';
import { CreditModel, MyPagesApiService } from '../../services/mypages-api.service';

@Component({
    selector: 'np-my-loans',
    templateUrl: './my-loans.component.html',
    styles: [],
})
export class MyLoansComponent implements OnInit {
    constructor(private apiService: MyPagesApiService, private config: CustomerPagesConfigService) {}

    public m: Model;

    async ngOnInit() {
        const credits = await this.apiService.fetchCredits(null, true);

        let isMl = this.config.isMortgageLoansStandardEnabled();
        let title = isMl ? 'Bolån' : 'Lån';

        let loanBlocks: LoansBlockModel[];
        let inactiveLoanBlocks: LoansBlockModel[];
        if (this.config.isMortgageLoansStandardEnabled()) {
            loanBlocks = this.getMlLoanBlocks(credits.ActiveCredits);
            inactiveLoanBlocks = this.getMlLoanBlocks(credits.InactiveCredits);
        } else {
            loanBlocks = [
                {
                    propertyId: '',
                    loans: credits.ActiveCredits.map((x) => {
                        return {
                            creditNr: x.CreditNr,
                            capitalBalanceAmount: x.CapitalBalance,
                        };
                    }),
                },
            ];

            inactiveLoanBlocks = [
                {
                    propertyId: '',
                    loans: credits.InactiveCredits?.map((x) => {
                        return {
                            creditNr: x.CreditNr,
                            capitalBalanceAmount: x.CapitalBalance,
                            status: x.Status,
                            endDate: x.EndDate,
                        };
                    }),
                },
            ];
        }

        this.m = new Model(title, isMl, loanBlocks, inactiveLoanBlocks);
    }

    getMlLoanBlocks(credits: CreditModel[]) {
        let blocks: NumberDictionary<LoansBlockModel> = {};
        for (let credit of credits) {
            if (!blocks[credit.MortgageLoan.CollateralId]) {
                blocks[credit.MortgageLoan.CollateralId] = {
                    propertyId: createMortgagePropertyIdFromCollateralItems(
                        (x) => credit.MortgageLoan.CollateralStringItems[x],
                        true
                    ),
                    loans: [],
                };
            }
            blocks[credit.MortgageLoan.CollateralId].loans.push({
                creditNr: credit.CreditNr,
                capitalBalanceAmount: credit.CapitalBalance,
                status: credit.Status,
                endDate: credit.EndDate,
            });
        }
        return getDictionaryValues(blocks);
    }

    //todo create shared enum for CreditStatus
    getInactiveCreditText(credit: LoanModel): string {
        const creditEndDateText = credit.endDate
            ? ` ${moment(credit.endDate).format('YYYY-MM-DD')}`
            : ' [datum saknas]';

        switch (credit.status) {
            case 'SentToDebtCollection':
                return 'skickat på inkasso' + creditEndDateText;
            case 'Settled':
            case 'WrittenOff':
                return 'avslutat sedan' + creditEndDateText;
            default:
                return '';
        }
    }
}

class Model {
    public shellInitialData: MypagesShellComponentInitialData;
    constructor(
        public title: string,
        public isMl: boolean,
        public loanBlocks: LoansBlockModel[],
        public inactiveLoans: LoansBlockModel[]
    ) {
        this.shellInitialData = {
            activeMenuItemCode: MyPagesMenuItemCode.Loans,
        };
    }
}

interface LoanModel {
    creditNr: string;
    capitalBalanceAmount: number;
    status?: string;
    endDate?: string;
}

interface LoansBlockModel {
    propertyId: string;
    loans: LoanModel[];
}
