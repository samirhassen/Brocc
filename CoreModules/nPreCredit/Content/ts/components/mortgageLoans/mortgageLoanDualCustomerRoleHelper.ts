namespace MortgageLoanDualCustomerRoleHelperNs {
    export function getApplicationCustomerRolesByCustomerId(applicationNr: string, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<ApplicationCustomerRolesResponse> {
        let rolesByCustomerId: NTechPreCreditApi.INumberDictionary<string[]> = {}
        return apiClient.fetchApplicationInfoWithApplicants(applicationNr).then(ai2 => {
            for (let applicantNr of Object.keys(ai2.CustomerIdByApplicantNr)) {
                rolesByCustomerId[ai2.CustomerIdByApplicantNr[applicantNr]] = ['Applicant']
            }
            return ComplexApplicationListHelper.getAllCustomerIds(applicationNr, ['ApplicationObject'], apiClient, rolesByCustomerId).then(listCustomers => {
                let customerIds = NTechPreCreditApi.getNumberDictionarKeys(listCustomers)
                return apiClient.fetchCustomerItemsBulk(customerIds, ['firstName', 'birthDate']).then(customerItems => {
                    let firstNameAndBirthDateByCustomerId: NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string }> = {}
                    for (let customerId of customerIds) {
                        firstNameAndBirthDateByCustomerId[customerId] = {
                            birthDate: customerItems[customerId]['birthDate'],
                            firstName: customerItems[customerId]['firstName'],
                        }
                    }
                    return {
                        customerIds: customerIds,
                        rolesByCustomerId: listCustomers,
                        customerIdByApplicantNr: ai2.CustomerIdByApplicantNr,
                        firstNameAndBirthDateByCustomerId: firstNameAndBirthDateByCustomerId
                    }
                })
            })
        })
    }

    export interface ApplicationCustomerRolesResponse {
        customerIds: number[],
        rolesByCustomerId: NTechPreCreditApi.INumberDictionary<string[]>
        customerIdByApplicantNr: NTechPreCreditApi.INumberDictionary<number>
        firstNameAndBirthDateByCustomerId: NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string }>
    }
}