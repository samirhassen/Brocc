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
.controller('ctr', ['$scope', '$http', '$locale', '$translate', '$timeout', '$q', function ($scope, $http, $locale, $translate, $timeout, $q) {
    window.scope = $scope
    $scope.countries = initialData.countries
    $scope.isTest = !initialData.isProduction
    $scope.isBankAccountDataSharingEnabled = initialData.isBankAccountDataSharingEnabled
    
    function initBasedOnState(state) {
        $scope.state = state

        $scope.aq = null
        $scope.dc = null
        if (state.IsActive && state.ActiveState.ShouldAnswerAdditionalQuestions) {
            initAdditionalQuestions($scope, initialData, $translate, $http, initBasedOnState)
        } else if (state.IsActive && state.ActiveState.ShouldChooseDocumentSource) {
            initChooseDocumentSource($scope, initialData, $translate, $http, $q, $timeout, initBasedOnState)
        } else if (state.IsActive && state.ActiveState.IsWatingForDocumentUpload) {
            initDocumentCheck($scope, initialData, $translate, $http, $q, $timeout, initBasedOnState)
        } else if (state.IsActive && state.ActiveState.ShouldAnswerExternalAdditionalQuestions) {
            document.location.href = state.ActiveState.ExternalAdditionalQuestionsData.RedirectUrl;
        }
    }
    
    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    $scope.isValidIBAN = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        
        return ntech.fi.isValidIBAN(value.replace(" ", ""))
    }

    $scope.changeLanguage = function (key, event) {
        event.preventDefault()
        $translate.use(key)
    }

    $scope.currentLanguage = function () {
        return $translate.use()
    }

    $scope.hasCo = function () {
        return $scope.state.NrOfApplicants > 1
    }   

    $scope.currentStateName = function () {
        var s = $scope.state
        if (!s.IsActive) {
            if (s.ClosedState && s.ClosedState.WasAccepted) {
                return 'ClosedAsAccepted'
            } else {
                return 'ClosedAsOther'
            }
        } else if (s.IsActive && s.ActiveState.ShouldChooseDocumentSource && $scope.isBankAccountDataSharingEnabled) {
            return 'ShouldChooseDocumentSource'
        } else if (s.IsActive && s.ActiveState.IsWatingForDocumentUpload) {
            return 'IsWatingForDocumentUpload'
        } else if (s.IsActive && s.ActiveState.IsWaitingForClient && !s.ActiveState.IsAwaitingFinalApproval) {
            return 'IsWaitingForClient'
        } else if (s.IsActive && s.ActiveState.IsWaitingForClient && s.ActiveState.IsAwaitingFinalApproval) {
            return 'IsAwaitingFinalApproval'
        } else if (s.IsActive && !s.ActiveState.IsWaitingForClient && s.ActiveState.ShouldAnswerAdditionalQuestions) {
            return 'ShouldAnswerAdditionalQuestions'
        } else if (s.IsActive && !s.ActiveState.IsWaitingForClient && s.ActiveState.ShouldSignAgreements) {
            return 'ShouldSignAgreements'
        }
    }

    $scope.currentStateStepNr = function () {
        var s = $scope.currentStateName();
        if (s == 'ShouldAnswerAdditionalQuestions') {
            return 2
        } else if (s == 'ShouldSignAgreements') {
            return 3
        } else if (s == "ShouldChooseDocumentSource") {
            return 4
        } else if (s == 'IsWatingForDocumentUpload') {
            return 5
        } else if (s == 'IsAwaitingFinalApproval') {
            return 6
        } else {
            return null
        }
    }

    initBasedOnState(initialData.state)
}])