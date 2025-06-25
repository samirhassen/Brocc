var NTechWsDocsComponentNs;
(function (NTechWsDocsComponentNs) {
    class NTechWsDocsController extends NTechComponents.NTechComponentControllerBase {
        constructor($http, $q, $timeout, ntechComponentService) {
            super(ntechComponentService, $http, $q);
            this.$http = $http;
            this.$timeout = $timeout;
            this.filterText = '';
            this.lb = '\n';
        }
        componentName() {
            return 'ntechWsDocs';
        }
        onChanges() {
            this.filterText = '';
        }
        getFilteredMethods() {
            if (!this.filterText) {
                return this.initialData.methods;
            }
            let methods = [];
            //TODO: Use double metaphone or some other text index. Also maybe index into objects?
            for (let m of this.initialData.methods) {
                if (m.Path.toLowerCase().indexOf(this.filterText.toLowerCase()) >= 0) {
                    methods.push(m);
                }
            }
            return methods;
        }
        getTypeDesc(t) {
            let b = '';
            b += `${t.Name}: ` + this.lb;
            for (let p of t.PrimtiveProperties) {
                b += `    ${p.Name}: ${p.TypeCode}${(p.IsArray ? "[]" : "")}` + this.lb;
            }
            for (let p of t.CompoundProperties) {
                b += `    ${p.Name}: ${p.Type.Name}${(p.IsArray ? "[]" : "")}` + this.lb;
            }
            return b.substr(0, b.length - 1);
        }
        getFullMethodPath(m) {
            //TODO: Normalize '/'-es
            let s = location.origin + this.initialData.apiRootPath + '/' + m.Path;
            if (m.Method == 'GET') {
                s = s + m.RequestExample;
            }
            return s;
        }
        getSampleHeaders(m) {
            let b = '';
            if (m.Method == 'POST') {
                b += 'Content-Type: application/json' + this.lb;
            }
            b += `Authorization: Bearer ${this.initialData.testingToken ? this.initialData.testingToken : '<ACCESS_TOKEN_GOES_HERE>'}`;
            return b;
        }
        getMethodType(m) {
        }
    }
    NTechWsDocsController.$inject = ['$http', '$q', '$timeout', 'ntechComponentService'];
    NTechWsDocsComponentNs.NTechWsDocsController = NTechWsDocsController;
    class NTechWsDocsComponent {
        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<',
            };
            this.controller = NTechWsDocsController;
            this.template = `<div>
                <div class="pt-1 pb-2">
                    <div class="pull-left"><a class="n-back" ng-if="$ctrl.initialData.whiteListedReturnUrl" ng-href="{{$ctrl.initialData.whiteListedReturnUrl}}"><span class="glyphicon glyphicon-arrow-left"></span></a></div>
                    <h1 class="adjusted" ng-class="">Api documentation</h1>
                </div>
                <div class="pt-2">
                    <form>
                        <div class="form-group">
                            <label>Filter methods</label>
                            <input type="text" class="form-control" placeholder="Name part" ng-model="$ctrl.filterText">
                        </div>
                    </form>                
                </div>
                <hr>
                <div ng-repeat="m in $ctrl.getFilteredMethods() track by $index" class="pt-2">
                    <h2><span ng-click="m.isExpanded = !m.isExpanded" class="glyphicon" ng-class="{ 'chevron-bg glyphicon-chevron-down' : m.isExpanded, 'chevron-bg glyphicon-chevron-right' : !m.isExpanded }"></span><span class="copyable">{{m.Path}}</span></h2>
                    <div ng-if="m.isExpanded">
                        <div>
                            <h3>Request template</h3>
                            <div>
                                <pre class="copyable col-xs-1" style="overflow:hidden; white-space: pre;">{{m.Method}}</pre>
                                <pre class="copyable col-xs-11" style="overflow:hidden; white-space: pre">{{$ctrl.getFullMethodPath(m)}}</pre>
                            </div>
                            <pre class="copyable" style="overflow:hidden; white-space: pre">{{$ctrl.getSampleHeaders(m)}}</pre>
                            <pre class="copyable" ng-if="m.Method == 'POST'">{{m.RequestExample}}</pre>
                            <h3>Response template</h3>
                            <pre class="copyable">{{m.ResponseExample}}</pre>                        
                        </div>
                        <div>
                            <h3>Types</h3>                            
                            <pre>{{$ctrl.getTypeDesc(m.RequestType)}}</pre>
                            <pre ng-if="m.ResponseType.Name != 'FileResponseType'">{{$ctrl.getTypeDesc(m.ResponseType)}}</pre>
                            <pre ng-repeat="t in m.OtherTypes track by $index">{{$ctrl.getTypeDesc(t)}}</pre>                        
                        </div>
                    </div>
                </div>            
            </div>`;
        }
    }
    NTechWsDocsComponentNs.NTechWsDocsComponent = NTechWsDocsComponent;
    class InitialData {
    }
    NTechWsDocsComponentNs.InitialData = InitialData;
    class ServiceMethodDocumentation {
    }
    NTechWsDocsComponentNs.ServiceMethodDocumentation = ServiceMethodDocumentation;
    class CompoundType {
    }
    NTechWsDocsComponentNs.CompoundType = CompoundType;
    class PrimtiveProperty {
    }
    NTechWsDocsComponentNs.PrimtiveProperty = PrimtiveProperty;
    class CompoundProperty {
    }
    NTechWsDocsComponentNs.CompoundProperty = CompoundProperty;
})(NTechWsDocsComponentNs || (NTechWsDocsComponentNs = {}));
angular.module('ntech.components').component('ntechWsDocs', new NTechWsDocsComponentNs.NTechWsDocsComponent());
