import { Component, Input, OnInit, SimpleChanges } from '@angular/core';

@Component({
    selector: 'table-pager',
    templateUrl: './table-pager.component.html',
    styles: [],
})
export class TablePagerComponent implements OnInit {
    constructor() {}

    @Input()
    public initialData: TablePagerInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        this.setup(this.initialData);
    }

    setup(pagingResult: IPagingResult) {
        if (!pagingResult) {
            this.m = {
                pages: [],
                isNextAllowed: false,
                isPreviousAllowed: false,
                nextPageNr: 0,
                previousPageNr: 0,
            };
            return;
        }

        let pages: Page[] = [];
        //9 items including separators are the most shown at one time
        //The two items before and after the current item are shown
        //The first and last item are always shown
        for (var i = 0; i < pagingResult.totalNrOfPages; i++) {
            if (
                (i >= pagingResult.currentPageNr - 2 && i <= pagingResult.currentPageNr + 2) ||
                pagingResult.totalNrOfPages <= 9
            ) {
                pages.push({ pageNr: i, isCurrentPage: pagingResult.currentPageNr == i, isSeparator: false }); //Primary pages are always visible
            } else if (i == 0 || i == pagingResult.totalNrOfPages - 1) {
                pages.push({ pageNr: i, isCurrentPage: pagingResult.currentPageNr == i, isSeparator: false }); //First and last page are always visible
            } else if (i == pagingResult.currentPageNr - 3 || i == pagingResult.currentPageNr + 3) {
                pages.push({ pageNr: i, isSeparator: true, isCurrentPage: false }); //First and last page are always visible
            }
        }

        this.m = {
            pages: pages,
            isPreviousAllowed: pagingResult.currentPageNr > 0,
            previousPageNr: pagingResult.currentPageNr - 1,
            isNextAllowed: pagingResult.currentPageNr < pagingResult.totalNrOfPages - 1,
            nextPageNr: pagingResult.currentPageNr + 1,
        };
    }

    gotoPage(pageNr: number, evt?: Event) {
        evt?.preventDefault();
        this?.initialData?.onGotoPage(pageNr);
    }
}

export interface IPagingResult {
    currentPageNr: number;
    totalNrOfPages: number;
}

export class TablePagerInitialData implements IPagingResult {
    currentPageNr: number;
    totalNrOfPages: number;
    onGotoPage: (pageNr: number) => void;
}

class Model {
    pages: Page[];
    isPreviousAllowed: boolean;
    previousPageNr?: number;
    isNextAllowed: boolean;
    nextPageNr?: number;
}

class Page {
    isCurrentPage: boolean;
    isSeparator: boolean;
    pageNr: number;
}

export function splitIntoPages<T>(allItems: T[], pageSize: number): T[][] {
    let pages: T[][] = [];
    let currentPage: T[] = [];
    for (let item of allItems) {
        currentPage.push(item);
        if (currentPage.length >= pageSize) {
            pages.push(currentPage);
            currentPage = [];
        }
    }
    if (currentPage.length > 0) {
        pages.push(currentPage);
    }
    return pages;
}
