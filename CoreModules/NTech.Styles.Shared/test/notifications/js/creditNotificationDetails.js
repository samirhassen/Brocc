var data = "eyJOb3RpZmNhdGlvblBkZkxpbmsiOiIvQXBpL0FyY2hpdmVEb2N1bWVudD9rZXk9ZDRhM2E2MjQtZGFlMC00NjlkLThhZTItYjRkMGIwMTU4MzhhLnBkZiIsIk9jclBheW1lbnRSZWZlcmVuY2UiOiIxMTExMTIwNTIiLCJOb3RpZmljYXRpb25EYXRlIjoiMjAyMS0wNC0xNFQwMDowMDowMCIsIkR1ZURhdGUiOiIyMDIxLTA0LTI4VDAwOjAwOjAwIiwiUGF5bWVudElCQU4iOiJGSTUxODE5OTk3MTAwNTg4ODkiLCJQYXltZW50QmFua0dpcm8iOm51bGwsIkJhbGFuY2UiOnsiUmVtaW5kZXJGZWVJbml0aWFsQW1vdW50IjowLCJSZW1pbmRlckZlZVdyaXR0ZW5PZmZBbW91bnQiOjAsIlJlbWluZGVyRmVlUGFpZEFtb3VudCI6MCwiUmVtaW5kZXJGZWVSZW1haW5pbmdBbW91bnQiOjAsIk5vdGlmaWNhdGlvbkZlZUluaXRpYWxBbW91bnQiOjUsIk5vdGlmaWNhdGlvbkZlZVdyaXR0ZW5PZmZBbW91bnQiOjAsIk5vdGlmaWNhdGlvbkZlZVBhaWRBbW91bnQiOjAsIk5vdGlmaWNhdGlvbkZlZVJlbWFpbmluZ0Ftb3VudCI6NSwiSW50ZXJlc3RJbml0aWFsQW1vdW50Ijo1NC42MSwiSW50ZXJlc3RXcml0dGVuT2ZmQW1vdW50IjowLCJJbnRlcmVzdFBhaWRBbW91bnQiOjAsIkludGVyZXN0UmVtYWluaW5nQW1vdW50Ijo1NC42MSwiQ2FwaXRhbEluaXRpYWxBbW91bnQiOjI3LjU4LCJDYXBpdGFsV3JpdHRlbk9mZkFtb3VudCI6MCwiQ2FwaXRhbFBhaWRBbW91bnQiOjAsIkNhcGl0YWxSZW1haW5pbmdBbW91bnQiOjI3LjU4LCJUb3RhbEluaXRpYWxBbW91bnQiOjg3LjE5LCJUb3RhbFJlbWFpbmluZ0Ftb3VudCI6ODcuMTksIlRvdGFsV3JpdHRlbk9mZkFtb3VudCI6MCwiVG90YWxQYWlkQW1vdW50IjowfSwiUGF5bWVudHMiOltdLCJSZW1pbmRlcnMiOltdLCJQYXltZW50T3JkZXIiOlsiUmVtaW5kZXJGZWUiLCJOb3RpZmljYXRpb25GZWUiLCJJbnRlcmVzdCIsIkNhcGl0YWwiXX0=";

var app = angular.module('app', ['ntech.forms'])
app.controller('notificationDetails', ['$scope', '$http', function ($scope, $http) {
	$scope.n = JSON.parse(atob(data))    
	$scope.editData = null
	
	$scope.beginEdit = function() {
		$scope.editData = {}
	}
	$scope.cancelEdit = function() {
		$scope.editData = null
	}
	$scope.saveEdit = function() {
		$scope.editData = null
	}
	
	$scope.isEdit = function(){
		return !!$scope.editData
	}
	
    window.creditNotificationDetailsScope = $scope
}])