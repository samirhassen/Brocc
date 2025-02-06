angular
.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms'])
.config(['$translateProvider', function ($translateProvider) {
    $translateProvider
        .useUrlLoader(initialData.translateUrl)
        .registerAvailableLanguageKeys(['fi', 'sv'], {
            'sv_*': 'sv',
            'fi_*': 'fi',
            '*': 'fi'
        })
        .translations('sv', initialData.svTranslations)
        .translations('fi', initialData.fiTranslations)
        .useSanitizeValueStrategy('escape')
        .determinePreferredLanguage()
        .fallbackLanguage('fi')
        .useLocalStorage()
}])
.controller('ctr', ['$scope', '$http', '$locale', '$translate', function ($scope, $http, $locale, $translate) {
    function init() {
        $scope.educationCodes = ['education_grundskola', 'education_yrkesskola', 'education_gymnasie', 'education_hogskola']
        $scope.housingCodes = ['housing_egenbostad', 'housing_bostadsratt', 'housing_hyresbostad', 'housing_hosforaldrar', 'housing_tjanstebostad']
        $scope.employmentCodes = ['employment_fastanstalld', 'employment_visstidsanstalld', 'employment_foretagare', 'employment_pensionar', 'employment_sjukpensionar', 'employment_studerande', 'employment_arbetslos']
        $scope.marriageCodes = ['marriage_gift', 'marriage_ogift', 'marriage_sambo']
        $scope.hasInitialCampaignCode = !!initialData.campaignCode
        $scope.allSources = [{ k: 'ansokan_cc_brevhem', c: '1' },
            { k: 'ansokan_cc_internet', c: '2' },
            { k: 'ansokan_cc_tidning', c: '3' },
            { k: 'ansokan_cc_rekommendation', c: '4' },
            { k: 'ansokan_cc_tvradio', c: '5' },
            { k: 'ansokan_cc_annat', c: '6' }]
        $scope.getCampaignCodeFromSource = function (source) {
            if (!source) {
                return null
            }
            return 'H000' + source.c.toString() + '0'
        }
        if (!$scope.hasInitialCampaignCode) {
            $scope.$watch('app.hasCampaignCode', function () {
                if ($scope.app) {
                    $scope.app.campaignCode = null
                }
            })
        }

        $scope.years = []
        for (var i = 1; i <= 15; i++) {
            $scope.years.push(i)
        }
        $scope.app = { applicant1: { creditReportConsent: false, customerConsent: false, approvedSat: false }, applicant2: { creditReportConsent: false, customerConsent: false, approvedSat: false } }
        $scope.app.amount = initialData.amount
        $scope.app.repaymentTimeInYears = initialData.repaymentTimeInYears
        $scope.app.campaignCode = initialData.campaignCode
        window.scope = $scope
    }

    function onEducationChanged(applicant) {
        if (applicant.education) {
            $translate('ansokan_' + applicant.education).then(function (t) {
                applicant.educationText = t
            })
        } else {
            applicant.educationText = null
        }
    }
    $scope.$watch('app.applicant1.education', function () {
        onEducationChanged($scope.app.applicant1)        
    })
    $scope.$watch('app.applicant2.education', function () {
        onEducationChanged($scope.app.applicant2)
    })

    function onHousingChanged(applicant) {
        if (applicant.housing) {
            $translate('ansokan_' + applicant.housing).then(function (t) {
                applicant.housingText = t
            })
        } else {
            applicant.housingText = null
        }
    }
    $scope.$watch('app.applicant1.housing', function () {
        onHousingChanged($scope.app.applicant1)
    })
    $scope.$watch('app.applicant2.housing', function () {
        onHousingChanged($scope.app.applicant2)
    })
    
    function onEmploymentChanged(applicant) {
        if (applicant.employment) {
            $translate('ansokan_' + applicant.employment).then(function (t) {
                applicant.employmentText = t
            })
            if (!$scope.isEmployedSinceRequired(applicant)) {
                delete applicant['employedSinceMonth']
            }            
            if (!$scope.areEmploymentExtrasRequired(applicant)) {
                delete applicant['employer']
                delete applicant['employerPhone']
            }
        } else {
            applicant.employmentText = null
        }
    }
    $scope.$watch('app.applicant1.employment', function () {
        onEmploymentChanged($scope.app.applicant1)
    })
    $scope.$watch('app.applicant2.employment', function () {
        onEmploymentChanged($scope.app.applicant2)
    })

    function onMarriageChanged(applicant) {
        if (applicant.marriage) {
            $translate('ansokan_' + applicant.marriage).then(function (t) {
                applicant.marriageText = t
            })
        } else {
            applicant.marriageText = null
        }
    }
    $scope.$watch('app.applicant1.marriage', function () {
        onMarriageChanged($scope.app.applicant1)
    })
    $scope.$watch('app.applicant2.marriage', function () {
        onMarriageChanged($scope.app.applicant2)
    })
    
    function onCustomerConsentChanged(applicant) {
        if (applicant.customerConsent) {
            $translate('ansokan_kundsamtycke').then(function (t) {
                applicant.customerConsentText = t
            })
        } else {
            applicant.customerConsentText = null
        }
    }
    $scope.$watch('app.applicant1.customerConsent', function () {
        onCustomerConsentChanged($scope.app.applicant1)
    })
    $scope.$watch('app.applicant2.customerConsent', function () {
        onCustomerConsentChanged($scope.app.applicant2)
    })

    function onCreditReportConsentChanged(applicant) {
        if (applicant.creditReportConsent) {
            $translate('ansokan_kreditsamtycke').then(function (t) {
                applicant.creditReportConsentText = t
            })
        } else {
            applicant.creditReportConsentText = null
        }
    }
    $scope.$watch('app.applicant1.creditReportConsent', function () {
        onCreditReportConsentChanged($scope.app.applicant1)
    })
    $scope.$watch('app.applicant2.creditReportConsent', function () {
        onCreditReportConsentChanged($scope.app.applicant2)
    })

    $scope.isTest = !initialData.isProduction
    $scope.loadTestData = function (evt) {
        if (evt) {
            evt.preventDefault()
        }        
        var last = localStorage.getItem('last_successful_application_v2')
        if (last) {
            $scope.app = JSON.parse(last)
        }
    }

    $scope.changeLanguage = function (key, event) {
        event.preventDefault()
        $translate.use(key)
    }

    $scope.currentLanguage = function () {
        return $translate.use()
    }

    init()

    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    $scope.hasApplicant2 = function () {
        return $scope.app.nrOfApplicants && $scope.app.nrOfApplicants == '2'
    }

    $scope.areEmploymentExtrasRequired = function (applicant) {
        return applicant.employment == 'employment_fastanstalld' || applicant.employment == 'employment_visstidsanstalld' || applicant.employment == 'employment_foretagare'
    }

    $scope.isEmployedSinceRequired = function (applicant) {
        //Alla anställningsformer utom "studerande" och "arbetslös" ska få upp följdfrågan "anställd sedan".
        return !(applicant.employment == 'employment_studerande' || applicant.employment == 'employment_arbetslos')
    }

    $scope.isValidCivicNr_applicant1 = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        if (!ntech.fi.isValidCivicNr(value))
            return false;
        if ($scope && $scope.app && $scope.app.applicant2 && $scope.app.applicant2.civicRegNr) {
            return !areSameCivicRegNrs(value, $scope.app.applicant2.civicRegNr)
        }
        return true
    }

    $scope.isValidCivicNr_applicant2 = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        if (!ntech.fi.isValidCivicNr(value))
            return false;
        if ($scope && $scope.app && $scope.app.applicant1 && $scope.app.applicant1.civicRegNr) {
            return !areSameCivicRegNrs(value, $scope.app.applicant1.civicRegNr)
        }
        return true
    }

    function areSameCivicRegNrs(c1, c2) {
        if (!c1 || !c2 || !ntech.fi.isValidCivicNr(c1) || !ntech.fi.isValidCivicNr(c2)) {
            return false
        }
        return c1.replace(/^[ ]+|[ ]+$/g, '').toLowerCase() === c2.replace(/^[ ]+|[ ]+$/g, '').toLowerCase()
    }
            
    $scope.isValidPositiveInt = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        var v = value.toString()
        return (/^(\+)?([0-9]+)$/.test(value))
    }

    $scope.isValidEmail = function (value) {
        //Just to help the user in case they mix up the fields. Not trying to ensure it's actually possible to send email here
        if (isNullOrWhitespace(value))
            return true;

        var i = value.indexOf('@')
        return value.length >= 3 && i > 0 && i < (value.length-1)
    }

    $scope.isValidMonthFi = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        return moment('01.' + value, "DD.MM.YYYY", true).isValid()
    }

    $scope.isValidLoansToSettleAmount = function (value) {
        if (isNullOrWhitespace(value) || isNullOrWhitespace($scope.app.amount))
            return true;
        return parseInt(value, 10) <= parseInt($scope.app.amount, 10)
    }

    function translateValueForTransmission(value) {
        if (typeof (value) === "boolean") {
            return value === true ? "true" : "false"
        } else {
            return value
        }        
    }

    function createApplicationRequest() {
        function fixApplicant(applicant) {
            if (!applicant) {
                return
            }
            if (applicant.employedSinceMonth) {
                applicant.employedSinceMonth = moment('01.' + applicant.employedSinceMonth, "DD.MM.YYYY", true).format('YYYY-MM')
            }
            angular.forEach(applicant, function (value, key) {
                if (isNullOrWhitespace(value)) {
                    delete applicant[key]
                }
            })
        }

        var a = angular.copy($scope.app)
        angular.forEach(a, function (value, key) {
            if (isNullOrWhitespace(value)) {
                delete a[key]
            }
        })

        fixApplicant(a.applicant1)

        if (a.nrOfApplicants && a.nrOfApplicants == '1') {
            delete a['applicant2']
        } else {
            fixApplicant(a.applicant2)
        }

        //Convert to list
        var req = {
            userLanguage : $scope.currentLanguage(),
            nrOfApplicants: a.nrOfApplicants,
            items : []
        }

        var applicationItemsSource = angular.copy(a)
        delete applicationItemsSource['applicant1']
        delete applicationItemsSource['applicant2']
        delete applicationItemsSource['nrOfApplicants']
        
        angular.forEach(applicationItemsSource, function (value, key) {
            req.items.push({ group: 'application', name: key, value: translateValueForTransmission(value) })
        })

        angular.forEach(a.applicant1, function (value, key) {
            req.items.push({ group: 'applicant1', name: key, value: translateValueForTransmission(value) })
        })
        req.items.push({ group: 'applicant1', name: 'civicRegNrCountry', value: 'FI' })

        if (a.nrOfApplicants && a.nrOfApplicants == '2') {
            angular.forEach(a.applicant2, function (value, key) {
                req.items.push({ group: 'applicant2', name: key, value: translateValueForTransmission(value) })
            })
            req.items.push({ group: 'applicant2', name: 'civicRegNrCountry', value: 'FI' })
        }
        
        return req
    }

    $scope.isFormInvalid = function () {
        return $scope.fc.$invalid || $scope.f1.$invalid || ($scope.hasApplicant2() && $scope.f2.$invalid)
    }
    $scope.apply = function () {
        if ($scope.isFormInvalid()) {
            $scope.fc.$setSubmitted()
            $scope.f1.$setSubmitted()
            $scope.f2.$setSubmitted()
            return
        }
        $scope.isApplying = true
        
        var request = createApplicationRequest()

        $http({
            method: 'POST',
            url: initialData.applyUrl,
            data: request
        }).then(function (result) {
            if (!initialData.isProduction) {
                localStorage.setItem('last_successful_application_v2', JSON.stringify(angular.copy($scope.app)))
            }
            if (result.data.redirectToUrl) {
                document.location = result.data.redirectToUrl
            } else if (result.data.debugapp) {
                $scope.debugapp = result.data.debugapp
            } else {
                document.location = initialData.failedUrl
            }
        }, function (result) {
            document.location = initialData.failedUrl
        })
    }
    $scope.reset = function () {
        document.location = initialData.cancelUrl
    }
    $scope.showInvalid = function (field) {
        return (field.$invalid && (field.$touched || $scope.fc.$submitted))
    }
}])