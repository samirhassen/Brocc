module NTechCustomerApi {
    export class ApiClient {
        constructor(private onError: ((errorMessage: string) => void),
            private $http: ng.IHttpService,
            private $q: ng.IQService) {
        }

        private activePostCount: number = 0;
        public loggingContext: string = null;

        private post<TRequest, TResult>(url: string, data: TRequest): ng.IPromise<TResult> {
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
            })
            return d.promise
        }

        public getUserModuleUrl(moduleName: string, serviceLocalUrl: string, parameters?: IStringDictionary<string>): ng.IPromise<{ Url: string, UrlInternal: string, UrlExternal: string }> {
            return this.post('/Api/GetUserModuleUrl', { moduleName: moduleName, moduleLocalUrl: serviceLocalUrl, parameters: parameters })
        }

        isLoading() {
            return this.activePostCount > 0;
        }

        unlockSensitiveItemByName(customerId: number, itemName: string): ng.IPromise<string> {
            return this.post('/Customer/UnlockSensitiveItemByName', { customerId: customerId, itemName: itemName });
        }

        updateCustomer(items: CustomerPropertyModel[], force: boolean): ng.IPromise<string> {
            return this.post('/Customer/UpdateCustomer', { items: items, force: force });
        }

        fetchLegacyCustomerCardUiData(customerId: number, backUrl: string): ng.IPromise<FetchLegacyCustomerCardUiDataResponse> {
            return this.post('/Api/LegacyCustomerCard/FetchUiData', { customerId: customerId, backUrl: backUrl })
        }

        fetchCustomerContactInfo(customerId: number, includeSensitive: boolean, includeCivicRegNr: boolean): ng.IPromise<FetchCustomerContactInfoResponse> {
            return this.post('/Api/ContactInfo/Fetch', { customerId: customerId, includeSensitive: includeSensitive, includeCivicRegNr: includeCivicRegNr })
        }

        fetchCustomerContactInfoEditValueData(customerId: number, name: string): ng.IPromise<FetchCustomerContactInfoEditValueDataResponse> {
            return this.post('/Api/ContactInfo/FetchEditValueData', { customerId: customerId, name: name })
        }

        changeCustomerContactInfoValue(customerId: number, name: string, value: string, includesNewValuesInResponse: boolean): ng.IPromise<ChangeCustomerContactInfoValueResponse> {
            return this.post('/Api/ContactInfo/ChangeValue', { customerId: customerId, name: name, value: value, includesNewValuesInResponse: includesNewValuesInResponse })
        }

        kycManagementFetchLocalDecisionData(customerId: number): ng.IPromise<KycLocalDecisionCurrentModel> {
            return this.post('/Api/KycManagement/FetchLocalDecisionCurrentData', { customerId: customerId })
        }

        kycManagementFetchLocalDecisionHistoryData(customerId: number, isModellingPep: boolean): ng.IPromise<KycLocalDecisionHistoryModel> {
            return this.post('/Api/KycManagement/FetchLocalDecisionHistoryData', { customerId: customerId, isModellingPep: isModellingPep })
        }

        kycManagementSetLocalDecision(customerId: number, isModellingPep: boolean, currentValue: boolean, includeNewCurrentData: boolean): ng.IPromise<SetLocalDecisionResponse> {
            return this.post('/Api/KycManagement/SetLocalDecision', { customerId: customerId, isModellingPep: isModellingPep, currentValue: currentValue, includeNewCurrentData: includeNewCurrentData })
        }

        kycManagementFetchLatestCustomerQuestionsSet(customerId: number): ng.IPromise<CustomerQuestionsSet> {
            return this.post('/Api/KycManagement/FetchLatestCustomerQuestionsSet', { customerId: customerId })
        }

        kycManagementQueryDetails(queryId: number): ng.IPromise<{ PepExternalIds: string[], SactionExternalIds: string[] }> {
            return this.post('/Api/KycScreening/QueryResultDetails', { queryId });
        }

        fetchLatestTrapetsQueryResult(customerId: number): ng.IPromise<TrapetsQueryResultModel> {
            return this.post('/Api/KycManagement/FetchLatestTrapetsQueryResult', { customerId: customerId })
        }

        fetchTrapetsQueryHistorySummary(customerId: number): ng.IPromise<TrapetsQueryHistorySummaryModel> {
            return this.post('/Api/KycManagement/FetchTrapetsQueryHistorySummary', { customerId: customerId })
        }

        fetchQueryResultHistoryDetails(customerId: number, historyDayCount: number): ng.IPromise<{
            QueryDatesWithListHits: {
                QueryDate: string
                PepExternalIds: string[]
                SanctionExternalIds: string[]
            }[]
        }> {
            return this.post('/Api/KycScreening/QueryResultHistoryDetails', {
                customerId, historyDayCount
            });
        }

        fetchCustomerPropertiesWithGroupedEditHistory(customerId: number, propertyNames: string[]): ng.IPromise<FetchPropertiesWithGroupedEditHistoryResult> {
            return this.post('/Api/KycManagement/FetchPropertiesWithGroupedEditHistory', { customerId: customerId, propertyNames: propertyNames })
        }

        fetchCustomerItemsDict(customerId: number, itemNames: string[]): ng.IPromise<{ [index: string]: string }> {
            let deferred = this.$q.defer<{ [index: string]: string }>()
            this.post('/Customer/BulkFetchPropertiesByCustomerIds', {
                propertyNames: itemNames,
                customerIds: [customerId]
            }).then((result: any) => {
                let r: { [index: string]: string } = {}
                if (result.customers && result.customers.length > 0 && result.customers[0].Properties) {
                    for (let p of result.customers[0].Properties) {
                        r[p.Name] = p.Value
                    }
                    deferred.resolve(r)
                }
            })
            return deferred.promise;
        }

        fetchCustomerItems(customerId: number, itemNames: string[]): ng.IPromise<CustomerItem[]> {
            let deferred = this.$q.defer<CustomerItem[]>()
            this.post('/Customer/BulkFetchPropertiesByCustomerIds', {
                propertyNames: itemNames,
                customerIds: [customerId]
            }).then((result: any) => {
                let items: CustomerItem[] = []
                if (result.customers && result.customers.length > 0 && result.customers[0].Properties) {
                    for (let p of result.customers[0].Properties) {
                        items.push({ name: p.Name, value: p.Value })
                    }
                    deferred.resolve(items)
                }
            })
            return deferred.promise;
        }

        fetchCustomerComments(customerId: number): ng.IPromise<CustomerComment[]> {
            return this.post('/Api/CustomerComments/FetchAllForCustomer', { customerId: customerId })
        }

        addCustomerComment(customerId: number, newCommentText: string, attachedFileAsDataUrl: string, attachedFileName: string): ng.IPromise<CustomerComment> {
            return this.post('/Api/CustomerComments/Add', { customerId: customerId, newCommentText: newCommentText, attachedFileAsDataUrl: attachedFileAsDataUrl, attachedFileName: attachedFileName })
        }

        fetchCustomerRelations(customerId: number): ng.IPromise<FetchCustomerRelationsResult> {
            return this.post('/Api/CustomerRelations/FetchForCustomer', { customerId: customerId })
        }

        getCustomerIdsByCivicRegNrs(civicRegNrs: string): ng.IPromise<{ CustomerId: number }> {
            return this.post('/Api/CustomerIdByCivicRegNr', { civicRegNr: civicRegNrs })
        }

        CreateOrUpdateCustomer(CivicRegNr: string, EventType: string, items: PropertyModel[]): ng.IPromise<{ CustomerId: number }> {
            return this.post('/Api/PersonCustomer/CreateOrUpdate', { CivicRegNr: CivicRegNr, EventType: EventType, Properties: items })
        }

        parseCivicRegNr(civicRegNr: string): ng.IPromise<{ NormalizedValue: string, CrossCountryStorageValue: string, Country: string, BirthDate: Date, IsMale: boolean }> {
            return this.post('/Api/ParseCivicRegNr', { civicRegNr: civicRegNr })
        }

        findCustomerChannels(searchText: string,
            searchType: FindCustomerChannelsSearchType = FindCustomerChannelsSearchType.Omni,
            includeGeneralChannel: boolean = false): ng.IPromise<{ CustomerChannels: CustomerChannelModel[] }> {
            return this.post('/api/CustomerMessage/FindCustomerChannels', {
                SearchText: searchText,
                SearchType: searchType.toString(),
                includeGeneralChannel: includeGeneralChannel
            })
        }

        private bulkFetchPropertiesByCustomerIds(customerIds: number[], itemNames: string[]): ng.IPromise<{
            customers: {
                CustomerId: number
                Properties: { Name: string, Value: string }[]
            }[]
        }> {
            return this.post('/Customer/BulkFetchPropertiesByCustomerIds', {
                customerIds: customerIds,
                propertyNames: itemNames
            })
        }

        fetchCustomerItemsBulk(customerIds: number[], itemNames: string[]): ng.IPromise<INumberDictionary<IStringDictionary<string>>> {
            return this.bulkFetchPropertiesByCustomerIds(customerIds, itemNames).then(x => {
                let r: INumberDictionary<IStringDictionary<string>> = {}
                if (x.customers) {
                    for (let c of x.customers) {
                        let cd: IStringDictionary<string> = {}
                        for (let p of c.Properties) {
                            cd[p.Name] = p.Value
                        }
                        r[c.CustomerId] = cd
                    }
                }
                return r
            })
        }

        getSecureCustomerMessages(request: {
            CustomerId?: number
            ChannelType?: string
            ChannelId?: string
            IncludeMessageTexts?: boolean
            SkipCount?: number
            TakeCount?: number
            IncludeChannels?: boolean
            IsHandled?: boolean
            IsFromCustomer?: boolean
        }): ng.IPromise<{ TotalMessageCount: number, AreMessageTextsIncluded: boolean, Messages: CustomerMessageModel[], Channels: CustomerChannelModel[] }> {
            return this.post('/api/CustomerMessage/GetMessages', request)
        }

        GetCustomerMessagesByChannel(request: {
            CustomerId?: number
            ChannelType?: string
            ChannelId?: string
            IncludeMessageTexts?: boolean
            SkipCount?: number
            TakeCount?: number
            IncludeChannels?: boolean
            IsHandled?: boolean
            IsFromCustomer?: boolean
        }): ng.IPromise<{ TotalMessageCount: number, AreMessageTextsIncluded: boolean, GroupedMessages: CustomerMessagesGroupedByChannelTypeChannelId[], Channels: CustomerChannelModel[] }> {
            return this.post('/api/CustomerMessage/GetCustomerMessagesByChannel', request)
        }

        createSecureCustomerMessage(request: { CustomerId: number, ChannelType: string, ChannelId: string, Text: string, IsFromCustomer: boolean, FlagPreviousMessagesAsHandled?: boolean, NotifyCustomerByEmail?: boolean }): ng.IPromise<{ CreatedMessage: CustomerMessageModel, WasNotificationEmailSent: boolean }> {
            return this.post('/api/CustomerMessage/CreateMessage', request)
        }

        attachMessageDocument(request: { MessageId: number, AttachedFileAsDataUrl: string, AttachedFileName: string })
            : ng.IPromise<{ Id: number }> {
            return this.post('/api/CustomerMessage/attachMessageDocument', request)
        }

        handleMessages(request: { MessageIds: number[] })
            : ng.IPromise<{ Status: string }> {
            return this.post('/api/CustomerMessage/HandleMessages', request)
        }

        wipeCustomerContactInfo(customerIds: number[]) {
            return this.post('/api/TestSupport/Wipe-Customer-ContactInfo', { CustomerIds: customerIds })
        }
    }

    export interface CustomerMessageModel {
        Id: number
        Text: string
        CustomerId: number
        IsFromCustomer: boolean
        CreationDate: Date
        CreatedByUserId: number
        HandledDate: Date
        HandledByUserId: number
        ChannelType: string
        ChannelId: string
        Messages: CustomerMessageAttachedDocumentModel[]
    }
    export interface CustomerMessageAttachedDocumentModel {
        Id: number
        CustomerMessageId: number
        FileName: string
        ArchiveKey: string
        ContentTypeMimetype: string
    }
    export interface CustomerMessagesGroupedByChannelTypeChannelId {
        CreationDate: Date
        ChannelType: string
        ChannelId: string
        CustomerId: number
    }

    export interface CustomerChannelModel {
        CustomerId: number
        ChannelType: string
        ChannelId: string
        IsRelation: boolean
        RelationStartDate: Date
        RelationEndDate: Date
    }

    enum FindCustomerChannelsSearchType {
        Omni = "Omni",
        Email = "Email",
        CustomerName = "CustomerName",
        OrgOrCivicRegNr = "OrgOrCivicRegNr",
        RelationId = "RelationId"
    }

    export class PropertyModel {
        Value: string
        Name: string
        ForceUpdate: boolean
    }
    export interface FetchCustomerRelationsResult {
        CustomerRelations: CustomerRelationModel[]
    }

    export interface CustomerRelationModel {
        CustomerId: number;
        StartDate: NTechDates.DateOnly;
        EndDate: NTechDates.DateOnly;
        RelationId: string;
        RelationType: string;
        RelationNavigationUrl: string;
    }

    export interface FetchPropertiesWithGroupedEditHistoryResult {
        CurrentValues: { [index: string]: string }
        HistoryItems: HistoryItem[]
    }

    export interface HistoryItem {
        UserId: number
        UserDisplayName: string
        EditDate: Date
        Values: { [index: string]: string }
    }

    export class CustomerComment {
        Id: number;
        CommentDate: Date;
        CommentText: string;
        AttachmentFilename: string;
        AttachmentUrl: string;
        CommentByName: string;
        DirectUrlShortName: string;
        DirectUrl: string;
    }

    export class CustomerItem {
        public name: string
        public value: string
    }

    export class TrapetsQueryHistorySummaryModel {
        CustomerId: number
        PepItems: TrapetsQueryHistorySummaryItem[]
        SanctionItems: TrapetsQueryHistorySummaryItem[]
    }

    export class TrapetsQueryHistorySummaryItem {
        FromDate: Date
        ToDate: Date
        Count: number
        Value: boolean
    }

    export class TrapetsQueryResultModel {
        Id: number
        CustomerId: number
        QueryDate: Date
        IsPepHit: boolean
        IsSanctionHit: boolean
    }

    export class CustomerQuestionsSet {
        AnswerDate: Date
        Source: string
        CustomerId: number
        Items: CustomerQuestionsSetItem[]
    }

    export class CustomerQuestionsSetItem {
        QuestionGroup: string
        QuestionCode: string
        AnswerCode: string
        QuestionText: string
        AnswerText: string
    }

    export class KycLocalDecisionCurrentModel {
        CustomerId: number
        IsPep: boolean
        IsSanction: boolean
        AmlRiskClass: string
    }

    export class KycLocalDecisionHistoryModel {
        CustomerId: number
        IsModellingPep: boolean
        CurrentValue: KycLocalDecisionHistoryItem
        HistoricalValues: KycLocalDecisionHistoryItem[]
    }

    export class KycLocalDecisionHistoryItem {
        CustomerId: number
        IsModellingPep: boolean
        ChangeDate: Date
        ChangedByUserId: number
        ChangedByUserDisplayName: string
        Value: boolean
    }

    export class SetLocalDecisionResponse {
        NewCurrentData: KycLocalDecisionCurrentModel
    }

    export class ChangeCustomerContactInfoValueResponse {
        currentValue: ICustomerPropertyModelExtended
        historicalValues: ICustomerPropertyModelExtended[]
    }

    export class FetchCustomerContactInfoEditValueDataResponse {
        customerId: number
        name: string
        templateName: string
        currentValue: ICustomerPropertyModelExtended
        historicalValues: ICustomerPropertyModelExtended[]
    }

    export interface ICustomerPropertyModelExtended {
        Id: number
        ChangeDate: Date
        ChangedById: number
        ChangedByDisplayName: string
        Name: string
        Group: string
        CustomerId: number
        Value: string
        IsSensitive: boolean
    }

    export class FetchCustomerContactInfoResponse {
        customerId: boolean
        isCompany: string
        companyName: string
        firstName: string
        lastName: string
        birthDate: Date
        civicRegNr: string
        orgnr: string
        addressStreet: string
        addressZipcode: string
        addressCity: string
        addressCountry: string
        email: string
        phone: string
        sensitiveItems: string[]
        includeSensitive: boolean
        includeCivicRegNr: boolean
    }

    export class FetchLegacyCustomerCardUiDataResponse {
        //TODO: Move all the model interface to this namespace after refactoring is done
        customerId: number
        customerCardItems: CustomerCardNs.ICustomerPropertyEditModel[]
    }

    export class CustomerPropertyModel {
        Name: string
        Group: string
        CustomerId: number
        Value: string
        IsSensitive: boolean
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