var initChooseDocumentSource = function ($scope, initialData, $translate, $http, $q, $timeout, initBasedOnState) {
    var ds = {
        sourceCode: '',
        isForcedBankAccountDataSharing: initialData.isForcedBankAccountDataSharing
    }

    function setIsLoading(isLoading) {
        ds.isLoading = isLoading
    }

    $scope.ds = ds
    ds.commitShouldChooseDocumentSource = function (applicantNr) {
        setIsLoading(true);
        $http.post(initialData.commitShouldChooseDocumentSource, {
            token: $scope.state.Token,
            sourceCode: ds.sourceCode,
            applicantNr: applicantNr
        }).then(function (response) {
            //TODO: Are there cases where we should not do this?
            if ($scope.ds.sourceCode == 'shareAccount') {
                document.location.href = applicantNr == 2 ? initialData.applicant2AccountDataShareUrl : initialData.applicant1AccountDataShareUrl
            } else {
                setIsLoading(false);
                initBasedOnState(response.data.state); 
            }
        }, function (err) {
            toastr.error('Failed to go to document upload')
            setIsLoading(false);
        })
    }    
}