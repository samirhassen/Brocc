////////////////////////////////
//Ta bort medsokande ///////////
////////////////////////////////
//initialData.nrOfApplicants = 1
//initialData.applicant2 = null
////////////////////////////////

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
        window.scope = $scope
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

    $scope.readonly = {
        nrOfApplicants: initialData.nrOfApplicants,
        applicant1: initialData.applicant1,
        applicant2: initialData.applicant2
    }

    $scope.hasCo = function () {
        return $scope.readonly.nrOfApplicants > 1
    }

    $scope.countries = initialData.countries;

    $scope.isCountrySelected = function (c, i) {
        var s = false
        angular.forEach($scope.aa(i).taxCountries, function (v) {
            if (v === c) {
                s = true
            }
        })
        return s
    }

    $scope.answers = {
        applicant1 : {
            taxCountries: ['FI'],
            taxNumbers : {},
            pep: {},
            products: {}
        },
        applicant2 : {
            taxCountries: ['FI'],
            taxNumbers: {},
            pep: {},
            products: {}
        }
    }

    $scope.tmp1 = {}
    $scope.tmp2 = {}

    $scope.tmp = function (i) {
        if (i == 1) {
            return $scope.tmp1
        } else if (i == 2) {
            return $scope.tmp2
        }
    }

    $scope.aa = function (i) {
        if (i == 1) {
            return $scope.answers.applicant1
        } else if (i == 2) {
            return $scope.answers.applicant2
        }
    }

    $scope.isPep = function (i) {
        var isPep = false
        angular.forEach($scope.aa(i).pep, function (v, k) {
            if (k !== 'no' && v) {
                isPep = true
            }
        })
        return isPep
    }

    $scope.hasAnsweredPep = function (i) {
        var c = 0
        angular.forEach($scope.aa(i).pep, function (v) {
            if (v) {
                c = c + 1
            }            
        })
        return c > 0
    }

    $scope.hasAnsweredProducts = function (i) {
        var c = 0
        angular.forEach($scope.aa(i).products, function (v) {
            if (v) {
                c = c + 1
            }
        })
        return c > 0
    }

    $scope.countryName = function (c) {
        var n = c
        angular.forEach($scope.countries, function (v) {
            if (c == v.key) {
                n = v[$scope.currentLanguage()]
            }
        })
        return n
    }

    $scope.removeCountry = function (c, i) {
        var t = []
        angular.forEach($scope.aa(i).taxCountries, function (v) {
            if (v != c) {
                t.push(v)
            }
        })
        $scope.aa(i).taxCountries = t
    }

    $scope.addCountry = function (c, i, evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.aa(i).taxCountries.push(c)
        $scope.tmp(i).Country = null
    }

    $scope.kycQuestions = initialData.kycQuestions

    $scope.purposeAnswerClearText = function (i) {
        var answer = $scope.aa(i).purpose
        var tt = answer
        angular.forEach($scope.kycQuestions['loan_purpose'].answers, function (v) {
            if (v.key == answer) {
                tt = v[$scope.currentLanguage()]
            }
        })

        return tt
    }

    $scope.whoseMoneyAnswerClearText = function (i) {
        var answer = $scope.aa(i).whosmoney
        var extra = $scope.aa(i).whosmoney_extra
        var tt = answer
        angular.forEach($scope.kycQuestions['loan_whosmoney'].answers, function (v) {
            if (v.key == answer) {
                tt = v[$scope.currentLanguage()]
            }
        })

        if (extra) {
            tt = tt + ' (' + extra + ')'
        }
        return tt
    }
    
    $scope.paymentfrequencyAnswerClearText = function (i) {
        var answer = $scope.aa(i).paymentfrequency
        var tt = answer
        angular.forEach($scope.kycQuestions['loan_paymentfrequency'].answers, function (v) {
            if (v.key == answer) {
                tt = v[$scope.currentLanguage()]
            }
        })

        return tt
    }

    $scope.pepAnswerClearText = function (i) {
        var getPepAnswerText = function(t) {
            var tt = t
            angular.forEach($scope.kycQuestions['customer_pep'].answers, function (v) {
                if (v.key == t) {
                    tt = v[$scope.currentLanguage()]
                }
            })
            return tt
        }
        var answer = $scope.aa(i).pep
        if (answer.no) {
            return getPepAnswerText('no')
        } else {
            var t = ''
            angular.forEach($scope.kycQuestions['customer_pep'].answers, function (v) {
                if (answer[v.key]) {
                    if (t.length > 0) {
                        t = t + ', '
                    }
                    t = t + v[$scope.currentLanguage()]
                }
            })
            return t
        }
    }
    
    $scope.mainOccupationAnswerClearText = function (i) {
        var answer = $scope.aa(i).mainOccupation
        var tt = answer
        angular.forEach($scope.kycQuestions['customer_mainoccupation'].answers, function (v) {
            if (v.key == answer) {
                tt = v[$scope.currentLanguage()]
            }
        })

        return tt
    }

    $scope.productsAnswerClearText = function (i) {
        var answer = $scope.aa(i).products
        var t = ''
        if(answer.none) {
            if (t.length > 0) {
                t = t + ', '
            }
            t = t + $translate.instant('ansokan_products_none')
        }
        if(answer.savings) {
            if (t.length > 0) {
                t = t + ', '
            }
            t = t + $translate.instant('ansokan_products_savings')
        }
        if(answer.loanorcredit) {
            if (t.length > 0) {
                t = t + ', '
            }
            t = t + $translate.instant('ansokan_products_loanorcredit')
        }
        return t
    }

    $scope.apply = function () {
        if ($scope.fc.$invalid) {
            $scope.fc.$setSubmitted()
            return
        }
        $scope.isApplying = true

        function hashToCommaSeparatedList(h) {
            var r = ''
            angular.forEach(h, function (v, k) {
                if (v) {
                    if (r.length > 0) {
                        r = r + ','
                    }
                    r = r + k
                }
            })
            return r
        }

        var result = {
            id: initialData.id,
            iban: $scope.answers.iban.trim().replace(/ /g, '').toUpperCase(),
            kycQuestionsXml: initialData.kycQuestionsXml,
            userLanguage: $scope.currentLanguage()
        }
        //Kontonummer för utbetalning
        for (var i = 1; i <= $scope.readonly.nrOfApplicants; i++) {
            var aa = $scope.aa(i)
            var ar = {
                questions : []
            }
            //Vad ska pengarna användas till?
            ar.questions.push({
                name: 'loan_purpose',
                value: aa.purpose
            })
            ar.questions.push({
                name: 'loan_purpose_text',
                value: $scope.purposeAnswerClearText(i)
            })

            //Vems pengar kommer användas för återbetalning?
            ar.questions.push({
                name: 'loan_whosmoney',
                value: aa.whosmoney
            })
            ar.questions.push({
                name: 'loan_whosmoney_other',
                value: aa.whosmoney_extra
            })
            ar.questions.push({
                name: 'loan_whosmoney_text',
                value: $scope.whoseMoneyAnswerClearText(i)
            })

            //Hur ofta kommer avbetalningarna göras?
            ar.questions.push({
                name: 'loan_paymentfrequency',
                value: aa.paymentfrequency
            })
            ar.questions.push({
                name: 'loan_paymentfrequency_text',
                value: $scope.paymentfrequencyAnswerClearText(i)
            })
            
            //Pep
            if (aa.pep.no == true) {
                ar.questions.push({
                    name: 'customer_ispep',
                    value: 'false'
                })
                ar.questions.push({
                    name: 'customer_pep_roles',
                    value: 'none'
                })
                ar.questions.push({
                    name: 'customer_pep_name',
                    value: 'none'
                })
            } else {
                ar.questions.push({
                    name: 'customer_ispep',
                    value: 'true'
                })
                ar.questions.push({
                    name: 'customer_pep_roles',
                    value: hashToCommaSeparatedList(aa.pep)
                })
                ar.questions.push({
                    name: 'customer_pep_name',
                    value: aa.pep_who
                })
            }
            ar.questions.push({
                name: 'customer_pep_text',
                value: $scope.pepAnswerClearText(i)
            })

            //Vilken är din huvudsakliga sysselsättning?
            ar.questions.push({
                name: 'customer_mainoccupation',
                value: aa.mainOccupation
            })
            ar.questions.push({
                name: 'customer_mainoccupation_text',
                value: $scope.mainOccupationAnswerClearText(i)
            })            

            //Vilka av Balanzias produkter använder du?
            ar.questions.push({
                name: 'customer_products',
                value: hashToCommaSeparatedList(aa.products)
            })

            ar.questions.push({
                name: 'customer_products_text',
                value: $scope.productsAnswerClearText(i)
            })

            //Land du är skattepliktig i + nr
            var taxCountries
            taxCountries = []
            angular.forEach(aa.taxCountries, function (v) {
                if (v) {
                    var c = {}
                    c.countryIsoCode = v
                    if (aa.taxNumbers[v]) {
                        c.taxNumber = aa.taxNumbers[v]
                    }
                    taxCountries.push(c)
                }
            })

            ar.questions.push({
                name: 'customer_taxcountries',
                value: JSON.stringify(taxCountries)
            })
            
            if (i == 1) {
                result.applicant1 = ar
            } else {
                result.applicant2 = ar
            }
        }

		console.log('Applying: ' + result)

		if (!initialData.isProduction) {
			localStorage.setItem('last_successful_additionaquestions_v1', JSON.stringify({ answers: angular.copy($scope.answers) }))
		}
    }

    $scope.isTest = !initialData.isProduction
    $scope.loadTestData = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        var last = localStorage.getItem('last_successful_additionaquestions_v1')
        if (last) {
            var a = JSON.parse(last)
            $scope.answers = a.answers
        }
    }

    init()
}])