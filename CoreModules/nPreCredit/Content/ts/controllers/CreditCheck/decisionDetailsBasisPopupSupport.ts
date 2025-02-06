var decisionDetailsBasisPopupSupport = (function ($http, $scope) {
    function setup<T>(items: T[]) : any[] {
        if (!items || items.length === 0) {
            return null
        }
        let result = []
        angular.forEach(items, (v) => {
            result.push({ value: v })
        })
        return result
    }

    function createDecisionBasisDetails(decisionModel) {
        let r = decisionModel.recommendation

        let decisionDetailsBasis = {
            minimumDemandsItems: setup(r.RejectionsDebugItems),
            leftToLiveOnItems: setup(r.LeftToLiveOnDebugItems),
            satLeftToLiveOnItems: setup(r.SatLeftToLiveOnDebugItems),
            scoreItems: setup(r.ScoreDebugItems),
            riskGroupItems: setup(r.RiskGroupDebugItems),
            creditReportsUsed: null,
            autoFollowRejectionItems: setup(r.AutoFollowRejectionDebugItems),
            scoringDataModelDetailsInitialData: (null as SimpleTableComponentNs.InitialData)
        }
        
        var addLocalCreditReport = function (m, applicantNr) {
            //Handles legacy credit reports that are stored directly in the decision
            var c = { IsStoredLocally: true, ApplicantNr: applicantNr, ProviderName: 'BisnodeFi', LocallyStoredItems: [] } //Always bisnode since we dont store credit report like this anymore
            angular.forEach(m, function (v, k) {
                c.LocallyStoredItems.push({ Name: k, Value: v })
            })
            decisionDetailsBasis.creditReportsUsed = decisionDetailsBasis.creditReportsUsed || []
            decisionDetailsBasis.creditReportsUsed.push(c)
        }
        if (decisionModel.creditReportsUsed) {
            decisionDetailsBasis.creditReportsUsed = angular.copy(decisionModel.creditReportsUsed)
        } else {
            if (decisionModel.creditreport1) {
                addLocalCreditReport(decisionModel.creditreport1, 1)
            }
            if (decisionModel.creditreport2) {
                addLocalCreditReport(decisionModel.creditreport2, 2)
            }
        }
        if (decisionDetailsBasis.creditReportsUsed) {
            for (let c of decisionDetailsBasis.creditReportsUsed) {
                c.onExpanded = (service: ToggleBlockComponentNs.Service) => {
                    unlockCreditReportItems(c, null)
                    service.setLocked(false)
                }
            }
        }

        if (decisionModel.recommendation && decisionModel.recommendation.ScoringData) {
            let data = decisionModel.recommendation.ScoringData as NTechPreCreditApi.ScoringDataModelFlat

            decisionDetailsBasis.scoringDataModelDetailsInitialData = {
                columns: [{ className: 'col-xs-5', labelText: 'Name' }, { className: 'col-xs-2', labelText: 'Level' }, { className: 'col-xs-5', labelText: 'Value' }],
                tableRows: NTechPreCreditApi.ScoringDataModelFlat.toDataTable(data)
            }
        }

        return decisionDetailsBasis
    }

    function unlockCreditReportItems(creditReportUsed, evt) {
        if (evt) {
            evt.preventDefault()
        }
        if (creditReportUsed && !creditReportUsed.IsStoredLocally) {
            $scope.isLoading = true
            $http({
                method: 'POST',
                url: '/api/CreditDecisionDetails/FetchCreditReport',
                data: {
                    creditReportId: creditReportUsed.CreditReportId
                }
            }).then(function successCallback(response) {
                $scope.isLoading = false
                if (response.data.creditReport) {
                    if (creditReportUsed.ProviderName === 'SatFi') {
                        creditReportUsed.rawCreditReportItems = response.data.creditReport.Items
                        creditReportUsed.creditReportItems = createSatFiCreditReportItems(response.data.creditReport.Items)
                    } else {
                        creditReportUsed.creditReportItems = response.data.creditReport.Items
                    }                    
                } else {
                    toastr.warning('No credit reports available')
                }                
                }, function errorCallback(response) {
                    $scope.isLoading = false
                toastr.error(response.statusText)
            })
        } else if (creditReportUsed && creditReportUsed.IsStoredLocally) {
            creditReportUsed.creditReportItems = creditReportUsed.LocallyStoredItems
        } else {
            toastr.warning('No credit reports available')
        }
    }

    function createSatFiCreditReportItems(items: Array<any>) {
        var newItems = []
        var getVal = function (name) {
            var v = _.findWhere(items, { Name: name })
            if (v) {
                return v.Value
            } else {
                return null
            }
        }
        var create2Row = function (desc, name) {
            return {
                Col1: desc,
                Col2: getVal(name),
                ColCount : 2
            }
        }
        var create4Row = function (desc, name1, name2, name3) {
            var v = {
                Col1: desc,
                ColCount: 4,
                Col2: null,
                Col3: null,
                Col4: null
            }
            if (name1) {
                v.Col2 = getVal(name1)
            }
            if (name2) {
                v.Col3 = getVal(name2)
            }
            if (name3) {
                v.Col4 = getVal(name3)
            }
            return v
        }

        newItems.push(create4Row('Credit-granting comp.', 'c15', null, null))
        newItems.push(create4Row('Granted credits 12 mth', 'h15', null, null))
        newItems.push(create4Row('Most recent credit', 'k11', null, null))
        newItems.push(create4Row('Oldest credit', 'k12', null, null))
        newItems.push({ ColCount: 1 })
        newItems.push({ ColCount: 4, Col2: 'Number', Col3: 'Euro', Col4: 'Limit' })
        newItems.push(create4Row('Unsecure loans', 'd11', 'd12', null))
        newItems.push(create4Row('Secured credits', 'e11', 'e12', null))
        newItems.push(create4Row('Card credits', 'f11', 'f12', 'f13'))
        newItems.push(create4Row('TOTAL', 'count', 'c01', null))
        newItems.push({ ColCount: 1 })
        newItems.push(create4Row('Over 60 days unpaid', null, 'c03', null))
        newItems.push(create4Row('Total monthly payment', null, 'c04', null))
        
        return newItems
    }

    return {
        createDecisionBasisDetails: createDecisionBasisDetails,
        unlockCreditReportItems: unlockCreditReportItems
    }
})