var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
ntech.angular.setupTranslation(app);
app.controller('ctr', ['$scope', '$http', '$q', '$translate', 'modalDialogService', function ($scope, $http, $q, $translate, modalDialogService) {
        var apiClient = new NTechPreCreditApi.ApiClient(function (err) {
            toastr.error(err);
        }, $http, $q);
        $scope.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, $q, { applicationNr: initialData.applicationNr }, NavigationTargetHelper.NavigationTargetCode.UnsecuredLoanApplication);
        };
        $scope.petrusDialogId = modalDialogService.generateDialogId();
        $scope.nrOfApplicants = initialData.nrOfApplicants;
        $scope.isAccepted = initialData.isAccepted;
        $scope.currentDecisionId = initialData.currentDecisionId;
        $scope.decisionDate = initialData.decisionDate;
        $scope.decisions = initialData.decisions;
        $scope.applicationNr = initialData.applicationNr;
        $scope.decisionModel = JSON.parse(initialData.decisionModelJson);
        $scope.satUi = {};
        var reportsModel = [{ applicationNrAndApplicantNr: { applicationNr: initialData.applicationNr, applicantNr: 1 }, customerId: null, creditReportProviderName: initialData.creditReportProviderName, listProviders: initialData.listCreditReportProviders }];
        if ($scope.nrOfApplicants > 1) {
            reportsModel.push({ applicationNrAndApplicantNr: { applicationNr: initialData.applicationNr, applicantNr: 2 }, customerId: null, creditReportProviderName: initialData.creditReportProviderName, listProviders: initialData.listCreditReportProviders });
        }
        $scope.customerCreditReports = reportsModel;
        $scope.changedCreditApplicationItems = initialData.changedCreditApplicationItems;
        $scope.urlToHere = initialData.urlToHere;
        $scope.maxPauseItem = initialData.maxPauseItem;
        $scope.isPetrusOnly = function () {
            return $scope.decisionModel && $scope.decisionModel && $scope.decisionModel.recommendation && $scope.decisionModel.recommendation.PetrusVersion >= 2;
        };
        $scope.toExternalRejectionReasons = function (internalRejectionReasons) {
            if (!internalRejectionReasons) {
                return [];
            }
            var tmp = {};
            angular.forEach(internalRejectionReasons, function (value) {
                if (initialData.scoringRuleToRejectionReasonMapping[value]) {
                    tmp[initialData.scoringRuleToRejectionReasonMapping[value]] = true;
                }
                else {
                    tmp[value] = true;
                }
            });
            var r = [];
            angular.forEach(tmp, function (value, key) {
                r.push(key);
            });
            return r;
        };
        $scope.getManualControlReasonDisplayText = function (code) {
            if (initialData.manualControlReasonToDisplayTextMapping) {
                var v = initialData.manualControlReasonToDisplayTextMapping[code];
                if (v) {
                    return v;
                }
            }
            return code;
        };
        $scope.headerClassFromStatus = function (isAccepted) {
            return { 'text-success': isAccepted, 'text-danger': !isAccepted };
        };
        $scope.iconClassFromStatus = function (isAccepted) {
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': !isAccepted, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': !isAccepted };
        };
        $scope.showValue = function (groupName, name, evt) {
            if (evt) {
                evt.preventDefault();
            }
            window.location.href = '/CreditApplicationEdit/ViewValue?&applicationNr=' + $scope.applicationNr + '&groupName=' + groupName + '&name=' + name + '&backTarget=UnsecuredLoanCreditCheckViewInitial';
        };
        $scope.hasCo = function () {
            return $scope.decisionModel.application.nrOfApplicants > 1;
        };
        $scope.isValueChanged = function (groupName, name) {
            var isValueChanged = false;
            angular.forEach($scope.changedCreditApplicationItems, function (item) {
                if (item.Item1 === groupName && item.Item2 === name) {
                    isValueChanged = true;
                }
            });
            return isValueChanged;
        };
        $scope.appliedForRepaymentTimeInMonths = function () {
            var a = $scope.decisionModel.application.application;
            if (a.repaymentTimeInMonths) {
                return a.repaymentTimeInMonths;
            }
            else if (a.repaymentTimeInYears) {
                return (12 * parseInt(a.repaymentTimeInYears)).toString();
            }
            else {
                return '<None>';
            }
        };
        $scope.getDisplayRejectionReason = function (e) {
            if (initialData.rejectionReasonToDisplayNameMapping[e]) {
                return initialData.rejectionReasonToDisplayNameMapping[e];
            }
            else {
                return e;
            }
        };
        $scope.getDisplayRejectionReasons = function (reasons) {
            var displayNames = [];
            angular.forEach(reasons, function (e) {
                displayNames.push($scope.getDisplayRejectionReason(e));
            });
            return displayNames;
        };
        $scope.navigateTo = function (url) {
            document.location.href = url;
        };
        $scope.getCreditUrl = function (creditNr) {
            return initialData.creditUrlPattern.replace('NNN', creditNr);
        };
        function displayOtherApplicantsWithSameAddress() {
            if ($scope.decisionModel.applicantsOnOtherApplicationsWithSameAddress) {
                var hits = [];
                angular.forEach($scope.decisionModel.applicantsOnOtherApplicationsWithSameAddress, function (h) {
                    angular.forEach(h.ApplicationNrs, function (n) {
                        hits.push({ applicantNr: h.ApplicantNr, applicationNr: n });
                    });
                });
                $scope.otherApplicationsWithSameAddress = hits;
            }
        }
        displayOtherApplicantsWithSameAddress();
        $scope.updateSatUi = function (applicantNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            var satReport = _.find($scope.decisionModel.creditReportsUsed, function (x) { return x.ProviderName === 'SatFi' && x.ApplicantNr === applicantNr; });
            if (satReport) {
                $scope.satUi['applicant' + applicantNr] = { isLoading: true };
                var addReportItem = function (items, name, target) {
                    var i = _.find(items, function (x) { return x.Name === name; });
                    if (i) {
                        target[i.Name] = i.Value;
                    }
                };
                $http({
                    method: 'POST',
                    url: initialData.fetchSatReportUrl,
                    data: {
                        creditReportId: satReport.CreditReportId,
                        requestedCreditReportFields: ['c01', 'c03', 'c04', 'count']
                    }
                }).then(function successCallback(response) {
                    if (response.data.satReport) {
                        var result = response.data.satReport;
                        var tmp = {
                            report: {
                                date: result.RequestDate,
                                fields: {}
                            }
                        };
                        addReportItem(result.Items, 'c01', tmp.report.fields);
                        addReportItem(result.Items, 'c03', tmp.report.fields);
                        addReportItem(result.Items, 'c04', tmp.report.fields);
                        addReportItem(result.Items, 'count', tmp.report.fields);
                        $scope.satUi['applicant' + applicantNr] = tmp;
                    }
                    else {
                        $scope.satUi['applicant' + applicantNr] = {};
                    }
                }, function errorCallback(response) {
                    $scope.satUi['applicant' + applicantNr] = {};
                    toastr.error(response.statusText);
                });
            }
            else {
                $scope.satUi['applicant' + applicantNr] = {};
            }
        };
        if ($scope.decisionModel && $scope.decisionModel.creditReportsUsed) {
            $scope.updateSatUi(1);
            if ($scope.hasCo()) {
                $scope.updateSatUi(2);
            }
        }
        $scope.unlockBusinessConnectionText = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.unlockHasBusinessConnectionForViewUrl,
                data: {
                    applicationNr: $scope.applicationNr
                }
            }).then(function successCallback(response) {
                $scope.isLoading = false;
                $scope.isHasBusinessConnectionLoaded = true;
                $scope.hasBusinessConnection = response.data.hasBusinessConnection;
            }, function errorCallback(response) {
                $scope.isLoading = false;
                toastr.error(response.statusText);
            });
        };
        $scope.unlockImmigrationDateText = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.unlockImmigrationDateForViewUrl,
                data: {
                    applicationNr: $scope.applicationNr
                }
            }).then(function successCallback(response) {
                $scope.isLoading = false;
                $scope.isImmigrationDateLoaded = true;
                $scope.immigrationDateText = response.data.immigrationDateText;
            }, function errorCallback(response) {
                $scope.isLoading = false;
                toastr.error(response.statusText);
            });
        };
        var dds = decisionDetailsBasisPopupSupport($http, $scope);
        $scope.decisionDetailsBasis = dds.createDecisionBasisDetails($scope.decisionModel);
        $scope.unlockCreditReportItems = dds.unlockCreditReportItems;
        $scope.wasPetrusUsed = function () {
            var _a, _b, _c, _d, _e, _f;
            var petrusOneUsed = !!((_d = (_c = (_b = (_a = $scope === null || $scope === void 0 ? void 0 : $scope.decisionModel) === null || _a === void 0 ? void 0 : _a.recommendation) === null || _b === void 0 ? void 0 : _b.ScoringData) === null || _c === void 0 ? void 0 : _c.Items) === null || _d === void 0 ? void 0 : _d.find(function (x) { return (x === null || x === void 0 ? void 0 : x.Name) === 'isAcceptedByPetrus'; }));
            var petrusTwoUsed = !!((_f = (_e = $scope === null || $scope === void 0 ? void 0 : $scope.decisionModel) === null || _e === void 0 ? void 0 : _e.recommendation) === null || _f === void 0 ? void 0 : _f.PetrusApplicationId);
            return petrusOneUsed || petrusTwoUsed;
        };
        $scope.showPetrusLog = function () {
            var apiClient = new NTechPreCreditApi.ApiClient(null, $http, $q);
            apiClient.loggingContext = 'viewCreditCheck';
            apiClient.rejectWithFullError = true;
            apiClient.postUsingApiGateway('NTechHost', 'Api/PreCredit/Petrus/Audit-Trail', { applicationId: $scope.decisionModel.recommendation.PetrusApplicationId }).then(function (x) {
                $scope.petrusLogXml = x;
                modalDialogService.openDialog($scope.petrusDialogId);
            }, function (err) {
                var _a;
                if (((_a = err === null || err === void 0 ? void 0 : err.data) === null || _a === void 0 ? void 0 : _a.errorCode) === 'auditTrailMissing') {
                    $scope.petrusLogXml = err.data.errorMessage;
                    modalDialogService.openDialog($scope.petrusDialogId);
                }
                else {
                    toastr.error(err === null || err === void 0 ? void 0 : err.statusText);
                }
                console.log(err);
            });
        };
        window['scope'] = $scope;
    }]);
