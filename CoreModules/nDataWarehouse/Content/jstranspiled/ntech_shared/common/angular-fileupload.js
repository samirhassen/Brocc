var NtechAngularFileUpload;
(function (NtechAngularFileUpload) {
    var FileUploadHelperResult = /** @class */ (function () {
        function FileUploadHelperResult() {
        }
        return FileUploadHelperResult;
    }());
    NtechAngularFileUpload.FileUploadHelperResult = FileUploadHelperResult;
    var FileUploadHelper = /** @class */ (function () {
        //NOTE: The form will be reset so make sure it doesnt contain other things beside the file control and stateless things
        function FileUploadHelper(fileElement, formElement, scope, $q) {
            this.fileElement = fileElement;
            this.formElement = formElement;
            this.scope = scope;
            this.$q = $q;
        }
        FileUploadHelper.prototype.showFilePicker = function () {
            this.fileElement.click();
        };
        FileUploadHelper.prototype.hasAttachedFiles = function () {
            if (!this.fileElement) {
                return false;
            }
            return this.fileElement.files.length > 0;
        };
        FileUploadHelper.prototype.reset = function () {
            if (!this.formElement) {
                return;
            }
            this.formElement.reset();
        };
        FileUploadHelper.prototype.addFileAttachedListener = function (onFilesAttached) {
            var _this = this;
            if (!this.fileElement) {
                return; //Make using ng-if less troublesome
            }
            this.fileElement.onchange = function (evt) {
                var attachedFiles = _this.fileElement.files;
                var names = [];
                for (var i = 0; i < attachedFiles.length; i++) {
                    names.push(attachedFiles.item(i).name);
                }
                _this.scope.$apply(function () {
                    onFilesAttached(names);
                });
            };
        };
        FileUploadHelper.prototype.loadSingleAttachedFileAsDataUrl = function () {
            var _this = this;
            var deferred = this.$q.defer();
            var attachedFiles = this.fileElement.files;
            if (attachedFiles.length == 1) {
                var r = new FileReader();
                var f = attachedFiles[0];
                if (f.size > (10 * 1024 * 1024)) {
                    deferred.reject('Attached file is too big!');
                    return deferred.promise;
                }
                r.onloadend = function (e) {
                    var result = {
                        dataUrl: e.target.result,
                        filename: f.name
                    };
                    _this.reset();
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
        };
        return FileUploadHelper;
    }());
    NtechAngularFileUpload.FileUploadHelper = FileUploadHelper;
})(NtechAngularFileUpload || (NtechAngularFileUpload = {}));
