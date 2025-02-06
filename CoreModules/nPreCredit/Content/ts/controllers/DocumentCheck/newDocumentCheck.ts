var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components'])

ntech.angular.setupTranslation(app)

class NewDocumentCheckCtr {
    static $inject = ['$http', '$q', '$timeout', '$filter', '$scope']
    constructor(
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService,
        private $filter: ng.IFilterService,
        private $scope: ng.IScope
    ) {
        window.scope = this; //for console debugging

        this.initialData = initialData;
        this.isViewMode = this.initialData.isViewMode;
        this.hasCoApplicant = !!this.initialData.applicant2;
        this.setIncomeView(this.initialData.applicant1.confirmedIncome, this.hasCoApplicant ? this.initialData.applicant2.confirmedIncome : null);

        this.applicant1 = {
            attachedFileName: null,
            fileupload: new NtechAngularFileUpload.FileUploadHelper((<HTMLInputElement>document.getElementById('fileupload1')),
                (<HTMLFormElement>document.getElementById('fileuploadform1')),
                $scope, $q),
            documents: this.initialData.applicant1.documents
        }

        this.applicant2 = {
            attachedFileName: null,
            fileupload: new NtechAngularFileUpload.FileUploadHelper((<HTMLInputElement>document.getElementById('fileupload2')),
                (<HTMLFormElement>document.getElementById('fileuploadform2')),
                $scope, $q),
            documents: this.hasCoApplicant ? this.initialData.applicant2.documents : null
        }

        this.rejectionReasons = [{ isChecked: false, text: 'Income to low' }, { isChecked: false, text: 'Incomplete documents' }]
        this.otherRejectionReason = ''

        for (let applicantNr of [1, 2]) {
            let a = this.applicant(applicantNr);
            a.fileupload.addFileAttachedListener(filenames => {
                if (filenames.length == 0) {
                    a.attachedFileName = null;
                } else if (filenames.length == 1) {
                    a.attachedFileName = filenames[0];
                } else {
                    a.attachedFileName = 'Error - multiple files selected!';
                }
            });
        }
    }

    onBack(evt?: Event) {
        if (evt) {
            evt.preventDefault()
        }
        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q, { applicationNr: initialData.applicationNr }, NavigationTargetHelper.NavigationTargetCode.UnsecuredLoanApplication)
    }

    setIncomeView(income1: number, income2: number) {
        this.incomeView = {
            confirmedIncome1: income1,
            confirmedIncome2: this.hasCoApplicant ? income2 : null
        };
    }

    hasCoApplicant: boolean
    initialData: NewDocumentCheckNs.IInitialData
    isLoading: boolean
    apiClient: NTechPreCreditApi.ApiClient

    applicant1: NewDocumentCheckNs.ApplicantViewModel
    applicant2: NewDocumentCheckNs.ApplicantViewModel
    rejectionReasons: NewDocumentCheckNs.RejectionReasonModel[]
    otherRejectionReason: string

    applicant(applicantNr: number): NewDocumentCheckNs.ApplicantViewModel {
        if (applicantNr == 1) {
            return this.applicant1;
        } else if (applicantNr == 2) {
            return this.applicant2;
        } else {
            return null;
        }
    }

    isViewMode: boolean;
    incomeEdit: NewDocumentCheckNs.IncomeEditModel;
    incomeView: NewDocumentCheckNs.IncomeViewModel;

    chooseFile(applicantNr: number, evt: Event) {
        if (evt) {
            evt.preventDefault();
        }

        this.applicant(applicantNr).fileupload.showFilePicker();
    }

    saveChoosenFile(applicantNr: number, evt: Event) {
        this.isLoading = true;
        this.applicant(applicantNr).fileupload.loadSingleAttachedFileAsDataUrl().then(result => {
            this.$http.post(this.initialData.attachDocumentUrl, {
                applicationNr: this.initialData.applicationNr,
                applicantNr: applicantNr,
                dataUrl: result.dataUrl,
                filename: result.filename
            }).then((result: ng.IHttpResponse<Array<NewDocumentCheckNs.IDocument>>) => {
                this.applicant(applicantNr).documents = result.data;
                this.applicant(applicantNr).attachedFileName = null;
                this.applicant(applicantNr).fileupload.reset();
                this.isLoading = false;
            }, errorResult => {
                toastr.error(errorResult.statusText);
                this.applicant(applicantNr).attachedFileName = null;
                this.applicant(applicantNr).fileupload.reset();
                this.isLoading = false;
            })
        }, err => {
            toastr.error(err);
            this.applicant(applicantNr).fileupload.reset();
            this.isLoading = false;
        })
    }

    beginEditIncome(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }

        this.incomeEdit = {
            confirmedIncome1: this.formatIncomeForEdit(this.incomeView.confirmedIncome1),
            confirmedIncome2: this.hasCoApplicant ? this.formatIncomeForEdit(this.incomeView.confirmedIncome2) : ''
        };
    }

    cancelEditIncome(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        this.incomeEdit = null
    }

    parseDecimalOrNull(n: string) {
        if (ntech.forms.isNullOrWhitespace(n) || !this.isValidPositiveDecimal(n)) {
            return null;
        }
        return parseFloat(n.replace(',', '.'))
    }

    isValidPositiveDecimal(value: string): boolean {
        return ntech.forms.isValidPositiveDecimal(value);
    }

    formatIncomeForDisplay(income: number) {
        if (income == null) {
            return '-';
        } else {
            return this.$filter('currency')(income);
        }
    }

    formatIncomeForEdit(income: number) {
        if (income == null) {
            return '';
        } else {
            return income.toString();
        }
    }

    confirmEditIncome() {
        this.isLoading = true;

        let request: NewDocumentCheckNs.SetConfirmedIncomeRequest = {
            applicationNr: this.initialData.applicationNr,
            confirmedIncome1: this.incomeEdit.confirmedIncome1,
            confirmedIncome2: this.incomeEdit.confirmedIncome2
        }

        let tmpModel: NewDocumentCheckNs.IncomeEditModel = {
            confirmedIncome1: this.incomeEdit.confirmedIncome1,
            confirmedIncome2: this.incomeEdit.confirmedIncome2
        }

        this.$http.post(this.initialData.setConfirmedIncomeUrl, request).then((response: angular.IHttpResponse<NewDocumentCheckNs.ISetConfirmedResponse>) => {
            this.isLoading = false;
            this.incomeEdit = null;
            this.setIncomeView(response.data.confirmedIncome1, response.data.confirmedIncome2);
        }, response => {
            toastr.error(response.statusText, 'Failed');
            this.isLoading = false;
        })
    }

    acceptDocumentCheck(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http.post(this.initialData.acceptUrl, { applicationNr: this.initialData.applicationNr }).then(response => {
            this.onBack()
        }, err => {
            this.isLoading = false;
            toastr.error(err.statusText)
        });
    }

    rejectDocumentCheck(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        let reasons = [];
        for (let r of this.rejectionReasons) {
            if (r.isChecked) {
                reasons.push(r.text);
            }
        }
        if (!ntech.forms.isNullOrWhitespace(this.otherRejectionReason)) {
            reasons.push('other: ' + this.otherRejectionReason);
        }
        this.isLoading = true;
        this.$http.post(this.initialData.rejectUrl, { applicationNr: this.initialData.applicationNr, rejectionReasons: reasons }).then(response => {
            this.onBack()
        }, err => {
            this.isLoading = false;
            toastr.error(err.statusText)
        });
    }

    isRejectDocumentCheckAllowed(): boolean {
        for (let r of this.rejectionReasons) {
            if (r.isChecked) {
                return true;
            }
        }
        if (!ntech.forms.isNullOrWhitespace(this.otherRejectionReason)) {
            return true;
        }
        return false;
    }
}

app.controller('newDocumentCheckCtr', NewDocumentCheckCtr)

module NewDocumentCheckNs {
    export interface IInitialData {
        applicationNr: string,
        applicant1: IApplicantData,
        applicant2: IApplicantData,
        backUrl: string,
        acceptUrl: string,
        rejectUrl: string,
        setConfirmedIncomeUrl: string,
        attachDocumentUrl: string,
        isViewMode: boolean,
        documentCheckStatus: string,
        documentCheckRejectionReasons: string[]
    }

    export interface IApplicantData {
        confirmedIncome: number,
        statedIncome: number,
        employer: string,
        employment: string
        documents: Array<IDocument>
    }

    export class SetConfirmedIncomeRequest {
        applicationNr: string;
        confirmedIncome1: string;
        confirmedIncome2: string;
    }
    export interface ISetConfirmedResponse {
        confirmedIncome1: number;
        confirmedIncome2: number;
    }
    export class IncomeViewModel {
        confirmedIncome1: number;
        confirmedIncome2: number;
    }
    export class IncomeEditModel {
        confirmedIncome1: string;
        confirmedIncome2: string;
    }

    export class ApplicantViewModel {
        attachedFileName: string;
        fileupload: NtechAngularFileUpload.FileUploadHelper;
        documents: Array<IDocument>;
    }
    export class RejectionReasonModel {
        text: string;
        isChecked: boolean;
    }
    export interface IDocument {
        Id: number,
        DocumentArchiveKey: string,
        DocumentFileName: string,
        DocumentUrl: string
    }
}