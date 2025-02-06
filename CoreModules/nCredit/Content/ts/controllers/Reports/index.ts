class ReportsCtrl {
    static $inject = ['$scope', '$http', '$timeout', '$q']
    constructor(
        $scope: ReportsNs.ILocalScope,
        private $http: ng.IHttpService,
        private $timeout: ng.ITimeoutService,
        $q: ng.IQService
    ) {
        $scope.backUrl = initialData.backUrl
        $scope.preGeneratedReports = initialData.preGeneratedReports
        $scope.dwAgeInDays = initialData.lastDwUpdateAgeInDays
        $scope.creditQuarters = initialData.creditQuarters
        $scope.reportUrls = initialData.reportUrls
        window.scope = $scope

        $scope.isValidDate = (value) => {
            if (ntech.forms.isNullOrWhitespace(value)) {
                return true
            }
            return moment(value, 'YYYY-MM-DD', true).isValid()
        }

        let isValidDateTwoDates = (value : any, nr: number) => {
            if (!($scope.current && $scope.current.model)) {
                return false
            }

            let m = $scope.current.model
            if (!$scope.isValidDate(value)) {
                return false
            }

            let otherValue = nr === 1 ? m.date2 : m.date1

            if (!($scope.isValidDate(otherValue))) {
                return false
            }
            if (m.maxDateIntervalLengthInDays) {
                let nrOfDays = Math.abs(moment(value).diff(moment(otherValue), 'days'))
                let isValid = nrOfDays <= m.maxDateIntervalLengthInDays
                $timeout(() => {
                    m.invalidDateIntervalLengthInDays = isValid ? null : nrOfDays
                })
                return isValid
            } else {
                return $scope.isValidDate(value)
            }
        }
        $scope.isValidDateTwoDates1 = value => isValidDateTwoDates(value, 1)
        $scope.isValidDateTwoDates2 = value => isValidDateTwoDates(value, 2)

        let client = new NTechCreditApi.ApiClient(msg => toastr.error(msg), $http, $q)
        $scope.onBack = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, client, $q)
        }

        $scope.isValid2Dates = (fromdate, todate) => {
            if (ntech.forms.isNullOrWhitespace(fromdate)) {
                return true
            }
            if (ntech.forms.isNullOrWhitespace(todate)) {
                return true
            }
            if (moment(todate, 'YYYY-MM-DD', true).isValid() == false)
                return true
            if (moment(fromdate, 'YYYY-MM-DD', true).isValid() == false)
                return true
            var months;
            months = (new Date(todate).getFullYear() - new Date(fromdate).getFullYear()) * 12;
            months -= new Date(fromdate).getMonth();
            months += new Date(todate).getMonth();
            if (months > 4)
                return true
            else
                return false
        }

        $scope.GetErrorTextValid2Dates = (fromdate, todate) => {
            if (ntech.forms.isNullOrWhitespace(fromdate)) {
                return 'Invalid date'
            }
            if (ntech.forms.isNullOrWhitespace(todate)) {
                return 'Invalid date'
            }
            if (moment(todate, 'YYYY-MM-DD', true).isValid() == false)
                return 'Invalid date'
            if (moment(fromdate, 'YYYY-MM-DD', true).isValid() == false)
                return 'Invalid date'
            var months;
            months = (new Date(todate).getFullYear() - new Date(fromdate).getFullYear()) * 12;
            months -= new Date(fromdate).getMonth();
            months += new Date(todate).getMonth();
            if (months > 4)
                return 'Max date span allowed is 4 months'
            else
                return ''
        }

        if (initialData.abTestExperiments) {
            $scope.experimentIdOptions = [];

            initialData.abTestExperiments.forEach(x => 
                $scope.experimentIdOptions.push(x)
            )
        }

        if (initialData.waterfallParameters) {
            let u = initialData.waterfallParameters

            $scope.providerOptions = []
            for (var i = 0; i < u.providerNames.length; i++) {
                let providerName = u.providerNames[i];
                let providerDisplayName = u.providerDisplayNameByProviderName[providerName] || providerName;
                $scope.providerOptions.push([providerName, providerDisplayName])
            }

            $scope.applicationMonthOptions = []
            for (var i = 0; i < u.applicationMonths.length; i++) {
                $scope.applicationMonthOptions.push([moment(u.applicationMonths[i]).format('YYYY-MM') + '-01', moment(u.applicationMonths[i]).format('YYYY-MM')])
            }

            $scope.applicationQuartersOptions = u.applicationQuarters
            $scope.applicationYearsOptions = u.applicationYears

            if (u.scoreGroups) {
                $scope.scoreGroupOptions = []
                for (var i = 0; i < u.scoreGroups.length; i++) {
                    $scope.scoreGroupOptions.push([u.scoreGroups[i], u.scoreGroups[i]])
                }
            }

            $scope.$watch('current.model.groupPeriod', function () {
                if ($scope.current && $scope.current.model && $scope.current.model.groupPeriod) {
                    $scope.current.model.monthFromDate = $scope.applicationMonthOptions && $scope.applicationMonthOptions.length > 0 ? $scope.applicationMonthOptions[0][0] : '';
                    $scope.current.model.monthToDate = $scope.applicationMonthOptions && $scope.applicationMonthOptions.length > 0 ? $scope.applicationMonthOptions[$scope.applicationMonthOptions.length - 1][0] : '';
                }
            })
        }
        
        $scope.$watch('reportName', function () {
            var r = $scope.reportName
            if (r == 'providerFeedback') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'applicationAnalysis') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).add(-1, 'days').format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'paymentsConsumerCredits') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'reservationBasis') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).startOf('month').add(-1, 'days').format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'quarterlyRATI') {
                var quarters = []
                for (var i = 1; i <= 8; i++) {
                    var m = moment(initialData.today).subtract(i, 'quarter').endOf('quarter')
                    quarters.push({
                        value: m.format('YYYY-MM-DD'),
                        startDate: m.startOf('quarter'),
                        endDate: m,
                        text: 'Q' + m.format('Q YYYY')
                    })
                }

                $scope.current = {
                    modelType: 'recentQuarter',
                    model: {
                        quarter: quarters[0],
                        quarters: quarters
                    }
                }
            } else if (r == 'liquidityExposure') {
                var months = []
                for (var i = 1; i <= 8; i++) {
                    var m = moment(initialData.today).subtract(i, 'month').endOf('month')
                    months.push({
                        value: m.format('YYYY-MM-DD'),
                        endDate: m,
                        text: m.format('YYYY-MM')
                    })
                }

                $scope.current = {
                    modelType: 'recentMonth',
                    model: {
                        month: months[0],
                        months: months
                    }
                }
            } else if (r == 'lcr') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).add(-1, 'days').format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'loanPerformance') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'swedishQuarterlyF818') {
                let quarters = []
                for (let q of $scope.creditQuarters) {
                    let m = moment(q.ToDate)
                    quarters.push({
                        value: m.format('YYYY-MM-DD'),
                        startDate: m.startOf('quarter'),
                        endDate: m,
                        text: 'Q' + m.format('Q YYYY')
                    })
                }

                $scope.current = {
                    modelType: 'recentQuarter',
                    model: {
                        quarter: quarters[0],
                        quarters: quarters
                    }
                }
            } else if (r == 'mortgageLoanPerformance') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'mortgageLoanIfrsCollateral') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'mortgageAverageInterestRates') {
                let months = []
                for (var i = 1; i <= 12; i++) {
                    var m = moment(initialData.today).subtract(i, 'month').endOf('month')
                    months.push({
                        value: m.format('YYYY-MM-DD'),
                        endDate: m,
                        text: m.format('YYYY-MM')
                    })
                }
                let languages = [{ value: 'local', text: 'Local' }, { value: 'en', text: 'English' }];
                let includeDetailsOptions = [{ value: 'false', text: 'No' }, { value: 'true', text: 'Yes' }];
                $scope.current = {
                    modelType: 'mortgageAverageInterestRates',
                    model: {
                        month: months[0],
                        months: months,
                        language: languages[0],
                        languages: languages,
                        includeDetails: includeDetailsOptions[0],
                        includeDetailsOptions: includeDetailsOptions
                    }
                }
            } else if (r == 'companyLoanledger') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'companyLoanCustomLedger') {
                var months = []
                for (var i = 0; i <= 8; i++) {
                    var m = moment(initialData.today).subtract(i, 'month').endOf('month')
                    months.push({
                        value: m.format('YYYY-MM-DD'),
                        endDate: m,
                        text: m.format('YYYY-MM')
                    })
                }

                $scope.current = {
                    modelType: 'recentMonth',
                    model: {
                        month: months[0],
                        months: months
                    }
                }                
            } else if (r == 'companyLoanOverdueNotifications' || r == 'alternatePaymentPlans') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'bookkeepingLoanLedger') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).add(-1, 'days').format('YYYY-MM-DD')
                    },
                    date1LabelText: 'BookKeeping date'
                }
            } else if (r == 'cancelledApplications') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                }
            } else if (r == 'unplacedBalance') {
                $scope.current = {
                    modelType: 'dateAndDropdown',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD'),
                        dropdown1: 'false'
                    },
                    date1LabelText: 'Date',
                    dropdown1LabelText: 'Date type',
                    dropdown1Options: [['false', 'BookKeeping'], ['true', 'Transaction']],
                    dropdown1ParameterName: 'useTransactionDate'
                }
            } else if (r == 'applicationRejectionReasons') {
                $scope.current = {
                    modelType: 'dropdown',
                    model: {
                        dropdown1: $scope.providerOptions.length > 0 ? $scope.providerOptions[0][0] : ''
                    },
                    dropdown1LabelText: 'Provider name',
                    dropdown1Options: $scope.providerOptions,
                    dropdown1ParameterName: 'providerName'
                }
            } else if (r == 'contactlist') {
                $scope.current = {
                    modelType: 'contactlist',
                    model: {
                    }
                }
            } else if (r == 'applicationWaterfall') {
                $scope.current = {
                    modelType: 'applicationWaterfall',
                    applicationMonthOptions: angular.copy($scope.applicationMonthOptions),
                    applicationYearsOptions: angular.copy($scope.applicationYearsOptions),
                    applicationQuartersOptions: angular.copy($scope.applicationQuartersOptions),
                    providerOptions: angular.copy($scope.providerOptions),
                    scoreGroupOptions: angular.copy($scope.scoreGroupOptions),
                    model: {
                        groupPeriod: 'monthly',
                        monthFromDate: $scope.applicationMonthOptions && $scope.applicationMonthOptions.length > 0 ? $scope.applicationMonthOptions[0][0] : '',
                        monthToDate: $scope.applicationMonthOptions && $scope.applicationMonthOptions.length > 0 ? $scope.applicationMonthOptions[$scope.applicationMonthOptions.length - 1][0] : '',
                        providerName: '',
                        scoreGroup: '',
                        campaignCode: ''
                    }
                }
            } else if (r == 'standardApplicationWaterfall') {
                $scope.current = {
                    modelType: 'standardApplicationWaterfall',
                    applicationMonthOptions: angular.copy($scope.applicationMonthOptions),
                    applicationYearsOptions: angular.copy($scope.applicationYearsOptions),
                    applicationQuartersOptions: angular.copy($scope.applicationQuartersOptions),
                    providerOptions: angular.copy($scope.providerOptions),
                    scoreGroupOptions: angular.copy($scope.scoreGroupOptions),
                    model: {
                        groupPeriod: 'monthly',
                        monthFromDate: $scope.applicationMonthOptions && $scope.applicationMonthOptions.length > 0 ? $scope.applicationMonthOptions[0][0] : '',
                        monthToDate: $scope.applicationMonthOptions && $scope.applicationMonthOptions.length > 0 ? $scope.applicationMonthOptions[$scope.applicationMonthOptions.length - 1][0] : '',
                        providerName: '',
                        scoreGroup: '',
                        campaignCode: ''
                    }
                }
            } else if (r == 'mortgageLoanApplicationWaterfall') {
                $scope.current = {
                    modelType: 'mortgageLoanApplicationWaterfall',
                    applicationMonthOptions: angular.copy($scope.applicationMonthOptions),
                    applicationYearsOptions: angular.copy($scope.applicationYearsOptions),
                    applicationQuartersOptions: angular.copy($scope.applicationQuartersOptions),
                    providerOptions: angular.copy($scope.providerOptions),
                    model: {
                        groupPeriod: 'monthly',
                        monthFromDate: $scope.applicationMonthOptions && $scope.applicationMonthOptions.length > 0 ? $scope.applicationMonthOptions[0][0] : '',
                        monthToDate: $scope.applicationMonthOptions && $scope.applicationMonthOptions.length > 0 ? $scope.applicationMonthOptions[$scope.applicationMonthOptions.length - 1][0] : '',
                        providerName: ''
                    }
                }
            } else if (r == 'companyLoanApplicationList') {
                $scope.current = {
                    modelType: 'dropdown',
                    model: {
                        dropdown1: 'thisweek'
                    },
                    dropdown1LabelText: 'Period',
                    dropdown1Options: [['today', 'Today'], ['thisweek', 'This week'], ['thismonth', 'This month']],
                    dropdown1ParameterName: 'PeriodName'
                }
            }
            else if (r == 'mortgageLoanCollateral') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                }
            }
            else if (r == 'mortgageLoanQuarterlyBKI') {
                let quarters = []
                for (let q of $scope.creditQuarters) {
                    let m = moment(q.ToDate)
                    quarters.push({
                        value: m.format('YYYY-MM-DD'),
                        startDate: m.startOf('quarter'),
                        endDate: m,
                        text: 'Q' + m.format('Q YYYY')
                    })
                }

                $scope.current = {
                    modelType: 'recentQuarter',
                    model: {
                        quarter: quarters[0],
                        quarters: quarters
                    }
                }
            }
            else if (r === 'mortgageLoanApplications') {
                $scope.current = {
                    modelType: 'twoDates',
                    useInputPair1: true,
                    model: {
                        name1: 'From date',
                        name2: 'To date',
                        date1: moment(initialData.today).add(-30, 'days').format('YYYY-MM-DD'),
                        date2: moment(initialData.today).format('YYYY-MM-DD'),
                        paramName1: 'FromApplicationDate',
                        paramName2: 'ToApplicationDate',
                        maxDateIntervalLengthInDays: 35,
                        inputPair1: {
                            label1: 'Campaign parameter - Name',
                            label2: 'Value',
                            placeholder1: 'ie utm_campaign',
                            placeholder2: 'ie summer-2020',
                            parameterName1: 'CampaignParameterName',
                            parameterName2: 'CampaignParameterValue'
                        }
                    }
                }
            }
            else if (r === 'abTestExperiments') {
                $scope.current = {
                    modelType: 'abTestExperiments',
                    experimentIdOptions: angular.copy($scope.experimentIdOptions),
                    model: {
                        experimentId: $scope.experimentId
                    }
                }
            }
            else if (r === 'legacyAmlReportingAidFi' || r === 'amlReportingAidCompanySe') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).startOf('year').add(-1, 'days').format('YYYY-MM-DD')
                    }
                }
            }
            else if (r === 'mortgageFixedInterestRateHistory' || r === 'kycQuestionsStatus') {
                $scope.current = {
                    modelType: 'noOptions',
                    model: { }
                }
            }            
            else if (r === 'bookkeepingReconciliation') {
                $scope.current = {
                    modelType: 'twoDates',
                    model: {
                        name1: 'From date',
                        name2: 'To date',
                        date1: moment(initialData.today).add(-30, 'days').format('YYYY-MM-DD'),
                        date2: moment(initialData.today).format('YYYY-MM-DD'),
                        paramName1: 'FromDate',
                        paramName2: 'ToDate',
                        maxDateIntervalLengthInDays: 186,
                    }
                }
            }
            else {
                $scope.current = {
                    modelType: '',
                    model: {}
                }
            }
        })

        $scope.createReport = (evt: Event) => {
            var baseUrl = initialData.reportUrls[$scope.reportName]
            if ($scope.current.modelType === 'dateOnly') {
                $scope.current.reportUrl = baseUrl + '?date=' + $scope.current.model.date
            } else if  ($scope.current.modelType === 'twoDates') {
                let m = $scope.current.model
                let pn1 = m.paramName1 || 'date1'
                let pn2 = m.paramName2 || 'date2'
                let reportUrl = `${baseUrl}?${pn1}=${m.date1}&${pn2}=${m.date2}`
                if ($scope.current.dropdown1Options) {
                    reportUrl += `&${$scope.current.dropdown1ParameterName}=${$scope.current.model.dropdown1}`
                }
                if ($scope.current.useInputPair1) {
                    let i = m.inputPair1
                    if (i.value1 && i.value2) {                        
                        reportUrl += `&${i.parameterName1}=${encodeURIComponent(i.value1)}&${i.parameterName2}=${encodeURIComponent(i.value2)}`
                    }
                }
                $scope.current.reportUrl = reportUrl
            } else if ($scope.current.modelType === 'consistency') {
                $scope.current.reportUrl = baseUrl + '?date1=' + $scope.current.model.date1 + '&date2=' + $scope.current.model.date2
            } else if ($scope.current.modelType === 'recentQuarter') {
                $scope.current.reportUrl = baseUrl + '?quarterEndDate=' + $scope.current.model.quarter.value
            } else if ($scope.current.modelType === 'recentMonth') {
                $scope.current.reportUrl = baseUrl + '?monthEndDate=' + $scope.current.model.month.value
            } else if ($scope.current.modelType === 'dateAndDropdown') {
                $scope.current.reportUrl = baseUrl + '?date=' + $scope.current.model.date + '&' + $scope.current.dropdown1ParameterName + '=' + $scope.current.model.dropdown1
            } else if ($scope.current.modelType === 'dropdown') {
                $scope.current.reportUrl = baseUrl + '?' + $scope.current.dropdown1ParameterName + '=' + $scope.current.model.dropdown1
            } else if ($scope.current.modelType === 'applicationWaterfall') {
                let m = $scope.current.model
                let url = baseUrl + '?FromMonthDate=' + m.monthFromDate + '&ToMonthDate=' + m.monthToDate + '&groupPeriod=' + m.groupPeriod
                if (m.providerName) {
                    url = url + '&ProviderName=' + m.providerName
                }
                if (m.scoreGroup) {
                    url = url + '&ScoreGroup=' + m.scoreGroup
                }
                if (m.campaignCode) {
                    url = url + '&CampaignCode=' + m.campaignCode
                }
                $scope.current.reportUrl = url                
            } else if ($scope.current.modelType === 'standardApplicationWaterfall') {
                let m = $scope.current.model
                let url = '/Ui/Report/nPreCredit/api/UnsecuredLoanStandard/Reports/Waterfall' + '?FromInPeriodDate=' + m.monthFromDate + '&ToInPeriodDate=' + m.monthToDate + '&PeriodType=' + m.groupPeriod
                if (m.providerName) {
                    url = url + '&ProviderName=' + m.providerName
                }
                $scope.current.reportUrl = url
            } else if ($scope.current.modelType === 'mortgageLoanApplicationWaterfall') {
                let m = $scope.current.model
                let url = '/Ui/Report/nPreCredit/api/MortgageLoan/Reports/Waterfall' + '?FromInPeriodDate=' + m.monthFromDate + '&ToInPeriodDate=' + m.monthToDate + '&PeriodType=' + m.groupPeriod
                if (m.providerName) {
                    url = url + '&ProviderName=' + m.providerName
                }
                if (m.campaignParameterName && m.campaignParameterValue) {
                    url = url + '&CampaignParameterName=' + encodeURIComponent(m.campaignParameterName)
                    url = url + '&CampaignParameterValue=' + encodeURIComponent(m.campaignParameterValue)
                }
                $scope.current.reportUrl = url
            }
            else if ($scope.current.modelType === 'abTestExperiments') {
                let m = $scope.current.model
                let url = `/Ui/Report/nPreCredit/api/AbTesting/Reports/ExperimentStatus?ExperimentId=${encodeURIComponent(m.experimentId)}`;
                $scope.current.reportUrl = url
            }
            else if ($scope.current.modelType === 'noOptions') {
                $scope.current.reportUrl = baseUrl
            }
            else if ($scope.current.modelType == 'mortgageAverageInterestRates') {
                let m = $scope.current.model;
                $scope.current.reportUrl = baseUrl + `?date=${m.month.value}&language=${m.language.value}&includeDetails=${m.includeDetails.value}`
            } else {
                evt.preventDefault()
                return
            }
        }

        $scope.createContactList = (evt: Event) => {
            var post = (path, params, method) => {
                method = method || "post"; // Set method to post by default if not specified.

                // The rest of this code assumes you are not using a library.
                // It can be made less wordy if you use one.
                var form = document.createElement("form");
                form.setAttribute("method", method);
                form.setAttribute("action", path);

                for (var key in params) {
                    if (params.hasOwnProperty(key)) {
                        var hiddenField = document.createElement("input");
                        hiddenField.setAttribute("type", "hidden");
                        hiddenField.setAttribute("name", key);
                        hiddenField.setAttribute("value", params[key]);

                        form.appendChild(hiddenField);
                    }
                }

                document.body.appendChild(form);
                form.submit();
            }
            let d = {}
            if ($scope.current.model.applicationNrs) {
                d['applicationNrsFlat'] = $scope.current.model.applicationNrs
            }
            if ($scope.current.model.creditNrs) {
                d['creditNrsFlat'] = $scope.current.model.creditNrs
            }
            post(initialData.reportUrls[$scope.reportName], d, null)
        }
    }
}

var app = angular.module('app', ['ntech.forms']);
app.controller('ctr', ReportsCtrl)

module ReportsNs {
    export interface ILocalScope extends ng.IScope {
        backUrl: string
        preGeneratedReports: any
        dwAgeInDays: any
        isValidDate: (value: any) => boolean
        isValidDateTwoDates1: (value: any) => boolean
        isValidDateTwoDates2: (value: any) => boolean
        twoDatesForm?: ng.IFormController
        providerOptions: any
        applicationMonthOptions: any
        applicationQuartersOptions: any
        applicationYearsOptions: any
        scoreGroupOptions: any
        current: any
        reportName: string
        createReport: (evt: Event) => void
        createContactList: (evt: Event) => void
        isClientBalanziaFi: boolean
        creditQuarters: { InYearOrdinalNr: number, FromDate: Date, ToDate: Date, Name: string }[]
        isValid2Dates: (fromdate: any, todate: any) => boolean
        GetErrorTextValid2Dates: (fromdate: any, todate: any) => any
        onBack: (evt: Event) => void
        experimentId: number
        experimentIdOptions: any
        reportUrls: NTechCreditApi.IStringStringDictionary
    }
}