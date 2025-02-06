import { Component, OnInit } from '@angular/core';
import { ConfigService } from 'src/app/backend/config.service';
import { BehaviorSubject } from 'rxjs';

@Component({
    selector: 'calculator-footer',
    templateUrl: './calculator-footer.component.html',
    styleUrls: []
})
export class CalculatorFooterComponent implements OnInit {
    isLegalInterestCeilingEnabled: BehaviorSubject<boolean>
    pdfSuffix: BehaviorSubject<string>

    constructor(private configService: ConfigService) {
        this.isLegalInterestCeilingEnabled = new BehaviorSubject<boolean>(this.configService.isLegalInterestCeilingEnabled())

        let suffixFromBool = x => x ? '_2' : '_1'

        this.pdfSuffix = new BehaviorSubject<string>(suffixFromBool(this.isLegalInterestCeilingEnabled.value))
        this.isLegalInterestCeilingEnabled.subscribe(x => {
            this.pdfSuffix.next(suffixFromBool(x))
        })        
    }

    ngOnInit() {

    }

}
