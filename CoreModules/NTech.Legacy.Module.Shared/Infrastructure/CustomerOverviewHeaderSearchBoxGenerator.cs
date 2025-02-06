using NTech.Services.Infrastructure;

namespace NTech.Legacy.Module.Shared.Infrastructure
{
    public static class CustomerOverviewHeaderSearchBoxGenerator
    {
        /*
        This is sort of a hack to not have to copy paste this javascript into all modules.
        Change _Layout.cshtml from this:
        @RenderSection("Scripts", true)
        To:
        <script type="text/javascript"  @Html.CspScriptNonce()>
            @Html.Raw(NTech.Legacy.Module.Shared.Infrastructure.CustomerOverviewHeaderSearchBoxGenerator.GenerateScriptBlock(NEnv.ServiceRegistry, NEnv.ClientCfg));
        </script>
        @RenderSection("Scripts", true)
         */
        public static string GenerateScriptBlock(NTechServiceRegistry serviceRegistry, ClientConfiguration clientConfiguration)
        {
            if (!clientConfiguration.IsFeatureEnabled("ntech.feature.customeroverview"))
                return "";

            var isHelpSearchActive = clientConfiguration.IsFeatureEnabled("ntech.feature.helpsearch");

            const string NormalSearchTemplate =
@"inputElement.value = '';
            inputElement.placeholder = 'Loading...';
            inputElement.disabled = true;
            let encodedQuery = 'eq__' + toUrlSafeBase64String(query);
            let targetUrl = '[[SEARCH_PATTERN]]'.replace('[[QUERY]]', encodeURIComponent(encodedQuery));
            document.location.href = targetUrl;";

            const string Template =
@"function singleElement(names) {
    if (names.length === 0) {
        return null
    }
    let element = document
    for (let name of names) {
        let elements = element.getElementsByClassName(name);
        if (elements.length === 1) {
            element = elements[0]
        } else {
            return null
        }
    }
    return element
}

function toUrlSafeBase64String(data) {
    let encoded = btoa(JSON.stringify(data)).replace('+', '-').replace('/', '_');
    while (encoded[encoded.length - 1] === '=') {
        encoded = encoded.substr(0, encoded.length - 1);
    }
    return encoded;
}

function addSearchBeforeElement(element) {    
    let searchElement = document.createElement('div');
    searchElement.id = 'nTechGlobalSearch';
    searchElement.innerHTML = `<div class=""search-field""><form><input class=""search-placeholder-icon"" type=""text"" placeholder=""customer search"" autocomplete=""nope"" /></form></div>`;
    let formElement = searchElement.getElementsByTagName('form')[0];
    let inputElement = searchElement.getElementsByTagName('input')[0];
    formElement.onsubmit = evt => {
        evt.preventDefault();
        if (!inputElement.value) {
            return
        }
        let query = (inputElement.value ?? '').trim();
        if(!query.toLowerCase().startsWith('q:')) {            
           [[ON_NORMAL_SEARCH]]
        } else {
            [[ON_HELP_SEARCH]]            
        }
    };
    element.parentElement.insertBefore(searchElement, element);
}
let navigationClear = singleElement(['navigation', 'content-position', 'clearfix']);
if (navigationClear) {
    addSearchBeforeElement(navigationClear);
}";
            var customerSearchUrlPattern = serviceRegistry.Internal.ServiceUrl("nBackOffice", "s/customer-overview/search/[[QUERY]]").ToString();
            return Template
                .Replace("[[ON_NORMAL_SEARCH]]", NormalSearchTemplate)
                .Replace("[[ON_HELP_SEARCH]]", isHelpSearchActive ? AddHelpSearchBlock() : NormalSearchTemplate)
                .Replace("[[SEARCH_PATTERN]]", customerSearchUrlPattern);
        }

        private static string AddHelpSearchBlock()
        {
            return @"
                var popupDiv = document.getElementById('helpSearch1390123789')
                    if (!popupDiv) {
                        popupDiv = document.createElement('div');
                        popupDiv.className = 'modal fade';
                        popupDiv.id = 'helpSearch1390123789';
                        popupDiv.role = 'dialog';
                        popupDiv.innerHTML = `<div class=""modal-dialog"">
                            <div class=""modal-content"">
                                <div class=""modal-header"">
                                    <button type=""button"" class=""close"" data-dismiss=""modal"">&times;</button>
                                    <h4 class=""modal-title"">AI</h4>
                                </div>
                                <div class=""modal-body"">
                                    <div id=""response1390123789"" style=""white-space: pre-wrap""></div>
                                </div>
                            </div>
                        </div>`;
                        document.body.appendChild(popupDiv);
                    }; 
    
            var response = $('#response1390123789');    
            response.html('Thinking...');
            $('#helpSearch1390123789').modal('show');
            
            function postQuery(request, onSucess) {
                $.ajax({
                    url: '/Api/Gateway/NTechHost/Api/Help/Query',
                    type: 'POST',
                    data: JSON.stringify(request),
                    dataType:'json',
                    contentType: 'application/json; charset=utf-8',
                    success: data => {
                        onSucess(data);
                    }
                });
            }

            function pollSearch(id, count) {
                if(count > 15) {
                    return;
                }
                setTimeout(() => {
                    postQuery({ ongoingQueryId: id}, data => {
                        var response = $('#response1390123789');
                        response.html(data.answer);
                        if(!data.isComplete) {
                            pollSearch(data.id, count + 1);
                        }                        
                    });
                }, 300);
            }

            postQuery({ newQuery: query.substring(2).trim() }, data => {
                var response = $('#response1390123789');
                response.html(data.answer);
                if(!data.isComplete) {
                    pollSearch(data.id, 0);
                }
            });";
        }
    }
}