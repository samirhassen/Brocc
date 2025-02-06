class ErrorListCtrl {
    static $inject = ['$scope', '$http', '$q']
    constructor(
        private $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        window.scope = this;
        this.backUrl = initialData.backUrl;
        this.fetchUnhandledItems(null);
        this.lastHistoryPageNr = null;
        this.userDisplayNameByUserId = initialData.userDisplayNameByUserId;
    }

    isLoading: boolean
    backUrl: string
    unhandled: ErrorListNs.IFetchItemsResult

    userDisplayNameByUserId: ErrorListNs.NumberKeyedDictionary<string>
    
    isHistoryVisible: boolean
    loadedHistoryItems: Array<ErrorListNs.IEInvoiceActionItem>
    lastHistoryPageNr: number
    isAllHistoryLoaded: boolean
    
    getUnhandledCount() {
        let count = 0;
        if (this.unhandled && this.unhandled.pageItems) {
            for (var i of this.unhandled.pageItems) {
                if (!i.IsHandledLocally) {
                    count = count + 1;
                }
            }
        }
        return count;
    }

    getUserDisplayNameByUserId(userId: number) {
        let v = this.userDisplayNameByUserId[userId];
        if (v) {
            return v
        } else {
            return 'User ' + userId
        }
    }

    fetchUnhandledItems(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        this.fetchErrorListActionItems(false, 0, 100, false, r => {
            this.unhandled = r
        });
    }

    toggleHistory(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        if (this.lastHistoryPageNr === null) {
            this.fetchHistoryPage(0, null, () => {
                this.isHistoryVisible = true
            })
        } else {
            this.isHistoryVisible = !this.isHistoryVisible
        }
    }

    fetchHistoryPage(pageNr: number, evt: Event, onSuccess: () => void) {
        if (evt) {
            evt.preventDefault()
        }
        this.fetchErrorListActionItems(true, pageNr, 30, true, r => {
            if (pageNr == 0) {
                this.loadedHistoryItems = r.pageItems
            } else {
                for (var x of r.pageItems) {
                    this.loadedHistoryItems.push(x)
                }
            }
            this.lastHistoryPageNr = pageNr
            this.isAllHistoryLoaded = this.loadedHistoryItems.length >= r.totalCount
            if (onSuccess) {
                onSuccess()
            }
        })
    }

    fetchErrorListActionItems(isHandled: boolean, pageNr: number, pageSize: number, isOrderedByHandledDate: boolean, onSuccess: (result: ErrorListNs.IFetchItemsResult) => void) {
        this.isLoading = true
        this.$http({
            method: 'POST',
            url: initialData.fetchErrorListActionItemsUrl,
            data: {
                isHandled: isHandled,
                pageNr: pageNr,
                pageSize: pageSize,
                isOrderedByHandledDate: isOrderedByHandledDate
            }
        }).then((response: ng.IHttpResponse<ErrorListNs.IFetchItemsResult>) => {
            this.isLoading = false
            onSuccess(response.data)
        }, (response) => {
            this.isLoading = false
            toastr.error(response.statusText)
        })
    }

    markActionAsHandled(item: ErrorListNs.IEInvoiceActionItem, evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true
        this.$http({
            method: 'POST',
            url: initialData.markActionAsHandledUrl,
            data: {
                actionId: item.Id
            }
        }).then((response) => {
            this.isLoading = false
            item.IsHandledLocally = true

            this.isHistoryVisible = false
            this.loadedHistoryItems = null
            this.lastHistoryPageNr = null
            this.isAllHistoryLoaded = false
        }, (response) => {
            this.isLoading = false
            toastr.error(response.statusText)
        })
    }

    unlockHistoryItem(item: ErrorListNs.IEInvoiceActionItem, evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        this.isLoading = true
        this.$http({
            method: 'POST',
            url: initialData.fetchActionDetailsUrl,
            data: { actionId: item.Id }
        }).then((response: ng.IHttpResponse<ErrorListNs.IActionItemDetails>) => {
            item.ItemDetails = response.data
            this.isLoading = false
        }, (response) => {
            this.isLoading = false
            toastr.error('Failed!')
        })
    }
}

var app = angular.module('app', ['ntech.forms', 'ntech.components'])
app.controller('errorListCtrl', ErrorListCtrl)

module ErrorListNs {
    export interface IFetchItemsResult {
        pageItems: Array<IEInvoiceActionItem>,
        totalCount: number
    }

    export interface IEInvoiceActionItem {
        Id: number,
        ActionName: string,
        ActionMessage: string,
        ActionDate: Date,
        CreatedByUserId: number,
        CreatedByUserDisplayName: string,
        HandledByUserId: number,
        HandledByUserDisplayName: string,
        HandledDate: Date,
        CreditNr: string,
        EInvoiceFiMessageHeaderId: number,
        ItemDetails: IActionItemDetails,
        IsHandledLocally: boolean
    }

    export interface IActionItemDetails {
        ActionId: number;
        ActionMessage: string;
        HasEInvoiceMessage: boolean;
        EInvoiceMessageItems: Array<IActionItemDetailsItem>;
        HasConnectedBusinessEvent: boolean;
        ConnectedBusinessEventType: string;
        ConnectedBusinessEventItems: Array<IActionItemDetailsItem>;
        IsHidden: boolean;
    }

    export interface IActionItemDetailsItem {
        Name: string;
        Value: string;
    }

    export interface NumberKeyedDictionary<T> {
        [Key: number]: T;
    }
}