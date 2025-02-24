var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
ntech.angular.setupTranslation(app);
var NewDocumentCheckCtr = /** @class */ (function () {
    function NewDocumentCheckCtr($http, $q, $timeout, $filter, $scope) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        this.$filter = $filter;
        this.$scope = $scope;
        window.scope = this; //for console debugging
        this.initialData = initialData;
        this.isViewMode = this.initialData.isViewMode;
        this.hasCoApplicant = !!this.initialData.applicant2;
        this.setIncomeView(this.initialData.applicant1.confirmedIncome, this.hasCoApplicant ? this.initialData.applicant2.confirmedIncome : null);
        this.applicant1 = {
            attachedFileName: null,
            fileupload: new NtechAngularFileUpload.FileUploadHelper(document.getElementById('fileupload1'), document.getElementById('fileuploadform1'), $scope, $q),
            documents: this.initialData.applicant1.documents
        };
        this.applicant2 = {
            attachedFileName: null,
            fileupload: new NtechAngularFileUpload.FileUploadHelper(document.getElementById('fileupload2'), document.getElementById('fileuploadform2'), $scope, $q),
            documents: this.hasCoApplicant ? this.initialData.applicant2.documents : null
        };
        this.rejectionReasons = [{ isChecked: false, text: 'Income to low' }, { isChecked: false, text: 'Incomplete documents' }];
        this.otherRejectionReason = '';
        var _loop_1 = function (applicantNr) {
            var a = this_1.applicant(applicantNr);
            a.fileupload.addFileAttachedListener(function (filenames) {
                if (filenames.length == 0) {
                    a.attachedFileName = null;
                }
                else if (filenames.length == 1) {
                    a.attachedFileName = filenames[0];
                }
                else {
                    a.attachedFileName = 'Error - multiple files selected!';
                }
            });
        };
        var this_1 = this;
        for (var _i = 0, _a = [1, 2]; _i < _a.length; _i++) {
            var applicantNr = _a[_i];
            _loop_1(applicantNr);
        }
    }
    NewDocumentCheckCtr.prototype.onBack = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q, { applicationNr: initialData.applicationNr }, NavigationTargetHelper.NavigationTargetCode.UnsecuredLoanApplication);
    };
    NewDocumentCheckCtr.prototype.setIncomeView = function (income1, income2) {
        this.incomeView = {
            confirmedIncome1: income1,
            confirmedIncome2: this.hasCoApplicant ? income2 : null
        };
    };
    NewDocumentCheckCtr.prototype.applicant = function (applicantNr) {
        if (applicantNr == 1) {
            return this.applicant1;
        }
        else if (applicantNr == 2) {
            return this.applicant2;
        }
        else {
            return null;
        }
    };
    NewDocumentCheckCtr.prototype.chooseFile = function (applicantNr, evt) {
        if (evt) {
            evt.preventDefault();
        }
        this.applicant(applicantNr).fileupload.showFilePicker();
    };
    NewDocumentCheckCtr.prototype.saveChoosenFile = function (applicantNr, evt) {
        var _this = this;
        this.isLoading = true;
        this.applicant(applicantNr).fileupload.loadSingleAttachedFileAsDataUrl().then(function (result) {
            _this.$http.post(_this.initialData.attachDocumentUrl, {
                applicationNr: _this.initialData.applicationNr,
                applicantNr: applicantNr,
                dataUrl: result.dataUrl,
                filename: result.filename
            }).then(function (result) {
                _this.applicant(applicantNr).documents = result.data;
                _this.applicant(applicantNr).attachedFileName = null;
                _this.applicant(applicantNr).fileupload.reset();
                _this.isLoading = false;
            }, function (errorResult) {
                toastr.error(errorResult.statusText);
                _this.applicant(applicantNr).attachedFileName = null;
                _this.applicant(applicantNr).fileupload.reset();
                _this.isLoading = false;
            });
        }, function (err) {
            toastr.error(err);
            _this.applicant(applicantNr).fileupload.reset();
            _this.isLoading = false;
        });
    };
    NewDocumentCheckCtr.prototype.beginEditIncome = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        this.incomeEdit = {
            confirmedIncome1: this.formatIncomeForEdit(this.incomeView.confirmedIncome1),
            confirmedIncome2: this.hasCoApplicant ? this.formatIncomeForEdit(this.incomeView.confirmedIncome2) : ''
        };
    };
    NewDocumentCheckCtr.prototype.cancelEditIncome = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        this.incomeEdit = null;
    };
    NewDocumentCheckCtr.prototype.parseDecimalOrNull = function (n) {
        if (ntech.forms.isNullOrWhitespace(n) || !this.isValidPositiveDecimal(n)) {
            return null;
        }
        return parseFloat(n.replace(',', '.'));
    };
    NewDocumentCheckCtr.prototype.isValidPositiveDecimal = function (value) {
        return ntech.forms.isValidPositiveDecimal(value);
    };
    NewDocumentCheckCtr.prototype.formatIncomeForDisplay = function (income) {
        if (income == null) {
            return '-';
        }
        else {
            return this.$filter('currency')(income);
        }
    };
    NewDocumentCheckCtr.prototype.formatIncomeForEdit = function (income) {
        if (income == null) {
            return '';
        }
        else {
            return income.toString();
        }
    };
    NewDocumentCheckCtr.prototype.confirmEditIncome = function () {
        var _this = this;
        this.isLoading = true;
        var request = {
            applicationNr: this.initialData.applicationNr,
            confirmedIncome1: this.incomeEdit.confirmedIncome1,
            confirmedIncome2: this.incomeEdit.confirmedIncome2
        };
        var tmpModel = {
            confirmedIncome1: this.incomeEdit.confirmedIncome1,
            confirmedIncome2: this.incomeEdit.confirmedIncome2
        };
        this.$http.post(this.initialData.setConfirmedIncomeUrl, request).then(function (response) {
            _this.isLoading = false;
            _this.incomeEdit = null;
            _this.setIncomeView(response.data.confirmedIncome1, response.data.confirmedIncome2);
        }, function (response) {
            toastr.error(response.statusText, 'Failed');
            _this.isLoading = false;
        });
    };
    NewDocumentCheckCtr.prototype.acceptDocumentCheck = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http.post(this.initialData.acceptUrl, { applicationNr: this.initialData.applicationNr }).then(function (response) {
            _this.onBack();
        }, function (err) {
            _this.isLoading = false;
            toastr.error(err.statusText);
        });
    };
    NewDocumentCheckCtr.prototype.rejectDocumentCheck = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        var reasons = [];
        for (var _i = 0, _a = this.rejectionReasons; _i < _a.length; _i++) {
            var r = _a[_i];
            if (r.isChecked) {
                reasons.push(r.text);
            }
        }
        if (!ntech.forms.isNullOrWhitespace(this.otherRejectionReason)) {
            reasons.push('other: ' + this.otherRejectionReason);
        }
        this.isLoading = true;
        this.$http.post(this.initialData.rejectUrl, { applicationNr: this.initialData.applicationNr, rejectionReasons: reasons }).then(function (response) {
            _this.onBack();
        }, function (err) {
            _this.isLoading = false;
            toastr.error(err.statusText);
        });
    };
    NewDocumentCheckCtr.prototype.isRejectDocumentCheckAllowed = function () {
        for (var _i = 0, _a = this.rejectionReasons; _i < _a.length; _i++) {
            var r = _a[_i];
            if (r.isChecked) {
                return true;
            }
        }
        if (!ntech.forms.isNullOrWhitespace(this.otherRejectionReason)) {
            return true;
        }
        return false;
    };
    NewDocumentCheckCtr.$inject = ['$http', '$q', '$timeout', '$filter', '$scope'];
    return NewDocumentCheckCtr;
}());
app.controller('newDocumentCheckCtr', NewDocumentCheckCtr);
var NewDocumentCheckNs;
(function (NewDocumentCheckNs) {
    var SetConfirmedIncomeRequest = /** @class */ (function () {
        function SetConfirmedIncomeRequest() {
        }
        return SetConfirmedIncomeRequest;
    }());
    NewDocumentCheckNs.SetConfirmedIncomeRequest = SetConfirmedIncomeRequest;
    var IncomeViewModel = /** @class */ (function () {
        function IncomeViewModel() {
        }
        return IncomeViewModel;
    }());
    NewDocumentCheckNs.IncomeViewModel = IncomeViewModel;
    var IncomeEditModel = /** @class */ (function () {
        function IncomeEditModel() {
        }
        return IncomeEditModel;
    }());
    NewDocumentCheckNs.IncomeEditModel = IncomeEditModel;
    var ApplicantViewModel = /** @class */ (function () {
        function ApplicantViewModel() {
        }
        return ApplicantViewModel;
    }());
    NewDocumentCheckNs.ApplicantViewModel = ApplicantViewModel;
    var RejectionReasonModel = /** @class */ (function () {
        function RejectionReasonModel() {
        }
        return RejectionReasonModel;
    }());
    NewDocumentCheckNs.RejectionReasonModel = RejectionReasonModel;
})(NewDocumentCheckNs || (NewDocumentCheckNs = {}));
