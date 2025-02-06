namespace CompanyLoanInitialCreditCheckNewComponentNs {

    export class CompanyLoanInitialCreditCheckNewController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope', '$timeout']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: IScope) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'companyLoanInitialCreditCheckNew'
        }

        onBack = (evt) => {
            if (evt) {
                evt.preventDefault()
            }
            let target = this.initialData.backTarget 
                ? NavigationTargetHelper.createCodeTarget(this.initialData.backTarget)
                : NavigationTargetHelper.createCrossModule('CompanyLoanApplication', { applicationNr: initialData.applicationNr })            
            
            NavigationTargetHelper.handleBack(target, this.apiClient, this.$q, { applicationNr: initialData.applicationNr })
        }

        private getRejectionReasonsFromScoringResult(result: NTechCompanyLoanPreCreditApi.InitialCreditCheckResponse, onMissing: ((n: string) => void)): string[] {
            let reasons: string[] = null
            if (!result.WasAccepted && result.RejectionRuleNames && result.RejectionRuleNames.length > 0) {
                reasons = []
                for (let rejectionRuleName of result.RejectionRuleNames) {
                    let reasonName = this.initialData.rejectionRuleToReasonNameMapping[rejectionRuleName]                    
                    if (reasonName) {
                        reasons.push(reasonName)
                    } else {
                        if (onMissing) {
                            onMissing(rejectionRuleName)
                        }
                    }
                }
            }
            return reasons
        }

        private createRejectModelFromScoringResult(result: NTechCompanyLoanPreCreditApi.InitialCreditCheckResponse): RejectModel {
            let r: RejectModel = {
                otherReason: '',
                reasons: {},
                rejectModelCheckboxesCol1: [],
                rejectModelCheckboxesCol2: [],
                initialReasons: this.getRejectionReasonsFromScoringResult(result, x => toastr.warning('Unmapped rejection rule: ' + x + '. Check the rejection reasons by hand!'))
            }
            
            for (let reasonName of Object.keys(this.initialData.rejectionReasonToDisplayNameMapping)) {
                let displayName = this.initialData.rejectionReasonToDisplayNameMapping[reasonName]
                if (r.rejectModelCheckboxesCol1.length > r.rejectModelCheckboxesCol2.length) {
                    r.rejectModelCheckboxesCol2.push(new RejectionCheckboxModel(reasonName, displayName))
                } else {
                    r.rejectModelCheckboxesCol1.push(new RejectionCheckboxModel(reasonName, displayName))
                }
            }

            if (r.initialReasons) {
                for (let reasonName of r.initialReasons) {
                    r.reasons[reasonName] = true
                }
            }

            return r
        }

        init(applicationNr: string, result: NTechCompanyLoanPreCreditApi.InitialCreditCheckResponse) {
            let setModel = (offer: EditOfferModel) => {
                this.m = {
                    applicationNr: applicationNr,
                    recommendationServerKey: result.TempCopyStorageKey,
                    mode: result.Offer ? 'acceptNewLoan' : 'reject',
                    acceptModel: {
                        offer: offer,
                        isPendingValidation: false,
                        validationResult: {
                            handledAcceptedOverLimit: false,
                            isAllowedToOverrideHandlerLimit: false,
                            isOverHandlerLimit: false
                        }
                    },
                    rejectModel: this.createRejectModelFromScoringResult(result),
                    recommendationInitialData: {
                        applicationNr: applicationNr,
                        recommendation: result,
                        apiClient: this.initialData.apiClient,
                        companyLoanApiClient: this.initialData.companyLoanApiClient,
                        creditUrlPattern: this.initialData.creditUrlPattern,
                        isTest: this.initialData.isTest,
                        rejectionReasonToDisplayNameMapping: this.initialData.rejectionReasonToDisplayNameMapping,
                        rejectionRuleToReasonNameMapping: this.initialData.rejectionRuleToReasonNameMapping,
                        isEditAllowed: true,
                        navigationTargetToHere: NTechNavigationTarget.createCrossModuleNavigationTargetCode("CompanyLoanNewCreditCheck", { applicationNr: applicationNr })
                    }
                }
            }

            if (result.Offer) {
                setModel(this.createAcceptModelFromOffer(result.Offer))
            } else {
                this.apiClient.fetchCurrentReferenceInterestRate().then(referenceInterestRate => {
                    setModel({
                        initialFeeAmount: '',
                        loanAmount: '',
                        monthlyFeeAmount: '',
                        nominalInterestRatePercent: '',
                        referenceInterestRatePercent: this.formatNumberForEdit(referenceInterestRate),
                        repaymentTimeInMonths: ''
                    })
                })
            }
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.initialData.companyLoanApiClient.initialCreditCheck(this.initialData.applicationNr, true).then(x => {
                this.init(this.initialData.applicationNr, x)
            })
        }

        private createAcceptModelFromOffer(offer: NTechCompanyLoanPreCreditApi.CompanyLoanOfferModel): EditOfferModel {
            return {
                loanAmount: this.formatNumberForEdit(offer.LoanAmount),
                initialFeeAmount: this.formatNumberForEdit(offer.InitialFeeAmount),
                monthlyFeeAmount: this.formatNumberForEdit(offer.MonthlyFeeAmount),
                nominalInterestRatePercent: this.formatNumberForEdit(offer.NominalInterestRatePercent),
                repaymentTimeInMonths: this.formatNumberForEdit(offer.RepaymentTimeInMonths),
                referenceInterestRatePercent: this.formatNumberForEdit(offer.ReferenceInterestRatePercent)
            }
        }

        wasAcceptedRecommendationChanged(): boolean {
            if (!this.m.recommendationInitialData.recommendation.WasAccepted) {
                return true
            }
            let initial = this.createAcceptModelFromOffer(this.m.recommendationInitialData.recommendation.Offer)
            let current = this.m.acceptModel.offer
            return (JSON.stringify(initial) !== JSON.stringify(current))
        }

        onAcceptModelChanged() {
            if (!this.m || !this.$scope.acceptform) {
                return
            }
            if (this.$scope.acceptform.$invalid) {
                return
            }

            if (this.wasAcceptedRecommendationChanged()) {
                this.m.acceptModel.isPendingValidation = false
                this.m.acceptModel.validationResult = {
                    handledAcceptedOverLimit: false,
                    isAllowedToOverrideHandlerLimit: false,
                    isOverHandlerLimit: false
                }
            } else {
                this.m.acceptModel.isPendingValidation = true
                this.m.acceptModel.validationResult = null                
                this.initialData.apiClient.checkIfOverHandlerLimit(this.m.applicationNr, this.parseDecimalOrNull(this.m.acceptModel.offer.loanAmount), true).then(result => {
                    this.m.acceptModel.isPendingValidation = false
                    this.m.acceptModel.validationResult = {
                        handledAcceptedOverLimit: false,
                        isAllowedToOverrideHandlerLimit: result.isAllowedToOverrideHandlerLimit,
                        isOverHandlerLimit: result.isOverHandlerLimit
                    }
                })
            }
        }

        acceptNewLoan(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (this.$scope.acceptform.$invalid) {
                return
            }

            let offerRaw = this.m.acceptModel.offer
            let offer = {
                LoanAmount: this.parseDecimalOrNull(offerRaw.loanAmount),
                AnnuityAmount: null,
                InitialFeeAmount: this.parseDecimalOrNull(offerRaw.initialFeeAmount),
                MonthlyFeeAmount: this.parseDecimalOrNull(offerRaw.monthlyFeeAmount),
                NominalInterestRatePercent: this.parseDecimalOrNull(offerRaw.nominalInterestRatePercent),
                ReferenceInterestRatePercent: this.parseDecimalOrNull(offerRaw.referenceInterestRatePercent),
                RepaymentTimeInMonths: this.parseDecimalOrNull(offerRaw.repaymentTimeInMonths)
            }
            this.initialData.companyLoanApiClient.commitInitialCreditCheckDecisionAccept(
                this.m.applicationNr,
                this.m.recommendationServerKey,
                offer).then(x => {
                    this.onBack(null)
                })
        }

        totalInterestRatePercent(): number {
            if (!this.m || !this.m.acceptModel || !this.m.acceptModel.offer) {
                return null
            }
            return this.parseDecimalOrNull(this.m.acceptModel.offer.nominalInterestRatePercent) + this.parseDecimalOrNull(this.m.acceptModel.offer.referenceInterestRatePercent)
        }

        reject(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            let reasons: string[] = null
            reasons = this.getRejectionReasons()
  
            this.initialData.companyLoanApiClient.commitInitialCreditCheckDecisionReject(
                this.m.applicationNr,
                this.m.recommendationServerKey,
                reasons).then(x => {
                    this.onBack(null)
                })
        }

        setAcceptRejectMode(mode: string, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (this.m) {
                this.m.mode = mode
            }
        }

        getRejectionReasons() {
            if (!this.m || !this.m.rejectModel) {
                return null
            }

            var reasons: string[] = []
            for (let key of Object.keys(this.m.rejectModel.reasons)) {
                if (this.m.rejectModel.reasons[key] === true) {
                    reasons.push(key)
                }
            }
            if (!this.isNullOrWhitespace(this.m.rejectModel.otherReason)) {
                reasons.push('other: ' + this.m.rejectModel.otherReason)
            }

            return reasons
        }

        anyRejectionReasonGiven() {
            var reasons = this.getRejectionReasons()
            return reasons && reasons.length > 0
        }
    }

    export class CompanyLoanInitialCreditCheckNewComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanInitialCreditCheckNewController;
            this.templateUrl = 'company-loan-initial-credit-check-new.html';
        }
    }

    export interface LocalInitialData {
        applicationNr: string
        rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>
        rejectionRuleToReasonNameMapping: NTechPreCreditApi.IStringDictionary<string>
        creditUrlPattern: string
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {

    }

    export class Model {
        applicationNr: string
        recommendationServerKey: string
        mode: string
        acceptModel: AcceptNewLoanModel
        rejectModel: RejectModel
        recommendationInitialData: CompanyLoanInitialCreditCheckRecommendationComponentNs.InitialData
    }

    export class EditOfferModel {
        loanAmount: string
        repaymentTimeInMonths: string
        initialFeeAmount: string
        monthlyFeeAmount: string
        nominalInterestRatePercent: string
        referenceInterestRatePercent: string
    }

    export class AcceptNewLoanModel {
        offer: EditOfferModel
        isPendingValidation: boolean
        validationResult: AcceptNewLoanModelValidationModel
    }

    export class RejectModel {
        rejectModelCheckboxesCol1: RejectionCheckboxModel[]
        rejectModelCheckboxesCol2: RejectionCheckboxModel[]
        reasons: NTechPreCreditApi.IStringDictionary<boolean>
        otherReason: string
        initialReasons: string[]
    }

    export class AcceptNewLoanModelValidationModel {
        isOverHandlerLimit: boolean
        isAllowedToOverrideHandlerLimit: boolean
        handledAcceptedOverLimit: boolean
    }

    export interface IScope extends ng.IScope {
        acceptform: ng.IFormController
    }

    export class RejectionCheckboxModel {
        constructor(public reason: string, public displayName: string) {

        }
    }
}

angular.module('ntech.components').component('companyLoanInitialCreditCheckNew', new CompanyLoanInitialCreditCheckNewComponentNs.CompanyLoanInitialCreditCheckNewComponent())