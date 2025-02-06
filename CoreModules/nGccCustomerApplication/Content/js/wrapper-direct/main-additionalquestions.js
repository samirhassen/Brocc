var initAdditionalQuestions = function ($scope, initialData, $translate, $http, initBasedOnState) {
    $scope.aq = {}
    $scope.aq.loadTestData = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        var last = localStorage.getItem('last_successful_additionaquestions_v5')
        if (last) {
            var a = JSON.parse(last)
            $scope.aq.answers = a.answers
        }
    }

    $scope.aq.isAdditionalLoanOffer = $scope.state.ActiveState.AdditionalQuestionsData.IsAdditionalLoanOffer;

    $scope.aq.answers = {
        applicant1: {
            applicantNr: 1,
            products: {},
        },
        applicant2: {
            applicantNr: 2,
            products: {},
        }
    }

    $scope.aq.tmp1 = {}
    $scope.aq.tmp2 = {}

    $scope.aq.tmp = function (i) {
        if (i == 1) {
            return $scope.aq.tmp1
        } else if (i == 2) {
            return $scope.aq.tmp2
        }
    }

    $scope.aq.aa = function (i) {
        if (i == 1) {
            return $scope.aq.answers.applicant1
        } else if (i == 2) {
            return $scope.aq.answers.applicant2
        }
    }

    $scope.aq.kycQuestions = initialData.kycQuestions

    $scope.aq.getConsentAnswersJson = function (applicantNr) {
        let consentAnswers = {
            date: new Date().toLocaleString(),
            applicantNr: applicantNr,
            additionalQuestions_KfConsentText: $translate.instant('ansokan_kf_text'),
            additionalQuestions_KfLink: {
                uri: $translate.instant('ansokan_kf_uri'),
                rawLinkText: $translate.instant('ansokan_kf_link'),
            },
            additionalQuestions_ConsentText: {
                consentChecked: $scope.aq.aa(applicantNr).customerConsent,
                text: $translate.instant('ansokan_consent_text')
            }
        }
        return JSON.stringify(consentAnswers);
    }

    $scope.aq.apply = function () {
        if ($scope.aq.fc.$invalid) {
            $scope.aq.fc.$setSubmitted()
            return
        }
        $scope.aq.isApplying = true

        var result = {
            token: $scope.state.Token,
            iban: $scope.aq.answers.iban.trim().replace(/ /g, '').toUpperCase(),
            userLanguage: $scope.currentLanguage()
        }
        
        for (var i = 1; i <= $scope.state.NrOfApplicants; i++) {
            var ar = {
                consentRawJson: $scope.aq.getConsentAnswersJson(i)
            }

            if (i == 1) {
                result.applicant1 = ar
            } else {
                result.applicant2 = ar
            }
        }

        $http({
            method: 'POST',
            url: '/application-wrapper-direct-apply-additionalquestions',
            data: result
        }).then(function (result) {            
            if (!initialData.isProduction) {
                localStorage.setItem('last_successful_additionaquestions_v4', JSON.stringify({ answers: angular.copy($scope.aq.answers) }))
            }            
            var s = result.data.state
            if (s.IsActive && s.NrOfApplicants === 1 && s.ActiveState.ShouldSignAgreements) {
                document.location = '/application-wrapper-direct-sign?applicantNr=1&token=' + s.Token
            } else {
                initBasedOnState(result.data.state)
            }
        }, function (result) {
            initBasedOnState(null)
        })
    }
}