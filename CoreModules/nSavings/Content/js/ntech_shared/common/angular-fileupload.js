var NtechAngularFileUpload;
(function (NtechAngularFileUpload) {
    class FileUploadHelperResult {
    }
    NtechAngularFileUpload.FileUploadHelperResult = FileUploadHelperResult;
    class FileUploadHelper {
        //NOTE: The form will be reset so make sure it doesnt contain other things beside the file control and stateless things
        constructor(fileElement, formElement, scope, $q) {
            this.fileElement = fileElement;
            this.formElement = formElement;
            this.scope = scope;
            this.$q = $q;
        }
        showFilePicker() {
            this.fileElement.click();
        }
        hasAttachedFiles() {
            if (!this.fileElement) {
                return false;
            }
            return this.fileElement.files.length > 0;
        }
        reset() {
            if (!this.formElement) {
                return;
            }
            this.formElement.reset();
        }
        addFileAttachedListener(onFilesAttached) {
            if (!this.fileElement) {
                return; //Make using ng-if less troublesome
            }
            this.fileElement.onchange = evt => {
                let attachedFiles = this.fileElement.files;
                let names = [];
                for (var i = 0; i < attachedFiles.length; i++) {
                    names.push(attachedFiles.item(i).name);
                }
                this.scope.$apply(() => {
                    onFilesAttached(names);
                });
            };
        }
        loadSingleAttachedFileAsDataUrl() {
            let deferred = this.$q.defer();
            let attachedFiles = this.fileElement.files;
            if (attachedFiles.length == 1) {
                let r = new FileReader();
                var f = attachedFiles[0];
                if (f.size > (10 * 1024 * 1024)) {
                    deferred.reject('Attached file is too big!');
                    return deferred.promise;
                }
                r.onloadend = (e) => {
                    let result = {
                        dataUrl: e.target.result,
                        filename: f.name
                    };
                    this.reset();
                    deferred.resolve(result);
                };
                r.readAsDataURL(f);
            }
            else if (attachedFiles.length == 0) {
                deferred.reject('No agreement attached!');
            }
            else {
                deferred.reject('Multiple files have been attached. Please reload the page and only attach a single file.');
            }
            return deferred.promise;
        }
    }
    NtechAngularFileUpload.FileUploadHelper = FileUploadHelper;
})(NtechAngularFileUpload || (NtechAngularFileUpload = {}));
