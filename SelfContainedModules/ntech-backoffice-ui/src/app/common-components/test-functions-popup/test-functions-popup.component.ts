import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import * as moment from 'moment';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'test-functions-popup',
    templateUrl: './test-functions-popup.component.html',
    styles: [],
})
export class TestFunctionsPopupComponent implements OnInit {
    constructor(configService: ConfigService) {
        this.isTest = configService.isNTechTest();
        this.isPopupVisible = false;
    }

    public isTest: boolean;
    public isPopupVisible: boolean;

    @Input()
    public model: TestFunctionsModel;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.isPopupVisible = false;
    }

    callFunction(i: TestFunctionItem, evt?: Event) {
        evt?.preventDefault();
        i.functionCall();
        this.isPopupVisible = false;
    }

    togglePopup(evt?: Event) {
        evt?.preventDefault();
        this.isPopupVisible = !this.isPopupVisible;
    }
}

export class TestFunctionsModel {
    constructor() {
        this.items = [];
    }

    public items: TestFunctionItem[];

    addLink(text: string, linkUrl: string) {
        this.items.push({ text: text, isLink: true, linkUrl: linkUrl });
    }

    addFunctionCall(text: string, functionCall?: () => void) {
        this.items.push({ text: text, isFunctionCall: true, functionCall: functionCall });
    }

    /**
     * Gets random int
     * @param min
     * @param max
     * @returns random int - min & max inclusive
     */
    getRandomInt(min: number, max: number): number {
        let localMin = Math.ceil(min);
        let localMax = Math.floor(max);
        return Math.floor(Math.random() * (localMax - localMin + 1)) + localMin;
    }

    pickRandom<T>(values: T[]) {
        if (!values || values.length === 0) {
            return null;
        }
        return values[this.getRandomInt(0, values.length - 1)];
    }

    getRandomHistoricalDate(currentDate: moment.Moment, minDaysBack: number, maxDaysBack: number) {
        let d = currentDate.clone();
        return d.subtract(this.getRandomInt(minDaysBack, maxDaysBack), 'days');
    }

    getRandomFutureDate(currentDate: moment.Moment, minDaysForward: number, maxDaysForward: number) {
        let d = currentDate.clone();
        return d.add(this.getRandomInt(minDaysForward, maxDaysForward), 'days');
    }

    generateTestPdfDataUrl(text: string): string {
        //Small pdf from https://stackoverflow.com/questions/17279712/what-is-the-smallest-possible-valid-pdf
        let pdfData = `%PDF-1.2
9 0 obj
<<
>>
stream
BT/ 9 Tf(${text})' ET
endstream
endobj
4 0 obj
<<
/Type /Page
/Parent 5 0 R
/Contents 9 0 R
>>
endobj
5 0 obj
<<
/Kids [4 0 R ]
/Count 1
/Type /Pages
/MediaBox [ 0 0 300 50 ]
>>
endobj
3 0 obj
<<
/Pages 5 0 R
/Type /Catalog
>>
endobj
trailer
<<
/Root 3 0 R
>>
%%EOF`;

        return 'data:application/pdf;base64,' + btoa(unescape(encodeURIComponent(pdfData)));
    }
}

class TestFunctionItem {
    text: string;
    isLink?: boolean;
    linkUrl?: string;
    isFunctionCall?: boolean;
    functionCall?: () => void;
}
