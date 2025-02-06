module NTechSavingsApi {
    export class ApiClient {
        constructor(private onError: ((errorMessage: string) => void),
            private $http: ng.IHttpService,
            private $q: ng.IQService) {
        }

        private activePostCount: number = 0;
        public loggingContext: string = null;

        private post<TRequest, TResult>(url: string, data: TRequest): ng.IPromise<TResult> {
            let startTimeMs = performance.now();
            this.activePostCount++;
            let d: ng.IDeferred<TResult> = this.$q.defer()
            this.$http.post(url, data).then((result: ng.IHttpResponse<TResult>) => {
                d.resolve(result.data)
            }, err => {
                if (this.onError) {
                    this.onError(err.statusText)
                }
                d.reject(err.statusText)
            }).finally(() => {
                this.activePostCount--;
                let totalTimeMs = performance.now() - startTimeMs;
                let c = this.loggingContext == null ? '' : (this.loggingContext + ': ')
                console.log(`${c}post - ${url}: ${totalTimeMs}ms`);
            })
            return d.promise
        }

        public postUsingApiGateway<TRequest, TResult>(seviceName: string, serviceLocalUrl: string, data: TRequest): ng.IPromise<TResult> {
            return this.post<TRequest, TResult>(`/Api/Gateway/${seviceName}${serviceLocalUrl[0] === '/' ? '' : '/'}${serviceLocalUrl}`, data)
        }

        isLoading() {
            return this.activePostCount > 0;
        }

        keyValueStoreGet(key: string, keySpace: string): ng.IPromise<KeyValueStoreGetResult> {
            return this.post('/api/KeyValueStore/Get', {
                "Key": key,
                "KeySpace": keySpace
            });
        }

        keyValueStoreRemove(key: string, keySpace: string): ng.IPromise<void> {
            return this.post('/api/KeyValueStore/Remove', {
                "Key": key,
                "KeySpace": keySpace
            });
        }

        keyValueStoreSet(key: string, keySpace: string, value: string): ng.IPromise<void> {
            return this.post('/api/KeyValueStore/Set', {
                "Key": key,
                "KeySpace": keySpace,
                "Value": value
            });
        }

        fetchUserNameByUserId(userId: number): ng.IPromise<FetchUserNameByUserIdResult> {
            return this.post('/api/UserName/ByUserId', { UserId: userId })
        }

        fetchFatcaExportFiles(pageSize?: number, pageNr?: number): ng.IPromise<FetchFatcaExportFilesResult> {
            return this.post('/api/Fatca/FetchExportFiles', {
                PageSize: pageSize,
                PageNr: pageNr
            })
        }

        createFatcaExportFile(year: number, exportProfile?: string): ng.IPromise<CreateFatcaExportFileResult> {
            return this.post('/api/Fatca/CreateExportFile', {
                Year: year,
                ExportProfile: exportProfile
            })
        }

        getCustomerMessagesTexts(messageIds: number[]): ng.IPromise<{ MessageTextByMessageId: INumberDictionary<string>, MessageTextFormat: INumberDictionary<string>, IsFromCustomerByMessageId: INumberDictionary<boolean>, AttachedDocumentsByMessageId: INumberDictionary<string> }> {
            return this.postUsingApiGateway('nCustomer', 'api/CustomerMessage/GetMessageTexts', {
                MessageIds: messageIds
            })
        }

        createAndDeliverFinnishCustomsAccountsExportFile(skipDeliver: boolean): ng.IPromise<{}> {
            return this.post('/api/FinnishCustomsAccounts/CreateExportFile', { skipDeliver: skipDeliver })
        }

        fetchFinnishCustomsAccountsExportFiles(pageSize: number, pageNr: number): ng.IPromise<{
            TotalPageCount: number,
            PageExports: {
                Id: number
                TransactionDate: Date
                ArchiveKey: string
                ArchiveDocumentUrl: string
                UserId: number
                UserDisplayName: string
                ExportResultStatus: string
            }[]
        }> {
            return this.post('/api/FinnishCustomsAccounts/FetchExportFiles', { pageSize, pageNr })
        }
    }

    export interface CreateFatcaExportFileResult {
        FileArchiveKey: string
        ExportResult: ExportFileResultModel
    }

    export interface ExportFileResultModel {
        SuccessProfileNames: string[]
        FailedProfileNames: string[]
        TimeInMs: number
        Errors: string[]
    }

    export interface FetchFatcaExportFilesResult {
        Files: FatcaExportFileModel[]
    }

    export interface FatcaExportFileModel {
        Id: number
        TransactionDate: Date
        ForYearDate: Date
        NrOfAccounts: number
        ArchiveKey: string
        ArchiveDocumentUrl: string
        UserId: number
        UserDisplayName: string
        ExportResult: ExportFileResultModel
    }

    export interface KeyValueStoreGetResult {
        Value: string
    }

    export interface FetchUserNameByUserIdResult {
        UserName: string
    }

    export interface IStringStringDictionary {
        [key: string]: string
    }

    export interface IStringDictionary<T> {
        [key: string]: T
    }

    export interface INumberDictionary<T> {
        [key: number]: T
    }
}