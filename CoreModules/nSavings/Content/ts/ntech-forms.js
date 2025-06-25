if (ntech === undefined) {
    var ntech = {}
}

ntech.forms = (function () {
    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    function isValidPositiveDecimal(value) {
        if (isNullOrWhitespace(value))
            return true;
        let v = value.toString()
        return (/^([0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v)
    }

    function isValidDecimal(value) {
        if (isNullOrWhitespace(value))
            return true;
        var v = value.toString()
        return (/^([-]?)([0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v)
    }

    function escapeRegExp(str) {
        return str.replace(/([.*+?^=!:${}()|\[\]\/\\])/g, "\\$1");
    }

    function replaceAll(str, find, replace) {
        return str.replace(new RegExp(escapeRegExp(find), 'g'), replace);
    }

    return {
        isNullOrWhitespace: isNullOrWhitespace,
        isValidPositiveDecimal: isValidPositiveDecimal,
        isValidDecimal: isValidDecimal,
        replaceAll: replaceAll
    }
}());

(function (window, document) {
    'use strict';

    angular
        .module('ntech.forms', [])
        .factory('ntechEncryptionService', function ntechEncryptionServiceFactory() {
            let allNames = []
            let allGroups = {}
            let clearText = {}
            let hasClearText = {}
            let isDecrypting = {}
            let config = null

            function contains(a, obj) {
                for (const item of a) {
                    if (item === obj) {
                        return true;
                    }
                }
                return false;
            }

            function registerName(name, groupName) {
                if (!contains(allNames, name)) {
                    allNames.push(name)
                }
                if (groupName) {
                    if (!allGroups[groupName]) {
                        allGroups[groupName] = []
                    }
                    let g = allGroups[groupName]
                    if (!contains(g, name)) {
                        g.push(name)
                    }
                }
            }

            function decrypt(names) {
                return config.decrypt(names)
            }

            function onDecrypted(name, clearText) {
                return config.onDecrypted(name, clearText)
            }

            function doDecrypt(names) {
                let dnames = []
                for (const element of names) {
                    if (!clearText[element]) {
                        dnames.push(element)
                    }
                }
                if (dnames.length > 0) {
                    for (const element of dnames) {
                        isDecrypting[element] = true
                    }
                    return decrypt(dnames).then(function (result) {
                        for (const element of result) {
                            let r = element;
                            hasClearText[r.name] = true
                            clearText[r.name] = r.clearText
                            isDecrypting[r.name] = null
                            onDecrypted(r.name, r.clearText)
                        }
                    })
                }
            }

            function isArray(o) {
                return Object.prototype.toString.call(o) === '[object Array]'
            }

            return {
                registerName: registerName,
                decryptAll: function () {
                    doDecrypt(allNames)
                },
                decryptGroup: function (groupName) {
                    doDecrypt(allGroups[groupName])
                },
                decrypt: function (nameOrNames) {
                    if (isArray(nameOrNames)) {
                        doDecrypt(nameOrNames)
                    } else {
                        doDecrypt([nameOrNames])
                    }
                },
                areAllDecrypted: function () {
                    for (const element of allNames) {
                        if (!hasClearText[element]) {
                            return false
                        }
                    }
                    return true
                },
                isGroupDecrypted: function (groupName) {
                    let g = allGroups[groupName]
                    if (!g) {
                        return false
                    }

                    for (const element of g) {
                        if (!hasClearText[element]) {
                            return false
                        }
                    }
                    return true
                },
                isDecrypted: function (name) {
                    return hasClearText[name] === true
                },
                isDecrypting: function (name) {
                    return isDecrypting[name] === true
                },
                isAnyDecrypting: function () {
                    for (const element of allNames) {
                        if (isDecrypting[element]) {
                            return true
                        }
                    }
                    return false
                },
                getClearText: function (name) {
                    return clearText[name]
                },
                configure: function (cfg) {
                    config = cfg
                }
            }
        })
        .directive('encryptedBlock', ['$q', 'ntechEncryptionService', function ($q, ntechEncryptionService) {
            return {
                scope: {
                    item: '=itemName',
                    hideUi: '=',
                    groupName: '='
                },
                templateUrl: 'encryptedBlock',
                transclude: true,
                link: function (scope, el, attrs, ctrl, transclude) {
                    let crypto = ntechEncryptionService
                    crypto.registerName(scope.item, scope.groupName)
                    scope.decrypt = function (names) {
                        return crypto.decrypt(scope.item)
                    }
                    scope.isDecrypted = function () {
                        return !!(crypto.isDecrypted(scope.item));
                    }
                    scope.isDecrypting = function () {
                        return !!(crypto.isDecrypting(scope.item));
                    }
                    scope.isAnyDecrypting = function () {
                        return crypto.isAnyDecrypting()
                    }
                }
            };
        }])
        .directive('customValidate', function () {
            return {
                restrict: 'A',
                require: '?ngModel',
                scope: {isValid: '&customValidate'},
                link: function (scope, elm, attr, ctrl) {
                    if (!ctrl) return

                    let isValid = scope.isValid();

                    ctrl.$parsers.push(function (modelValue, viewValue) {
                        if (isValid(modelValue)) {
                            ctrl.$setValidity('custom', true)
                        } else {
                            ctrl.$setValidity('custom', false)
                        }

                        return modelValue
                    })
                }
            }
        })
        .directive('customValidateAsync', function () {
            return {
                restrict: 'A',
                require: '?ngModel',
                scope: {isValid: '&customValidateAsync'},
                link: function (scope, elm, attr, ctrl) {
                    if (!ctrl) return

                    ctrl.$asyncValidators.isValidCustomAsync = scope.isValid();
                }
            }
        })
        .directive('money', ['$filter', function ($filter) {
            return {
                require: 'ngModel',
                scope: {isValid: '&moneyValidate'},
                link: function (scope, ele, attr, ctrl) {
                    if (!ctrl) return

                    let isValid = scope.isValid()
                    if (!isValid) {
                        isValid = ntech.forms.isValidPositiveDecimal
                    }

                    ctrl.$parsers.unshift(function (viewValue) {
                        let v
                        if (!ntech.forms.isNullOrWhitespace(viewValue)) {
                            v = ntech.forms.replaceAll(viewValue.toString().replace(/\s/g, ""), ",", ".")
                        } else {
                            v = viewValue
                        }
                        if (isValid(v)) {
                            ctrl.$setValidity('money', true)
                            return parseFloat(v)
                        } else {
                            ctrl.$setValidity('money', false)
                            return null
                        }
                    })

                    ctrl.$formatters.unshift(function (value) {
                        return $filter('number')(value, 2)
                    })
                }
            }
        }])
        .directive('bootstrapValidation', function () {
            return {
                restrict: "A",
                require: 'form',
                scope: {errorTarget: '=bootstrapValidation'},
                link: function (scope, element, attrs, formCtrl) {
                    element.find('.form-group').each(function () {
                        let t = 'formgroup'
                        if (scope.errorTarget === 'parent') {
                            t = 'parent'
                        }
                        var formGroup = $(this)
                        var inputs = formGroup.find('input[ng-model],textarea[ng-model],select[ng-model]')

                        if (inputs.length > 0) {
                            inputs.each(function () {
                                var input = $(this)
                                scope.$watch(function () {
                                    return input.controller('ngModel').$invalid && (!input.controller('ngModel').$pristine || input.controller('ngModel').$$parentForm.$submitted)
                                }, function (isInvalid) {
                                    if (t === 'parent') {
                                        input.parent().toggleClass('has-error', isInvalid)
                                    } else {
                                        formGroup.toggleClass('has-error', isInvalid)
                                    }
                                })
                            })

                            inputs.each(function () {
                                var input = $(this)
                                scope.$watch(function () {
                                    return input.controller('ngModel').$valid && (!input.controller('ngModel').$pristine || input.controller('ngModel').$$parentForm.$submitted)
                                }, function (isValid) {
                                    if (t === 'parent') {
                                        input.parent().toggleClass('has-success', isValid)
                                    } else {
                                        formGroup.toggleClass('has-success', isValid)
                                    }
                                })
                            })

                        }
                    })
                }
            }
        })
}(window, document))