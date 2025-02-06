import { ApiService } from '../backend/api-service'

export function startAdditionalQuestionsSession(apiService: ApiService, loginSessionDataToken: string, applicationNr: string) {
    apiService.startAdditionalQuestionsSession(loginSessionDataToken, applicationNr).subscribe(y => {
        if(!y.IsError) {
            apiService.navigateToQuestionsRoute('offer', y.Model.id)
        } else {
            if(y.ErrorCode === 'notFound') {
                apiService.navigateToQuestionsRoute('result-not-found', null)
            } else if(y.ErrorCode === 'notPendingAnswers') {
                apiService.navigateToQuestionsRoute('result-not-pending', null)
            } else {
                apiService.navigateToQuestionsRoute('result-failed', null)
            }                            
        }
    })
}