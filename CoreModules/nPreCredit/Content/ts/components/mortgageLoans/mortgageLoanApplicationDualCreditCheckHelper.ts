namespace MortgageLoanApplicationDualCreditCheckSharedNs {
    export const CurrentMortgageLoansListName: string = 'CurrentMortgageLoans'
    export const CurrentOtherLoansListName: string = 'CurrentOtherLoans'
    export const FieldNames: string[] = ['bankName', 'loanTotalAmount', 'loanMonthlyAmount', 'loanShouldBeSettled', 'loanApplicant1IsParty', 'loanApplicant2IsParty']

    export function getDecisionBasisHtmlTemplate(isNew: boolean) {
        return `<div ng-if="$ctrl.m.b">
    <h2 class="custom-header">Decision basis</h2>
    <hr class="hr-section" />

    <div class="row">
        <div class="col-xs-8">
            <div class="editblock">
                <div class="row pb-3">
                    <div class="col-xs-6">
                        <application-editor initial-data="$ctrl.m.b.applicationBasisFields"></application-editor>
                    </div>
                </div>

                <div class="row">
                    <div class="col-xs-6">
                        <h2 class="custom-header text-center">{{$ctrl.m.b.applicant1DetailInfo}}</h2>
                        <hr class="hr-section" />
                    </div>
                    <div class="col-xs-6">
                        <h2 class="custom-header text-center" ng-if="$ctrl.m.b.hasCoApplicant">{{$ctrl.m.b.applicant2DetailInfo}}</h2>
                        <hr ng-if="$ctrl.m.b.hasCoApplicant" class="hr-section" />
                    </div>
                </div>
                <div class="row pb-3">
                    <div class="col-xs-6">
                        <application-editor initial-data="$ctrl.m.b.applicant1BasisFields"></application-editor>
                    </div>
                    <div class="col-xs-6">
                        <application-editor ng-if="$ctrl.m.b.hasCoApplicant" initial-data="$ctrl.m.b.applicant2BasisFields"></application-editor>
                    </div>
                </div>
                <div>
                    <div class="pb-2">
                        <h3>Current mortgage loans</h3>
                        <hr class="hr-section" />

                        <button ng-if="$ctrl.m.b.isEditAllowed" class="n-direct-btn n-green-btn" ng-click="$ctrl.m.b.addExistingLoan(true, $event)" ng-if="$ctrl.m.b.isEditAllowed">Add</button>
                        <hr class="hr-section dotted" />

                        <div ng-repeat="c in $ctrl.m.b.currentMortgageLoans">
                            <div class="row">
                                <div class="col-xs-6">
                                    <application-editor initial-data="c.d"></application-editor>
                                </div>
                                <div class="col-xs-6">
                                    <div class="text-right">
                                        <button ng-if="$ctrl.m.b.isEditAllowed" class="n-icon-btn n-red-btn" ng-click="$ctrl.m.b.deleteExistingLoan(true, c.nr, $event)"><span class="glyphicon glyphicon-minus"></span></button>
                                    </div>
                                </div>
                            </div>
                            <hr class="hr-section dotted" />
                        </div>

                    </div>

                    <div class="pb-2">
                        <h3>Current consumer loans</h3>
                        <hr class="hr-section" />

                        <button ng-if="$ctrl.m.b.isEditAllowed" class="n-direct-btn n-green-btn" ng-click="$ctrl.m.b.addExistingLoan(false, $event)" ng-if="$ctrl.m.b.isEditAllowed">Add</button>
                        <hr class="hr-section dotted" />

                        <div ng-repeat="c in $ctrl.m.b.additionalCurrentOtherLoans">
                            <div class="row">
                                <div class="col-xs-6">
                                    <application-editor initial-data="c.d"></application-editor>
                                </div>
                                <div class="col-xs-6">
                                    <div class="text-right">
                                        <button ng-if="$ctrl.m.b.isEditAllowed" class="n-icon-btn n-red-btn" ng-click="$ctrl.m.b.deleteExistingLoan(false, c.nr, $event)"><span class="glyphicon glyphicon-minus"></span></button>
                                    </div>
                                </div>
                            </div>
                            <hr class="hr-section dotted" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-xs-4">
            <h2 class="custom-header text-center">Other applications</h2>
            <hr class="hr-section" />
            <mortgage-loan-other-connected-applications-compact initial-data="$ctrl.m.b.otherApplicationsData"></mortgage-loan-other-connected-applications-compact>
            <h2 class="custom-header text-center pt-3">Object</h2>
            <hr class="hr-section" />
            <mortgage-loan-dual-collateral-compact initial-data="$ctrl.m.b.objectCollateralData"></mortgage-loan-dual-collateral-compact>
            <h2 class="custom-header text-center pt-3">Other</h2>
            <hr class="hr-section" />
            <mortgage-loan-dual-collateral-compact initial-data="$ctrl.m.b.otherCollateralData"></mortgage-loan-dual-collateral-compact>
            <h2 class="custom-header text-center pt-3">External Credit Report</h2>
            <hr class="hr-section" />
            <list-and-buy-credit-reports-for-customer initial-data="$ctrl.m.customerCreditReports[0]"></list-and-buy-credit-reports-for-customer>            
            <list-and-buy-credit-reports-for-customer ng-if="$ctrl.m.b.hasCoApplicant" initial-data="$ctrl.m.customerCreditReports[1]"></list-and-buy-credit-reports-for-customer>
        </div>
    </div>

</div>`
    }

    export class DecisionBasisModel implements DecisionBasisModelSharedWithLeads {
        constructor(public addExistingLoan: (isMortgageLoan: boolean, evt: Event) => void,
            public deleteExistingLoan: (isMortgageLoan: boolean, nr: number, evt?: Event) => void,
            public additionalCurrentOtherLoans: { d: ApplicationEditorComponentNs.InitialData, nr: number }[],
            public currentMortgageLoans: { d: ApplicationEditorComponentNs.InitialData, nr: number }[],
            public isEditAllowed: boolean
        ) {
        }

        public hasCoApplicant: boolean
        public applicationBasisFields: ApplicationEditorComponentNs.InitialData
        public applicant1BasisFields: ApplicationEditorComponentNs.InitialData
        public applicant2BasisFields: ApplicationEditorComponentNs.InitialData
        public applicant1DetailInfo: string
        public applicant2DetailInfo: string
        public otherApplicationsData: MortgageLoanOtherConnectedApplicationsCompactComponentNs.InitialData
        public objectCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData
        public otherCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData
    }

    export interface DecisionBasisModelSharedWithLeads {
        hasCoApplicant: boolean
        applicationBasisFields: ApplicationEditorComponentNs.InitialData
        applicant1BasisFields: ApplicationEditorComponentNs.InitialData
        applicant2BasisFields: ApplicationEditorComponentNs.InitialData
        applicant1DetailInfo: string
        applicant2DetailInfo: string
        otherApplicationsData: MortgageLoanOtherConnectedApplicationsCompactComponentNs.InitialData
        objectCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData
        otherCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData
    }

    function createLoansInitialData(
        isViewComponent: boolean,
        sourceComponent: NTechComponents.NTechComponentControllerBaseTemplate,
        apiClient: NTechPreCreditApi.ApiClient,
        $q: ng.IQService,
        loansListName: string,
        nr: number,
        aiModel: NTechPreCreditApi.ApplicationInfoModel,
        backTarget: NavigationTargetHelper.CodeOrUrl): ApplicationEditorComponentNs.InitialData {
        let isEditAllowed = !isViewComponent && aiModel.IsActive && !aiModel.IsFinalDecisionMade && !aiModel.HasLockedAgreement

        return ApplicationEditorComponentNs.createInitialData(aiModel.ApplicationNr, aiModel.ApplicationType, backTarget, apiClient, $q, x => {
            for (let fieldName of FieldNames) {
                x.addComplexApplicationListItem(
                    ComplexApplicationListHelper.getDataSourceItemName(loansListName, nr.toString(), fieldName, ComplexApplicationListHelper.RepeatableCode.No), isViewComponent)
            }
        }, {
            isInPlaceEditAllowed: isEditAllowed,
            afterInPlaceEditsCommited: () => {
                //Milder alternative is to reload ltv data. Also do the same in object in this case
                sourceComponent.signalReloadRequired()
            }
        })
    }

    export function initializeSharedDecisionModel(
        m: DecisionBasisModelSharedWithLeads, hasCoApplicant: boolean,
        aiModel: NTechPreCreditApi.ApplicationInfoModel,
        backTarget: NavigationTargetHelper.CodeOrUrl,
        apiClient: NTechPreCreditApi.ApiClient, $q: ng.IQService,
        isInPlaceEditAllowed: boolean, afterInPlaceEditsCommited: () => void,
        backTargetCode: NavigationTargetHelper.NavigationTargetCode | string,
        isReadOnly: boolean,
        applicantDataByApplicantNr: NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string }>) {

        let editorOpts: ApplicationEditorComponentNs.CreateFormOptions = {
            isInPlaceEditAllowed: isInPlaceEditAllowed,
            afterInPlaceEditsCommited: afterInPlaceEditsCommited
        }
        let createEditor = (setup: (i: ApplicationDataSourceHelper.ApplicationDataSourceService) => void) => ApplicationEditorComponentNs.createInitialData(
            aiModel.ApplicationNr, aiModel.ApplicationType, backTarget, apiClient, $q, setup, editorOpts)

        let createApplicantBasisFields = applicantNr => createEditor(x => {
            for (let name of ['hasApprovedCreditCheck', 'isFirstTimeBuyer', 'employment', 'employedSince', 'employer', 'profession', 'employedTo', 'marriage', 'monthlyIncomeSalaryAmount', 'monthlyIncomePensionAmount', 'monthlyIncomeCapitalAmount', 'monthlyIncomeBenefitsAmount', 'monthlyIncomeOtherAmount', 'childrenMinorCount', 'childrenAdultCount', 'costOfLivingRent', 'costOfLivingFees']) {
                x.addDataSourceItem('CreditApplicationItem', `applicant${applicantNr}.${name}`, isReadOnly, true)
            }
            return x
        })

        let applicationBasisFields = createEditor(x => {
            x.addDataSourceItem('CreditApplicationItem', 'application.mortgageLoanApplicationType', true, true)
            x.addDataSourceItem('CreditApplicationItem', 'application.requestedLoanAmount', isReadOnly, true)
            x.addDataSourceItem('CreditApplicationItem', 'application.existingMortgageLoanAmount', isReadOnly, true)
            x.addDataSourceItem('CreditApplicationItem', 'application.leftToLiveOn', isReadOnly, true)
            x.addDataSourceItem('CreditApplicationItem', 'application.requestedDueDay', isReadOnly, true)
            x.addDataSourceItem('CreditApplicationItem', 'application.wasHandledByBroker', isReadOnly, true)
            x.addDataSourceItem('CreditApplicationItem', 'application.requestedContactDateAndTime', isReadOnly, true)
            x.addDataSourceItem('CreditApplicationItem', 'application.ownSavingsAmount', isReadOnly, true)
            x.addDataSourceItem('CreditApplicationItem', 'application.consumerBankAccountIban', isReadOnly, true)
        })
        let applicant1BasisFields = createApplicantBasisFields(1)
        let applicant2BasisFields = aiModel.NrOfApplicants > 1 ? createApplicantBasisFields(2) : null

        let otherApplicationsData: MortgageLoanOtherConnectedApplicationsCompactComponentNs.InitialData = {
            applicationNr: aiModel.ApplicationNr
        }

        let objectCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData = {
            applicationNr: aiModel.ApplicationNr,
            onlyMainCollateral: true,
            onlyOtherCollaterals: false,
            allowDelete: false,
            allowViewDetails: true,
            viewDetailsUrlTargetCode: backTargetCode
        }
        let otherCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData = {
            applicationNr: aiModel.ApplicationNr,
            onlyMainCollateral: false,
            onlyOtherCollaterals: true,
            allowDelete: false,
            allowViewDetails: true,
            viewDetailsUrlTargetCode: backTargetCode
        }

        m.hasCoApplicant = hasCoApplicant
        m.applicationBasisFields = applicationBasisFields
        m.applicant1BasisFields = applicant1BasisFields
        m.applicant2BasisFields = applicant2BasisFields
        m.applicant1DetailInfo = `${applicantDataByApplicantNr[1].firstName}, ${applicantDataByApplicantNr[1].birthDate}`
        m.applicant2DetailInfo = hasCoApplicant ? `${applicantDataByApplicantNr[2].firstName}, ${applicantDataByApplicantNr[2].birthDate}` : ''
        m.otherApplicationsData = otherApplicationsData
        m.otherCollateralData = otherCollateralData
        m.objectCollateralData = objectCollateralData
    }

    export function createDecisionBasisModel(
        isViewComponent: boolean,
        sourceComponent: NTechComponents.NTechComponentControllerBaseTemplate,
        apiClient: NTechPreCreditApi.ApiClient,
        $q: ng.IQService,
        aiModel: NTechPreCreditApi.ApplicationInfoModel,
        hasCoApplicant: boolean,
        isEditAllowed: boolean,
        backTarget: NavigationTargetHelper.CodeOrUrl,
        mortgageLoanNrs: number[],
        otherLoanNrs: number[],
        isFinal: boolean,
        applicantDataByApplicantNr: NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string }>): MortgageLoanApplicationDualCreditCheckSharedNs.DecisionBasisModel {

        let backTargetCode = isFinal
            ? (isViewComponent ? NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckViewFinal : NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckNewFinal)
            : (isViewComponent ? NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckViewInitial : NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckNewInitial)

        let currentMortgageLoans: { d: ApplicationEditorComponentNs.InitialData, nr: number }[] = []
        let additionalCurrentOtherLoans: { d: ApplicationEditorComponentNs.InitialData, nr: number }[] = []

        for (let nr of mortgageLoanNrs) {
            currentMortgageLoans.push({ d: createLoansInitialData(isViewComponent, sourceComponent, apiClient, $q, CurrentMortgageLoansListName, nr, aiModel, backTarget), nr: nr })
        }
        for (let nr of otherLoanNrs) {
            additionalCurrentOtherLoans.push({ d: createLoansInitialData(isViewComponent, sourceComponent, apiClient, $q, CurrentOtherLoansListName, nr, aiModel, backTarget), nr: nr })
        }

        let m = new MortgageLoanApplicationDualCreditCheckSharedNs.DecisionBasisModel((isMortgageLoan: boolean, evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            let currentItems = isMortgageLoan ? currentMortgageLoans : additionalCurrentOtherLoans
            let currentListName = isMortgageLoan ? CurrentMortgageLoansListName : CurrentOtherLoansListName
            let currentMax = 0
            for (let c of currentItems) {
                currentMax = Math.max(c.nr, currentMax)
            }
            let newNr = currentMax + 1
            apiClient.fetchApplicationInfo(aiModel.ApplicationNr).then(ai => {
                let itemName = ComplexApplicationListHelper.getDataSourceItemName(currentListName, newNr.toString(), 'exists', ComplexApplicationListHelper.RepeatableCode.No)
                return apiClient.setApplicationEditItemData(aiModel.ApplicationNr, 'ComplexApplicationList', itemName, 'true', false).then(x => {
                    let nr = currentMax + 1
                    currentItems.push({
                        d: createLoansInitialData(isViewComponent, sourceComponent, apiClient, $q, currentListName, nr, ai, backTarget),
                        nr: nr
                    })
                })
            })
        }, (isMortgageLoan: boolean, nr: number, evt?: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            let currentListName = isMortgageLoan ? CurrentMortgageLoansListName : CurrentOtherLoansListName
            ComplexApplicationListHelper.deleteRow(aiModel.ApplicationNr, currentListName, nr, apiClient).then(x => {
                sourceComponent.signalReloadRequired()
            })
        }, additionalCurrentOtherLoans, currentMortgageLoans, isEditAllowed)

        let isInPlaceEditAllowed = !isViewComponent && aiModel.IsActive && !aiModel.IsFinalDecisionMade && !aiModel.HasLockedAgreement
        initializeSharedDecisionModel(m, hasCoApplicant, aiModel, backTarget, apiClient, $q, isInPlaceEditAllowed, () => {
            sourceComponent.signalReloadRequired()
        }, backTargetCode, isViewComponent, applicantDataByApplicantNr)

        return m
    }

    export function getLtvBasisAndLoanListNrs(sourceComponent: NTechComponents.NTechComponentControllerBaseTemplate, applicationNr: string, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<{
        valuationAmount: { key: number, value: number }[],
        statValuationAmount: { key: number, value: number }[],
        priceAmount: { key: number, value: number }[],
        mortgageLoanNrs: number[],
        otherLoanNrs: number[],
        mortgageLoansToSettleAmount: number,
        otherLoansToSettleAmount: number,
        securityElsewhereAmount: number[],
        housingCompanyLoans: number[]
    }> {
        let listNames: string[] = [MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName, MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName]
        let itemNames: string[] = getItemNamesArr(listNames);

        return apiClient.fetchCreditApplicationItemComplex(applicationNr, itemNames, ApplicationDataSourceHelper.MissingItemReplacementValue).then(x => {
            let mortgageLoansToSettleAmount = 0
            let otherLoansToSettleAmount = 0
            let valuationAmount: { key: number, value: number }[] = []
            let statValuationAmount: { key: number, value: number }[] = []
            let priceAmount: { key: number, value: number }[] = []
            let mortgageLoanNrs: number[] = []
            let otherLoanNrs: number[] = []
            let securityElsewhereAmount: number[] = []
            let housingCompanyLoans: number[] = []
            for (let compoundName of Object.keys(x)) {
                let n = ComplexApplicationListHelper.parseCompoundItemName(compoundName)
                let value = x[compoundName]
                if (n.itemName == 'loanTotalAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                     if (n.listName === MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName) {
                        mortgageLoansToSettleAmount += sourceComponent.parseDecimalOrNull(value)
                    } else if (n.listName === MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName) {
                        otherLoansToSettleAmount += sourceComponent.parseDecimalOrNull(value)
                    }
                } else if (n.itemName == 'valuationAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    valuationAmount.push({ key: sourceComponent.parseDecimalOrNull(n.nr), value: sourceComponent.parseDecimalOrNull(value) });
                } else if (n.itemName == 'statValuationAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    statValuationAmount.push({ key: sourceComponent.parseDecimalOrNull(n.nr), value: sourceComponent.parseDecimalOrNull(value) });
                } else if (n.itemName == 'priceAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    priceAmount.push({ key: sourceComponent.parseDecimalOrNull(n.nr), value: sourceComponent.parseDecimalOrNull(value) });
                } else if (n.itemName == 'securityElsewhereAmount' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    securityElsewhereAmount.push(sourceComponent.parseDecimalOrNull(value))
                } else if (n.itemName == 'housingCompanyLoans' && value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    housingCompanyLoans.push(sourceComponent.parseDecimalOrNull(value))
                } else if (n.itemName === 'exists' && value === 'true') {
                    if (n.listName === MortgageLoanApplicationDualCreditCheckSharedNs.CurrentMortgageLoansListName) {
                        mortgageLoanNrs.push(parseInt(n.nr))
                    } else if (n.listName === MortgageLoanApplicationDualCreditCheckSharedNs.CurrentOtherLoansListName) {
                        otherLoanNrs.push(parseInt(n.nr))
                    }
                }
            }

            return {
                valuationAmount: valuationAmount,
                statValuationAmount: statValuationAmount,
                priceAmount: priceAmount,
                mortgageLoanNrs: mortgageLoanNrs,
                otherLoanNrs: otherLoanNrs,
                mortgageLoansToSettleAmount: mortgageLoansToSettleAmount,
                otherLoansToSettleAmount: otherLoansToSettleAmount,
                securityElsewhereAmount: securityElsewhereAmount,
                housingCompanyLoans: housingCompanyLoans
            }
        })
    }


    function getItemNamesArr(listNames: string[]): string[] {
        let itemNames: string[] = ["ApplicationObject#*#u#valuationAmount"]
        itemNames.push("ApplicationObject#*#u#statValuationAmount");
        itemNames.push("ApplicationObject#*#u#priceAmount");
        itemNames.push("ApplicationObject#*#u#securityElsewhereAmount");
        itemNames.push("ApplicationObject#*#u#housingCompanyLoans");
        for (let listName of listNames) {
            itemNames.push(`${listName}#*#*#loanTotalAmount`)
            itemNames.push(`${listName}#*#*#loanShouldBeSettled`)
            itemNames.push(`${listName}#*#u#exists`)
        }
        return itemNames;
    }


    export function getCustomerCreditHistoryByApplicationNr(applicationNr: string, apiClient: NTechPreCreditApi.ApiClient ) {
        return apiClient.fetchApplicationInfoWithApplicants(applicationNr)
            .then((x) => {
                let customerIds = Object.keys(x.CustomerIdByApplicantNr).map((i) => x.CustomerIdByApplicantNr[i]); 
                return apiClient.fetchCreditHistoryByCustomerId(customerIds)
                    .then(res => {
                        let credits: {CreditNr: string, CapitalBalance: number}[] = [];
                        for (let credit of res["credits"]) {
                            credits.push({
                                CreditNr: credit.CreditNr,
                                CapitalBalance: credit.CapitalBalance
                            })
                        }
                        return credits;
                    })
            })
    }

    export function getApplicantDataByApplicantNr(applicationNr: string, hasCoApplicant: boolean, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string }>> {
        let applicantNrs = (hasCoApplicant ? [1, 2] : [1])
        let names: string[] = []
        for (let applicantNr of applicantNrs) {
            names.push(`a${applicantNr}.firstName`)
            names.push(`a${applicantNr}.birthDate`)
        }
        return apiClient.fetchApplicationDataSourceItems(applicationNr, [{
            DataSourceName: 'CustomerCardItem',
            MissingItemReplacementValue: '-',
            ReplaceIfMissing: true,
            Names: names,
            ErrorIfMissing: false,
            IncludeEditorModel: false,
            IncludeIsChanged: false
        }]).then(x => {
            let result = NTechPreCreditApi.FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items)
            let d: NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string }> = {}
            for (let applicantNr of applicantNrs) {
                d[applicantNr] = {
                    firstName: result[`a${applicantNr}.firstName`],
                    birthDate: result[`a${applicantNr}.birthDate`],
                }
            }
            return d
        })
    }
}