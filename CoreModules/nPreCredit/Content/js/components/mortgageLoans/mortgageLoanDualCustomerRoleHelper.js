var MortgageLoanDualCustomerRoleHelperNs;
(function (MortgageLoanDualCustomerRoleHelperNs) {
    function getApplicationCustomerRolesByCustomerId(applicationNr, apiClient) {
        var rolesByCustomerId = {};
        return apiClient.fetchApplicationInfoWithApplicants(applicationNr).then(function (ai2) {
            for (var _i = 0, _a = Object.keys(ai2.CustomerIdByApplicantNr); _i < _a.length; _i++) {
                var applicantNr = _a[_i];
                rolesByCustomerId[ai2.CustomerIdByApplicantNr[applicantNr]] = ['Applicant'];
            }
            return ComplexApplicationListHelper.getAllCustomerIds(applicationNr, ['ApplicationObject'], apiClient, rolesByCustomerId).then(function (listCustomers) {
                var customerIds = NTechPreCreditApi.getNumberDictionarKeys(listCustomers);
                return apiClient.fetchCustomerItemsBulk(customerIds, ['firstName', 'birthDate']).then(function (customerItems) {
                    var firstNameAndBirthDateByCustomerId = {};
                    for (var _i = 0, customerIds_1 = customerIds; _i < customerIds_1.length; _i++) {
                        var customerId = customerIds_1[_i];
                        firstNameAndBirthDateByCustomerId[customerId] = {
                            birthDate: customerItems[customerId]['birthDate'],
                            firstName: customerItems[customerId]['firstName'],
                        };
                    }
                    return {
                        customerIds: customerIds,
                        rolesByCustomerId: listCustomers,
                        customerIdByApplicantNr: ai2.CustomerIdByApplicantNr,
                        firstNameAndBirthDateByCustomerId: firstNameAndBirthDateByCustomerId
                    };
                });
            });
        });
    }
    MortgageLoanDualCustomerRoleHelperNs.getApplicationCustomerRolesByCustomerId = getApplicationCustomerRolesByCustomerId;
})(MortgageLoanDualCustomerRoleHelperNs || (MortgageLoanDualCustomerRoleHelperNs = {}));
