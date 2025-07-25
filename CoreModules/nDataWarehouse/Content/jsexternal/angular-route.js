/**
 * @license AngularJS v1.7.6
 * (c) 2010-2018 Google, Inc. http://angularjs.org
 * License: MIT
 */
(function (window, angular) {
    'use strict';

    /* global shallowCopy: true */

    /**
     * Creates a shallow copy of an object, an array or a primitive.
     *
     * Assumes that there are no proto properties for objects.
     */
    function shallowCopy(src, dst) {
        if (isArray(src)) {
            dst = dst || [];

            for (var i = 0, ii = src.length; i < ii; i++) {
                dst[i] = src[i];
            }
        } else if (isObject(src)) {
            dst = dst || {};

            for (var key in src) {
                if (!(key.charAt(0) === '$' && key.charAt(1) === '$')) {
                    dst[key] = src[key];
                }
            }
        }

        return dst || src;
    }

    /* global routeToRegExp: true */

    /**
     * @param {string} path - The path to parse. (It is assumed to have query and hash stripped off.)
     * @param {Object} opts - Options.
     * @return {Object} - An object containing an array of path parameter names (`keys`) and a regular
     *     expression (`regexp`) that can be used to identify a matching URL and extract the path
     *     parameter values.
     *
     * @description
     * Parses the given path, extracting path parameter names and a regular expression to match URLs.
     *
     * Originally inspired by `pathRexp` in `visionmedia/express/lib/utils.js`.
     */
    function routeToRegExp(path, opts) {
        var keys = [];

        var pattern = path
            .replace(/([().])/g, '\\$1')
            .replace(/(\/)?:(\w+)(\*\?|[?*])?/g, function (_, slash, key, option) {
                var optional = option === '?' || option === '*?';
                var star = option === '*' || option === '*?';
                keys.push({ name: key, optional: optional });
                slash = slash || '';
                return (
                    (optional ? '(?:' + slash : slash + '(?:') +
                    (star ? '(.+?)' : '([^/]+)') +
                    (optional ? '?)?' : ')')
                );
            })
            .replace(/([/$*])/g, '\\$1');

        if (opts.ignoreTrailingSlashes) {
            pattern = pattern.replace(/\/+$/, '') + '/*';
        }

        return {
            keys: keys,
            regexp: new RegExp(
                '^' + pattern + '(?:[?#]|$)',
                opts.caseInsensitiveMatch ? 'i' : ''
            )
        };
    }

    /* global routeToRegExp: false */
    /* global shallowCopy: false */

    // `isArray` and `isObject` are necessary for `shallowCopy()` (included via `src/shallowCopy.js`).
    // They are initialized inside the `$RouteProvider`, to ensure `window.angular` is available.
    var isArray;
    var isObject;
    var isDefined;
    var noop;

    /**
     * @ngdoc module
     * @name ngRoute
     * @description
     *
     * The `ngRoute` module provides routing and deeplinking services and directives for AngularJS apps.
     *
     * ## Example
     * See {@link ngRoute.$route#examples $route} for an example of configuring and using `ngRoute`.
     *
     */
    /* global -ngRouteModule */
    var ngRouteModule = angular.
        module('ngRoute', []).
        info({ angularVersion: '1.7.6' }).
        provider('$route', $RouteProvider).
        // Ensure `$route` will be instantiated in time to capture the initial `$locationChangeSuccess`
        // event (unless explicitly disabled). This is necessary in case `ngView` is included in an
        // asynchronously loaded template.
        run(instantiateRoute);
    var $routeMinErr = angular.$$minErr('ngRoute');
    var isEagerInstantiationEnabled;


    /**
     * @ngdoc provider
     * @name $routeProvider
     * @this
     *
     * @description
     *
     * Used for configuring routes.
     *
     * ## Example
     * See {@link ngRoute.$route#examples $route} for an example of configuring and using `ngRoute`.
     *
     * ## Dependencies
     * Requires the {@link ngRoute `ngRoute`} module to be installed.
     */
    function $RouteProvider() {
        isArray = angular.isArray;
        isObject = angular.isObject;
        isDefined = angular.isDefined;
        noop = angular.noop;

        function inherit(parent, extra) {
            return angular.extend(Object.create(parent), extra);
        }

        var routes = {};

        /**
         * @ngdoc method
         * @name $routeProvider#when
         *
         * @param {string} path Route path (matched against `$location.path`). If `$location.path`
         *    contains redundant trailing slash or is missing one, the route will still match and the
         *    `$location.path` will be updated to add or drop the trailing slash to exactly match the
         *    route definition.
         *
         *    * `path` can contain named groups starting with a colon: e.g. `:name`. All characters up
         *        to the next slash are matched and stored in `$routeParams` under the given `name`
         *        when the route matches.
         *    * `path` can contain named groups starting with a colon and ending with a star:
         *        e.g.`:name*`. All characters are eagerly stored in `$routeParams` under the given `name`
         *        when the route matches.
         *    * `path` can contain optional named groups with a question mark: e.g.`:name?`.
         *
         *    For example, routes like `/color/:color/largecode/:largecode*\/edit` will match
         *    `/color/brown/largecode/code/with/slashes/edit` and extract:
         *
         *    * `color: brown`
         *    * `largecode: code/with/slashes`.
         *
         *
         * @param {Object} route Mapping information to be assigned to `$route.current` on route
         *    match.
         *
         *    Object properties:
         *
         *    - `controller` – `{(string|Function)=}` – Controller fn that should be associated with
         *      newly created scope or the name of a {@link angular.Module#controller registered
         *      controller} if passed as a string.
         *    - `controllerAs` – `{string=}` – An identifier name for a reference to the controller.
         *      If present, the controller will be published to scope under the `controllerAs` name.
         *    - `template` – `{(string|Function)=}` – html template as a string or a function that
         *      returns an html template as a string which should be used by {@link
         *      ngRoute.directive:ngView ngView} or {@link ng.directive:ngInclude ngInclude} directives.
         *      This property takes precedence over `templateUrl`.
         *
         *      If `template` is a function, it will be called with the following parameters:
         *
         *      - `{Array.<Object>}` - route parameters extracted from the current
         *        `$location.path()` by applying the current route
         *
         *      One of `template` or `templateUrl` is required.
         *
         *    - `templateUrl` – `{(string|Function)=}` – path or function that returns a path to an html
         *      template that should be used by {@link ngRoute.directive:ngView ngView}.
         *
         *      If `templateUrl` is a function, it will be called with the following parameters:
         *
         *      - `{Array.<Object>}` - route parameters extracted from the current
         *        `$location.path()` by applying the current route
         *
         *      One of `templateUrl` or `template` is required.
         *
         *    - `resolve` - `{Object.<string, Function>=}` - An optional map of dependencies which should
         *      be injected into the controller. If any of these dependencies are promises, the router
         *      will wait for them all to be resolved or one to be rejected before the controller is
         *      instantiated.
         *      If all the promises are resolved successfully, the values of the resolved promises are
         *      injected and {@link ngRoute.$route#$routeChangeSuccess $routeChangeSuccess} event is
         *      fired. If any of the promises are rejected the
         *      {@link ngRoute.$route#$routeChangeError $routeChangeError} event is fired.
         *      For easier access to the resolved dependencies from the template, the `resolve` map will
         *      be available on the scope of the route, under `$resolve` (by default) or a custom name
         *      specified by the `resolveAs` property (see below). This can be particularly useful, when
         *      working with {@link angular.Module#component components} as route templates.<br />
         *      <div class="alert alert-warning">
         *        **Note:** If your scope already contains a property with this name, it will be hidden
         *        or overwritten. Make sure, you specify an appropriate name for this property, that
         *        does not collide with other properties on the scope.
         *      </div>
         *      The map object is:
         *
         *      - `key` – `{string}`: a name of a dependency to be injected into the controller.
         *      - `factory` - `{string|Function}`: If `string` then it is an alias for a service.
         *        Otherwise if function, then it is {@link auto.$injector#invoke injected}
         *        and the return value is treated as the dependency. If the result is a promise, it is
         *        resolved before its value is injected into the controller. Be aware that
         *        `ngRoute.$routeParams` will still refer to the previous route within these resolve
         *        functions.  Use `$route.current.params` to access the new route parameters, instead.
         *
         *    - `resolveAs` - `{string=}` - The name under which the `resolve` map will be available on
         *      the scope of the route. If omitted, defaults to `$resolve`.
         *
         *    - `redirectTo` – `{(string|Function)=}` – value to update
         *      {@link ng.$location $location} path with and trigger route redirection.
         *
         *      If `redirectTo` is a function, it will be called with the following parameters:
         *
         *      - `{Object.<string>}` - route parameters extracted from the current
         *        `$location.path()` by applying the current route templateUrl.
         *      - `{string}` - current `$location.path()`
         *      - `{Object}` - current `$location.search()`
         *
         *      The custom `redirectTo` function is expected to return a string which will be used
         *      to update `$location.url()`. If the function throws an error, no further processing will
         *      take place and the {@link ngRoute.$route#$routeChangeError $routeChangeError} event will
         *      be fired.
         *
         *      Routes that specify `redirectTo` will not have their controllers, template functions
         *      or resolves called, the `$location` will be changed to the redirect url and route
         *      processing will stop. The exception to this is if the `redirectTo` is a function that
         *      returns `undefined`. In this case the route transition occurs as though there was no
         *      redirection.
         *
         *    - `resolveRedirectTo` – `{Function=}` – a function that will (eventually) return the value
         *      to update {@link ng.$location $location} URL with and trigger route redirection. In
         *      contrast to `redirectTo`, dependencies can be injected into `resolveRedirectTo` and the
         *      return value can be either a string or a promise that will be resolved to a string.
         *
         *      Similar to `redirectTo`, if the return value is `undefined` (or a promise that gets
         *      resolved to `undefined`), no redirection takes place and the route transition occurs as
         *      though there was no redirection.
         *
         *      If the function throws an error or the returned promise gets rejected, no further
         *      processing will take place and the
         *      {@link ngRoute.$route#$routeChangeError $routeChangeError} event will be fired.
         *
         *      `redirectTo` takes precedence over `resolveRedirectTo`, so specifying both on the same
         *      route definition, will cause the latter to be ignored.
         *
         *    - `[reloadOnUrl=true]` - `{boolean=}` - reload route when any part of the URL changes
         *      (including the path) even if the new URL maps to the same route.
         *
         *      If the option is set to `false` and the URL in the browser changes, but the new URL maps
         *      to the same route, then a `$routeUpdate` event is broadcasted on the root scope (without
         *      reloading the route).
         *
         *    - `[reloadOnSearch=true]` - `{boolean=}` - reload route when only `$location.search()`
         *      or `$location.hash()` changes.
         *
         *      If the option is set to `false` and the URL in the browser changes, then a `$routeUpdate`
         *      event is broadcasted on the root scope (without reloading the route).
         *
         *      <div class="alert alert-warning">
         *        **Note:** This option has no effect if `reloadOnUrl` is set to `false`.
         *      </div>
         *
         *    - `[caseInsensitiveMatch=false]` - `{boolean=}` - match routes without being case sensitive
         *
         *      If the option is set to `true`, then the particular route can be matched without being
         *      case sensitive
         *
         * @returns {Object} self
         *
         * @description
         * Adds a new route definition to the `$route` service.
         */
        this.when = function (path, route) {
            //copy original route object to preserve params inherited from proto chain
            var routeCopy = shallowCopy(route);
            if (angular.isUndefined(routeCopy.reloadOnUrl)) {
                routeCopy.reloadOnUrl = true;
            }
            if (angular.isUndefined(routeCopy.reloadOnSearch)) {
                routeCopy.reloadOnSearch = true;
            }
            if (angular.isUndefined(routeCopy.caseInsensitiveMatch)) {
                routeCopy.caseInsensitiveMatch = this.caseInsensitiveMatch;
            }
            routes[path] = angular.extend(
                routeCopy,
                { originalPath: path },
                path && routeToRegExp(path, routeCopy)
            );

            // create redirection for trailing slashes
            if (path) {
                var redirectPath = (path[path.length - 1] === '/')
                    ? path.substr(0, path.length - 1)
                    : path + '/';

                routes[redirectPath] = angular.extend(
                    { originalPath: path, redirectTo: path },
                    routeToRegExp(redirectPath, routeCopy)
                );
            }

            return this;
        };

        /**
         * @ngdoc property
         * @name $routeProvider#caseInsensitiveMatch
         * @description
         *
         * A boolean property indicating if routes defined
         * using this provider should be matched using a case insensitive
         * algorithm. Defaults to `false`.
         */
        this.caseInsensitiveMatch = false;

        /**
         * @ngdoc method
         * @name $routeProvider#otherwise
         *
         * @description
         * Sets route definition that will be used on route change when no other route definition
         * is matched.
         *
         * @param {Object|string} params Mapping information to be assigned to `$route.current`.
         * If called with a string, the value maps to `redirectTo`.
         * @returns {Object} self
         */
        this.otherwise = function (params) {
            if (typeof params === 'string') {
                params = { redirectTo: params };
            }
            this.when(null, params);
            return this;
        };

        /**
         * @ngdoc method
         * @name $routeProvider#eagerInstantiationEnabled
         * @kind function
         *
         * @description
         * Call this method as a setter to enable/disable eager instantiation of the
         * {@link ngRoute.$route $route} service upon application bootstrap. You can also call it as a
         * getter (i.e. without any arguments) to get the current value of the
         * `eagerInstantiationEnabled` flag.
         *
         * Instantiating `$route` early is necessary for capturing the initial
         * {@link ng.$location#$locationChangeStart $locationChangeStart} event and navigating to the
         * appropriate route. Usually, `$route` is instantiated in time by the
         * {@link ngRoute.ngView ngView} directive. Yet, in cases where `ngView` is included in an
         * asynchronously loaded template (e.g. in another directive's template), the directive factory
         * might not be called soon enough for `$route` to be instantiated _before_ the initial
         * `$locationChangeSuccess` event is fired. Eager instantiation ensures that `$route` is always
         * instantiated in time, regardless of when `ngView` will be loaded.
         *
         * The default value is true.
         *
         * **Note**:<br />
         * You may want to disable the default behavior when unit-testing modules that depend on
         * `ngRoute`, in order to avoid an unexpected request for the default route's template.
         *
         * @param {boolean=} enabled - If provided, update the internal `eagerInstantiationEnabled` flag.
         *
         * @returns {*} The current value of the `eagerInstantiationEnabled` flag if used as a getter or
         *     itself (for chaining) if used as a setter.
         */
        isEagerInstantiationEnabled = true;
        this.eagerInstantiationEnabled = function eagerInstantiationEnabled(enabled) {
            if (isDefined(enabled)) {
                isEagerInstantiationEnabled = enabled;
                return this;
            }

            return isEagerInstantiationEnabled;
        };


        this.$get = ['$rootScope',
            '$location',
            '$routeParams',
            '$q',
            '$injector',
            '$templateRequest',
            '$sce',
            '$browser',
            function ($rootScope, $location, $routeParams, $q, $injector, $templateRequest, $sce, $browser) {

                /**
                 * @ngdoc service
                 * @name $route
                 * @requires $location
                 * @requires $routeParams
                 *
                 * @property {Object} current Reference to the current route definition.
                 * The route definition contains:
                 *
                 *   - `controller`: The controller constructor as defined in the route definition.
                 *   - `locals`: A map of locals which is used by {@link ng.$controller $controller} service for
                 *     controller instantiation. The `locals` contain
                 *     the resolved values of the `resolve` map. Additionally the `locals` also contain:
                 *
                 *     - `$scope` - The current route scope.
                 *     - `$template` - The current route template HTML.
                 *
                 *     The `locals` will be assigned to the route scope's `$resolve` property. You can override
                 *     the property name, using `resolveAs` in the route definition. See
                 *     {@link ngRoute.$routeProvider $routeProvider} for more info.
                 *
                 * @property {Object} routes Object with all route configuration Objects as its properties.
                 *
                 * @description
                 * `$route` is used for deep-linking URLs to controllers and views (HTML partials).
                 * It watches `$location.url()` and tries to map the path to an existing route definition.
                 *
                 * Requires the {@link ngRoute `ngRoute`} module to be installed.
                 *
                 * You can define routes through {@link ngRoute.$routeProvider $routeProvider}'s API.
                 *
                 * The `$route` service is typically used in conjunction with the
                 * {@link ngRoute.directive:ngView `ngView`} directive and the
                 * {@link ngRoute.$routeParams `$routeParams`} service.
                 *
                 * @example
                 * This example shows how changing the URL hash causes the `$route` to match a route against the
                 * URL, and the `ngView` pulls in the partial.
                 *
                 * <example name="$route-service" module="ngRouteExample"
                 *          deps="angular-route.js" fixBase="true">
                 *   <file name="index.html">
                 *     <div ng-controller="MainController">
                 *       Choose:
                 *       <a href="Book/Moby">Moby</a> |
                 *       <a href="Book/Moby/ch/1">Moby: Ch1</a> |
                 *       <a href="Book/Gatsby">Gatsby</a> |
                 *       <a href="Book/Gatsby/ch/4?key=value">Gatsby: Ch4</a> |
                 *       <a href="Book/Scarlet">Scarlet Letter</a><br/>
                 *
                 *       <div ng-view></div>
                 *
                 *       <hr />
                 *
                 *       <pre>$location.path() = {{$location.path()}}</pre>
                 *       <pre>$route.current.templateUrl = {{$route.current.templateUrl}}</pre>
                 *       <pre>$route.current.params = {{$route.current.params}}</pre>
                 *       <pre>$route.current.scope.name = {{$route.current.scope.name}}</pre>
                 *       <pre>$routeParams = {{$routeParams}}</pre>
                 *     </div>
                 *   </file>
                 *
                 *   <file name="book.html">
                 *     controller: {{name}}<br />
                 *     Book Id: {{params.bookId}}<br />
                 *   </file>
                 *
                 *   <file name="chapter.html">
                 *     controller: {{name}}<br />
                 *     Book Id: {{params.bookId}}<br />
                 *     Chapter Id: {{params.chapterId}}
                 *   </file>
                 *
                 *   <file name="script.js">
                 *     angular.module('ngRouteExample', ['ngRoute'])
                 *
                 *      .controller('MainController', function($scope, $route, $routeParams, $location) {
                 *          $scope.$route = $route;
                 *          $scope.$location = $location;
                 *          $scope.$routeParams = $routeParams;
                 *      })
                 *
                 *      .controller('BookController', function($scope, $routeParams) {
                 *          $scope.name = 'BookController';
                 *          $scope.params = $routeParams;
                 *      })
                 *
                 *      .controller('ChapterController', function($scope, $routeParams) {
                 *          $scope.name = 'ChapterController';
                 *          $scope.params = $routeParams;
                 *      })
                 *
                 *     .config(function($routeProvider, $locationProvider) {
                 *       $routeProvider
                 *        .when('/Book/:bookId', {
                 *         templateUrl: 'book.html',
                 *         controller: 'BookController',
                 *         resolve: {
                 *           // I will cause a 1 second delay
                 *           delay: function($q, $timeout) {
                 *             var delay = $q.defer();
                 *             $timeout(delay.resolve, 1000);
                 *             return delay.promise;
                 *           }
                 *         }
                 *       })
                 *       .when('/Book/:bookId/ch/:chapterId', {
                 *         templateUrl: 'chapter.html',
                 *         controller: 'ChapterController'
                 *       });
                 *
                 *       // configure html5 to get links working on jsfiddle
                 *       $locationProvider.html5Mode(true);
                 *     });
                 *
                 *   </file>
                 *
                 *   <file name="protractor.js" type="protractor">
                 *     it('should load and compile correct template', function() {
                 *       element(by.linkText('Moby: Ch1')).click();
                 *       var content = element(by.css('[ng-view]')).getText();
                 *       expect(content).toMatch(/controller: ChapterController/);
                 *       expect(content).toMatch(/Book Id: Moby/);
                 *       expect(content).toMatch(/Chapter Id: 1/);
                 *
                 *       element(by.partialLinkText('Scarlet')).click();
                 *
                 *       content = element(by.css('[ng-view]')).getText();
                 *       expect(content).toMatch(/controller: BookController/);
                 *       expect(content).toMatch(/Book Id: Scarlet/);
                 *     });
                 *   </file>
                 * </example>
                 */

                /**
                 * @ngdoc event
                 * @name $route#$routeChangeStart
                 * @eventType broadcast on root scope
                 * @description
                 * Broadcasted before a route change. At this  point the route services starts
                 * resolving all of the dependencies needed for the route change to occur.
                 * Typically this involves fetching the view template as well as any dependencies
                 * defined in `resolve` route property. Once  all of the dependencies are resolved
                 * `$routeChangeSuccess` is fired.
                 *
                 * The route change (and the `$location` change that triggered it) can be prevented
                 * by calling `preventDefault` method of the event. See {@link ng.$rootScope.Scope#$on}
                 * for more details about event object.
                 *
                 * @param {Object} angularEvent Synthetic event object.
                 * @param {Route} next Future route information.
                 * @param {Route} current Current route information.
                 */

                /**
                 * @ngdoc event
                 * @name $route#$routeChangeSuccess
                 * @eventType broadcast on root scope
                 * @description
                 * Broadcasted after a route change has happened successfully.
                 * The `resolve` dependencies are now available in the `current.locals` property.
                 *
                 * {@link ngRoute.directive:ngView ngView} listens for the directive
                 * to instantiate the controller and render the view.
                 *
                 * @param {Object} angularEvent Synthetic event object.
                 * @param {Route} current Current route information.
                 * @param {Route|Undefined} previous Previous route information, or undefined if current is
                 * first route entered.
                 */

                /**
                 * @ngdoc event
                 * @name $route#$routeChangeError
                 * @eventType broadcast on root scope
                 * @description
                 * Broadcasted if a redirection function fails or any redirection or resolve promises are
                 * rejected.
                 *
                 * @param {Object} angularEvent Synthetic event object
                 * @param {Route} current Current route information.
                 * @param {Route} previous Previous route information.
                 * @param {Route} rejection The thrown error or the rejection reason of the promise. Usually
                 * the rejection reason is the error that caused the promise to get rejected.
                 */

                /**
                 * @ngdoc event
                 * @name $route#$routeUpdate
                 * @eventType broadcast on root scope
                 * @description
                 * Broadcasted if the same instance of a route (including template, controller instance,
                 * resolved dependencies, etc.) is being reused. This can happen if either `reloadOnSearch` or
                 * `reloadOnUrl` has been set to `false`.
                 *
                 * @param {Object} angularEvent Synthetic event object
                 * @param {Route} current Current/previous route information.
                 */

                var forceReload = false,
                    preparedRoute,
                    preparedRouteIsUpdateOnly,
                    $route = {
                        routes: routes,

                        /**
                         * @ngdoc method
                         * @name $route#reload
                         *
                         * @description
                         * Causes `$route` service to reload the current route even if
                         * {@link ng.$location $location} hasn't changed.
                         *
                         * As a result of that, {@link ngRoute.directive:ngView ngView}
                         * creates new scope and reinstantiates the controller.
                         */
                        reload: function () {
                            forceReload = true;

                            var fakeLocationEvent = {
                                defaultPrevented: false,
                                preventDefault: function fakePreventDefault() {
                                    this.defaultPrevented = true;
                                    forceReload = false;
                                }
                            };

                            $rootScope.$evalAsync(function () {
                                prepareRoute(fakeLocationEvent);
                                if (!fakeLocationEvent.defaultPrevented) commitRoute();
                            });
                        },

                        /**
                         * @ngdoc method
                         * @name $route#updateParams
                         *
                         * @description
                         * Causes `$route` service to update the current URL, replacing
                         * current route parameters with those specified in `newParams`.
                         * Provided property names that match the route's path segment
                         * definitions will be interpolated into the location's path, while
                         * remaining properties will be treated as query params.
                         *
                         * @param {!Object<string, string>} newParams mapping of URL parameter names to values
                         */
                        updateParams: function (newParams) {
                            if (this.current && this.current.$$route) {
                                newParams = angular.extend({}, this.current.params, newParams);
                                $location.path(interpolate(this.current.$$route.originalPath, newParams));
                                // interpolate modifies newParams, only query params are left
                                $location.search(newParams);
                            } else {
                                throw $routeMinErr('norout', 'Tried updating route with no current route');
                            }
                        }
                    };

                $rootScope.$on('$locationChangeStart', prepareRoute);
                $rootScope.$on('$locationChangeSuccess', commitRoute);

                return $route;

                /////////////////////////////////////////////////////

                /**
                 * @param on {string} current url
                 * @param route {Object} route regexp to match the url against
                 * @return {?Object}
                 *
                 * @description
                 * Check if the route matches the current url.
                 *
                 * Inspired by match in
                 * visionmedia/express/lib/router/router.js.
                 */
                function switchRouteMatcher(on, route) {
                    var keys = route.keys,
                        params = {};

                    if (!route.regexp) return null;

                    var m = route.regexp.exec(on);
                    if (!m) return null;

                    for (var i = 1, len = m.length; i < len; ++i) {
                        var key = keys[i - 1];

                        var val = m[i];

                        if (key && val) {
                            params[key.name] = val;
                        }
                    }
                    return params;
                }

                function prepareRoute($locationEvent) {
                    var lastRoute = $route.current;

                    preparedRoute = parseRoute();
                    preparedRouteIsUpdateOnly = isNavigationUpdateOnly(preparedRoute, lastRoute);

                    if (!preparedRouteIsUpdateOnly && (lastRoute || preparedRoute)) {
                        if ($rootScope.$broadcast('$routeChangeStart', preparedRoute, lastRoute).defaultPrevented) {
                            if ($locationEvent) {
                                $locationEvent.preventDefault();
                            }
                        }
                    }
                }

                function commitRoute() {
                    var lastRoute = $route.current;
                    var nextRoute = preparedRoute;

                    if (preparedRouteIsUpdateOnly) {
                        lastRoute.params = nextRoute.params;
                        angular.copy(lastRoute.params, $routeParams);
                        $rootScope.$broadcast('$routeUpdate', lastRoute);
                    } else if (nextRoute || lastRoute) {
                        forceReload = false;
                        $route.current = nextRoute;

                        var nextRoutePromise = $q.resolve(nextRoute);

                        $browser.$$incOutstandingRequestCount('$route');

                        nextRoutePromise.
                            then(getRedirectionData).
                            then(handlePossibleRedirection).
                            then(function (keepProcessingRoute) {
                                return keepProcessingRoute && nextRoutePromise.
                                    then(resolveLocals).
                                    then(function (locals) {
                                        // after route change
                                        if (nextRoute === $route.current) {
                                            if (nextRoute) {
                                                nextRoute.locals = locals;
                                                angular.copy(nextRoute.params, $routeParams);
                                            }
                                            $rootScope.$broadcast('$routeChangeSuccess', nextRoute, lastRoute);
                                        }
                                    });
                            }).catch(function (error) {
                                if (nextRoute === $route.current) {
                                    $rootScope.$broadcast('$routeChangeError', nextRoute, lastRoute, error);
                                }
                            }).finally(function () {
                                // Because `commitRoute()` is called from a `$rootScope.$evalAsync` block (see
                                // `$locationWatch`), this `$$completeOutstandingRequest()` call will not cause
                                // `outstandingRequestCount` to hit zero.  This is important in case we are redirecting
                                // to a new route which also requires some asynchronous work.

                                $browser.$$completeOutstandingRequest(noop, '$route');
                            });
                    }
                }

                function getRedirectionData(route) {
                    var data = {
                        route: route,
                        hasRedirection: false
                    };

                    if (route) {
                        if (route.redirectTo) {
                            if (angular.isString(route.redirectTo)) {
                                data.path = interpolate(route.redirectTo, route.params);
                                data.search = route.params;
                                data.hasRedirection = true;
                            } else {
                                var oldPath = $location.path();
                                var oldSearch = $location.search();
                                var newUrl = route.redirectTo(route.pathParams, oldPath, oldSearch);

                                if (angular.isDefined(newUrl)) {
                                    data.url = newUrl;
                                    data.hasRedirection = true;
                                }
                            }
                        } else if (route.resolveRedirectTo) {
                            return $q.
                                resolve($injector.invoke(route.resolveRedirectTo)).
                                then(function (newUrl) {
                                    if (angular.isDefined(newUrl)) {
                                        data.url = newUrl;
                                        data.hasRedirection = true;
                                    }

                                    return data;
                                });
                        }
                    }

                    return data;
                }

                function handlePossibleRedirection(data) {
                    var keepProcessingRoute = true;

                    if (data.route !== $route.current) {
                        keepProcessingRoute = false;
                    } else if (data.hasRedirection) {
                        var oldUrl = $location.url();
                        var newUrl = data.url;

                        if (newUrl) {
                            $location.
                                url(newUrl).
                                replace();
                        } else {
                            newUrl = $location.
                                path(data.path).
                                search(data.search).
                                replace().
                                url();
                        }

                        if (newUrl !== oldUrl) {
                            // Exit out and don't process current next value,
                            // wait for next location change from redirect
                            keepProcessingRoute = false;
                        }
                    }

                    return keepProcessingRoute;
                }

                function resolveLocals(route) {
                    if (route) {
                        var locals = angular.extend({}, route.resolve);
                        angular.forEach(locals, function (value, key) {
                            locals[key] = angular.isString(value) ?
                                $injector.get(value) :
                                $injector.invoke(value, null, null, key);
                        });
                        var template = getTemplateFor(route);
                        if (angular.isDefined(template)) {
                            locals['$template'] = template;
                        }
                        return $q.all(locals);
                    }
                }

                function getTemplateFor(route) {
                    var template, templateUrl;
                    if (angular.isDefined(template = route.template)) {
                        if (angular.isFunction(template)) {
                            template = template(route.params);
                        }
                    } else if (angular.isDefined(templateUrl = route.templateUrl)) {
                        if (angular.isFunction(templateUrl)) {
                            templateUrl = templateUrl(route.params);
                        }
                        if (angular.isDefined(templateUrl)) {
                            route.loadedTemplateUrl = $sce.valueOf(templateUrl);
                            template = $templateRequest(templateUrl);
                        }
                    }
                    return template;
                }

                /**
                 * @returns {Object} the current active route, by matching it against the URL
                 */
                function parseRoute() {
                    // Match a route
                    var params, match;
                    angular.forEach(routes, function (route, path) {
                        if (!match && (params = switchRouteMatcher($location.path(), route))) {
                            match = inherit(route, {
                                params: angular.extend({}, $location.search(), params),
                                pathParams: params
                            });
                            match.$$route = route;
                        }
                    });
                    // No route matched; fallback to "otherwise" route
                    return match || routes[null] && inherit(routes[null], { params: {}, pathParams: {} });
                }

                /**
                 * @param {Object} newRoute - The new route configuration (as returned by `parseRoute()`).
                 * @param {Object} oldRoute - The previous route configuration (as returned by `parseRoute()`).
                 * @returns {boolean} Whether this is an "update-only" navigation, i.e. the URL maps to the same
                 *                    route and it can be reused (based on the config and the type of change).
                 */
                function isNavigationUpdateOnly(newRoute, oldRoute) {
                    // IF this is not a forced reload
                    return !forceReload
                        // AND both `newRoute`/`oldRoute` are defined
                        && newRoute && oldRoute
                        // AND they map to the same Route Definition Object
                        && (newRoute.$$route === oldRoute.$$route)
                        // AND `reloadOnUrl` is disabled
                        && (!newRoute.reloadOnUrl
                            // OR `reloadOnSearch` is disabled
                            || (!newRoute.reloadOnSearch
                                // AND both routes have the same path params
                                && angular.equals(newRoute.pathParams, oldRoute.pathParams)
                            )
                        );
                }

                /**
                 * @returns {string} interpolation of the redirect path with the parameters
                 */
                function interpolate(string, params) {
                    var result = [];
                    angular.forEach((string || '').split(':'), function (segment, i) {
                        if (i === 0) {
                            result.push(segment);
                        } else {
                            var segmentMatch = segment.match(/(\w+)(?:[?*])?(.*)/);
                            var key = segmentMatch[1];
                            result.push(params[key]);
                            result.push(segmentMatch[2] || '');
                            delete params[key];
                        }
                    });
                    return result.join('');
                }
            }];
    }

    instantiateRoute.$inject = ['$injector'];
    function instantiateRoute($injector) {
        if (isEagerInstantiationEnabled) {
            // Instantiate `$route`
            $injector.get('$route');
        }
    }

    ngRouteModule.provider('$routeParams', $RouteParamsProvider);


    /**
     * @ngdoc service
     * @name $routeParams
     * @requires $route
     * @this
     *
     * @description
     * The `$routeParams` service allows you to retrieve the current set of route parameters.
     *
     * Requires the {@link ngRoute `ngRoute`} module to be installed.
     *
     * The route parameters are a combination of {@link ng.$location `$location`}'s
     * {@link ng.$location#search `search()`} and {@link ng.$location#path `path()`}.
     * The `path` parameters are extracted when the {@link ngRoute.$route `$route`} path is matched.
     *
     * In case of parameter name collision, `path` params take precedence over `search` params.
     *
     * The service guarantees that the identity of the `$routeParams` object will remain unchanged
     * (but its properties will likely change) even when a route change occurs.
     *
     * Note that the `$routeParams` are only updated *after* a route change completes successfully.
     * This means that you cannot rely on `$routeParams` being correct in route resolve functions.
     * Instead you can use `$route.current.params` to access the new route's parameters.
     *
     * @example
     * ```js
     *  // Given:
     *  // URL: http://server.com/index.html#/Chapter/1/Section/2?search=moby
     *  // Route: /Chapter/:chapterId/Section/:sectionId
     *  //
     *  // Then
     *  $routeParams ==> {chapterId:'1', sectionId:'2', search:'moby'}
     * ```
     */
    function $RouteParamsProvider() {
        this.$get = function () { return {}; };
    }

    ngRouteModule.directive('ngView', ngViewFactory);
    ngRouteModule.directive('ngView', ngViewFillContentFactory);


    /**
     * @ngdoc directive
     * @name ngView
     * @restrict ECA
     *
     * @description
     * `ngView` is a directive that complements the {@link ngRoute.$route $route} service by
     * including the rendered template of the current route into the main layout (`index.html`) file.
     * Every time the current route changes, the included view changes with it according to the
     * configuration of the `$route` service.
     *
     * Requires the {@link ngRoute `ngRoute`} module to be installed.
     *
     * @animations
     * | Animation                        | Occurs                              |
     * |----------------------------------|-------------------------------------|
     * | {@link ng.$animate#enter enter}  | when the new element is inserted to the DOM |
     * | {@link ng.$animate#leave leave}  | when the old element is removed from to the DOM  |
     *
     * The enter and leave animation occur concurrently.
     *
     * @scope
     * @priority 400
     * @param {string=} onload Expression to evaluate whenever the view updates.
     *
     * @param {string=} autoscroll Whether `ngView` should call {@link ng.$anchorScroll
     *                  $anchorScroll} to scroll the viewport after the view is updated.
     *
     *                  - If the attribute is not set, disable scrolling.
     *                  - If the attribute is set without value, enable scrolling.
     *                  - Otherwise enable scrolling only if the `autoscroll` attribute value evaluated
     *                    as an expression yields a truthy value.
     * @example
        <example name="ngView-directive" module="ngViewExample"
                 deps="angular-route.js;angular-animate.js"
                 animations="true" fixBase="true">
          <file name="index.html">
            <div ng-controller="MainCtrl as main">
              Choose:
              <a href="Book/Moby">Moby</a> |
              <a href="Book/Moby/ch/1">Moby: Ch1</a> |
              <a href="Book/Gatsby">Gatsby</a> |
              <a href="Book/Gatsby/ch/4?key=value">Gatsby: Ch4</a> |
              <a href="Book/Scarlet">Scarlet Letter</a><br/>
    
              <div class="view-animate-container">
                <div ng-view class="view-animate"></div>
              </div>
              <hr />
    
              <pre>$location.path() = {{main.$location.path()}}</pre>
              <pre>$route.current.templateUrl = {{main.$route.current.templateUrl}}</pre>
              <pre>$route.current.params = {{main.$route.current.params}}</pre>
              <pre>$routeParams = {{main.$routeParams}}</pre>
            </div>
          </file>
    
          <file name="book.html">
            <div>
              controller: {{book.name}}<br />
              Book Id: {{book.params.bookId}}<br />
            </div>
          </file>
    
          <file name="chapter.html">
            <div>
              controller: {{chapter.name}}<br />
              Book Id: {{chapter.params.bookId}}<br />
              Chapter Id: {{chapter.params.chapterId}}
            </div>
          </file>
    
          <file name="animations.css">
            .view-animate-container {
              position:relative;
              height:100px!important;
              background:white;
              border:1px solid black;
              height:40px;
              overflow:hidden;
            }
    
            .view-animate {
              padding:10px;
            }
    
            .view-animate.ng-enter, .view-animate.ng-leave {
              transition:all cubic-bezier(0.250, 0.460, 0.450, 0.940) 1.5s;
    
              display:block;
              width:100%;
              border-left:1px solid black;
    
              position:absolute;
              top:0;
              left:0;
              right:0;
              bottom:0;
              padding:10px;
            }
    
            .view-animate.ng-enter {
              left:100%;
            }
            .view-animate.ng-enter.ng-enter-active {
              left:0;
            }
            .view-animate.ng-leave.ng-leave-active {
              left:-100%;
            }
          </file>
    
          <file name="script.js">
            angular.module('ngViewExample', ['ngRoute', 'ngAnimate'])
              .config(['$routeProvider', '$locationProvider',
                function($routeProvider, $locationProvider) {
                  $routeProvider
                    .when('/Book/:bookId', {
                      templateUrl: 'book.html',
                      controller: 'BookCtrl',
                      controllerAs: 'book'
                    })
                    .when('/Book/:bookId/ch/:chapterId', {
                      templateUrl: 'chapter.html',
                      controller: 'ChapterCtrl',
                      controllerAs: 'chapter'
                    });
    
                  $locationProvider.html5Mode(true);
              }])
              .controller('MainCtrl', ['$route', '$routeParams', '$location',
                function MainCtrl($route, $routeParams, $location) {
                  this.$route = $route;
                  this.$location = $location;
                  this.$routeParams = $routeParams;
              }])
              .controller('BookCtrl', ['$routeParams', function BookCtrl($routeParams) {
                this.name = 'BookCtrl';
                this.params = $routeParams;
              }])
              .controller('ChapterCtrl', ['$routeParams', function ChapterCtrl($routeParams) {
                this.name = 'ChapterCtrl';
                this.params = $routeParams;
              }]);
    
          </file>
    
          <file name="protractor.js" type="protractor">
            it('should load and compile correct template', function() {
              element(by.linkText('Moby: Ch1')).click();
              var content = element(by.css('[ng-view]')).getText();
              expect(content).toMatch(/controller: ChapterCtrl/);
              expect(content).toMatch(/Book Id: Moby/);
              expect(content).toMatch(/Chapter Id: 1/);
    
              element(by.partialLinkText('Scarlet')).click();
    
              content = element(by.css('[ng-view]')).getText();
              expect(content).toMatch(/controller: BookCtrl/);
              expect(content).toMatch(/Book Id: Scarlet/);
            });
          </file>
        </example>
     */


    /**
     * @ngdoc event
     * @name ngView#$viewContentLoaded
     * @eventType emit on the current ngView scope
     * @description
     * Emitted every time the ngView content is reloaded.
     */
    ngViewFactory.$inject = ['$route', '$anchorScroll', '$animate'];
    function ngViewFactory($route, $anchorScroll, $animate) {
        return {
            restrict: 'ECA',
            terminal: true,
            priority: 400,
            transclude: 'element',
            link: function (scope, $element, attr, ctrl, $transclude) {
                var currentScope,
                    currentElement,
                    previousLeaveAnimation,
                    autoScrollExp = attr.autoscroll,
                    onloadExp = attr.onload || '';

                scope.$on('$routeChangeSuccess', update);
                update();

                function cleanupLastView() {
                    if (previousLeaveAnimation) {
                        $animate.cancel(previousLeaveAnimation);
                        previousLeaveAnimation = null;
                    }

                    if (currentScope) {
                        currentScope.$destroy();
                        currentScope = null;
                    }
                    if (currentElement) {
                        previousLeaveAnimation = $animate.leave(currentElement);
                        previousLeaveAnimation.done(function (response) {
                            if (response !== false) previousLeaveAnimation = null;
                        });
                        currentElement = null;
                    }
                }

                function update() {
                    var locals = $route.current && $route.current.locals,
                        template = locals && locals.$template;

                    if (angular.isDefined(template)) {
                        var newScope = scope.$new();
                        var current = $route.current;

                        // Note: This will also link all children of ng-view that were contained in the original
                        // html. If that content contains controllers, ... they could pollute/change the scope.
                        // However, using ng-view on an element with additional content does not make sense...
                        // Note: We can't remove them in the cloneAttchFn of $transclude as that
                        // function is called before linking the content, which would apply child
                        // directives to non existing elements.
                        var clone = $transclude(newScope, function (clone) {
                            $animate.enter(clone, null, currentElement || $element).done(function onNgViewEnter(response) {
                                if (response !== false && angular.isDefined(autoScrollExp)
                                    && (!autoScrollExp || scope.$eval(autoScrollExp))) {
                                    $anchorScroll();
                                }
                            });
                            cleanupLastView();
                        });

                        currentElement = clone;
                        currentScope = current.scope = newScope;
                        currentScope.$emit('$viewContentLoaded');
                        currentScope.$eval(onloadExp);
                    } else {
                        cleanupLastView();
                    }
                }
            }
        };
    }

    // This directive is called during the $transclude call of the first `ngView` directive.
    // It will replace and compile the content of the element with the loaded template.
    // We need this directive so that the element content is already filled when
    // the link function of another directive on the same element as ngView
    // is called.
    ngViewFillContentFactory.$inject = ['$compile', '$controller', '$route'];
    function ngViewFillContentFactory($compile, $controller, $route) {
        return {
            restrict: 'ECA',
            priority: -400,
            link: function (scope, $element) {
                var current = $route.current,
                    locals = current.locals;

                $element.html(locals.$template);

                var link = $compile($element.contents());

                if (current.controller) {
                    locals.$scope = scope;
                    var controller = $controller(current.controller, locals);
                    if (current.controllerAs) {
                        scope[current.controllerAs] = controller;
                    }
                    $element.data('$ngControllerController', controller);
                    $element.children().data('$ngControllerController', controller);
                }
                scope[current.resolveAs || '$resolve'] = locals;

                link(scope);
            }
        };
    }


})(window, window.angular);