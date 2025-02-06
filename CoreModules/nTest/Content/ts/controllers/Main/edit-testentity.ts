var app = angular.module('app', []);
app.controller('ctr', ['$scope', '$http', '$q', ($scope: EditEntityNs.Scope, $http : ng.IHttpService, $q : ng.IQService) => {
    let apiClient = new NTechTestApi.ApiClient(msg => toastr.error(msg), $http, $q, x => $scope.isLoading = x)
    $scope.pickModel = {
        nrType: 'Person',
        nr: ''
    }
    
    $scope.pick = () => {
        if ($scope.pickModel.nrType !== 'Company') {
            apiClient.getTestPerson({ civicRegNr: $scope.pickModel.nr, civicRegNrCountry: initialData.baseCountry, requestedProperties: ['*'] }).then(x => {
                $scope.editModel = {
                    nr: $scope.pickModel.nr,
                    nrType: $scope.pickModel.nrType,
                    entity: x,
                    cacheMode: 'Preserve'
                }
                $scope.pickModel = null
            })
        } else {
            apiClient.getTestCompany({ orgnr: $scope.pickModel.nr, orgnrCountry: initialData.baseCountry, generateIfNotExists: false }).then(x => {
                $scope.editModel = {
                    nr: $scope.pickModel.nr,
                    nrType: $scope.pickModel.nrType,
                    entity: x,
                    cacheMode: 'Preserve'
                }
                $scope.pickModel = null
            })
        }
    }

    $scope.save = () => {
        var p = angular.copy($scope.editModel.entity)
        if ($scope.editModel.nrType !== 'Company') {
            p['civicRegNr'] = $scope.editModel.nr
            p['civicRegNrCountry'] = initialData.baseCountry
            apiClient.createOrUpdateTestPersons([JSON.stringify(p)], $scope.editModel.cacheMode === 'Clear').then(x => {
                $scope.editModel = null
                $scope.pickModel = {
                    nrType: 'Person',
                    nr: ''
                }
            })
        } else {
            p['orgnr'] = $scope.editModel.nr
            p['orgnrCountry'] = initialData.baseCountry
            apiClient.createOrUpdateTestCompanies([JSON.stringify(p)], $scope.editModel.cacheMode === 'Clear').then(x => {
                $scope.editModel = null
                $scope.pickModel = {
                    nrType: 'Company',
                    nr: ''
                }
            })
        }
    }

    $scope.editNames =  () => {
        if (!$scope.editModel) {
            return null
        }
        return Object.keys($scope.editModel.entity)
    }

    window.scope = $scope
}])

module EditEntityNs {
    export interface Scope extends ng.IScope {
        pickModel: { nrType: string, nr: string }
        editModel: { nrType: string, nr: string, entity: NTechTestApi.IStringDictionary<string>, cacheMode: string }
        isCompany: () => boolean
        pick: () => void
        save: () => void
        isLoading: boolean
        editNames: () => string[]
    }
}