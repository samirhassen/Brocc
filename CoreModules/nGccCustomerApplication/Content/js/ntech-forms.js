(function (window, document) {
    'use strict';

    angular
        .module('ntech.forms', [])
        .directive('customValidate', function () {
            return {
                restrict: 'A',
                require: '?ngModel',
                scope: { isValid: '&customValidate' },
                link: function (scope, elm, attr, ctrl) {
                    if (!ctrl) return

                    var isValid = scope.isValid();

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
}(window, document))