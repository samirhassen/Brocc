var COMPILED = !0, goog = goog || {};
goog.global = this;
goog.isDef = function(a) {
  return void 0 !== a;
};
goog.isString = function(a) {
  return "string" == typeof a;
};
goog.isBoolean = function(a) {
  return "boolean" == typeof a;
};
goog.isNumber = function(a) {
  return "number" == typeof a;
};
goog.exportPath_ = function(a, b, c) {
  a = a.split(".");
  c = c || goog.global;
  a[0] in c || !c.execScript || c.execScript("var " + a[0]);
  for (var d; a.length && (d = a.shift());) {
    !a.length && goog.isDef(b) ? c[d] = b : c = c[d] && c[d] !== Object.prototype[d] ? c[d] : c[d] = {};
  }
};
goog.define = function(a, b) {
  var c = b;
  COMPILED || (goog.global.CLOSURE_UNCOMPILED_DEFINES && void 0 === goog.global.CLOSURE_UNCOMPILED_DEFINES.nodeType && Object.prototype.hasOwnProperty.call(goog.global.CLOSURE_UNCOMPILED_DEFINES, a) ? c = goog.global.CLOSURE_UNCOMPILED_DEFINES[a] : goog.global.CLOSURE_DEFINES && void 0 === goog.global.CLOSURE_DEFINES.nodeType && Object.prototype.hasOwnProperty.call(goog.global.CLOSURE_DEFINES, a) && (c = goog.global.CLOSURE_DEFINES[a]));
  goog.exportPath_(a, c);
};
goog.DEBUG = !0;
goog.LOCALE = "en";
goog.TRUSTED_SITE = !0;
goog.STRICT_MODE_COMPATIBLE = !1;
goog.DISALLOW_TEST_ONLY_CODE = COMPILED && !goog.DEBUG;
goog.ENABLE_CHROME_APP_SAFE_SCRIPT_LOADING = !1;
goog.provide = function(a) {
  if (goog.isInModuleLoader_()) {
    throw Error("goog.provide can not be used within a goog.module.");
  }
  if (!COMPILED && goog.isProvided_(a)) {
    throw Error('Namespace "' + a + '" already declared.');
  }
  goog.constructNamespace_(a);
};
goog.constructNamespace_ = function(a, b) {
  if (!COMPILED) {
    delete goog.implicitNamespaces_[a];
    for (var c = a; (c = c.substring(0, c.lastIndexOf("."))) && !goog.getObjectByName(c);) {
      goog.implicitNamespaces_[c] = !0;
    }
  }
  goog.exportPath_(a, b);
};
goog.VALID_MODULE_RE_ = /^[a-zA-Z_$][a-zA-Z0-9._$]*$/;
goog.module = function(a) {
  if (!goog.isString(a) || !a || -1 == a.search(goog.VALID_MODULE_RE_)) {
    throw Error("Invalid module identifier");
  }
  if (!goog.isInModuleLoader_()) {
    throw Error("Module " + a + " has been loaded incorrectly. Note, modules cannot be loaded as normal scripts. They require some kind of pre-processing step. You're likely trying to load a module via a script tag or as a part of a concatenated bundle without rewriting the module. For more info see: https://github.com/google/closure-library/wiki/goog.module:-an-ES6-module-like-alternative-to-goog.provide.");
  }
  if (goog.moduleLoaderState_.moduleName) {
    throw Error("goog.module may only be called once per module.");
  }
  goog.moduleLoaderState_.moduleName = a;
  if (!COMPILED) {
    if (goog.isProvided_(a)) {
      throw Error('Namespace "' + a + '" already declared.');
    }
    delete goog.implicitNamespaces_[a];
  }
};
goog.module.get = function(a) {
  return goog.module.getInternal_(a);
};
goog.module.getInternal_ = function(a) {
  if (!COMPILED) {
    if (a in goog.loadedModules_) {
      return goog.loadedModules_[a];
    }
    if (!goog.implicitNamespaces_[a]) {
      return a = goog.getObjectByName(a), null != a ? a : null;
    }
  }
  return null;
};
goog.moduleLoaderState_ = null;
goog.isInModuleLoader_ = function() {
  return null != goog.moduleLoaderState_;
};
goog.module.declareLegacyNamespace = function() {
  if (!COMPILED && !goog.isInModuleLoader_()) {
    throw Error("goog.module.declareLegacyNamespace must be called from within a goog.module");
  }
  if (!COMPILED && !goog.moduleLoaderState_.moduleName) {
    throw Error("goog.module must be called prior to goog.module.declareLegacyNamespace.");
  }
  goog.moduleLoaderState_.declareLegacyNamespace = !0;
};
goog.setTestOnly = function(a) {
  if (goog.DISALLOW_TEST_ONLY_CODE) {
    throw a = a || "", Error("Importing test-only code into non-debug environment" + (a ? ": " + a : "."));
  }
};
goog.forwardDeclare = function(a) {
};
COMPILED || (goog.isProvided_ = function(a) {
  return a in goog.loadedModules_ || !goog.implicitNamespaces_[a] && goog.isDefAndNotNull(goog.getObjectByName(a));
}, goog.implicitNamespaces_ = {"goog.module":!0});
goog.getObjectByName = function(a, b) {
  for (var c = a.split("."), d = b || goog.global, e = 0; e < c.length; e++) {
    if (d = d[c[e]], !goog.isDefAndNotNull(d)) {
      return null;
    }
  }
  return d;
};
goog.globalize = function(a, b) {
  var c = b || goog.global, d;
  for (d in a) {
    c[d] = a[d];
  }
};
goog.addDependency = function(a, b, c, d) {
  if (goog.DEPENDENCIES_ENABLED) {
    var e = goog.getLoader_();
    e && e.addDependency(a, b, c, d);
  }
};
goog.ENABLE_DEBUG_LOADER = !0;
goog.logToConsole_ = function(a) {
  goog.global.console && goog.global.console.error(a);
};
goog.require = function(a) {
  goog.ENABLE_DEBUG_LOADER && goog.debugLoader_ && goog.getLoader_().earlyProcessLoad(a);
  if (!COMPILED) {
    if (goog.isProvided_(a)) {
      if (goog.isInModuleLoader_()) {
        return goog.module.getInternal_(a);
      }
    } else {
      if (goog.ENABLE_DEBUG_LOADER) {
        var b = goog.moduleLoaderState_;
        goog.moduleLoaderState_ = null;
        try {
          var c = goog.getLoader_();
          c ? c.load(a) : goog.logToConsole_("Could not load " + a + " because there is no debug loader.");
        } finally {
          goog.moduleLoaderState_ = b;
        }
      }
    }
    return null;
  }
};
goog.basePath = "";
goog.nullFunction = function() {
};
goog.abstractMethod = function() {
  throw Error("unimplemented abstract method");
};
goog.addSingletonGetter = function(a) {
  a.instance_ = void 0;
  a.getInstance = function() {
    if (a.instance_) {
      return a.instance_;
    }
    goog.DEBUG && (goog.instantiatedSingletons_[goog.instantiatedSingletons_.length] = a);
    return a.instance_ = new a;
  };
};
goog.instantiatedSingletons_ = [];
goog.LOAD_MODULE_USING_EVAL = !0;
goog.SEAL_MODULE_EXPORTS = goog.DEBUG;
goog.loadedModules_ = {};
goog.DEPENDENCIES_ENABLED = !COMPILED && goog.ENABLE_DEBUG_LOADER;
goog.TRANSPILE = "detect";
goog.TRANSPILER = "transpile.js";
goog.DEBUG_LOADER = "";
goog.hasBadLetScoping = null;
goog.useSafari10Workaround = function() {
  if (null == goog.hasBadLetScoping) {
    try {
      var a = !eval('"use strict";let x = 1; function f() { return typeof x; };f() == "number";');
    } catch (b) {
      a = !1;
    }
    goog.hasBadLetScoping = a;
  }
  return goog.hasBadLetScoping;
};
goog.workaroundSafari10EvalBug = function(a) {
  return "(function(){" + a + "\n;})();\n";
};
goog.loadModule = function(a) {
  var b = goog.moduleLoaderState_;
  try {
    goog.moduleLoaderState_ = {moduleName:void 0, declareLegacyNamespace:!1};
    if (goog.isFunction(a)) {
      var c = a.call(void 0, {});
    } else {
      if (goog.isString(a)) {
        goog.useSafari10Workaround() && (a = goog.workaroundSafari10EvalBug(a)), c = goog.loadModuleFromSource_.call(void 0, a);
      } else {
        throw Error("Invalid module definition");
      }
    }
    var d = goog.moduleLoaderState_.moduleName;
    if (!goog.isString(d) || !d) {
      throw Error('Invalid module name "' + d + '"');
    }
    goog.moduleLoaderState_.declareLegacyNamespace ? goog.constructNamespace_(d, c) : goog.SEAL_MODULE_EXPORTS && Object.seal && "object" == typeof c && null != c && Object.seal(c);
    goog.loadedModules_[d] = c;
  } finally {
    goog.moduleLoaderState_ = b;
  }
};
goog.loadModuleFromSource_ = function(a) {
  eval(a);
  return {};
};
goog.normalizePath_ = function(a) {
  a = a.split("/");
  for (var b = 0; b < a.length;) {
    "." == a[b] ? a.splice(b, 1) : b && ".." == a[b] && a[b - 1] && ".." != a[b - 1] ? a.splice(--b, 2) : b++;
  }
  return a.join("/");
};
goog.loadFileSync_ = function(a) {
  if (goog.global.CLOSURE_LOAD_FILE_SYNC) {
    return goog.global.CLOSURE_LOAD_FILE_SYNC(a);
  }
  try {
    var b = new goog.global.XMLHttpRequest;
    b.open("get", a, !1);
    b.send();
    return 0 == b.status || 200 == b.status ? b.responseText : null;
  } catch (c) {
    return null;
  }
};
goog.transpile_ = function(a, b) {
  var c = goog.global.$jscomp;
  c || (goog.global.$jscomp = c = {});
  var d = c.transpile;
  if (!d) {
    var e = goog.basePath + goog.TRANSPILER, f = goog.loadFileSync_(e);
    if (f) {
      (function() {
        eval(f + "\n//# sourceURL=" + e);
      }).call(goog.global);
      if (goog.global.$gwtExport && goog.global.$gwtExport.$jscomp && !goog.global.$gwtExport.$jscomp.transpile) {
        throw Error('The transpiler did not properly export the "transpile" method. $gwtExport: ' + JSON.stringify(goog.global.$gwtExport));
      }
      goog.global.$jscomp.transpile = goog.global.$gwtExport.$jscomp.transpile;
      c = goog.global.$jscomp;
      d = c.transpile;
    }
  }
  d || (d = c.transpile = function(a, b) {
    goog.logToConsole_(b + " requires transpilation but no transpiler was found.");
    return a;
  });
  return d(a, b);
};
goog.typeOf = function(a) {
  var b = typeof a;
  if ("object" == b) {
    if (a) {
      if (a instanceof Array) {
        return "array";
      }
      if (a instanceof Object) {
        return b;
      }
      var c = Object.prototype.toString.call(a);
      if ("[object Window]" == c) {
        return "object";
      }
      if ("[object Array]" == c || "number" == typeof a.length && "undefined" != typeof a.splice && "undefined" != typeof a.propertyIsEnumerable && !a.propertyIsEnumerable("splice")) {
        return "array";
      }
      if ("[object Function]" == c || "undefined" != typeof a.call && "undefined" != typeof a.propertyIsEnumerable && !a.propertyIsEnumerable("call")) {
        return "function";
      }
    } else {
      return "null";
    }
  } else {
    if ("function" == b && "undefined" == typeof a.call) {
      return "object";
    }
  }
  return b;
};
goog.isNull = function(a) {
  return null === a;
};
goog.isDefAndNotNull = function(a) {
  return null != a;
};
goog.isArray = function(a) {
  return "array" == goog.typeOf(a);
};
goog.isArrayLike = function(a) {
  var b = goog.typeOf(a);
  return "array" == b || "object" == b && "number" == typeof a.length;
};
goog.isDateLike = function(a) {
  return goog.isObject(a) && "function" == typeof a.getFullYear;
};
goog.isFunction = function(a) {
  return "function" == goog.typeOf(a);
};
goog.isObject = function(a) {
  var b = typeof a;
  return "object" == b && null != a || "function" == b;
};
goog.getUid = function(a) {
  return a[goog.UID_PROPERTY_] || (a[goog.UID_PROPERTY_] = ++goog.uidCounter_);
};
goog.hasUid = function(a) {
  return !!a[goog.UID_PROPERTY_];
};
goog.removeUid = function(a) {
  null !== a && "removeAttribute" in a && a.removeAttribute(goog.UID_PROPERTY_);
  try {
    delete a[goog.UID_PROPERTY_];
  } catch (b) {
  }
};
goog.UID_PROPERTY_ = "closure_uid_" + (1e9 * Math.random() >>> 0);
goog.uidCounter_ = 0;
goog.getHashCode = goog.getUid;
goog.removeHashCode = goog.removeUid;
goog.cloneObject = function(a) {
  var b = goog.typeOf(a);
  if ("object" == b || "array" == b) {
    if (a.clone) {
      return a.clone();
    }
    b = "array" == b ? [] : {};
    for (var c in a) {
      b[c] = goog.cloneObject(a[c]);
    }
    return b;
  }
  return a;
};
goog.bindNative_ = function(a, b, c) {
  return a.call.apply(a.bind, arguments);
};
goog.bindJs_ = function(a, b, c) {
  if (!a) {
    throw Error();
  }
  if (2 < arguments.length) {
    var d = Array.prototype.slice.call(arguments, 2);
    return function() {
      var c = Array.prototype.slice.call(arguments);
      Array.prototype.unshift.apply(c, d);
      return a.apply(b, c);
    };
  }
  return function() {
    return a.apply(b, arguments);
  };
};
goog.bind = function(a, b, c) {
  Function.prototype.bind && -1 != Function.prototype.bind.toString().indexOf("native code") ? goog.bind = goog.bindNative_ : goog.bind = goog.bindJs_;
  return goog.bind.apply(null, arguments);
};
goog.partial = function(a, b) {
  var c = Array.prototype.slice.call(arguments, 1);
  return function() {
    var b = c.slice();
    b.push.apply(b, arguments);
    return a.apply(this, b);
  };
};
goog.mixin = function(a, b) {
  for (var c in b) {
    a[c] = b[c];
  }
};
goog.now = goog.TRUSTED_SITE && Date.now || function() {
  return +new Date;
};
goog.globalEval = function(a) {
  if (goog.global.execScript) {
    goog.global.execScript(a, "JavaScript");
  } else {
    if (goog.global.eval) {
      if (null == goog.evalWorksForGlobals_) {
        try {
          goog.global.eval("var _evalTest_ = 1;");
        } catch (d) {
        }
        if ("undefined" != typeof goog.global._evalTest_) {
          try {
            delete goog.global._evalTest_;
          } catch (d) {
          }
          goog.evalWorksForGlobals_ = !0;
        } else {
          goog.evalWorksForGlobals_ = !1;
        }
      }
      if (goog.evalWorksForGlobals_) {
        goog.global.eval(a);
      } else {
        var b = goog.global.document, c = b.createElement("SCRIPT");
        c.type = "text/javascript";
        c.defer = !1;
        c.appendChild(b.createTextNode(a));
        b.head.appendChild(c);
        b.head.removeChild(c);
      }
    } else {
      throw Error("goog.globalEval not available");
    }
  }
};
goog.evalWorksForGlobals_ = null;
goog.getCssName = function(a, b) {
  if ("." == String(a).charAt(0)) {
    throw Error('className passed in goog.getCssName must not start with ".". You passed: ' + a);
  }
  var c = function(a) {
    return goog.cssNameMapping_[a] || a;
  }, d = function(a) {
    a = a.split("-");
    for (var b = [], d = 0; d < a.length; d++) {
      b.push(c(a[d]));
    }
    return b.join("-");
  };
  d = goog.cssNameMapping_ ? "BY_WHOLE" == goog.cssNameMappingStyle_ ? c : d : function(a) {
    return a;
  };
  d = b ? a + "-" + d(b) : d(a);
  return goog.global.CLOSURE_CSS_NAME_MAP_FN ? goog.global.CLOSURE_CSS_NAME_MAP_FN(d) : d;
};
goog.setCssNameMapping = function(a, b) {
  goog.cssNameMapping_ = a;
  goog.cssNameMappingStyle_ = b;
};
!COMPILED && goog.global.CLOSURE_CSS_NAME_MAPPING && (goog.cssNameMapping_ = goog.global.CLOSURE_CSS_NAME_MAPPING);
goog.getMsg = function(a, b) {
  b && (a = a.replace(/\{\$([^}]+)}/g, function(a, d) {
    return null != b && d in b ? b[d] : a;
  }));
  return a;
};
goog.getMsgWithFallback = function(a, b) {
  return a;
};
goog.exportSymbol = function(a, b, c) {
  goog.exportPath_(a, b, c);
};
goog.exportProperty = function(a, b, c) {
  a[b] = c;
};
goog.inherits = function(a, b) {
  function c() {
  }
  c.prototype = b.prototype;
  a.superClass_ = b.prototype;
  a.prototype = new c;
  a.prototype.constructor = a;
  a.base = function(a, c, f) {
    for (var d = Array(arguments.length - 2), e = 2; e < arguments.length; e++) {
      d[e - 2] = arguments[e];
    }
    return b.prototype[c].apply(a, d);
  };
};
goog.base = function(a, b, c) {
  var d = arguments.callee.caller;
  if (goog.STRICT_MODE_COMPATIBLE || goog.DEBUG && !d) {
    throw Error("arguments.caller not defined.  goog.base() cannot be used with strict mode code. See http://www.ecma-international.org/ecma-262/5.1/#sec-C");
  }
  if (d.superClass_) {
    for (var e = Array(arguments.length - 1), f = 1; f < arguments.length; f++) {
      e[f - 1] = arguments[f];
    }
    return d.superClass_.constructor.apply(a, e);
  }
  e = Array(arguments.length - 2);
  for (f = 2; f < arguments.length; f++) {
    e[f - 2] = arguments[f];
  }
  f = !1;
  for (var g = a.constructor; g; g = g.superClass_ && g.superClass_.constructor) {
    if (g.prototype[b] === d) {
      f = !0;
    } else {
      if (f) {
        return g.prototype[b].apply(a, e);
      }
    }
  }
  if (a[b] === d) {
    return a.constructor.prototype[b].apply(a, e);
  }
  throw Error("goog.base called from a method of one name to a method of a different name");
};
goog.scope = function(a) {
  if (goog.isInModuleLoader_()) {
    throw Error("goog.scope is not supported within a goog.module.");
  }
  a.call(goog.global);
};
COMPILED || (goog.global.COMPILED = COMPILED);
goog.defineClass = function(a, b) {
  var c = b.constructor, d = b.statics;
  c && c != Object.prototype.constructor || (c = function() {
    throw Error("cannot instantiate an interface (no constructor defined).");
  });
  c = goog.defineClass.createSealingConstructor_(c, a);
  a && goog.inherits(c, a);
  delete b.constructor;
  delete b.statics;
  goog.defineClass.applyProperties_(c.prototype, b);
  null != d && (d instanceof Function ? d(c) : goog.defineClass.applyProperties_(c, d));
  return c;
};
goog.defineClass.SEAL_CLASS_INSTANCES = goog.DEBUG;
goog.defineClass.createSealingConstructor_ = function(a, b) {
  if (!goog.defineClass.SEAL_CLASS_INSTANCES) {
    return a;
  }
  var c = !goog.defineClass.isUnsealable_(b), d = function() {
    var b = a.apply(this, arguments) || this;
    b[goog.UID_PROPERTY_] = b[goog.UID_PROPERTY_];
    this.constructor === d && c && Object.seal instanceof Function && Object.seal(b);
    return b;
  };
  return d;
};
goog.defineClass.isUnsealable_ = function(a) {
  return a && a.prototype && a.prototype[goog.UNSEALABLE_CONSTRUCTOR_PROPERTY_];
};
goog.defineClass.OBJECT_PROTOTYPE_FIELDS_ = "constructor hasOwnProperty isPrototypeOf propertyIsEnumerable toLocaleString toString valueOf".split(" ");
goog.defineClass.applyProperties_ = function(a, b) {
  for (var c in b) {
    Object.prototype.hasOwnProperty.call(b, c) && (a[c] = b[c]);
  }
  for (var d = 0; d < goog.defineClass.OBJECT_PROTOTYPE_FIELDS_.length; d++) {
    c = goog.defineClass.OBJECT_PROTOTYPE_FIELDS_[d], Object.prototype.hasOwnProperty.call(b, c) && (a[c] = b[c]);
  }
};
goog.tagUnsealableClass = function(a) {
  !COMPILED && goog.defineClass.SEAL_CLASS_INSTANCES && (a.prototype[goog.UNSEALABLE_CONSTRUCTOR_PROPERTY_] = !0);
};
goog.UNSEALABLE_CONSTRUCTOR_PROPERTY_ = "goog_defineClass_legacy_unsealable";
goog.DEPENDENCIES_ENABLED && (goog.inHtmlDocument_ = function() {
  var a = goog.global.document;
  return null != a && "write" in a;
}, goog.findBasePath_ = function() {
  if (goog.isDef(goog.global.CLOSURE_BASE_PATH) && goog.isString(goog.global.CLOSURE_BASE_PATH)) {
    goog.basePath = goog.global.CLOSURE_BASE_PATH;
  } else {
    if (goog.inHtmlDocument_()) {
      var a = goog.global.document, b = a.currentScript;
      a = b ? [b] : a.getElementsByTagName("SCRIPT");
      for (b = a.length - 1; 0 <= b; --b) {
        var c = a[b].src, d = c.lastIndexOf("?");
        d = -1 == d ? c.length : d;
        if ("base.js" == c.substr(d - 7, 7)) {
          goog.basePath = c.substr(0, d - 7);
          break;
        }
      }
    }
  }
}, goog.findBasePath_(), goog.Transpiler = function() {
  this.requiresTranspilation_ = null;
}, goog.Transpiler.prototype.createRequiresTranspilation_ = function() {
  function a(a, b) {
    d ? c[a] = !0 : b() ? c[a] = !1 : d = c[a] = !0;
  }
  function b(a) {
    try {
      return !!eval(a);
    } catch (g) {
      return !1;
    }
  }
  var c = {es3:!1}, d = !1, e = goog.global.navigator && goog.global.navigator.userAgent ? goog.global.navigator.userAgent : "";
  a("es5", function() {
    return b("[1,].length==1");
  });
  a("es6", function() {
    var a = e.match(/Edge\/(\d+)(\.\d)*/i);
    return a && 15 > Number(a[1]) ? !1 : b('(()=>{"use strict";class X{constructor(){if(new.target!=String)throw 1;this.x=42}}let q=Reflect.construct(X,[],String);if(q.x!=42||!(q instanceof String))throw 1;for(const a of[2,3]){if(a==2)continue;function f(z={a}){let a=0;return z.a}{function f(){return 0;}}return f()==3}})()');
  });
  a("es6-impl", function() {
    return !0;
  });
  a("es7", function() {
    return b("2 ** 2 == 4");
  });
  a("es8", function() {
    return b("async () => 1, true");
  });
  return c;
}, goog.Transpiler.prototype.needsTranspile = function(a) {
  if ("always" == goog.TRANSPILE) {
    return !0;
  }
  if ("never" == goog.TRANSPILE) {
    return !1;
  }
  this.requiresTranspilation_ || (this.requiresTranspilation_ = this.createRequiresTranspilation_());
  if (a in this.requiresTranspilation_) {
    return this.requiresTranspilation_[a];
  }
  throw Error("Unknown language mode: " + a);
}, goog.Transpiler.prototype.transpile = function(a, b) {
  return goog.transpile_(a, b);
}, goog.transpiler_ = new goog.Transpiler, goog.DebugLoader = function() {
  this.dependencies_ = {loadFlags:{}, nameToPath:{}, requires:{}, visited:{}, written:{}, deferred:{}};
  this.oldIeWaiting_ = !1;
  this.queuedModules_ = [];
  this.lastNonModuleScriptIndex_ = 0;
}, goog.DebugLoader.IS_OLD_IE_ = !(goog.global.atob || !goog.global.document || !goog.global.document.all), goog.DebugLoader.prototype.earlyProcessLoad = function(a) {
  goog.DebugLoader.IS_OLD_IE_ && this.maybeProcessDeferredDep_(a);
}, goog.DebugLoader.prototype.load = function(a) {
  var b = this.getPathFromDeps_(a);
  if (b) {
    var c = function(a) {
      if (!(a in f.written || a in f.visited)) {
        f.visited[a] = !0;
        if (a in f.requires) {
          for (var b in f.requires[a]) {
            if (!g.isProvided(b)) {
              if (b in f.nameToPath) {
                c(f.nameToPath[b]);
              } else {
                throw Error("Undefined nameToPath for " + b);
              }
            }
          }
        }
        a in e || (e[a] = !0, d.push(a));
      }
    }, d = [], e = {}, f = this.dependencies_, g = this;
    c(b);
    for (a = 0; a < d.length; a++) {
      b = d[a], this.dependencies_.written[b] = !0;
    }
    for (a = 0; a < d.length; a++) {
      if (b = d[a]) {
        var h = f.loadFlags[b] || {}, k = h.lang || "es3";
        k = this.getTranspiler().needsTranspile(k);
        "goog" == h.module || k ? this.importProcessedScript_(goog.basePath + b, "goog" == h.module, k) : this.importScript_(goog.basePath + b);
      } else {
        throw Error("Undefined script input");
      }
    }
  } else {
    throw a = "goog.require could not find: " + a, this.logToConsole(a), Error(a);
  }
}, goog.DebugLoader.prototype.addDependency = function(a, b, c, d) {
  var e;
  a = a.replace(/\\/g, "/");
  var f = this.dependencies_;
  d && "boolean" !== typeof d || (d = d ? {module:"goog"} : {});
  for (var g = 0; e = b[g]; g++) {
    f.nameToPath[e] = a, f.loadFlags[a] = d;
  }
  for (d = 0; b = c[d]; d++) {
    a in f.requires || (f.requires[a] = {}), f.requires[a][b] = !0;
  }
}, goog.DebugLoader.prototype.importScript_ = function(a, b) {
  (goog.global.CLOSURE_IMPORT_SCRIPT || goog.bind(this.writeScriptTag_, this))(a, b) && (this.dependencies_.written[a] = !0);
}, goog.DebugLoader.prototype.importProcessedScript_ = function(a, b, c) {
  this.importScript_("", 'goog.debugLoader_.retrieveAndExec_("' + a + '", ' + b + ", " + c + ");");
}, goog.DebugLoader.prototype.retrieveAndExec_ = function(a, b, c) {
  if (!COMPILED) {
    var d = a;
    a = this.normalizePath(a);
    var e = goog.global.CLOSURE_IMPORT_SCRIPT || goog.bind(this.writeScriptTag_, this), f = this.loadFileSync(a);
    if (null == f) {
      throw Error('Load of "' + a + '" failed');
    }
    c && (f = this.getTranspiler().transpile(f, a));
    f = b ? this.wrapModule_(a, f) : f + ("\n//# sourceURL=" + a);
    goog.DebugLoader.IS_OLD_IE_ && this.oldIeWaiting_ ? (this.dependencies_.deferred[d] = f, this.queuedModules_.push(d)) : e(a, f);
  }
}, goog.DebugLoader.prototype.wrapModule_ = function(a, b) {
  return goog.LOAD_MODULE_USING_EVAL && goog.isDef(goog.global.JSON) ? "goog.loadModule(" + goog.global.JSON.stringify(b + "\n//# sourceURL=" + a + "\n") + ");" : 'goog.loadModule(function(exports) {"use strict";' + b + "\n;return exports});\n//# sourceURL=" + a + "\n";
}, goog.DebugLoader.prototype.loadQueuedModules_ = function() {
  var a = this.queuedModules_.length;
  if (0 < a) {
    var b = this.queuedModules_;
    this.queuedModules_ = [];
    for (var c = 0; c < a; c++) {
      this.maybeProcessDeferredPath_(b[c]);
    }
  }
  this.oldIeWaiting_ = !1;
}, goog.DebugLoader.prototype.maybeProcessDeferredDep_ = function(a) {
  this.isDeferredModule_(a) && this.allDepsAreAvailable_(a) && (a = this.getPathFromDeps_(a), this.maybeProcessDeferredPath_(goog.basePath + a));
}, goog.DebugLoader.prototype.isDeferredModule_ = function(a) {
  var b = (a = this.getPathFromDeps_(a)) && this.dependencies_.loadFlags[a] || {}, c = b.lang || "es3";
  return a && ("goog" == b.module || this.getTranspiler().needsTranspile(c)) ? goog.basePath + a in this.dependencies_.deferred : !1;
}, goog.DebugLoader.prototype.allDepsAreAvailable_ = function(a) {
  if ((a = this.getPathFromDeps_(a)) && a in this.dependencies_.requires) {
    for (var b in this.dependencies_.requires[a]) {
      if (!this.isProvided(b) && !this.isDeferredModule_(b)) {
        return !1;
      }
    }
  }
  return !0;
}, goog.DebugLoader.prototype.maybeProcessDeferredPath_ = function(a) {
  if (a in this.dependencies_.deferred) {
    var b = this.dependencies_.deferred[a];
    delete this.dependencies_.deferred[a];
    goog.globalEval(b);
  }
}, goog.DebugLoader.prototype.writeScriptSrcNode_ = function(a) {
  goog.global.document.write('<script type="text/javascript" src="' + a + '">\x3c/script>');
}, goog.DebugLoader.prototype.appendScriptSrcNode_ = function(a) {
  var b = goog.global.document, c = b.createElement("script");
  c.type = "text/javascript";
  c.src = a;
  c.defer = !1;
  c.async = !1;
  b.head.appendChild(c);
}, goog.DebugLoader.prototype.writeScriptTag_ = function(a, b) {
  if (this.inHtmlDocument()) {
    var c = goog.global.document;
    if (!goog.ENABLE_CHROME_APP_SAFE_SCRIPT_LOADING && "complete" == c.readyState) {
      if (/\bdeps.js$/.test(a)) {
        return !1;
      }
      throw Error('Cannot write "' + a + '" after document load');
    }
    if (void 0 === b) {
      if (goog.DebugLoader.IS_OLD_IE_) {
        this.oldIeWaiting_ = !0;
        var d = " onreadystatechange='goog.debugLoader_.onScriptLoad_(this, " + ++this.lastNonModuleScriptIndex_ + ")' ";
        c.write('<script type="text/javascript" src="' + a + '"' + d + ">\x3c/script>");
      } else {
        goog.ENABLE_CHROME_APP_SAFE_SCRIPT_LOADING ? this.appendScriptSrcNode_(a) : this.writeScriptSrcNode_(a);
      }
    } else {
      c.write('<script type="text/javascript">' + this.protectScriptTag_(b) + "\x3c/script>");
    }
    return !0;
  }
  return !1;
}, goog.DebugLoader.prototype.protectScriptTag_ = function(a) {
  return a.replace(/<\/(SCRIPT)/ig, "\\x3c/$1");
}, goog.DebugLoader.prototype.onScriptLoad_ = function(a, b) {
  "complete" == a.readyState && this.lastNonModuleScriptIndex_ == b && this.loadQueuedModules_();
  return !0;
}, goog.DebugLoader.prototype.getPathFromDeps_ = function(a) {
  return a in this.dependencies_.nameToPath ? this.dependencies_.nameToPath[a] : null;
}, goog.DebugLoader.prototype.getTranspiler = function() {
  return goog.transpiler_;
}, goog.DebugLoader.prototype.isProvided = function(a) {
  return goog.isProvided_(a);
}, goog.DebugLoader.prototype.inHtmlDocument = function() {
  return goog.inHtmlDocument_();
}, goog.DebugLoader.prototype.logToConsole = function(a) {
  goog.logToConsole_(a);
}, goog.DebugLoader.prototype.loadFileSync = function(a) {
  return goog.loadFileSync_(a);
}, goog.DebugLoader.prototype.normalizePath = function(a) {
  return goog.normalizePath_(a);
}, goog.debugLoader_ = null, goog.registerDebugLoader = function(a) {
  if (goog.debugLoader_) {
    throw Error("Debug loader already registered!");
  }
  if (!(a instanceof goog.DebugLoader)) {
    throw Error("Not a goog.DebugLoader.");
  }
  goog.debugLoader_ = a;
}, goog.getLoader_ = function() {
  if (!goog.debugLoader_ && goog.DEBUG_LOADER) {
    throw Error("Loaded debug loader file but no loader was registered!");
  }
  goog.debugLoader_ || (goog.debugLoader_ = new goog.DebugLoader);
  return goog.debugLoader_;
}, function() {
  if (goog.DEBUG_LOADER) {
    var a = new goog.DebugLoader;
    a.importScript_(goog.basePath + goog.DEBUG_LOADER);
  }
  goog.global.CLOSURE_NO_DEPS || (a = a || new goog.DebugLoader, goog.DEBUG_LOADER || goog.registerDebugLoader(a), a.importScript_(goog.basePath + "deps.js"));
}());
goog.debug = {};
goog.debug.Error = function(a) {
  if (Error.captureStackTrace) {
    Error.captureStackTrace(this, goog.debug.Error);
  } else {
    var b = Error().stack;
    b && (this.stack = b);
  }
  a && (this.message = String(a));
  this.reportErrorToServer = !0;
};
goog.inherits(goog.debug.Error, Error);
goog.debug.Error.prototype.name = "CustomError";
goog.dom = {};
goog.dom.NodeType = {ELEMENT:1, ATTRIBUTE:2, TEXT:3, CDATA_SECTION:4, ENTITY_REFERENCE:5, ENTITY:6, PROCESSING_INSTRUCTION:7, COMMENT:8, DOCUMENT:9, DOCUMENT_TYPE:10, DOCUMENT_FRAGMENT:11, NOTATION:12};
goog.asserts = {};
goog.asserts.ENABLE_ASSERTS = goog.DEBUG;
goog.asserts.AssertionError = function(a, b) {
  goog.debug.Error.call(this, goog.asserts.subs_(a, b));
  this.messagePattern = a;
};
goog.inherits(goog.asserts.AssertionError, goog.debug.Error);
goog.asserts.AssertionError.prototype.name = "AssertionError";
goog.asserts.DEFAULT_ERROR_HANDLER = function(a) {
  throw a;
};
goog.asserts.errorHandler_ = goog.asserts.DEFAULT_ERROR_HANDLER;
goog.asserts.subs_ = function(a, b) {
  for (var c = a.split("%s"), d = "", e = c.length - 1, f = 0; f < e; f++) {
    d += c[f] + (f < b.length ? b[f] : "%s");
  }
  return d + c[e];
};
goog.asserts.doAssertFailure_ = function(a, b, c, d) {
  var e = "Assertion failed";
  if (c) {
    e += ": " + c;
    var f = d;
  } else {
    a && (e += ": " + a, f = b);
  }
  a = new goog.asserts.AssertionError("" + e, f || []);
  goog.asserts.errorHandler_(a);
};
goog.asserts.setErrorHandler = function(a) {
  goog.asserts.ENABLE_ASSERTS && (goog.asserts.errorHandler_ = a);
};
goog.asserts.assert = function(a, b, c) {
  goog.asserts.ENABLE_ASSERTS && !a && goog.asserts.doAssertFailure_("", null, b, Array.prototype.slice.call(arguments, 2));
  return a;
};
goog.asserts.fail = function(a, b) {
  goog.asserts.ENABLE_ASSERTS && goog.asserts.errorHandler_(new goog.asserts.AssertionError("Failure" + (a ? ": " + a : ""), Array.prototype.slice.call(arguments, 1)));
};
goog.asserts.assertNumber = function(a, b, c) {
  goog.asserts.ENABLE_ASSERTS && !goog.isNumber(a) && goog.asserts.doAssertFailure_("Expected number but got %s: %s.", [goog.typeOf(a), a], b, Array.prototype.slice.call(arguments, 2));
  return a;
};
goog.asserts.assertString = function(a, b, c) {
  goog.asserts.ENABLE_ASSERTS && !goog.isString(a) && goog.asserts.doAssertFailure_("Expected string but got %s: %s.", [goog.typeOf(a), a], b, Array.prototype.slice.call(arguments, 2));
  return a;
};
goog.asserts.assertFunction = function(a, b, c) {
  goog.asserts.ENABLE_ASSERTS && !goog.isFunction(a) && goog.asserts.doAssertFailure_("Expected function but got %s: %s.", [goog.typeOf(a), a], b, Array.prototype.slice.call(arguments, 2));
  return a;
};
goog.asserts.assertObject = function(a, b, c) {
  goog.asserts.ENABLE_ASSERTS && !goog.isObject(a) && goog.asserts.doAssertFailure_("Expected object but got %s: %s.", [goog.typeOf(a), a], b, Array.prototype.slice.call(arguments, 2));
  return a;
};
goog.asserts.assertArray = function(a, b, c) {
  goog.asserts.ENABLE_ASSERTS && !goog.isArray(a) && goog.asserts.doAssertFailure_("Expected array but got %s: %s.", [goog.typeOf(a), a], b, Array.prototype.slice.call(arguments, 2));
  return a;
};
goog.asserts.assertBoolean = function(a, b, c) {
  goog.asserts.ENABLE_ASSERTS && !goog.isBoolean(a) && goog.asserts.doAssertFailure_("Expected boolean but got %s: %s.", [goog.typeOf(a), a], b, Array.prototype.slice.call(arguments, 2));
  return a;
};
goog.asserts.assertElement = function(a, b, c) {
  !goog.asserts.ENABLE_ASSERTS || goog.isObject(a) && a.nodeType == goog.dom.NodeType.ELEMENT || goog.asserts.doAssertFailure_("Expected Element but got %s: %s.", [goog.typeOf(a), a], b, Array.prototype.slice.call(arguments, 2));
  return a;
};
goog.asserts.assertInstanceof = function(a, b, c, d) {
  !goog.asserts.ENABLE_ASSERTS || a instanceof b || goog.asserts.doAssertFailure_("Expected instanceof %s but got %s.", [goog.asserts.getType_(b), goog.asserts.getType_(a)], c, Array.prototype.slice.call(arguments, 3));
  return a;
};
goog.asserts.assertFinite = function(a, b, c) {
  !goog.asserts.ENABLE_ASSERTS || "number" == typeof a && isFinite(a) || goog.asserts.doAssertFailure_("Expected %s to be a finite number but it is not.", [a], b, Array.prototype.slice.call(arguments, 2));
  return a;
};
goog.asserts.assertObjectPrototypeIsIntact = function() {
  for (var a in Object.prototype) {
    goog.asserts.fail(a + " should not be enumerable in Object.prototype.");
  }
};
goog.asserts.getType_ = function(a) {
  return a instanceof Function ? a.displayName || a.name || "unknown type name" : a instanceof Object ? a.constructor.displayName || a.constructor.name || Object.prototype.toString.call(a) : null === a ? "null" : typeof a;
};
goog.array = {};
goog.NATIVE_ARRAY_PROTOTYPES = goog.TRUSTED_SITE;
goog.array.ASSUME_NATIVE_FUNCTIONS = !1;
goog.array.peek = function(a) {
  return a[a.length - 1];
};
goog.array.last = goog.array.peek;
goog.array.indexOf = goog.NATIVE_ARRAY_PROTOTYPES && (goog.array.ASSUME_NATIVE_FUNCTIONS || Array.prototype.indexOf) ? function(a, b, c) {
  goog.asserts.assert(null != a.length);
  return Array.prototype.indexOf.call(a, b, c);
} : function(a, b, c) {
  c = null == c ? 0 : 0 > c ? Math.max(0, a.length + c) : c;
  if (goog.isString(a)) {
    return goog.isString(b) && 1 == b.length ? a.indexOf(b, c) : -1;
  }
  for (; c < a.length; c++) {
    if (c in a && a[c] === b) {
      return c;
    }
  }
  return -1;
};
goog.array.lastIndexOf = goog.NATIVE_ARRAY_PROTOTYPES && (goog.array.ASSUME_NATIVE_FUNCTIONS || Array.prototype.lastIndexOf) ? function(a, b, c) {
  goog.asserts.assert(null != a.length);
  return Array.prototype.lastIndexOf.call(a, b, null == c ? a.length - 1 : c);
} : function(a, b, c) {
  c = null == c ? a.length - 1 : c;
  0 > c && (c = Math.max(0, a.length + c));
  if (goog.isString(a)) {
    return goog.isString(b) && 1 == b.length ? a.lastIndexOf(b, c) : -1;
  }
  for (; 0 <= c; c--) {
    if (c in a && a[c] === b) {
      return c;
    }
  }
  return -1;
};
goog.array.forEach = goog.NATIVE_ARRAY_PROTOTYPES && (goog.array.ASSUME_NATIVE_FUNCTIONS || Array.prototype.forEach) ? function(a, b, c) {
  goog.asserts.assert(null != a.length);
  Array.prototype.forEach.call(a, b, c);
} : function(a, b, c) {
  for (var d = a.length, e = goog.isString(a) ? a.split("") : a, f = 0; f < d; f++) {
    f in e && b.call(c, e[f], f, a);
  }
};
goog.array.forEachRight = function(a, b, c) {
  var d = a.length, e = goog.isString(a) ? a.split("") : a;
  for (--d; 0 <= d; --d) {
    d in e && b.call(c, e[d], d, a);
  }
};
goog.array.filter = goog.NATIVE_ARRAY_PROTOTYPES && (goog.array.ASSUME_NATIVE_FUNCTIONS || Array.prototype.filter) ? function(a, b, c) {
  goog.asserts.assert(null != a.length);
  return Array.prototype.filter.call(a, b, c);
} : function(a, b, c) {
  for (var d = a.length, e = [], f = 0, g = goog.isString(a) ? a.split("") : a, h = 0; h < d; h++) {
    if (h in g) {
      var k = g[h];
      b.call(c, k, h, a) && (e[f++] = k);
    }
  }
  return e;
};
goog.array.map = goog.NATIVE_ARRAY_PROTOTYPES && (goog.array.ASSUME_NATIVE_FUNCTIONS || Array.prototype.map) ? function(a, b, c) {
  goog.asserts.assert(null != a.length);
  return Array.prototype.map.call(a, b, c);
} : function(a, b, c) {
  for (var d = a.length, e = Array(d), f = goog.isString(a) ? a.split("") : a, g = 0; g < d; g++) {
    g in f && (e[g] = b.call(c, f[g], g, a));
  }
  return e;
};
goog.array.reduce = goog.NATIVE_ARRAY_PROTOTYPES && (goog.array.ASSUME_NATIVE_FUNCTIONS || Array.prototype.reduce) ? function(a, b, c, d) {
  goog.asserts.assert(null != a.length);
  d && (b = goog.bind(b, d));
  return Array.prototype.reduce.call(a, b, c);
} : function(a, b, c, d) {
  var e = c;
  goog.array.forEach(a, function(c, g) {
    e = b.call(d, e, c, g, a);
  });
  return e;
};
goog.array.reduceRight = goog.NATIVE_ARRAY_PROTOTYPES && (goog.array.ASSUME_NATIVE_FUNCTIONS || Array.prototype.reduceRight) ? function(a, b, c, d) {
  goog.asserts.assert(null != a.length);
  goog.asserts.assert(null != b);
  d && (b = goog.bind(b, d));
  return Array.prototype.reduceRight.call(a, b, c);
} : function(a, b, c, d) {
  var e = c;
  goog.array.forEachRight(a, function(c, g) {
    e = b.call(d, e, c, g, a);
  });
  return e;
};
goog.array.some = goog.NATIVE_ARRAY_PROTOTYPES && (goog.array.ASSUME_NATIVE_FUNCTIONS || Array.prototype.some) ? function(a, b, c) {
  goog.asserts.assert(null != a.length);
  return Array.prototype.some.call(a, b, c);
} : function(a, b, c) {
  for (var d = a.length, e = goog.isString(a) ? a.split("") : a, f = 0; f < d; f++) {
    if (f in e && b.call(c, e[f], f, a)) {
      return !0;
    }
  }
  return !1;
};
goog.array.every = goog.NATIVE_ARRAY_PROTOTYPES && (goog.array.ASSUME_NATIVE_FUNCTIONS || Array.prototype.every) ? function(a, b, c) {
  goog.asserts.assert(null != a.length);
  return Array.prototype.every.call(a, b, c);
} : function(a, b, c) {
  for (var d = a.length, e = goog.isString(a) ? a.split("") : a, f = 0; f < d; f++) {
    if (f in e && !b.call(c, e[f], f, a)) {
      return !1;
    }
  }
  return !0;
};
goog.array.count = function(a, b, c) {
  var d = 0;
  goog.array.forEach(a, function(a, f, g) {
    b.call(c, a, f, g) && ++d;
  }, c);
  return d;
};
goog.array.find = function(a, b, c) {
  b = goog.array.findIndex(a, b, c);
  return 0 > b ? null : goog.isString(a) ? a.charAt(b) : a[b];
};
goog.array.findIndex = function(a, b, c) {
  for (var d = a.length, e = goog.isString(a) ? a.split("") : a, f = 0; f < d; f++) {
    if (f in e && b.call(c, e[f], f, a)) {
      return f;
    }
  }
  return -1;
};
goog.array.findRight = function(a, b, c) {
  b = goog.array.findIndexRight(a, b, c);
  return 0 > b ? null : goog.isString(a) ? a.charAt(b) : a[b];
};
goog.array.findIndexRight = function(a, b, c) {
  var d = a.length, e = goog.isString(a) ? a.split("") : a;
  for (--d; 0 <= d; d--) {
    if (d in e && b.call(c, e[d], d, a)) {
      return d;
    }
  }
  return -1;
};
goog.array.contains = function(a, b) {
  return 0 <= goog.array.indexOf(a, b);
};
goog.array.isEmpty = function(a) {
  return 0 == a.length;
};
goog.array.clear = function(a) {
  if (!goog.isArray(a)) {
    for (var b = a.length - 1; 0 <= b; b--) {
      delete a[b];
    }
  }
  a.length = 0;
};
goog.array.insert = function(a, b) {
  goog.array.contains(a, b) || a.push(b);
};
goog.array.insertAt = function(a, b, c) {
  goog.array.splice(a, c, 0, b);
};
goog.array.insertArrayAt = function(a, b, c) {
  goog.partial(goog.array.splice, a, c, 0).apply(null, b);
};
goog.array.insertBefore = function(a, b, c) {
  var d;
  2 == arguments.length || 0 > (d = goog.array.indexOf(a, c)) ? a.push(b) : goog.array.insertAt(a, b, d);
};
goog.array.remove = function(a, b) {
  var c = goog.array.indexOf(a, b), d;
  (d = 0 <= c) && goog.array.removeAt(a, c);
  return d;
};
goog.array.removeLast = function(a, b) {
  var c = goog.array.lastIndexOf(a, b);
  return 0 <= c ? (goog.array.removeAt(a, c), !0) : !1;
};
goog.array.removeAt = function(a, b) {
  goog.asserts.assert(null != a.length);
  return 1 == Array.prototype.splice.call(a, b, 1).length;
};
goog.array.removeIf = function(a, b, c) {
  b = goog.array.findIndex(a, b, c);
  return 0 <= b ? (goog.array.removeAt(a, b), !0) : !1;
};
goog.array.removeAllIf = function(a, b, c) {
  var d = 0;
  goog.array.forEachRight(a, function(e, f) {
    b.call(c, e, f, a) && goog.array.removeAt(a, f) && d++;
  });
  return d;
};
goog.array.concat = function(a) {
  return Array.prototype.concat.apply([], arguments);
};
goog.array.join = function(a) {
  return Array.prototype.concat.apply([], arguments);
};
goog.array.toArray = function(a) {
  var b = a.length;
  if (0 < b) {
    for (var c = Array(b), d = 0; d < b; d++) {
      c[d] = a[d];
    }
    return c;
  }
  return [];
};
goog.array.clone = goog.array.toArray;
goog.array.extend = function(a, b) {
  for (var c = 1; c < arguments.length; c++) {
    var d = arguments[c];
    if (goog.isArrayLike(d)) {
      var e = a.length || 0, f = d.length || 0;
      a.length = e + f;
      for (var g = 0; g < f; g++) {
        a[e + g] = d[g];
      }
    } else {
      a.push(d);
    }
  }
};
goog.array.splice = function(a, b, c, d) {
  goog.asserts.assert(null != a.length);
  return Array.prototype.splice.apply(a, goog.array.slice(arguments, 1));
};
goog.array.slice = function(a, b, c) {
  goog.asserts.assert(null != a.length);
  return 2 >= arguments.length ? Array.prototype.slice.call(a, b) : Array.prototype.slice.call(a, b, c);
};
goog.array.removeDuplicates = function(a, b, c) {
  b = b || a;
  var d = function(a) {
    return goog.isObject(a) ? "o" + goog.getUid(a) : (typeof a).charAt(0) + a;
  };
  c = c || d;
  d = {};
  for (var e = 0, f = 0; f < a.length;) {
    var g = a[f++], h = c(g);
    Object.prototype.hasOwnProperty.call(d, h) || (d[h] = !0, b[e++] = g);
  }
  b.length = e;
};
goog.array.binarySearch = function(a, b, c) {
  return goog.array.binarySearch_(a, c || goog.array.defaultCompare, !1, b);
};
goog.array.binarySelect = function(a, b, c) {
  return goog.array.binarySearch_(a, b, !0, void 0, c);
};
goog.array.binarySearch_ = function(a, b, c, d, e) {
  for (var f = 0, g = a.length, h; f < g;) {
    var k = f + g >> 1;
    var l = c ? b.call(e, a[k], k, a) : b(d, a[k]);
    0 < l ? f = k + 1 : (g = k, h = !l);
  }
  return h ? f : ~f;
};
goog.array.sort = function(a, b) {
  a.sort(b || goog.array.defaultCompare);
};
goog.array.stableSort = function(a, b) {
  for (var c = Array(a.length), d = 0; d < a.length; d++) {
    c[d] = {index:d, value:a[d]};
  }
  var e = b || goog.array.defaultCompare;
  goog.array.sort(c, function(a, b) {
    return e(a.value, b.value) || a.index - b.index;
  });
  for (d = 0; d < a.length; d++) {
    a[d] = c[d].value;
  }
};
goog.array.sortByKey = function(a, b, c) {
  var d = c || goog.array.defaultCompare;
  goog.array.sort(a, function(a, c) {
    return d(b(a), b(c));
  });
};
goog.array.sortObjectsByKey = function(a, b, c) {
  goog.array.sortByKey(a, function(a) {
    return a[b];
  }, c);
};
goog.array.isSorted = function(a, b, c) {
  b = b || goog.array.defaultCompare;
  for (var d = 1; d < a.length; d++) {
    var e = b(a[d - 1], a[d]);
    if (0 < e || 0 == e && c) {
      return !1;
    }
  }
  return !0;
};
goog.array.equals = function(a, b, c) {
  if (!goog.isArrayLike(a) || !goog.isArrayLike(b) || a.length != b.length) {
    return !1;
  }
  var d = a.length;
  c = c || goog.array.defaultCompareEquality;
  for (var e = 0; e < d; e++) {
    if (!c(a[e], b[e])) {
      return !1;
    }
  }
  return !0;
};
goog.array.compare3 = function(a, b, c) {
  c = c || goog.array.defaultCompare;
  for (var d = Math.min(a.length, b.length), e = 0; e < d; e++) {
    var f = c(a[e], b[e]);
    if (0 != f) {
      return f;
    }
  }
  return goog.array.defaultCompare(a.length, b.length);
};
goog.array.defaultCompare = function(a, b) {
  return a > b ? 1 : a < b ? -1 : 0;
};
goog.array.inverseDefaultCompare = function(a, b) {
  return -goog.array.defaultCompare(a, b);
};
goog.array.defaultCompareEquality = function(a, b) {
  return a === b;
};
goog.array.binaryInsert = function(a, b, c) {
  c = goog.array.binarySearch(a, b, c);
  return 0 > c ? (goog.array.insertAt(a, b, -(c + 1)), !0) : !1;
};
goog.array.binaryRemove = function(a, b, c) {
  b = goog.array.binarySearch(a, b, c);
  return 0 <= b ? goog.array.removeAt(a, b) : !1;
};
goog.array.bucket = function(a, b, c) {
  for (var d = {}, e = 0; e < a.length; e++) {
    var f = a[e], g = b.call(c, f, e, a);
    goog.isDef(g) && (d[g] || (d[g] = [])).push(f);
  }
  return d;
};
goog.array.toObject = function(a, b, c) {
  var d = {};
  goog.array.forEach(a, function(e, f) {
    d[b.call(c, e, f, a)] = e;
  });
  return d;
};
goog.array.range = function(a, b, c) {
  var d = [], e = 0, f = a;
  c = c || 1;
  void 0 !== b && (e = a, f = b);
  if (0 > c * (f - e)) {
    return [];
  }
  if (0 < c) {
    for (a = e; a < f; a += c) {
      d.push(a);
    }
  } else {
    for (a = e; a > f; a += c) {
      d.push(a);
    }
  }
  return d;
};
goog.array.repeat = function(a, b) {
  for (var c = [], d = 0; d < b; d++) {
    c[d] = a;
  }
  return c;
};
goog.array.flatten = function(a) {
  for (var b = [], c = 0; c < arguments.length; c++) {
    var d = arguments[c];
    if (goog.isArray(d)) {
      for (var e = 0; e < d.length; e += 8192) {
        var f = goog.array.slice(d, e, e + 8192);
        f = goog.array.flatten.apply(null, f);
        for (var g = 0; g < f.length; g++) {
          b.push(f[g]);
        }
      }
    } else {
      b.push(d);
    }
  }
  return b;
};
goog.array.rotate = function(a, b) {
  goog.asserts.assert(null != a.length);
  a.length && (b %= a.length, 0 < b ? Array.prototype.unshift.apply(a, a.splice(-b, b)) : 0 > b && Array.prototype.push.apply(a, a.splice(0, -b)));
  return a;
};
goog.array.moveItem = function(a, b, c) {
  goog.asserts.assert(0 <= b && b < a.length);
  goog.asserts.assert(0 <= c && c < a.length);
  b = Array.prototype.splice.call(a, b, 1);
  Array.prototype.splice.call(a, c, 0, b[0]);
};
goog.array.zip = function(a) {
  if (!arguments.length) {
    return [];
  }
  for (var b = [], c = arguments[0].length, d = 1; d < arguments.length; d++) {
    arguments[d].length < c && (c = arguments[d].length);
  }
  for (d = 0; d < c; d++) {
    for (var e = [], f = 0; f < arguments.length; f++) {
      e.push(arguments[f][d]);
    }
    b.push(e);
  }
  return b;
};
goog.array.shuffle = function(a, b) {
  for (var c = b || Math.random, d = a.length - 1; 0 < d; d--) {
    var e = Math.floor(c() * (d + 1)), f = a[d];
    a[d] = a[e];
    a[e] = f;
  }
};
goog.array.copyByIndex = function(a, b) {
  var c = [];
  goog.array.forEach(b, function(b) {
    c.push(a[b]);
  });
  return c;
};
goog.array.concatMap = function(a, b, c) {
  return goog.array.concat.apply([], goog.array.map(a, b, c));
};
goog.object = {};
goog.object.is = function(a, b) {
  return a === b ? 0 !== a || 1 / a === 1 / b : a !== a && b !== b;
};
goog.object.forEach = function(a, b, c) {
  for (var d in a) {
    b.call(c, a[d], d, a);
  }
};
goog.object.filter = function(a, b, c) {
  var d = {}, e;
  for (e in a) {
    b.call(c, a[e], e, a) && (d[e] = a[e]);
  }
  return d;
};
goog.object.map = function(a, b, c) {
  var d = {}, e;
  for (e in a) {
    d[e] = b.call(c, a[e], e, a);
  }
  return d;
};
goog.object.some = function(a, b, c) {
  for (var d in a) {
    if (b.call(c, a[d], d, a)) {
      return !0;
    }
  }
  return !1;
};
goog.object.every = function(a, b, c) {
  for (var d in a) {
    if (!b.call(c, a[d], d, a)) {
      return !1;
    }
  }
  return !0;
};
goog.object.getCount = function(a) {
  var b = 0, c;
  for (c in a) {
    b++;
  }
  return b;
};
goog.object.getAnyKey = function(a) {
  for (var b in a) {
    return b;
  }
};
goog.object.getAnyValue = function(a) {
  for (var b in a) {
    return a[b];
  }
};
goog.object.contains = function(a, b) {
  return goog.object.containsValue(a, b);
};
goog.object.getValues = function(a) {
  var b = [], c = 0, d;
  for (d in a) {
    b[c++] = a[d];
  }
  return b;
};
goog.object.getKeys = function(a) {
  var b = [], c = 0, d;
  for (d in a) {
    b[c++] = d;
  }
  return b;
};
goog.object.getValueByKeys = function(a, b) {
  var c = goog.isArrayLike(b), d = c ? b : arguments;
  for (c = c ? 0 : 1; c < d.length; c++) {
    if (null == a) {
      return;
    }
    a = a[d[c]];
  }
  return a;
};
goog.object.containsKey = function(a, b) {
  return null !== a && b in a;
};
goog.object.containsValue = function(a, b) {
  for (var c in a) {
    if (a[c] == b) {
      return !0;
    }
  }
  return !1;
};
goog.object.findKey = function(a, b, c) {
  for (var d in a) {
    if (b.call(c, a[d], d, a)) {
      return d;
    }
  }
};
goog.object.findValue = function(a, b, c) {
  return (b = goog.object.findKey(a, b, c)) && a[b];
};
goog.object.isEmpty = function(a) {
  for (var b in a) {
    return !1;
  }
  return !0;
};
goog.object.clear = function(a) {
  for (var b in a) {
    delete a[b];
  }
};
goog.object.remove = function(a, b) {
  var c;
  (c = b in a) && delete a[b];
  return c;
};
goog.object.add = function(a, b, c) {
  if (null !== a && b in a) {
    throw Error('The object already contains the key "' + b + '"');
  }
  goog.object.set(a, b, c);
};
goog.object.get = function(a, b, c) {
  return null !== a && b in a ? a[b] : c;
};
goog.object.set = function(a, b, c) {
  a[b] = c;
};
goog.object.setIfUndefined = function(a, b, c) {
  return b in a ? a[b] : a[b] = c;
};
goog.object.setWithReturnValueIfNotSet = function(a, b, c) {
  if (b in a) {
    return a[b];
  }
  c = c();
  return a[b] = c;
};
goog.object.equals = function(a, b) {
  for (var c in a) {
    if (!(c in b) || a[c] !== b[c]) {
      return !1;
    }
  }
  for (c in b) {
    if (!(c in a)) {
      return !1;
    }
  }
  return !0;
};
goog.object.clone = function(a) {
  var b = {}, c;
  for (c in a) {
    b[c] = a[c];
  }
  return b;
};
goog.object.unsafeClone = function(a) {
  var b = goog.typeOf(a);
  if ("object" == b || "array" == b) {
    if (goog.isFunction(a.clone)) {
      return a.clone();
    }
    b = "array" == b ? [] : {};
    for (var c in a) {
      b[c] = goog.object.unsafeClone(a[c]);
    }
    return b;
  }
  return a;
};
goog.object.transpose = function(a) {
  var b = {}, c;
  for (c in a) {
    b[a[c]] = c;
  }
  return b;
};
goog.object.PROTOTYPE_FIELDS_ = "constructor hasOwnProperty isPrototypeOf propertyIsEnumerable toLocaleString toString valueOf".split(" ");
goog.object.extend = function(a, b) {
  for (var c, d, e = 1; e < arguments.length; e++) {
    d = arguments[e];
    for (c in d) {
      a[c] = d[c];
    }
    for (var f = 0; f < goog.object.PROTOTYPE_FIELDS_.length; f++) {
      c = goog.object.PROTOTYPE_FIELDS_[f], Object.prototype.hasOwnProperty.call(d, c) && (a[c] = d[c]);
    }
  }
};
goog.object.create = function(a) {
  var b = arguments.length;
  if (1 == b && goog.isArray(arguments[0])) {
    return goog.object.create.apply(null, arguments[0]);
  }
  if (b % 2) {
    throw Error("Uneven number of arguments");
  }
  for (var c = {}, d = 0; d < b; d += 2) {
    c[arguments[d]] = arguments[d + 1];
  }
  return c;
};
goog.object.createSet = function(a) {
  var b = arguments.length;
  if (1 == b && goog.isArray(arguments[0])) {
    return goog.object.createSet.apply(null, arguments[0]);
  }
  for (var c = {}, d = 0; d < b; d++) {
    c[arguments[d]] = !0;
  }
  return c;
};
goog.object.createImmutableView = function(a) {
  var b = a;
  Object.isFrozen && !Object.isFrozen(a) && (b = Object.create(a), Object.freeze(b));
  return b;
};
goog.object.isImmutableView = function(a) {
  return !!Object.isFrozen && Object.isFrozen(a);
};
goog.object.getAllPropertyNames = function(a, b, c) {
  if (!a) {
    return [];
  }
  if (!Object.getOwnPropertyNames || !Object.getPrototypeOf) {
    return goog.object.getKeys(a);
  }
  for (var d = {}; a && (a !== Object.prototype || b) && (a !== Function.prototype || c);) {
    for (var e = Object.getOwnPropertyNames(a), f = 0; f < e.length; f++) {
      d[e[f]] = !0;
    }
    a = Object.getPrototypeOf(a);
  }
  return goog.object.getKeys(d);
};
goog.string = {};
goog.string.DETECT_DOUBLE_ESCAPING = !1;
goog.string.FORCE_NON_DOM_HTML_UNESCAPING = !1;
goog.string.Unicode = {NBSP:"\u00a0"};
goog.string.startsWith = function(a, b) {
  return 0 == a.lastIndexOf(b, 0);
};
goog.string.endsWith = function(a, b) {
  var c = a.length - b.length;
  return 0 <= c && a.indexOf(b, c) == c;
};
goog.string.caseInsensitiveStartsWith = function(a, b) {
  return 0 == goog.string.caseInsensitiveCompare(b, a.substr(0, b.length));
};
goog.string.caseInsensitiveEndsWith = function(a, b) {
  return 0 == goog.string.caseInsensitiveCompare(b, a.substr(a.length - b.length, b.length));
};
goog.string.caseInsensitiveEquals = function(a, b) {
  return a.toLowerCase() == b.toLowerCase();
};
goog.string.subs = function(a, b) {
  for (var c = a.split("%s"), d = "", e = Array.prototype.slice.call(arguments, 1); e.length && 1 < c.length;) {
    d += c.shift() + e.shift();
  }
  return d + c.join("%s");
};
goog.string.collapseWhitespace = function(a) {
  return a.replace(/[\s\xa0]+/g, " ").replace(/^\s+|\s+$/g, "");
};
goog.string.isEmptyOrWhitespace = function(a) {
  return /^[\s\xa0]*$/.test(a);
};
goog.string.isEmptyString = function(a) {
  return 0 == a.length;
};
goog.string.isEmpty = goog.string.isEmptyOrWhitespace;
goog.string.isEmptyOrWhitespaceSafe = function(a) {
  return goog.string.isEmptyOrWhitespace(goog.string.makeSafe(a));
};
goog.string.isEmptySafe = goog.string.isEmptyOrWhitespaceSafe;
goog.string.isBreakingWhitespace = function(a) {
  return !/[^\t\n\r ]/.test(a);
};
goog.string.isAlpha = function(a) {
  return !/[^a-zA-Z]/.test(a);
};
goog.string.isNumeric = function(a) {
  return !/[^0-9]/.test(a);
};
goog.string.isAlphaNumeric = function(a) {
  return !/[^a-zA-Z0-9]/.test(a);
};
goog.string.isSpace = function(a) {
  return " " == a;
};
goog.string.isUnicodeChar = function(a) {
  return 1 == a.length && " " <= a && "~" >= a || "\u0080" <= a && "\ufffd" >= a;
};
goog.string.stripNewlines = function(a) {
  return a.replace(/(\r\n|\r|\n)+/g, " ");
};
goog.string.canonicalizeNewlines = function(a) {
  return a.replace(/(\r\n|\r|\n)/g, "\n");
};
goog.string.normalizeWhitespace = function(a) {
  return a.replace(/\xa0|\s/g, " ");
};
goog.string.normalizeSpaces = function(a) {
  return a.replace(/\xa0|[ \t]+/g, " ");
};
goog.string.collapseBreakingSpaces = function(a) {
  return a.replace(/[\t\r\n ]+/g, " ").replace(/^[\t\r\n ]+|[\t\r\n ]+$/g, "");
};
goog.string.trim = goog.TRUSTED_SITE && String.prototype.trim ? function(a) {
  return a.trim();
} : function(a) {
  return /^[\s\xa0]*([\s\S]*?)[\s\xa0]*$/.exec(a)[1];
};
goog.string.trimLeft = function(a) {
  return a.replace(/^[\s\xa0]+/, "");
};
goog.string.trimRight = function(a) {
  return a.replace(/[\s\xa0]+$/, "");
};
goog.string.caseInsensitiveCompare = function(a, b) {
  var c = String(a).toLowerCase(), d = String(b).toLowerCase();
  return c < d ? -1 : c == d ? 0 : 1;
};
goog.string.numberAwareCompare_ = function(a, b, c) {
  if (a == b) {
    return 0;
  }
  if (!a) {
    return -1;
  }
  if (!b) {
    return 1;
  }
  for (var d = a.toLowerCase().match(c), e = b.toLowerCase().match(c), f = Math.min(d.length, e.length), g = 0; g < f; g++) {
    c = d[g];
    var h = e[g];
    if (c != h) {
      return a = parseInt(c, 10), !isNaN(a) && (b = parseInt(h, 10), !isNaN(b) && a - b) ? a - b : c < h ? -1 : 1;
    }
  }
  return d.length != e.length ? d.length - e.length : a < b ? -1 : 1;
};
goog.string.intAwareCompare = function(a, b) {
  return goog.string.numberAwareCompare_(a, b, /\d+|\D+/g);
};
goog.string.floatAwareCompare = function(a, b) {
  return goog.string.numberAwareCompare_(a, b, /\d+|\.\d+|\D+/g);
};
goog.string.numerateCompare = goog.string.floatAwareCompare;
goog.string.urlEncode = function(a) {
  return encodeURIComponent(String(a));
};
goog.string.urlDecode = function(a) {
  return decodeURIComponent(a.replace(/\+/g, " "));
};
goog.string.newLineToBr = function(a, b) {
  return a.replace(/(\r\n|\r|\n)/g, b ? "<br />" : "<br>");
};
goog.string.htmlEscape = function(a, b) {
  if (b) {
    a = a.replace(goog.string.AMP_RE_, "&amp;").replace(goog.string.LT_RE_, "&lt;").replace(goog.string.GT_RE_, "&gt;").replace(goog.string.QUOT_RE_, "&quot;").replace(goog.string.SINGLE_QUOTE_RE_, "&#39;").replace(goog.string.NULL_RE_, "&#0;"), goog.string.DETECT_DOUBLE_ESCAPING && (a = a.replace(goog.string.E_RE_, "&#101;"));
  } else {
    if (!goog.string.ALL_RE_.test(a)) {
      return a;
    }
    -1 != a.indexOf("&") && (a = a.replace(goog.string.AMP_RE_, "&amp;"));
    -1 != a.indexOf("<") && (a = a.replace(goog.string.LT_RE_, "&lt;"));
    -1 != a.indexOf(">") && (a = a.replace(goog.string.GT_RE_, "&gt;"));
    -1 != a.indexOf('"') && (a = a.replace(goog.string.QUOT_RE_, "&quot;"));
    -1 != a.indexOf("'") && (a = a.replace(goog.string.SINGLE_QUOTE_RE_, "&#39;"));
    -1 != a.indexOf("\x00") && (a = a.replace(goog.string.NULL_RE_, "&#0;"));
    goog.string.DETECT_DOUBLE_ESCAPING && -1 != a.indexOf("e") && (a = a.replace(goog.string.E_RE_, "&#101;"));
  }
  return a;
};
goog.string.AMP_RE_ = /&/g;
goog.string.LT_RE_ = /</g;
goog.string.GT_RE_ = />/g;
goog.string.QUOT_RE_ = /"/g;
goog.string.SINGLE_QUOTE_RE_ = /'/g;
goog.string.NULL_RE_ = /\x00/g;
goog.string.E_RE_ = /e/g;
goog.string.ALL_RE_ = goog.string.DETECT_DOUBLE_ESCAPING ? /[\x00&<>"'e]/ : /[\x00&<>"']/;
goog.string.unescapeEntities = function(a) {
  return goog.string.contains(a, "&") ? !goog.string.FORCE_NON_DOM_HTML_UNESCAPING && "document" in goog.global ? goog.string.unescapeEntitiesUsingDom_(a) : goog.string.unescapePureXmlEntities_(a) : a;
};
goog.string.unescapeEntitiesWithDocument = function(a, b) {
  return goog.string.contains(a, "&") ? goog.string.unescapeEntitiesUsingDom_(a, b) : a;
};
goog.string.unescapeEntitiesUsingDom_ = function(a, b) {
  var c = {"&amp;":"&", "&lt;":"<", "&gt;":">", "&quot;":'"'};
  var d = b ? b.createElement("div") : goog.global.document.createElement("div");
  return a.replace(goog.string.HTML_ENTITY_PATTERN_, function(a, b) {
    var e = c[a];
    if (e) {
      return e;
    }
    if ("#" == b.charAt(0)) {
      var f = Number("0" + b.substr(1));
      isNaN(f) || (e = String.fromCharCode(f));
    }
    e || (d.innerHTML = a + " ", e = d.firstChild.nodeValue.slice(0, -1));
    return c[a] = e;
  });
};
goog.string.unescapePureXmlEntities_ = function(a) {
  return a.replace(/&([^;]+);/g, function(a, c) {
    switch(c) {
      case "amp":
        return "&";
      case "lt":
        return "<";
      case "gt":
        return ">";
      case "quot":
        return '"';
      default:
        if ("#" == c.charAt(0)) {
          var b = Number("0" + c.substr(1));
          if (!isNaN(b)) {
            return String.fromCharCode(b);
          }
        }
        return a;
    }
  });
};
goog.string.HTML_ENTITY_PATTERN_ = /&([^;\s<&]+);?/g;
goog.string.whitespaceEscape = function(a, b) {
  return goog.string.newLineToBr(a.replace(/  /g, " &#160;"), b);
};
goog.string.preserveSpaces = function(a) {
  return a.replace(/(^|[\n ]) /g, "$1" + goog.string.Unicode.NBSP);
};
goog.string.stripQuotes = function(a, b) {
  for (var c = b.length, d = 0; d < c; d++) {
    var e = 1 == c ? b : b.charAt(d);
    if (a.charAt(0) == e && a.charAt(a.length - 1) == e) {
      return a.substring(1, a.length - 1);
    }
  }
  return a;
};
goog.string.truncate = function(a, b, c) {
  c && (a = goog.string.unescapeEntities(a));
  a.length > b && (a = a.substring(0, b - 3) + "...");
  c && (a = goog.string.htmlEscape(a));
  return a;
};
goog.string.truncateMiddle = function(a, b, c, d) {
  c && (a = goog.string.unescapeEntities(a));
  if (d && a.length > b) {
    d > b && (d = b);
    var e = a.length - d;
    a = a.substring(0, b - d) + "..." + a.substring(e);
  } else {
    a.length > b && (d = Math.floor(b / 2), e = a.length - d, a = a.substring(0, d + b % 2) + "..." + a.substring(e));
  }
  c && (a = goog.string.htmlEscape(a));
  return a;
};
goog.string.specialEscapeChars_ = {"\x00":"\\0", "\b":"\\b", "\f":"\\f", "\n":"\\n", "\r":"\\r", "\t":"\\t", "\x0B":"\\x0B", '"':'\\"', "\\":"\\\\", "<":"<"};
goog.string.jsEscapeCache_ = {"'":"\\'"};
goog.string.quote = function(a) {
  a = String(a);
  for (var b = ['"'], c = 0; c < a.length; c++) {
    var d = a.charAt(c), e = d.charCodeAt(0);
    b[c + 1] = goog.string.specialEscapeChars_[d] || (31 < e && 127 > e ? d : goog.string.escapeChar(d));
  }
  b.push('"');
  return b.join("");
};
goog.string.escapeString = function(a) {
  for (var b = [], c = 0; c < a.length; c++) {
    b[c] = goog.string.escapeChar(a.charAt(c));
  }
  return b.join("");
};
goog.string.escapeChar = function(a) {
  if (a in goog.string.jsEscapeCache_) {
    return goog.string.jsEscapeCache_[a];
  }
  if (a in goog.string.specialEscapeChars_) {
    return goog.string.jsEscapeCache_[a] = goog.string.specialEscapeChars_[a];
  }
  var b = a.charCodeAt(0);
  if (31 < b && 127 > b) {
    var c = a;
  } else {
    if (256 > b) {
      if (c = "\\x", 16 > b || 256 < b) {
        c += "0";
      }
    } else {
      c = "\\u", 4096 > b && (c += "0");
    }
    c += b.toString(16).toUpperCase();
  }
  return goog.string.jsEscapeCache_[a] = c;
};
goog.string.contains = function(a, b) {
  return -1 != a.indexOf(b);
};
goog.string.caseInsensitiveContains = function(a, b) {
  return goog.string.contains(a.toLowerCase(), b.toLowerCase());
};
goog.string.countOf = function(a, b) {
  return a && b ? a.split(b).length - 1 : 0;
};
goog.string.removeAt = function(a, b, c) {
  var d = a;
  0 <= b && b < a.length && 0 < c && (d = a.substr(0, b) + a.substr(b + c, a.length - b - c));
  return d;
};
goog.string.remove = function(a, b) {
  return a.replace(b, "");
};
goog.string.removeAll = function(a, b) {
  var c = new RegExp(goog.string.regExpEscape(b), "g");
  return a.replace(c, "");
};
goog.string.replaceAll = function(a, b, c) {
  b = new RegExp(goog.string.regExpEscape(b), "g");
  return a.replace(b, c.replace(/\$/g, "$$$$"));
};
goog.string.regExpEscape = function(a) {
  return String(a).replace(/([-()\[\]{}+?*.$\^|,:#<!\\])/g, "\\$1").replace(/\x08/g, "\\x08");
};
goog.string.repeat = String.prototype.repeat ? function(a, b) {
  return a.repeat(b);
} : function(a, b) {
  return Array(b + 1).join(a);
};
goog.string.padNumber = function(a, b, c) {
  a = goog.isDef(c) ? a.toFixed(c) : String(a);
  c = a.indexOf(".");
  -1 == c && (c = a.length);
  return goog.string.repeat("0", Math.max(0, b - c)) + a;
};
goog.string.makeSafe = function(a) {
  return null == a ? "" : String(a);
};
goog.string.buildString = function(a) {
  return Array.prototype.join.call(arguments, "");
};
goog.string.getRandomString = function() {
  return Math.floor(2147483648 * Math.random()).toString(36) + Math.abs(Math.floor(2147483648 * Math.random()) ^ goog.now()).toString(36);
};
goog.string.compareVersions = function(a, b) {
  for (var c = 0, d = goog.string.trim(String(a)).split("."), e = goog.string.trim(String(b)).split("."), f = Math.max(d.length, e.length), g = 0; 0 == c && g < f; g++) {
    var h = d[g] || "", k = e[g] || "";
    do {
      h = /(\d*)(\D*)(.*)/.exec(h) || ["", "", "", ""];
      k = /(\d*)(\D*)(.*)/.exec(k) || ["", "", "", ""];
      if (0 == h[0].length && 0 == k[0].length) {
        break;
      }
      c = 0 == h[1].length ? 0 : parseInt(h[1], 10);
      var l = 0 == k[1].length ? 0 : parseInt(k[1], 10);
      c = goog.string.compareElements_(c, l) || goog.string.compareElements_(0 == h[2].length, 0 == k[2].length) || goog.string.compareElements_(h[2], k[2]);
      h = h[3];
      k = k[3];
    } while (0 == c);
  }
  return c;
};
goog.string.compareElements_ = function(a, b) {
  return a < b ? -1 : a > b ? 1 : 0;
};
goog.string.hashCode = function(a) {
  for (var b = 0, c = 0; c < a.length; ++c) {
    b = 31 * b + a.charCodeAt(c) >>> 0;
  }
  return b;
};
goog.string.uniqueStringCounter_ = 2147483648 * Math.random() | 0;
goog.string.createUniqueString = function() {
  return "goog_" + goog.string.uniqueStringCounter_++;
};
goog.string.toNumber = function(a) {
  var b = Number(a);
  return 0 == b && goog.string.isEmptyOrWhitespace(a) ? NaN : b;
};
goog.string.isLowerCamelCase = function(a) {
  return /^[a-z]+([A-Z][a-z]*)*$/.test(a);
};
goog.string.isUpperCamelCase = function(a) {
  return /^([A-Z][a-z]*)+$/.test(a);
};
goog.string.toCamelCase = function(a) {
  return String(a).replace(/\-([a-z])/g, function(a, c) {
    return c.toUpperCase();
  });
};
goog.string.toSelectorCase = function(a) {
  return String(a).replace(/([A-Z])/g, "-$1").toLowerCase();
};
goog.string.toTitleCase = function(a, b) {
  var c = goog.isString(b) ? goog.string.regExpEscape(b) : "\\s";
  return a.replace(new RegExp("(^" + (c ? "|[" + c + "]+" : "") + ")([a-z])", "g"), function(a, b, c) {
    return b + c.toUpperCase();
  });
};
goog.string.capitalize = function(a) {
  return String(a.charAt(0)).toUpperCase() + String(a.substr(1)).toLowerCase();
};
goog.string.parseInt = function(a) {
  isFinite(a) && (a = String(a));
  return goog.isString(a) ? /^\s*-?0x/i.test(a) ? parseInt(a, 16) : parseInt(a, 10) : NaN;
};
goog.string.splitLimit = function(a, b, c) {
  a = a.split(b);
  for (var d = []; 0 < c && a.length;) {
    d.push(a.shift()), c--;
  }
  a.length && d.push(a.join(b));
  return d;
};
goog.string.lastComponent = function(a, b) {
  if (b) {
    "string" == typeof b && (b = [b]);
  } else {
    return a;
  }
  for (var c = -1, d = 0; d < b.length; d++) {
    if ("" != b[d]) {
      var e = a.lastIndexOf(b[d]);
      e > c && (c = e);
    }
  }
  return -1 == c ? a : a.slice(c + 1);
};
goog.string.editDistance = function(a, b) {
  var c = [], d = [];
  if (a == b) {
    return 0;
  }
  if (!a.length || !b.length) {
    return Math.max(a.length, b.length);
  }
  for (var e = 0; e < b.length + 1; e++) {
    c[e] = e;
  }
  for (e = 0; e < a.length; e++) {
    d[0] = e + 1;
    for (var f = 0; f < b.length; f++) {
      d[f + 1] = Math.min(d[f] + 1, c[f + 1] + 1, c[f] + Number(a[e] != b[f]));
    }
    for (f = 0; f < c.length; f++) {
      c[f] = d[f];
    }
  }
  return d[b.length];
};
goog.proto2 = {};
goog.proto2.Descriptor = function(a, b, c) {
  this.messageType_ = a;
  this.name_ = b.name || null;
  this.fullName_ = b.fullName || null;
  this.containingType_ = b.containingType;
  this.fields_ = {};
  for (a = 0; a < c.length; a++) {
    b = c[a], this.fields_[b.getTag()] = b;
  }
};
goog.proto2.Descriptor.prototype.getName = function() {
  return this.name_;
};
goog.proto2.Descriptor.prototype.getFullName = function() {
  return this.fullName_;
};
goog.proto2.Descriptor.prototype.getContainingType = function() {
  return this.containingType_ ? this.containingType_.getDescriptor() : null;
};
goog.proto2.Descriptor.prototype.getFields = function() {
  var a = goog.object.getValues(this.fields_);
  goog.array.sort(a, function(a, c) {
    return a.getTag() - c.getTag();
  });
  return a;
};
goog.proto2.Descriptor.prototype.getFieldsMap = function() {
  return this.fields_;
};
goog.proto2.Descriptor.prototype.findFieldByName = function(a) {
  return goog.object.findValue(this.fields_, function(b, c, d) {
    return b.getName() == a;
  }) || null;
};
goog.proto2.Descriptor.prototype.findFieldByTag = function(a) {
  goog.asserts.assert(goog.string.isNumeric(a));
  return this.fields_[parseInt(a, 10)] || null;
};
goog.proto2.Descriptor.prototype.createMessageInstance = function() {
  return new this.messageType_;
};
goog.proto2.FieldDescriptor = function(a, b, c) {
  this.parent_ = a;
  goog.asserts.assert(goog.string.isNumeric(b));
  this.tag_ = b;
  this.name_ = c.name;
  this.isPacked_ = !!c.packed;
  this.isRepeated_ = !!c.repeated;
  this.isRequired_ = !!c.required;
  this.fieldType_ = c.fieldType;
  this.nativeType_ = c.type;
  this.deserializationConversionPermitted_ = !1;
  switch(this.fieldType_) {
    case goog.proto2.FieldDescriptor.FieldType.INT64:
    case goog.proto2.FieldDescriptor.FieldType.UINT64:
    case goog.proto2.FieldDescriptor.FieldType.FIXED64:
    case goog.proto2.FieldDescriptor.FieldType.SFIXED64:
    case goog.proto2.FieldDescriptor.FieldType.SINT64:
    case goog.proto2.FieldDescriptor.FieldType.FLOAT:
    case goog.proto2.FieldDescriptor.FieldType.DOUBLE:
      this.deserializationConversionPermitted_ = !0;
  }
  this.defaultValue_ = c.defaultValue;
};
goog.proto2.FieldDescriptor.FieldType = {DOUBLE:1, FLOAT:2, INT64:3, UINT64:4, INT32:5, FIXED64:6, FIXED32:7, BOOL:8, STRING:9, GROUP:10, MESSAGE:11, BYTES:12, UINT32:13, ENUM:14, SFIXED32:15, SFIXED64:16, SINT32:17, SINT64:18};
goog.proto2.FieldDescriptor.prototype.getTag = function() {
  return this.tag_;
};
goog.proto2.FieldDescriptor.prototype.getContainingType = function() {
  return this.parent_.prototype.getDescriptor();
};
goog.proto2.FieldDescriptor.prototype.getName = function() {
  return this.name_;
};
goog.proto2.FieldDescriptor.prototype.getDefaultValue = function() {
  if (void 0 === this.defaultValue_) {
    var a = this.nativeType_;
    if (a === Boolean) {
      this.defaultValue_ = !1;
    } else {
      if (a === Number) {
        this.defaultValue_ = 0;
      } else {
        if (a === String) {
          this.defaultValue_ = this.deserializationConversionPermitted_ ? "0" : "";
        } else {
          return new a;
        }
      }
    }
  }
  return this.defaultValue_;
};
goog.proto2.FieldDescriptor.prototype.getFieldType = function() {
  return this.fieldType_;
};
goog.proto2.FieldDescriptor.prototype.getNativeType = function() {
  return this.nativeType_;
};
goog.proto2.FieldDescriptor.prototype.deserializationConversionPermitted = function() {
  return this.deserializationConversionPermitted_;
};
goog.proto2.FieldDescriptor.prototype.getFieldMessageType = function() {
  return this.nativeType_.prototype.getDescriptor();
};
goog.proto2.FieldDescriptor.prototype.isCompositeType = function() {
  return this.fieldType_ == goog.proto2.FieldDescriptor.FieldType.MESSAGE || this.fieldType_ == goog.proto2.FieldDescriptor.FieldType.GROUP;
};
goog.proto2.FieldDescriptor.prototype.isPacked = function() {
  return this.isPacked_;
};
goog.proto2.FieldDescriptor.prototype.isRepeated = function() {
  return this.isRepeated_;
};
goog.proto2.FieldDescriptor.prototype.isRequired = function() {
  return this.isRequired_;
};
goog.proto2.FieldDescriptor.prototype.isOptional = function() {
  return !this.isRepeated_ && !this.isRequired_;
};
goog.proto2.Message = function() {
  this.values_ = {};
  this.fields_ = this.getDescriptor().getFieldsMap();
  this.deserializedFields_ = this.lazyDeserializer_ = null;
};
goog.proto2.Message.FieldType = {DOUBLE:1, FLOAT:2, INT64:3, UINT64:4, INT32:5, FIXED64:6, FIXED32:7, BOOL:8, STRING:9, GROUP:10, MESSAGE:11, BYTES:12, UINT32:13, ENUM:14, SFIXED32:15, SFIXED64:16, SINT32:17, SINT64:18};
goog.proto2.Message.prototype.initializeForLazyDeserializer = function(a, b) {
  this.lazyDeserializer_ = a;
  this.values_ = b;
  this.deserializedFields_ = {};
};
goog.proto2.Message.prototype.setUnknown = function(a, b) {
  goog.asserts.assert(!this.fields_[a], "Field is not unknown in this message");
  goog.asserts.assert(1 <= a, "Tag " + a + ' has value "' + b + '" in descriptor ' + this.getDescriptor().getName());
  goog.asserts.assert(null !== b, "Value cannot be null");
  this.values_[a] = b;
  this.deserializedFields_ && delete this.deserializedFields_[a];
};
goog.proto2.Message.prototype.forEachUnknown = function(a, b) {
  var c = b || this, d;
  for (d in this.values_) {
    var e = Number(d);
    this.fields_[e] || a.call(c, e, this.values_[d]);
  }
};
goog.proto2.Message.prototype.getDescriptor = goog.abstractMethod;
goog.proto2.Message.prototype.has = function(a) {
  goog.asserts.assert(a.getContainingType() == this.getDescriptor(), "The current message does not contain the given field");
  return this.has$Value(a.getTag());
};
goog.proto2.Message.prototype.arrayOf = function(a) {
  goog.asserts.assert(a.getContainingType() == this.getDescriptor(), "The current message does not contain the given field");
  return this.array$Values(a.getTag());
};
goog.proto2.Message.prototype.countOf = function(a) {
  goog.asserts.assert(a.getContainingType() == this.getDescriptor(), "The current message does not contain the given field");
  return this.count$Values(a.getTag());
};
goog.proto2.Message.prototype.get = function(a, b) {
  goog.asserts.assert(a.getContainingType() == this.getDescriptor(), "The current message does not contain the given field");
  return this.get$Value(a.getTag(), b);
};
goog.proto2.Message.prototype.getOrDefault = function(a, b) {
  goog.asserts.assert(a.getContainingType() == this.getDescriptor(), "The current message does not contain the given field");
  return this.get$ValueOrDefault(a.getTag(), b);
};
goog.proto2.Message.prototype.set = function(a, b) {
  goog.asserts.assert(a.getContainingType() == this.getDescriptor(), "The current message does not contain the given field");
  this.set$Value(a.getTag(), b);
};
goog.proto2.Message.prototype.add = function(a, b) {
  goog.asserts.assert(a.getContainingType() == this.getDescriptor(), "The current message does not contain the given field");
  this.add$Value(a.getTag(), b);
};
goog.proto2.Message.prototype.clear = function(a) {
  goog.asserts.assert(a.getContainingType() == this.getDescriptor(), "The current message does not contain the given field");
  this.clear$Field(a.getTag());
};
goog.proto2.Message.prototype.equals = function(a) {
  if (!a || this.constructor != a.constructor) {
    return !1;
  }
  for (var b = this.getDescriptor().getFields(), c = 0; c < b.length; c++) {
    var d = b[c], e = d.getTag();
    if (this.has$Value(e) != a.has$Value(e)) {
      return !1;
    }
    if (this.has$Value(e)) {
      var f = d.isCompositeType(), g = this.getValueForTag_(e);
      e = a.getValueForTag_(e);
      if (d.isRepeated()) {
        if (g.length != e.length) {
          return !1;
        }
        for (d = 0; d < g.length; d++) {
          var h = g[d], k = e[d];
          if (f ? !h.equals(k) : h != k) {
            return !1;
          }
        }
      } else {
        if (f ? !g.equals(e) : g != e) {
          return !1;
        }
      }
    }
  }
  return !0;
};
goog.proto2.Message.prototype.copyFrom = function(a) {
  goog.asserts.assert(this.constructor == a.constructor, "The source message must have the same type.");
  this != a && (this.values_ = {}, this.deserializedFields_ && (this.deserializedFields_ = {}), this.mergeFrom(a));
};
goog.proto2.Message.prototype.mergeFrom = function(a) {
  goog.asserts.assert(this.constructor == a.constructor, "The source message must have the same type.");
  for (var b = this.getDescriptor().getFields(), c = 0; c < b.length; c++) {
    var d = b[c], e = d.getTag();
    if (a.has$Value(e)) {
      this.deserializedFields_ && delete this.deserializedFields_[d.getTag()];
      var f = d.isCompositeType();
      if (d.isRepeated()) {
        d = a.array$Values(e);
        for (var g = 0; g < d.length; g++) {
          this.add$Value(e, f ? d[g].clone() : d[g]);
        }
      } else {
        d = a.getValueForTag_(e), f ? (f = this.getValueForTag_(e)) ? f.mergeFrom(d) : this.set$Value(e, d.clone()) : this.set$Value(e, d);
      }
    }
  }
};
goog.proto2.Message.prototype.clone = function() {
  var a = new this.constructor;
  a.copyFrom(this);
  return a;
};
goog.proto2.Message.prototype.initDefaults = function(a) {
  for (var b = this.getDescriptor().getFields(), c = 0; c < b.length; c++) {
    var d = b[c], e = d.getTag(), f = d.isCompositeType();
    this.has$Value(e) || d.isRepeated() || (f ? this.values_[e] = new (d.getNativeType()) : a && (this.values_[e] = d.getDefaultValue()));
    if (f) {
      if (d.isRepeated()) {
        for (d = this.array$Values(e), e = 0; e < d.length; e++) {
          d[e].initDefaults(a);
        }
      } else {
        this.get$Value(e).initDefaults(a);
      }
    }
  }
};
goog.proto2.Message.prototype.has$Value = function(a) {
  return null != this.values_[a];
};
goog.proto2.Message.prototype.getValueForTag_ = function(a) {
  var b = this.values_[a];
  return goog.isDefAndNotNull(b) ? this.lazyDeserializer_ ? a in this.deserializedFields_ ? this.deserializedFields_[a] : (b = this.lazyDeserializer_.deserializeField(this, this.fields_[a], b), this.deserializedFields_[a] = b) : b : null;
};
goog.proto2.Message.prototype.get$Value = function(a, b) {
  var c = this.getValueForTag_(a);
  if (this.fields_[a].isRepeated()) {
    var d = b || 0;
    goog.asserts.assert(0 <= d && d < c.length, "Given index %s is out of bounds.  Repeated field length: %s", d, c.length);
    return c[d];
  }
  return c;
};
goog.proto2.Message.prototype.get$ValueOrDefault = function(a, b) {
  return this.has$Value(a) ? this.get$Value(a, b) : this.fields_[a].getDefaultValue();
};
goog.proto2.Message.prototype.array$Values = function(a) {
  return this.getValueForTag_(a) || [];
};
goog.proto2.Message.prototype.count$Values = function(a) {
  return this.fields_[a].isRepeated() ? this.has$Value(a) ? this.values_[a].length : 0 : this.has$Value(a) ? 1 : 0;
};
goog.proto2.Message.prototype.set$Value = function(a, b) {
  goog.asserts.ENABLE_ASSERTS && this.checkFieldType_(this.fields_[a], b);
  this.values_[a] = b;
  this.deserializedFields_ && (this.deserializedFields_[a] = b);
};
goog.proto2.Message.prototype.add$Value = function(a, b) {
  goog.asserts.ENABLE_ASSERTS && this.checkFieldType_(this.fields_[a], b);
  this.values_[a] || (this.values_[a] = []);
  this.values_[a].push(b);
  this.deserializedFields_ && delete this.deserializedFields_[a];
};
goog.proto2.Message.prototype.checkFieldType_ = function(a, b) {
  a.getFieldType() == goog.proto2.FieldDescriptor.FieldType.ENUM ? goog.asserts.assertNumber(b) : goog.asserts.assert(Object(b).constructor == a.getNativeType());
};
goog.proto2.Message.prototype.clear$Field = function(a) {
  delete this.values_[a];
  this.deserializedFields_ && delete this.deserializedFields_[a];
};
goog.proto2.Message.createDescriptor = function(a, b) {
  var c = [], d = b[0], e;
  for (e in b) {
    0 != e && c.push(new goog.proto2.FieldDescriptor(a, e, b[e]));
  }
  return new goog.proto2.Descriptor(a, d, c);
};
goog.proto2.Serializer = function() {
};
goog.proto2.Serializer.DECODE_SYMBOLIC_ENUMS = !1;
goog.proto2.Serializer.prototype.serialize = goog.abstractMethod;
goog.proto2.Serializer.prototype.getSerializedValue = function(a, b) {
  return a.isCompositeType() ? this.serialize(b) : goog.isNumber(b) && !isFinite(b) ? b.toString() : b;
};
goog.proto2.Serializer.prototype.deserialize = function(a, b) {
  var c = a.createMessageInstance();
  this.deserializeTo(c, b);
  goog.asserts.assert(c instanceof goog.proto2.Message);
  return c;
};
goog.proto2.Serializer.prototype.deserializeTo = goog.abstractMethod;
goog.proto2.Serializer.prototype.getDeserializedValue = function(a, b) {
  if (a.isCompositeType()) {
    return b instanceof goog.proto2.Message ? b : this.deserialize(a.getFieldMessageType(), b);
  }
  if (a.getFieldType() == goog.proto2.FieldDescriptor.FieldType.ENUM) {
    if (goog.proto2.Serializer.DECODE_SYMBOLIC_ENUMS && goog.isString(b)) {
      var c = a.getNativeType();
      if (c.hasOwnProperty(b)) {
        return c[b];
      }
    }
    return goog.isString(b) && goog.proto2.Serializer.INTEGER_REGEX.test(b) && (c = Number(b), 0 < c) ? c : b;
  }
  if (!a.deserializationConversionPermitted()) {
    return b;
  }
  c = a.getNativeType();
  if (c === String) {
    if (goog.isNumber(b)) {
      return String(b);
    }
  } else {
    if (c === Number && goog.isString(b) && ("Infinity" === b || "-Infinity" === b || "NaN" === b || goog.proto2.Serializer.INTEGER_REGEX.test(b))) {
      return Number(b);
    }
  }
  return b;
};
goog.proto2.Serializer.INTEGER_REGEX = /^-?[0-9]+$/;
goog.proto2.LazyDeserializer = function() {
};
goog.inherits(goog.proto2.LazyDeserializer, goog.proto2.Serializer);
goog.proto2.LazyDeserializer.prototype.deserialize = function(a, b) {
  var c = a.createMessageInstance();
  c.initializeForLazyDeserializer(this, b);
  goog.asserts.assert(c instanceof goog.proto2.Message);
  return c;
};
goog.proto2.LazyDeserializer.prototype.deserializeTo = function(a, b) {
  throw Error("Unimplemented");
};
goog.proto2.LazyDeserializer.prototype.deserializeField = goog.abstractMethod;
goog.proto2.PbLiteSerializer = function() {
};
goog.inherits(goog.proto2.PbLiteSerializer, goog.proto2.LazyDeserializer);
goog.proto2.PbLiteSerializer.prototype.zeroIndexing_ = !1;
goog.proto2.PbLiteSerializer.prototype.setZeroIndexed = function(a) {
  this.zeroIndexing_ = a;
};
goog.proto2.PbLiteSerializer.prototype.serialize = function(a) {
  for (var b = a.getDescriptor().getFields(), c = [], d = this.zeroIndexing_, e = 0; e < b.length; e++) {
    var f = b[e];
    if (a.has(f)) {
      var g = f.getTag();
      g = d ? g - 1 : g;
      if (f.isRepeated()) {
        c[g] = [];
        for (var h = 0; h < a.countOf(f); h++) {
          c[g][h] = this.getSerializedValue(f, a.get(f, h));
        }
      } else {
        c[g] = this.getSerializedValue(f, a.get(f));
      }
    }
  }
  a.forEachUnknown(function(a, b) {
    c[d ? a - 1 : a] = b;
  });
  return c;
};
goog.proto2.PbLiteSerializer.prototype.deserializeField = function(a, b, c) {
  if (null == c) {
    return c;
  }
  if (b.isRepeated()) {
    a = [];
    goog.asserts.assert(goog.isArray(c), "Value must be array: %s", c);
    for (var d = 0; d < c.length; d++) {
      a[d] = this.getDeserializedValue(b, c[d]);
    }
    return a;
  }
  return this.getDeserializedValue(b, c);
};
goog.proto2.PbLiteSerializer.prototype.getSerializedValue = function(a, b) {
  return a.getFieldType() == goog.proto2.FieldDescriptor.FieldType.BOOL ? b ? 1 : 0 : goog.proto2.Serializer.prototype.getSerializedValue.apply(this, arguments);
};
goog.proto2.PbLiteSerializer.prototype.getDeserializedValue = function(a, b) {
  return a.getFieldType() == goog.proto2.FieldDescriptor.FieldType.BOOL ? (goog.asserts.assert(goog.isNumber(b) || goog.isBoolean(b), "Value is expected to be a number or boolean"), !!b) : goog.proto2.Serializer.prototype.getDeserializedValue.apply(this, arguments);
};
goog.proto2.PbLiteSerializer.prototype.deserialize = function(a, b) {
  var c = b;
  if (this.zeroIndexing_) {
    c = [];
    for (var d in b) {
      c[parseInt(d, 10) + 1] = b[d];
    }
  }
  return goog.proto2.PbLiteSerializer.superClass_.deserialize.call(this, a, c);
};
goog.string.StringBuffer = function(a, b) {
  null != a && this.append.apply(this, arguments);
};
goog.string.StringBuffer.prototype.buffer_ = "";
goog.string.StringBuffer.prototype.set = function(a) {
  this.buffer_ = "" + a;
};
goog.string.StringBuffer.prototype.append = function(a, b, c) {
  this.buffer_ += String(a);
  if (null != b) {
    for (var d = 1; d < arguments.length; d++) {
      this.buffer_ += arguments[d];
    }
  }
  return this;
};
goog.string.StringBuffer.prototype.clear = function() {
  this.buffer_ = "";
};
goog.string.StringBuffer.prototype.getLength = function() {
  return this.buffer_.length;
};
goog.string.StringBuffer.prototype.toString = function() {
  return this.buffer_;
};
/*

 Protocol Buffer 2 Copyright 2008 Google Inc.
 All other code copyright its respective owners.
 Copyright (C) 2010 The Libphonenumber Authors

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
*/
var i18n = {phonenumbers:{}};
i18n.phonenumbers.NumberFormat = function() {
  goog.proto2.Message.call(this);
};
goog.inherits(i18n.phonenumbers.NumberFormat, goog.proto2.Message);
i18n.phonenumbers.NumberFormat.descriptor_ = null;
i18n.phonenumbers.NumberFormat.prototype.getPattern = function() {
  return this.get$Value(1);
};
i18n.phonenumbers.NumberFormat.prototype.getPatternOrDefault = function() {
  return this.get$ValueOrDefault(1);
};
i18n.phonenumbers.NumberFormat.prototype.setPattern = function(a) {
  this.set$Value(1, a);
};
i18n.phonenumbers.NumberFormat.prototype.hasPattern = function() {
  return this.has$Value(1);
};
i18n.phonenumbers.NumberFormat.prototype.patternCount = function() {
  return this.count$Values(1);
};
i18n.phonenumbers.NumberFormat.prototype.clearPattern = function() {
  this.clear$Field(1);
};
i18n.phonenumbers.NumberFormat.prototype.getFormat = function() {
  return this.get$Value(2);
};
i18n.phonenumbers.NumberFormat.prototype.getFormatOrDefault = function() {
  return this.get$ValueOrDefault(2);
};
i18n.phonenumbers.NumberFormat.prototype.setFormat = function(a) {
  this.set$Value(2, a);
};
i18n.phonenumbers.NumberFormat.prototype.hasFormat = function() {
  return this.has$Value(2);
};
i18n.phonenumbers.NumberFormat.prototype.formatCount = function() {
  return this.count$Values(2);
};
i18n.phonenumbers.NumberFormat.prototype.clearFormat = function() {
  this.clear$Field(2);
};
i18n.phonenumbers.NumberFormat.prototype.getLeadingDigitsPattern = function(a) {
  return this.get$Value(3, a);
};
i18n.phonenumbers.NumberFormat.prototype.getLeadingDigitsPatternOrDefault = function(a) {
  return this.get$ValueOrDefault(3, a);
};
i18n.phonenumbers.NumberFormat.prototype.addLeadingDigitsPattern = function(a) {
  this.add$Value(3, a);
};
i18n.phonenumbers.NumberFormat.prototype.leadingDigitsPatternArray = function() {
  return this.array$Values(3);
};
i18n.phonenumbers.NumberFormat.prototype.hasLeadingDigitsPattern = function() {
  return this.has$Value(3);
};
i18n.phonenumbers.NumberFormat.prototype.leadingDigitsPatternCount = function() {
  return this.count$Values(3);
};
i18n.phonenumbers.NumberFormat.prototype.clearLeadingDigitsPattern = function() {
  this.clear$Field(3);
};
i18n.phonenumbers.NumberFormat.prototype.getNationalPrefixFormattingRule = function() {
  return this.get$Value(4);
};
i18n.phonenumbers.NumberFormat.prototype.getNationalPrefixFormattingRuleOrDefault = function() {
  return this.get$ValueOrDefault(4);
};
i18n.phonenumbers.NumberFormat.prototype.setNationalPrefixFormattingRule = function(a) {
  this.set$Value(4, a);
};
i18n.phonenumbers.NumberFormat.prototype.hasNationalPrefixFormattingRule = function() {
  return this.has$Value(4);
};
i18n.phonenumbers.NumberFormat.prototype.nationalPrefixFormattingRuleCount = function() {
  return this.count$Values(4);
};
i18n.phonenumbers.NumberFormat.prototype.clearNationalPrefixFormattingRule = function() {
  this.clear$Field(4);
};
i18n.phonenumbers.NumberFormat.prototype.getNationalPrefixOptionalWhenFormatting = function() {
  return this.get$Value(6);
};
i18n.phonenumbers.NumberFormat.prototype.getNationalPrefixOptionalWhenFormattingOrDefault = function() {
  return this.get$ValueOrDefault(6);
};
i18n.phonenumbers.NumberFormat.prototype.setNationalPrefixOptionalWhenFormatting = function(a) {
  this.set$Value(6, a);
};
i18n.phonenumbers.NumberFormat.prototype.hasNationalPrefixOptionalWhenFormatting = function() {
  return this.has$Value(6);
};
i18n.phonenumbers.NumberFormat.prototype.nationalPrefixOptionalWhenFormattingCount = function() {
  return this.count$Values(6);
};
i18n.phonenumbers.NumberFormat.prototype.clearNationalPrefixOptionalWhenFormatting = function() {
  this.clear$Field(6);
};
i18n.phonenumbers.NumberFormat.prototype.getDomesticCarrierCodeFormattingRule = function() {
  return this.get$Value(5);
};
i18n.phonenumbers.NumberFormat.prototype.getDomesticCarrierCodeFormattingRuleOrDefault = function() {
  return this.get$ValueOrDefault(5);
};
i18n.phonenumbers.NumberFormat.prototype.setDomesticCarrierCodeFormattingRule = function(a) {
  this.set$Value(5, a);
};
i18n.phonenumbers.NumberFormat.prototype.hasDomesticCarrierCodeFormattingRule = function() {
  return this.has$Value(5);
};
i18n.phonenumbers.NumberFormat.prototype.domesticCarrierCodeFormattingRuleCount = function() {
  return this.count$Values(5);
};
i18n.phonenumbers.NumberFormat.prototype.clearDomesticCarrierCodeFormattingRule = function() {
  this.clear$Field(5);
};
i18n.phonenumbers.PhoneNumberDesc = function() {
  goog.proto2.Message.call(this);
};
goog.inherits(i18n.phonenumbers.PhoneNumberDesc, goog.proto2.Message);
i18n.phonenumbers.PhoneNumberDesc.descriptor_ = null;
i18n.phonenumbers.PhoneNumberDesc.prototype.getNationalNumberPattern = function() {
  return this.get$Value(2);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.getNationalNumberPatternOrDefault = function() {
  return this.get$ValueOrDefault(2);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.setNationalNumberPattern = function(a) {
  this.set$Value(2, a);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.hasNationalNumberPattern = function() {
  return this.has$Value(2);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.nationalNumberPatternCount = function() {
  return this.count$Values(2);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.clearNationalNumberPattern = function() {
  this.clear$Field(2);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.getPossibleLength = function(a) {
  return this.get$Value(9, a);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.getPossibleLengthOrDefault = function(a) {
  return this.get$ValueOrDefault(9, a);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.addPossibleLength = function(a) {
  this.add$Value(9, a);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.possibleLengthArray = function() {
  return this.array$Values(9);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.hasPossibleLength = function() {
  return this.has$Value(9);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.possibleLengthCount = function() {
  return this.count$Values(9);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.clearPossibleLength = function() {
  this.clear$Field(9);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.getPossibleLengthLocalOnly = function(a) {
  return this.get$Value(10, a);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.getPossibleLengthLocalOnlyOrDefault = function(a) {
  return this.get$ValueOrDefault(10, a);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.addPossibleLengthLocalOnly = function(a) {
  this.add$Value(10, a);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.possibleLengthLocalOnlyArray = function() {
  return this.array$Values(10);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.hasPossibleLengthLocalOnly = function() {
  return this.has$Value(10);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.possibleLengthLocalOnlyCount = function() {
  return this.count$Values(10);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.clearPossibleLengthLocalOnly = function() {
  this.clear$Field(10);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.getExampleNumber = function() {
  return this.get$Value(6);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.getExampleNumberOrDefault = function() {
  return this.get$ValueOrDefault(6);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.setExampleNumber = function(a) {
  this.set$Value(6, a);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.hasExampleNumber = function() {
  return this.has$Value(6);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.exampleNumberCount = function() {
  return this.count$Values(6);
};
i18n.phonenumbers.PhoneNumberDesc.prototype.clearExampleNumber = function() {
  this.clear$Field(6);
};
i18n.phonenumbers.PhoneMetadata = function() {
  goog.proto2.Message.call(this);
};
goog.inherits(i18n.phonenumbers.PhoneMetadata, goog.proto2.Message);
i18n.phonenumbers.PhoneMetadata.descriptor_ = null;
i18n.phonenumbers.PhoneMetadata.prototype.getGeneralDesc = function() {
  return this.get$Value(1);
};
i18n.phonenumbers.PhoneMetadata.prototype.getGeneralDescOrDefault = function() {
  return this.get$ValueOrDefault(1);
};
i18n.phonenumbers.PhoneMetadata.prototype.setGeneralDesc = function(a) {
  this.set$Value(1, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasGeneralDesc = function() {
  return this.has$Value(1);
};
i18n.phonenumbers.PhoneMetadata.prototype.generalDescCount = function() {
  return this.count$Values(1);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearGeneralDesc = function() {
  this.clear$Field(1);
};
i18n.phonenumbers.PhoneMetadata.prototype.getFixedLine = function() {
  return this.get$Value(2);
};
i18n.phonenumbers.PhoneMetadata.prototype.getFixedLineOrDefault = function() {
  return this.get$ValueOrDefault(2);
};
i18n.phonenumbers.PhoneMetadata.prototype.setFixedLine = function(a) {
  this.set$Value(2, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasFixedLine = function() {
  return this.has$Value(2);
};
i18n.phonenumbers.PhoneMetadata.prototype.fixedLineCount = function() {
  return this.count$Values(2);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearFixedLine = function() {
  this.clear$Field(2);
};
i18n.phonenumbers.PhoneMetadata.prototype.getMobile = function() {
  return this.get$Value(3);
};
i18n.phonenumbers.PhoneMetadata.prototype.getMobileOrDefault = function() {
  return this.get$ValueOrDefault(3);
};
i18n.phonenumbers.PhoneMetadata.prototype.setMobile = function(a) {
  this.set$Value(3, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasMobile = function() {
  return this.has$Value(3);
};
i18n.phonenumbers.PhoneMetadata.prototype.mobileCount = function() {
  return this.count$Values(3);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearMobile = function() {
  this.clear$Field(3);
};
i18n.phonenumbers.PhoneMetadata.prototype.getTollFree = function() {
  return this.get$Value(4);
};
i18n.phonenumbers.PhoneMetadata.prototype.getTollFreeOrDefault = function() {
  return this.get$ValueOrDefault(4);
};
i18n.phonenumbers.PhoneMetadata.prototype.setTollFree = function(a) {
  this.set$Value(4, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasTollFree = function() {
  return this.has$Value(4);
};
i18n.phonenumbers.PhoneMetadata.prototype.tollFreeCount = function() {
  return this.count$Values(4);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearTollFree = function() {
  this.clear$Field(4);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPremiumRate = function() {
  return this.get$Value(5);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPremiumRateOrDefault = function() {
  return this.get$ValueOrDefault(5);
};
i18n.phonenumbers.PhoneMetadata.prototype.setPremiumRate = function(a) {
  this.set$Value(5, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasPremiumRate = function() {
  return this.has$Value(5);
};
i18n.phonenumbers.PhoneMetadata.prototype.premiumRateCount = function() {
  return this.count$Values(5);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearPremiumRate = function() {
  this.clear$Field(5);
};
i18n.phonenumbers.PhoneMetadata.prototype.getSharedCost = function() {
  return this.get$Value(6);
};
i18n.phonenumbers.PhoneMetadata.prototype.getSharedCostOrDefault = function() {
  return this.get$ValueOrDefault(6);
};
i18n.phonenumbers.PhoneMetadata.prototype.setSharedCost = function(a) {
  this.set$Value(6, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasSharedCost = function() {
  return this.has$Value(6);
};
i18n.phonenumbers.PhoneMetadata.prototype.sharedCostCount = function() {
  return this.count$Values(6);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearSharedCost = function() {
  this.clear$Field(6);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPersonalNumber = function() {
  return this.get$Value(7);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPersonalNumberOrDefault = function() {
  return this.get$ValueOrDefault(7);
};
i18n.phonenumbers.PhoneMetadata.prototype.setPersonalNumber = function(a) {
  this.set$Value(7, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasPersonalNumber = function() {
  return this.has$Value(7);
};
i18n.phonenumbers.PhoneMetadata.prototype.personalNumberCount = function() {
  return this.count$Values(7);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearPersonalNumber = function() {
  this.clear$Field(7);
};
i18n.phonenumbers.PhoneMetadata.prototype.getVoip = function() {
  return this.get$Value(8);
};
i18n.phonenumbers.PhoneMetadata.prototype.getVoipOrDefault = function() {
  return this.get$ValueOrDefault(8);
};
i18n.phonenumbers.PhoneMetadata.prototype.setVoip = function(a) {
  this.set$Value(8, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasVoip = function() {
  return this.has$Value(8);
};
i18n.phonenumbers.PhoneMetadata.prototype.voipCount = function() {
  return this.count$Values(8);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearVoip = function() {
  this.clear$Field(8);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPager = function() {
  return this.get$Value(21);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPagerOrDefault = function() {
  return this.get$ValueOrDefault(21);
};
i18n.phonenumbers.PhoneMetadata.prototype.setPager = function(a) {
  this.set$Value(21, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasPager = function() {
  return this.has$Value(21);
};
i18n.phonenumbers.PhoneMetadata.prototype.pagerCount = function() {
  return this.count$Values(21);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearPager = function() {
  this.clear$Field(21);
};
i18n.phonenumbers.PhoneMetadata.prototype.getUan = function() {
  return this.get$Value(25);
};
i18n.phonenumbers.PhoneMetadata.prototype.getUanOrDefault = function() {
  return this.get$ValueOrDefault(25);
};
i18n.phonenumbers.PhoneMetadata.prototype.setUan = function(a) {
  this.set$Value(25, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasUan = function() {
  return this.has$Value(25);
};
i18n.phonenumbers.PhoneMetadata.prototype.uanCount = function() {
  return this.count$Values(25);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearUan = function() {
  this.clear$Field(25);
};
i18n.phonenumbers.PhoneMetadata.prototype.getEmergency = function() {
  return this.get$Value(27);
};
i18n.phonenumbers.PhoneMetadata.prototype.getEmergencyOrDefault = function() {
  return this.get$ValueOrDefault(27);
};
i18n.phonenumbers.PhoneMetadata.prototype.setEmergency = function(a) {
  this.set$Value(27, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasEmergency = function() {
  return this.has$Value(27);
};
i18n.phonenumbers.PhoneMetadata.prototype.emergencyCount = function() {
  return this.count$Values(27);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearEmergency = function() {
  this.clear$Field(27);
};
i18n.phonenumbers.PhoneMetadata.prototype.getVoicemail = function() {
  return this.get$Value(28);
};
i18n.phonenumbers.PhoneMetadata.prototype.getVoicemailOrDefault = function() {
  return this.get$ValueOrDefault(28);
};
i18n.phonenumbers.PhoneMetadata.prototype.setVoicemail = function(a) {
  this.set$Value(28, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasVoicemail = function() {
  return this.has$Value(28);
};
i18n.phonenumbers.PhoneMetadata.prototype.voicemailCount = function() {
  return this.count$Values(28);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearVoicemail = function() {
  this.clear$Field(28);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNoInternationalDialling = function() {
  return this.get$Value(24);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNoInternationalDiallingOrDefault = function() {
  return this.get$ValueOrDefault(24);
};
i18n.phonenumbers.PhoneMetadata.prototype.setNoInternationalDialling = function(a) {
  this.set$Value(24, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasNoInternationalDialling = function() {
  return this.has$Value(24);
};
i18n.phonenumbers.PhoneMetadata.prototype.noInternationalDiallingCount = function() {
  return this.count$Values(24);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearNoInternationalDialling = function() {
  this.clear$Field(24);
};
i18n.phonenumbers.PhoneMetadata.prototype.getId = function() {
  return this.get$Value(9);
};
i18n.phonenumbers.PhoneMetadata.prototype.getIdOrDefault = function() {
  return this.get$ValueOrDefault(9);
};
i18n.phonenumbers.PhoneMetadata.prototype.setId = function(a) {
  this.set$Value(9, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasId = function() {
  return this.has$Value(9);
};
i18n.phonenumbers.PhoneMetadata.prototype.idCount = function() {
  return this.count$Values(9);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearId = function() {
  this.clear$Field(9);
};
i18n.phonenumbers.PhoneMetadata.prototype.getCountryCode = function() {
  return this.get$Value(10);
};
i18n.phonenumbers.PhoneMetadata.prototype.getCountryCodeOrDefault = function() {
  return this.get$ValueOrDefault(10);
};
i18n.phonenumbers.PhoneMetadata.prototype.setCountryCode = function(a) {
  this.set$Value(10, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasCountryCode = function() {
  return this.has$Value(10);
};
i18n.phonenumbers.PhoneMetadata.prototype.countryCodeCount = function() {
  return this.count$Values(10);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearCountryCode = function() {
  this.clear$Field(10);
};
i18n.phonenumbers.PhoneMetadata.prototype.getInternationalPrefix = function() {
  return this.get$Value(11);
};
i18n.phonenumbers.PhoneMetadata.prototype.getInternationalPrefixOrDefault = function() {
  return this.get$ValueOrDefault(11);
};
i18n.phonenumbers.PhoneMetadata.prototype.setInternationalPrefix = function(a) {
  this.set$Value(11, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasInternationalPrefix = function() {
  return this.has$Value(11);
};
i18n.phonenumbers.PhoneMetadata.prototype.internationalPrefixCount = function() {
  return this.count$Values(11);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearInternationalPrefix = function() {
  this.clear$Field(11);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPreferredInternationalPrefix = function() {
  return this.get$Value(17);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPreferredInternationalPrefixOrDefault = function() {
  return this.get$ValueOrDefault(17);
};
i18n.phonenumbers.PhoneMetadata.prototype.setPreferredInternationalPrefix = function(a) {
  this.set$Value(17, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasPreferredInternationalPrefix = function() {
  return this.has$Value(17);
};
i18n.phonenumbers.PhoneMetadata.prototype.preferredInternationalPrefixCount = function() {
  return this.count$Values(17);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearPreferredInternationalPrefix = function() {
  this.clear$Field(17);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNationalPrefix = function() {
  return this.get$Value(12);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNationalPrefixOrDefault = function() {
  return this.get$ValueOrDefault(12);
};
i18n.phonenumbers.PhoneMetadata.prototype.setNationalPrefix = function(a) {
  this.set$Value(12, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasNationalPrefix = function() {
  return this.has$Value(12);
};
i18n.phonenumbers.PhoneMetadata.prototype.nationalPrefixCount = function() {
  return this.count$Values(12);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearNationalPrefix = function() {
  this.clear$Field(12);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPreferredExtnPrefix = function() {
  return this.get$Value(13);
};
i18n.phonenumbers.PhoneMetadata.prototype.getPreferredExtnPrefixOrDefault = function() {
  return this.get$ValueOrDefault(13);
};
i18n.phonenumbers.PhoneMetadata.prototype.setPreferredExtnPrefix = function(a) {
  this.set$Value(13, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasPreferredExtnPrefix = function() {
  return this.has$Value(13);
};
i18n.phonenumbers.PhoneMetadata.prototype.preferredExtnPrefixCount = function() {
  return this.count$Values(13);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearPreferredExtnPrefix = function() {
  this.clear$Field(13);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNationalPrefixForParsing = function() {
  return this.get$Value(15);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNationalPrefixForParsingOrDefault = function() {
  return this.get$ValueOrDefault(15);
};
i18n.phonenumbers.PhoneMetadata.prototype.setNationalPrefixForParsing = function(a) {
  this.set$Value(15, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasNationalPrefixForParsing = function() {
  return this.has$Value(15);
};
i18n.phonenumbers.PhoneMetadata.prototype.nationalPrefixForParsingCount = function() {
  return this.count$Values(15);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearNationalPrefixForParsing = function() {
  this.clear$Field(15);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNationalPrefixTransformRule = function() {
  return this.get$Value(16);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNationalPrefixTransformRuleOrDefault = function() {
  return this.get$ValueOrDefault(16);
};
i18n.phonenumbers.PhoneMetadata.prototype.setNationalPrefixTransformRule = function(a) {
  this.set$Value(16, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasNationalPrefixTransformRule = function() {
  return this.has$Value(16);
};
i18n.phonenumbers.PhoneMetadata.prototype.nationalPrefixTransformRuleCount = function() {
  return this.count$Values(16);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearNationalPrefixTransformRule = function() {
  this.clear$Field(16);
};
i18n.phonenumbers.PhoneMetadata.prototype.getSameMobileAndFixedLinePattern = function() {
  return this.get$Value(18);
};
i18n.phonenumbers.PhoneMetadata.prototype.getSameMobileAndFixedLinePatternOrDefault = function() {
  return this.get$ValueOrDefault(18);
};
i18n.phonenumbers.PhoneMetadata.prototype.setSameMobileAndFixedLinePattern = function(a) {
  this.set$Value(18, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasSameMobileAndFixedLinePattern = function() {
  return this.has$Value(18);
};
i18n.phonenumbers.PhoneMetadata.prototype.sameMobileAndFixedLinePatternCount = function() {
  return this.count$Values(18);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearSameMobileAndFixedLinePattern = function() {
  this.clear$Field(18);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNumberFormat = function(a) {
  return this.get$Value(19, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.getNumberFormatOrDefault = function(a) {
  return this.get$ValueOrDefault(19, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.addNumberFormat = function(a) {
  this.add$Value(19, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.numberFormatArray = function() {
  return this.array$Values(19);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasNumberFormat = function() {
  return this.has$Value(19);
};
i18n.phonenumbers.PhoneMetadata.prototype.numberFormatCount = function() {
  return this.count$Values(19);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearNumberFormat = function() {
  this.clear$Field(19);
};
i18n.phonenumbers.PhoneMetadata.prototype.getIntlNumberFormat = function(a) {
  return this.get$Value(20, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.getIntlNumberFormatOrDefault = function(a) {
  return this.get$ValueOrDefault(20, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.addIntlNumberFormat = function(a) {
  this.add$Value(20, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.intlNumberFormatArray = function() {
  return this.array$Values(20);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasIntlNumberFormat = function() {
  return this.has$Value(20);
};
i18n.phonenumbers.PhoneMetadata.prototype.intlNumberFormatCount = function() {
  return this.count$Values(20);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearIntlNumberFormat = function() {
  this.clear$Field(20);
};
i18n.phonenumbers.PhoneMetadata.prototype.getMainCountryForCode = function() {
  return this.get$Value(22);
};
i18n.phonenumbers.PhoneMetadata.prototype.getMainCountryForCodeOrDefault = function() {
  return this.get$ValueOrDefault(22);
};
i18n.phonenumbers.PhoneMetadata.prototype.setMainCountryForCode = function(a) {
  this.set$Value(22, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasMainCountryForCode = function() {
  return this.has$Value(22);
};
i18n.phonenumbers.PhoneMetadata.prototype.mainCountryForCodeCount = function() {
  return this.count$Values(22);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearMainCountryForCode = function() {
  this.clear$Field(22);
};
i18n.phonenumbers.PhoneMetadata.prototype.getLeadingDigits = function() {
  return this.get$Value(23);
};
i18n.phonenumbers.PhoneMetadata.prototype.getLeadingDigitsOrDefault = function() {
  return this.get$ValueOrDefault(23);
};
i18n.phonenumbers.PhoneMetadata.prototype.setLeadingDigits = function(a) {
  this.set$Value(23, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasLeadingDigits = function() {
  return this.has$Value(23);
};
i18n.phonenumbers.PhoneMetadata.prototype.leadingDigitsCount = function() {
  return this.count$Values(23);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearLeadingDigits = function() {
  this.clear$Field(23);
};
i18n.phonenumbers.PhoneMetadata.prototype.getLeadingZeroPossible = function() {
  return this.get$Value(26);
};
i18n.phonenumbers.PhoneMetadata.prototype.getLeadingZeroPossibleOrDefault = function() {
  return this.get$ValueOrDefault(26);
};
i18n.phonenumbers.PhoneMetadata.prototype.setLeadingZeroPossible = function(a) {
  this.set$Value(26, a);
};
i18n.phonenumbers.PhoneMetadata.prototype.hasLeadingZeroPossible = function() {
  return this.has$Value(26);
};
i18n.phonenumbers.PhoneMetadata.prototype.leadingZeroPossibleCount = function() {
  return this.count$Values(26);
};
i18n.phonenumbers.PhoneMetadata.prototype.clearLeadingZeroPossible = function() {
  this.clear$Field(26);
};
i18n.phonenumbers.PhoneMetadataCollection = function() {
  goog.proto2.Message.call(this);
};
goog.inherits(i18n.phonenumbers.PhoneMetadataCollection, goog.proto2.Message);
i18n.phonenumbers.PhoneMetadataCollection.descriptor_ = null;
i18n.phonenumbers.PhoneMetadataCollection.prototype.getMetadata = function(a) {
  return this.get$Value(1, a);
};
i18n.phonenumbers.PhoneMetadataCollection.prototype.getMetadataOrDefault = function(a) {
  return this.get$ValueOrDefault(1, a);
};
i18n.phonenumbers.PhoneMetadataCollection.prototype.addMetadata = function(a) {
  this.add$Value(1, a);
};
i18n.phonenumbers.PhoneMetadataCollection.prototype.metadataArray = function() {
  return this.array$Values(1);
};
i18n.phonenumbers.PhoneMetadataCollection.prototype.hasMetadata = function() {
  return this.has$Value(1);
};
i18n.phonenumbers.PhoneMetadataCollection.prototype.metadataCount = function() {
  return this.count$Values(1);
};
i18n.phonenumbers.PhoneMetadataCollection.prototype.clearMetadata = function() {
  this.clear$Field(1);
};
i18n.phonenumbers.NumberFormat.prototype.getDescriptor = function() {
  var a = i18n.phonenumbers.NumberFormat.descriptor_;
  a || (i18n.phonenumbers.NumberFormat.descriptor_ = a = goog.proto2.Message.createDescriptor(i18n.phonenumbers.NumberFormat, {0:{name:"NumberFormat", fullName:"i18n.phonenumbers.NumberFormat"}, 1:{name:"pattern", required:!0, fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 2:{name:"format", required:!0, fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 3:{name:"leading_digits_pattern", repeated:!0, fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 4:{name:"national_prefix_formatting_rule", 
  fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 6:{name:"national_prefix_optional_when_formatting", fieldType:goog.proto2.Message.FieldType.BOOL, defaultValue:!1, type:Boolean}, 5:{name:"domestic_carrier_code_formatting_rule", fieldType:goog.proto2.Message.FieldType.STRING, type:String}}));
  return a;
};
i18n.phonenumbers.NumberFormat.getDescriptor = i18n.phonenumbers.NumberFormat.prototype.getDescriptor;
i18n.phonenumbers.PhoneNumberDesc.prototype.getDescriptor = function() {
  var a = i18n.phonenumbers.PhoneNumberDesc.descriptor_;
  a || (i18n.phonenumbers.PhoneNumberDesc.descriptor_ = a = goog.proto2.Message.createDescriptor(i18n.phonenumbers.PhoneNumberDesc, {0:{name:"PhoneNumberDesc", fullName:"i18n.phonenumbers.PhoneNumberDesc"}, 2:{name:"national_number_pattern", fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 9:{name:"possible_length", repeated:!0, fieldType:goog.proto2.Message.FieldType.INT32, type:Number}, 10:{name:"possible_length_local_only", repeated:!0, fieldType:goog.proto2.Message.FieldType.INT32, 
  type:Number}, 6:{name:"example_number", fieldType:goog.proto2.Message.FieldType.STRING, type:String}}));
  return a;
};
i18n.phonenumbers.PhoneNumberDesc.getDescriptor = i18n.phonenumbers.PhoneNumberDesc.prototype.getDescriptor;
i18n.phonenumbers.PhoneMetadata.prototype.getDescriptor = function() {
  var a = i18n.phonenumbers.PhoneMetadata.descriptor_;
  a || (i18n.phonenumbers.PhoneMetadata.descriptor_ = a = goog.proto2.Message.createDescriptor(i18n.phonenumbers.PhoneMetadata, {0:{name:"PhoneMetadata", fullName:"i18n.phonenumbers.PhoneMetadata"}, 1:{name:"general_desc", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 2:{name:"fixed_line", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 3:{name:"mobile", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 
  4:{name:"toll_free", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 5:{name:"premium_rate", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 6:{name:"shared_cost", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 7:{name:"personal_number", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 8:{name:"voip", fieldType:goog.proto2.Message.FieldType.MESSAGE, 
  type:i18n.phonenumbers.PhoneNumberDesc}, 21:{name:"pager", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 25:{name:"uan", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 27:{name:"emergency", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 28:{name:"voicemail", fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 24:{name:"no_international_dialling", 
  fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneNumberDesc}, 9:{name:"id", required:!0, fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 10:{name:"country_code", fieldType:goog.proto2.Message.FieldType.INT32, type:Number}, 11:{name:"international_prefix", fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 17:{name:"preferred_international_prefix", fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 12:{name:"national_prefix", fieldType:goog.proto2.Message.FieldType.STRING, 
  type:String}, 13:{name:"preferred_extn_prefix", fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 15:{name:"national_prefix_for_parsing", fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 16:{name:"national_prefix_transform_rule", fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 18:{name:"same_mobile_and_fixed_line_pattern", fieldType:goog.proto2.Message.FieldType.BOOL, defaultValue:!1, type:Boolean}, 19:{name:"number_format", repeated:!0, fieldType:goog.proto2.Message.FieldType.MESSAGE, 
  type:i18n.phonenumbers.NumberFormat}, 20:{name:"intl_number_format", repeated:!0, fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.NumberFormat}, 22:{name:"main_country_for_code", fieldType:goog.proto2.Message.FieldType.BOOL, defaultValue:!1, type:Boolean}, 23:{name:"leading_digits", fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 26:{name:"leading_zero_possible", fieldType:goog.proto2.Message.FieldType.BOOL, defaultValue:!1, type:Boolean}}));
  return a;
};
i18n.phonenumbers.PhoneMetadata.getDescriptor = i18n.phonenumbers.PhoneMetadata.prototype.getDescriptor;
i18n.phonenumbers.PhoneMetadataCollection.prototype.getDescriptor = function() {
  var a = i18n.phonenumbers.PhoneMetadataCollection.descriptor_;
  a || (i18n.phonenumbers.PhoneMetadataCollection.descriptor_ = a = goog.proto2.Message.createDescriptor(i18n.phonenumbers.PhoneMetadataCollection, {0:{name:"PhoneMetadataCollection", fullName:"i18n.phonenumbers.PhoneMetadataCollection"}, 1:{name:"metadata", repeated:!0, fieldType:goog.proto2.Message.FieldType.MESSAGE, type:i18n.phonenumbers.PhoneMetadata}}));
  return a;
};
i18n.phonenumbers.PhoneMetadataCollection.getDescriptor = i18n.phonenumbers.PhoneMetadataCollection.prototype.getDescriptor;
i18n.phonenumbers.PhoneNumber = function() {
  goog.proto2.Message.call(this);
};
goog.inherits(i18n.phonenumbers.PhoneNumber, goog.proto2.Message);
i18n.phonenumbers.PhoneNumber.descriptor_ = null;
i18n.phonenumbers.PhoneNumber.prototype.getCountryCode = function() {
  return this.get$Value(1);
};
i18n.phonenumbers.PhoneNumber.prototype.getCountryCodeOrDefault = function() {
  return this.get$ValueOrDefault(1);
};
i18n.phonenumbers.PhoneNumber.prototype.setCountryCode = function(a) {
  this.set$Value(1, a);
};
i18n.phonenumbers.PhoneNumber.prototype.hasCountryCode = function() {
  return this.has$Value(1);
};
i18n.phonenumbers.PhoneNumber.prototype.countryCodeCount = function() {
  return this.count$Values(1);
};
i18n.phonenumbers.PhoneNumber.prototype.clearCountryCode = function() {
  this.clear$Field(1);
};
i18n.phonenumbers.PhoneNumber.prototype.getNationalNumber = function() {
  return this.get$Value(2);
};
i18n.phonenumbers.PhoneNumber.prototype.getNationalNumberOrDefault = function() {
  return this.get$ValueOrDefault(2);
};
i18n.phonenumbers.PhoneNumber.prototype.setNationalNumber = function(a) {
  this.set$Value(2, a);
};
i18n.phonenumbers.PhoneNumber.prototype.hasNationalNumber = function() {
  return this.has$Value(2);
};
i18n.phonenumbers.PhoneNumber.prototype.nationalNumberCount = function() {
  return this.count$Values(2);
};
i18n.phonenumbers.PhoneNumber.prototype.clearNationalNumber = function() {
  this.clear$Field(2);
};
i18n.phonenumbers.PhoneNumber.prototype.getExtension = function() {
  return this.get$Value(3);
};
i18n.phonenumbers.PhoneNumber.prototype.getExtensionOrDefault = function() {
  return this.get$ValueOrDefault(3);
};
i18n.phonenumbers.PhoneNumber.prototype.setExtension = function(a) {
  this.set$Value(3, a);
};
i18n.phonenumbers.PhoneNumber.prototype.hasExtension = function() {
  return this.has$Value(3);
};
i18n.phonenumbers.PhoneNumber.prototype.extensionCount = function() {
  return this.count$Values(3);
};
i18n.phonenumbers.PhoneNumber.prototype.clearExtension = function() {
  this.clear$Field(3);
};
i18n.phonenumbers.PhoneNumber.prototype.getItalianLeadingZero = function() {
  return this.get$Value(4);
};
i18n.phonenumbers.PhoneNumber.prototype.getItalianLeadingZeroOrDefault = function() {
  return this.get$ValueOrDefault(4);
};
i18n.phonenumbers.PhoneNumber.prototype.setItalianLeadingZero = function(a) {
  this.set$Value(4, a);
};
i18n.phonenumbers.PhoneNumber.prototype.hasItalianLeadingZero = function() {
  return this.has$Value(4);
};
i18n.phonenumbers.PhoneNumber.prototype.italianLeadingZeroCount = function() {
  return this.count$Values(4);
};
i18n.phonenumbers.PhoneNumber.prototype.clearItalianLeadingZero = function() {
  this.clear$Field(4);
};
i18n.phonenumbers.PhoneNumber.prototype.getNumberOfLeadingZeros = function() {
  return this.get$Value(8);
};
i18n.phonenumbers.PhoneNumber.prototype.getNumberOfLeadingZerosOrDefault = function() {
  return this.get$ValueOrDefault(8);
};
i18n.phonenumbers.PhoneNumber.prototype.setNumberOfLeadingZeros = function(a) {
  this.set$Value(8, a);
};
i18n.phonenumbers.PhoneNumber.prototype.hasNumberOfLeadingZeros = function() {
  return this.has$Value(8);
};
i18n.phonenumbers.PhoneNumber.prototype.numberOfLeadingZerosCount = function() {
  return this.count$Values(8);
};
i18n.phonenumbers.PhoneNumber.prototype.clearNumberOfLeadingZeros = function() {
  this.clear$Field(8);
};
i18n.phonenumbers.PhoneNumber.prototype.getRawInput = function() {
  return this.get$Value(5);
};
i18n.phonenumbers.PhoneNumber.prototype.getRawInputOrDefault = function() {
  return this.get$ValueOrDefault(5);
};
i18n.phonenumbers.PhoneNumber.prototype.setRawInput = function(a) {
  this.set$Value(5, a);
};
i18n.phonenumbers.PhoneNumber.prototype.hasRawInput = function() {
  return this.has$Value(5);
};
i18n.phonenumbers.PhoneNumber.prototype.rawInputCount = function() {
  return this.count$Values(5);
};
i18n.phonenumbers.PhoneNumber.prototype.clearRawInput = function() {
  this.clear$Field(5);
};
i18n.phonenumbers.PhoneNumber.prototype.getCountryCodeSource = function() {
  return this.get$Value(6);
};
i18n.phonenumbers.PhoneNumber.prototype.getCountryCodeSourceOrDefault = function() {
  return this.get$ValueOrDefault(6);
};
i18n.phonenumbers.PhoneNumber.prototype.setCountryCodeSource = function(a) {
  this.set$Value(6, a);
};
i18n.phonenumbers.PhoneNumber.prototype.hasCountryCodeSource = function() {
  return this.has$Value(6);
};
i18n.phonenumbers.PhoneNumber.prototype.countryCodeSourceCount = function() {
  return this.count$Values(6);
};
i18n.phonenumbers.PhoneNumber.prototype.clearCountryCodeSource = function() {
  this.clear$Field(6);
};
i18n.phonenumbers.PhoneNumber.prototype.getPreferredDomesticCarrierCode = function() {
  return this.get$Value(7);
};
i18n.phonenumbers.PhoneNumber.prototype.getPreferredDomesticCarrierCodeOrDefault = function() {
  return this.get$ValueOrDefault(7);
};
i18n.phonenumbers.PhoneNumber.prototype.setPreferredDomesticCarrierCode = function(a) {
  this.set$Value(7, a);
};
i18n.phonenumbers.PhoneNumber.prototype.hasPreferredDomesticCarrierCode = function() {
  return this.has$Value(7);
};
i18n.phonenumbers.PhoneNumber.prototype.preferredDomesticCarrierCodeCount = function() {
  return this.count$Values(7);
};
i18n.phonenumbers.PhoneNumber.prototype.clearPreferredDomesticCarrierCode = function() {
  this.clear$Field(7);
};
i18n.phonenumbers.PhoneNumber.CountryCodeSource = {UNSPECIFIED:0, FROM_NUMBER_WITH_PLUS_SIGN:1, FROM_NUMBER_WITH_IDD:5, FROM_NUMBER_WITHOUT_PLUS_SIGN:10, FROM_DEFAULT_COUNTRY:20};
i18n.phonenumbers.PhoneNumber.prototype.getDescriptor = function() {
  var a = i18n.phonenumbers.PhoneNumber.descriptor_;
  a || (i18n.phonenumbers.PhoneNumber.descriptor_ = a = goog.proto2.Message.createDescriptor(i18n.phonenumbers.PhoneNumber, {0:{name:"PhoneNumber", fullName:"i18n.phonenumbers.PhoneNumber"}, 1:{name:"country_code", required:!0, fieldType:goog.proto2.Message.FieldType.INT32, type:Number}, 2:{name:"national_number", required:!0, fieldType:goog.proto2.Message.FieldType.UINT64, type:Number}, 3:{name:"extension", fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 4:{name:"italian_leading_zero", 
  fieldType:goog.proto2.Message.FieldType.BOOL, type:Boolean}, 8:{name:"number_of_leading_zeros", fieldType:goog.proto2.Message.FieldType.INT32, defaultValue:1, type:Number}, 5:{name:"raw_input", fieldType:goog.proto2.Message.FieldType.STRING, type:String}, 6:{name:"country_code_source", fieldType:goog.proto2.Message.FieldType.ENUM, defaultValue:i18n.phonenumbers.PhoneNumber.CountryCodeSource.UNSPECIFIED, type:i18n.phonenumbers.PhoneNumber.CountryCodeSource}, 7:{name:"preferred_domestic_carrier_code", 
  fieldType:goog.proto2.Message.FieldType.STRING, type:String}}));
  return a;
};
i18n.phonenumbers.PhoneNumber.ctor = i18n.phonenumbers.PhoneNumber;
i18n.phonenumbers.PhoneNumber.ctor.getDescriptor = i18n.phonenumbers.PhoneNumber.prototype.getDescriptor;
/*

 Copyright (C) 2010 The Libphonenumber Authors

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
*/
i18n.phonenumbers.metadata = {};
i18n.phonenumbers.metadata.countryCodeToRegionCodeMap = {1:"US AG AI AS BB BM BS CA DM DO GD GU JM KN KY LC MP MS PR SX TC TT VC VG VI".split(" "), 7:["RU", "KZ"], 20:["EG"], 27:["ZA"], 30:["GR"], 31:["NL"], 32:["BE"], 33:["FR"], 34:["ES"], 36:["HU"], 39:["IT", "VA"], 40:["RO"], 41:["CH"], 43:["AT"], 44:["GB", "GG", "IM", "JE"], 45:["DK"], 46:["SE"], 47:["NO", "SJ"], 48:["PL"], 49:["DE"], 51:["PE"], 52:["MX"], 53:["CU"], 54:["AR"], 55:["BR"], 56:["CL"], 57:["CO"], 58:["VE"], 60:["MY"], 61:["AU", 
"CC", "CX"], 62:["ID"], 63:["PH"], 64:["NZ"], 65:["SG"], 66:["TH"], 81:["JP"], 82:["KR"], 84:["VN"], 86:["CN"], 90:["TR"], 91:["IN"], 92:["PK"], 93:["AF"], 94:["LK"], 95:["MM"], 98:["IR"], 211:["SS"], 212:["MA", "EH"], 213:["DZ"], 216:["TN"], 218:["LY"], 220:["GM"], 221:["SN"], 222:["MR"], 223:["ML"], 224:["GN"], 225:["CI"], 226:["BF"], 227:["NE"], 228:["TG"], 229:["BJ"], 230:["MU"], 231:["LR"], 232:["SL"], 233:["GH"], 234:["NG"], 235:["TD"], 236:["CF"], 237:["CM"], 238:["CV"], 239:["ST"], 240:["GQ"], 
241:["GA"], 242:["CG"], 243:["CD"], 244:["AO"], 245:["GW"], 246:["IO"], 247:["AC"], 248:["SC"], 249:["SD"], 250:["RW"], 251:["ET"], 252:["SO"], 253:["DJ"], 254:["KE"], 255:["TZ"], 256:["UG"], 257:["BI"], 258:["MZ"], 260:["ZM"], 261:["MG"], 262:["RE", "YT"], 263:["ZW"], 264:["NA"], 265:["MW"], 266:["LS"], 267:["BW"], 268:["SZ"], 269:["KM"], 290:["SH", "TA"], 291:["ER"], 297:["AW"], 298:["FO"], 299:["GL"], 350:["GI"], 351:["PT"], 352:["LU"], 353:["IE"], 354:["IS"], 355:["AL"], 356:["MT"], 357:["CY"], 
358:["FI", "AX"], 359:["BG"], 370:["LT"], 371:["LV"], 372:["EE"], 373:["MD"], 374:["AM"], 375:["BY"], 376:["AD"], 377:["MC"], 378:["SM"], 380:["UA"], 381:["RS"], 382:["ME"], 385:["HR"], 386:["SI"], 387:["BA"], 389:["MK"], 420:["CZ"], 421:["SK"], 423:["LI"], 500:["FK"], 501:["BZ"], 502:["GT"], 503:["SV"], 504:["HN"], 505:["NI"], 506:["CR"], 507:["PA"], 508:["PM"], 509:["HT"], 590:["GP", "BL", "MF"], 591:["BO"], 592:["GY"], 593:["EC"], 594:["GF"], 595:["PY"], 596:["MQ"], 597:["SR"], 598:["UY"], 599:["CW", 
"BQ"], 670:["TL"], 672:["NF"], 673:["BN"], 674:["NR"], 675:["PG"], 676:["TO"], 677:["SB"], 678:["VU"], 679:["FJ"], 680:["PW"], 681:["WF"], 682:["CK"], 683:["NU"], 685:["WS"], 686:["KI"], 687:["NC"], 688:["TV"], 689:["PF"], 690:["TK"], 691:["FM"], 692:["MH"], 800:["001"], 808:["001"], 850:["KP"], 852:["HK"], 853:["MO"], 855:["KH"], 856:["LA"], 870:["001"], 878:["001"], 880:["BD"], 881:["001"], 882:["001"], 883:["001"], 886:["TW"], 888:["001"], 960:["MV"], 961:["LB"], 962:["JO"], 963:["SY"], 964:["IQ"], 
965:["KW"], 966:["SA"], 967:["YE"], 968:["OM"], 970:["PS"], 971:["AE"], 972:["IL"], 973:["BH"], 974:["QA"], 975:["BT"], 976:["MN"], 977:["NP"], 979:["001"], 992:["TJ"], 993:["TM"], 994:["AZ"], 995:["GE"], 996:["KG"], 998:["UZ"]};
i18n.phonenumbers.metadata.countryToMetadata = {AC:[, [, , "[46]\\d{4}|[01589]\\d{5}", , , , , , , [5, 6]], [, , "6[2-467]\\d{3}", , , , "62889", , , [5]], [, , "4\\d{4}", , , , "40123", , , [5]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "AC", 247, "00", , , , , , , , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "[01589]\\d{5}", , , , "542011", , , [6]], , , [, , , , , , , , , [-1]]], AD:[, 
[, , "[16]\\d{5,8}|[37-9]\\d{5}", , , , , , , [6, 8, 9]], [, , "[78]\\d{5}", , , , "712345", , , [6]], [, , "(?:3\\d|6(?:[0-8]|90\\d{2}))\\d{4}", , , , "312345", , , [6, 9]], [, , "180[02]\\d{4}", , , , "18001234", , , [8]], [, , "[19]\\d{5}", , , , "912345", , , [6]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "AD", 376, "00", , , , , , , , [[, "(\\d{3})(\\d{3})", "$1 $2", ["[137-9]|6[0-8]"]], [, "(\\d{4})(\\d{4})", "$1 $2", ["180", "180[02]"]], [, "(\\d{3})(\\d{3})(\\d{3})", 
"$1 $2 $3", ["690"]]], , [, , , , , , , , , [-1]], , , [, , "1800\\d{4}", , , , "18000000", , , [8]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], AE:[, [, , "[2-79]\\d{7,8}|800\\d{2,9}", , , , , , , [5, 6, 7, 8, 9, 10, 11, 12]], [, , "[2-4679][2-8]\\d{6}", , , , "22345678", , , [8], [7]], [, , "5[024-68]\\d{7}", , , , "501234567", , , [9]], [, , "400\\d{6}|800\\d{2,9}", , , , "800123456"], [, , "900[02]\\d{5}", , , , "900234567", , , [9]], [, , "700[05]\\d{5}", , , , "700012345", , , 
[9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "AE", 971, "00", "0", , , "0", , , , [[, "([2-4679])(\\d{3})(\\d{4})", "$1 $2 $3", ["[2-4679][2-8]"], "0$1"], [, "(5\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["5"], "0$1"], [, "([479]00)(\\d)(\\d{5})", "$1 $2 $3", ["[479]00"], "$1"], [, "([68]00)(\\d{2,9})", "$1 $2", ["[68]00"], "$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "600[25]\\d{5}", , , , "600212345", , , [9]], , , [, , , , , , , , , [-1]]], AF:[, [, , "[2-7]\\d{8}", 
, , , , , , [9], [7]], [, , "(?:[25][0-8]|[34][0-4]|6[0-5])[2-9]\\d{6}", , , , "234567890", , , , [7]], [, , "7(?:[014-9]\\d|2[89]|30)\\d{6}", , , , "701234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "AF", 93, "00", "0", , , "0", , , , [[, "([2-7]\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["[2-7]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , 
[-1]]], AG:[, [, , "[2589]\\d{9}", , , , , , , [10], [7]], [, , "268(?:4(?:6[0-38]|84)|56[0-2])\\d{4}", , , , "2684601234", , , , [7]], [, , "268(?:464|7(?:1[3-9]|2\\d|3[246]|64|7[0-689]|8[02-68]))\\d{4}", , , , "2684641234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , "26848[01]\\d{4}", , , , "2684801234", , , , [7]], "AG", 
1, "011", "1", , , "1", , , , , , [, , "26840[69]\\d{4}", , , , "2684061234", , , , [7]], , "268", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], AI:[, [, , "[2589]\\d{9}", , , , , , , [10], [7]], [, , "2644(?:6[12]|9[78])\\d{4}", , , , "2644612345", , , , [7]], [, , "264(?:235|476|5(?:3[6-9]|8[1-4])|7(?:29|72))\\d{4}", , , , "2642351234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, 
, , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "AI", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "264", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], AL:[, [, , "[2-57]\\d{7}|6\\d{8}|8\\d{5,7}|9\\d{5}", , , , , , , [6, 7, 8, 9], [5]], [, , "(?:[2358](?:[16-9]\\d[2-9]|[2-5][2-9]\\d)|4(?:[2-57-9][2-9]\\d|6\\d{2}))\\d{4}", , , , "22345678", , , [8], [5, 6, 7]], [, , "6(?:[689][2-9]|7[2-6])\\d{6}", 
, , , "662123456", , , [9]], [, , "800\\d{4}", , , , "8001234", , , [7]], [, , "900[1-9]\\d{2}", , , , "900123", , , [6]], [, , "808[1-9]\\d{2}", , , , "808123", , , [6]], [, , "700[2-9]\\d{4}", , , , "70021234", , , [8]], [, , , , , , , , , [-1]], "AL", 355, "00", "0", , , "0", , , , [[, "(4)(\\d{3})(\\d{4})", "$1 $2 $3", ["4[0-6]"], "0$1"], [, "(6\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["6"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["[2358][2-5]|4[7-9]"], "0$1"], [, "(\\d{3})(\\d{3,5})", 
"$1 $2", ["[235][16-9]|[79]|8[016-9]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], AM:[, [, , "[1-9]\\d{7}", , , , , , , [8], [5, 6]], [, , "(?:1[0-2]\\d|2(?:2[2-46]|3[1-8]|4[2-69]|5[2-7]|6[1-9]|8[1-7])|3[12]2|47\\d)\\d{5}", , , , "10123456", , , , [5, 6]], [, , "(?:4[1349]|55|77|88|9[1-9])\\d{6}", , , , "77123456"], [, , "800\\d{5}", , , , "80012345"], [, , "90[016]\\d{5}", , , , "90012345"], [, , "80[1-4]\\d{5}", , 
, , "80112345"], [, , , , , , , , , [-1]], [, , "60(?:2[078]|[3-7]\\d|8[0-5])\\d{4}", , , , "60271234"], "AM", 374, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{6})", "$1 $2", ["1|47"], "(0$1)"], [, "(\\d{2})(\\d{6})", "$1 $2", ["4[1349]|[5-7]|88|9[1-9]"], "0$1"], [, "(\\d{3})(\\d{5})", "$1 $2", ["[23]"], "(0$1)"], [, "(\\d{3})(\\d{2})(\\d{3})", "$1 $2 $3", ["8|90"], "0 $1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], AO:[, [, 
, "[29]\\d{8}", , , , , , , [9]], [, , "2\\d(?:[26-9]\\d|\\d[26-9])\\d{5}", , , , "222123456"], [, , "9[1-49]\\d{7}", , , , "923123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "AO", 244, "00", , , , , , , , [[, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], AR:[, [, , "11\\d{8}|[2368]\\d{9}|9\\d{10}", 
, , , , , , [10, 11], [6, 7, 8]], [, , "11\\d{8}|(?:2(?:2(?:[013]\\d|2[13-79]|4[1-6]|5[2457]|6[124-8]|7[1-4]|8[13-6]|9[1267])|3(?:1[467]|2[03-6]|3[13-8]|[49][2-6]|5[2-8]|[067]\\d)|4(?:7[3-8]|9\\d)|6(?:[01346]\\d|2[24-6]|5[15-8])|80\\d|9(?:[0124789]\\d|3[1-6]|5[234]|6[2-46]))|3(?:3(?:2[79]|6\\d|8[2578])|4(?:[78]\\d|0[0124-9]|[1-35]\\d|4[24-7]|6[02-9]|9[123678])|5(?:[138]\\d|2[1245]|4[1-9]|6[2-4]|7[1-6])|6[24]\\d|7(?:[0469]\\d|1[1568]|2[013-9]|3[145]|5[14-8]|7[2-57]|8[0-24-9])|8(?:[013578]\\d|2[15-7]|4[13-6]|6[1-357-9]|9[124]))|670\\d)\\d{6}", 
, , , "1123456789", , , [10], [6, 7, 8]], [, , "675\\d{7}|9(?:11[2-9]\\d{7}|(?:2(?:2[013]|3[067]|49|6[01346]|80|9[147-9])|3(?:36|4[12358]|5[138]|6[24]|7[069]|8[013578]))[2-9]\\d{6}|\\d{4}[2-9]\\d{5})", , , , "91123456789", , , , [6, 7, 8]], [, , "800\\d{7}", , , , "8001234567", , , [10]], [, , "60[04579]\\d{7}", , , , "6001234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "AR", 54, "00", "0", , , "0?(?:(11|2(?:2(?:02?|[13]|2[13-79]|4[1-6]|5[2457]|6[124-8]|7[1-4]|8[13-6]|9[1267])|3(?:02?|1[467]|2[03-6]|3[13-8]|[49][2-6]|5[2-8]|[67])|4(?:7[3-578]|9)|6(?:[0136]|2[24-6]|4[6-8]?|5[15-8])|80|9(?:0[1-3]|[19]|2\\d|3[1-6]|4[02568]?|5[2-4]|6[2-46]|72?|8[23]?))|3(?:3(?:2[79]|6|8[2578])|4(?:0[0-24-9]|[12]|3[5-8]?|4[24-7]|5[4-68]?|6[02-9]|7[126]|8[2379]?|9[1-36-8])|5(?:1|2[1245]|3[237]?|4[1-46-9]|6[2-4]|7[1-6]|8[2-5]?)|6[24]|7(?:[069]|1[1568]|2[15]|3[145]|4[13]|5[14-8]|7[2-57]|8[126])|8(?:[01]|2[15-7]|3[2578]?|4[13-6]|5[4-8]?|6[1-357-9]|7[36-8]?|8[5-8]?|9[124])))?15)?", 
"9$1", , , [[, "([68]\\d{2})(\\d{3})(\\d{4})", "$1-$2-$3", ["[68]"], "0$1"], [, "(\\d{2})(\\d{4})", "$1-$2", ["[2-9]"], "$1"], [, "(\\d{3})(\\d{4})", "$1-$2", ["[2-9]"], "$1"], [, "(\\d{4})(\\d{4})", "$1-$2", ["[2-9]"], "$1"], [, "(9)(11)(\\d{4})(\\d{4})", "$2 15-$3-$4", ["911"], "0$1"], [, "(9)(\\d{3})(\\d{3})(\\d{4})", "$2 15-$3-$4", ["9(?:2[2-4689]|3[3-8])", "9(?:2(?:2[013]|3[067]|49|6[01346]|8|9[147-9])|3(?:36|4[1-358]|5[138]|6|7[069]|8[013578]))", "9(?:2(?:2(?:0[013-9]|[13])|3(?:0[013-9]|[67])|49|6(?:[0136]|4[0-59])|8|9(?:[19]|44|7[013-9]|8[14]))|3(?:36|4(?:[12]|3[4-6]|[58]4)|5(?:1|3[0-24-689]|8[46])|6|7[069]|8(?:[01]|34|[578][45])))", 
"9(?:2(?:2(?:0[013-9]|[13])|3(?:0[013-9]|[67])|49|6(?:[0136]|4[0-59])|8|9(?:[19]|44|7[013-9]|8[14]))|3(?:36|4(?:[12]|3(?:4|5[014]|6[1-39])|[58]4)|5(?:1|3[0-24-689]|8[46])|6|7[069]|8(?:[01]|34|[578][45])))"], "0$1"], [, "(9)(\\d{4})(\\d{2})(\\d{4})", "$2 15-$3-$4", ["9[23]"], "0$1"], [, "(11)(\\d{4})(\\d{4})", "$1 $2-$3", ["11"], "0$1", , 1], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2-$3", ["2(?:2[013]|3[067]|49|6[01346]|8|9[147-9])|3(?:36|4[1-358]|5[138]|6|7[069]|8[013578])", "2(?:2(?:0[013-9]|[13])|3(?:0[013-9]|[67])|49|6(?:[0136]|4[0-59])|8|9(?:[19]|44|7[013-9]|8[14]))|3(?:36|4(?:[12]|3[4-6]|[58]4)|5(?:1|3[0-24-689]|8[46])|6|7[069]|8(?:[01]|34|[578][45]))", 
"2(?:2(?:0[013-9]|[13])|3(?:0[013-9]|[67])|49|6(?:[0136]|4[0-59])|8|9(?:[19]|44|7[013-9]|8[14]))|3(?:36|4(?:[12]|3(?:4|5[014]|6[1-39])|[58]4)|5(?:1|3[0-24-689]|8[46])|6|7[069]|8(?:[01]|34|[578][45]))"], "0$1", , 1], [, "(\\d{4})(\\d{2})(\\d{4})", "$1 $2-$3", ["[23]"], "0$1", , 1], [, "(\\d{3})", "$1", ["1[0-2]|911"], "$1"]], [[, "([68]\\d{2})(\\d{3})(\\d{4})", "$1-$2-$3", ["[68]"], "0$1"], [, "(9)(11)(\\d{4})(\\d{4})", "$1 $2 $3-$4", ["911"]], [, "(9)(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3-$4", ["9(?:2[2-4689]|3[3-8])", 
"9(?:2(?:2[013]|3[067]|49|6[01346]|8|9[147-9])|3(?:36|4[1-358]|5[138]|6|7[069]|8[013578]))", "9(?:2(?:2(?:0[013-9]|[13])|3(?:0[013-9]|[67])|49|6(?:[0136]|4[0-59])|8|9(?:[19]|44|7[013-9]|8[14]))|3(?:36|4(?:[12]|3[4-6]|[58]4)|5(?:1|3[0-24-689]|8[46])|6|7[069]|8(?:[01]|34|[578][45])))", "9(?:2(?:2(?:0[013-9]|[13])|3(?:0[013-9]|[67])|49|6(?:[0136]|4[0-59])|8|9(?:[19]|44|7[013-9]|8[14]))|3(?:36|4(?:[12]|3(?:4|5[014]|6[1-39])|[58]4)|5(?:1|3[0-24-689]|8[46])|6|7[069]|8(?:[01]|34|[578][45])))"]], [, "(9)(\\d{4})(\\d{2})(\\d{4})", 
"$1 $2 $3-$4", ["9[23]"]], [, "(11)(\\d{4})(\\d{4})", "$1 $2-$3", ["11"], "0$1", , 1], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2-$3", ["2(?:2[013]|3[067]|49|6[01346]|8|9[147-9])|3(?:36|4[1-358]|5[138]|6|7[069]|8[013578])", "2(?:2(?:0[013-9]|[13])|3(?:0[013-9]|[67])|49|6(?:[0136]|4[0-59])|8|9(?:[19]|44|7[013-9]|8[14]))|3(?:36|4(?:[12]|3[4-6]|[58]4)|5(?:1|3[0-24-689]|8[46])|6|7[069]|8(?:[01]|34|[578][45]))", "2(?:2(?:0[013-9]|[13])|3(?:0[013-9]|[67])|49|6(?:[0136]|4[0-59])|8|9(?:[19]|44|7[013-9]|8[14]))|3(?:36|4(?:[12]|3(?:4|5[014]|6[1-39])|[58]4)|5(?:1|3[0-24-689]|8[46])|6|7[069]|8(?:[01]|34|[578][45]))"], 
"0$1", , 1], [, "(\\d{4})(\\d{2})(\\d{4})", "$1 $2-$3", ["[23]"], "0$1", , 1]], [, , , , , , , , , [-1]], , , [, , "810\\d{7}", , , , "8101234567", , , [10]], [, , "810\\d{7}", , , , "8101234567", , , [10]], , , [, , , , , , , , , [-1]]], AS:[, [, , "[5689]\\d{9}", , , , , , , [10], [7]], [, , "6846(?:22|33|44|55|77|88|9[19])\\d{4}", , , , "6846221234", , , , [7]], [, , "684(?:2(?:5[2468]|72)|7(?:3[13]|70))\\d{4}", , , , "6847331234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , 
"8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "AS", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "684", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], AT:[, [, , "[1-9]\\d{3,12}", , , , , , , [4, 5, 6, 7, 8, 9, 10, 11, 12, 13], [3]], [, , "1\\d{3,12}|(?:2(?:1[467]|2[13-8]|5[2357]|6[1-46-8]|7[1-8]|8[124-7]|9[1458])|3(?:1[1-8]|3[23568]|4[5-7]|5[1378]|6[1-38]|8[3-68])|4(?:2[1-8]|35|63|7[1368]|8[2457])|5(?:12|2[1-8]|3[357]|4[147]|5[12578]|6[37])|6(?:13|2[1-47]|4[1-35-8]|5[468]|62)|7(?:2[1-8]|3[25]|4[13478]|5[68]|6[16-8]|7[1-6]|9[45]))\\d{3,10}", 
, , , "1234567890", , , , [3]], [, , "6(?:5[0-3579]|6[013-9]|[7-9]\\d)\\d{4,10}", , , , "664123456", , , [7, 8, 9, 10, 11, 12, 13]], [, , "800\\d{6,10}", , , , "800123456", , , [9, 10, 11, 12, 13]], [, , "9(?:0[01]|3[019])\\d{6,10}", , , , "900123456", , , [9, 10, 11, 12, 13]], [, , "8(?:10\\d|2(?:[01]\\d|8\\d?))\\d{5,9}", , , , "810123456", , , [8, 9, 10, 11, 12, 13]], [, , , , , , , , , [-1]], [, , "780\\d{6,10}", , , , "780123456", , , [9, 10, 11, 12, 13]], "AT", 43, "00", "0", , , "0", , , , 
[[, "(116\\d{3})", "$1", ["116"], "$1"], [, "(1)(\\d{3,12})", "$1 $2", ["1"], "0$1"], [, "(5\\d)(\\d{3,5})", "$1 $2", ["5[079]"], "0$1"], [, "(5\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["5[079]"], "0$1"], [, "(5\\d)(\\d{4})(\\d{4,7})", "$1 $2 $3", ["5[079]"], "0$1"], [, "(\\d{3})(\\d{3,10})", "$1 $2", ["(?:31|4)6|51|6(?:5[0-3579]|[6-9])|7(?:[28]0|32)|[89]"], "0$1"], [, "(\\d{4})(\\d{3,9})", "$1 $2", ["2|3(?:1[1-578]|[3-8])|4[2378]|5[2-6]|6(?:[12]|4[1-9]|5[468])|7(?:[24][1-8]|35|[5-79])"], "0$1"]], , 
[, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "5(?:(?:0[1-9]|17)\\d{2,10}|[79]\\d{3,11})|720\\d{6,10}", , , , "50123", , , [5, 6, 7, 8, 9, 10, 11, 12, 13]], , , [, , , , , , , , , [-1]]], AU:[, [, , "1\\d{4,9}|[2-578]\\d{8}", , , , , , , [5, 6, 7, 8, 9, 10]], [, , "[237]\\d{8}|8(?:51(?:0(?:0[03-9]|[1247]\\d|3[2-9]|5[0-8]|6[1-9]|8[0-6])|1(?:1[69]|[23]\\d|4[0-4]))|[6-8]\\d{4}|9(?:[02-9]\\d{3}|1(?:[0-57-9]\\d{2}|6[0135-9]\\d)))\\d{3}", , , , "212345678", , , [9], [8]], [, , "14(?:5\\d|71)\\d{5}|4(?:[0-3]\\d|4[047-9]|5[0-25-9]|6[6-9]|7[02-9]|8[12457-9]|9[017-9])\\d{6}", 
, , , "412345678", , , [9]], [, , "180(?:0\\d{3}|2)\\d{3}", , , , "1800123456", , , [7, 10]], [, , "19(?:0[0126]\\d|[679])\\d{5}", , , , "1900123456", , , [8, 10]], [, , "13(?:00\\d{3}|45[0-4]|\\d)\\d{3}", , , , "1300123456", , , [6, 8, 10]], [, , "500\\d{6}", , , , "500123456", , , [9]], [, , "550\\d{6}", , , , "550123456", , , [9]], "AU", 61, "(?:14(?:1[14]|34|4[17]|[56]6|7[47]|88)0011)|001[14-689]", "0", , , "0", , "0011", , [[, "([2378])(\\d{4})(\\d{4})", "$1 $2 $3", ["[2378]"], "(0$1)"], [, 
"(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["14|[45]"], "0$1"], [, "(16)(\\d{3,4})", "$1 $2", ["16"], "0$1"], [, "(16)(\\d{3})(\\d{2,4})", "$1 $2 $3", ["16"], "0$1"], [, "(1[389]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["1[389]0", "1(?:[38]0|9)0"]], [, "(180)(2\\d{3})", "$1 $2", ["180", "1802"]], [, "(19\\d)(\\d{3})", "$1 $2", ["19[13]"]], [, "(19\\d{2})(\\d{4})", "$1 $2", ["19[679]"]], [, "(13)(\\d{2})(\\d{2})", "$1 $2 $3", ["13[1-9]"]]], , [, , "16\\d{3,7}", , , , "1612345", , , [5, 6, 7, 8, 9]], 1, 
, [, , "1(?:3(?:00\\d{3}|45[0-4]|\\d)\\d{3}|80(?:0\\d{6}|2\\d{3}))", , , , "1300123456", , , [6, 7, 8, 10]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], AW:[, [, , "[25-9]\\d{6}", , , , , , , [7]], [, , "5(?:2\\d|8[1-9])\\d{4}", , , , "5212345"], [, , "(?:290|5[69]\\d|6(?:[03]0|22|4[0-2]|[69]\\d)|7(?:[34]\\d|7[07])|9(?:6[45]|9[4-8]))\\d{4}", , , , "5601234"], [, , "800\\d{4}", , , , "8001234"], [, , "900\\d{4}", , , , "9001234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, 
, "28\\d{5}|501\\d{4}", , , , "5011234"], "AW", 297, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], AX:[, [, , "[15]\\d{6,9}|2\\d{4,9}|3\\d{5,9}|4\\d{7,10}|[67]\\d{7,9}|8\\d{7,8}", , , , , , , [5, 6, 7, 8, 9, 10, 11]], [, , "18[1-8]\\d{4,6}", , , , "181234567", , , [7, 8, 9]], [, , "4(?:[0-8]\\d{6,8}|9\\d{9})|50\\d{6,8}", , , , "412345678", , , [8, 9, 10, 11]], [, , "800\\d{5,6}", 
, , , "800123456", , , [8, 9]], [, , "[67]00\\d{5,6}", , , , "600123456", , , [8, 9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "AX", 358, "00|99(?:[02469]|5(?:11|33|5[59]|88|9[09]))", "0", , , "0", , "00", , , , [, , , , , , , , , [-1]], , , [, , "100\\d{4,6}|20(?:0\\d{4,6}|2[023]\\d{4,5}|9[89]\\d{1,6})|300\\d{3,7}|60(?:[12]\\d{5,6}|6\\d{7})|7(?:1\\d{7}|3\\d{8}|5[03-9]\\d{5,6})", , , , "1001234", , , [5, 6, 7, 8, 9, 10]], [, , "10(?:0\\d{4,6}|[1-46-9]\\d{5,7}|5\\d{4,7})|2(?:0(?:0\\d{4,6}|[1346-8]\\d{5,7}|2(?:[023]\\d{4,5}|[14-9]\\d{4,6})|5(?:\\d{3}|\\d{5,7})|9(?:[0-7]\\d{4,6}|[89]\\d{1,6}))|9\\d{5,8})|3(?:0(?:0\\d{3,7}|[1-57-9]\\d{5,7}|6(?:\\d{3}|\\d{5,7}))|44\\d{3}|93\\d{5,7})|60(?:[12]\\d{5,6}|6\\d{7})|7(?:1\\d{7}|3\\d{8}|5[03-9]\\d{5,6})", 
, , , "10112345", , , [5, 6, 7, 8, 9, 10]], , , [, , , , , , , , , [-1]]], AZ:[, [, , "[1-9]\\d{8}", , , , , , , [9], [7]], [, , "(?:1[28]\\d{3}|2(?:02|1[24]|2[2-4]|33|[45]2|6[23])\\d{2}|365(?:[0-46-9]\\d|5[0-35-9]))\\d{4}", , , , "123123456", , , , [7]], [, , "(?:36554|(?:4[04]|5[015]|60|7[07])\\d{3})\\d{4}", , , , "401234567"], [, , "88\\d{7}", , , , "881234567"], [, , "900200\\d{3}", , , , "900200123"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "AZ", 994, "00", 
"0", , , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["1[28]|2(?:[0-36]|[45]2)|365"], "(0$1)"], [, "(\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[4-8]"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["9"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BA:[, [, , "[3-9]\\d{7,8}", , , , , , , [8, 9], [6]], [, , "(?:[35]\\d|49)\\d{6}", , , , "30123456", , , [8], [6]], [, , "6(?:0(?:3\\d|40)|[1-356]\\d|44[0-6]|71[137])\\d{5}", 
, , , "61123456"], [, , "8[08]\\d{6}", , , , "80123456", , , [8]], [, , "9[0246]\\d{6}", , , , "90123456", , , [8]], [, , "8[12]\\d{6}", , , , "82123456", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BA", 387, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{3})", "$1 $2-$3", ["[3-5]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["6[1-356]|[7-9]"], "0$1"], [, "(\\d{2})(\\d{2})(\\d{2})(\\d{3})", "$1 $2 $3 $4", ["6[047]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , 
, , , , , , [-1]], [, , "70[23]\\d{5}", , , , "70223456", , , [8]], , , [, , , , , , , , , [-1]]], BB:[, [, , "[2589]\\d{9}", , , , , , , [10], [7]], [, , "246(?:2(?:2[78]|7[0-4])|4(?:1[024-6]|2\\d|3[2-9])|5(?:20|[34]\\d|54|7[1-3])|6(?:2\\d|38)|7(?:37|57)|9(?:1[89]|63))\\d{4}", , , , "2464123456", , , , [7]], [, , "246(?:2(?:[356]\\d|4[0-57-9]|8[0-79])|45\\d|69[5-7]|8(?:[2-5]\\d|83))\\d{4}", , , , "2462501234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900\\d{7}|246976\\d{4}", 
, , , "9002123456", , , , [7]], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , "24631\\d{5}", , , , "2463101234", , , , [7]], "BB", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "246", [, , , , , , , , , [-1]], [, , "246(?:292|367|4(?:1[7-9]|3[01]|44|67)|736)\\d{4}", , , , "2464301234", , , , [7]], , , [, , , , , , , , , [-1]]], BD:[, [, , "[2-79]\\d{5,9}|1\\d{9}|8[0-7]\\d{4,8}", , , , , , , [6, 7, 8, 9, 10]], [, , "2(?:[45]\\d{3}|7(?:1[0-267]|2[0-289]|3[0-29]|4[01]|5[1-3]|6[013]|7[0178]|91)|8(?:0[125]|[139][1-6]|2[0157-9]|41|6[1-35]|7[1-5]|8[1-8]|90)|9(?:0[0-2]|1[0-4]|2[568]|3[3-6]|5[5-7]|6[0167]|7[15]|8[0146-9]))\\d{4}|3(?:12?[5-7]\\d{2}|0(?:2(?:[025-79]\\d|[348]\\d{1,2})|3(?:[2-4]\\d|[56]\\d?))|2(?:1\\d{2}|2(?:[12]\\d|[35]\\d{1,2}|4\\d?))|3(?:1\\d{2}|2(?:[2356]\\d|4\\d{1,2}))|4(?:1\\d{2}|2(?:2\\d{1,2}|[47]|5\\d{2}))|5(?:1\\d{2}|29)|[67]1\\d{2}|8(?:1\\d{2}|2(?:2\\d{2}|3|4\\d)))\\d{3}|4(?:0(?:2(?:[09]\\d|7)|33\\d{2})|1\\d{3}|2(?:1\\d{2}|2(?:[25]\\d?|[348]\\d|[67]\\d{1,2}))|3(?:1\\d{2}(?:\\d{2})?|2(?:[045]\\d|[236-9]\\d{1,2})|32\\d{2})|4(?:[18]\\d{2}|2(?:[2-46]\\d{2}|3)|5[25]\\d{2})|5(?:1\\d{2}|2(?:3\\d|5))|6(?:[18]\\d{2}|2(?:3(?:\\d{2})?|[46]\\d{1,2}|5\\d{2}|7\\d)|5(?:3\\d?|4\\d|[57]\\d{1,2}|6\\d{2}|8))|71\\d{2}|8(?:[18]\\d{2}|23\\d{2}|54\\d{2})|9(?:[18]\\d{2}|2[2-5]\\d{2}|53\\d{1,2}))\\d{3}|5(?:02[03489]\\d{2}|1\\d{2}|2(?:1\\d{2}|2(?:2(?:\\d{2})?|[457]\\d{2}))|3(?:1\\d{2}|2(?:[37](?:\\d{2})?|[569]\\d{2}))|4(?:1\\d{2}|2[46]\\d{2})|5(?:1\\d{2}|26\\d{1,2})|6(?:[18]\\d{2}|2|53\\d{2})|7(?:1|24)\\d{2}|8(?:1|26)\\d{2}|91\\d{2})\\d{3}|6(?:0(?:1\\d{2}|2(?:3\\d{2}|4\\d{1,2}))|2(?:2[2-5]\\d{2}|5(?:[3-5]\\d{2}|7)|8\\d{2})|3(?:1|2[3478])\\d{2}|4(?:1|2[34])\\d{2}|5(?:1|2[47])\\d{2}|6(?:[18]\\d{2}|6(?:2(?:2\\d|[34]\\d{2})|5(?:[24]\\d{2}|3\\d|5\\d{1,2})))|72[2-5]\\d{2}|8(?:1\\d{2}|2[2-5]\\d{2})|9(?:1\\d{2}|2[2-6]\\d{2}))\\d{3}|7(?:(?:02|[3-589]1|6[12]|72[24])\\d{2}|21\\d{3}|32)\\d{3}|8(?:(?:4[12]|[5-7]2|1\\d?)|(?:0|3[12]|[5-7]1|217)\\d)\\d{4}|9(?:[35]1|(?:[024]2|81)\\d|(?:1|[24]1)\\d{2})\\d{3}", 
, , , "27111234", , , [6, 7, 8, 9]], [, , "(?:1[13-9]\\d|(?:3[78]|44)[02-9]|6(?:44|6[02-9]))\\d{7}", , , , "1812345678", , , [10]], [, , "80[03]\\d{7}", , , , "8001234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "96(?:0[49]|1[0-4]|6[69])\\d{6}", , , , "9604123456", , , [10]], "BD", 880, "00", "0", , , "0", , , , [[, "(2)(\\d{7,8})", "$1-$2", ["2"], "0$1"], [, "(\\d{2})(\\d{4,6})", "$1-$2", ["[3-79]1"], "0$1"], [, "(\\d{4})(\\d{3,6})", "$1-$2", 
["1|3(?:0|[2-58]2)|4(?:0|[25]2|3[23]|[4689][25])|5(?:[02-578]2|6[25])|6(?:[0347-9]2|[26][25])|7[02-9]2|8(?:[023][23]|[4-7]2)|9(?:[02][23]|[458]2|6[016])"], "0$1"], [, "(\\d{3})(\\d{3,7})", "$1-$2", ["[3-79][2-9]|8"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BE:[, [, , "[1-9]\\d{7,8}", , , , , , , [8, 9]], [, , "(?:1[0-69]|[23][2-8]|4[23]|5\\d|6[013-57-9]|71|8[1-79]|9[2-4])\\d{6}|80[2-8]\\d{5}", , , , "12345678", , 
, [8]], [, , "4(?:6[0135-8]|[79]\\d|8[3-9])\\d{6}", , , , "470123456", , , [9]], [, , "800\\d{5}", , , , "80012345", , , [8]], [, , "(?:70[2-467]|90[0-79])\\d{5}", , , , "90123456", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BE", 32, "00", "0", , , "0", , , , [[, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["4[6-9]"], "0$1"], [, "(\\d)(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[23]|4[23]|9[2-4]"], "0$1"], [, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", 
"$1 $2 $3 $4", ["[156]|7[018]|8(?:0[1-9]|[1-79])"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{3})", "$1 $2 $3", ["(?:80|9)0"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "78\\d{6}", , , , "78123456", , , [8]], , , [, , , , , , , , , [-1]]], BF:[, [, , "[25-7]\\d{7}", , , , , , , [8]], [, , "2(?:0(?:49|5[23]|6[56]|9[016-9])|4(?:4[569]|5[4-6]|6[56]|7[0179])|5(?:[34]\\d|50|6[5-7]))\\d{4}", , , , "20491234"], [, , "(?:5[15-8]|[67]\\d)\\d{6}", , , , "70123456"], [, , , , , , , , , 
[-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BF", 226, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BG:[, [, , "[23567]\\d{5,7}|[489]\\d{6,8}", , , , , , , [6, 7, 8, 9], [4, 5]], [, , "2\\d{5,7}|(?:[36]\\d|5[1-9]|8[1-6]|9[1-7])\\d{5,6}|(?:4(?:[124-7]\\d|3[1-6])|7(?:0[1-9]|[1-9]\\d))\\d{4,5}", 
, , , "2123456", , , [6, 7, 8], [4, 5]], [, , "(?:8[7-9]\\d|9(?:8\\d|9[69]))\\d{6}|4(?:3[0789]|8\\d)\\d{5}", , , , "48123456", , , [8, 9]], [, , "800\\d{5}", , , , "80012345", , , [8]], [, , "90\\d{6}", , , , "90123456", , , [8]], [, , , , , , , , , [-1]], [, , "700\\d{5}", , , , "70012345", , , [8]], [, , , , , , , , , [-1]], "BG", 359, "00", "0", , , "0", , , , [[, "(2)(\\d)(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["2"], "0$1"], [, "(2)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["2"], "0$1"], [, "(\\d{3})(\\d{4})", 
"$1 $2", ["43[124-7]|70[1-9]"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{2})", "$1 $2 $3", ["43[124-7]|70[1-9]"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{3})", "$1 $2 $3", ["[78]00"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["99[69]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{2,3})", "$1 $2 $3", ["[356]|4[124-7]|7[1-9]|8[1-6]|9[1-7]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["48|8[7-9]|9[08]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , 
, , , , , , , [-1]]], BH:[, [, , "[136-9]\\d{7}", , , , , , , [8]], [, , "(?:1(?:3[1356]|6[0156]|7\\d)\\d|6(?:1[16]\\d|500|6(?:0\\d|3[12]|44|7[7-9])|9[69][69])|7(?:1(?:11|78)|7\\d{2}))\\d{4}", , , , "17001234"], [, , "(?:3(?:[1-4679]\\d|5[013-69]|8[0-47-9])\\d|6(?:3(?:00|33|6[16])|6(?:[69]\\d|3[03-9]|7[0-6])))\\d{4}", , , , "36001234"], [, , "80\\d{6}", , , , "80123456"], [, , "(?:87|9[014578])\\d{6}", , , , "90123456"], [, , "84\\d{6}", , , , "84123456"], [, , , , , , , , , [-1]], [, , , , , , , 
, , [-1]], "BH", 973, "00", , , , , , , , [[, "(\\d{4})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BI:[, [, , "[267]\\d{7}", , , , , , , [8]], [, , "22\\d{6}", , , , "22201234"], [, , "(?:29|6[189]|7[124-9])\\d{6}", , , , "79561234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BI", 257, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", 
"$1 $2 $3 $4"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BJ:[, [, , "[2689]\\d{7}", , , , , , , [8]], [, , "2(?:02|1[037]|2[45]|3[68])\\d{5}", , , , "20211234"], [, , "(?:6[1-8]|9[03-9])\\d{6}", , , , "90011234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "857[58]\\d{4}", , , , "85751234"], "BJ", 229, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", 
"$1 $2 $3 $4", ["[2689]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "81\\d{6}", , , , "81123456"], , , [, , , , , , , , , [-1]]], BL:[, [, , "[56]\\d{8}", , , , , , , [9]], [, , "590(?:2[7-9]|5[12]|87)\\d{4}", , , , "590271234"], [, , "690(?:0[05-9]|[1-9]\\d)\\d{4}", , , , "690001234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BL", 590, "00", "0", , , "0", , , , , , [, , , , , , , , , 
[-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BM:[, [, , "[4589]\\d{9}", , , , , , , [10], [7]], [, , "441(?:2(?:02|23|61|[3479]\\d)|[46]\\d{2}|5(?:4\\d|60|89)|824)\\d{4}", , , , "4412345678", , , , [7]], [, , "441(?:[37]\\d|5[0-39])\\d{5}", , , , "4413701234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", 
, , , "5002345678"], [, , , , , , , , , [-1]], "BM", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "441", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BN:[, [, , "[2-578]\\d{6}", , , , , , , [7]], [, , "2(?:[013-9]\\d|2[0-7])\\d{4}|[3-5]\\d{6}", , , , "2345678"], [, , "22[89]\\d{4}|[78]\\d{6}", , , , "7123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BN", 
673, "00", , , , , , , , [[, "([2-578]\\d{2})(\\d{4})", "$1 $2", ["[2-578]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BO:[, [, , "[23467]\\d{7}|8\\d{8}", , , , , , , [8, 9], [7]], [, , "(?:2(?:2\\d{2}|5(?:11|[258]\\d|9[67])|6(?:12|2\\d|9[34])|8(?:2[34]|39|62))|3(?:3\\d{2}|4(?:6\\d|8[24])|8(?:25|42|5[257]|86|9[25])|9(?:2\\d|3[234]|4[248]|5[24]|6[2-6]|7\\d))|4(?:4\\d{2}|6(?:11|[24689]\\d|72)))\\d{4}", , , , "22123456", , , 
[8], [7]], [, , "[67]\\d{7}", , , , "71234567", , , [8]], [, , "80017\\d{4}", , , , "800171234", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BO", 591, "00(1\\d)?", "0", , , "0(1\\d)?", , , , [[, "([234])(\\d{7})", "$1 $2", ["[2-4]"], , "0$CC $1"], [, "([67]\\d{7})", "$1", ["[67]"], , "0$CC $1"], [, "(800)(\\d{2})(\\d{4})", "$1 $2 $3", ["800"], , "0$CC $1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , 
, , , , [-1]], , , [, , , , , , , , , [-1]]], BQ:[, [, , "[347]\\d{6}", , , , , , , [7]], [, , "(?:318[023]|41(?:6[023]|70)|7(?:1[578]|50)\\d)\\d{3}", , , , "7151234"], [, , "(?:31(?:8[14-8]|9[14578])|416[145-9]|7(?:0[01]|7[07]|8\\d|9[056])\\d)\\d{3}", , , , "3181234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BQ", 599, "00", , , , , , , , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , 
, , , , , [-1]], , , [, , , , , , , , , [-1]]], BR:[, [, , "[1-46-9]\\d{7,10}|5(?:[0-4]\\d{7,9}|5(?:[2-8]\\d{7}|9\\d{7,8}))", , , , , , , [8, 9, 10, 11]], [, , "(?:[14689][1-9]|2[12478]|3[1-578]|5[13-5]|7[13-579])[2-5]\\d{7}", , , , "1123456789", , , [10], [8]], [, , "(?:[189][1-9]|2[12478])(?:7|9\\d)\\d{7}|(?:3[1-578]|[46][1-9]|5[13-5]|7[13-579])(?:[6-8]|9\\d?)\\d{7}", , , , "11961234567", , , [10, 11], [8]], [, , "800\\d{6,7}", , , , "800123456", , , [9, 10]], [, , "(?:300|[59]00\\d?)\\d{6}", , 
, , "300123456", , , [9, 10]], [, , "(?:300\\d(?:\\d{2})?|40(?:0\\d|20))\\d{4}", , , , "40041234", , , [8, 10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BR", 55, "00(?:1[245]|2[1-35]|31|4[13]|[56]5|99)", "0", , , "0(?:(1[245]|2[1-35]|31|4[13]|[56]5|99)(\\d{10,11}))?", "$2", , , [[, "(\\d{4})(\\d{4})", "$1-$2", ["300|40[02]", "300|40(?:0|20)"]], [, "([3589]00)(\\d{2,3})(\\d{4})", "$1 $2 $3", ["[3589]00"], "0$1"], [, "(\\d{3,5})", "$1", ["1[125689]"]], [, "(\\d{4})(\\d{4})", "$1-$2", ["[2-9](?:0[1-9]|[1-9])"]], 
[, "(\\d{5})(\\d{4})", "$1-$2", ["9(?:0[1-9]|[1-9])"]], [, "(\\d{2})(\\d{4})(\\d{4})", "$1 $2-$3", ["[1-9][1-9]"], "($1)", "0 $CC ($1)"], [, "(\\d{2})(\\d{5})(\\d{4})", "$1 $2-$3", ["[1-9][1-9]9"], "($1)", "0 $CC ($1)"]], [[, "(\\d{4})(\\d{4})", "$1-$2", ["300|40[02]", "300|40(?:0|20)"]], [, "([3589]00)(\\d{2,3})(\\d{4})", "$1 $2 $3", ["[3589]00"], "0$1"], [, "(\\d{2})(\\d{4})(\\d{4})", "$1 $2-$3", ["[1-9][1-9]"], "($1)", "0 $CC ($1)"], [, "(\\d{2})(\\d{5})(\\d{4})", "$1 $2-$3", ["[1-9][1-9]9"], 
"($1)", "0 $CC ($1)"]], [, , , , , , , , , [-1]], , , [, , "(?:300\\d|40(?:0\\d|20))\\d{4}", , , , "40041234", , , [8]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BS:[, [, , "[2589]\\d{9}", , , , , , , [10], [7]], [, , "242(?:3(?:02|[236][1-9]|4[0-24-9]|5[0-68]|7[347]|8[0-4]|9[2-467])|461|502|6(?:0[1-4]|12|2[013]|[45]0|7[67]|8[78]|9[89])|7(?:02|88))\\d{4}", , , , "2423456789", , , , [7]], [, , "242(?:3(?:5[79]|7[56]|95)|4(?:[23][1-9]|4[1-35-9]|5[1-8]|6[2-8]|7\\d|81)|5(?:2[45]|3[35]|44|5[1-46-9]|65|77)|6[34]6|7(?:27|38)|8(?:0[1-9]|1[02-9]|2\\d|[89]9))\\d{4}", 
, , , "2423591234", , , , [7]], [, , "242300\\d{4}|8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456", , , , [7]], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "BS", 1, "011?", "1", , , "1", , "011", , , , [, , , , , , , , , [-1]], , "242", [, , , , , , , , , [-1]], [, , "242225[0-46-9]\\d{3}", , , , "2422250123"], , , [, , , , , , , , , [-1]]], BT:[, [, , "[1-8]\\d{6,7}", , 
, , , , , [7, 8], [6]], [, , "(?:2[3-6]|[34][5-7]|5[236]|6[2-46]|7[246]|8[2-4])\\d{5}", , , , "2345678", , , [7], [6]], [, , "(?:1[67]|77)\\d{6}", , , , "17123456", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BT", 975, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["1|77"]], [, "([2-8])(\\d{3})(\\d{3})", "$1 $2 $3", ["[2-68]|7[246]"]]], , [, , , , , , , , , [-1]], , , [, , 
, , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BW:[, [, , "[2-79]\\d{6,7}", , , , , , , [7, 8]], [, , "(?:2(?:4[0-48]|6[0-24]|9[0578])|3(?:1[0-35-9]|55|[69]\\d|7[01])|4(?:6[03]|7[1267]|9[0-5])|5(?:3[0389]|4[0489]|7[1-47]|88|9[0-49])|6(?:2[1-35]|5[149]|8[067]))\\d{4}", , , , "2401234", , , [7]], [, , "7(?:[1-6]\\d|7[014-8])\\d{5}", , , , "71123456", , , [8]], [, , , , , , , , , [-1]], [, , "90\\d{5}", , , , "9012345", , , [7]], [, , , , , , , , , [-1]], [, , , , , , 
, , , [-1]], [, , "79[12][01]\\d{4}", , , , "79101234", , , [8]], "BW", 267, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2", ["[2-6]"]], [, "(7\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["7"]], [, "(90)(\\d{5})", "$1 $2", ["90"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BY:[, [, , "[1-4]\\d{8}|8(?:0(?:0\\d{3,7}|[13]\\d{7})|(?:10|20\\d)\\d{7})|9\\d{9,10}", , , , , , , [6, 7, 8, 9, 10, 11], [5]], [, , "(?:1(?:5(?:1[1-5]|[24]\\d|6[2-4]|9[1-7])|6(?:[235]\\d|4[1-7])|7\\d{2})|2(?:1(?:[246]\\d|3[0-35-9]|5[1-9])|2(?:[235]\\d|4[0-8])|3(?:[26]\\d|3[02-79]|4[024-7]|5[03-7])))\\d{5}", 
, , , "152450911", , , [9], [5, 6, 7]], [, , "(?:2(?:5[5679]|9[1-9])|33\\d|44\\d)\\d{6}", , , , "294911911", , , [9]], [, , "8(?:0[13]|20\\d)\\d{7}|800\\d{3,7}", , , , "8011234567"], [, , "(?:810|902)\\d{7}", , , , "9021234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "249\\d{6}", , , , "249123456", , , [9]], "BY", 375, "810", "8", , , "8?0?", , "8~10", , [[, "(\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1 $2-$3-$4", ["17[0-3589]|2[4-9]|[34]", "17(?:[02358]|1[0-2]|9[0189])|2[4-9]|[34]"], 
"8 0$1"], [, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2-$3-$4", ["1(?:5[24]|6[235]|7[467])|2(?:1[246]|2[25]|3[26])", "1(?:5[24]|6(?:2|3[04-9]|5[0346-9])|7(?:[46]|7[37-9]))|2(?:1[246]|2[25]|3[26])"], "8 0$1"], [, "(\\d{4})(\\d{2})(\\d{3})", "$1 $2-$3", ["1(?:5[169]|6[3-5]|7[179])|2(?:1[35]|2[34]|3[3-5])", "1(?:5[169]|6(?:3[1-3]|4|5[125])|7(?:1[3-9]|7[0-24-6]|9[2-7]))|2(?:1[35]|2[34]|3[3-5])"], "8 0$1"], [, "([89]\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["8[01]|9"], "8 $1"], [, "(82\\d)(\\d{4})(\\d{4})", 
"$1 $2 $3", ["82"], "8 $1"], [, "(800)(\\d{3})", "$1 $2", ["800"], "8 $1"], [, "(800)(\\d{2})(\\d{2,4})", "$1 $2 $3", ["800"], "8 $1"]], , [, , , , , , , , , [-1]], , , [, , "8(?:0[13]|10|20\\d)\\d{7}|800\\d{3,7}|902\\d{7}", , , , "82012345678"], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], BZ:[, [, , "[2-8]\\d{6}|0\\d{10}", , , , , , , [7, 11]], [, , "(?:2(?:[02]\\d|36)|[3-58][02]\\d|7(?:[02]\\d|32))\\d{4}", , , , "2221234", , , [7]], [, , "6[0-35-7]\\d{5}", , , , "6221234", , , [7]], 
[, , "0800\\d{7}", , , , "08001234123", , , [11]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "BZ", 501, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1-$2", ["[2-8]"]], [, "(0)(800)(\\d{4})(\\d{3})", "$1-$2-$3-$4", ["080", "0800"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CA:[, [, , "[2-9]\\d{9}", , , , , , , [10], [7]], [, , "(?:2(?:04|[23]6|[48]9|50)|3(?:06|43|65)|4(?:03|1[68]|3[178]|50)|5(?:06|1[49]|48|79|8[17])|6(?:0[04]|13|22|39|47)|7(?:0[59]|78|8[02])|8(?:[06]7|19|25|73)|90[25])[2-9]\\d{6}", 
, , , "2042345678", , , , [7]], [, , "(?:2(?:04|[23]6|[48]9|50)|3(?:06|43|65)|4(?:03|1[68]|3[178]|50)|5(?:06|1[49]|48|79|8[17])|6(?:0[04]|13|22|39|47)|7(?:0[59]|78|8[02])|8(?:[06]7|19|25|73)|90[25])[2-9]\\d{6}", , , , "2042345678", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "CA", 1, "011", "1", , , "1", 
, , 1, , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CC:[, [, , "[1458]\\d{5,9}", , , , , , , [6, 7, 9, 10], [8]], [, , "89162\\d{4}", , , , "891621234", , , [9], [8]], [, , "14(?:5\\d|71)\\d{5}|4(?:[0-2]\\d|3[0-57-9]|4[047-9]|5[0-25-9]|6[6-9]|7[02-9]|8[12547-9]|9[017-9])\\d{6}", , , , "412345678", , , [9]], [, , "180(?:0\\d{3}|2)\\d{3}", , , , "1800123456", , , [7, 10]], [, , "190[0126]\\d{6}", , , , "1900123456", , , [10]], 
[, , "13(?:00\\d{2})?\\d{4}", , , , "1300123456", , , [6, 10]], [, , "500\\d{6}", , , , "500123456", , , [9]], [, , "550\\d{6}", , , , "550123456", , , [9]], "CC", 61, "(?:14(?:1[14]|34|4[17]|[56]6|7[47]|88))?001[14-689]", "0", , , "0", , "0011", , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CD:[, [, , "[2-6]\\d{6}|[18]\\d{6,8}|9\\d{8}", , , , , , , [7, 9]], [, , "1(?:2\\d{7}|\\d{6})|[2-6]\\d{6}", , , , "1234567"], [, , "8(?:[0-2459]\\d{2}|8)\\d{5}|9[017-9]\\d{7}", 
, , , "991234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CD", 243, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["12"], "0$1"], [, "([89]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["8[0-2459]|9"], "0$1"], [, "(\\d{2})(\\d{2})(\\d{3})", "$1 $2 $3", ["88"], "0$1"], [, "(\\d{2})(\\d{5})", "$1 $2", ["[1-6]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , 
, , , [-1]], , , [, , , , , , , , , [-1]]], CF:[, [, , "[278]\\d{7}", , , , , , , [8]], [, , "2[12]\\d{6}", , , , "21612345"], [, , "7[0257]\\d{6}", , , , "70012345"], [, , , , , , , , , [-1]], [, , "8776\\d{4}", , , , "87761234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CF", 236, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , 
, , [-1]]], CG:[, [, , "[028]\\d{8}", , , , , , , [9]], [, , "222[1-589]\\d{5}", , , , "222123456"], [, , "0[14-6]\\d{7}", , , , "061234567"], [, , , , , , , , , [-1]], [, , "80(?:0\\d{2}|11[01])\\d{4}", , , , "800123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CG", 242, "00", , , , , , , , [[, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["801"]], [, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["[02]"]], [, "(\\d)(\\d{4})(\\d{4})", "$1 $2 $3", ["800"]]], 
, [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CH:[, [, , "[2-9]\\d{8}|860\\d{9}", , , , , , , [9, 12]], [, , "(?:2[12467]|3[1-4]|4[134]|5[256]|6[12]|[7-9]1)\\d{7}", , , , "212345678", , , [9]], [, , "7[5-9]\\d{7}", , , , "781234567", , , [9]], [, , "800\\d{6}", , , , "800123456", , , [9]], [, , "90[016]\\d{6}", , , , "900123456", , , [9]], [, , "84[0248]\\d{6}", , , , "840123456", , , [9]], [, , "878\\d{6}", , , , "878123456", , 
, [9]], [, , , , , , , , , [-1]], "CH", 41, "00", "0", , , "0", , , , [[, "([2-9]\\d)(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[2-7]|[89]1"], "0$1"], [, "([89]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["8[047]|90"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4 $5", ["860"], "0$1"]], , [, , "74[0248]\\d{6}", , , , "740123456", , , [9]], , , [, , , , , , , , , [-1]], [, , "5[18]\\d{7}", , , , "581234567", , , [9]], , , [, , "860\\d{9}", , , , "860123456789", , , [12]]], CI:[, [, 
, "[02-8]\\d{7}", , , , , , , [8]], [, , "(?:2(?:0[023]|1[02357]|[23][045]|4[03-5])|3(?:0[06]|1[069]|[2-4][07]|5[09]|6[08]))\\d{5}", , , , "21234567"], [, , "(?:0[1-9]|4\\d|5[14-9]|6[015-79]|[78][4-9])\\d{6}", , , , "01234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CI", 225, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], 
[, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CK:[, [, , "[2-8]\\d{4}", , , , , , , [5]], [, , "(?:2\\d|3[13-7]|4[1-5])\\d{3}", , , , "21234"], [, , "[5-8]\\d{4}", , , , "71234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CK", 682, "00", , , , , , , , [[, "(\\d{2})(\\d{3})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 
CL:[, [, , "1230\\d{7}|[2-46-9]\\d{8,10}|5[1-3578]\\d{7}", , , , , , , [9, 10, 11], [7, 8]], [, , "2(?:1962\\d{4}|2\\d{7}|32[0-467]\\d{5})|(?:3[2-5]|[47][1-35]|5[1-3578]|6[13-57]|9[3-9])\\d{7}", , , , "221234567", , , [9], [7, 8]], [, , "2(?:1962\\d{4}|2\\d{7}|32[0-467]\\d{5})|(?:3[2-5]|[47][1-35]|5[1-3578]|6[13-57]|9[3-9])\\d{7}", , , , "961234567", , , [9], [8]], [, , "800\\d{6}|1230\\d{7}", , , , "800123456", , , [9, 11]], [, , , , , , , , , [-1]], [, , "600\\d{7,8}", , , , "6001234567", , , [10, 
11]], [, , , , , , , , , [-1]], [, , "44\\d{7}", , , , "441234567", , , [9]], "CL", 56, "(?:0|1(?:1[0-69]|2[0-57]|5[13-58]|69|7[0167]|8[018]))0", "0", , , "0|(1(?:1[0-69]|2[0-57]|5[13-58]|69|7[0167]|8[018]))", , , 1, [[, "(\\d)(\\d{4})(\\d{4})", "$1 $2 $3", ["2[23]"], "($1)", "$CC ($1)"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["[357]|4[1-35]|6[13-57]"], "($1)", "$CC ($1)"], [, "(9)(\\d{4})(\\d{4})", "$1 $2 $3", ["9"], "0$1"], [, "(44)(\\d{3})(\\d{4})", "$1 $2 $3", ["44"], "0$1"], [, "([68]00)(\\d{3})(\\d{3,4})", 
"$1 $2 $3", ["[68]00"], "$1"], [, "(600)(\\d{3})(\\d{2})(\\d{3})", "$1 $2 $3 $4", ["600"], "$1"], [, "(1230)(\\d{3})(\\d{4})", "$1 $2 $3", ["123", "1230"], "$1"], [, "(\\d{5})(\\d{4})", "$1 $2", ["219"], "($1)", "$CC ($1)"], [, "(\\d{4,5})", "$1", ["[1-9]"], "$1"]], [[, "(\\d)(\\d{4})(\\d{4})", "$1 $2 $3", ["2[23]"], "($1)", "$CC ($1)"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["[357]|4[1-35]|6[13-57]"], "($1)", "$CC ($1)"], [, "(9)(\\d{4})(\\d{4})", "$1 $2 $3", ["9"], "0$1"], [, "(44)(\\d{3})(\\d{4})", 
"$1 $2 $3", ["44"], "0$1"], [, "([68]00)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["[68]00"], "$1"], [, "(600)(\\d{3})(\\d{2})(\\d{3})", "$1 $2 $3 $4", ["600"], "$1"], [, "(1230)(\\d{3})(\\d{4})", "$1 $2 $3", ["123", "1230"], "$1"], [, "(\\d{5})(\\d{4})", "$1 $2", ["219"], "($1)", "$CC ($1)"]], [, , , , , , , , , [-1]], , , [, , "600\\d{7,8}", , , , "6001234567", , , [10, 11]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CM:[, [, , "[2368]\\d{7,8}", , , , , , , [8, 9]], [, , "2(?:22|33|4[23])\\d{6}", 
, , , "222123456", , , [9]], [, , "6[5-9]\\d{7}", , , , "671234567", , , [9]], [, , "88\\d{6}", , , , "88012345", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CM", 237, "00", , , , , , , , [[, "([26])(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4 $5", ["[26]"]], [, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[23]|88"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , 
, , , , [-1]]], CN:[, [, , "[1-7]\\d{6,11}|8[0-357-9]\\d{6,9}|9\\d{7,10}", , , , , , , [7, 8, 9, 10, 11, 12], [5, 6]], [, , "21(?:100\\d{2}|95\\d{3,4}|\\d{8,10})|(?:10|2[02-57-9]|3(?:11|7[179])|4(?:[15]1|3[1-35])|5(?:1\\d|2[37]|3[12]|51|7[13-79]|9[15])|7(?:31|5[457]|6[09]|91)|8(?:[57]1|98))(?:100\\d{2}|95\\d{3,4}|\\d{8})|(?:3(?:1[02-9]|35|49|5\\d|7[02-68]|9[1-68])|4(?:1[02-9]|2[179]|3[3-9]|5[2-9]|6[4789]|7\\d|8[23])|5(?:3[03-9]|4[36]|5[02-9]|6[1-46]|7[028]|80|9[2-46-9])|6(?:3[1-5]|6[0238]|9[12])|7(?:01|[17]\\d|2[248]|3[04-9]|4[3-6]|5[0-4689]|6[2368]|9[02-9])|8(?:078|1[236-8]|2[5-7]|3\\d|5[1-9]|7[02-9]|8[3678]|9[1-7])|9(?:0[1-3689]|1[1-79]|[379]\\d|4[13]|5[1-5]))(?:100\\d{2}|95\\d{3,4}|\\d{7})|80(?:29|6[03578]|7[018]|81)\\d{4}", 
, , , "1012345678", , , , [5, 6]], [, , "1(?:[38]\\d{3}|4[57]\\d{2}|5[0-35-9]\\d{2}|66\\d{2}|7(?:[0-35-8]\\d{2}|40[0-5])|99\\d{2})\\d{6}", , , , "13123456789", , , [11]], [, , "(?:10)?800\\d{7}", , , , "8001234567", , , [10, 12]], [, , "16[08]\\d{5}", , , , "16812345", , , [8]], [, , "400\\d{7}|950\\d{7,8}|(?:10|2[0-57-9]|3(?:[157]\\d|35|49|9[1-68])|4(?:[17]\\d|2[179]|[35][1-9]|6[4789]|8[23])|5(?:[1357]\\d|2[37]|4[36]|6[1-46]|80|9[1-9])|6(?:3[1-5]|6[0238]|9[12])|7(?:01|[1579]\\d|2[248]|3[014-9]|4[3-6]|6[023689])|8(?:1[236-8]|2[5-7]|[37]\\d|5[14-9]|8[3678]|9[1-8])|9(?:0[1-3689]|1[1-79]|[379]\\d|4[13]|5[1-5]))96\\d{3,4}", 
, , , "4001234567", , , [7, 8, 9, 10, 11], [5, 6]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CN", 86, "(1(?:[129]\\d{3}|79\\d{2}))?00", "0", , , "(1(?:[129]\\d{3}|79\\d{2}))|0", , "00", , [[, "(80\\d{2})(\\d{4})", "$1 $2", ["80[26-8]"], "0$1", "$CC $1", 1], [, "([48]00)(\\d{3})(\\d{4})", "$1 $2 $3", ["[48]00"]], [, "(\\d{5,6})", "$1", ["100|95"]], [, "(\\d{2})(\\d{5,6})", "$1 $2", ["(?:10|2\\d)[19]", "(?:10|2\\d)(?:10|9[56])", "(?:10|2\\d)(?:100|9[56])"], "0$1", "$CC $1"], [, "(\\d{3})(\\d{5,6})", 
"$1 $2", ["[3-9]", "[3-9]\\d\\d[19]", "[3-9]\\d\\d(?:10|9[56])"], "0$1", "$CC $1"], [, "(\\d{3,4})(\\d{4})", "$1 $2", ["[2-9]"]], [, "(21)(\\d{4})(\\d{4,6})", "$1 $2 $3", ["21"], "0$1", "$CC $1", 1], [, "([12]\\d)(\\d{4})(\\d{4})", "$1 $2 $3", ["10[1-9]|2[02-9]", "10[1-9]|2[02-9]", "10(?:[1-79]|8(?:0[1-9]|[1-9]))|2[02-9]"], "0$1", "$CC $1", 1], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["3(?:1[02-9]|35|49|5|7[02-68]|9[1-68])|4(?:1[02-9]|2[179]|[35][2-9]|6[47-9]|7|8[23])|5(?:3[03-9]|4[36]|5[02-9]|6[1-46]|7[028]|80|9[2-46-9])|6(?:3[1-5]|6[0238]|9[12])|7(?:01|[1579]|2[248]|3[04-9]|4[3-6]|6[2368])|8(?:1[236-8]|2[5-7]|3|5[1-9]|7[02-9]|8[36-8]|9[1-7])|9(?:0[1-3689]|1[1-79]|[379]|4[13]|5[1-5])"], 
"0$1", "$CC $1", 1], [, "(\\d{3})(\\d{4})(\\d{4})", "$1 $2 $3", ["3(?:11|7[179])|4(?:[15]1|3[1-35])|5(?:1|2[37]|3[12]|51|7[13-79]|9[15])|7(?:[39]1|5[457]|6[09])|8(?:[57]1|98)"], "0$1", "$CC $1", 1], [, "(\\d{4})(\\d{3})(\\d{4})", "$1 $2 $3", ["807", "8078"], "0$1", "$CC $1", 1], [, "(\\d{3})(\\d{4})(\\d{4})", "$1 $2 $3", ["1(?:[3-57-9]|66)"], , "$CC $1"], [, "(10800)(\\d{3})(\\d{4})", "$1 $2 $3", ["108", "1080", "10800"]], [, "(\\d{3})(\\d{7,8})", "$1 $2", ["950"]]], [[, "(80\\d{2})(\\d{4})", "$1 $2", 
["80[26-8]"], "0$1", "$CC $1", 1], [, "([48]00)(\\d{3})(\\d{4})", "$1 $2 $3", ["[48]00"]], [, "(\\d{2})(\\d{5,6})", "$1 $2", ["(?:10|2\\d)[19]", "(?:10|2\\d)(?:10|9[56])", "(?:10|2\\d)(?:100|9[56])"], "0$1", "$CC $1"], [, "(\\d{3})(\\d{5,6})", "$1 $2", ["[3-9]", "[3-9]\\d\\d[19]", "[3-9]\\d\\d(?:10|9[56])"], "0$1", "$CC $1"], [, "(21)(\\d{4})(\\d{4,6})", "$1 $2 $3", ["21"], "0$1", "$CC $1", 1], [, "([12]\\d)(\\d{4})(\\d{4})", "$1 $2 $3", ["10[1-9]|2[02-9]", "10[1-9]|2[02-9]", "10(?:[1-79]|8(?:0[1-9]|[1-9]))|2[02-9]"], 
"0$1", "$CC $1", 1], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["3(?:1[02-9]|35|49|5|7[02-68]|9[1-68])|4(?:1[02-9]|2[179]|[35][2-9]|6[47-9]|7|8[23])|5(?:3[03-9]|4[36]|5[02-9]|6[1-46]|7[028]|80|9[2-46-9])|6(?:3[1-5]|6[0238]|9[12])|7(?:01|[1579]|2[248]|3[04-9]|4[3-6]|6[2368])|8(?:1[236-8]|2[5-7]|3|5[1-9]|7[02-9]|8[36-8]|9[1-7])|9(?:0[1-3689]|1[1-79]|[379]|4[13]|5[1-5])"], "0$1", "$CC $1", 1], [, "(\\d{3})(\\d{4})(\\d{4})", "$1 $2 $3", ["3(?:11|7[179])|4(?:[15]1|3[1-35])|5(?:1|2[37]|3[12]|51|7[13-79]|9[15])|7(?:[39]1|5[457]|6[09])|8(?:[57]1|98)"], 
"0$1", "$CC $1", 1], [, "(\\d{4})(\\d{3})(\\d{4})", "$1 $2 $3", ["807", "8078"], "0$1", "$CC $1", 1], [, "(\\d{3})(\\d{4})(\\d{4})", "$1 $2 $3", ["1(?:[3-57-9]|66)"], , "$CC $1"], [, "(10800)(\\d{3})(\\d{4})", "$1 $2 $3", ["108", "1080", "10800"]], [, "(\\d{3})(\\d{7,8})", "$1 $2", ["950"]]], [, , , , , , , , , [-1]], , , [, , "(?:4|(?:10)?8)00\\d{7}|950\\d{7,8}", , , , "4001234567", , , [10, 11, 12]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CO:[, [, , "(?:[13]\\d{0,3}|[24-8])\\d{7}", 
, , , , , , [8, 10, 11], [7]], [, , "[124-8][2-9]\\d{6}", , , , "12345678", , , [8], [7]], [, , "3(?:0[0-5]|1\\d|2[0-3]|5[01])\\d{7}", , , , "3211234567", , , [10]], [, , "1800\\d{7}", , , , "18001234567", , , [11]], [, , "19(?:0[01]|4[78])\\d{7}", , , , "19001234567", , , [11]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CO", 57, "00(?:4(?:[14]4|56)|[579])", "0", , , "0([3579]|4(?:44|56))?", , , , [[, "(\\d)(\\d{7})", "$1 $2", ["1(?:[2-7]|8[2-9]|9[0-3])|[24-8]", 
"1(?:[2-7]|8[2-9]|9(?:09|[1-3]))|[24-8]"], "($1)", "0$CC $1"], [, "(\\d{3})(\\d{7})", "$1 $2", ["3"], , "0$CC $1"], [, "(1)(\\d{3})(\\d{7})", "$1-$2-$3", ["1(?:80|9[04])", "1(?:800|9(?:0[01]|4[78]))"], "0$1"]], [[, "(\\d)(\\d{7})", "$1 $2", ["1(?:[2-7]|8[2-9]|9[0-3])|[24-8]", "1(?:[2-7]|8[2-9]|9(?:09|[1-3]))|[24-8]"], "($1)", "0$CC $1"], [, "(\\d{3})(\\d{7})", "$1 $2", ["3"], , "0$CC $1"], [, "(1)(\\d{3})(\\d{7})", "$1 $2 $3", ["1(?:80|9[04])", "1(?:800|9(?:0[01]|4[78]))"]]], [, , , , , , , , , [-1]], 
, , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CR:[, [, , "[24-9]\\d{7,9}", , , , , , , [8, 10]], [, , "2(?:[024-7]\\d{2}|1(?:0[7-9]|[1-9]\\d))\\d{4}", , , , "22123456", , , [8]], [, , "5(?:0[01]|7[0-3])\\d{5}|6(?:[0-4]\\d{3}|500[01])\\d{3}|(?:7[0-3]|8[3-9])\\d{6}", , , , "83123456", , , [8]], [, , "800\\d{7}", , , , "8001234567", , , [10]], [, , "90[059]\\d{7}", , , , "9001234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "210[0-6]\\d{4}|4\\d{7}|5100\\d{4}", 
, , , "40001234", , , [8]], "CR", 506, "00", , , , "(19(?:0[012468]|1[09]|20|66|77|99))", , , , [[, "(\\d{4})(\\d{4})", "$1 $2", ["[24-7]|8[3-9]"], , "$CC $1"], [, "(\\d{3})(\\d{3})(\\d{4})", "$1-$2-$3", ["[89]0"], , "$CC $1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CU:[, [, , "[2-57]\\d{5,7}", , , , , , , [6, 7, 8], [4, 5]], [, , "2[1-4]\\d{5,6}|3(?:1\\d{6}|[23]\\d{4,6})|4(?:[125]\\d{5,6}|[36]\\d{6}|[78]\\d{4,6})|7\\d{6,7}", 
, , , "71234567", , , , [4, 5]], [, , "5\\d{7}", , , , "51234567", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CU", 53, "119", "0", , , "0", , , , [[, "(\\d)(\\d{6,7})", "$1 $2", ["7"], "(0$1)"], [, "(\\d{2})(\\d{4,6})", "$1 $2", ["[2-4]"], "(0$1)"], [, "(\\d)(\\d{7})", "$1 $2", ["5"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 
CV:[, [, , "[259]\\d{6}", , , , , , , [7]], [, , "2(?:2[1-7]|3[0-8]|4[12]|5[1256]|6\\d|7[1-3]|8[1-5])\\d{4}", , , , "2211234"], [, , "(?:9\\d|59)\\d{5}", , , , "9911234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CV", 238, "0", , , , , , , , [[, "(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CW:[, [, 
, "[134679]\\d{6,7}", , , , , , , [7, 8]], [, , "9(?:[48]\\d{2}|50\\d|7(?:2[0-24]|[34]\\d|6[35-7]|77|8[7-9]))\\d{4}", , , , "94151234", , , [8]], [, , "9(?:5(?:[12467]\\d|3[01])|6(?:[15-9]\\d|3[01]))\\d{4}", , , , "95181234", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "60[0-2]\\d{4}", , , , "6001234", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "CW", 599, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2", ["[13-7]"]], [, "(9)(\\d{3})(\\d{4})", "$1 $2 $3", 
["9"]]], , [, , "955\\d{5}", , , , "95581234", , , [8]], 1, , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CX:[, [, , "[1458]\\d{5,9}", , , , , , , [6, 7, 8, 9, 10]], [, , "89164\\d{4}", , , , "891641234", , , [9], [8]], [, , "14(?:5\\d|71)\\d{5}|4(?:[0-2]\\d|3[0-57-9]|4[047-9]|5[0-25-9]|6[6-9]|7[02-9]|8[12547-9]|9[017-9])\\d{6}", , , , "412345678", , , [9]], [, , "180(?:0\\d{3}|2)\\d{3}", , , , "1800123456", , , [7, 10]], [, , "190[0126]\\d{6}", , , , "1900123456", 
, , [10]], [, , "13(?:00\\d{2})?\\d{4}", , , , "1300123456", , , [6, 8, 10]], [, , "500\\d{6}", , , , "500123456", , , [9]], [, , "550\\d{6}", , , , "550123456", , , [9]], "CX", 61, "(?:14(?:1[14]|34|4[17]|[56]6|7[47]|88))?001[14-689]", "0", , , "0", , "0011", , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], CY:[, [, , "[257-9]\\d{7}", , , , , , , [8]], [, , "2[2-6]\\d{6}", , , , "22345678"], [, , "9[4-79]\\d{6}", , , , "96123456"], 
[, , "800\\d{5}", , , , "80001234"], [, , "90[09]\\d{5}", , , , "90012345"], [, , "80[1-9]\\d{5}", , , , "80112345"], [, , "700\\d{5}", , , , "70012345"], [, , , , , , , , , [-1]], "CY", 357, "00", , , , , , , , [[, "(\\d{2})(\\d{6})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "(?:50|77)\\d{6}", , , , "77123456"], , , [, , , , , , , , , [-1]]], CZ:[, [, , "[2-8]\\d{8}|9\\d{8,11}", , , , , , , [9, 10, 11, 12]], [, , "2\\d{8}|(?:3[1257-9]|4[16-9]|5[13-9])\\d{7}", , , 
, "212345678", , , [9]], [, , "(?:60[1-8]|7(?:0[2-5]|[2379]\\d))\\d{6}", , , , "601123456", , , [9]], [, , "800\\d{6}", , , , "800123456", , , [9]], [, , "9(?:0[05689]|76)\\d{6}", , , , "900123456", , , [9]], [, , "8[134]\\d{7}", , , , "811234567", , , [9]], [, , "70[01]\\d{6}", , , , "700123456", , , [9]], [, , "9[17]0\\d{6}", , , , "910123456", , , [9]], "CZ", 420, "00", , , , , , , , [[, "([2-9]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["[2-8]|9[015-7]"]], [, "(96\\d)(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3 $4", 
["96"]], [, "(9\\d)(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3 $4", ["9[36]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "9(?:5\\d|7[234])\\d{6}", , , , "972123456", , , [9]], , , [, , "9(?:3\\d{9}|6\\d{7,10})", , , , "93123456789"]], DE:[, [, , "[1-35-9]\\d{3,14}|4(?:[0-8]\\d{3,12}|9(?:[0-37]\\d|4(?:[1-35-8]|4\\d?)|5\\d{1,2}|6[1-8]\\d?)\\d{2,8})", , , , , , , [4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], [3]], [, , "2\\d{5,13}|3(?:0\\d{3,13}|2\\d{9}|[3-9]\\d{4,13})|4(?:0\\d{3,12}|[1-8]\\d{4,12}|9(?:[0-37]\\d|4(?:[1-35-8]|4\\d?)|5\\d{1,2}|6[1-8]\\d?)\\d{2,8})|5(?:0[2-8]|[1256]\\d|[38][0-8]|4\\d{0,2}|[79][0-7])\\d{3,11}|6(?:\\d{5,13}|9\\d{3,12})|7(?:0[2-8]|[1-9]\\d)\\d{3,10}|8(?:0[2-9]|[1-8]\\d|9\\d?)\\d{3,10}|9(?:0[6-9]\\d{3,10}|1\\d{4,12}|[2-9]\\d{4,11})", 
, , , "30123456", , , [5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], [3, 4]], [, , "1(?:5[0-25-9]\\d{8}|6[023]\\d{7,8}|7\\d{8,9})", , , , "15123456789", , , [10, 11]], [, , "800\\d{7,12}", , , , "8001234567890", , , [10, 11, 12, 13, 14, 15]], [, , "137[7-9]\\d{6}|900(?:[135]\\d{6}|9\\d{7})", , , , "9001234567", , , [10, 11]], [, , "1(?:3(?:7[1-6]\\d{6}|8\\d{4})|80\\d{5,11})", , , , "18012345", , , [7, 8, 9, 10, 11, 12, 13, 14]], [, , "700\\d{8}", , , , "70012345678", , , [11]], [, , , , , , , , , [-1]], 
"DE", 49, "00", "0", , , "0", , , , [[, "(1\\d{2})(\\d{7,8})", "$1 $2", ["1[67]"], "0$1"], [, "(15\\d{3})(\\d{6})", "$1 $2", ["15[0568]"], "0$1"], [, "(1\\d{3})(\\d{7})", "$1 $2", ["15"], "0$1"], [, "(\\d{2})(\\d{3,11})", "$1 $2", ["3[02]|40|[68]9"], "0$1"], [, "(\\d{3})(\\d{3,11})", "$1 $2", ["2(?:0[1-389]|1[124]|2[18]|3[14]|[4-9]1)|3(?:[35-9][15]|4[015])|[4-8][1-9]1|9(?:06|[1-9]1)"], "0$1"], [, "(\\d{4})(\\d{2,11})", "$1 $2", ["[24-6]|3(?:[3569][02-46-9]|4[2-4679]|7[2-467]|8[2-46-8])|[7-9](?:0[1-9]|[1-9])", 
"[24-6]|3(?:3(?:0[1-467]|2[127-9]|3[124578]|[46][1246]|7[1257-9]|8[1256]|9[145])|4(?:2[135]|3[1357]|4[13578]|6[1246]|7[1356]|9[1346])|5(?:0[14]|2[1-3589]|3[1357]|[49][1246]|6[1-4]|7[1346]|8[13568])|6(?:0[356]|2[1-489]|3[124-6]|4[1347]|6[13]|7[12579]|8[1-356]|9[135])|7(?:2[1-7]|3[1357]|4[145]|6[1-5]|7[1-4])|8(?:21|3[1468]|4[1347]|6[0135-9]|7[1467]|8[136])|9(?:0[12479]|2[1358]|3[1357]|4[134679]|6[1-9]|7[136]|8[147]|9[1468]))|[7-9](?:0[1-9]|[1-9])"], "0$1"], [, "(3\\d{4})(\\d{1,10})", "$1 $2", ["3"], 
"0$1"], [, "(800)(\\d{7,12})", "$1 $2", ["800"], "0$1"], [, "(\\d{3})(\\d)(\\d{4,10})", "$1 $2 $3", ["1(?:37|80)|900", "1(?:37|80)|900[1359]"], "0$1"], [, "(1\\d{2})(\\d{5,11})", "$1 $2", ["181"], "0$1"], [, "(18\\d{3})(\\d{6})", "$1 $2", ["185", "1850", "18500"], "0$1"], [, "(18\\d{2})(\\d{7})", "$1 $2", ["18[68]"], "0$1"], [, "(18\\d)(\\d{8})", "$1 $2", ["18[2-579]"], "0$1"], [, "(700)(\\d{4})(\\d{4})", "$1 $2 $3", ["700"], "0$1"], [, "(138)(\\d{4})", "$1 $2", ["138"], "0$1"], [, "(15[013-68])(\\d{2})(\\d{8})", 
"$1 $2 $3", ["15[013-68]"], "0$1"], [, "(15[279]\\d)(\\d{2})(\\d{7})", "$1 $2 $3", ["15[279]"], "0$1"], [, "(1[67]\\d)(\\d{2})(\\d{7,8})", "$1 $2 $3", ["1(?:6[023]|7)"], "0$1"]], , [, , "16(?:4\\d{1,10}|[89]\\d{1,11})", , , , "16412345", , , [4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]], , , [, , , , , , , , , [-1]], [, , "18(?:1\\d{5,11}|[2-9]\\d{8})", , , , "18500123456", , , [8, 9, 10, 11, 12, 13, 14]], , , [, , "1(?:5(?:(?:2\\d55|7\\d99|9\\d33)\\d{7}|(?:[034568]00|113)\\d{8})|6(?:013|255|399)\\d{7,8}|7(?:[015]13|[234]55|[69]33|[78]99)\\d{7,8})", 
, , , "177991234567", , , [12, 13]]], DJ:[, [, , "[27]\\d{7}", , , , , , , [8]], [, , "2(?:1[2-5]|7[45])\\d{5}", , , , "21360003"], [, , "77\\d{6}", , , , "77831001"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "DJ", 253, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 
DK:[, [, , "[2-9]\\d{7}", , , , , , , [8]], [, , "(?:[2-7]\\d|8[126-9]|9[1-36-9])\\d{6}", , , , "32123456"], [, , "(?:[2-7]\\d|8[126-9]|9[1-36-9])\\d{6}", , , , "20123456"], [, , "80\\d{6}", , , , "80123456"], [, , "90\\d{6}", , , , "90123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "DK", 45, "00", , , , , , , 1, [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, 
, , , , , , , , [-1]]], DM:[, [, , "[57-9]\\d{9}", , , , , , , [10], [7]], [, , "767(?:2(?:55|66)|4(?:2[01]|4[0-25-9])|50[0-4]|70[1-3])\\d{4}", , , , "7674201234", , , , [7]], [, , "767(?:2(?:[234689]5|7[5-7])|31[5-7]|61[1-7])\\d{4}", , , , "7672251234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "DM", 
1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "767", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], DO:[, [, , "[589]\\d{9}", , , , , , , [10], [7]], [, , "8(?:[04]9[2-9]\\d{6}|29(?:2(?:[0-59]\\d|6[04-9]|7[0-27]|8[0237-9])|3(?:[0-35-9]\\d|4[7-9])|[45]\\d{2}|6(?:[0-27-9]\\d|[3-5][1-9]|6[0135-8])|7(?:0[013-9]|[1-37]\\d|4[1-35689]|5[1-4689]|6[1-57-9]|8[1-79]|9[1-8])|8(?:0[146-9]|1[0-48]|[248]\\d|3[1-79]|5[01589]|6[013-68]|7[124-8]|9[0-8])|9(?:[0-24]\\d|3[02-46-9]|5[0-79]|60|7[0169]|8[57-9]|9[02-9]))\\d{4})", 
, , , "8092345678", , , , [7]], [, , "8[024]9[2-9]\\d{6}", , , , "8092345678", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "DO", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "8[024]9", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], DZ:[, [, , "(?:[1-4]|[5-9]\\d)\\d{7}", 
, , , , , , [8, 9]], [, , "(?:1\\d|2[013-79]|3[0-8]|4[0135689])\\d{6}|9619\\d{5}", , , , "12345678"], [, , "(?:5[4-6]|7[7-9])\\d{7}|6(?:[569]\\d|7[0-6])\\d{6}", , , , "551234567", , , [9]], [, , "800\\d{6}", , , , "800123456", , , [9]], [, , "80[3-689]1\\d{5}", , , , "808123456", , , [9]], [, , "80[12]1\\d{5}", , , , "801123456", , , [9]], [, , , , , , , , , [-1]], [, , "98[23]\\d{6}", , , , "983123456", , , [9]], "DZ", 213, "00", "0", , , "0", , , , [[, "([1-4]\\d)(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", 
["[1-4]"], "0$1"], [, "([5-8]\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[5-8]"], "0$1"], [, "(9\\d)(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["9"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], EC:[, [, , "1\\d{9,10}|[2-8]\\d{7}|9\\d{8}", , , , , , , [8, 9, 10, 11], [7]], [, , "[2-7][2-7]\\d{6}", , , , "22123456", , , [8], [7]], [, , "9(?:(?:39|[45][89]|7[7-9]|[89]\\d)\\d{2}|6(?:[017-9]\\d{2}|2(?:[0-47-9]\\d|5[1-9]|60)|30\\d))\\d{4}", 
, , , "991234567", , , [9]], [, , "1800\\d{6,7}", , , , "18001234567", , , [10, 11]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "[2-7]890\\d{4}", , , , "28901234", , , [8]], "EC", 593, "00", "0", , , "0", , , , [[, "(\\d)(\\d{3})(\\d{4})", "$1 $2-$3", ["[247]|[356][2-8]"], "(0$1)"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["9"], "0$1"], [, "(1800)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["180", "1800"], "$1"]], [[, "(\\d)(\\d{3})(\\d{4})", "$1-$2-$3", ["[247]|[356][2-8]"]], 
[, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["9"], "0$1"], [, "(1800)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["180", "1800"], "$1"]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], EE:[, [, , "[3-9]\\d{6,7}|800\\d{6,7}", , , , , , , [7, 8, 10]], [, , "(?:3[23589]|4[3-8]|6\\d|7[1-9]|88)\\d{5}", , , , "3212345", , , [7]], [, , "(?:5\\d|8[1-5])\\d{6}|5(?:[02]\\d{2}|1(?:[0-8]\\d|95)|5[0-478]\\d|64[0-4]|65[1-589])\\d{3}", , , , "51234567", , , 
[7, 8]], [, , "800(?:0\\d{3}|1\\d|[2-9])\\d{3}", , , , "80012345"], [, , "(?:40\\d{2}|900)\\d{4}", , , , "9001234", , , [7, 8]], [, , , , , , , , , [-1]], [, , "70[0-2]\\d{5}", , , , "70012345", , , [8]], [, , , , , , , , , [-1]], "EE", 372, "00", , , , , , , , [[, "([3-79]\\d{2})(\\d{4})", "$1 $2", ["[369]|4[3-8]|5(?:[0-2]|5[0-478]|6[45])|7[1-9]", "[369]|4[3-8]|5(?:[02]|1(?:[0-8]|95)|5[0-478]|6(?:4[0-4]|5[1-589]))|7[1-9]"]], [, "(70)(\\d{2})(\\d{4})", "$1 $2 $3", ["70"]], [, "(8000)(\\d{3})(\\d{3})", 
"$1 $2 $3", ["800", "8000"]], [, "([458]\\d{3})(\\d{3,4})", "$1 $2", ["40|5|8(?:00|[1-5])", "40|5|8(?:00[1-9]|[1-5])"]]], , [, , , , , , , , , [-1]], , , [, , "800[2-9]\\d{3}", , , , "8002123", , , [7]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], EG:[, [, , "1\\d{4,9}|[24-6]\\d{8}|3\\d{7}|[89]\\d{8,9}", , , , , , , [8, 9, 10], [7]], [, , "(?:1(?:3[23]\\d|5(?:[23]|9\\d))|2[2-4]\\d{2}|3\\d{2}|4(?:0[2-5]|[578][23]|64)\\d|5(?:0[2-7]|5\\d|7[23])\\d|6[24-689]3\\d|8(?:2[2-57]|4[26]|6[237]|8[2-4])\\d|9(?:2[27]|3[24]|52|6[2356]|7[2-4])\\d)\\d{5}", 
, , , "234567890", , , [8, 9], [7]], [, , "1[0125]\\d{8}", , , , "1001234567", , , [10]], [, , "800\\d{7}", , , , "8001234567", , , [10]], [, , "900\\d{7}", , , , "9001234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "EG", 20, "00", "0", , , "0", , , , [[, "(\\d)(\\d{7,8})", "$1 $2", ["[23]"], "0$1"], [, "(\\d{2})(\\d{6,7})", "$1 $2", ["1(?:3|5[239])|[4-6]|[89][2-9]"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["1[0-25]|[89]00"], "0$1"]], 
, [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], EH:[, [, , "[5-9]\\d{8}", , , , , , , [9]], [, , "528[89]\\d{5}", , , , "528812345"], [, , "(?:6(?:[0-79]\\d|8[0-247-9])|7(?:[07][07]|6[12]))\\d{6}", , , , "650123456"], [, , "80\\d{7}", , , , "801234567"], [, , "89\\d{7}", , , , "891234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "5924[01]\\d{4}", , , , "592401234"], "EH", 212, "00", "0", , , "0", , , , , , [, , , 
, , , , , , [-1]], , "528[89]", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], ER:[, [, , "[178]\\d{6}", , , , , , , [7], [6]], [, , "1(?:1[12568]|20|40|55|6[146])\\d{4}|8\\d{6}", , , , "8370362", , , , [6]], [, , "17[1-3]\\d{4}|7\\d{6}", , , , "7123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "ER", 291, "00", "0", , , "0", , , , [[, "(\\d)(\\d{3})(\\d{3})", "$1 $2 $3", 
, "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], ES:[, [, , "[5-9]\\d{8}", , , , , , , [9]], [, , "8(?:[1356]\\d|[28][0-8]|[47][1-9])\\d{6}|9(?:[135]\\d{7}|[28][0-8]\\d{6}|4[1-9]\\d{6}|6(?:[0-8]\\d{6}|9(?:0(?:[0-57-9]\\d{4}|6(?:0[0-8]|1[1-9]|[2-9]\\d)\\d{2})|[1-9]\\d{5}))|7(?:[124-9]\\d{2}|3(?:[0-8]\\d|9[1-9]))\\d{4})", , , , "810123456"], [, , "(?:6\\d{6}|7[1-48]\\d{5}|9(?:6906(?:09|10)|7390\\d{2}))\\d{2}", , , , "612345678"], 
[, , "[89]00\\d{6}", , , , "800123456"], [, , "80[367]\\d{6}", , , , "803123456"], [, , "90[12]\\d{6}", , , , "901123456"], [, , "70\\d{7}", , , , "701234567"], [, , , , , , , , , [-1]], "ES", 34, "00", , , , , , , , [[, "([89]00)(\\d{3})(\\d{3})", "$1 $2 $3", ["[89]00"]], [, "([5-9]\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[568]|[79][0-8]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "51\\d{7}", , , , "511234567"], , , [, , , , , , , , , [-1]]], ET:[, [, , "[1-59]\\d{8}", 
, , , , , , [9], [7]], [, , "(?:11(?:1(?:1[124]|2[2-57]|3[1-5]|5[5-8]|8[6-8])|2(?:13|3[6-8]|5[89]|7[05-9]|8[2-6])|3(?:2[01]|3[0-289]|4[1289]|7[1-4]|87)|4(?:1[69]|3[2-49]|4[0-3]|6[5-8])|5(?:1[578]|44|5[0-4])|6(?:18|2[69]|39|4[5-7]|5[1-5]|6[0-59]|8[015-8]))|2(?:2(?:11[1-9]|22[0-7]|33\\d|44[1467]|66[1-68])|5(?:11[124-6]|33[2-8]|44[1467]|55[14]|66[1-3679]|77[124-79]|880))|3(?:3(?:11[0-46-8]|22[0-6]|33[0134689]|44[04]|55[0-6]|66[01467])|4(?:44[0-8]|55[0-69]|66[0-3]|77[1-5]))|4(?:6(?:22[0-24-7]|33[1-5]|44[13-69]|55[14-689]|660|88[1-4])|7(?:11[1-9]|22[1-9]|33[13-7]|44[13-6]|55[1-689]))|5(?:7(?:227|55[05]|(?:66|77)[14-8])|8(?:11[149]|22[013-79]|33[0-68]|44[013-8]|550|66[1-5]|77\\d)))\\d{4}", 
, , , "111112345", , , , [7]], [, , "9(?:[1-46-8]\\d|5[89])\\d{6}", , , , "911234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "ET", 251, "00", "0", , , "0", , , , [[, "([1-59]\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["[1-59]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], FI:[, [, , "[16]\\d{6,9}|2\\d{4,9}|[35]\\d{5,9}|4\\d{7,10}|7\\d{7,9}|8\\d{6,8}|9\\d{4,8}", 
, , , , , , [5, 6, 7, 8, 9, 10, 11]], [, , "1[3-79][1-8]\\d{4,6}|[23568][1-8]\\d{5,7}|9[1-8]\\d{3,7}", , , , "131234567", , , [5, 6, 7, 8, 9]], [, , "4[0-8]\\d{6,8}|50\\d{4,8}", , , , "412345678", , , [6, 7, 8, 9, 10, 11]], [, , "800\\d{5,6}", , , , "800123456", , , [8, 9]], [, , "[67]00\\d{5,6}", , , , "600123456", , , [8, 9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "FI", 358, "00|99(?:[02469]|5(?:11|33|5[59]|88|9[09]))", "0", , , "0", , "00", , [[, "(\\d{3})(\\d{3,7})", 
"$1 $2", ["(?:[1-3]0|[6-8])0"], "0$1"], [, "(116\\d{3})", "$1", ["116"], "$1"], [, "(\\d{2})(\\d{3,9})", "$1 $2", ["1(?:0[1-9]|[3-9])|2(?:0[1-9]|9)|30[1-9]|4|50|7(?:[13]|5[03-9])"], "0$1"], [, "(75\\d{3})", "$1", ["75[12]"], "0$1"], [, "(\\d)(\\d{4})", "$1 $2", ["9[1-8]"], "0$1"], [, "(\\d)(\\d{5,9})", "$1 $2", ["[235689][1-8]"], "0$1"], [, "(39\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["39"], "0$1"]], [[, "(\\d{3})(\\d{3,7})", "$1 $2", ["(?:[1-3]0|[6-8])0"], "0$1"], [, "(116\\d{3})", "$1", ["116"], "$1"], 
[, "(\\d{2})(\\d{3,9})", "$1 $2", ["1(?:0[1-9]|[3-9])|2(?:0[1-9]|9)|30[1-9]|4|50|7(?:[13]|5[03-9])"], "0$1"], [, "(\\d)(\\d{4})", "$1 $2", ["9[1-8]"], "0$1"], [, "(\\d)(\\d{5,9})", "$1 $2", ["[235689][1-8]"], "0$1"], [, "(39\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["39"], "0$1"]], [, , , , , , , , , [-1]], 1, , [, , "100\\d{4,6}|20(?:0\\d{4,6}|2[023]\\d{4,5}|9[89]\\d{1,6})|300\\d{3,7}|60(?:[12]\\d{5,6}|6\\d{7})|7(?:1\\d{7}|3\\d{8}|5[03-9]\\d{5,6})", , , , "1001234", , , [5, 6, 7, 8, 9, 10]], [, , "10(?:0\\d{4,6}|[1-46-9]\\d{5,7}|5\\d{4,7})|2(?:0(?:0\\d{4,6}|[1346-8]\\d{5,7}|2(?:[023]\\d{4,5}|[14-9]\\d{4,6})|5(?:\\d{3}|\\d{5,7})|9(?:[0-7]\\d{4,6}|[89]\\d{1,6}))|9\\d{5,8})|3(?:0(?:0\\d{3,7}|[1-57-9]\\d{5,7}|6(?:\\d{3}|\\d{5,7}))|44\\d{3}|93\\d{5,7})|60(?:[12]\\d{5,6}|6\\d{7})|7(?:1\\d{7}|3\\d{8}|5[03-9]\\d{5,6})", 
, , , "10112345", , , [5, 6, 7, 8, 9, 10]], , , [, , , , , , , , , [-1]]], FJ:[, [, , "[2-9]\\d{6}|0\\d{10}", , , , , , , [7, 11]], [, , "(?:3[0-5]|6[25-7]|8[58])\\d{5}", , , , "3212345", , , [7]], [, , "(?:[279]\\d|45|5[01568]|8[034679])\\d{5}", , , , "7012345", , , [7]], [, , "0800\\d{7}", , , , "08001234567", , , [11]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "FJ", 679, "0(?:0|52)", , , , , , "00", , [[, "(\\d{3})(\\d{4})", "$1 $2", 
["[2-9]"]], [, "(\\d{4})(\\d{3})(\\d{4})", "$1 $2 $3", ["0"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], FK:[, [, , "[2-7]\\d{4}", , , , , , , [5]], [, , "[2-47]\\d{4}", , , , "31234"], [, , "[56]\\d{4}", , , , "51234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "FK", 500, "00", , , , , , , , , , [, , , , , , , , , [-1]], , , [, , , , , 
, , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], FM:[, [, , "[39]\\d{6}", , , , , , , [7]], [, , "3[2357]0[1-9]\\d{3}|9[2-6]\\d{5}", , , , "3201234"], [, , "3[2357]0[1-9]\\d{3}|9[2-7]\\d{5}", , , , "3501234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "FM", 691, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , 
, , [-1]], , , [, , , , , , , , , [-1]]], FO:[, [, , "[2-9]\\d{5}", , , , , , , [6]], [, , "(?:20|[3-4]\\d|8[19])\\d{4}", , , , "201234"], [, , "(?:[27][1-9]|5\\d)\\d{4}", , , , "211234"], [, , "80[257-9]\\d{3}", , , , "802123"], [, , "90(?:[1345][15-7]|2[125-7]|99)\\d{2}", , , , "901123"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "(?:6[0-36]|88)\\d{4}", , , , "601234"], "FO", 298, "00", , , , "(10(?:01|[12]0|88))", , , , [[, "(\\d{6})", "$1", , , "$CC $1"]], , [, , , , , , , , , 
[-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], FR:[, [, , "[1-9]\\d{8}", , , , , , , [9]], [, , "[1-5]\\d{8}", , , , "123456789"], [, , "(?:6\\d|7[3-9])\\d{7}", , , , "612345678"], [, , "80[0-5]\\d{6}", , , , "801234567"], [, , "89[1-37-9]\\d{6}", , , , "891123456"], [, , "8(?:1[0-29]|2[0156]|84|90)\\d{6}", , , , "810123456"], [, , , , , , , , , [-1]], [, , "9\\d{8}", , , , "912345678"], "FR", 33, "00", "0", , , "0", , , , [[, "([1-79])(\\d{2})(\\d{2})(\\d{2})(\\d{2})", 
"$1 $2 $3 $4 $5", ["[1-79]"], "0$1"], [, "(1\\d{2})(\\d{3})", "$1 $2", ["11"], "$1"], [, "(8\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["8"], "0 $1"]], [[, "([1-79])(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4 $5", ["[1-79]"], "0$1"], [, "(8\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["8"], "0 $1"]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "80[6-9]\\d{6}", , , , "806123456"], , , [, , , , , , , , , [-1]]], GA:[, [, , "0?\\d{7}", , , , , , , [7, 8]], [, , "01\\d{6}", 
, , , "01441234", , , [8]], [, , "0?[2-7]\\d{6}", , , , "06031234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "GA", 241, "00", , , , , , , , [[, "(\\d)(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[2-7]"], "0$1"], [, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["0"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GB:[, [, , "\\d{7,10}", 
, , , , , , [7, 9, 10], [4, 5, 6, 8]], [, , "2(?:0[01378]|3[0189]|4[017]|8[0-46-9]|9[0-2])\\d{7}|1(?:1(?:3[0-48]|[46][0-4]|5[0-26-9]|[78][0-49])|21[0-7]|31[0-8]|[4-69]1\\d)\\d{6}|1(?:2(?:0[024-9]|2[3-9]|3[3-79]|4[1-689]|[58][02-9]|6[0-47-9]|7[013-9]|9\\d)|3(?:0\\d|[25][02-9]|3[02-579]|[468][0-46-9]|7[1-35-79]|9[2-578])|4(?:0[03-9]|[28][02-57-9]|[37]\\d|4[02-69]|5[0-8]|[69][0-79])|5(?:0[1-35-9]|2[024-9]|3[015689]|4[02-9]|5[03-9]|6\\d|7[0-35-9]|8[0-468]|9[0-57-9])|6(?:0[034689]|2[0-35689]|[38][013-9]|4[1-467]|5[0-69]|6[13-9]|7[0-8]|9[0124578])|7(?:0[0246-9]|2\\d|3[0236-8]|4[03-9]|5[0-46-9]|6[013-9]|7[0-35-9]|8[024-9]|9[02-9])|8(?:0[35-9]|2[1-57-9]|3[02-578]|4[0-578]|5[124-9]|6[2-69]|7\\d|8[02-9]|9[02569])|9(?:0[02-589]|2[02-689]|3[1-57-9]|4[2-9]|5[0-579]|6[2-47-9]|7[0-24578]|8\\d|9[2-57]))\\d{6}|1(?:2(?:0(?:46[1-4]|87[2-9])|545[1-79]|76(?:2\\d|3[1-8]|6[1-6])|9(?:7(?:2[0-4]|3[2-5])|8(?:2[2-8]|7[0-47-9]|8[345])))|3(?:638[2-5]|647[23]|8(?:47[04-9]|64[0157-9]))|4(?:044[1-7]|20(?:2[23]|8\\d)|6(?:0(?:30|5[2-57]|6[1-8]|7[2-8])|140)|8(?:052|87[123]))|5(?:24(?:3[2-79]|6\\d)|276\\d|6(?:26[06-9]|686))|6(?:06(?:4\\d|7[4-79])|295[567]|35[34]\\d|47(?:24|61)|59(?:5[08]|6[67]|74)|955[0-4])|7(?:26(?:6[13-9]|7[0-7])|442\\d|50(?:2[0-3]|[3-68]2|76))|8(?:27[56]\\d|37(?:5[2-5]|8[239])|84(?:3[2-58]))|9(?:0(?:0(?:6[1-8]|85)|52\\d)|3583|4(?:66[1-8]|9(?:2[01]|81))|63(?:23|3[1-4])|9561))\\d{3}|176888[2-46-8]\\d{2}|16977[23]\\d{3}", 
, , , "1212345678", , , [9, 10], [4, 5, 6, 7, 8]], [, , "7(?:[1-3]\\d{3}|4(?:[0-46-9]\\d{2}|5(?:[0-689]\\d|7[0-57-9]))|5(?:0[0-8]|[13-9]\\d|2[0-35-9])\\d|7(?:0(?:0[01]|[1-9]\\d)|[1-7]\\d{2}|8[02-9]\\d|9[0-689]\\d)|8(?:[014-9]\\d|[23][0-8])\\d|9(?:[024-9]\\d{2}|1(?:[02-9]\\d|1[028])|3[0-689]\\d))\\d{5}", , , , "7400123456", , , [10]], [, , "80(?:0(?:1111|\\d{6,7})|8\\d{7})", , , , "8001234567"], [, , "(?:87[123]|9(?:[01]\\d|8[2349]))\\d{7}", , , , "9012345678", , , [10]], [, , "8(?:4(?:5464\\d|[2-5]\\d{7})|70\\d{7})", 
, , , "8431234567", , , [7, 10]], [, , "70\\d{8}", , , , "7012345678", , , [10]], [, , "56\\d{8}", , , , "5612345678", , , [10]], "GB", 44, "00", "0", " x", , "0", , , , [[, "(7\\d{3})(\\d{6})", "$1 $2", ["7(?:[1-57-9]|62)", "7(?:[1-57-9]|624)"], "0$1"], [, "(\\d{2})(\\d{4})(\\d{4})", "$1 $2 $3", ["2|5[56]|7[06]"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["1(?:[02-9]1|1)|3|9[018]"], "0$1"], [, "(\\d{5})(\\d{4,5})", "$1 $2", ["1(?:38|5[23]|69|76|94)", "1(?:(?:38|69)7|5(?:24|39)|768|946)", 
"1(?:3873|5(?:242|39[4-6])|(?:697|768)[347]|9467)"], "0$1"], [, "(1\\d{3})(\\d{5,6})", "$1 $2", ["1"], "0$1"], [, "(800)(\\d{4})", "$1 $2", ["800", "8001", "80011", "800111", "8001111"], "0$1"], [, "(845)(46)(4\\d)", "$1 $2 $3", ["845", "8454", "84546", "845464"], "0$1"], [, "(8\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["8(?:4[2-5]|7[0-3])"], "0$1"], [, "(80\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["80"], "0$1"], [, "(800)(\\d{6})", "$1 $2", ["800"], "0$1"]], , [, , "76(?:0[012]|2[356]|4[0134]|5[49]|6[0-369]|77|81|9[39])\\d{6}", 
, , , "7640123456", , , [10]], 1, , [, , , , , , , , , [-1]], [, , "(?:3[0347]|55)\\d{8}", , , , "5512345678", , , [10]], , , [, , , , , , , , , [-1]]], GD:[, [, , "[4589]\\d{9}", , , , , , , [10], [7]], [, , "473(?:2(?:3[0-2]|69)|3(?:2[89]|86)|4(?:[06]8|3[5-9]|4[0-49]|5[5-79]|68|73|90)|63[68]|7(?:58|84)|800|938)\\d{4}", , , , "4732691234", , , , [7]], [, , "473(?:4(?:0[2-79]|1[04-9]|2[0-5]|58)|5(?:2[01]|3[3-8])|901)\\d{4}", , , , "4734031234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", 
, , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "GD", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "473", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GE:[, [, , "[34578]\\d{8}", , , , , , , [9], [6]], [, , "(?:3(?:[256]\\d|4[124-9]|7[0-4])|4(?:1\\d|2[2-7]|3[1-79]|4[2-8]|7[239]|9[1-7]))\\d{6}", , , , "322123456", 
, , , [6]], [, , "5(?:[14]4|5[0157-9]|68|7[0147-9]|9[0-35-9])\\d{6}", , , , "555123456"], [, , "800\\d{6}", , , , "800123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "706\\d{6}", , , , "706123456"], "GE", 995, "00", "0", , , "0", , , , [[, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[348]"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["7"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["5"]]], , [, , , , , , , , , [-1]], 
, , [, , "706\\d{6}", , , , "706123456"], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GF:[, [, , "[56]\\d{8}", , , , , , , [9]], [, , "594(?:10|2[012457-9]|3[0-57-9]|4[3-9]|5[7-9]|6[0-3]|9[014])\\d{4}", , , , "594101234"], [, , "694(?:[0249]\\d|1[2-9]|3[0-48])\\d{4}", , , , "694201234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "GF", 594, "00", "0", , , "0", , , , [[, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", 
"$1 $2 $3 $4", , "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GG:[, [, , "[135789]\\d{6,9}", , , , , , , [7, 9, 10], [6]], [, , "1481[25-9]\\d{5}", , , , "1481256789", , , [10], [6]], [, , "7(?:781\\d|839\\d|911[17])\\d{5}", , , , "7781123456", , , [10]], [, , "80(?:0(?:1111|\\d{6,7})|8\\d{7})", , , , "8001234567"], [, , "(?:87[123]|9(?:[01]\\d|8[0-3]))\\d{7}", , , , "9012345678", , , [10]], [, , "8(?:4(?:5464\\d|[2-5]\\d{7})|70\\d{7})", 
, , , "8431234567", , , [7, 10]], [, , "70\\d{8}", , , , "7012345678", , , [10]], [, , "56\\d{8}", , , , "5612345678", , , [10]], "GG", 44, "00", "0", , , "0", , , , , , [, , "76(?:0[012]|2[356]|4[0134]|5[49]|6[0-369]|77|81|9[39])\\d{6}", , , , "7640123456", , , [10]], , , [, , , , , , , , , [-1]], [, , "(?:3[0347]|55)\\d{8}", , , , "5512345678", , , [10]], , , [, , , , , , , , , [-1]]], GH:[, [, , "[235]\\d{8}|8\\d{7}", , , , , , , [8, 9], [7]], [, , "3(?:0(?:[237]\\d|80)|[167](?:2[0-6]|7\\d|80)|2(?:2[0-5]|7\\d|80)|3(?:2[0-3]|7\\d|80)|4(?:2[013-9]|3[01]|7\\d|80)|5(?:2[0-7]|7\\d|80)|8(?:2[0-2]|7\\d|80)|9(?:[28]0|7\\d))\\d{5}", 
, , , "302345678", , , [9], [7]], [, , "(?:2[034678]\\d|5(?:[0457]\\d|6[01]))\\d{6}", , , , "231234567", , , [9]], [, , "800\\d{5}", , , , "80012345", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "GH", 233, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["[235]"], "0$1"], [, "(\\d{3})(\\d{5})", "$1 $2", ["8"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , "800\\d{5}", , , , "80012345", , , [8]], [, , , , , 
, , , , [-1]], , , [, , , , , , , , , [-1]]], GI:[, [, , "[256]\\d{7}", , , , , , , [8]], [, , "2(?:00\\d{2}|1(?:6[24-7]\\d|90[0-2])|2(?:2[2457]\\d|50[0-2]))\\d{3}", , , , "20012345"], [, , "(?:5[46-8]|62)\\d{6}", , , , "57123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "GI", 350, "00", , , , , , , , [[, "(\\d{3})(\\d{5})", "$1 $2", ["2"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , 
, , , , , , [-1]], , , [, , , , , , , , , [-1]]], GL:[, [, , "[1-689]\\d{5}", , , , , , , [6]], [, , "(?:19|3[1-7]|6[14689]|8[14-79]|9\\d)\\d{4}", , , , "321000"], [, , "(?:[25][1-9]|4[2-9])\\d{4}", , , , "221234"], [, , "80\\d{4}", , , , "801234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "3[89]\\d{4}", , , , "381234"], "GL", 299, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, 
, , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GM:[, [, , "[2-9]\\d{6}", , , , , , , [7]], [, , "(?:4(?:[23]\\d{2}|4(?:1[024679]|[6-9]\\d))|5(?:54[0-7]|6(?:[67]\\d)|7(?:1[04]|2[035]|3[58]|48))|8\\d{3})\\d{3}", , , , "5661234"], [, , "[23679]\\d{6}", , , , "3012345"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "GM", 220, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , 
, [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GN:[, [, , "[367]\\d{7,8}", , , , , , , [8, 9]], [, , "30(?:24|3[12]|4[1-35-7]|5[13]|6[189]|[78]1|9[1478])\\d{4}", , , , "30241234", , , [8]], [, , "6[02356]\\d{7}", , , , "601123456", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "722\\d{6}", , , , "722123456", , , [9]], "GN", 224, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", 
"$1 $2 $3 $4", ["3"]], [, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[67]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GP:[, [, , "[56]\\d{8}", , , , , , , [9]], [, , "590(?:0[13468]|1[012]|2[0-68]|3[28]|4[0-8]|5[579]|6[0189]|70|8[0-689]|9\\d)\\d{4}", , , , "590201234"], [, , "690(?:0[05-9]|[1-9]\\d)\\d{4}", , , , "690001234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , 
, , , , , [-1]], [, , , , , , , , , [-1]], "GP", 590, "00", "0", , , "0", , , , [[, "([56]90)(\\d{2})(\\d{4})", "$1 $2-$3", ["[56]90"], "0$1"]], , [, , , , , , , , , [-1]], 1, , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GQ:[, [, , "[23589]\\d{8}", , , , , , , [9]], [, , "3(?:3(?:3\\d[7-9]|[0-24-9]\\d[46])|5\\d{2}[7-9])\\d{4}", , , , "333091234"], [, , "(?:222|55[15])\\d{6}", , , , "222123456"], [, , "80\\d[1-9]\\d{5}", , , , "800123456"], [, , "90\\d[1-9]\\d{5}", 
, , , "900123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "GQ", 240, "00", , , , , , , , [[, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["[235]"]], [, "(\\d{3})(\\d{6})", "$1 $2", ["[89]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GR:[, [, , "[26-9]\\d{9}", , , , , , , [10]], [, , "2(?:1\\d{2}|2(?:2[1-46-9]|3[1-8]|4[1-7]|5[1-4]|6[1-8]|7[1-5]|[89][1-9])|3(?:1\\d|2[1-57]|[35][1-3]|4[13]|7[1-7]|8[124-6]|9[1-79])|4(?:1\\d|2[1-8]|3[1-4]|4[13-5]|6[1-578]|9[1-5])|5(?:1\\d|[29][1-4]|3[1-5]|4[124]|5[1-6])|6(?:1\\d|3[1245]|4[1-7]|5[13-9]|[269][1-6]|7[14]|8[1-5])|7(?:1\\d|2[1-5]|3[1-6]|4[1-7]|5[1-57]|6[135]|9[125-7])|8(?:1\\d|2[1-5]|[34][1-4]|9[1-57]))\\d{6}", 
, , , "2123456789"], [, , "69\\d{8}", , , , "6912345678"], [, , "800\\d{7}", , , , "8001234567"], [, , "90[19]\\d{7}", , , , "9091234567"], [, , "8(?:0[16]|12|25)\\d{7}", , , , "8011234567"], [, , "70\\d{8}", , , , "7012345678"], [, , , , , , , , , [-1]], "GR", 30, "00", , , , , , , , [[, "([27]\\d)(\\d{4})(\\d{4})", "$1 $2 $3", ["21|7"]], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["2[2-9]1|[689]"]], [, "(2\\d{3})(\\d{6})", "$1 $2", ["2[2-9][02-9]"]]], , [, , , , , , , , , [-1]], , , [, , , , , 
, , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GT:[, [, , "[2-7]\\d{7}|1[89]\\d{9}", , , , , , , [8, 11]], [, , "[267][2-9]\\d{6}", , , , "22456789", , , [8]], [, , "[345]\\d{7}", , , , "51234567", , , [8]], [, , "18[01]\\d{8}", , , , "18001112222", , , [11]], [, , "19\\d{9}", , , , "19001112222", , , [11]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "GT", 502, "00", , , , , , , , [[, "(\\d{4})(\\d{4})", "$1 $2", ["[2-7]"]], [, "(\\d{4})(\\d{3})(\\d{4})", 
"$1 $2 $3", ["1"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GU:[, [, , "[5689]\\d{9}", , , , , , , [10], [7]], [, , "671(?:3(?:00|3[39]|4[349]|55|6[26])|4(?:56|7[1-9]|8[0236-9])|5(?:55|6[2-5]|88)|6(?:3[2-578]|4[24-9]|5[34]|78|8[5-9])|7(?:[079]7|2[0167]|3[45]|47|8[789])|8(?:[2-5789]8|6[48])|9(?:2[29]|6[79]|7[179]|8[789]|9[78]))\\d{4}", , , , "6713001234", , , , [7]], [, , "671(?:3(?:00|3[39]|4[349]|55|6[26])|4(?:56|7[1-9]|8[0236-9])|5(?:55|6[2-5]|88)|6(?:3[2-578]|4[24-9]|5[34]|78|8[5-9])|7(?:[079]7|2[0167]|3[45]|47|8[789])|8(?:[2-5789]8|6[48])|9(?:2[29]|6[79]|7[179]|8[789]|9[78]))\\d{4}", 
, , , "6713001234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "GU", 1, "011", "1", , , "1", , , 1, , , [, , , , , , , , , [-1]], , "671", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], GW:[, [, , "(?:4(?:0\\d{5}|4\\d{7})|9\\d{8})", , , , , , , [7, 9]], [, , "443\\d{6}", 
, , , "443201234", , , [9]], [, , "9(?:5(?:5\\d|6[0-2])|6(?:5[0-2]|6\\d|9[012])|77\\d)\\d{5}", , , , "955012345", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "40\\d{5}", , , , "4012345", , , [7]], "GW", 245, "00", , , , , , , , [[, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["44|9[5-7]"]], [, "(\\d{3})(\\d{4})", "$1 $2", ["40"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , 
, , , , , [-1]]], GY:[, [, , "[2-46-9]\\d{6}", , , , , , , [7]], [, , "(?:2(?:1[6-9]|2[0-35-9]|3[1-4]|5[3-9]|6\\d|7[0-24-79])|3(?:2[25-9]|3\\d)|4(?:4[0-24]|5[56])|77[1-57])\\d{4}", , , , "2201234"], [, , "6\\d{6}", , , , "6091234"], [, , "(?:289|862)\\d{4}", , , , "2891234"], [, , "9008\\d{3}", , , , "9008123"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "GY", 592, "001", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , 
, , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], HK:[, [, , "[235-7]\\d{7}|8[0-3]\\d{6,7}|9\\d{4,10}", , , , , , , [5, 6, 7, 8, 9, 11]], [, , "(?:2(?:[13-8]\\d|2[013-9]|9[0-24-9])|3(?:[1569][0-24-9]|4[0-246-9]|7[0-24-69]|89)|58[01])\\d{5}", , , , "21234567", , , [8]], [, , "(?:5(?:[1-59][0-46-9]|6[0-4689]|7[0-469])|6(?:0[1-9]|[1459]\\d|[2368][0-57-9]|7[0-79])|9(?:0[1-9]|1[02-9]|[2358][0-8]|[467]\\d))\\d{5}", , , , "51234567", , , [8]], [, , "800\\d{6}", , , , "800123456", , 
, [9]], [, , "900(?:[0-24-9]\\d{7}|3\\d{1,4})", , , , "90012345678", , , [5, 6, 7, 8, 11]], [, , , , , , , , , [-1]], [, , "8(?:1[1-4679]|2[0-367]|3[02-47])\\d{5}", , , , "81123456", , , [8]], [, , , , , , , , , [-1]], "HK", 852, "00(?:[126-9]|30|5[09])?", , , , , , "00", , [[, "(\\d{4})(\\d{4})", "$1 $2", ["[235-7]|[89](?:0[1-9]|[1-9])"]], [, "(800)(\\d{3})(\\d{3})", "$1 $2 $3", ["800"]], [, "(900)(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3 $4", ["900"]], [, "(900)(\\d{2,5})", "$1 $2", ["900"]]], , [, 
, "7(?:1[0-369]|[23][0-37-9]|47|5[1578]|6[0235]|7[278]|8[236-9]|9[025-9])\\d{5}", , , , "71234567", , , [8]], , , [, , , , , , , , , [-1]], [, , "30(?:0[1-9]|[15-7]\\d|2[047]|89)\\d{4}", , , , "30161234", , , [8]], , , [, , , , , , , , , [-1]]], HN:[, [, , "[237-9]\\d{7}", , , , , , , [8]], [, , "2(?:2(?:0[019]|1[1-36]|[23]\\d|4[04-6]|5[57]|7[013689]|8[0146-9]|9[012])|4(?:07|2[3-59]|3[13-689]|4[0-68]|5[1-35])|5(?:16|4[03-5]|5\\d|6[4-6]|74)|6(?:[056]\\d|17|3[04]|4[0-378]|[78][0-8]|9[01])|7(?:6[46-9]|7[02-9]|8[034])|8(?:79|8[0-35789]|9[1-57-9]))\\d{4}", 
, , , "22123456"], [, , "[37-9]\\d{7}", , , , "91234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "HN", 504, "00", , , , , , , , [[, "(\\d{4})(\\d{4})", "$1-$2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], HR:[, [, , "[1-7]\\d{5,8}|[89]\\d{6,8}", , , , , , , [6, 7, 8, 9]], [, , "1\\d{7}|(?:2[0-3]|3[1-5]|4[02-47-9]|5[1-3])\\d{6,7}", , , 
, "12345678", , , [8, 9], [6, 7]], [, , "9(?:01\\d|[1259]\\d{2}|7(?:[0679]\\d|51)|8\\d{1,2})\\d{5}", , , , "921234567", , , [8, 9]], [, , "80[01]\\d{4,6}", , , , "800123456", , , [7, 8, 9]], [, , "6(?:[01]\\d{0,2}|[459]\\d{2})\\d{4}", , , , "611234", , , [6, 7, 8]], [, , , , , , , , , [-1]], [, , "7[45]\\d{6}", , , , "74123456", , , [8]], [, , , , , , , , , [-1]], "HR", 385, "00", "0", , , "0", , , , [[, "(1)(\\d{4})(\\d{3})", "$1 $2 $3", ["1"], "0$1"], [, "([2-5]\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", 
["[2-5]"], "0$1"], [, "(9\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["9"], "0$1"], [, "(6[01])(\\d{2})(\\d{2,3})", "$1 $2 $3", ["6[01]"], "0$1"], [, "([67]\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["[67]"], "0$1"], [, "(80[01])(\\d{2})(\\d{2,3})", "$1 $2 $3", ["80[01]"], "0$1"], [, "(80[01])(\\d{3})(\\d{3})", "$1 $2 $3", ["80[01]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "(?:62\\d?|72)\\d{6}", , , , "62123456", , , [8, 9]], , , [, , , , , , , , , [-1]]], HT:[, [, , "[2-489]\\d{7}", 
, , , , , , [8]], [, , "2(?:2\\d|5[1-5]|81|9[149])\\d{5}", , , , "22453300"], [, , "[34]\\d{7}", , , , "34101234"], [, , "8\\d{7}", , , , "80012345"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "(?:9(?:[67][0-4]|8[0-3589]|9\\d))\\d{5}", , , , "98901234"], "HT", 509, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{4})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], HU:[, [, , "[1-9]\\d{7,8}", 
, , , , , , [8, 9], [6]], [, , "(?:1\\d|2[2-9]|3[2-7]|4[24-9]|5[2-79]|6[23689]|7[2-9]|8[2-57-9]|9[2-69])\\d{6}", , , , "12345678", , , [8], [6]], [, , "(?:[257]0|3[01])\\d{7}", , , , "201234567", , , [9]], [, , "[48]0\\d{6}", , , , "80123456", , , [8]], [, , "9[01]\\d{6}", , , , "90123456", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "21\\d{7}", , , , "211234567", , , [9]], "HU", 36, "00", "06", , , "06", , , , [[, "(1)(\\d{3})(\\d{4})", "$1 $2 $3", ["1"], "($1)"], [, "(\\d{2})(\\d{3})(\\d{3,4})", 
"$1 $2 $3", ["[2-9]"], "($1)"]], , [, , , , , , , , , [-1]], , , [, , "[48]0\\d{6}", , , , "80123456", , , [8]], [, , "38\\d{7}", , , , "381234567", , , [9]], , , [, , , , , , , , , [-1]]], ID:[, [, , "(?:[1-79]\\d{6,10}|8\\d{7,11})", , , , , , , [7, 8, 9, 10, 11, 12], [5, 6]], [, , "2(?:1(?:14\\d{3}|[0-8]\\d{6,7}|500\\d{3}|9\\d{6})|2\\d{6,8}|4\\d{7,8})|(?:2(?:[35][1-4]|6[0-8]|7[1-6]|8\\d|9[1-8])|3(?:1|[25][1-8]|3[1-68]|4[1-3]|6[1-3568]|7[0-469]|8\\d)|4(?:0[1-589]|1[01347-9]|2[0-36-8]|3[0-24-68]|43|5[1-378]|6[1-5]|7[134]|8[1245])|5(?:1[1-35-9]|2[25-8]|3[124-9]|4[1-3589]|5[1-46]|6[1-8])|6(?:19?|[25]\\d|3[1-69]|4[1-6])|7(?:02|[125][1-9]|[36]\\d|4[1-8]|7[0-36-9])|9(?:0[12]|1[013-8]|2[0-479]|5[125-8]|6[23679]|7[159]|8[01346]))\\d{5,8}", 
, , , "612345678", , , [7, 8, 9, 10, 11], [5, 6]], [, , "(?:2(?:1(?:3[145]|4[01]|5[1-469]|60|8[0359]|9\\d)|2(?:88|9[1256])|3[1-4]9|4(?:36|91)|5(?:1[349]|[2-4]9)|6[0-7]9|7(?:[1-36]9|4[39])|8[1-5]9|9[1-48]9)|3(?:19[1-3]|2[12]9|3[13]9|4(?:1[69]|39)|5[14]9|6(?:1[69]|2[89])|709)|4[13]19|5(?:1(?:19|8[39])|4[129]9|6[12]9)|6(?:19[12]|2(?:[23]9|77))|7(?:1[13]9|2[15]9|419|5(?:1[89]|29)|6[15]9|7[178]9))\\d{5,6}|8[1-35-9]\\d{7,10}", , , , "812345678", , , [9, 10, 11, 12]], [, , "177\\d{6,8}|800\\d{5,7}", , , 
, "8001234567", , , [8, 9, 10, 11]], [, , "809\\d{7}", , , , "8091234567", , , [10]], [, , "804\\d{7}", , , , "8041234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "ID", 62, "0(?:0[1789]|10(?:00|1[67]))", "0", , , "0", , , , [[, "(\\d{2})(\\d{5,8})", "$1 $2", ["2[124]|[36]1"], "(0$1)"], [, "(\\d{3})(\\d{5,8})", "$1 $2", ["2[035-9]|[36][02-9]|[4579]"], "(0$1)"], [, "(8\\d{2})(\\d{3,4})(\\d{3})", "$1-$2-$3", ["8[1-35-9]"], "0$1"], [, "(8\\d{2})(\\d{4})(\\d{4,5})", "$1-$2-$3", 
["8[1-35-9]"], "0$1"], [, "(1)(500)(\\d{3})", "$1 $2 $3", ["150", "1500"], "$1"], [, "(177)(\\d{6,8})", "$1 $2", ["177"], "0$1"], [, "(800)(\\d{5,7})", "$1 $2", ["800"], "0$1"], [, "(804)(\\d{3})(\\d{4})", "$1 $2 $3", ["804"], "0$1"], [, "(80\\d)(\\d)(\\d{3})(\\d{3})", "$1 $2 $3 $4", ["80[79]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , "8071\\d{6}", , , , "8071123456", , , [10]], [, , "1500\\d{3}|8071\\d{6}", , , , "8071123456", , , [7, 10]], , , [, , , , , , , , , [-1]]], IE:[, [, , "[124-9]\\d{6,9}", 
, , , , , , [7, 8, 9, 10], [5, 6]], [, , "1\\d{7,8}|2(?:1\\d{6,7}|3\\d{7}|[24-9]\\d{5})|4(?:0[24]\\d{5}|[1-469]\\d{7}|5\\d{6}|7\\d{5}|8[0-46-9]\\d{7})|5(?:0[45]\\d{5}|1\\d{6}|[23679]\\d{7}|8\\d{5})|6(?:1\\d{6}|[237-9]\\d{5}|[4-6]\\d{7})|7[14]\\d{7}|9(?:1\\d{6}|[04]\\d{7}|[35-9]\\d{5})", , , , "2212345", , , , [5, 6]], [, , "8(?:22\\d{6}|[35-9]\\d{7})", , , , "850123456", , , [9]], [, , "1800\\d{6}", , , , "1800123456", , , [10]], [, , "15(?:1[2-8]|[2-8]0|9[089])\\d{6}", , , , "1520123456", , , [10]], 
[, , "18[59]0\\d{6}", , , , "1850123456", , , [10]], [, , "700\\d{6}", , , , "700123456", , , [9]], [, , "76\\d{7}", , , , "761234567", , , [9]], "IE", 353, "00", "0", , , "0", , , , [[, "(1)(\\d{3,4})(\\d{4})", "$1 $2 $3", ["1"], "(0$1)"], [, "(\\d{2})(\\d{5})", "$1 $2", ["2[24-9]|47|58|6[237-9]|9[35-9]"], "(0$1)"], [, "(\\d{3})(\\d{5})", "$1 $2", ["40[24]|50[45]"], "(0$1)"], [, "(48)(\\d{4})(\\d{4})", "$1 $2 $3", ["48"], "(0$1)"], [, "(818)(\\d{3})(\\d{3})", "$1 $2 $3", ["818"], "(0$1)"], [, "(\\d{2})(\\d{3})(\\d{3,4})", 
"$1 $2 $3", ["[24-69]|7[14]"], "(0$1)"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["76|8[35-9]"], "0$1"], [, "(8\\d)(\\d)(\\d{3})(\\d{4})", "$1 $2 $3 $4", ["8[35-9]5"], "0$1"], [, "(700)(\\d{3})(\\d{3})", "$1 $2 $3", ["700"], "0$1"], [, "(\\d{4})(\\d{3})(\\d{3})", "$1 $2 $3", ["1(?:5|8[059])", "1(?:5|8[059]0)"], "$1"]], , [, , , , , , , , , [-1]], , , [, , "18[59]0\\d{6}", , , , "1850123456", , , [10]], [, , "818\\d{6}", , , , "818123456", , , [9]], , , [, , "8[35-9]5\\d{7}", , , , "8551234567", 
, , [10]]], IL:[, [, , "1\\d{6,11}|[2-589]\\d{3}(?:\\d{3,6})?|6\\d{3}|7\\d{6,9}", , , , , , , [4, 7, 8, 9, 10, 11, 12]], [, , "(?:153\\d{1,2}|[2-489])\\d{7}", , , , "21234567", , , [8, 11, 12], [7]], [, , "5(?:[0-47-9]\\d{2}|5(?:01|2[23]|3[2-4]|4[45]|5[5689]|6[6-8]|7[0178]|8[6-9]|9[2-9])|6[2-9]\\d)\\d{5}", , , , "501234567", , , [9]], [, , "1(?:80[019]\\d{3}|255)\\d{3}", , , , "1800123456", , , [7, 10]], [, , "1(?:212|(?:9(?:0[01]|19)|200)\\d{2})\\d{4}", , , , "1919123456", , , [8, 9, 10]], [, , 
"1700\\d{6}", , , , "1700123456", , , [10]], [, , , , , , , , , [-1]], [, , "7(?:18\\d|2[23]\\d|3[237]\\d|47\\d|6[58]\\d|7\\d{2}|8(?:2\\d|33|55|77|81)|9[2579]\\d)\\d{5}", , , , "771234567", , , [9]], "IL", 972, "0(?:0|1[2-9])", "0", , , "0", , , , [[, "([2-489])(\\d{3})(\\d{4})", "$1-$2-$3", ["[2-489]"], "0$1"], [, "([57]\\d)(\\d{3})(\\d{4})", "$1-$2-$3", ["[57]"], "0$1"], [, "(153)(\\d{1,2})(\\d{3})(\\d{4})", "$1 $2 $3 $4", ["153"]], [, "(1)([7-9]\\d{2})(\\d{3})(\\d{3})", "$1-$2-$3-$4", ["1[7-9]"]], 
[, "(1255)(\\d{3})", "$1-$2", ["125", "1255"]], [, "(1200)(\\d{3})(\\d{3})", "$1-$2-$3", ["120", "1200"]], [, "(1212)(\\d{2})(\\d{2})", "$1-$2-$3", ["121", "1212"]], [, "(1599)(\\d{6})", "$1-$2", ["159", "1599"]], [, "(151)(\\d{1,2})(\\d{3})(\\d{4})", "$1-$2 $3-$4", ["151"]], [, "(\\d{4})", "*$1", ["[2-689]"]]], , [, , , , , , , , , [-1]], , , [, , "1700\\d{6}|[2-689]\\d{3}", , , , "1700123456", , , [4, 10]], [, , "[2-689]\\d{3}|1599\\d{6}", , , , "1599123456", , , [4, 10]], , , [, , "151\\d{8,9}", 
, , , "15112340000", , , [11, 12]]], IM:[, [, , "[135789]\\d{6,9}", , , , , , , [10], [6]], [, , "1624[5-8]\\d{5}", , , , "1624756789", , , , [6]], [, , "7(?:4576|[59]24\\d|624[0-4689])\\d{5}", , , , "7924123456"], [, , "808162\\d{4}", , , , "8081624567"], [, , "(?:872299|90[0167]624)\\d{4}", , , , "9016247890"], [, , "8(?:4(?:40[49]06|5624\\d)|70624\\d)\\d{3}", , , , "8456247890"], [, , "70\\d{8}", , , , "7012345678"], [, , "56\\d{8}", , , , "5612345678"], "IM", 44, "00", "0", , , "0", , , , , , 
[, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "3(?:08162\\d|3\\d{5}|4(?:40[49]06|5624\\d)|7(?:0624\\d|2299\\d))\\d{3}|55\\d{8}", , , , "5512345678"], , , [, , , , , , , , , [-1]]], IN:[, [, , "008\\d{9}|1\\d{7,12}|[2-9]\\d{9,10}", , , , , , , [8, 9, 10, 11, 12, 13], [6, 7]], [, , "(?:11|2[02]|33|4[04]|79|80)[2-7]\\d{7}|(?:1(?:2[0-249]|3[0-25]|4[145]|[59][14]|6[014]|7[1257]|8[01346])|2(?:1[257]|3[013]|4[01]|5[0137]|6[0158]|78|8[1568]|9[14])|3(?:26|4[1-3]|5[34]|6[01489]|7[02-46]|8[159])|4(?:1[36]|2[1-47]|3[15]|5[12]|6[0-26-9]|7[0-24-9]|8[013-57]|9[014-7])|5(?:1[025]|[36][25]|22|4[28]|5[12]|[78]1|9[15])|6(?:12|[2345]1|57|6[13]|7[14]|80)|7(?:12|2[14]|3[134]|4[47]|5[15]|[67]1|88)|8(?:16|2[014]|3[126]|6[136]|7[078]|8[34]|91))[2-7]\\d{6}|(?:(?:1(?:2[35-8]|3[346-9]|4[236-9]|[59][0235-9]|6[235-9]|7[34689]|8[257-9])|2(?:1[134689]|3[24-8]|4[2-8]|5[25689]|6[2-4679]|7[13-79]|8[2-479]|9[235-9])|3(?:01|1[79]|2[1-5]|4[25-8]|5[125689]|6[235-7]|7[157-9]|8[2-46-8])|4(?:1[14578]|2[5689]|3[2-467]|5[4-7]|6[35]|73|8[2689]|9[2389])|5(?:[16][146-9]|2[14-8]|3[1346]|4[14-69]|5[46]|7[2-4]|8[2-8]|9[246])|6(?:1[1358]|2[2457]|3[2-4]|4[235-7]|[57][2-689]|6[24-578]|8[1-6])|8(?:1[1357-9]|2[235-8]|3[03-57-9]|4[0-24-9]|5\\d|6[2457-9]|7[1-6]|8[1256]|9[2-4]))\\d|7(?:(?:1[013-9]|2[0235-9]|3[2679]|4[1-35689]|5[2-46-9]|[67][02-9]|9\\d)\\d|8(?:2[0-6]|[013-8]\\d)))[2-7]\\d{5}", 
, , , "1123456789", , , [10], [6, 7, 8]], [, , "(?:6(?:00[1-3]\\d|28[0-2]\\d|3(?:5(?:0[0-6]|1\\d)|[89]0\\d)|900\\d)|7(?:0\\d{3}|19[0-5]\\d|2(?:[0235679]\\d{2}|[14][017-9]\\d|8(?:[0-59]\\d|[678][089]))|3(?:[05-8]\\d{2}|1(?:[089]\\d|11|7[5-8])|2(?:[0-49][089]|[5-8]\\d)|3[017-9]\\d|4(?:[07-9]\\d|11)|9(?:[016-9]\\d|[2-5][089]))|4(?:0\\d{2}|1(?:[015-9]\\d|[23][089]|4[089])|2(?:0[089]|[1-7][089]|[89]\\d)|3(?:[0-8][089]|9\\d)|4(?:[089]\\d|11|7[02-8])|[56]\\d[089]|7(?:[089]\\d|11|7[02-8])|8(?:[0-24-7][089]|[389]\\d)|9(?:[0-6][089]|7[089]|[89]\\d))|5(?:[0346-8]\\d{2}|1(?:[07-9]\\d|11)|2(?:[04-9]\\d|[123][089])|5[017-9]\\d|9(?:[0-6][089]|[7-9]\\d))|6(?:0(?:[0-47]\\d|[5689][089])|(?:1[0-257-9]|[6-9]\\d)\\d|2(?:[0-4]\\d|[5-9][089])|3(?:[02-8][089]|[19]\\d)|4\\d[089]|5(?:[0-367][089]|[4589]\\d))|7(?:0(?:0[02-9]|[13-6][089]|[289]\\d|7[89])|[1-9]\\d{2})|8(?:[0-79]\\d{2}|8(?:[089]\\d|11|7[02-9]))|9(?:[089]\\d{2}|313|7(?:[02-8]\\d|9[07-9])))|8(?:0(?:[01589]\\d{2}|6[67]\\d|7(?:[02-8]\\d|9[05-9]))|1(?:[02-57-9]\\d{2}|1(?:[0-35-9]\\d|4[0-46-9])|6(?:[089]\\d|7[02-8]))|2(?:0(?:[089]\\d|7[02])|[14](?:[089]\\d|7[02-8])|[235-9]\\d{2})|3(?:[0357-9]\\d{2}|1(?:[089]\\d|7[02-6])|2(?:[09]\\d|77|8[0-689])|4\\d{2}|6(?:[089]\\d|7[02-7]))|[45]\\d{3}|6(?:[02457-9]\\d{2}|1(?:[089]\\d|7[02-8])|3(?:[089]\\d|7[02-8])|6(?:[08]\\d|7[02-8]|9\\d))|7(?:0[07-9]\\d|[1-69]\\d{2}|[78](?:[089]\\d|7[02-8]))|8(?:[0-25-9]\\d{2}|3(?:[089]\\d|7[02-8])|4(?:[0489]\\d|7[02-68]))|9(?:[02-9]\\d{2}|1(?:[0289]\\d|7[2-6])))|9\\d{4})\\d{5}", 
, , , "8123456789", , , [10]], [, , "00800\\d{7}|1(?:600\\d{6}|80(?:0\\d{4,9}|3\\d{9}))", , , , "1800123456"], [, , "186[12]\\d{9}", , , , "1861123456789", , , [13]], [, , "1860\\d{7}", , , , "18603451234", , , [11]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "IN", 91, "00", "0", , , "0", , , , [[, "(\\d{8})", "$1", ["561", "5616", "56161"], "$1", , 1], [, "(\\d{5})(\\d{5})", "$1 $2", ["6(?:[09]0|28|3[589])|7(?:[02-8]|19|9[037-9])|8(?:0[015-9]|[1-9])|9", "6(?:[09]0|28[0-2]|3(?:5[01]|[89]0))|7(?:[07]|19[0-5]|2(?:[0235-9]|[14][017-9])|3(?:[025-9]|[134][017-9])|4(?:[0-35689]|[47][017-9])|5(?:[02-46-9]|[15][017-9])|6(?:[02-9]|1[0-257-9])|8(?:[0-79]|8[0189])|9(?:[089]|31|7[02-9]))|8(?:0(?:[01589]|6[67]|7[02-9])|1(?:[0-57-9]|6[07-9])|2(?:[014][07-9]|[235-9])|3(?:[03-57-9]|[126][07-9])|[45]|6(?:[02457-9]|[136][07-9])|7(?:[078][07-9]|[1-69])|8(?:[0-25-9]|3[07-9]|4[047-9])|9(?:[02-9]|1[027-9]))|9", 
"6(?:[09]0|28[0-2]|3(?:5(?:0[0-6]|1)|[89]0))|7(?:0|19[0-5]|2(?:[0235-79]|[14][017-9]|8(?:[0-69]|[78][089]))|3(?:[05-8]|1(?:[0189]|7[5-9])|2(?:[0-49][089]|[5-8])|3[017-9]|4(?:[07-9]|11)|9(?:[01689]|[2-5][089]|7[0189]))|4(?:[056]|1(?:[0135-9]|[24][089])|[29](?:[0-7][089]|[89])|3(?:[0-8][089]|9)|[47](?:[089]|11|7[02-8])|8(?:[0-24-7][089]|[389]))|5(?:[0346-9]|[15][017-9]|2(?:[03-9]|[12][089]))|6(?:[0346-9]|1[0-257-9]|2(?:[0-4]|[5-9][089])|5(?:[0-367][089]|[4589]))|7(?:0(?:[02-9]|1[089])|[1-9])|8(?:[0-79]|8(?:0[0189]|11|8[013-9]|9))|9(?:[089]|313|7(?:[02-8]|9[07-9])))|8(?:0(?:[01589]|6[67]|7(?:[02-8]|9[05-9]))|1(?:[02-57-9]|1(?:[0-35-9]|4[0-46-9])|6(?:[089]|7[02-8]))|2(?:0(?:[089]|7[02])|[14](?:[089]|7[02-8])|[235-9])|3(?:[03-57-9]|1(?:[089]|7[02-6])|2(?:[09]|77|8[0-689])|6(?:[089]|7[02-7]))|[45]|6(?:[02457-9]|[136](?:[089]|7[02-8]))|7(?:0[07-9]|[1-69]|[78](?:[089]|7[02-8]))|8(?:[0-25-9]|3(?:[089]|7[02-8])|4(?:[0489]|7[02-68]))|9(?:[02-9]|1(?:[0289]|7[2-6])))|9"], 
"0$1", , 1], [, "(\\d{2})(\\d{4})(\\d{4})", "$1 $2 $3", ["11|2[02]|33|4[04]|79[1-9]|80[2-46]"], "0$1", , 1], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["1(?:2[0-249]|3[0-25]|4[145]|[59][14]|[68][1-9]|7[1257])|2(?:1[257]|3[013]|4[01]|5[0137]|6[0158]|78|8[1568]|9[14])|3(?:26|4[1-3]|5[34]|6[01489]|7[02-46]|8[159])|4(?:1[36]|2[1-47]|3[15]|5[12]|6[0-26-9]|7[0-24-9]|8[013-57]|9[014-7])|5(?:1[025]|22|[36][25]|4[28]|5[12]|[78]1|9[15])|6(?:12|[2-4]1|5[17]|6[13]|7[14]|80)|7(?:12|2[14]|3[134]|4[47]|5[15]|[67]1|88)|8(?:16|2[014]|3[126]|6[136]|7[078]|8[34]|91)"], 
"0$1", , 1], [, "(\\d{4})(\\d{3})(\\d{3})", "$1 $2 $3", ["1(?:[23579]|[468][1-9])|[2-8]"], "0$1", , 1], [, "(\\d{2})(\\d{3})(\\d{4})(\\d{3})", "$1 $2 $3 $4", ["008"], "0$1", , 1], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["140"], "$1", , 1], [, "(\\d{4})(\\d{2})(\\d{4})", "$1 $2 $3", ["160", "1600"], "$1", , 1], [, "(\\d{4})(\\d{4,5})", "$1 $2", ["180", "1800"], "$1", , 1], [, "(\\d{4})(\\d{2,4})(\\d{4})", "$1 $2 $3", ["180", "1800"], "$1", , 1], [, "(\\d{4})(\\d{3,4})(\\d{4})", "$1 $2 $3", ["186", 
"1860"], "$1", , 1], [, "(\\d{4})(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3 $4", ["18[06]"], "$1", , 1]], , [, , , , , , , , , [-1]], , , [, , "00800\\d{7}|1(?:600\\d{6}|8(?:0(?:0\\d{4,9}|3\\d{9})|6(?:0\\d{7}|[12]\\d{9})))", , , , "1800123456"], [, , "140\\d{7}", , , , "1409305260", , , [10]], , , [, , , , , , , , , [-1]]], IO:[, [, , "3\\d{6}", , , , , , , [7]], [, , "37\\d{5}", , , , "3709100"], [, , "38\\d{5}", , , , "3801234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , 
[-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "IO", 246, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], IQ:[, [, , "[1-7]\\d{7,9}", , , , , , , [8, 9, 10], [6, 7]], [, , "1\\d{7}|(?:2[13-5]|3[02367]|4[023]|5[03]|6[026])\\d{6,7}", , , , "12345678", , , [8, 9], [6, 7]], [, , "7[3-9]\\d{8}", , , , "7912345678", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , 
[-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "IQ", 964, "00", "0", , , "0", , , , [[, "(1)(\\d{3})(\\d{4})", "$1 $2 $3", ["1"], "0$1"], [, "([2-6]\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["[2-6]"], "0$1"], [, "(7\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["7"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], IR:[, [, , "[1-8]\\d{5,9}|9(?:[0-4]\\d{8}|9\\d{8})", , , , , , , [6, 7, 10], [4, 5, 
8]], [, , "(?:(?:1[137]|2[13-68]|3[1458]|4[145]|5[1468]|6[16]|7[1467]|8[13467])(?:\\d{8}|(?:[16]|[289]\\d?)\\d{3}))|94(?:000\\d|11[0-7]\\d|2\\d{3}|3016|440\\d)\\d{4}", , , , "2123456789", , , , [4, 5, 8]], [, , "9(?:0(?:[1-3]\\d{2}|44\\d)|[13]\\d{3}|2[0-2]\\d{2}|9(?:0\\d{2}|44\\d|810|9(?:00|1[13]|9[89])))\\d{5}", , , , "9123456789", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "(?:[2-6]0\\d|993)\\d{7}", , , , "9932123456", 
, , [10]], "IR", 98, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{4})(\\d{4})", "$1 $2 $3", ["[1-8]"], "0$1"], [, "(\\d{2})(\\d{4,5})", "$1 $2", ["[1-8]"], "0$1"], [, "(\\d{4,5})", "$1", ["96"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["9"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , "(?:9411[1-7]|94440)\\d{5}", , , , "9411110000", , , [10]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], IS:[, [, , "[4-9]\\d{6}|38\\d{7}", , , , , , , [7, 9]], [, , "(?:4(?:1[0-24-69]|2[0-7]|[37][0-8]|4[0-245]|5[0-68]|6\\d|8[0-36-8])|5(?:05|[156]\\d|2[02578]|3[0-79]|4[03-7]|7[0-2578]|8[0-35-9]|9[013-689])|87[23])\\d{4}", 
, , , "4101234", , , [7]], [, , "38[589]\\d{6}|(?:6(?:1[1-8]|2[0-6]|3[027-9]|4[014679]|5[0159]|[67][0-69]|9\\d)|7(?:5[057]|[6-8]\\d)|8(?:2[0-59]|3[0-4]|[469]\\d|5[1-9]|88))\\d{4}", , , , "6111234"], [, , "800\\d{4}", , , , "8001234", , , [7]], [, , "90\\d{5}", , , , "9011234", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "49\\d{5}", , , , "4921234", , , [7]], "IS", 354, "1(?:0(?:01|10|20)|100)|00", , , , , , "00", , [[, "(\\d{3})(\\d{4})", "$1 $2", ["[4-9]"]], [, "(3\\d{2})(\\d{3})(\\d{3})", 
"$1 $2 $3", ["3"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "809\\d{4}", , , , "8091234", , , [7]], , , [, , "(?:6(?:2[78]|8\\d)|8(?:7[0189]|80)|95[48])\\d{4}", , , , "6271234", , , [7]]], IT:[, [, , "[01589]\\d{5,10}|3(?:[12457-9]\\d{8}|[36]\\d{7,9})", , , , , , , [6, 7, 8, 9, 10, 11]], [, , "0(?:[26]\\d{4,9}|(?:1(?:[0159]\\d|[27][1-5]|31|4[1-4]|6[1356]|8[2-57])|3(?:[0159]\\d|2[1-4]|3[12]|[48][1-6]|6[2-59]|7[1-7])|4(?:[0159]\\d|[23][1-9]|4[245]|6[1-5]|7[1-4]|81)|5(?:[0159]\\d|2[1-5]|3[2-6]|4[1-79]|6[4-6]|7[1-578]|8[3-8])|7(?:[0159]\\d|2[12]|3[1-7]|4[2346]|6[13569]|7[13-6]|8[1-59])|8(?:[0159]\\d|2[34578]|3[1-356]|[6-8][1-5])|9(?:[0159]\\d|[238][1-5]|4[12]|6[1-8]|7[1-6]))\\d{2,7})", 
, , , "0212345678"], [, , "3(?:[12457-9]\\d{8}|6\\d{7,8}|3\\d{7,9})", , , , "3123456789", , , [9, 10, 11]], [, , "80(?:0\\d{6}|3\\d{3})", , , , "800123456", , , [6, 9]], [, , "0878\\d{5}|1(?:44|6[346])\\d{6}|89(?:2\\d{3}|4(?:[0-4]\\d{2}|[5-9]\\d{4})|5(?:[0-4]\\d{2}|[5-9]\\d{6})|9\\d{6})", , , , "899123456", , , [6, 8, 9, 10]], [, , "84(?:[08]\\d{6}|[17]\\d{3})", , , , "848123456", , , [6, 9]], [, , "1(?:78\\d|99)\\d{6}", , , , "1781234567", , , [9, 10]], [, , "55\\d{8}", , , , "5512345678", , , [10]], 
"IT", 39, "00", , , , , , , , [[, "(\\d{2})(\\d{3,4})(\\d{4})", "$1 $2 $3", ["0[26]|55"]], [, "(0[26])(\\d{4})(\\d{5})", "$1 $2 $3", ["0[26]"]], [, "(0[26])(\\d{4,6})", "$1 $2", ["0[26]"]], [, "(0\\d{2})(\\d{3,4})(\\d{4})", "$1 $2 $3", ["0[13-57-9][0159]"]], [, "(\\d{3})(\\d{3,6})", "$1 $2", ["0[13-57-9][0159]|8(?:03|4[17]|9[245])", "0[13-57-9][0159]|8(?:03|4[17]|9(?:2|[45][0-4]))"]], [, "(0\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["0[13-57-9][2-46-8]"]], [, "(0\\d{3})(\\d{2,6})", "$1 $2", ["0[13-57-9][2-46-8]"]], 
[, "(\\d{3})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["[13]|8(?:00|4[08]|9[59])", "[13]|8(?:00|4[08]|9(?:5[5-9]|9))"]], [, "(\\d{4})(\\d{4})", "$1 $2", ["894", "894[5-9]"]], [, "(\\d{3})(\\d{4})(\\d{4})", "$1 $2 $3", ["3"]]], , [, , , , , , , , , [-1]], 1, , [, , "848\\d{6}", , , , "848123456", , , [9]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], JE:[, [, , "[135789]\\d{6,9}", , , , , , , [10], [6]], [, , "1534[0-24-8]\\d{5}", , , , "1534456789", , , , [6]], [, , "7(?:509\\d|7(?:00[378]|97[7-9])|829\\d|937\\d)\\d{5}", 
, , , "7797712345"], [, , "80(?:07(?:35|81)|8901)\\d{4}", , , , "8007354567"], [, , "(?:871206|90(?:066[59]|1810|71(?:07|55)))\\d{4}", , , , "9018105678"], [, , "8(?:4(?:4(?:4(?:05|42|69)|703)|5(?:041|800))|70002)\\d{4}", , , , "8447034567"], [, , "701511\\d{4}", , , , "7015115678"], [, , "56\\d{8}", , , , "5612345678"], "JE", 44, "00", "0", , , "0", , , , , , [, , "76(?:0[012]|2[356]|4[0134]|5[49]|6[0-369]|77|81|9[39])\\d{6}", , , , "7640123456"], , , [, , , , , , , , , [-1]], [, , "3(?:0(?:07(?:35|81)|8901)|3\\d{4}|4(?:4(?:4(?:05|42|69)|703)|5(?:041|800))|7(?:0002|1206))\\d{4}|55\\d{8}", 
, , , "5512345678"], , , [, , , , , , , , , [-1]]], JM:[, [, , "[589]\\d{9}", , , , , , , [10], [7]], [, , "876(?:5(?:0[12]|1[0-468]|2[35]|63)|6(?:0[1-3579]|1[027-9]|[23]\\d|40|5[06]|6[2-589]|7[05]|8[04]|9[4-9])|7(?:0[2-689]|[1-6]\\d|8[056]|9[45])|9(?:0[1-8]|1[02378]|[2-8]\\d|9[2-468]))\\d{4}", , , , "8765123456", , , , [7]], [, , "876(?:2[14-9]\\d|[348]\\d{2}|5(?:0[3-9]|[2-57-9]\\d|6[0-24-9])|7(?:0[07]|7\\d|8[1-47-9]|9[0-36-9])|9(?:[01]9|9[0579]))\\d{4}", , , , "8762101234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", 
, , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "JM", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "876", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], JO:[, [, , "[235-9]\\d{7,8}", , , , , , , [8, 9]], [, , "(?:2(?:6(?:2[0-35-9]|3[0-57-8]|4[24-7]|5[0-24-8]|[6-8][023]|9[0-3])|7(?:0[1-79]|10|2[014-7]|3[0-689]|4[019]|5[0-3578]))|32(?:0[1-69]|1[1-35-7]|2[024-7]|3\\d|4[0-3]|[57][023]|6[03])|53(?:0[0-3]|[13][023]|2[0-59]|49|5[0-35-9]|6[15]|7[45]|8[1-6]|9[0-36-9])|6(?:2[50]0|3(?:00|33)|4(?:0[0125]|1[2-7]|2[0569]|[38][07-9]|4[025689]|6[0-589]|7\\d|9[0-2])|5(?:[01][056]|2[034]|3[0-57-9]|4[17-8]|5[0-69]|6[0-35-9]|7[1-379]|8[0-68]|9[02-39]))|87(?:[02]0|7[08]|90))\\d{4}", 
, , , "62001234", , , [8]], [, , "7(?:55[0-49]|7[025-9]\\d|8[0-25-9]\\d|9[0-25-9]\\d)\\d{5}", , , , "790123456", , , [9]], [, , "80\\d{6}", , , , "80012345", , , [8]], [, , "900\\d{5}", , , , "90012345", , , [8]], [, , "85\\d{6}", , , , "85012345", , , [8]], [, , "70\\d{7}", , , , "700123456", , , [9]], [, , , , , , , , , [-1]], "JO", 962, "00", "0", , , "0", , , , [[, "(\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["[2356]|87"], "(0$1)"], [, "(7)(\\d{4})(\\d{4})", "$1 $2 $3", ["7[457-9]"], "0$1"], [, "(\\d{2})(\\d{7})", 
"$1 $2", ["70"], "0$1"], [, "(\\d{3})(\\d{5,6})", "$1 $2", ["8[0158]|9"], "0$1"]], , [, , "74(?:66|77)\\d{5}", , , , "746612345", , , [9]], , , [, , , , , , , , , [-1]], [, , "8(?:10|8\\d)\\d{5}", , , , "88101234", , , [8]], , , [, , , , , , , , , [-1]]], JP:[, [, , "[1-9]\\d{8,9}|00(?:[36]\\d{7,14}|7\\d{5,7}|8\\d{7})", , , , , , , [8, 9, 10, 11, 12, 13, 14, 15, 16, 17]], [, , "(?:1(?:1[235-8]|2[3-6]|3[3-9]|4[2-6]|[58][2-8]|6[2-7]|7[2-9]|9[1-9])|2[2-9]\\d|[36][1-9]\\d|4(?:6[02-8]|[2-578]\\d|9[2-59])|5(?:6[1-9]|7[2-8]|[2-589]\\d)|7(?:3[4-9]|4[02-9]|[25-9]\\d)|8(?:3[2-9]|4[5-9]|5[1-9]|8[03-9]|[2679]\\d)|9(?:[679][1-9]|[2-58]\\d))\\d{6}", 
, , , "312345678", , , [9]], [, , "[7-9]0[1-9]\\d{7}", , , , "9012345678", , , [10]], [, , "120\\d{6}|800\\d{7}|00(?:37\\d{6,13}|66\\d{6,13}|777(?:[01]\\d{2}|5\\d{3}|8\\d{4})|882[1245]\\d{4})", , , , "120123456"], [, , "990\\d{6}", , , , "990123456", , , [9]], [, , , , , , , , , [-1]], [, , "60\\d{7}", , , , "601234567", , , [9]], [, , "50[1-9]\\d{7}", , , , "5012345678", , , [10]], "JP", 81, "010", "0", , , "0", , , , [[, "(\\d{3})(\\d{3})(\\d{3})", "$1-$2-$3", ["(?:12|57|99)0"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{4})", 
"$1-$2-$3", ["800"], "0$1"], [, "(\\d{4})(\\d{4})", "$1-$2", ["007", "0077"], "$1"], [, "(\\d{4})(\\d{2})(\\d{3,4})", "$1-$2-$3", ["007", "0077"], "$1"], [, "(\\d{4})(\\d{2})(\\d{4})", "$1-$2-$3", ["008", "0088"], "$1"], [, "(\\d{4})(\\d{3})(\\d{3,4})", "$1-$2-$3", ["00[36]", "00(?:37|66)"], "$1"], [, "(\\d{4})(\\d{4})(\\d{4,5})", "$1-$2-$3", ["00[36]", "00(?:37|66)"], "$1"], [, "(\\d{4})(\\d{5})(\\d{5,6})", "$1-$2-$3", ["00[36]", "00(?:37|66)"], "$1"], [, "(\\d{4})(\\d{6})(\\d{6,7})", "$1-$2-$3", 
["00[36]", "00(?:37|66)"], "$1"], [, "(\\d{2})(\\d{4})(\\d{4})", "$1-$2-$3", ["[2579]0|80[1-9]"], "0$1"], [, "(\\d{4})(\\d)(\\d{4})", "$1-$2-$3", ["1(?:26|3[79]|4[56]|5[4-68]|6[3-5])|499|5(?:76|97)|746|8(?:3[89]|47|51|63)|9(?:49|80|9[16])", "1(?:267|3(?:7[247]|9[278])|4(?:5[67]|66)|5(?:47|58|64|8[67])|6(?:3[245]|48|5[4-68]))|499[2468]|5(?:76|97)9|7468|8(?:3(?:8[78]|96)|477|51[24]|636)|9(?:496|802|9(?:1[23]|69))", "1(?:267|3(?:7[247]|9[278])|4(?:5[67]|66)|5(?:47|58|64|8[67])|6(?:3[245]|48|5[4-68]))|499[2468]|5(?:769|979[2-69])|7468|8(?:3(?:8[78]|96[2457-9])|477|51[24]|636[2-57-9])|9(?:496|802|9(?:1[23]|69))"], 
"0$1"], [, "(\\d{3})(\\d{2})(\\d{4})", "$1-$2-$3", ["1(?:2[3-6]|3[3-9]|4[2-6]|5[2-8]|[68][2-7]|7[2-689]|9[1-578])|2(?:2[03-689]|3[3-58]|4[0-468]|5[04-8]|6[013-8]|7[06-9]|8[02-57-9]|9[13])|4(?:2[28]|3[689]|6[035-7]|7[05689]|80|9[3-5])|5(?:3[1-36-9]|4[4578]|5[013-8]|6[1-9]|7[2-8]|8[14-7]|9[4-9])|7(?:2[15]|3[5-9]|4[02-9]|6[135-8]|7[0-4689]|9[014-9])|8(?:2[49]|3[3-8]|4[5-8]|5[2-9]|6[35-9]|7[579]|8[03-579]|9[2-8])|9(?:[23]0|4[02-46-9]|5[024-79]|6[4-9]|7[2-47-9]|8[02-7]|9[3-7])", "1(?:2[3-6]|3[3-9]|4[2-6]|5(?:[236-8]|[45][2-69])|[68][2-7]|7[2-689]|9[1-578])|2(?:2(?:[04-689]|3[23])|3[3-58]|4[0-468]|5(?:[0468][2-9]|5[78]|7[2-4])|6(?:[0135-8]|4[2-5])|7(?:[0679]|8[2-7])|8(?:[024578]|3[25-9]|9[6-9])|9(?:11|3[2-4]))|4(?:2(?:2[2-9]|8[237-9])|3[689]|6[035-7]|7(?:[059][2-8]|[68])|80|9[3-5])|5(?:3[1-36-9]|4[4578]|5[013-8]|6[1-9]|7[2-8]|8[14-7]|9(?:[4-7]|[89][2-8]))|7(?:2[15]|3[5-9]|4[02-9]|6[135-8]|7[0-4689]|9(?:[017-9]|4[6-8]|5[2-478]|6[2-589]))|8(?:2(?:4[4-8]|9[2-8])|3(?:[3-6][2-9]|7[2-6]|8[2-5])|4[5-8]|5[2-9]|6(?:[37]|5[4-7]|6[2-9]|8[2-8]|9[236-9])|7[579]|8[03-579]|9[2-8])|9(?:[23]0|4[02-46-9]|5[024-79]|6[4-9]|7[2-47-9]|8[02-7]|9(?:3[34]|4[2-69]|[5-7]))", 
"1(?:2[3-6]|3[3-9]|4[2-6]|5(?:[236-8]|[45][2-69])|[68][2-7]|7[2-689]|9[1-578])|2(?:2(?:[04-689]|3[23])|3[3-58]|4[0-468]|5(?:[0468][2-9]|5[78]|7[2-4])|6(?:[0135-8]|4[2-5])|7(?:[0679]|8[2-7])|8(?:[024578]|3[25-9]|9[6-9])|9(?:11|3[2-4]))|4(?:2(?:2[2-9]|8[237-9])|3[689]|6[035-7]|7(?:[059][2-8]|[68])|80|9[3-5])|5(?:3[1-36-9]|4[4578]|5[013-8]|6[1-9]|7[2-8]|8[14-7]|9(?:[4-7]|[89][2-8]))|7(?:2[15]|3[5-9]|4[02-9]|6[135-8]|7[0-4689]|9(?:[017-9]|4[6-8]|5[2-478]|6[2-589]))|8(?:2(?:4[4-8]|9(?:20|[3578]|4[04-9]|6[56]))|3(?:[3-6][2-9]|7(?:[2-5]|6[0-59])|8[2-5])|4[5-8]|5[2-9]|6(?:[37]|5(?:[467]|5[014-9])|6(?:[2-8]|9[02-69])|8[2-8]|9(?:[236-8]|9[23]))|7[579]|8[03-579]|9[2-8])|9(?:[23]0|4[02-46-9]|5[024-79]|6[4-9]|7[2-47-9]|8[02-7]|9(?:3(?:3[02-9]|4[0-24689])|4[2-69]|[5-7]))", 
"1(?:2[3-6]|3[3-9]|4[2-6]|5(?:[236-8]|[45][2-69])|[68][2-7]|7[2-689]|9[1-578])|2(?:2(?:[04-689]|3[23])|3[3-58]|4[0-468]|5(?:[0468][2-9]|5[78]|7[2-4])|6(?:[0135-8]|4[2-5])|7(?:[0679]|8[2-7])|8(?:[024578]|3[25-9]|9[6-9])|9(?:11|3[2-4]))|4(?:2(?:2[2-9]|8[237-9])|3[689]|6[035-7]|7(?:[059][2-8]|[68])|80|9[3-5])|5(?:3[1-36-9]|4[4578]|5[013-8]|6[1-9]|7[2-8]|8[14-7]|9(?:[4-7]|[89][2-8]))|7(?:2[15]|3[5-9]|4[02-9]|6[135-8]|7[0-4689]|9(?:[017-9]|4[6-8]|5[2-478]|6[2-589]))|8(?:2(?:4[4-8]|9(?:20|[3578]|4[04-9]|6(?:5[25]|60)))|3(?:[3-6][2-9]|7(?:[2-5]|6[0-59])|8[2-5])|4[5-8]|5[2-9]|6(?:[37]|5(?:[467]|5[014-9])|6(?:[2-8]|9[02-69])|8[2-8]|9(?:[236-8]|9[23]))|7[579]|8[03-579]|9[2-8])|9(?:[23]0|4[02-46-9]|5[024-79]|6[4-9]|7[2-47-9]|8[02-7]|9(?:3(?:3[02-9]|4[0-24689])|4[2-69]|[5-7]))"], 
"0$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1-$2-$3", ["1|2(?:2[37]|5[5-9]|64|78|8[39]|91)|4(?:2[2689]|64|7[347])|5[2-589]|60|8(?:2[124589]|3[279]|[46-9])|9(?:[235-8]|93)", "1|2(?:2[37]|5(?:[57]|[68]0|9[19])|64|78|8[39]|917)|4(?:2(?:20|[68]|9[178])|64|7[347])|5[2-589]|60|8(?:2[124589]|3[279]|[46-9])|9(?:[235-8]|93[34])", "1|2(?:2[37]|5(?:[57]|[68]0|9(?:17|99))|64|78|8[39]|917)|4(?:2(?:20|[68]|9[178])|64|7[347])|5[2-589]|60|8(?:2[124589]|3[279]|[46-9])|9(?:[235-8]|93(?:31|4))"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{4})", 
"$1-$2-$3", ["2(?:[34]7|[56]9|74|9[14-79])|82|993"], "0$1"], [, "(\\d)(\\d{4})(\\d{4})", "$1-$2-$3", ["3|4(?:2[09]|7[01])|6[1-9]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1-$2-$3", ["[2479][1-9]"], "0$1"]], [[, "(\\d{3})(\\d{3})(\\d{3})", "$1-$2-$3", ["(?:12|57|99)0"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{4})", "$1-$2-$3", ["800"], "0$1"], [, "(\\d{2})(\\d{4})(\\d{4})", "$1-$2-$3", ["[2579]0|80[1-9]"], "0$1"], [, "(\\d{4})(\\d)(\\d{4})", "$1-$2-$3", ["1(?:26|3[79]|4[56]|5[4-68]|6[3-5])|499|5(?:76|97)|746|8(?:3[89]|47|51|63)|9(?:49|80|9[16])", 
"1(?:267|3(?:7[247]|9[278])|4(?:5[67]|66)|5(?:47|58|64|8[67])|6(?:3[245]|48|5[4-68]))|499[2468]|5(?:76|97)9|7468|8(?:3(?:8[78]|96)|477|51[24]|636)|9(?:496|802|9(?:1[23]|69))", "1(?:267|3(?:7[247]|9[278])|4(?:5[67]|66)|5(?:47|58|64|8[67])|6(?:3[245]|48|5[4-68]))|499[2468]|5(?:769|979[2-69])|7468|8(?:3(?:8[78]|96[2457-9])|477|51[24]|636[2-57-9])|9(?:496|802|9(?:1[23]|69))"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{4})", "$1-$2-$3", ["1(?:2[3-6]|3[3-9]|4[2-6]|5[2-8]|[68][2-7]|7[2-689]|9[1-578])|2(?:2[03-689]|3[3-58]|4[0-468]|5[04-8]|6[013-8]|7[06-9]|8[02-57-9]|9[13])|4(?:2[28]|3[689]|6[035-7]|7[05689]|80|9[3-5])|5(?:3[1-36-9]|4[4578]|5[013-8]|6[1-9]|7[2-8]|8[14-7]|9[4-9])|7(?:2[15]|3[5-9]|4[02-9]|6[135-8]|7[0-4689]|9[014-9])|8(?:2[49]|3[3-8]|4[5-8]|5[2-9]|6[35-9]|7[579]|8[03-579]|9[2-8])|9(?:[23]0|4[02-46-9]|5[024-79]|6[4-9]|7[2-47-9]|8[02-7]|9[3-7])", 
"1(?:2[3-6]|3[3-9]|4[2-6]|5(?:[236-8]|[45][2-69])|[68][2-7]|7[2-689]|9[1-578])|2(?:2(?:[04-689]|3[23])|3[3-58]|4[0-468]|5(?:[0468][2-9]|5[78]|7[2-4])|6(?:[0135-8]|4[2-5])|7(?:[0679]|8[2-7])|8(?:[024578]|3[25-9]|9[6-9])|9(?:11|3[2-4]))|4(?:2(?:2[2-9]|8[237-9])|3[689]|6[035-7]|7(?:[059][2-8]|[68])|80|9[3-5])|5(?:3[1-36-9]|4[4578]|5[013-8]|6[1-9]|7[2-8]|8[14-7]|9(?:[4-7]|[89][2-8]))|7(?:2[15]|3[5-9]|4[02-9]|6[135-8]|7[0-4689]|9(?:[017-9]|4[6-8]|5[2-478]|6[2-589]))|8(?:2(?:4[4-8]|9[2-8])|3(?:[3-6][2-9]|7[2-6]|8[2-5])|4[5-8]|5[2-9]|6(?:[37]|5[4-7]|6[2-9]|8[2-8]|9[236-9])|7[579]|8[03-579]|9[2-8])|9(?:[23]0|4[02-46-9]|5[024-79]|6[4-9]|7[2-47-9]|8[02-7]|9(?:3[34]|4[2-69]|[5-7]))", 
"1(?:2[3-6]|3[3-9]|4[2-6]|5(?:[236-8]|[45][2-69])|[68][2-7]|7[2-689]|9[1-578])|2(?:2(?:[04-689]|3[23])|3[3-58]|4[0-468]|5(?:[0468][2-9]|5[78]|7[2-4])|6(?:[0135-8]|4[2-5])|7(?:[0679]|8[2-7])|8(?:[024578]|3[25-9]|9[6-9])|9(?:11|3[2-4]))|4(?:2(?:2[2-9]|8[237-9])|3[689]|6[035-7]|7(?:[059][2-8]|[68])|80|9[3-5])|5(?:3[1-36-9]|4[4578]|5[013-8]|6[1-9]|7[2-8]|8[14-7]|9(?:[4-7]|[89][2-8]))|7(?:2[15]|3[5-9]|4[02-9]|6[135-8]|7[0-4689]|9(?:[017-9]|4[6-8]|5[2-478]|6[2-589]))|8(?:2(?:4[4-8]|9(?:20|[3578]|4[04-9]|6[56]))|3(?:[3-6][2-9]|7(?:[2-5]|6[0-59])|8[2-5])|4[5-8]|5[2-9]|6(?:[37]|5(?:[467]|5[014-9])|6(?:[2-8]|9[02-69])|8[2-8]|9(?:[236-8]|9[23]))|7[579]|8[03-579]|9[2-8])|9(?:[23]0|4[02-46-9]|5[024-79]|6[4-9]|7[2-47-9]|8[02-7]|9(?:3(?:3[02-9]|4[0-24689])|4[2-69]|[5-7]))", 
"1(?:2[3-6]|3[3-9]|4[2-6]|5(?:[236-8]|[45][2-69])|[68][2-7]|7[2-689]|9[1-578])|2(?:2(?:[04-689]|3[23])|3[3-58]|4[0-468]|5(?:[0468][2-9]|5[78]|7[2-4])|6(?:[0135-8]|4[2-5])|7(?:[0679]|8[2-7])|8(?:[024578]|3[25-9]|9[6-9])|9(?:11|3[2-4]))|4(?:2(?:2[2-9]|8[237-9])|3[689]|6[035-7]|7(?:[059][2-8]|[68])|80|9[3-5])|5(?:3[1-36-9]|4[4578]|5[013-8]|6[1-9]|7[2-8]|8[14-7]|9(?:[4-7]|[89][2-8]))|7(?:2[15]|3[5-9]|4[02-9]|6[135-8]|7[0-4689]|9(?:[017-9]|4[6-8]|5[2-478]|6[2-589]))|8(?:2(?:4[4-8]|9(?:20|[3578]|4[04-9]|6(?:5[25]|60)))|3(?:[3-6][2-9]|7(?:[2-5]|6[0-59])|8[2-5])|4[5-8]|5[2-9]|6(?:[37]|5(?:[467]|5[014-9])|6(?:[2-8]|9[02-69])|8[2-8]|9(?:[236-8]|9[23]))|7[579]|8[03-579]|9[2-8])|9(?:[23]0|4[02-46-9]|5[024-79]|6[4-9]|7[2-47-9]|8[02-7]|9(?:3(?:3[02-9]|4[0-24689])|4[2-69]|[5-7]))"], 
"0$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1-$2-$3", ["1|2(?:2[37]|5[5-9]|64|78|8[39]|91)|4(?:2[2689]|64|7[347])|5[2-589]|60|8(?:2[124589]|3[279]|[46-9])|9(?:[235-8]|93)", "1|2(?:2[37]|5(?:[57]|[68]0|9[19])|64|78|8[39]|917)|4(?:2(?:20|[68]|9[178])|64|7[347])|5[2-589]|60|8(?:2[124589]|3[279]|[46-9])|9(?:[235-8]|93[34])", "1|2(?:2[37]|5(?:[57]|[68]0|9(?:17|99))|64|78|8[39]|917)|4(?:2(?:20|[68]|9[178])|64|7[347])|5[2-589]|60|8(?:2[124589]|3[279]|[46-9])|9(?:[235-8]|93(?:31|4))"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{4})", 
"$1-$2-$3", ["2(?:[34]7|[56]9|74|9[14-79])|82|993"], "0$1"], [, "(\\d)(\\d{4})(\\d{4})", "$1-$2-$3", ["3|4(?:2[09]|7[01])|6[1-9]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1-$2-$3", ["[2479][1-9]"], "0$1"]], [, , "20\\d{8}", , , , "2012345678", , , [10]], , , [, , "00(?:37\\d{6,13}|66\\d{6,13}|777(?:[01]\\d{2}|5\\d{3}|8\\d{4})|882[1245]\\d{4})", , , , "00777012"], [, , "570\\d{6}", , , , "570123456", , , [9]], , , [, , , , , , , , , [-1]]], KE:[, [, , "20\\d{6,7}|[4-9]\\d{6,9}", , , , , , , [7, 
8, 9, 10]], [, , "20\\d{6,7}|4(?:0\\d{6,7}|[136]\\d{7}|[245]\\d{5,7})|5(?:[08]\\d{7}|[1-79]\\d{5,7})|6(?:[01457-9]\\d{5,7}|2\\d{7}|6\\d{6,7})", , , , "202012345", , , [7, 8, 9]], [, , "7(?:[0-3679]\\d|4[0-46-9]|5[0-6]|8[0-25-9])\\d{6}", , , , "712123456", , , [9]], [, , "800[24-8]\\d{5,6}", , , , "800223456", , , [9, 10]], [, , "900[02-9]\\d{5}", , , , "900223456", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "KE", 254, "000", "0", , , "005|0", , , , [[, 
"(\\d{2})(\\d{5,7})", "$1 $2", ["[24-6]"], "0$1"], [, "(\\d{3})(\\d{6})", "$1 $2", ["7"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["[89]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], KG:[, [, , "[235-8]\\d{8,9}", , , , , , , [9, 10], [5, 6]], [, , "(?:3(?:1(?:[256]\\d|3[1-9]|47)|2(?:22|3[0-479]|6[0-7])|4(?:22|5[6-9]|6\\d)|5(?:22|3[4-7]|59|6\\d)|6(?:22|5[35-7]|6\\d)|7(?:22|3[468]|4[1-9]|59|[67]\\d)|9(?:22|4[1-8]|6\\d))|6(?:09|12|2[2-4])\\d)\\d{5}", 
, , , "312123456", , , [9], [5, 6]], [, , "(?:20[0-35]|5[0-24-7]\\d|7[07]\\d)\\d{6}", , , , "700123456", , , [9]], [, , "800\\d{6,7}", , , , "800123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "KG", 996, "00", "0", , , "0", , , , [[, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["[25-7]|31[25]"], "0$1"], [, "(\\d{4})(\\d{5})", "$1 $2", ["3(?:1[36]|[2-9])"], "0$1"], [, "(\\d{3})(\\d{3})(\\d)(\\d{3})", "$1 $2 $3 $4", ["8"], "0$1"]], , [, 
, , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], KH:[, [, , "[1-9]\\d{7,9}", , , , , , , [8, 9, 10], [6, 7]], [, , "(?:2[3-6]|3[2-6]|4[2-4]|[5-7][2-5])(?:[237-9]|4[56]|5\\d|6\\d?)\\d{5}|23(?:4[234]|8\\d{2})\\d{4}", , , , "23756789", , , [8, 9], [6, 7]], [, , "(?:1(?:[013-79]\\d|[28]\\d{1,2})|2[3-6]48|3(?:[18]\\d{2}|[2-6]48)|4[2-4]48|5[2-5]48|6(?:[016-9]\\d|[2-5]48)|7(?:[07-9]\\d|[16]\\d{2}|[2-5]48)|8(?:[013-79]\\d|8\\d{2})|9(?:6\\d{2}|7\\d{1,2}|[0-589]\\d))\\d{5}", 
, , , "91234567", , , [8, 9]], [, , "1800(?:1\\d|2[019])\\d{4}", , , , "1800123456", , , [10]], [, , "1900(?:1\\d|2[09])\\d{4}", , , , "1900123456", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "KH", 855, "00[14-9]", "0", , , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["1\\d[1-9]|[2-9]"], "0$1"], [, "(1[89]00)(\\d{3})(\\d{3})", "$1 $2 $3", ["1[89]0", "1[89]00"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , 
[-1]], , , [, , , , , , , , , [-1]]], KI:[, [, , "[2458]\\d{4}|3\\d{4,7}|[67]\\d{7}", , , , , , , [5, 8]], [, , "(?:[24]\\d|3[1-9]|50|8[0-5])\\d{3}|(?:65(?:02[12]|12[56]|22[89]|[3-5]00)|7(?:27\\d{2}|3100|5(?:02[12]|12[56]|22[89]|[34](?:00|81)|500)))\\d{3}", , , , "31234"], [, , "(?:6(?:200[01]|30[01]\\d)|7(?:200[01]|3(?:0[0-5]\\d|140)))\\d{3}", , , , "72001234", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "30(?:0[01]\\d{2}|12(?:11|20))\\d{2}", 
, , , "30010000", , , [8]], "KI", 686, "00", , , , "0", , , , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], KM:[, [, , "[3478]\\d{6}", , , , , , , [7]], [, , "7[4-7]\\d{5}", , , , "7712345"], [, , "[34]\\d{6}", , , , "3212345"], [, , , , , , , , , [-1]], [, , "8\\d{6}", , , , "8001234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "KM", 269, "00", , , , , , , , [[, "(\\d{3})(\\d{2})(\\d{2})", 
"$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], KN:[, [, , "[589]\\d{9}", , , , , , , [10], [7]], [, , "869(?:2(?:29|36)|302|4(?:6[015-9]|70))\\d{4}", , , , "8692361234", , , , [7]], [, , "869(?:5(?:5[6-8]|6[5-7])|66\\d|76[02-7])\\d{4}", , , , "8697652917", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", 
, , , "5002345678"], [, , , , , , , , , [-1]], "KN", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "869", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], KP:[, [, , "1\\d{9}|[28]\\d{7}", , , , , , , [8, 10], [6, 7]], [, , "2\\d{7}|85\\d{6}", , , , "21234567", , , [8], [6, 7]], [, , "19[123]\\d{7}", , , , "1921234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , 
, , [-1]], "KP", 850, "00|99", "0", , , "0", , , , [[, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["1"], "0$1"], [, "(\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["2"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["8"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , "2(?:[0-24-9]\\d{2}|3(?:[0-79]\\d|8[02-9]))\\d{4}", , , , "23821234", , , [8]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], KR:[, [, , "00(?:3\\d{8,9}|7\\d{9,11})|[1-7]\\d{4,9}|8\\d{8}", , , , , , , [5, 6, 8, 9, 10, 11, 12, 13, 
14], [3, 7]], [, , "2[1-9]\\d{6,7}|(?:3[1-3]|[46][1-4]|5[1-5])(?:1\\d{2,3}|[1-9]\\d{6,7})", , , , "22123456", , , [5, 6, 8, 9, 10], [3, 7]], [, , "1[0-26-9]\\d{7,8}", , , , "1000000000", , , [9, 10]], [, , "(?:00(?:3(?:08|68\\d)|798\\d{1,3})|80\\d)\\d{6}", , , , "801234567", , , [9, 11, 12, 13, 14]], [, , "60[2-9]\\d{6}", , , , "602345678", , , [9]], [, , , , , , , , , [-1]], [, , "50\\d{8}", , , , "5012345678", , , [10]], [, , "70\\d{8}", , , , "7012345678", , , [10]], "KR", 82, "00(?:[1259]|3(?:[46]5|91)|7(?:00|27|3|55|6[126]))", 
"0", , , "0(8[1-46-8]|85\\d{2})?", , , , [[, "(\\d{2})(\\d{3,4})", "$1-$2", ["(?:3[1-3]|[46][1-4]|5[1-5])1"], "0$1", "0$CC-$1"], [, "(\\d{4})(\\d{4})", "$1-$2", ["1(?:5[246-9]|6[046-8]|8[03579])", "1(?:5(?:22|44|66|77|88|99)|6(?:[07]0|44|6[16]|88)|8(?:00|33|55|77|99))"], "$1", "0$CC-$1"], [, "(\\d{5})", "$1", ["1[016-9]1", "1[016-9]11", "1[016-9]114"], "0$1", "0$CC-$1"], [, "(\\d)(\\d{3,4})(\\d{4})", "$1-$2-$3", ["2[1-9]"], "0$1", "0$CC-$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1-$2-$3", ["60[2-9]|80"], 
"0$1", "0$CC-$1"], [, "(\\d{2})(\\d{3,4})(\\d{4})", "$1-$2-$3", ["1[0-25-9]|(?:3[1-3]|[46][1-4]|5[1-5])[1-9]"], "0$1", "0$CC-$1"], [, "(\\d{2})(\\d{4})(\\d{4})", "$1-$2-$3", ["[57]0"], "0$1", "0$CC-$1"], [, "(\\d{5})(\\d{3})(\\d{3})", "$1 $2 $3", ["003", "0030", "00308"], "$1", "0$CC-$1"], [, "(\\d{5})(\\d{3,4})(\\d{4})", "$1 $2 $3", ["00[37]", "00(?:36|79)", "00(?:36|79)8"], "$1", "0$CC-$1"], [, "(\\d{5})(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3 $4", ["007", "0079", "00798"], "$1", "0$CC-$1"]], [[, "(\\d{2})(\\d{3,4})", 
"$1-$2", ["(?:3[1-3]|[46][1-4]|5[1-5])1"], "0$1", "0$CC-$1"], [, "(\\d{4})(\\d{4})", "$1-$2", ["1(?:5[246-9]|6[046-8]|8[03579])", "1(?:5(?:22|44|66|77|88|99)|6(?:[07]0|44|6[16]|88)|8(?:00|33|55|77|99))"], "$1", "0$CC-$1"], [, "(\\d{5})", "$1", ["1[016-9]1", "1[016-9]11", "1[016-9]114"], "0$1", "0$CC-$1"], [, "(\\d)(\\d{3,4})(\\d{4})", "$1-$2-$3", ["2[1-9]"], "0$1", "0$CC-$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1-$2-$3", ["60[2-9]|80"], "0$1", "0$CC-$1"], [, "(\\d{2})(\\d{3,4})(\\d{4})", "$1-$2-$3", 
["1[0-25-9]|(?:3[1-3]|[46][1-4]|5[1-5])[1-9]"], "0$1", "0$CC-$1"], [, "(\\d{2})(\\d{4})(\\d{4})", "$1-$2-$3", ["[57]0"], "0$1", "0$CC-$1"]], [, , "15\\d{7,8}", , , , "1523456789", , , [9, 10]], , , [, , "00(?:3(?:08|68\\d)|798\\d{1,3})\\d{6}", , , , "007981234567", , , [11, 12, 13, 14]], [, , "1(?:5(?:22|44|66|77|88|99)|6(?:00|44|6[16]|70|88)|8(?:00|33|55|77|99))\\d{4}", , , , "15441234", , , [8]], , , [, , , , , , , , , [-1]]], KW:[, [, , "[12569]\\d{6,7}", , , , , , , [7, 8]], [, , "(?:18\\d|2(?:[23]\\d{2}|4(?:[1-35-9]\\d|44)|5(?:0[034]|[2-46]\\d|5[1-3]|7[1-7])))\\d{4}", 
, , , "22345678"], [, , "(?:5(?:[05]\\d{2}|1[0-7]\\d|2(?:22|5[25])|6[56]\\d)|6(?:0[034679]\\d|222|5[015-9]\\d|6\\d{2}|7(?:0[013-9]|[67]\\d)|9(?:[069]\\d|3[039]))|9(?:0[09]\\d|22\\d|4[01479]\\d|55\\d|6[0679]\\d|7(?:02|[1-9]\\d)|8[057-9]\\d|9\\d{2}))\\d{4}", , , , "50012345", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "KW", 965, "00", , , , , , , , [[, "(\\d{4})(\\d{3,4})", "$1 $2", ["[16]|2(?:[0-35-9]|4[0-35-9])|52[25]|9[024-9]"]], 
[, "(\\d{3})(\\d{5})", "$1 $2", ["244|5(?:[015]|6[56])"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], KY:[, [, , "[3589]\\d{9}", , , , , , , [10], [7]], [, , "345(?:2(?:22|44)|444|6(?:23|38|40)|7(?:4[35-79]|6[6-9]|77)|8(?:00|1[45]|25|[48]8)|9(?:14|4[035-9]))\\d{4}", , , , "3452221234", , , , [7]], [, , "345(?:32[1-9]|5(?:1[67]|2[5-79]|4[6-9]|50|76)|649|9(?:1[67]|2[2-9]|3[689]))\\d{4}", , , , "3453231234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", 
, , , "8002345678"], [, , "900[2-9]\\d{6}|345976\\d{4}", , , , "9002345678"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "KY", 1, "011", "1", , , "1", , , , , , [, , "345849\\d{4}", , , , "3458491234"], , "345", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], KZ:[, [, , "(?:33\\d|7\\d{2}|80[089])\\d{7}", , , , , , , [10]], [, , "33622\\d{5}|7(?:1(?:0(?:[23]\\d|4[0-3]|59|63)|1(?:[23]\\d|4[0-79]|59)|2(?:[23]\\d|59)|3(?:2\\d|3[0-79]|4[0-35-9]|59)|4(?:[24]\\d|3[013-9]|5[1-9])|5(?:2\\d|3[1-9]|4[0-7]|59)|6(?:[234]\\d|5[19]|61)|72\\d|8(?:[27]\\d|3[1-46-9]|4[0-5]))|2(?:1(?:[23]\\d|4[46-9]|5[3469])|2(?:2\\d|3[0679]|46|5[12679])|3(?:[234]\\d|5[139])|4(?:2\\d|3[1235-9]|59)|5(?:[23]\\d|4[01246-8]|59|61)|6(?:2\\d|3[1-9]|4[0-4]|59)|7(?:[2379]\\d|40|5[279])|8(?:[23]\\d|4[0-3]|59)|9(?:2\\d|3[124578]|59)))\\d{5}", 
, , , "7123456789"], [, , "7(?:0[012578]|47|6[02-4]|7[15-8]|85)\\d{7}", , , , "7710009998"], [, , "800\\d{7}", , , , "8001234567"], [, , "809\\d{7}", , , , "8091234567"], [, , , , , , , , , [-1]], [, , "808\\d{7}", , , , "8081234567"], [, , "751\\d{7}", , , , "7511234567"], "KZ", 7, "810", "8", , , "8", , "8~10", , , , [, , , , , , , , , [-1]], , , [, , "751\\d{7}", , , , "7511234567"], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], LA:[, [, , "[2-8]\\d{7,9}", , , , , , , [8, 9, 10], [6]], 
[, , "(?:2[13]|3(?:0\\d|[14])|[5-7][14]|41|8[1468])\\d{6}", , , , "21212862", , , [8, 9], [6]], [, , "20(?:2[2389]|5[24-689]|7[6-8]|9[125-9])\\d{6}", , , , "2023123456", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "LA", 856, "00", "0", , , "0", , , , [[, "(20)(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3 $4", ["20"], "0$1"], [, "([2-8]\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["2[13]|3[14]|[4-8]"], "0$1"], [, "(30)(\\d{2})(\\d{2})(\\d{3})", 
"$1 $2 $3 $4", ["30"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], LB:[, [, , "[13-9]\\d{6,7}", , , , , , , [7, 8]], [, , "(?:[14-6]\\d{2}|7(?:[2-57]\\d|62|8[0-7]|9[04-9])|8[02-9]\\d|9\\d{2})\\d{4}", , , , "1123456", , , [7]], [, , "(?:3\\d|7(?:[01]\\d|6[013-9]|8[89]|9[1-3])|81\\d)\\d{5}", , , , "71123456"], [, , , , , , , , , [-1]], [, , "9[01]\\d{6}", , , , "90123456", , , [8]], [, , "80\\d{6}", , , , "80123456", , 
, [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "LB", 961, "00", "0", , , "0", , , , [[, "(\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["[13-69]|7(?:[2-57]|62|8[0-7]|9[04-9])|8[02-9]"], "0$1"], [, "([7-9]\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["7(?:[01]|6[013-9]|8[89]|9[1-3])|[89][01]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], LC:[, [, , "[5789]\\d{9}", , , , , , , [10], [7]], [, , "758(?:4(?:30|5[0-9]|6[2-9]|8[0-2])|57[0-2]|638)\\d{4}", 
, , , "7584305678", , , , [7]], [, , "758(?:28[4-7]|384|4(?:6[01]|8[4-9])|5(?:1[89]|20|84)|7(?:1[2-9]|2\\d|3[01]))\\d{4}", , , , "7582845678", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "LC", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "758", [, , , , , , , , , [-1]], [, , , , , , , , 
, [-1]], , , [, , , , , , , , , [-1]]], LI:[, [, , "6\\d{8}|[23789]\\d{6}", , , , , , , [7, 9]], [, , "(?:2(?:01|1[27]|3\\d|6[02-578]|96)|3(?:7[0135-7]|8[048]|9[0269]))\\d{4}", , , , "2345678", , , [7]], [, , "6(?:5(?:09|1\\d|20)|6(?:0[0-6]|10|2[06-9]|39))\\d{5}|7(?:[37-9]\\d|42|56)\\d{4}", , , , "660234567"], [, , "80(?:02[28]|9\\d{2})\\d{2}", , , , "8002222", , , [7]], [, , "90(?:02[258]|1(?:23|3[14])|66[136])\\d{2}", , , , "9002222", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], 
[, , , , , , , , , [-1]], "LI", 423, "00", "0", , , "0|10(?:01|20|66)", , , , [[, "(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3", ["[237-9]"]], [, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["6[56]"]], [, "(69)(7\\d{2})(\\d{4})", "$1 $2 $3", ["697"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "870(?:28|87)\\d{2}", , , , "8702812", , , [7]], , , [, , "697(?:42|56|[78]\\d)\\d{4}", , , , "697861234", , , [9]]], LK:[, [, , "[1-9]\\d{8}", , , , , , , [9], [7]], [, , "1(?:1[2-57]\\d{6}|973\\d{5})|(?:2[13-7]|3[1-8]|4[157]|5[12457]|6[35-7]|[89]1)[2-57]\\d{6}", 
, , , "112345678", , , , [7]], [, , "7[0125-8]\\d{7}", , , , "712345678"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "LK", 94, "00", "0", , , "0", , , , [[, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["[1-689]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["7"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], LR:[, [, , "2\\d{7,8}|[378]\\d{8}|4\\d{6}|5\\d{6,8}", 
, , , , , , [7, 8, 9]], [, , "(?:2\\d{3}|33333)\\d{4}", , , , "21234567", , , [8, 9]], [, , "(?:20\\d{2}|330\\d|4[67]|5(?:55)?\\d|77\\d{2}|88\\d{2})\\d{5}", , , , "770123456", , , [7, 9]], [, , , , , , , , , [-1]], [, , "332(?:02|[2-5]\\d)\\d{4}", , , , "332021234", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "LR", 231, "00", "0", , , "0", , , , [[, "(2\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["2"], "0$1"], [, "([4-5])(\\d{3})(\\d{3})", "$1 $2 $3", ["[45]"], 
"0$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["[23578]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], LS:[, [, , "[2568]\\d{7}", , , , , , , [8]], [, , "2\\d{7}", , , , "22123456"], [, , "[56]\\d{7}", , , , "50123456"], [, , "800[256]\\d{4}", , , , "80021234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "LS", 266, "00", , , , , , , , [[, "(\\d{4})(\\d{4})", 
"$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], LT:[, [, , "[3-9]\\d{7}", , , , , , , [8]], [, , "(?:3[1478]|4[124-6]|52)\\d{6}", , , , "31234567"], [, , "6\\d{7}", , , , "61234567"], [, , "800\\d{5}", , , , "80012345"], [, , "9(?:0[0239]|10)\\d{5}", , , , "90012345"], [, , "808\\d{5}", , , , "80812345"], [, , "700\\d{5}", , , , "70012345"], [, , , , , , , , , [-1]], "LT", 370, "00", "8", , , "[08]", , , , [[, "([34]\\d)(\\d{6})", 
"$1 $2", ["37|4(?:1|5[45]|6[2-4])"], "(8-$1)", , 1], [, "([3-6]\\d{2})(\\d{5})", "$1 $2", ["3[148]|4(?:[24]|6[09])|528|6"], "(8-$1)", , 1], [, "([7-9]\\d{2})(\\d{2})(\\d{3})", "$1 $2 $3", ["[7-9]"], "8 $1", , 1], [, "(5)(2\\d{2})(\\d{4})", "$1 $2 $3", ["52[0-79]"], "(8-$1)", , 1]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "70[67]\\d{5}", , , , "70712345"], , , [, , , , , , , , , [-1]]], LU:[, [, , "[24-9]\\d{3,10}|3(?:[0-46-9]\\d{2,9}|5[013-9]\\d{1,8})", , , , , , , [4, 5, 6, 
7, 8, 9, 10, 11]], [, , "(?:2[2-9]\\d{2,9}|(?:3(?:[0-46-9]\\d|5[013-9])|[457]\\d{2}|8(?:0[2-9]|[13-9]\\d)|9(?:0[89]|[2-579]\\d))\\d{1,8})", , , , "27123456"], [, , "6[25-79][18]\\d{6}", , , , "628123456", , , [9]], [, , "800\\d{5}", , , , "80012345", , , [8]], [, , "90[015]\\d{5}", , , , "90012345", , , [8]], [, , "801\\d{5}", , , , "80112345", , , [8]], [, , , , , , , , , [-1]], [, , "20(?:1\\d{5}|[2-689]\\d{1,7})", , , , "20201234", , , [4, 5, 6, 7, 8, 9, 10]], "LU", 352, "00", , , , "(15(?:0[06]|1[12]|35|4[04]|55|6[26]|77|88|99)\\d)", 
, , , [[, "(\\d{2})(\\d{3})", "$1 $2", ["[2-5]|7[1-9]|[89](?:0[2-9]|[1-9])"], , "$CC $1"], [, "(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3", ["[2-5]|7[1-9]|[89](?:0[2-9]|[1-9])"], , "$CC $1"], [, "(\\d{2})(\\d{2})(\\d{3})", "$1 $2 $3", ["20"], , "$CC $1"], [, "(\\d{2})(\\d{2})(\\d{2})(\\d{1,2})", "$1 $2 $3 $4", ["2(?:[0367]|4[3-8])"], , "$CC $1"], [, "(\\d{2})(\\d{2})(\\d{2})(\\d{3})", "$1 $2 $3 $4", ["20"], , "$CC $1"], [, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})(\\d{1,2})", "$1 $2 $3 $4 $5", ["2(?:[0367]|4[3-8])"], 
, "$CC $1"], [, "(\\d{2})(\\d{2})(\\d{2})(\\d{1,4})", "$1 $2 $3 $4", ["2(?:[12589]|4[12])|[3-5]|7[1-9]|8(?:0[2-9]|[1-9])|9(?:0[2-46-9]|[1-9])"], , "$CC $1"], [, "(\\d{3})(\\d{2})(\\d{3})", "$1 $2 $3", ["70|80[01]|90[015]"], , "$CC $1"], [, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["6"], , "$CC $1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], LV:[, [, , "[2689]\\d{7}", , , , , , , [8]], [, , "6\\d{7}", , , , "63123456"], [, , 
"2\\d{7}", , , , "21234567"], [, , "80\\d{6}", , , , "80123456"], [, , "90\\d{6}", , , , "90123456"], [, , "81\\d{6}", , , , "81123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "LV", 371, "00", , , , , , , , [[, "([2689]\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["[2689]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], LY:[, [, , "[25679]\\d{8}", , , , , , , [9], [7]], [, , "(?:2[1345]|5[1347]|6[123479]|71)\\d{7}", , , , 
"212345678", , , , [7]], [, , "9[1-6]\\d{7}", , , , "912345678"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "LY", 218, "00", "0", , , "0", , , , [[, "([25-79]\\d)(\\d{7})", "$1-$2", ["[25-79]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MA:[, [, , "[5-9]\\d{8}", , , , , , , [9]], [, , "5(?:2(?:[015-79]\\d|2[02-9]|3[2-57]|4[2-8]|8[235-7])\\d|3(?:[0-48]\\d|[57][2-9]|6[2-8]|9[3-9])\\d|4[067]\\d{2}|5[03]\\d{2})\\d{4}", 
, , , "520123456"], [, , "(?:6(?:[0-79]\\d|8[0-247-9])|7(?:[07][07]|6[12]))\\d{6}", , , , "650123456"], [, , "80\\d{7}", , , , "801234567"], [, , "89\\d{7}", , , , "891234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "5924[01]\\d{4}", , , , "592401234"], "MA", 212, "00", "0", , , "0", , , , [[, "([5-7]\\d{2})(\\d{6})", "$1-$2", ["5(?:2[015-7]|3[0-4])|[67]"], "0$1"], [, "([58]\\d{3})(\\d{5})", "$1-$2", ["5(?:2[2-489]|3[5-9]|92)|892", "5(?:2(?:[2-48]|9[0-7])|3(?:[5-79]|8[0-7])|924)|892"], 
"0$1"], [, "(5\\d{4})(\\d{4})", "$1-$2", ["5(?:29|38)", "5(?:29|38)[89]"], "0$1"], [, "([5]\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["5(?:4[067]|5[03])"], "0$1"], [, "(8[09])(\\d{7})", "$1-$2", ["8(?:0|9[013-9])"], "0$1"]], , [, , , , , , , , , [-1]], 1, , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MC:[, [, , "[34689]\\d{7,8}", , , , , , , [8, 9]], [, , "870\\d{5}|9[2-47-9]\\d{6}", , , , "99123456", , , [8]], [, , "3\\d{7}|4(?:4\\d|5[1-9])\\d{5}|6\\d{8}", 
, , , "612345678"], [, , "90\\d{6}", , , , "90123456", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "MC", 377, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[39]"], "$1"], [, "(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["4"], "0$1"], [, "(6)(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4 $5", ["6"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{2})", "$1 $2 $3", ["8"], "$1"]], , [, , , , , , , , , [-1]], , , 
[, , "870\\d{5}", , , , "87012345", , , [8]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MD:[, [, , "[235-9]\\d{7}", , , , , , , [8]], [, , "(?:2[1-9]\\d|3[1-79]\\d|5(?:33|5[257]))\\d{5}", , , , "22212345"], [, , "(?:562|6\\d{2}|7(?:[189]\\d|6[07]|7[457-9]))\\d{5}", , , , "62112345"], [, , "800\\d{5}", , , , "80012345"], [, , "90[056]\\d{5}", , , , "90012345"], [, , "808\\d{5}", , , , "80812345"], [, , , , , , , , , [-1]], [, , "3[08]\\d{6}", , , , "30123456"], "MD", 373, "00", "0", 
, , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["22|3"], "0$1"], [, "([25-7]\\d{2})(\\d{2})(\\d{3})", "$1 $2 $3", ["2[13-9]|[5-7]"], "0$1"], [, "([89]\\d{2})(\\d{5})", "$1 $2", ["[89]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "803\\d{5}", , , , "80312345"], , , [, , , , , , , , , [-1]]], ME:[, [, , "[2-9]\\d{7,8}", , , , , , , [8], [6]], [, , "(?:20[2-8]|3(?:0[2-7]|[12][235-7]|3[24-7])|4(?:0[2-467]|1[267])|5(?:0[2467]|1[267]|2[2367]))\\d{5}", , , , "30234567", 
, , , [6]], [, , "6(?:00\\d|3[024]\\d|6[0-25]\\d|[7-9]\\d{2})\\d{4}", , , , "67622901"], [, , "80[0-258]\\d{5}", , , , "80080002"], [, , "(?:9(?:4[1568]|5[178]))\\d{5}", , , , "94515151"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "78[1-49]\\d{5}", , , , "78108780"], "ME", 382, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["[2-57-9]|6[036-9]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "77[1-9]\\d{5}", , , , "77273012"], , , [, 
, , , , , , , , [-1]]], MF:[, [, , "[56]\\d{8}", , , , , , , [9]], [, , "590(?:[02][79]|13|5[0-268]|[78]7)\\d{4}", , , , "590271234"], [, , "690(?:0[05-9]|[1-9]\\d)\\d{4}", , , , "690001234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "MF", 590, "00", "0", , , "0", , , , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MG:[, [, , "[23]\\d{8}", 
, , , , , , [9], [7]], [, , "20(?:2\\d{2}|4[47]\\d|5[3467]\\d|6[279]\\d|7(?:2[29]|[35]\\d)|8[268]\\d|9[245]\\d)\\d{4}", , , , "202123456", , , , [7]], [, , "3[2-49]\\d{7}", , , , "321234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "22\\d{7}", , , , "221234567"], "MG", 261, "00", "0", , , "0", , , , [[, "([23]\\d)(\\d{2})(\\d{3})(\\d{2})", "$1 $2 $3 $4", ["[23]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], 
[, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MH:[, [, , "[2-6]\\d{6}", , , , , , , [7]], [, , "(?:247|528|625)\\d{4}", , , , "2471234"], [, , "(?:235|329|45[56]|545)\\d{4}", , , , "2351234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "635\\d{4}", , , , "6351234"], "MH", 692, "011", "1", , , "1", , , , [[, "(\\d{3})(\\d{4})", "$1-$2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], 
, , [, , , , , , , , , [-1]]], MK:[, [, , "[2-578]\\d{7}", , , , , , , [8], [6, 7]], [, , "(?:2(?:[23]\\d|5[124578]|6[01])|3(?:1[3-6]|[23][2-6]|4[2356])|4(?:[23][2-6]|4[3-6]|5[256]|6[25-8]|7[24-6]|8[4-6]))\\d{5}", , , , "22212345", , , , [6, 7]], [, , "7(?:[0-25-8]\\d{2}|32\\d|421|9[23]\\d)\\d{4}", , , , "72345678"], [, , "800\\d{5}", , , , "80012345"], [, , "5[02-9]\\d{6}", , , , "50012345"], [, , "8(?:0[1-9]|[1-9]\\d)\\d{5}", , , , "80123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], 
"MK", 389, "00", "0", , , "0", , , , [[, "(2)(\\d{3})(\\d{4})", "$1 $2 $3", ["2"], "0$1"], [, "([347]\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["[347]"], "0$1"], [, "([58]\\d{2})(\\d)(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[58]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], ML:[, [, , "[246-9]\\d{7}", , , , , , , [8]], [, , "(?:2(?:0(?:2\\d|7[0-8])|1(?:2[5-7]|[3-689]\\d))|44[1239]\\d)\\d{4}", , , , "20212345"], [, , "(?:2(?:079|17\\d)|[679]\\d{3}|8[239]\\d{2})\\d{4}", 
, , , "65012345"], [, , "80\\d{6}", , , , "80012345"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "ML", 223, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[246-9]"]], [, "(\\d{4})", "$1", ["67|74"]]], [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[246-9]"]]], [, , , , , , , , , [-1]], , , [, , "80\\d{6}", , , , "80012345"], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MM:[, [, , "[178]\\d{5,7}|[24-6]\\d{5,8}|9(?:[279]\\d{0,2}|5|[34]\\d{1,2}|6(?:\\d{1,2})?|8(?:\\d{2})?)\\d{6}", 
, , , , , , [6, 7, 8, 9, 10], [5]], [, , "1(?:2\\d{1,2}|[35]\\d|4(?:\\d|2[236]|39)|6\\d?|[89][0-6]\\d)\\d{4}|2(?:2(?:000\\d{3}|\\d{4})|3\\d{4}|4(?:0\\d{5}|26\\d{4}|39\\d{4}|\\d{4})|5(?:1\\d{3,6}|[02-9]\\d{3,5})|[6-9]\\d{4})|4(?:2[245-8]|3(?:2(?:02)?|[346]|56?)|[46][2-6]|5[3-5])\\d{4}|5(?:2(?:2(?:\\d{1,2})?|[3-8])|3[2-68]|4(?:21?|[4-8])|5[23]|6[2-4]|7[2-8]|8[24-7]|9[2-7])\\d{4}|6(?:0[23]|1(?:2(?:0|4\\d)?|[356])|2[2-6]|3[24-6]|4(?:2(?:4\\d)?|[3-6])|5[2-4]|6[2-8]|7(?:[2367]|4(?:\\d|39)|5\\d?|8[145]\\d)|8[245]|9(?:20?|4))\\d{4}|7(?:[04][24-8]|1(?:20?|[3-7])|22|3[2-4]|5[2-7])\\d{4}|8(?:1(?:2\\d{1,2}|[3-689]\\d)|2(?:2\\d|3(?:\\d|20)|[4-8]\\d)|3[24]\\d|4[24-7]\\d|5[245]\\d|6[23]\\d)\\d{3}", 
, , , "1234567", , , [6, 7, 8, 9], [5]], [, , "17[01]\\d{4}|9(?:2(?:[0-4]|5\\d{2}|6[0-5]\\d)|3(?:[0-36]|4[069])\\d|4(?:0[0-4]\\d|[1379]\\d|2\\d{2}|4[0-589]\\d|5\\d{2}|88)|5[0-6]|6(?:1\\d|9\\d{2}|\\d)|7(?:3\\d|[6-9]\\d{2})|8(?:\\d|9\\d{2})|9(?:1\\d|[5-7]\\d{2}|[089]))\\d{5}", , , , "92123456", , , [7, 8, 9, 10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "1333\\d{4}", , , , "13331234", , , [8]], "MM", 95, "00", "0", , , "0", , , , 
[[, "(\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["1|2[245]"], "0$1"], [, "(2)(\\d{4})(\\d{4})", "$1 $2 $3", ["251"], "0$1"], [, "(\\d)(\\d{2})(\\d{3})", "$1 $2 $3", ["16|2"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["432|67|81"], "0$1"], [, "(\\d{2})(\\d{2})(\\d{3,4})", "$1 $2 $3", ["[4-8]"], "0$1"], [, "(9)(\\d{3})(\\d{4,6})", "$1 $2 $3", ["9(?:2[0-4]|[35-9]|4[137-9])"], "0$1"], [, "(9)([34]\\d{4})(\\d{4})", "$1 $2 $3", ["9(?:3[0-36]|4[0-57-9])"], "0$1"], [, "(9)(\\d{3})(\\d{3})(\\d{3})", 
"$1 $2 $3 $4", ["92[56]"], "0$1"], [, "(9)(\\d{3})(\\d{3})(\\d{2})", "$1 $2 $3 $4", ["93"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MN:[, [, , "[12]\\d{7,9}|[57-9]\\d{7}", , , , , , , [8, 9, 10], [6, 7]], [, , "[12](?:1\\d|2(?:[1-3]\\d?|7\\d)|3[2-8]\\d{1,2}|4[2-68]\\d{1,2}|5[1-4689]\\d{1,2})\\d{5}|5[0568]\\d{6}", , , , "50123456", , , , [6, 7]], [, , "(?:8(?:[05689]\\d|3[01])|9[013-9]\\d)\\d{5}", , , , "88123456", 
, , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "7[05-8]\\d{6}", , , , "75123456", , , [8]], "MN", 976, "001", "0", , , "0", , , , [[, "([12]\\d)(\\d{2})(\\d{4})", "$1 $2 $3", ["[12]1"], "0$1"], [, "([12]2\\d)(\\d{5,6})", "$1 $2", ["[12]2[1-3]"], "0$1"], [, "([12]\\d{3})(\\d{5})", "$1 $2", ["[12](?:27|[3-5])", "[12](?:27|[3-5]\\d)2"], "0$1"], [, "(\\d{4})(\\d{4})", "$1 $2", ["[57-9]"], "$1"], [, "([12]\\d{4})(\\d{4,5})", "$1 $2", 
["[12](?:27|[3-5])", "[12](?:27|[3-5]\\d)[4-9]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MO:[, [, , "[268]\\d{7}", , , , , , , [8]], [, , "(?:28[2-57-9]|8(?:11|[2-57-9]\\d))\\d{5}", , , , "28212345"], [, , "6(?:[2356]\\d|8[158])\\d{5}", , , , "66123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "MO", 853, "00", , , , , , , , 
[[, "([268]\\d{3})(\\d{4})", "$1 $2", ["[268]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MP:[, [, , "[5689]\\d{9}", , , , , , , [10], [7]], [, , "670(?:2(?:3[3-7]|56|8[5-8])|32[1238]|4(?:33|8[348])|5(?:32|55|88)|6(?:64|70|82)|78[3589]|8[3-9]8|989)\\d{4}", , , , "6702345678", , , , [7]], [, , "670(?:2(?:3[3-7]|56|8[5-8])|32[1238]|4(?:33|8[348])|5(?:32|55|88)|6(?:64|70|82)|78[3589]|8[3-9]8|989)\\d{4}", , , , "6702345678", 
, , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "MP", 1, "011", "1", , , "1", , , 1, , , [, , , , , , , , , [-1]], , "670", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MQ:[, [, , "[56]\\d{8}", , , , , , , [9]], [, , "596(?:0[2-5]|[12]0|3[05-9]|4[024-8]|[5-7]\\d|89|9[4-8])\\d{4}", 
, , , "596301234"], [, , "696(?:[0-47-9]\\d|5[0-6]|6[0-4])\\d{4}", , , , "696201234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "MQ", 596, "00", "0", , , "0", , , , [[, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", , "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MR:[, [, , "[2-48]\\d{7}", , , , , , , [8]], [, , "25[08]\\d{5}|35\\d{6}|45[1-7]\\d{5}", 
, , , "35123456"], [, , "[234][0-46-9]\\d{6}", , , , "22123456"], [, , "800\\d{5}", , , , "80012345"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "MR", 222, "00", , , , , , , , [[, "([2-48]\\d)(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[2-48]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MS:[, [, , "[5689]\\d{9}", , , , , , , [10], [7]], [, , "664491\\d{4}", , , 
, "6644912345", , , , [7]], [, , "66449[2-6]\\d{4}", , , , "6644923456", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "MS", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "664", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MT:[, [, , "[2357-9]\\d{7}", , 
, , , , , [8]], [, , "2(?:0(?:[169]\\d|3[1-4])|[1-357]\\d{2})\\d{4}", , , , "21001234"], [, , "(?:7(?:210|[79]\\d{2})|9(?:2(?:1[01]|31)|69[67]|8(?:1[1-3]|89|97)|9\\d{2}))\\d{4}", , , , "96961234"], [, , "800[3467]\\d{4}", , , , "80071234"], [, , "5(?:0(?:0(?:37|43)|6\\d{2}|70\\d|9[0168]\\d)|[12]\\d0[1-5])\\d{3}", , , , "50037123"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "3550\\d{4}", , , , "35501234"], "MT", 356, "00", , , , , , , , [[, "(\\d{4})(\\d{4})", "$1 $2"]], , [, , "7117\\d{4}", 
, , , "71171234"], , , [, , , , , , , , , [-1]], [, , "501\\d{5}", , , , "50112345"], , , [, , , , , , , , , [-1]]], MU:[, [, , "[2-9]\\d{6,7}", , , , , , , [7, 8]], [, , "(?:2(?:[03478]\\d|1[0-7]|6[1-69])|4(?:[013568]\\d|2[4-7])|5(?:44\\d|471)|6\\d{2}|8(?:14|3[129]))\\d{4}", , , , "2012345"], [, , "5(?:2[59]\\d|4(?:2[1-389]|4\\d|7[1-9]|9\\d)|7\\d{2}|8(?:[0-25689]\\d|4[3479]|7[15-8])|9[0-8]\\d)\\d{4}", , , , "52512345", , , [8]], [, , "80[012]\\d{4}", , , , "8001234", , , [7]], [, , "30\\d{5}", , 
, , "3012345", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "3(?:20|9\\d)\\d{4}", , , , "3201234", , , [7]], "MU", 230, "0(?:0|[2-7]0|33)", , , , , , "020", , [[, "([2-46-9]\\d{2})(\\d{4})", "$1 $2", ["[2-46-9]"]], [, "(5\\d{3})(\\d{4})", "$1 $2", ["5"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MV:[, [, , "[346-8]\\d{6,9}|9(?:00\\d{7}|\\d{6})", , , , , , , [7, 10]], [, , "(?:3(?:0[0-3]|3[0-59])|6(?:[57][02468]|6[024568]|8[024689]))\\d{4}", 
, , , "6701234", , , [7]], [, , "(?:46[46]|7[2-9]\\d|9[15-9]\\d)\\d{4}", , , , "7712345", , , [7]], [, , "800\\d{7}", , , , "8001234567", , , [10]], [, , "900\\d{7}", , , , "9001234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "MV", 960, "0(?:0|19)", , , , , , "00", , [[, "(\\d{3})(\\d{4})", "$1-$2", ["[3467]|9(?:0[1-9]|[1-9])"]], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["[89]00"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , 
"4[05]0\\d{4}", , , , "4001234", , , [7]], , , [, , , , , , , , , [-1]]], MW:[, [, , "(?:1(?:\\d{2})?|[2789]\\d{2})\\d{6}", , , , , , , [7, 9]], [, , "(?:1[2-9]|21\\d{2})\\d{5}", , , , "1234567"], [, , "(?:111|77\\d|88\\d|99\\d)\\d{6}", , , , "991234567", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "MW", 265, "00", "0", , , "0", , , , [[, "(\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["1"], "0$1"], [, "(2\\d{2})(\\d{3})(\\d{3})", 
"$1 $2 $3", ["2"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[17-9]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MX:[, [, , "[1-9]\\d{9,10}", , , , , , , [10, 11], [7, 8]], [, , "(?:33|55|81)\\d{8}|(?:2(?:0[01]|2[1-9]|3[1-35-8]|4[13-9]|7[1-689]|8[1-578]|9[467])|3(?:1[1-79]|[2458][1-9]|7[1-8]|9[1-5])|4(?:1[1-57-9]|[24-6][1-9]|[37][1-8]|8[1-35-9]|9[2-689])|5(?:88|9[1-79])|6(?:1[2-68]|[234][1-9]|5[1-3689]|6[12457-9]|7[1-7]|8[67]|9[4-8])|7(?:[13467][1-9]|2[1-8]|5[13-9]|8[1-69]|9[17])|8(?:2[13-689]|3[1-6]|4[124-6]|6[1246-9]|7[1-378]|9[12479])|9(?:1[346-9]|2[1-4]|3[2-46-8]|5[1348]|[69][1-9]|7[12]|8[1-8]))\\d{7}", 
, , , "2221234567", , , [10], [7, 8]], [, , "1(?:(?:33|55|81)\\d{8}|(?:2(?:2[1-9]|3[1-35-8]|4[13-9]|7[1-689]|8[1-578]|9[467])|3(?:1[1-79]|[2458][1-9]|7[1-8]|9[1-5])|4(?:1[1-57-9]|[24-6][1-9]|[37][1-8]|8[1-35-9]|9[2-689])|5(?:88|9[1-79])|6(?:1[2-68]|[2-4][1-9]|5[1-3689]|6[12457-9]|7[1-7]|8[67]|9[4-8])|7(?:[13467][1-9]|2[1-8]|5[13-9]|8[1-69]|9[17])|8(?:2[13-689]|3[1-6]|4[124-6]|6[1246-9]|7[1-378]|9[12479])|9(?:1[346-9]|2[1-4]|3[2-46-8]|5[1348]|[69][1-9]|7[12]|8[1-8]))\\d{7})", , , , "12221234567", 
, , [11]], [, , "8(?:00|88)\\d{7}", , , , "8001234567", , , [10]], [, , "900\\d{7}", , , , "9001234567", , , [10]], [, , "300\\d{7}", , , , "3001234567", , , [10]], [, , "500\\d{7}", , , , "5001234567", , , [10]], [, , , , , , , , , [-1]], "MX", 52, "0[09]", "01", , , "0[12]|04[45](\\d{10})", "1$1", , , [[, "([358]\\d)(\\d{4})(\\d{4})", "$1 $2 $3", ["33|55|81"], "01 $1", , 1], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["[2467]|3[0-2457-9]|5[089]|8[02-9]|9[0-35-9]"], "01 $1", , 1], [, "(1)([358]\\d)(\\d{4})(\\d{4})", 
"044 $2 $3 $4", ["1(?:33|55|81)"], "$1", , 1], [, "(1)(\\d{3})(\\d{3})(\\d{4})", "044 $2 $3 $4", ["1(?:[2467]|3[0-2457-9]|5[089]|8[2-9]|9[1-35-9])"], "$1", , 1]], [[, "([358]\\d)(\\d{4})(\\d{4})", "$1 $2 $3", ["33|55|81"], "01 $1", , 1], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["[2467]|3[0-2457-9]|5[089]|8[02-9]|9[0-35-9]"], "01 $1", , 1], [, "(1)([358]\\d)(\\d{4})(\\d{4})", "$1 $2 $3 $4", ["1(?:33|55|81)"]], [, "(1)(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3 $4", ["1(?:[2467]|3[0-2457-9]|5[089]|8[2-9]|9[1-35-9])"]]], 
[, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MY:[, [, , "[13-9]\\d{7,9}", , , , , , , [8, 9, 10], [6, 7]], [, , "(?:3[2-9]\\d|[4-9][2-9])\\d{6}", , , , "323456789", , , [8, 9], [6, 7]], [, , "1(?:0(?:[23568]\\d|4[0-6]|7[016-9]|9[0-8])\\d|1(?:[1-5]\\d{2}|6(?:0[5-9]|[1-9]\\d))\\d|[23679][2-9]\\d{2}|4(?:[235-9]\\d{2}|400)|59\\d{3}|8(?:1[23]\\d|[236]\\d{2}|4(?:[06]\\d|7[0-4])\\d|5[7-9]\\d|7[016-9]\\d|8(?:[01]\\d|[27][0-4])|9[0-8]\\d))\\d{4}", 
, , , "123456789", , , [9, 10]], [, , "1[378]00\\d{6}", , , , "1300123456", , , [10]], [, , "1600\\d{6}", , , , "1600123456", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "154(?:6(?:0\\d|1[0-3])|8(?:[25]1|4[0189]|7[0-4679]))\\d{4}", , , , "1546012345", , , [10]], "MY", 60, "00", "0", , , "0", , , , [[, "([4-79])(\\d{3})(\\d{4})", "$1-$2 $3", ["[4-79]"], "0$1"], [, "(3)(\\d{4})(\\d{4})", "$1-$2 $3", ["3"], "0$1"], [, "([18]\\d)(\\d{3})(\\d{3,4})", "$1-$2 $3", ["1[02-46-9][1-9]|8"], 
"0$1"], [, "(1)([36-8]00)(\\d{2})(\\d{4})", "$1-$2-$3-$4", ["1[36-8]0", "1[36-8]00"]], [, "(11)(\\d{4})(\\d{4})", "$1-$2 $3", ["11"], "0$1"], [, "(15[49])(\\d{3})(\\d{4})", "$1-$2 $3", ["15[49]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], MZ:[, [, , "[28]\\d{7,8}", , , , , , , [8, 9]], [, , "2(?:[1346]\\d|5[0-2]|[78][12]|93)\\d{5}", , , , "21123456", , , [8]], [, , "8[2-7]\\d{7}", , , , "821234567", , , [9]], [, , "800\\d{6}", 
, , , "800123456", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "MZ", 258, "00", , , , , , , , [[, "([28]\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["2|8[2-7]"]], [, "(80\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["80"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], NA:[, [, , "[68]\\d{7,8}", , , , , , , [8, 9]], [, , "6(?:1(?:17|2(?:[0189]\\d|[2-6]|7\\d?)|3(?:[01378]|2\\d)|4(?:[024]|10?|3[15]?)|69|7[014])|2(?:17|5(?:[0-36-8]|4\\d?)|69|70)|3(?:17|2(?:[0237]\\d?|[14-689])|34|6[289]|7[01]|81)|4(?:17|2(?:[012]|7\\d?)|4(?:[06]|1\\d?)|5(?:[01357]|[25]\\d?)|69|7[01])|5(?:17|2(?:[0459]|[23678]\\d?)|69|7[01])|6(?:17|2(?:5|6\\d?)|38|42|69|7[01])|7(?:17|2(?:[569]|[234]\\d?)|3(?:0\\d?|[13])|6[89]|7[01]))\\d{4}", 
, , , "61221234"], [, , "(?:60|8[125])\\d{7}", , , , "811234567", , , [9]], [, , , , , , , , , [-1]], [, , "8701\\d{5}", , , , "870123456", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "8(?:3\\d{2}|86)\\d{5}", , , , "88612345"], "NA", 264, "00", "0", , , "0", , , , [[, "(8\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["8[1-35]"], "0$1"], [, "(6\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["6"], "0$1"], [, "(88)(\\d{3})(\\d{3})", "$1 $2 $3", ["88"], "0$1"], [, "(870)(\\d{3})(\\d{3})", "$1 $2 $3", 
["870"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], NC:[, [, , "[2-57-9]\\d{5}", , , , , , , [6]], [, , "(?:2[03-9]|3[0-5]|4[1-7]|88)\\d{4}", , , , "201234"], [, , "(?:5[0-4]|[79]\\d|8[0-79])\\d{4}", , , , "751234"], [, , , , , , , , , [-1]], [, , "36\\d{4}", , , , "366711"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "NC", 687, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})", "$1.$2.$3", 
["[2-46-9]|5[0-4]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], NE:[, [, , "[0289]\\d{7}", , , , , , , [8]], [, , "2(?:0(?:20|3[1-7]|4[134]|5[14]|6[14578]|7[1-578])|1(?:4[145]|5[14]|6[14-68]|7[169]|88))\\d{4}", , , , "20201234"], [, , "(?:8[089]|9\\d)\\d{6}", , , , "93123456"], [, , "08\\d{6}", , , , "08123456"], [, , "09\\d{6}", , , , "09123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], 
"NE", 227, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["09|[289]"]], [, "(08)(\\d{3})(\\d{3})", "$1 $2 $3", ["08"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], NF:[, [, , "[13]\\d{5}", , , , , , , [6], [5]], [, , "(?:1(?:06|17|28|39)|3[012]\\d)\\d{3}", , , , "106609", , , , [5]], [, , "3[58]\\d{4}", , , , "381234", , , , [5]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , 
, , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "NF", 672, "00", , , , , , , , [[, "(\\d{2})(\\d{4})", "$1 $2", ["1"]], [, "(\\d)(\\d{5})", "$1 $2", ["3"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], NG:[, [, , "[1-6]\\d{5,8}|9\\d{5,9}|[78]\\d{5,13}", , , , , , , [7, 8, 10, 11, 12, 13, 14], [5, 6]], [, , "[12]\\d{6,7}|9(?:0[3-9]|[1-9]\\d)\\d{5}|(?:3\\d|4[023568]|5[02368]|6[02-469]|7[4-69]|8[2-9])\\d{6}|(?:4[47]|5[14579]|6[1578]|7[0-357])\\d{5,6}|(?:78|41)\\d{5}", 
, , , "12345678", , , [7, 8], [5, 6]], [, , "(?:1(?:7[34]\\d|8(?:04|[124579]\\d|8[0-3])|95\\d)|287[0-7]|3(?:18[1-8]|88[0-7]|9(?:8[5-9]|6[1-5]))|4(?:28[0-2]|6(?:7[1-9]|8[02-47])|88[0-2])|5(?:2(?:7[7-9]|8\\d)|38[1-79]|48[0-7]|68[4-7])|6(?:2(?:7[7-9]|8\\d)|4(?:3[7-9]|[68][129]|7[04-69]|9[1-8])|58[0-2]|98[7-9])|7(?:38[0-7]|69[1-8]|78[2-4])|8(?:28[3-9]|38[0-2]|4(?:2[12]|3[147-9]|5[346]|7[4-9]|8[014-689]|90)|58[1-8]|78[2-9]|88[5-7])|98[07]\\d)\\d{4}|(?:70(?:[1-689]\\d|7[0-3])|8(?:0(?:1[01]|[2-9]\\d)|1(?:[0-8]\\d|9[01]))|90[235-9]\\d)\\d{6}", 
, , , "8021234567", , , [8, 10]], [, , "800\\d{7,11}", , , , "80017591759", , , [10, 11, 12, 13, 14]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "NG", 234, "009", "0", , , "0", , , , [[, "(\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["[12]|9(?:0[3-9]|[1-9])"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{2,3})", "$1 $2 $3", ["[3-6]|7(?:0[1-9]|[1-79])|8[2-9]"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["70|8[01]|90[235-9]"], "0$1"], [, "([78]00)(\\d{4})(\\d{4,5})", 
"$1 $2 $3", ["[78]00"], "0$1"], [, "([78]00)(\\d{5})(\\d{5,6})", "$1 $2 $3", ["[78]00"], "0$1"], [, "(78)(\\d{2})(\\d{3})", "$1 $2 $3", ["78"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "700\\d{7,11}", , , , "7001234567", , , [10, 11, 12, 13, 14]], , , [, , , , , , , , , [-1]]], NI:[, [, , "[125-8]\\d{7}", , , , , , , [8]], [, , "2\\d{7}", , , , "21234567"], [, , "(?:5(?:5[0-7]|[78]\\d)|6(?:20|3[035]|4[045]|5[05]|77|8[1-9]|9[059])|7[5-8]\\d|8\\d{2})\\d{5}", , , , "81234567"], 
[, , "1800\\d{4}", , , , "18001234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "NI", 505, "00", , , , , , , , [[, "(\\d{4})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], NL:[, [, , "1\\d{4,8}|[2-7]\\d{8}|[89]\\d{6,9}", , , , , , , [5, 6, 7, 8, 9, 10]], [, , "(?:1[0135-8]|2[02-69]|3[0-68]|4[0135-9]|[57]\\d|8[478])\\d{7}", , , , "101234567", , , [9]], 
[, , "6[1-58]\\d{7}", , , , "612345678", , , [9]], [, , "800\\d{4,7}", , , , "8001234", , , [7, 8, 9, 10]], [, , "90[069]\\d{4,7}", , , , "9061234", , , [7, 8, 9, 10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "(?:6760|85\\d{2})\\d{5}", , , , "851234567", , , [9]], "NL", 31, "00", "0", , , "0", , , , [[, "([1-578]\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["1[035]|2[0346]|3[03568]|4[0356]|5[0358]|7|8[4578]"], "0$1"], [, "([1-5]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["1[16-8]|2[259]|3[124]|4[17-9]|5[124679]"], 
"0$1"], [, "(6)(\\d{8})", "$1 $2", ["6[0-57-9]"], "0$1"], [, "(66)(\\d{7})", "$1 $2", ["66"], "0$1"], [, "(14)(\\d{3,4})", "$1 $2", ["14"], "$1"], [, "([89]0\\d)(\\d{4,7})", "$1 $2", ["[89]0"], "0$1"]], , [, , "66\\d{7}", , , , "662345678", , , [9]], , , [, , "140(?:1(?:[035]|[16-8]\\d)|2(?:[0346]|[259]\\d)|3(?:[03568]|[124]\\d)|4(?:[0356]|[17-9]\\d)|5(?:[0358]|[124679]\\d)|7\\d|8[458])", , , , "14023", , , [5, 6]], [, , "140(?:1(?:[035]|[16-8]\\d)|2(?:[0346]|[259]\\d)|3(?:[03568]|[124]\\d)|4(?:[0356]|[17-9]\\d)|5(?:[0358]|[124679]\\d)|7\\d|8[458])", 
, , , "14020", , , [5, 6]], , , [, , , , , , , , , [-1]]], NO:[, [, , "0\\d{4}|[2-9]\\d{7}", , , , , , , [5, 8]], [, , "(?:2[1-4]|3[1-3578]|5[1-35-7]|6[1-4679]|7[0-8])\\d{6}", , , , "21234567", , , [8]], [, , "(?:4[015-8]|5[89]|87|9\\d)\\d{6}", , , , "40612345", , , [8]], [, , "80[01]\\d{5}", , , , "80012345", , , [8]], [, , "82[09]\\d{5}", , , , "82012345", , , [8]], [, , "810(?:0[0-6]|[2-8]\\d)\\d{3}", , , , "81021234", , , [8]], [, , "880\\d{5}", , , , "88012345", , , [8]], [, , "85[0-5]\\d{5}", 
, , , "85012345", , , [8]], "NO", 47, "00", , , , , , , , [[, "([489]\\d{2})(\\d{2})(\\d{3})", "$1 $2 $3", ["[489]"]], [, "([235-7]\\d)(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[235-7]"]]], , [, , , , , , , , , [-1]], 1, , [, , , , , , , , , [-1]], [, , "0\\d{4}|81(?:0(?:0[7-9]|1\\d)|5\\d{2})\\d{3}", , , , "01234"], , , [, , "81[23]\\d{5}", , , , "81212345", , , [8]]], NP:[, [, , "[1-8]\\d{7}|9(?:[1-69]\\d{6,8}|7[2-6]\\d{5,7}|8\\d{8})", , , , , , , [8, 10], [6, 7]], [, , "(?:1[0-6]\\d|2[13-79][2-6]|3[135-8][2-6]|4[146-9][2-6]|5[135-7][2-6]|6[13-9][2-6]|7[15-9][2-6]|8[1-46-9][2-6]|9[1-79][2-6])\\d{5}", 
, , , "14567890", , , [8], [6, 7]], [, , "9(?:6[013]|7[245]|8[0-24-6])\\d{7}", , , , "9841234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "NP", 977, "00", "0", , , "0", , , , [[, "(1)(\\d{7})", "$1-$2", ["1[2-6]"], "0$1"], [, "(\\d{2})(\\d{6})", "$1-$2", ["1[01]|[2-8]|9(?:[1-69]|7[15-9])"], "0$1"], [, "(9\\d{2})(\\d{7})", "$1-$2", ["9(?:6[013]|7[245]|8)"], "$1"]], , [, , , , , , , , , [-1]], , , 
[, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], NR:[, [, , "[458]\\d{6}", , , , , , , [7]], [, , "(?:444|888)\\d{4}", , , , "4441234"], [, , "55[4-9]\\d{4}", , , , "5551234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "NR", 674, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , 
, , , , , , [-1]]], NU:[, [, , "[1-5]\\d{3}", , , , , , , [4]], [, , "[34]\\d{3}", , , , "4002"], [, , "[125]\\d{3}", , , , "1234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "NU", 683, "00", , , , , , , , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], NZ:[, [, , "6[235-9]\\d{6}|[2-57-9]\\d{7,9}", , , , , , , [8, 9, 10], [7]], [, , "(?:3[2-79]|[49][2-9]|6[235-9]|7[2-57-9])\\d{6}|24099\\d{3}", 
, , , "32345678", , , [8], [7]], [, , "2(?:[028]\\d{7,8}|1(?:[03]\\d{5,7}|[12457]\\d{5,6}|[689]\\d{5})|[79]\\d{7})", , , , "211234567"], [, , "508\\d{6,7}|80\\d{6,8}", , , , "800123456"], [, , "90\\d{6,7}", , , , "900123456", , , [8, 9]], [, , , , , , , , , [-1]], [, , "70\\d{7}", , , , "701234567", , , [9]], [, , , , , , , , , [-1]], "NZ", 64, "0(?:0|161)", "0", , , "0", , "00", , [[, "(\\d)(\\d{3})(\\d{4})", "$1-$2 $3", ["240|[346]|7[2-57-9]|9[1-9]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", 
["21"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{3,5})", "$1 $2 $3", ["2(?:1[1-9]|[69]|7[0-35-9])|70|86"], "0$1"], [, "(2\\d)(\\d{3,4})(\\d{4})", "$1 $2 $3", ["2[028]"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{3})", "$1 $2 $3", ["90"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["2(?:10|74)|5|[89]0"], "0$1"]], , [, , "[28]6\\d{6,7}", , , , "26123456", , , [8, 9]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], OM:[, [, , "(?:5|[279]\\d)\\d{6}|800\\d{5,6}", , , , 
, , , [7, 8, 9]], [, , "2[2-6]\\d{6}", , , , "23123456", , , [8]], [, , "7[19]\\d{6}|9(?:0[1-9]|[1-9]\\d)\\d{5}", , , , "92123456", , , [8]], [, , "8007\\d{4,5}|500\\d{4}", , , , "80071234"], [, , "900\\d{5}", , , , "90012345", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "OM", 968, "00", , , , , , , , [[, "(2\\d)(\\d{6})", "$1 $2", ["2"]], [, "([79]\\d{3})(\\d{4})", "$1 $2", ["[79]"]], [, "([58]00)(\\d{4,6})", "$1 $2", ["[58]00"]]], , [, , , , , , , , , 
[-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], PA:[, [, , "[1-9]\\d{6,7}", , , , , , , [7, 8]], [, , "(?:1(?:0[0-8]|1[49]|2[37]|3[0137]|4[147]|5[05]|6[58]|7[0167]|8[58]|9[139])|2(?:[0235679]\\d|1[0-7]|4[04-9]|8[028])|3(?:[09]\\d|1[014-7]|2[0-3]|3[03]|4[03-57]|55|6[068]|7[06-8]|8[06-9])|4(?:3[013-69]|4\\d|7[0-589])|5(?:[01]\\d|2[0-7]|[56]0|79)|7(?:0[09]|2[0-267]|3[06]|[469]0|5[06-9]|7[0-24-79]|8[7-9])|8(?:09|[34]\\d|5[0134]|8[02])|9(?:0[6-9]|1[016-8]|2[036-8]|3[3679]|40|5[0489]|6[06-9]|7[046-9]|8[36-8]|9[1-9]))\\d{4}", 
, , , "2001234", , , [7]], [, , "(?:1[16]1|21[89]|8(?:1[01]|7[23]))\\d{4}|6(?:[024-9]\\d|1[0-5]|3[0-24-9])\\d{5}", , , , "61234567"], [, , "800\\d{4}", , , , "8001234", , , [7]], [, , "(?:8(?:55|60|7[78])|9(?:00|81))\\d{4}", , , , "8601234", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "PA", 507, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1-$2", ["[1-57-9]"]], [, "(\\d{4})(\\d{4})", "$1-$2", ["6"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , 
[-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], PE:[, [, , "[14-9]\\d{7,8}", , , , , , , [8, 9], [6, 7]], [, , "(?:1\\d|4[1-4]|5[1-46]|6[1-7]|7[2-46]|8[2-4])\\d{6}", , , , "11234567", , , [8], [6, 7]], [, , "9\\d{8}", , , , "912345678", , , [9]], [, , "800\\d{5}", , , , "80012345", , , [8]], [, , "805\\d{5}", , , , "80512345", , , [8]], [, , "801\\d{5}", , , , "80112345", , , [8]], [, , "80[24]\\d{5}", , , , "80212345", , , [8]], [, , , , , , , , , [-1]], "PE", 51, "19(?:1[124]|77|90)00", 
"0", " Anexo ", , "0", , , , [[, "(1)(\\d{7})", "$1 $2", ["1"], "(0$1)"], [, "([4-8]\\d)(\\d{6})", "$1 $2", ["[4-7]|8[2-4]"], "(0$1)"], [, "(\\d{3})(\\d{5})", "$1 $2", ["80"], "(0$1)"], [, "(9\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["9"], "$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], PF:[, [, , "4\\d{5,7}|8\\d{7}", , , , , , , [6, 8]], [, , "4(?:[09][45689]\\d|4)\\d{4}", , , , "40412345"], [, , "8[79]\\d{6}", , , , "87123456", 
, , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "PF", 689, "00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["4[09]|8[79]"]], [, "(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3", ["44"]]], , [, , , , , , , , , [-1]], , , [, , "44\\d{4}", , , , "441234", , , [6]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], PG:[, [, , "[1-9]\\d{6,7}", , , , , , , [7, 8]], [, , "(?:3[0-2]\\d|4[257]\\d|5[34]\\d|64[1-9]|77(?:[0-24]\\d|30)|85[02-46-9]|9[78]\\d)\\d{4}", 
, , , "3123456", , , [7]], [, , "7(?:[0-689]\\d|75)\\d{5}", , , , "70123456", , , [8]], [, , "180\\d{4}", , , , "1801234", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "2(?:0[0-47]|7[568])\\d{4}", , , , "2751234", , , [7]], "PG", 675, "140[1-3]|00", , , , , , "00", , [[, "(\\d{3})(\\d{4})", "$1 $2", ["[13-689]|27"]], [, "(\\d{4})(\\d{4})", "$1 $2", ["20|7"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, 
, , , , , , , , [-1]]], PH:[, [, , "1\\d{10,12}|2\\d{5,7}|[3-7]\\d{8}|8\\d{7,9}|9\\d{9}", , , , , , , [6, 8, 9, 10, 11, 12, 13], [5, 7]], [, , "2\\d{5}(?:\\d{2})?|(?:3[2-68]|4[2-9]|5[2-6]|6[2-58]|7[24578]|8[2-8])\\d{7}|88(?:22\\d{6}|42\\d{4})", , , , "21234567", , , [6, 8, 9, 10], [5, 7]], [, , "(?:81[37]|9(?:0[5-9]|1[024-9]|2[0-35-9]|3[02-9]|4[235-9]|5[056]|6[5-7]|7[34-79]|89|9[4-9]))\\d{7}", , , , "9051234567", , , [10]], [, , "1800\\d{7,9}", , , , "180012345678", , , [11, 12, 13]], [, , , , , 
, , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "PH", 63, "00", "0", , , "0", , , , [[, "(2)(\\d{3})(\\d{4})", "$1 $2 $3", ["2"], "(0$1)"], [, "(2)(\\d{5})", "$1 $2", ["2"], "(0$1)"], [, "(\\d{4})(\\d{4,6})", "$1 $2", ["3(?:23|39|46)|4(?:2[3-6]|[35]9|4[26]|76)|5(?:22|44)|642|8(?:62|8[245])", "3(?:230|397|461)|4(?:2(?:35|[46]4|51)|396|4(?:22|63)|59[347]|76[15])|5(?:221|446)|642[23]|8(?:622|8(?:[24]2|5[13]))"], "(0$1)"], [, "(\\d{5})(\\d{4})", "$1 $2", ["346|4(?:27|9[35])|883", 
"3469|4(?:279|9(?:30|56))|8834"], "(0$1)"], [, "([3-8]\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["[3-8]"], "(0$1)"], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["81|9"], "0$1"], [, "(1800)(\\d{3})(\\d{4})", "$1 $2 $3", ["180", "1800"]], [, "(1800)(\\d{1,2})(\\d{3})(\\d{4})", "$1 $2 $3 $4", ["180", "1800"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], PK:[, [, , "1\\d{8}|[2-8]\\d{5,11}|9(?:[013-9]\\d{4,10}|2\\d(?:111\\d{6}|\\d{3,7}))", 
, , , , , , [8, 9, 10, 11, 12], [6, 7]], [, , "(?:21|42)[2-9]\\d{7}|(?:2[25]|4[0146-9]|5[1-35-7]|6[1-8]|7[14]|8[16]|91)[2-9]\\d{6}|(?:2(?:3[2358]|4[2-4]|9[2-8])|45[3479]|54[2-467]|60[468]|72[236]|8(?:2[2-689]|3[23578]|4[3478]|5[2356])|9(?:2[2-8]|3[27-9]|4[2-6]|6[3569]|9[25-8]))[2-9]\\d{5,6}|58[126]\\d{7}", , , , "2123456789", , , [9, 10], [6, 7, 8]], [, , "3(?:[014]\\d|2[0-5]|3[0-7]|55|64)\\d{7}", , , , "3012345678", , , [10]], [, , "800\\d{5}", , , , "80012345", , , [8]], [, , "900\\d{5}", , , , 
"90012345", , , [8]], [, , , , , , , , , [-1]], [, , "122\\d{6}", , , , "122044444", , , [9]], [, , , , , , , , , [-1]], "PK", 92, "00", "0", , , "0", , , , [[, "([89]00)(\\d{3})(\\d{2})", "$1 $2 $3", ["[89]00"], "0$1"], [, "(1\\d{3})(\\d{5})", "$1 $2", ["1"], "$1"], [, "(\\d{2})(\\d{7,8})", "$1 $2", ["(?:2[125]|4[0-246-9]|5[1-35-7]|6[1-8]|7[14]|8[16]|91)[2-9]"], "(0$1)"], [, "(\\d{3})(\\d{6,7})", "$1 $2", ["2[349]|45|54|60|72|8[2-5]|9[2-469]", "(?:2[349]|45|54|60|72|8[2-5]|9[2-469])\\d[2-9]"], "(0$1)"], 
[, "(58\\d{3})(\\d{5})", "$1 $2", ["58[126]"], "(0$1)"], [, "(3\\d{2})(\\d{7})", "$1 $2", ["3"], "0$1"], [, "(\\d{2})(111)(\\d{3})(\\d{3})", "$1 $2 $3 $4", ["(?:2[125]|4[0-246-9]|5[1-35-7]|6[1-8]|7[14]|8[16]|91)1", "(?:2[125]|4[0-246-9]|5[1-35-7]|6[1-8]|7[14]|8[16]|91)11", "(?:2[125]|4[0-246-9]|5[1-35-7]|6[1-8]|7[14]|8[16]|91)111"], "(0$1)"], [, "(\\d{3})(111)(\\d{3})(\\d{3})", "$1 $2 $3 $4", ["2[349]|45|54|60|72|8[2-5]|9[2-9]", "(?:2[349]|45|54|60|72|8[2-5]|9[2-9])\\d1", "(?:2[349]|45|54|60|72|8[2-5]|9[2-9])\\d11", 
"(?:2[349]|45|54|60|72|8[2-5]|9[2-9])\\d111"], "(0$1)"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "(?:2(?:[125]|3[2358]|4[2-4]|9[2-8])|4(?:[0-246-9]|5[3479])|5(?:[1-35-7]|4[2-467])|6(?:[1-8]|0[468])|7(?:[14]|2[236])|8(?:[16]|2[2-689]|3[23578]|4[3478]|5[2356])|9(?:1|22|3[27-9]|4[2-6]|6[3569]|9[2-7]))111\\d{6}", , , , "21111825888", , , [11, 12]], , , [, , , , , , , , , [-1]]], PL:[, [, , "[1-57-9]\\d{6,8}|6\\d{5,8}", , , , , , , [6, 7, 8, 9]], [, , "(?:1[2-8]|2[2-69]|3[2-4]|4[1-468]|5[24-689]|6[1-3578]|7[14-7]|8[1-79]|9[145])(?:\\d{7}|19\\d{3})", 
, , , "123456789", , , [7, 9]], [, , "(?:45|5[0137]|6[069]|7[2389]|88)\\d{7}", , , , "512345678", , , [9]], [, , "800\\d{6}", , , , "800123456", , , [9]], [, , "70[01346-8]\\d{6}", , , , "701234567", , , [9]], [, , "801\\d{6}", , , , "801234567", , , [9]], [, , , , , , , , , [-1]], [, , "39\\d{7}", , , , "391234567", , , [9]], "PL", 48, "00", , , , , , , , [[, "(\\d{3})(\\d{3})", "$1 $2", ["11[68]|64"]], [, "(\\d{5})", "$1", ["19"]], [, "(\\d{2})(\\d{2})(\\d{3})", "$1 $2 $3", ["1[2-8]|2[2-69]|3[2-4]|4[1-468]|5[24-689]|6[1-3578]|7[14-7]|8[1-79]|9[145]"]], 
[, "(\\d{3})(\\d{2})(\\d{2,3})", "$1 $2 $3", ["64"]], [, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["26|39|45|5[0137]|6[0469]|7[02389]|8[08]"]], [, "(\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[14]|2[0-57-9]|3[2-4]|5[24-689]|6[1-3578]|7[14-7]|8[1-79]|9[145]"]]], , [, , "64\\d{4,7}", , , , "641234567"], , , [, , , , , , , , , [-1]], [, , "804\\d{6}", , , , "804123456", , , [9]], , , [, , , , , , , , , [-1]]], PM:[, [, , "[45]\\d{5}", , , , , , , [6]], [, , "41\\d{4}", , , , "411234"], [, , "(?:40|55)\\d{4}", 
, , , "551234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "PM", 508, "00", "0", , , "0", , , , [[, "([45]\\d)(\\d{2})(\\d{2})", "$1 $2 $3", ["[45]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], PR:[, [, , "[5789]\\d{9}", , , , , , , [10], [7]], [, , "(?:787|939)[2-9]\\d{6}", , , , "7872345678", , , , [7]], [, , "(?:787|939)[2-9]\\d{6}", 
, , , "7872345678", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002345678"], [, , "900[2-9]\\d{6}", , , , "9002345678"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "PR", 1, "011", "1", , , "1", , , 1, , , [, , , , , , , , , [-1]], , "787|939", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], PS:[, [, , "1\\d{9}|[24589]\\d{7,8}", , , , , , , [8, 9, 10], [7]], [, , "(?:22[234789]|42[45]|82[01458]|92[369])\\d{5}", 
, , , "22234567", , , [8], [7]], [, , "5[69]\\d{7}", , , , "599123456", , , [9]], [, , "1800\\d{6}", , , , "1800123456", , , [10]], [, , , , , , , , , [-1]], [, , "1700\\d{6}", , , , "1700123456", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "PS", 970, "00", "0", , , "0", , , , [[, "([2489])(2\\d{2})(\\d{4})", "$1 $2 $3", ["[2489]2"], "0$1"], [, "(5[69]\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["5[69]"], "0$1"], [, "(1[78]00)(\\d{3})(\\d{3})", "$1 $2 $3", ["1[78]0", "1[78]00"], "$1"]], 
, [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], PT:[, [, , "[2-46-9]\\d{8}", , , , , , , [9]], [, , "2(?:[12]\\d|[35][1-689]|4[1-59]|6[1-35689]|7[1-9]|8[1-69]|9[1256])\\d{6}", , , , "212345678"], [, , "9(?:[1236]\\d{2}|480)\\d{5}", , , , "912345678"], [, , "80[02]\\d{6}", , , , "800123456"], [, , "6(?:0[178]|4[68])\\d{6}|76(?:0[1-57]|1[2-47]|2[237])\\d{5}", , , , "760123456"], [, , "80(?:8\\d|9[1579])\\d{5}", , , , "808123456"], [, 
, "884[0-4689]\\d{5}", , , , "884123456"], [, , "30\\d{7}", , , , "301234567"], "PT", 351, "00", , , , , , , , [[, "(2\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["2[12]"]], [, "([2-46-9]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["2[3-9]|[346-9]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "7(?:0(?:7\\d|8[17]))\\d{5}", , , , "707123456"], , , [, , "600\\d{6}", , , , "600110000"]], PW:[, [, , "[2-8]\\d{6}", , , , , , , [7]], [, , "2552255|(?:277|345|488|5(?:35|44|87)|6(?:22|54|79)|7(?:33|47)|8(?:24|55|76))\\d{4}", 
, , , "2771234"], [, , "(?:6[234689]0|77[45789])\\d{4}", , , , "6201234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "PW", 680, "01[12]", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], PY:[, [, , "5[0-5]\\d{4,7}|[2-46-9]\\d{5,8}", , , , , , , [6, 7, 8, 9], [5]], [, , "(?:[26]1|3[289]|4[124678]|7[123]|8[1236])\\d{5,7}|(?:2(?:2[4568]|7[15]|9[1-5])|3(?:18|3[167]|4[2357]|51)|4(?:18|2[45]|3[12]|5[13]|64|71|9[1-47])|5(?:[1-4]\\d|5[0234])|6(?:3[1-3]|44|7[1-4678])|7(?:17|4[0-4]|6[1-578]|75|8[0-8])|858)\\d{5,6}", 
, , , "212345678", , , [7, 8, 9], [5, 6]], [, , "9(?:6[12]|[78][1-6]|9[1-5])\\d{6}", , , , "961456789", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "8700[0-4]\\d{4}", , , , "870012345", , , [9]], "PY", 595, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{5})", "$1 $2", ["[26]1|3[289]|4[1246-8]|7[1-3]|8[1-36]"], "(0$1)"], [, "(\\d{2})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["[26]1|3[289]|4[1246-8]|7[1-3]|8[1-36]"], "(0$1)"], [, "(\\d{3})(\\d{3,6})", 
"$1 $2", ["[2-9]0"], "0$1"], [, "(\\d{3})(\\d{6})", "$1 $2", ["9[1-9]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["870", "8700"]], [, "(\\d{3})(\\d{4,5})", "$1 $2", ["[2-8][1-9]"], "(0$1)"], [, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["[2-8][1-9]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "[2-9]0\\d{4,7}", , , , "201234567"], , , [, , , , , , , , , [-1]]], QA:[, [, , "[2-8]\\d{6,7}", , , , , , , [7, 8]], [, , "4[04]\\d{6}", , , , "44123456", , , [8]], [, 
, "[3567]\\d{7}", , , , "33123456", , , [8]], [, , "800\\d{4}", , , , "8001234", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "QA", 974, "00", , , , , , , , [[, "([28]\\d{2})(\\d{4})", "$1 $2", ["[28]"]], [, "([3-7]\\d{3})(\\d{4})", "$1 $2", ["[3-7]"]]], , [, , "2(?:[12]\\d|61)\\d{4}", , , , "2123456", , , [7]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], RE:[, [, , "[268]\\d{8}", , , , 
, , , [9]], [, , "262\\d{6}", , , , "262161234"], [, , "69(?:2\\d{2}|3(?:0[0-46]|1[013]|2[0-2]|3[039]|4[0-7]|5[05]|6[06]|7[07]|8[0-38]|9[0-479]))\\d{4}", , , , "692123456"], [, , "80\\d{7}", , , , "801234567"], [, , "89[1-37-9]\\d{6}", , , , "891123456"], [, , "8(?:1[019]|2[0156]|84|90)\\d{6}", , , , "810123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "RE", 262, "00", "0", , , "0", , , , [[, "([268]\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[268]"], "0$1"]], , [, , , , , , , 
, , [-1]], 1, "262|69|8", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], RO:[, [, , "[23]\\d{5,8}|[7-9]\\d{8}", , , , , , , [6, 9]], [, , "2(?:1(?:\\d{7}|9\\d{3})|[3-6](?:\\d{7}|\\d9\\d{2}))|3(?:1\\d{4}(?:\\d{3})?|[3-6]\\d{7})", , , , "211234567"], [, , "7(?:[0-8]\\d{2}|99\\d)\\d{5}", , , , "712345678", , , [9]], [, , "800\\d{6}", , , , "800123456", , , [9]], [, , "90[036]\\d{6}", , , , "900123456", , , [9]], [, , "801\\d{6}", , , , "801123456", , , [9]], [, , 
, , , , , , , [-1]], [, , , , , , , , , [-1]], "RO", 40, "00", "0", " int ", , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["[23]1"], "0$1"], [, "(\\d{2})(\\d{4})", "$1 $2", ["[23]1"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["[23][3-7]|[7-9]"], "0$1"], [, "(2\\d{2})(\\d{3})", "$1 $2", ["2[3-6]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "37\\d{7}", , , , "372123456", , , [9]], , , [, , , , , , , , , [-1]]], RS:[, [, , "[126-9]\\d{4,11}|3(?:[0-79]\\d{3,10}|8[2-9]\\d{2,9})", 
, , , , , , [6, 7, 8, 9, 10, 11, 12], [5]], [, , "(?:1(?:[02-9][2-9]|1[1-9])\\d|2(?:[0-24-7][2-9]\\d|[389](?:0[2-9]|[2-9]\\d))|3(?:[0-8][2-9]\\d|9(?:[2-9]\\d|0[2-9])))\\d{3,8}", , , , "10234567", , , [7, 8, 9, 10, 11, 12], [5, 6]], [, , "6(?:[0-689]|7\\d)\\d{6,7}", , , , "601234567", , , [8, 9, 10]], [, , "800\\d{3,9}", , , , "80012345"], [, , "(?:90[0169]|78\\d)\\d{3,7}", , , , "90012345"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "RS", 381, "00", "0", , , "0", 
, , , [[, "([23]\\d{2})(\\d{4,9})", "$1 $2", ["(?:2[389]|39)0"], "0$1"], [, "([1-3]\\d)(\\d{5,10})", "$1 $2", ["1|2(?:[0-24-7]|[389][1-9])|3(?:[0-8]|9[1-9])"], "0$1"], [, "(6\\d)(\\d{6,8})", "$1 $2", ["6"], "0$1"], [, "([89]\\d{2})(\\d{3,9})", "$1 $2", ["[89]"], "0$1"], [, "(7[26])(\\d{4,9})", "$1 $2", ["7[26]"], "0$1"], [, "(7[08]\\d)(\\d{4,9})", "$1 $2", ["7[08]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "7[06]\\d{4,10}", , , , "700123456"], , , [, , , , , , , , 
, [-1]]], RU:[, [, , "[347-9]\\d{9}", , , , , , , [10]], [, , "(?:3(?:0[12]|4[1-35-79]|5[1-3]|65|8[1-58]|9[0145])|4(?:01|1[1356]|2[13467]|7[1-5]|8[1-7]|9[1-689])|8(?:1[1-8]|2[01]|3[13-6]|4[0-8]|5[15]|6[1-35-79]|7[1-37-9]))\\d{7}", , , , "3011234567"], [, , "9\\d{9}", , , , "9123456789"], [, , "80[04]\\d{7}", , , , "8001234567"], [, , "80[39]\\d{7}", , , , "8091234567"], [, , , , , , , , , [-1]], [, , "808\\d{7}", , , , "8081234567"], [, , , , , , , , , [-1]], "RU", 7, "810", "8", , , "8", , "8~10", 
, [[, "(\\d{3})(\\d{2})(\\d{2})", "$1-$2-$3", ["[1-79]"], "$1", , 1], [, "([3489]\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1 $2-$3-$4", ["[3489]"], "8 ($1)", , 1], [, "(7\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["7"], "8 ($1)", , 1]], [[, "([3489]\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1 $2-$3-$4", ["[3489]"], "8 ($1)", , 1], [, "(7\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["7"], "8 ($1)", , 1]], [, , , , , , , , , [-1]], 1, , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], RW:[, 
[, , "[027-9]\\d{7,8}", , , , , , , [8, 9]], [, , "2[258]\\d{7}|06\\d{6}", , , , "250123456"], [, , "7[238]\\d{7}", , , , "720123456", , , [9]], [, , "800\\d{6}", , , , "800123456", , , [9]], [, , "900\\d{6}", , , , "900123456", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "RW", 250, "00", "0", , , "0", , , , [[, "(2\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["2"]], [, "([7-9]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["[7-9]"], "0$1"], [, "(0\\d)(\\d{2})(\\d{2})(\\d{2})", 
"$1 $2 $3 $4", ["0"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SA:[, [, , "1\\d{7,8}|(?:[2-467]|92)\\d{7}|5\\d{8}|8\\d{9}", , , , , , , [8, 9, 10], [7]], [, , "11\\d{7}|1?(?:2[24-8]|3[35-8]|4[3-68]|6[2-5]|7[235-7])\\d{6}", , , , "112345678", , , [8, 9], [7]], [, , "(?:5(?:[013-689]\\d|7[0-36-8])|811\\d)\\d{6}", , , , "512345678", , , [9, 10]], [, , "800\\d{7}", , , , "8001234567", , , [10]], [, , , , , , , , , [-1]], [, , 
"92[05]\\d{6}", , , , "920012345", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "SA", 966, "00", "0", , , "0", , , , [[, "([1-467])(\\d{3})(\\d{4})", "$1 $2 $3", ["[1-467]"], "0$1"], [, "(1\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["1[1-467]"], "0$1"], [, "(5\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["5"], "0$1"], [, "(92\\d{2})(\\d{5})", "$1 $2", ["92"], "$1"], [, "(800)(\\d{3})(\\d{4})", "$1 $2 $3", ["800"], "$1"], [, "(811)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["811"], "0$1"]], , [, , , , , , , 
, , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SB:[, [, , "[1-9]\\d{4,6}", , , , , , , [5, 7]], [, , "(?:1[4-79]|[23]\\d|4[0-2]|5[03]|6[0-37])\\d{3}", , , , "40123", , , [5]], [, , "48\\d{3}|7(?:30|[46-8]\\d|5[025-9]|9[0-5])\\d{4}|8[4-9]\\d{5}|9(?:1[2-9]|2[013-9]|3[0-2]|[46]\\d|5[0-46-9]|7[0-689]|8[0-79]|9[0-8])\\d{4}", , , , "7421234"], [, , "1[38]\\d{3}", , , , "18123", , , [5]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , 
, , , [-1]], [, , "5[12]\\d{3}", , , , "51123", , , [5]], "SB", 677, "0[01]", , , , , , , , [[, "(\\d{2})(\\d{5})", "$1 $2", ["[7-9]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SC:[, [, , "[24689]\\d{5,6}", , , , , , , [7]], [, , "4[2-46]\\d{5}", , , , "4217123"], [, , "2[5-8]\\d{5}", , , , "2510123"], [, , "8000\\d{3}", , , , "8000000"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "(?:64\\d|971)\\d{4}", 
, , , "6412345"], "SC", 248, "0(?:[02]|10?)", , , , , , "00", , [[, "(\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["[246]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SD:[, [, , "[19]\\d{8}", , , , , , , [9]], [, , "1(?:[125]\\d|8[3567])\\d{6}", , , , "121231234"], [, , "9[0-3569]\\d{7}", , , , "911231234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], 
"SD", 249, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", , "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SE:[, [, , "[1-35-9]\\d{5,11}|4\\d{6,8}", , , , , , , [6, 7, 8, 9, 10, 12]], [, , "1(?:0[1-8]\\d{6}|[136]\\d{5,7}|(?:2[0-35]|4[0-4]|5[0-25-9]|7[13-6]|[89]\\d)\\d{5,6})|2(?:[136]\\d{5,7}|(?:2[0-7]|4[0136-8]|5[0138]|7[018]|8[01]|9[0-57])\\d{5,6})|3(?:[356]\\d{5,7}|(?:0[0-4]|1\\d|2[0-25]|4[056]|7[0-2]|8[0-3]|9[023])\\d{5,6})|4(?:[0246]\\d{5,7}|(?:1[013-8]|3[0135]|5[14-79]|7[0-246-9]|8[0156]|9[0-689])\\d{5,6})|5(?:0[0-6]|[15][0-5]|2[0-68]|3[0-4]|4\\d|6[03-5]|7[013]|8[0-79]|9[01])\\d{5,6}|6(?:[03]\\d{5,7}|(?:1[1-3]|2[0-4]|4[02-57]|5[0-37]|6[0-3]|7[0-2]|8[0247]|9[0-356])\\d{5,6})|8\\d{6,8}|9(?:0[1-9]\\d{4,6}|(?:1[0-68]|2\\d|3[02-5]|4[0-3]|5[0-4]|[68][01]|7[0135-8])\\d{5,6})", 
, , , "8123456", , , [7, 8, 9]], [, , "7[02369]\\d{7}", , , , "701234567", , , [9]], [, , "20\\d{4,7}", , , , "20123456", , , [6, 7, 8, 9]], [, , "649\\d{6}|9(?:00|39|44)[1-8]\\d{3,6}", , , , "9001234567", , , [7, 8, 9, 10]], [, , "77(?:0\\d{3}(?:\\d{3})?|[1-7]\\d{6})", , , , "771234567", , , [6, 9]], [, , "75[1-8]\\d{6}", , , , "751234567", , , [9]], [, , , , , , , , , [-1]], "SE", 46, "00", "0", , , "0", , , , [[, "(8)(\\d{2,3})(\\d{2,3})(\\d{2})", "$1-$2 $3 $4", ["8"], "0$1"], [, "([1-69]\\d)(\\d{2,3})(\\d{2})(\\d{2})", 
"$1-$2 $3 $4", ["1[013689]|2[0136]|3[1356]|4[0246]|54|6[03]|90"], "0$1"], [, "([1-469]\\d)(\\d{3})(\\d{2})", "$1-$2 $3", ["[12][136]|3[356]|4[0246]|6[03]|90"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1-$2 $3 $4", ["1[2457]|2(?:[247-9]|5[0138])|3[0247-9]|4[1357-9]|5[0-35-9]|6(?:[124-689]|7[0-2])|9(?:[125-8]|3[0-5]|4[0-3])"], "0$1"], [, "(\\d{3})(\\d{2,3})(\\d{2})", "$1-$2 $3", ["1[2457]|2(?:[247-9]|5[0138])|3[0247-9]|4[1357-9]|5[0-35-9]|6(?:[124-689]|7[0-2])|9(?:[125-8]|3[0-5]|4[0-3])"], 
"0$1"], [, "(7\\d)(\\d{3})(\\d{2})(\\d{2})", "$1-$2 $3 $4", ["7"], "0$1"], [, "(77)(\\d{2})(\\d{2})", "$1-$2$3", ["77"], "0$1"], [, "(20)(\\d{2,3})(\\d{2})", "$1-$2 $3", ["20"], "0$1"], [, "(9[034]\\d)(\\d{2})(\\d{2})(\\d{3})", "$1-$2 $3 $4", ["9[034]"], "0$1"], [, "(9[034]\\d)(\\d{4})", "$1-$2", ["9[034]"], "0$1"], [, "(\\d{3})(\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1-$2 $3 $4 $5", ["25[245]|67[3-6]"], "0$1"]], [[, "(8)(\\d{2,3})(\\d{2,3})(\\d{2})", "$1 $2 $3 $4", ["8"]], [, "([1-69]\\d)(\\d{2,3})(\\d{2})(\\d{2})", 
"$1 $2 $3 $4", ["1[013689]|2[0136]|3[1356]|4[0246]|54|6[03]|90"]], [, "([1-469]\\d)(\\d{3})(\\d{2})", "$1 $2 $3", ["[12][136]|3[356]|4[0246]|6[03]|90"]], [, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["1[2457]|2(?:[247-9]|5[0138])|3[0247-9]|4[1357-9]|5[0-35-9]|6(?:[124-689]|7[0-2])|9(?:[125-8]|3[0-5]|4[0-3])"]], [, "(\\d{3})(\\d{2,3})(\\d{2})", "$1 $2 $3", ["1[2457]|2(?:[247-9]|5[0138])|3[0247-9]|4[1357-9]|5[0-35-9]|6(?:[124-689]|7[0-2])|9(?:[125-8]|3[0-5]|4[0-3])"]], [, "(7\\d)(\\d{3})(\\d{2})(\\d{2})", 
"$1 $2 $3 $4", ["7"]], [, "(77)(\\d{2})(\\d{2})", "$1 $2 $3", ["77"]], [, "(20)(\\d{2,3})(\\d{2})", "$1 $2 $3", ["20"]], [, "(9[034]\\d)(\\d{2})(\\d{2})(\\d{3})", "$1 $2 $3 $4", ["9[034]"]], [, "(9[034]\\d)(\\d{4})", "$1 $2", ["9[034]"]], [, "(\\d{3})(\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4 $5", ["25[245]|67[3-6]"]]], [, , "74[02-9]\\d{6}", , , , "740123456", , , [9]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , "(?:25[245]|67[3-6])\\d{9}", , , , "254123456789", , , [12]]], 
SG:[, [, , "[36]\\d{7}|[17-9]\\d{7,10}", , , , , , , [8, 10, 11]], [, , "6[1-9]\\d{6}", , , , "61234567", , , [8]], [, , "(?:8[1-8]|9[0-8])\\d{6}", , , , "81234567", , , [8]], [, , "1?800\\d{7}", , , , "18001234567", , , [10, 11]], [, , "1900\\d{7}", , , , "19001234567", , , [11]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "3[12]\\d{6}", , , , "31234567", , , [8]], "SG", 65, "0[0-3]\\d", , , , , , , , [[, "([3689]\\d{3})(\\d{4})", "$1 $2", ["[369]|8[1-9]"]], [, "(1[89]00)(\\d{3})(\\d{4})", 
"$1 $2 $3", ["1[89]0", "1[89]00"]], [, "(7000)(\\d{4})(\\d{3})", "$1 $2 $3", ["700", "7000"]], [, "(800)(\\d{3})(\\d{4})", "$1 $2 $3", ["800"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "7000\\d{7}", , , , "70001234567", , , [11]], , , [, , , , , , , , , [-1]]], SH:[, [, , "[256]\\d{4}|8\\d{3}", , , , , , , [4, 5]], [, , "2(?:[0-57-9]\\d|6[4-9])\\d{2}", , , , "22158"], [, , "[56]\\d{4}", , , , "51234", , , [5]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , 
, , , , [-1]], [, , , , , , , , , [-1]], [, , "262\\d{2}", , , , "26212", , , [5]], "SH", 290, "00", , , , , , , , , , [, , , , , , , , , [-1]], 1, , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SI:[, [, , "[1-7]\\d{6,7}|[89]\\d{4,7}", , , , , , , [5, 6, 7, 8]], [, , "(?:1\\d|[25][2-8]|3[24-8]|4[24-8]|7[3-8])\\d{6}", , , , "11234567", , , [8], [7]], [, , "(?:[37][01]\\d|4[0139]\\d|51\\d|6(?:[48]\\d|9[69]))\\d{5}", , , , "31234567", , , [8]], [, , "80\\d{4,6}", 
, , , "80123456", , , [6, 7, 8]], [, , "90\\d{4,6}|89[1-3]\\d{2,5}", , , , "90123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "(?:59|8[1-3])\\d{6}", , , , "59012345", , , [8]], "SI", 386, "00", "0", , , "0", , , , [[, "(\\d)(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[12]|[34][24-8]|5[2-8]|7[3-8]"], "(0$1)"], [, "([3-7]\\d)(\\d{3})(\\d{3})", "$1 $2 $3", ["[37][01]|4[0139]|51|6"], "0$1"], [, "([89][09])(\\d{3,6})", "$1 $2", ["[89][09]"], "0$1"], [, "([58]\\d{2})(\\d{5})", "$1 $2", 
["59|8[1-3]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SJ:[, [, , "0\\d{4}|[45789]\\d{7}", , , , , , , [5, 8]], [, , "79\\d{6}", , , , "79123456", , , [8]], [, , "(?:4[015-8]|5[89]|9\\d)\\d{6}", , , , "41234567", , , [8]], [, , "80[01]\\d{5}", , , , "80012345", , , [8]], [, , "82[09]\\d{5}", , , , "82012345", , , [8]], [, , "810(?:0[0-6]|[2-8]\\d)\\d{3}", , , , "81021234", , , [8]], [, , "880\\d{5}", , , , "88012345", 
, , [8]], [, , "85[0-5]\\d{5}", , , , "85012345", , , [8]], "SJ", 47, "00", , , , , , , , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "0\\d{4}|81(?:0(?:0[7-9]|1\\d)|5\\d{2})\\d{3}", , , , "01234"], , , [, , "81[23]\\d{5}", , , , "81212345", , , [8]]], SK:[, [, , "(?:[2-68]\\d{5,8}|9\\d{6,8})", , , , , , , [6, 7, 9]], [, , "2(?:1(?:6\\d{3,4}|7\\d{3})|[2-9]\\d{7})|[3-5][1-8](?:1(?:6\\d{2,3}|7\\d{3})|\\d{7})", , , , "221234567"], [, , "9(?:0(?:[1-8]\\d|9[1-9])|(?:1[0-24-9]|[45]\\d)\\d)\\d{5}", 
, , , "912123456", , , [9]], [, , "800\\d{6}", , , , "800123456", , , [9]], [, , "9(?:[78]\\d{7}|00\\d{6})", , , , "900123456", , , [9]], [, , "8[5-9]\\d{7}", , , , "850123456", , , [9]], [, , , , , , , , , [-1]], [, , "6(?:02|5[0-4]|9[0-6])\\d{6}", , , , "690123456", , , [9]], "SK", 421, "00", "0", , , "0", , , , [[, "(2)(1[67])(\\d{3,4})", "$1 $2 $3", ["21[67]"], "0$1"], [, "([3-5]\\d)(\\d{2})(\\d{2,3})", "$1 $2 $3", ["[3-5]"], "0$1"], [, "(2)(\\d{3})(\\d{3})(\\d{2})", "$1/$2 $3 $4", ["2"], "0$1"], 
[, "([3-5]\\d)(\\d{3})(\\d{2})(\\d{2})", "$1/$2 $3 $4", ["[3-5]"], "0$1"], [, "([689]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["[689]"], "0$1"], [, "(9090)(\\d{3})", "$1 $2", ["909", "9090"], "0$1"]], , [, , "9090\\d{3}", , , , "9090123", , , [7]], , , [, , "(?:602|8(?:00|[5-9]\\d)|9(?:00|[78]\\d))\\d{6}|9090\\d{3}", , , , "800123456", , , [7, 9]], [, , "96\\d{7}", , , , "961234567", , , [9]], , , [, , , , , , , , , [-1]]], SL:[, [, , "[2-9]\\d{7}", , , , , , , [8], [6]], [, , "[235]2[2-4][2-9]\\d{4}", 
, , , "22221234", , , , [6]], [, , "(?:2[15]|3[03-5]|4[04]|5[05]|66|7[6-9]|8[08]|99)\\d{6}", , , , "25123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "SL", 232, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{6})", "$1 $2", , "(0$1)"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SM:[, [, , "[05-7]\\d{7,9}", , , , , , , [8, 10], [6]], [, , 
"0549(?:8[0157-9]|9\\d)\\d{4}", , , , "0549886377", , , [10], [6]], [, , "6[16]\\d{6}", , , , "66661212", , , [8]], [, , , , , , , , , [-1]], [, , "7[178]\\d{6}", , , , "71123456", , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "5[158]\\d{6}", , , , "58001110", , , [8]], "SM", 378, "00", , , , "(?:0549)?([89]\\d{5})", "0549$1", , , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[5-7]"]], [, "(0549)(\\d{6})", "$1 $2", ["054", "0549"]], [, "(\\d{6})", "0549 $1", ["[89]"]]], 
[[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[5-7]"]], [, "(0549)(\\d{6})", "($1) $2", ["054", "0549"]], [, "(\\d{6})", "(0549) $1", ["[89]"]]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SN:[, [, , "[3789]\\d{8}", , , , , , , [9]], [, , "3(?:0(?:1[0-2]|80)|282|3(?:8[1-9]|9[3-9])|611)\\d{5}", , , , "301012345"], [, , "7(?:[06-8]\\d|21|90)\\d{6}", , , , "701234567"], [, , "800\\d{6}", , , , "800123456"], [, , "88[4689]\\d{6}", 
, , , "884123456"], [, , "81[02468]\\d{6}", , , , "810123456"], [, , , , , , , , , [-1]], [, , "39[01]\\d{6}|3392\\d{5}|93330\\d{4}", , , , "933301234"], "SN", 221, "00", , , , , , , , [[, "(\\d{2})(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[379]"]], [, "(\\d{3})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["8"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SO:[, [, , "[1-9]\\d{5,8}", , , , , , , [6, 7, 8, 9]], [, , "(?:1\\d{1,2}|2[0-79]\\d|3[0-46-8]?\\d|4[0-7]?\\d|59\\d|8[125])\\d{4}", 
, , , "4012345", , , [6, 7]], [, , "(?:15\\d|2(?:4\\d|8)|3[59]\\d{2}|4[89]\\d{2}|6[1-9]?\\d{2}|7(?:[1-8]\\d|9\\d{1,2})|8[08]\\d{2}|9(?:0[67]|[2-9])\\d)\\d{5}", , , , "71123456", , , [7, 8, 9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "SO", 252, "00", "0", , , "0", , , , [[, "(\\d{6})", "$1", ["[134]"]], [, "(\\d)(\\d{6})", "$1 $2", ["[13-5]|2[0-79]"]], [, "(\\d)(\\d{7})", "$1 $2", ["24|[67]"]], [, "(\\d{2})(\\d{4})", 
"$1 $2", ["8[125]"]], [, "(\\d{2})(\\d{5,7})", "$1 $2", ["15|28|6[1-35-9]|799|9[2-9]"]], [, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["3[59]|4[89]|6[24-6]|79|8[08]|90"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SR:[, [, , "[2-8]\\d{5,6}", , , , , , , [6, 7]], [, , "(?:2[1-3]|3[0-7]|4\\d|5[2-58]|68\\d)\\d{4}", , , , "211234"], [, , "(?:7[124-7]|8[125-9])\\d{5}", , , , "7412345", , , [7]], [, , , , , , , , , [-1]], [, , , , 
, , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "56\\d{4}", , , , "561234", , , [6]], "SR", 597, "00", , , , , , , , [[, "(\\d{3})(\\d{3})", "$1-$2", ["[2-4]|5[2-58]"]], [, "(\\d{2})(\\d{2})(\\d{2})", "$1-$2-$3", ["56"]], [, "(\\d{3})(\\d{4})", "$1-$2", ["[6-8]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SS:[, [, , "[19]\\d{8}", , , , , , , [9]], [, , "18\\d{7}", , , , "181234567"], [, , "(?:12|9[1257])\\d{7}", 
, , , "977123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "SS", 211, "00", "0", , , "0", , , , [[, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", , "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], ST:[, [, , "[29]\\d{6}", , , , , , , [7]], [, , "22\\d{5}", , , , "2221234"], [, , "9(?:0(?:0[5-9]|[1-9]\\d)|[89]\\d{2})\\d{3}", , , , "9812345"], 
[, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "ST", 239, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SV:[, [, , "[267]\\d{7}|[89]\\d{6}(?:\\d{4})?", , , , , , , [7, 8, 11]], [, , "2[1-6]\\d{6}", , , , "21234567", , , [8]], [, , "[67]\\d{7}", , , , "70123456", , , [8]], [, , "800\\d{4}(?:\\d{4})?", 
, , , "8001234", , , [7, 11]], [, , "900\\d{4}(?:\\d{4})?", , , , "9001234", , , [7, 11]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "SV", 503, "00", , , , , , , , [[, "(\\d{4})(\\d{4})", "$1 $2", ["[267]"]], [, "(\\d{3})(\\d{4})", "$1 $2", ["[89]"]], [, "(\\d{3})(\\d{4})(\\d{4})", "$1 $2 $3", ["[89]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SX:[, [, , "[5789]\\d{9}", , , , , , , [10], 
[7]], [, , "7215(?:4[2-8]|8[239]|9[056])\\d{4}", , , , "7215425678", , , , [7]], [, , "7215(?:1[02]|2\\d|5[034679]|8[014-8])\\d{4}", , , , "7215205678", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002123456"], [, , "900[2-9]\\d{6}", , , , "9002123456"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "SX", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "721", [, , , , , , , , , [-1]], [, , , 
, , , , , , [-1]], , , [, , , , , , , , , [-1]]], SY:[, [, , "[1-59]\\d{7,8}", , , , , , , [8, 9], [6, 7]], [, , "(?:1(?:1\\d?|4\\d|[2356])|2(?:1\\d?|[235])|3(?:[13]\\d|4)|4[13]|5[1-3])\\d{6}", , , , "112345678", , , , [6, 7]], [, , "9(?:22|[3-589]\\d|6[024-9])\\d{6}", , , , "944567890", , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "SY", 963, "00", "0", , , "0", , , , [[, "(\\d{2})(\\d{3})(\\d{3,4})", "$1 $2 $3", 
["[1-5]"], "0$1", , 1], [, "(9\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["9"], "0$1", , 1]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], SZ:[, [, , "[027]\\d{7}", , , , , , , [8]], [, , "2[2-9]\\d{6}", , , , "22171234"], [, , "7[6-9]\\d{6}", , , , "76123456"], [, , "0800\\d{4}", , , , "08001234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "SZ", 268, "00", , , , , , , , 
[[, "(\\d{4})(\\d{4})", "$1 $2", ["[027]"]]], , [, , , , , , , , , [-1]], , , [, , "0800\\d{4}", , , , "08001234"], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TA:[, [, , "8\\d{3}", , , , , , , [4]], [, , "8\\d{3}", , , , "8999"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "TA", 290, "00", , , , , , , , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , 
, , , , [-1]], , , [, , , , , , , , , [-1]]], TC:[, [, , "[5689]\\d{9}", , , , , , , [10], [7]], [, , "649(?:712|9(?:4\\d|50))\\d{4}", , , , "6497121234", , , , [7]], [, , "649(?:2(?:3[129]|4[1-7])|3(?:3[1-389]|4[1-8])|4[34][1-3])\\d{4}", , , , "6492311234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002345678"], [, , "900[2-9]\\d{6}", , , , "9002345678"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , "64971[01]\\d{4}", , , 
, "6497101234", , , , [7]], "TC", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "649", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TD:[, [, , "[2679]\\d{7}", , , , , , , [8]], [, , "22(?:[3789]0|5[0-5]|6[89])\\d{4}", , , , "22501234"], [, , "(?:6[023568]\\d|77\\d|9\\d{2})\\d{5}", , , , "63012345"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "TD", 235, "00|16", 
, , , , , "00", , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TG:[, [, , "[29]\\d{7}", , , , , , , [8]], [, , "2(?:2[2-7]|3[23]|44|55|66|77)\\d{5}", , , , "22212345"], [, , "9[0-36-9]\\d{6}", , , , "90112345"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "TG", 228, "00", , , , , , , , [[, 
"(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[29]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TH:[, [, , "1\\d{8,9}|[2-9]\\d{7,8}", , , , , , , [8, 9, 10]], [, , "(?:2\\d|3[2-9]|4[2-5]|5[2-6]|7[3-7])\\d{6}", , , , "21234567", , , [8]], [, , "(?:14|6[1-6]|[89]\\d)\\d{7}", , , , "812345678", , , [9]], [, , "1800\\d{6}", , , , "1800123456", , , [10]], [, , "1900\\d{6}", , , , "1900123456", , , [10]], [, , , , , , , , 
, [-1]], [, , , , , , , , , [-1]], [, , "6[08]\\d{7}", , , , "601234567", , , [9]], "TH", 66, "00", "0", , , "0", , , , [[, "(2)(\\d{3})(\\d{4})", "$1 $2 $3", ["2"], "0$1"], [, "([13-9]\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["14|[3-9]"], "0$1"], [, "(1[89]00)(\\d{3})(\\d{3})", "$1 $2 $3", ["1[89]0", "1[89]00"], "$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TJ:[, [, , "[3-57-9]\\d{8}", , , , , , , [9], [3, 5, 7]], [, , "(?:3(?:1[3-5]|2[245]|3[12]|4[24-7]|5[25]|72)|4(?:46|74|87))\\d{6}", 
, , , "372123456", , , , [3, 5, 7]], [, , "(?:41[18]|(?:5[05]|77|88|9[0-35-9])\\d)\\d{6}", , , , "917123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "TJ", 992, "810", "8", , , "8", , "8~10", , [[, "([349]\\d{2})(\\d{2})(\\d{4})", "$1 $2 $3", ["[34]7|91[78]"], , , 1], [, "([457-9]\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["4[148]|[578]|9(?:[0235-9]|1[59])"], , , 1], [, "(331700)(\\d)(\\d{2})", "$1 $2 $3", ["331", 
"3317", "33170", "331700"], , , 1], [, "(\\d{4})(\\d)(\\d{4})", "$1 $2 $3", ["3[1-5]", "3(?:[1245]|3(?:[02-9]|1[0-589]))"], , , 1]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TK:[, [, , "[2-47]\\d{3,6}", , , , , , , [4, 5, 6, 7]], [, , "(?:2[2-4]|[34]\\d)\\d{2,5}", , , , "3101"], [, , "7[2-4]\\d{2,5}", , , , "7290"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , 
, , , , , , , [-1]], "TK", 690, "00", , , , , , , , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TL:[, [, , "[2-489]\\d{6}|7\\d{6,7}", , , , , , , [7, 8]], [, , "(?:2[1-5]|3[1-9]|4[1-4])\\d{5}", , , , "2112345", , , [7]], [, , "7[3-8]\\d{6}", , , , "77212345", , , [8]], [, , "80\\d{5}", , , , "8012345", , , [7]], [, , "90\\d{5}", , , , "9012345", , , [7]], [, , , , , , , , , [-1]], [, , "70\\d{5}", , , , "7012345", , , [7]], 
[, , , , , , , , , [-1]], "TL", 670, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2", ["[2-489]|70"]], [, "(\\d{4})(\\d{4})", "$1 $2", ["7[3-8]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TM:[, [, , "[1-6]\\d{7}", , , , , , , [8]], [, , "(?:1(?:2\\d|3[1-9])|2(?:22|4[0-35-8])|3(?:22|4[03-9])|4(?:22|3[128]|4\\d|6[15])|5(?:22|5[7-9]|6[014-689]))\\d{5}", , , , "12345678"], [, , "6[1-9]\\d{6}", , , , "66123456"], [, , , , 
, , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "TM", 993, "810", "8", , , "8", , "8~10", , [[, "(\\d{2})(\\d{2})(\\d{2})(\\d{2})", "$1 $2-$3-$4", ["12"], "(8 $1)"], [, "(\\d{2})(\\d{6})", "$1 $2", ["6"], "8 $1"], [, "(\\d{3})(\\d)(\\d{2})(\\d{2})", "$1 $2-$3-$4", ["13|[2-5]"], "(8 $1)"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TN:[, [, , "[2-57-9]\\d{7}", 
, , , , , , [8]], [, , "3(?:[012]\\d|6[0-4]|91)\\d{5}|7\\d{7}|81200\\d{3}", , , , "71234567"], [, , "(?:[259]\\d|4[0-6])\\d{6}", , , , "20123456"], [, , "8010\\d{4}", , , , "80101234"], [, , "88\\d{6}", , , , "88123456"], [, , "8[12]10\\d{4}", , , , "81101234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "TN", 216, "00", , , , , , , , [[, "(\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 
TO:[, [, , "[02-8]\\d{4,6}", , , , , , , [5, 7]], [, , "(?:2\\d|3[1-8]|4[1-4]|[56]0|7[0149]|8[05])\\d{3}", , , , "20123", , , [5]], [, , "(?:7[578]|8[47-9])\\d{5}", , , , "7715123", , , [7]], [, , "0800\\d{3}", , , , "0800222", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "TO", 676, "00", , , , , , , , [[, "(\\d{2})(\\d{3})", "$1-$2", ["[1-6]|7[0-4]|8[05]"]], [, "(\\d{3})(\\d{4})", "$1 $2", ["7[5-9]|8[47-9]"]], [, "(\\d{4})(\\d{3})", 
"$1 $2", ["0"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TR:[, [, , "[2-589]\\d{9}|444\\d{4}", , , , , , , [7, 10]], [, , "(?:2(?:[13][26]|[28][2468]|[45][268]|[67][246])|3(?:[13][28]|[24-6][2468]|[78][02468]|92)|4(?:[16][246]|[23578][2468]|4[26]))\\d{7}", , , , "2123456789", , , [10]], [, , "5(?:(?:0[1-7]|22|[34]\\d|5[1-59]|9[246])\\d{2}|6161)\\d{5}", , , , "5012345678", , , [10]], [, , "800\\d{7}", , , , "8001234567", , 
, [10]], [, , "900\\d{7}", , , , "9001234567", , , [10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "TR", 90, "00", "0", , , "0", , , , [[, "(\\d{3})(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[23]|4(?:[0-35-9]|4[0-35-9])"], "(0$1)", , 1], [, "(\\d{3})(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["5[02-69]"], "0$1", , 1], [, "(\\d{3})(\\d{3})(\\d{4})", "$1 $2 $3", ["51|[89]"], "0$1", , 1], [, "(444)(\\d{1})(\\d{3})", "$1 $2 $3", ["444"], , , 1]], , [, , "512\\d{7}", 
, , , "5123456789", , , [10]], , , [, , "444\\d{4}", , , , "4441444", , , [7]], [, , "444\\d{4}|850\\d{7}", , , , "4441444"], , , [, , , , , , , , , [-1]]], TT:[, [, , "[589]\\d{9}", , , , , , , [10], [7]], [, , "868(?:2(?:01|[23]\\d)|6(?:0[79]|1[02-8]|2[1-9]|[3-69]\\d|7[0-79])|82[124])\\d{4}", , , , "8682211234", , , , [7]], [, , "868(?:2(?:6[6-9]|[789]\\d)|3(?:0[1-9]|1[02-9]|[2-9]\\d)|4[6-9]\\d|6(?:20|78|8\\d)|7(?:0[1-9]|1[02-9]|[2-9]\\d))\\d{4}", , , , "8682911234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", 
, , , "8002345678"], [, , "900[2-9]\\d{6}", , , , "9002345678"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "TT", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "868", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , "868619\\d{4}", , , , "8686191234", , , , [7]]], TV:[, [, , "[279]\\d{4,6}", , , , , , , [5, 6, 7]], [, , "2[02-9]\\d{3}", , , , "20123", , , [5]], [, , "(?:70\\d|90)\\d{4}", , , , "901234", 
, , [6, 7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "TV", 688, "00", , , , , , , , , , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], TW:[, [, , "2\\d{6,8}|[3-689]\\d{7,8}|7\\d{7,9}", , , , , , , [7, 8, 9, 10]], [, , "(?:2(?:[235-8]\\d{3}|4\\d{2,3})|3[2-9]\\d{2}|4(?:[239]\\d|[78])\\d{2}|5[2-8]\\d{2}|6[235-79]\\d{2}|7[1-9]\\d{2}|8(?:2(?:3\\d|66)|[7-9]\\d{2}))\\d{4}", 
, , , "221234567", , , [8, 9]], [, , "9[0-8]\\d{7}", , , , "912345678", , , [9]], [, , "80[0-79]\\d{6}", , , , "800123456", , , [9]], [, , "20(?:2|[013-9]\\d{2})\\d{4}", , , , "203123456", , , [7, 9]], [, , , , , , , , , [-1]], [, , "99\\d{7}", , , , "990123456", , , [9]], [, , "70\\d{8}", , , , "7012345678", , , [10]], "TW", 886, "0(?:0[25679]|19)", "0", "#", , "0", , , , [[, "(20)(\\d)(\\d{4})", "$1 $2 $3", ["202"], "0$1"], [, "([258]0)(\\d{3})(\\d{4})", "$1 $2 $3", ["20[013-9]|50[0-46-9]|80[0-79]"], 
"0$1"], [, "([2-8])(\\d{3,4})(\\d{4})", "$1 $2 $3", ["[25][2-8]|[346]|[78][1-9]"], "0$1"], [, "(9\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["9"], "0$1"], [, "(70)(\\d{4})(\\d{4})", "$1 $2 $3", ["70"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "50[0-46-9]\\d{6}", , , , "500123456", , , [9]], , , [, , , , , , , , , [-1]]], TZ:[, [, , "\\d{9}", , , , , , , [7, 9]], [, , "2[2-8]\\d{7}", , , , "222345678"], [, , "(?:6[2-9]|7[13-9])\\d{7}", , , , "621234567", , , [9]], [, , "80[08]\\d{6}", 
, , , "800123456", , , [9]], [, , "90\\d{7}", , , , "900123456", , , [9]], [, , "8(?:40|6[01])\\d{6}", , , , "840123456", , , [9]], [, , , , , , , , , [-1]], [, , "41\\d{7}", , , , "412345678", , , [9]], "TZ", 255, "00[056]", "0", , , "0", , , , [[, "([24]\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["[24]"], "0$1"], [, "([67]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["[67]"], "0$1"], [, "([89]\\d{2})(\\d{2})(\\d{4})", "$1 $2 $3", ["[89]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , "(?:8(?:[04]0|6[01])|90\\d)\\d{6}", 
, , , "800123456", , , [9]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], UA:[, [, , "[3-9]\\d{8}", , , , , , , [9], [5, 6, 7]], [, , "(?:3[1-8]|4[13-8]|5[1-7]|6[12459])\\d{7}", , , , "311234567", , , , [5, 6, 7]], [, , "(?:39|50|6[36-8]|7[1-3]|9[1-9])\\d{7}", , , , "391234567"], [, , "800\\d{6}", , , , "800123456"], [, , "900[2-49]\\d{5}", , , , "900212345"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "89[1-579]\\d{6}", , , , "891234567"], "UA", 380, "00", "0", , , "0", 
, "0~0", , [[, "([3-9]\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["[38]9|4(?:[45][0-5]|87)|5(?:0|[67][37])|6[36-8]|7|9[1-9]", "[38]9|4(?:[45][0-5]|87)|5(?:0|6(?:3[14-7]|7)|7[37])|6[36-8]|7|9[1-9]"], "0$1"], [, "([3-689]\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["(?:3[1-8]|4[136-8])2|5(?:[12457]2|6[24])|6(?:[12][29]|[49]2|5[24])|8[0-8]|90", "3(?:[1-46-8]2[013-9]|52)|4(?:[1378]2|62[013-9])|5(?:[12457]2|6[24])|6(?:[12][29]|[49]2|5[24])|8[0-8]|90"], "0$1"], [, "([3-6]\\d{3})(\\d{5})", "$1 $2", ["3(?:[1-46-8]|5[013-9])|4(?:[137][013-9]|[45][6-9]|6|8[4-6])|5(?:[1245][013-9]|3|6[0135689]|7[4-6])|6(?:[12][13-8]|[49][013-9]|5[0135-9])", 
"3(?:[1-46-8](?:[013-9]|22)|5[013-9])|4(?:[137][013-9]|[45][6-9]|6(?:[013-9]|22)|8[4-6])|5(?:[1245][013-9]|3|6(?:[015689]|3[02389])|7[4-6])|6(?:[12][13-8]|[49][013-9]|5[0135-9])"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], UG:[, [, , "\\d{9}", , , , , , , [9], [5, 6, 7]], [, , "20(?:[0147]\\d{3}|2(?:40|[5-9]\\d)\\d|3(?:0[0-4]|[2367]\\d)\\d|5[0-4]\\d{2}|6(?:00[0-2]|30[0-4]|[5-9]\\d{2})|8[0-2]\\d{2})\\d{3}|[34]\\d{8}", 
, , , "312345678", , , , [5, 6, 7]], [, , "7(?:0[0-7]\\d|[1578]\\d{2}|2(?:[03]\\d|60)|30\\d|4[0-4]\\d|9(?:[0-6]\\d|74))\\d{5}", , , , "712345678"], [, , "800[123]\\d{5}", , , , "800123456"], [, , "90[123]\\d{6}", , , , "901123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "UG", 256, "00[057]", "0", , , "0", , , , [[, "(\\d{3})(\\d{6})", "$1 $2", ["20[0-8]|4(?:6[45]|[7-9])|[7-9]", "20(?:[013-8]|2[5-9])|4(?:6[45]|[7-9])|[7-9]"], "0$1"], [, "(\\d{2})(\\d{7})", "$1 $2", 
["3|4(?:[1-5]|6[0-36-9])"], "0$1"], [, "(2024)(\\d{5})", "$1 $2", ["202", "2024"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], US:[, [, , "[2-9]\\d{9}", , , , , , , [10], [7]], [, , "(?:2(?:0[1-35-9]|1[02-9]|2[03-589]|3[149]|4[08]|5[1-46]|6[0279]|7[026]|8[13])|3(?:0[1-57-9]|1[02-9]|2[0135]|3[0-24679]|4[67]|5[12]|6[014]|8[056])|4(?:0[124-9]|1[02-579]|2[3-5]|3[0245]|4[0235]|58|6[39]|7[0589]|8[04])|5(?:0[1-57-9]|1[0235-8]|20|3[0149]|4[01]|5[19]|6[1-47]|7[013-5]|8[056])|6(?:0[1-35-9]|1[024-9]|2[03689]|3[016]|4[16]|5[017]|6[0-279]|78|8[012])|7(?:0[1-46-8]|1[02-9]|2[04-7]|3[1247]|4[037]|5[47]|6[02359]|7[02-59]|8[156])|8(?:0[1-68]|1[02-8]|28|3[0-258]|4[3578]|5[046-9]|6[02-5]|7[028])|9(?:0[1346-9]|1[02-9]|2[0589]|3[014678]|4[0179]|5[12469]|7[0-3589]|8[04-69]))[2-9]\\d{6}", 
, , , "2015550123", , , , [7]], [, , "(?:2(?:0[1-35-9]|1[02-9]|2[03-589]|3[149]|4[08]|5[1-46]|6[0279]|7[026]|8[13])|3(?:0[1-57-9]|1[02-9]|2[0135]|3[0-24679]|4[67]|5[12]|6[014]|8[056])|4(?:0[124-9]|1[02-579]|2[3-5]|3[0245]|4[0235]|58|6[39]|7[0589]|8[04])|5(?:0[1-57-9]|1[0235-8]|20|3[0149]|4[01]|5[19]|6[1-47]|7[013-5]|8[056])|6(?:0[1-35-9]|1[024-9]|2[03689]|3[016]|4[16]|5[017]|6[0-279]|78|8[012])|7(?:0[1-46-8]|1[02-9]|2[04-7]|3[1247]|4[037]|5[47]|6[02359]|7[02-59]|8[156])|8(?:0[1-68]|1[02-8]|28|3[0-258]|4[3578]|5[046-9]|6[02-5]|7[028])|9(?:0[1346-9]|1[02-9]|2[0589]|3[014678]|4[0179]|5[12469]|7[0-3589]|8[04-69]))[2-9]\\d{6}", 
, , , "2015550123", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002345678"], [, , "900[2-9]\\d{6}", , , , "9002345678"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "US", 1, "011", "1", , , "1", , , 1, [[, "(\\d{3})(\\d{4})", "$1-$2", , , , 1], [, "(\\d{3})(\\d{3})(\\d{4})", "($1) $2-$3", , , , 1]], [[, "(\\d{3})(\\d{3})(\\d{4})", "$1-$2-$3"]], [, , , , , , , , , [-1]], 1, , [, , , , , , , , , [-1]], [, 
, , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], UY:[, [, , "[2489]\\d{6,7}", , , , , , , [7, 8]], [, , "2\\d{7}|4[2-7]\\d{6}", , , , "21231234", , , [8], [7]], [, , "9[1-9]\\d{6}", , , , "94231234", , , [8]], [, , "80[05]\\d{4}", , , , "8001234", , , [7]], [, , "90[0-8]\\d{4}", , , , "9001234", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "UY", 598, "0(?:1[3-9]\\d|0)", "0", " int. ", , "0", , "00", , [[, "(\\d{4})(\\d{4})", "$1 $2", ["[24]"]], [, "(\\d{2})(\\d{3})(\\d{3})", 
"$1 $2 $3", ["9[1-9]"], "0$1"], [, "(\\d{3})(\\d{4})", "$1 $2", ["[89]0"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], UZ:[, [, , "[679]\\d{8}", , , , , , , [9], [7]], [, , "(?:6(?:1(?:22|3[124]|4[1-4]|5[123578]|64)|2(?:22|3[0-57-9]|41)|5(?:22|3[3-7]|5[024-8])|6\\d{2}|7(?:[23]\\d|7[69])|9(?:22|4[1-8]|6[135]))|7(?:0(?:5[4-9]|6[0146]|7[12456]|9[135-8])|1[12]\\d|2(?:22|3[1345789]|4[123579]|5[14])|3(?:2\\d|3[1578]|4[1-35-7]|5[1-57]|61)|4(?:2\\d|3[1-579]|7[1-79])|5(?:22|5[1-9]|6[1457])|6(?:22|3[12457]|4[13-8])|9(?:22|5[1-9])))\\d{5}", 
, , , "662345678", , , , [7]], [, , "6(?:1(?:2(?:98|2[01])|35[0-4]|50\\d|61[23]|7(?:[01][017]|4\\d|55|9[5-9]))|2(?:11\\d|2(?:[12]1|9[01379])|5(?:[126]\\d|3[0-4])|7\\d{2})|5(?:19[01]|2(?:27|9[26])|30\\d|59\\d|7\\d{2})|6(?:2(?:1[5-9]|2[0367]|38|41|52|60)|3[79]\\d|4(?:56|83)|7(?:[07]\\d|1[017]|3[07]|4[047]|5[057]|67|8[0178]|9[79])|9[0-3]\\d)|7(?:2(?:24|3[237]|4[5-9]|7[15-8])|5(?:7[12]|8[0589])|7(?:0\\d|[39][07])|9(?:0\\d|7[079]))|9(?:2(?:1[1267]|5\\d|3[01]|7[0-4])|5[67]\\d|6(?:2[0-26]|8\\d)|7\\d{2}))\\d{4}|7(?:0\\d{3}|1(?:13[01]|6(?:0[47]|1[67]|66)|71[3-69]|98\\d)|2(?:2(?:2[79]|95)|3(?:2[5-9]|6[0-6])|57\\d|7(?:0\\d|1[17]|2[27]|3[37]|44|5[057]|66|88))|3(?:2(?:1[0-6]|21|3[469]|7[159])|33\\d|5(?:0[0-4]|5[579]|9\\d)|7(?:[0-3579]\\d|4[0467]|6[67]|8[078])|9[4-6]\\d)|4(?:2(?:29|5[0257]|6[0-7]|7[1-57])|5(?:1[0-4]|8\\d|9[5-9])|7(?:0\\d|1[024589]|2[0127]|3[0137]|[46][07]|5[01]|7[5-9]|9[079])|9(?:7[015-9]|[89]\\d))|5(?:112|2(?:0\\d|2[29]|[49]4)|3[1568]\\d|52[6-9]|7(?:0[01578]|1[017]|[23]7|4[047]|[5-7]\\d|8[78]|9[079]))|6(?:2(?:2[1245]|4[2-4])|39\\d|41[179]|5(?:[349]\\d|5[0-2])|7(?:0[017]|[13]\\d|22|44|55|67|88))|9(?:22[128]|3(?:2[0-4]|7\\d)|57[05629]|7(?:2[05-9]|3[37]|4\\d|60|7[2579]|87|9[07])))\\d{4}|9[0-57-9]\\d{7}", 
, , , "912345678"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "UZ", 998, "810", "8", , , "8", , "8~10", , [[, "([679]\\d)(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["[679]"], "8 $1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], VA:[, [, , "(?:0(?:878\\d{5}|6698\\d{5})|[1589]\\d{5,10}|3(?:[12457-9]\\d{8}|[36]\\d{7,9}))", , , , , , , [6, 8, 9, 
10, 11]], [, , "06698\\d{5}", , , , "0669812345", , , [10]], [, , "3(?:[12457-9]\\d{8}|6\\d{7,8}|3\\d{7,9})", , , , "3123456789", , , [9, 10, 11]], [, , "80(?:0\\d{6}|3\\d{3})", , , , "800123456", , , [6, 9]], [, , "0878\\d{5}|1(?:44|6[346])\\d{6}|89(?:2\\d{3}|4(?:[0-4]\\d{2}|[5-9]\\d{4})|5(?:[0-4]\\d{2}|[5-9]\\d{6})|9\\d{6})", , , , "899123456", , , [6, 8, 9, 10]], [, , "84(?:[08]\\d{6}|[17]\\d{3})", , , , "848123456", , , [6, 9]], [, , "1(?:78\\d|99)\\d{6}", , , , "1781234567", , , [9, 10]], [, 
, "55\\d{8}", , , , "5512345678", , , [10]], "VA", 39, "00", , , , , , , , , , [, , , , , , , , , [-1]], , , [, , "848\\d{6}", , , , "848123456", , , [9]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], VC:[, [, , "[5789]\\d{9}", , , , , , , [10], [7]], [, , "784(?:266|3(?:6[6-9]|7\\d|8[0-24-6])|4(?:38|5[0-36-8]|8[0-8])|5(?:55|7[0-2]|93)|638|784)\\d{4}", , , , "7842661234", , , , [7]], [, , "784(?:4(?:3[0-5]|5[45]|89|9[0-58])|5(?:2[6-9]|3[0-4]))\\d{4}", , , , "7844301234", , , , [7]], [, 
, "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002345678"], [, , "900[2-9]\\d{6}", , , , "9002345678"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "VC", 1, "011", "1", , , "1", , , , , , [, , , , , , , , , [-1]], , "784", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], VE:[, [, , "[24589]\\d{9}", , , , , , , [10], [7]], [, , "(?:2(?:12|3[457-9]|[58][1-9]|[467]\\d|9[1-6])|50[01])\\d{7}", 
, , , "2121234567", , , , [7]], [, , "4(?:1[24-8]|2[46])\\d{7}", , , , "4121234567"], [, , "800\\d{7}", , , , "8001234567"], [, , "900\\d{7}", , , , "9001234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "VE", 58, "00", "0", , , "0", , , , [[, "(\\d{3})(\\d{7})", "$1-$2", , "0$1", "$CC $1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], VG:[, [, , "[2589]\\d{9}", , , , , , , [10], [7]], [, 
, "284(?:(?:229|4(?:22|9[45])|774|8(?:52|6[459]))\\d{4}|496[0-5]\\d{3})", , , , "2842291234", , , , [7]], [, , "284(?:(?:3(?:0[0-3]|4[0-7]|68|9[34])|4(?:4[0-6]|68|99)|54[0-57])\\d{4}|496[6-9]\\d{3})", , , , "2843001234", , , , [7]], [, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002345678"], [, , "900[2-9]\\d{6}", , , , "9002345678"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "VG", 1, "011", "1", , , "1", , , , , , 
[, , , , , , , , , [-1]], , "284", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], VI:[, [, , "[3589]\\d{9}", , , , , , , [10], [7]], [, , "340(?:2(?:01|2[0678]|44|77)|3(?:32|44)|4(?:22|7[34])|5(?:1[34]|55)|6(?:26|4[23]|77|9[023])|7(?:1[2-589]|27|7\\d)|884|998)\\d{4}", , , , "3406421234", , , , [7]], [, , "340(?:2(?:01|2[0678]|44|77)|3(?:32|44)|4(?:22|7[34])|5(?:1[34]|55)|6(?:26|4[23]|77|9[023])|7(?:1[2-589]|27|7\\d)|884|998)\\d{4}", , , , "3406421234", , , , [7]], 
[, , "8(?:00|33|44|55|66|77|88)[2-9]\\d{6}", , , , "8002345678"], [, , "900[2-9]\\d{6}", , , , "9002345678"], [, , , , , , , , , [-1]], [, , "5(?:00|22|33|44|66|77|88)[2-9]\\d{6}", , , , "5002345678"], [, , , , , , , , , [-1]], "VI", 1, "011", "1", , , "1", , , 1, , , [, , , , , , , , , [-1]], , "340", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], VN:[, [, , "1\\d{6,9}|2\\d{9}|6\\d{6,7}|7\\d{6}|8\\d{6,8}|9\\d{8}", , , , , , , [7, 8, 9, 10]], [, , "2(?:0[3-9]|1[0-689]|2[0-25-9]|3[2-9]|4[2-8]|5[124-9]|6[0-39]|7[0-7]|8[2-7]|9[0-4679])\\d{7}", 
, , , "2101234567", , , [10]], [, , "(?:9\\d|1(?:2\\d|6[2-9]|8[68]|99))\\d{7}|8(?:6[89]|8\\d|9[89])\\d{6}", , , , "912345678", , , [9, 10]], [, , "1800\\d{4,6}", , , , "1800123456", , , [8, 9, 10]], [, , "1900\\d{4,6}", , , , "1900123456", , , [8, 9, 10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "VN", 84, "00", "0", , , "0", , , , [[, "([17]99)(\\d{4})", "$1 $2", ["[17]99"], "0$1", , 1], [, "(\\d{2})(\\d{4})(\\d{4})", "$1 $2 $3", ["2[48]"], "0$1", , 1], [, "(80)(\\d{5})", 
"$1 $2", ["80"], "0$1", , 1], [, "(69\\d)(\\d{4,5})", "$1 $2", ["69"], "0$1", , 1], [, "(\\d{3})(\\d{4})(\\d{3})", "$1 $2 $3", ["2[0-35-79]"], "0$1", , 1], [, "([89]\\d)(\\d{3})(\\d{2})(\\d{2})", "$1 $2 $3 $4", ["8(?:8|9[89])|9"], "0$1", , 1], [, "(1[2689]\\d)(\\d{3})(\\d{4})", "$1 $2 $3", ["1(?:[26]|8[68]|99)"], "0$1", , 1], [, "(86[89])(\\d{3})(\\d{3})", "$1 $2 $3", ["86[89]"], "0$1", , 1], [, "(1[89]00)(\\d{4,6})", "$1 $2", ["1[89]0", "1[89]00"], "$1", , 1]], , [, , , , , , , , , [-1]], , , [, 
, "[17]99\\d{4}|69\\d{5,6}", , , , "1992000", , , [7, 8]], [, , "[17]99\\d{4}|69\\d{5,6}|80\\d{5}", , , , "1992000", , , [7, 8]], , , [, , , , , , , , , [-1]]], VU:[, [, , "[2-57-9]\\d{4,6}", , , , , , , [5, 7]], [, , "(?:2[02-9]\\d|3(?:[5-7]\\d|8[0-8])|48[4-9]|88\\d)\\d{2}", , , , "22123", , , [5]], [, , "(?:5(?:7[2-5]|[0-689]\\d)|7[013-7]\\d)\\d{4}", , , , "5912345", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , 
, , [-1]], "VU", 678, "00", , , , , , , , [[, "(\\d{3})(\\d{4})", "$1 $2", ["[579]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "3[03]\\d{3}|900\\d{4}", , , , "30123"], , , [, , , , , , , , , [-1]]], WF:[, [, , "[4-8]\\d{5}", , , , , , , [6]], [, , "(?:50|68|72)\\d{4}", , , , "501234"], [, , "(?:50|68|72|8[23])\\d{4}", , , , "501234"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "WF", 681, 
"00", , , , , , , , [[, "(\\d{2})(\\d{2})(\\d{2})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , "[48]0\\d{4}", , , , "401234"]], WS:[, [, , "[2-8]\\d{4,6}", , , , , , , [5, 6, 7]], [, , "(?:[2-5]\\d|6[1-9]|84\\d{2})\\d{3}", , , , "22123", , , [5, 7]], [, , "(?:60|7[25-7]\\d)\\d{4}", , , , "601234", , , [6, 7]], [, , "800\\d{3}", , , , "800123", , , [6]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, 
, , , , , , , , [-1]], "WS", 685, "0", , , , , , , , [[, "(8\\d{2})(\\d{3,4})", "$1 $2", ["8"]], [, "(7\\d)(\\d{5})", "$1 $2", ["7"]], [, "(\\d{5})", "$1", ["[2-6]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], YE:[, [, , "[1-7]\\d{6,8}", , , , , , , [7, 8, 9], [6]], [, , "(?:1(?:7\\d|[2-68])|2[2-68]|3[2358]|4[2-58]|5[2-6]|6[3-58]|7[24-68])\\d{5}", , , , "1234567", , , [7, 8], [6]], [, , "7[0137]\\d{7}", , , , "712345678", , 
, [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "YE", 967, "00", "0", , , "0", , , , [[, "([1-7])(\\d{3})(\\d{3,4})", "$1 $2 $3", ["[1-6]|7[24-68]"], "0$1"], [, "(7\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["7[0137]"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], YT:[, [, , "[268]\\d{8}", , , , , , , [9]], [, , "269(?:6[0-4]|50)\\d{4}", 
, , , "269601234"], [, , "639(?:0[0-79]|1[019]|[26]\\d|3[09]|[45]0|7[06]|9[04-79])\\d{4}", , , , "639012345"], [, , "80\\d{7}", , , , "801234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "YT", 262, "00", "0", , , "0", , , , , , [, , , , , , , , , [-1]], , "269|63", [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], ZA:[, [, , "[1-79]\\d{8}|8\\d{4,8}", , , , , , , [5, 6, 7, 8, 9]], [, , "(?:1[0-8]|2[1-378]|3[1-69]|4\\d|5[1346-8])\\d{7}", 
, , , "101234567", , , [9]], [, , "(?:6\\d|7[0-46-9])\\d{7}|8(?:[1-4]\\d{1,5}|5\\d{5})\\d{2}", , , , "711234567"], [, , "80\\d{7}", , , , "801234567", , , [9]], [, , "86[2-9]\\d{6}|9[0-2]\\d{7}", , , , "862345678", , , [9]], [, , "860\\d{6}", , , , "860123456", , , [9]], [, , , , , , , , , [-1]], [, , "87\\d{7}", , , , "871234567", , , [9]], "ZA", 27, "00", "0", , , "0", , , , [[, "(860)(\\d{3})(\\d{3})", "$1 $2 $3", ["860"], "0$1"], [, "(\\d{2})(\\d{3,4})", "$1 $2", ["8[1-4]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{2,3})", 
"$1 $2 $3", ["8[1-4]"], "0$1"], [, "(\\d{2})(\\d{3})(\\d{4})", "$1 $2 $3", ["[1-79]|8(?:[0-57]|6[1-9])"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "861\\d{6}", , , , "861123456", , , [9]], , , [, , , , , , , , , [-1]]], ZM:[, [, , "[289]\\d{8}", , , , , , , [9]], [, , "21[1-8]\\d{6}", , , , "211234567"], [, , "9(?:5[034589]|[67]\\d)\\d{6}", , , , "955123456"], [, , "800\\d{6}", , , , "800123456"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , 
, [-1]], [, , , , , , , , , [-1]], "ZM", 260, "00", "0", , , "0", , , , [[, "([29]\\d)(\\d{7})", "$1 $2", ["[29]"], "0$1"], [, "(800)(\\d{3})(\\d{3})", "$1 $2 $3", ["800"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], ZW:[, [, , "2(?:[0-57-9]\\d{3,8}|6(?:[14]\\d{7}|\\d{4}))|[13-79]\\d{4,9}|8[06]\\d{5,8}", , , , , , , [5, 6, 7, 8, 9, 10], [3, 4]], [, , "(?:2(?:0(?:4\\d|5\\d{2})|2[278]\\d|48\\d|7(?:[1-7]\\d|[089]\\d{2})|8(?:[2-57-9]|[146]\\d{2})|98)|3(?:08|17|3[78]|7(?:[19]|[56]\\d)|8[37]|98)|5[15][78]|6(?:28\\d{2}|37|6[78]|75\\d|98|8(?:7\\d|8)))\\d{3}|(?:2(?:1[39]|2[0157]|31|[56][14]|7[35]|84)|329)\\d{7}|(?:1(?:3\\d{2}|[4-8]|9\\d)|2(?:0\\d{2}|12|292|[569]\\d)|3(?:[26]|[013459]\\d)|5(?:0|1[2-4]|26|[37]2|5\\d{2}|[689]\\d)|6(?:[39]|[01246]\\d|[78]\\d{2}))\\d{3}|(?:29\\d|39|54)\\d{6}|(?:(?:25|54)83\\d|2582\\d{2}|65[2-8])\\d{2}|(?:4\\d{6,7}|9[2-9]\\d{4,5})", 
, , , "1312345", , , , [3, 4]], [, , "(?:7(?:1[2-8]|3[2-9]|7[1-9]|8[2-5])|8644)\\d{6}", , , , "712345678", , , [9, 10]], [, , "80(?:[01]\\d|20|8[0-8])\\d{3}", , , , "8001234", , , [7]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "86(?:1[12]|30|55|77|8[368])\\d{6}", , , , "8686123456", , , [10]], "ZW", 263, "00", "0", , , "0", , , , [[, "([49])(\\d{3})(\\d{2,4})", "$1 $2 $3", ["4|9[2-9]"], "0$1"], [, "(7\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["7"], "0$1"], [, 
"(86\\d{2})(\\d{3})(\\d{3})", "$1 $2 $3", ["86[24]"], "0$1"], [, "([2356]\\d{2})(\\d{3,5})", "$1 $2", ["2(?:0[45]|2[278]|[49]8|[78])|3(?:[09]8|17|3[78]|7[1569]|8[37])|5[15][78]|6(?:[29]8|37|[68][78]|75)"], "0$1"], [, "(\\d{3})(\\d{3})(\\d{3,4})", "$1 $2 $3", ["2(?:1[39]|2[0157]|31|[56][14]|7[35]|84)|329"], "0$1"], [, "([1-356]\\d)(\\d{3,5})", "$1 $2", ["1[3-9]|2[02569]|3[0-69]|5[05689]|6"], "0$1"], [, "([235]\\d)(\\d{3})(\\d{3,4})", "$1 $2 $3", ["[23]9|54"], "0$1"], [, "([25]\\d{3})(\\d{3,5})", "$1 $2", 
["(?:25|54)8", "258[23]|5483"], "0$1"], [, "(8\\d{3})(\\d{6})", "$1 $2", ["86"], "0$1"], [, "(80\\d)(\\d{4})", "$1 $2", ["80"], "0$1"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 800:[, [, , "\\d{8}", , , , , , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "\\d{8}", , , , "12345678"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "001", 800, , , , , 
, , , 1, [[, "(\\d{4})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 808:[, [, , "\\d{8}", , , , , , , [8]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "\\d{8}", , , , "12345678"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "001", 808, , , , , , , , 1, [[, "(\\d{4})(\\d{4})", "$1 $2"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , 
, [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 870:[, [, , "[35-7]\\d{8}", , , , , , , [9]], [, , , , , , , , , [-1]], [, , "(?:[356]\\d|7[6-8])\\d{7}", , , , "301234567"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "001", 870, , , , , , , , , [[, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , 
, [-1]]], 878:[, [, , "1\\d{11}", , , , , , , [12]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "10\\d{10}", , , , "101234567890"], "001", 878, , , , , , , , 1, [[, "(\\d{2})(\\d{5})(\\d{5})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 881:[, [, , "[67]\\d{8}", , , , , , , [9]], [, , , , , , , , 
, [-1]], [, , "[67]\\d{8}", , , , "612345678"], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "001", 881, , , , , , , , , [[, "(\\d)(\\d{3})(\\d{5})", "$1 $2 $3", ["[67]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 882:[, [, , "[13]\\d{6,11}", , , , , , , [7, 8, 9, 10, 11, 12]], [, , , , , , , , , [-1]], [, , "3(?:2\\d{3}|37\\d{2}|4(?:2|7\\d{3}))\\d{4}", 
, , , "3421234", , , [7, 9, 10]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "1(?:3(?:0[0347]|[13][0139]|2[035]|4[013568]|6[0459]|7[06]|8[15678]|9[0689])\\d{4}|6\\d{5,10})|3(?:45|9\\d{3})\\d{7}", , , , "390123456789"], "001", 882, , , , , , , , , [[, "(\\d{2})(\\d{4})(\\d{3})", "$1 $2 $3", ["3[23]"]], [, "(\\d{2})(\\d{5})", "$1 $2", ["16|342"]], [, "(\\d{2})(\\d{4})(\\d{4})", "$1 $2 $3", ["34[57]"]], [, "(\\d{3})(\\d{4})(\\d{4})", 
"$1 $2 $3", ["348"]], [, "(\\d{2})(\\d{2})(\\d{4})", "$1 $2 $3", ["1"]], [, "(\\d{2})(\\d{3,4})(\\d{4})", "$1 $2 $3", ["16"]], [, "(\\d{2})(\\d{4,5})(\\d{5})", "$1 $2 $3", ["16|39"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , "348[57]\\d{7}", , , , "34851234567", , , [11]]], 883:[, [, , "51\\d{7}(?:\\d{3})?", , , , , , , [9, 12]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , 
, , , [-1]], [, , , , , , , , , [-1]], [, , "51(?:00\\d{5}(?:\\d{3})?|[13]0\\d{8})", , , , "510012345"], "001", 883, , , , , , , , 1, [[, "(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3", ["510"]], [, "(\\d{3})(\\d{3})(\\d{3})(\\d{3})", "$1 $2 $3 $4", ["510"]], [, "(\\d{4})(\\d{4})(\\d{4})", "$1 $2 $3", ["51[13]"]]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]], 888:[, [, , "\\d{11}", , , , , , , [11]], [, , , , , , , , , [-1]], [, , , , 
, , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "001", 888, , , , , , , , 1, [[, "(\\d{3})(\\d{3})(\\d{5})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , "\\d{11}", , , , "12345678901"], , , [, , , , , , , , , [-1]]], 979:[, [, , "\\d{9}", , , , , , , [9]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , "\\d{9}", , , , "123456789"], 
[, , , , , , , , , [-1]], [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], "001", 979, , , , , , , , 1, [[, "(\\d)(\\d{4})(\\d{4})", "$1 $2 $3"]], , [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]], [, , , , , , , , , [-1]], , , [, , , , , , , , , [-1]]]};
/*

 Copyright (C) 2010 The Libphonenumber Authors.

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
*/
i18n.phonenumbers.PhoneNumberUtil = function() {
  this.regionToMetadataMap = {};
};
goog.addSingletonGetter(i18n.phonenumbers.PhoneNumberUtil);
i18n.phonenumbers.Error = {INVALID_COUNTRY_CODE:"Invalid country calling code", NOT_A_NUMBER:"The string supplied did not seem to be a phone number", TOO_SHORT_AFTER_IDD:"Phone number too short after IDD", TOO_SHORT_NSN:"The string supplied is too short to be a phone number", TOO_LONG:"The string supplied is too long to be a phone number"};
i18n.phonenumbers.PhoneNumberUtil.NANPA_COUNTRY_CODE_ = 1;
i18n.phonenumbers.PhoneNumberUtil.MIN_LENGTH_FOR_NSN_ = 2;
i18n.phonenumbers.PhoneNumberUtil.MAX_LENGTH_FOR_NSN_ = 17;
i18n.phonenumbers.PhoneNumberUtil.MAX_LENGTH_COUNTRY_CODE_ = 3;
i18n.phonenumbers.PhoneNumberUtil.MAX_INPUT_STRING_LENGTH_ = 250;
i18n.phonenumbers.PhoneNumberUtil.UNKNOWN_REGION_ = "ZZ";
i18n.phonenumbers.PhoneNumberUtil.COLOMBIA_MOBILE_TO_FIXED_LINE_PREFIX_ = "3";
i18n.phonenumbers.PhoneNumberUtil.MOBILE_TOKEN_MAPPINGS_ = {52:"1", 54:"9"};
i18n.phonenumbers.PhoneNumberUtil.GEO_MOBILE_COUNTRIES_ = [52, 54, 55];
i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN = "+";
i18n.phonenumbers.PhoneNumberUtil.STAR_SIGN_ = "*";
i18n.phonenumbers.PhoneNumberUtil.RFC3966_EXTN_PREFIX_ = ";ext=";
i18n.phonenumbers.PhoneNumberUtil.RFC3966_PREFIX_ = "tel:";
i18n.phonenumbers.PhoneNumberUtil.RFC3966_PHONE_CONTEXT_ = ";phone-context=";
i18n.phonenumbers.PhoneNumberUtil.RFC3966_ISDN_SUBADDRESS_ = ";isub=";
i18n.phonenumbers.PhoneNumberUtil.DIGIT_MAPPINGS = {0:"0", 1:"1", 2:"2", 3:"3", 4:"4", 5:"5", 6:"6", 7:"7", 8:"8", 9:"9", "\uff10":"0", "\uff11":"1", "\uff12":"2", "\uff13":"3", "\uff14":"4", "\uff15":"5", "\uff16":"6", "\uff17":"7", "\uff18":"8", "\uff19":"9", "\u0660":"0", "\u0661":"1", "\u0662":"2", "\u0663":"3", "\u0664":"4", "\u0665":"5", "\u0666":"6", "\u0667":"7", "\u0668":"8", "\u0669":"9", "\u06f0":"0", "\u06f1":"1", "\u06f2":"2", "\u06f3":"3", "\u06f4":"4", "\u06f5":"5", "\u06f6":"6", "\u06f7":"7", 
"\u06f8":"8", "\u06f9":"9"};
i18n.phonenumbers.PhoneNumberUtil.DIALLABLE_CHAR_MAPPINGS_ = {0:"0", 1:"1", 2:"2", 3:"3", 4:"4", 5:"5", 6:"6", 7:"7", 8:"8", 9:"9", "+":i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN, "*":"*", "#":"#"};
i18n.phonenumbers.PhoneNumberUtil.ALPHA_MAPPINGS_ = {A:"2", B:"2", C:"2", D:"3", E:"3", F:"3", G:"4", H:"4", I:"4", J:"5", K:"5", L:"5", M:"6", N:"6", O:"6", P:"7", Q:"7", R:"7", S:"7", T:"8", U:"8", V:"8", W:"9", X:"9", Y:"9", Z:"9"};
i18n.phonenumbers.PhoneNumberUtil.ALL_NORMALIZATION_MAPPINGS_ = {0:"0", 1:"1", 2:"2", 3:"3", 4:"4", 5:"5", 6:"6", 7:"7", 8:"8", 9:"9", "\uff10":"0", "\uff11":"1", "\uff12":"2", "\uff13":"3", "\uff14":"4", "\uff15":"5", "\uff16":"6", "\uff17":"7", "\uff18":"8", "\uff19":"9", "\u0660":"0", "\u0661":"1", "\u0662":"2", "\u0663":"3", "\u0664":"4", "\u0665":"5", "\u0666":"6", "\u0667":"7", "\u0668":"8", "\u0669":"9", "\u06f0":"0", "\u06f1":"1", "\u06f2":"2", "\u06f3":"3", "\u06f4":"4", "\u06f5":"5", "\u06f6":"6", 
"\u06f7":"7", "\u06f8":"8", "\u06f9":"9", A:"2", B:"2", C:"2", D:"3", E:"3", F:"3", G:"4", H:"4", I:"4", J:"5", K:"5", L:"5", M:"6", N:"6", O:"6", P:"7", Q:"7", R:"7", S:"7", T:"8", U:"8", V:"8", W:"9", X:"9", Y:"9", Z:"9"};
i18n.phonenumbers.PhoneNumberUtil.ALL_PLUS_NUMBER_GROUPING_SYMBOLS_ = {0:"0", 1:"1", 2:"2", 3:"3", 4:"4", 5:"5", 6:"6", 7:"7", 8:"8", 9:"9", A:"A", B:"B", C:"C", D:"D", E:"E", F:"F", G:"G", H:"H", I:"I", J:"J", K:"K", L:"L", M:"M", N:"N", O:"O", P:"P", Q:"Q", R:"R", S:"S", T:"T", U:"U", V:"V", W:"W", X:"X", Y:"Y", Z:"Z", a:"A", b:"B", c:"C", d:"D", e:"E", f:"F", g:"G", h:"H", i:"I", j:"J", k:"K", l:"L", m:"M", n:"N", o:"O", p:"P", q:"Q", r:"R", s:"S", t:"T", u:"U", v:"V", w:"W", x:"X", y:"Y", z:"Z", 
"-":"-", "\uff0d":"-", "\u2010":"-", "\u2011":"-", "\u2012":"-", "\u2013":"-", "\u2014":"-", "\u2015":"-", "\u2212":"-", "/":"/", "\uff0f":"/", " ":" ", "\u3000":" ", "\u2060":" ", ".":".", "\uff0e":"."};
i18n.phonenumbers.PhoneNumberUtil.SINGLE_INTERNATIONAL_PREFIX_ = /[\d]+(?:[~\u2053\u223C\uFF5E][\d]+)?/;
i18n.phonenumbers.PhoneNumberUtil.VALID_PUNCTUATION = "-x\u2010-\u2015\u2212\u30fc\uff0d-\uff0f \u00a0\u00ad\u200b\u2060\u3000()\uff08\uff09\uff3b\uff3d.\\[\\]/~\u2053\u223c\uff5e";
i18n.phonenumbers.PhoneNumberUtil.VALID_DIGITS_ = "0-9\uff10-\uff19\u0660-\u0669\u06f0-\u06f9";
i18n.phonenumbers.PhoneNumberUtil.VALID_ALPHA_ = "A-Za-z";
i18n.phonenumbers.PhoneNumberUtil.PLUS_CHARS_ = "+\uff0b";
i18n.phonenumbers.PhoneNumberUtil.PLUS_CHARS_PATTERN = new RegExp("[" + i18n.phonenumbers.PhoneNumberUtil.PLUS_CHARS_ + "]+");
i18n.phonenumbers.PhoneNumberUtil.LEADING_PLUS_CHARS_PATTERN_ = new RegExp("^[" + i18n.phonenumbers.PhoneNumberUtil.PLUS_CHARS_ + "]+");
i18n.phonenumbers.PhoneNumberUtil.SEPARATOR_PATTERN_ = "[" + i18n.phonenumbers.PhoneNumberUtil.VALID_PUNCTUATION + "]+";
i18n.phonenumbers.PhoneNumberUtil.CAPTURING_DIGIT_PATTERN = new RegExp("([" + i18n.phonenumbers.PhoneNumberUtil.VALID_DIGITS_ + "])");
i18n.phonenumbers.PhoneNumberUtil.VALID_START_CHAR_PATTERN_ = new RegExp("[" + i18n.phonenumbers.PhoneNumberUtil.PLUS_CHARS_ + i18n.phonenumbers.PhoneNumberUtil.VALID_DIGITS_ + "]");
i18n.phonenumbers.PhoneNumberUtil.SECOND_NUMBER_START_PATTERN_ = /[\\\/] *x/;
i18n.phonenumbers.PhoneNumberUtil.UNWANTED_END_CHAR_PATTERN_ = new RegExp("[^" + i18n.phonenumbers.PhoneNumberUtil.VALID_DIGITS_ + i18n.phonenumbers.PhoneNumberUtil.VALID_ALPHA_ + "#]+$");
i18n.phonenumbers.PhoneNumberUtil.VALID_ALPHA_PHONE_PATTERN_ = /(?:.*?[A-Za-z]){3}.*/;
i18n.phonenumbers.PhoneNumberUtil.MIN_LENGTH_PHONE_NUMBER_PATTERN_ = "[" + i18n.phonenumbers.PhoneNumberUtil.VALID_DIGITS_ + "]{" + i18n.phonenumbers.PhoneNumberUtil.MIN_LENGTH_FOR_NSN_ + "}";
i18n.phonenumbers.PhoneNumberUtil.VALID_PHONE_NUMBER_ = "[" + i18n.phonenumbers.PhoneNumberUtil.PLUS_CHARS_ + "]*(?:[" + i18n.phonenumbers.PhoneNumberUtil.VALID_PUNCTUATION + i18n.phonenumbers.PhoneNumberUtil.STAR_SIGN_ + "]*[" + i18n.phonenumbers.PhoneNumberUtil.VALID_DIGITS_ + "]){3,}[" + i18n.phonenumbers.PhoneNumberUtil.VALID_PUNCTUATION + i18n.phonenumbers.PhoneNumberUtil.STAR_SIGN_ + i18n.phonenumbers.PhoneNumberUtil.VALID_ALPHA_ + i18n.phonenumbers.PhoneNumberUtil.VALID_DIGITS_ + "]*";
i18n.phonenumbers.PhoneNumberUtil.DEFAULT_EXTN_PREFIX_ = " ext. ";
i18n.phonenumbers.PhoneNumberUtil.CAPTURING_EXTN_DIGITS_ = "([" + i18n.phonenumbers.PhoneNumberUtil.VALID_DIGITS_ + "]{1,7})";
i18n.phonenumbers.PhoneNumberUtil.EXTN_PATTERNS_FOR_PARSING_ = i18n.phonenumbers.PhoneNumberUtil.RFC3966_EXTN_PREFIX_ + i18n.phonenumbers.PhoneNumberUtil.CAPTURING_EXTN_DIGITS_ + "|[ \u00a0\\t,]*(?:e?xt(?:ensi(?:o\u0301?|\u00f3))?n?|\uff45?\uff58\uff54\uff4e?|[;,x\uff58#\uff03~\uff5e]|int|anexo|\uff49\uff4e\uff54)[:\\.\uff0e]?[ \u00a0\\t,-]*" + i18n.phonenumbers.PhoneNumberUtil.CAPTURING_EXTN_DIGITS_ + "#?|[- ]+([" + i18n.phonenumbers.PhoneNumberUtil.VALID_DIGITS_ + "]{1,5})#";
i18n.phonenumbers.PhoneNumberUtil.EXTN_PATTERN_ = new RegExp("(?:" + i18n.phonenumbers.PhoneNumberUtil.EXTN_PATTERNS_FOR_PARSING_ + ")$", "i");
i18n.phonenumbers.PhoneNumberUtil.VALID_PHONE_NUMBER_PATTERN_ = new RegExp("^" + i18n.phonenumbers.PhoneNumberUtil.MIN_LENGTH_PHONE_NUMBER_PATTERN_ + "$|^" + i18n.phonenumbers.PhoneNumberUtil.VALID_PHONE_NUMBER_ + "(?:" + i18n.phonenumbers.PhoneNumberUtil.EXTN_PATTERNS_FOR_PARSING_ + ")?$", "i");
i18n.phonenumbers.PhoneNumberUtil.NON_DIGITS_PATTERN_ = /\D+/;
i18n.phonenumbers.PhoneNumberUtil.FIRST_GROUP_PATTERN_ = /(\$\d)/;
i18n.phonenumbers.PhoneNumberUtil.NP_PATTERN_ = /\$NP/;
i18n.phonenumbers.PhoneNumberUtil.FG_PATTERN_ = /\$FG/;
i18n.phonenumbers.PhoneNumberUtil.CC_PATTERN_ = /\$CC/;
i18n.phonenumbers.PhoneNumberUtil.FIRST_GROUP_ONLY_PREFIX_PATTERN_ = /^\(?\$1\)?$/;
i18n.phonenumbers.PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY = "001";
i18n.phonenumbers.PhoneNumberFormat = {E164:0, INTERNATIONAL:1, NATIONAL:2, RFC3966:3};
i18n.phonenumbers.PhoneNumberType = {FIXED_LINE:0, MOBILE:1, FIXED_LINE_OR_MOBILE:2, TOLL_FREE:3, PREMIUM_RATE:4, SHARED_COST:5, VOIP:6, PERSONAL_NUMBER:7, PAGER:8, UAN:9, VOICEMAIL:10, UNKNOWN:-1};
i18n.phonenumbers.PhoneNumberUtil.MatchType = {NOT_A_NUMBER:0, NO_MATCH:1, SHORT_NSN_MATCH:2, NSN_MATCH:3, EXACT_MATCH:4};
i18n.phonenumbers.PhoneNumberUtil.ValidationResult = {IS_POSSIBLE:0, IS_POSSIBLE_LOCAL_ONLY:4, INVALID_COUNTRY_CODE:1, TOO_SHORT:2, INVALID_LENGTH:5, TOO_LONG:3};
i18n.phonenumbers.PhoneNumberUtil.extractPossibleNumber = function(a) {
  var b = a.search(i18n.phonenumbers.PhoneNumberUtil.VALID_START_CHAR_PATTERN_);
  0 <= b ? (a = a.substring(b), a = a.replace(i18n.phonenumbers.PhoneNumberUtil.UNWANTED_END_CHAR_PATTERN_, ""), b = a.search(i18n.phonenumbers.PhoneNumberUtil.SECOND_NUMBER_START_PATTERN_), 0 <= b && (a = a.substring(0, b))) : a = "";
  return a;
};
i18n.phonenumbers.PhoneNumberUtil.isViablePhoneNumber = function(a) {
  return a.length < i18n.phonenumbers.PhoneNumberUtil.MIN_LENGTH_FOR_NSN_ ? !1 : i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(i18n.phonenumbers.PhoneNumberUtil.VALID_PHONE_NUMBER_PATTERN_, a);
};
i18n.phonenumbers.PhoneNumberUtil.normalize = function(a) {
  return i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(i18n.phonenumbers.PhoneNumberUtil.VALID_ALPHA_PHONE_PATTERN_, a) ? i18n.phonenumbers.PhoneNumberUtil.normalizeHelper_(a, i18n.phonenumbers.PhoneNumberUtil.ALL_NORMALIZATION_MAPPINGS_, !0) : i18n.phonenumbers.PhoneNumberUtil.normalizeDigitsOnly(a);
};
i18n.phonenumbers.PhoneNumberUtil.normalizeSB_ = function(a) {
  var b = i18n.phonenumbers.PhoneNumberUtil.normalize(a.toString());
  a.clear();
  a.append(b);
};
i18n.phonenumbers.PhoneNumberUtil.normalizeDigitsOnly = function(a) {
  return i18n.phonenumbers.PhoneNumberUtil.normalizeHelper_(a, i18n.phonenumbers.PhoneNumberUtil.DIGIT_MAPPINGS, !0);
};
i18n.phonenumbers.PhoneNumberUtil.normalizeDiallableCharsOnly = function(a) {
  return i18n.phonenumbers.PhoneNumberUtil.normalizeHelper_(a, i18n.phonenumbers.PhoneNumberUtil.DIALLABLE_CHAR_MAPPINGS_, !0);
};
i18n.phonenumbers.PhoneNumberUtil.convertAlphaCharactersInNumber = function(a) {
  return i18n.phonenumbers.PhoneNumberUtil.normalizeHelper_(a, i18n.phonenumbers.PhoneNumberUtil.ALL_NORMALIZATION_MAPPINGS_, !1);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getLengthOfGeographicalAreaCode = function(a) {
  var b = this.getMetadataForRegion(this.getRegionCodeForNumber(a));
  return null != b && (b.hasNationalPrefix() || a.hasItalianLeadingZero()) && this.isNumberGeographical(a) ? this.getLengthOfNationalDestinationCode(a) : 0;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getLengthOfNationalDestinationCode = function(a) {
  if (a.hasExtension()) {
    var b = a.clone();
    b.clearExtension();
  } else {
    b = a;
  }
  b = this.format(b, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL).split(i18n.phonenumbers.PhoneNumberUtil.NON_DIGITS_PATTERN_);
  0 == b[0].length && b.shift();
  return 2 >= b.length ? 0 : this.getNumberType(a) == i18n.phonenumbers.PhoneNumberType.MOBILE && (a = i18n.phonenumbers.PhoneNumberUtil.getCountryMobileToken(a.getCountryCodeOrDefault()), "" != a) ? b[2].length + a.length : b[1].length;
};
i18n.phonenumbers.PhoneNumberUtil.getCountryMobileToken = function(a) {
  return i18n.phonenumbers.PhoneNumberUtil.MOBILE_TOKEN_MAPPINGS_[a] || "";
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getSupportedRegions = function() {
  return goog.array.filter(Object.keys(i18n.phonenumbers.metadata.countryToMetadata), function(a) {
    return isNaN(a);
  });
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getSupportedGlobalNetworkCallingCodes = function() {
  var a = goog.array.filter(Object.keys(i18n.phonenumbers.metadata.countryToMetadata), function(a) {
    return !isNaN(a);
  });
  return goog.array.map(a, function(a) {
    return parseInt(a, 10);
  });
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getSupportedCallingCodes = function() {
  var a = Object.keys(i18n.phonenumbers.metadata.countryCodeToRegionCodeMap);
  return goog.array.join(this.getSupportedGlobalNetworkCallingCodes(), goog.array.map(a, function(a) {
    return parseInt(a, 10);
  }));
};
i18n.phonenumbers.PhoneNumberUtil.descHasPossibleNumberData_ = function(a) {
  return null != a && (1 != a.possibleLengthCount() || -1 != a.possibleLengthArray()[0]);
};
i18n.phonenumbers.PhoneNumberUtil.descHasData_ = function(a) {
  return null != a && (a.hasExampleNumber() || i18n.phonenumbers.PhoneNumberUtil.descHasPossibleNumberData_(a) || a.hasNationalNumberPattern());
};
i18n.phonenumbers.PhoneNumberUtil.getSupportedTypesForMetadata_ = function(a) {
  var b = [];
  goog.object.forEach(i18n.phonenumbers.PhoneNumberType, function(c) {
    if (c != i18n.phonenumbers.PhoneNumberType.FIXED_LINE_OR_MOBILE && c != i18n.phonenumbers.PhoneNumberType.UNKNOWN) {
      var d = i18n.phonenumbers.PhoneNumberUtil.getNumberDescByType_(a, c);
      i18n.phonenumbers.PhoneNumberUtil.descHasData_(d) && b.push(c);
    }
  });
  return b;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getSupportedTypesForRegion = function(a) {
  return this.isValidRegionCode_(a) ? i18n.phonenumbers.PhoneNumberUtil.getSupportedTypesForMetadata_(this.getMetadataForRegion(a)) : [];
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getSupportedTypesForNonGeoEntity = function(a) {
  a = this.getMetadataForNonGeographicalRegion(a);
  return null == a ? [] : i18n.phonenumbers.PhoneNumberUtil.getSupportedTypesForMetadata_(a);
};
i18n.phonenumbers.PhoneNumberUtil.normalizeHelper_ = function(a, b, c) {
  for (var d = new goog.string.StringBuffer, e, f, g = a.length, h = 0; h < g; ++h) {
    e = a.charAt(h), f = b[e.toUpperCase()], null != f ? d.append(f) : c || d.append(e);
  }
  return d.toString();
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formattingRuleHasFirstGroupOnly = function(a) {
  return 0 == a.length || i18n.phonenumbers.PhoneNumberUtil.FIRST_GROUP_ONLY_PREFIX_PATTERN_.test(a);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isNumberGeographical = function(a) {
  var b = this.getNumberType(a);
  return b == i18n.phonenumbers.PhoneNumberType.FIXED_LINE || b == i18n.phonenumbers.PhoneNumberType.FIXED_LINE_OR_MOBILE || goog.array.contains(i18n.phonenumbers.PhoneNumberUtil.GEO_MOBILE_COUNTRIES_, a.getCountryCodeOrDefault()) && b == i18n.phonenumbers.PhoneNumberType.MOBILE;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isValidRegionCode_ = function(a) {
  return null != a && isNaN(a) && a.toUpperCase() in i18n.phonenumbers.metadata.countryToMetadata;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.hasValidCountryCallingCode_ = function(a) {
  return a in i18n.phonenumbers.metadata.countryCodeToRegionCodeMap;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.format = function(a, b) {
  if (0 == a.getNationalNumber() && a.hasRawInput()) {
    var c = a.getRawInputOrDefault();
    if (0 < c.length) {
      return c;
    }
  }
  c = a.getCountryCodeOrDefault();
  var d = this.getNationalSignificantNumber(a);
  if (b == i18n.phonenumbers.PhoneNumberFormat.E164) {
    return this.prefixNumberWithCountryCallingCode_(c, i18n.phonenumbers.PhoneNumberFormat.E164, d, "");
  }
  if (!this.hasValidCountryCallingCode_(c)) {
    return d;
  }
  var e = this.getRegionCodeForCountryCode(c), f = this.getMetadataForRegionOrCallingCode_(c, e);
  e = this.maybeGetFormattedExtension_(a, f, b);
  d = this.formatNsn_(d, f, b);
  return this.prefixNumberWithCountryCallingCode_(c, b, d, e);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formatByPattern = function(a, b, c) {
  var d = a.getCountryCodeOrDefault(), e = this.getNationalSignificantNumber(a);
  if (!this.hasValidCountryCallingCode_(d)) {
    return e;
  }
  var f = this.getRegionCodeForCountryCode(d);
  f = this.getMetadataForRegionOrCallingCode_(d, f);
  var g = this.chooseFormattingPatternForNumber_(c, e);
  if (null != g) {
    c = g.clone();
    g = g.getNationalPrefixFormattingRuleOrDefault();
    if (0 < g.length) {
      var h = f.getNationalPrefixOrDefault();
      0 < h.length ? (g = g.replace(i18n.phonenumbers.PhoneNumberUtil.NP_PATTERN_, h).replace(i18n.phonenumbers.PhoneNumberUtil.FG_PATTERN_, "$1"), c.setNationalPrefixFormattingRule(g)) : c.clearNationalPrefixFormattingRule();
    }
    e = this.formatNsnUsingPattern_(e, c, b);
  }
  a = this.maybeGetFormattedExtension_(a, f, b);
  return this.prefixNumberWithCountryCallingCode_(d, b, e, a);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formatNationalNumberWithCarrierCode = function(a, b) {
  var c = a.getCountryCodeOrDefault(), d = this.getNationalSignificantNumber(a);
  if (!this.hasValidCountryCallingCode_(c)) {
    return d;
  }
  var e = this.getRegionCodeForCountryCode(c), f = this.getMetadataForRegionOrCallingCode_(c, e);
  e = this.maybeGetFormattedExtension_(a, f, i18n.phonenumbers.PhoneNumberFormat.NATIONAL);
  d = this.formatNsn_(d, f, i18n.phonenumbers.PhoneNumberFormat.NATIONAL, b);
  return this.prefixNumberWithCountryCallingCode_(c, i18n.phonenumbers.PhoneNumberFormat.NATIONAL, d, e);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getMetadataForRegionOrCallingCode_ = function(a, b) {
  return i18n.phonenumbers.PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY == b ? this.getMetadataForNonGeographicalRegion(a) : this.getMetadataForRegion(b);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formatNationalNumberWithPreferredCarrierCode = function(a, b) {
  return this.formatNationalNumberWithCarrierCode(a, 0 < a.getPreferredDomesticCarrierCodeOrDefault().length ? a.getPreferredDomesticCarrierCodeOrDefault() : b);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formatNumberForMobileDialing = function(a, b, c) {
  var d = a.getCountryCodeOrDefault();
  if (!this.hasValidCountryCallingCode_(d)) {
    return a.hasRawInput() ? a.getRawInputOrDefault() : "";
  }
  var e = "";
  a = a.clone();
  a.clearExtension();
  var f = this.getRegionCodeForCountryCode(d), g = this.getNumberType(a), h = g != i18n.phonenumbers.PhoneNumberType.UNKNOWN;
  if (b == f) {
    e = g == i18n.phonenumbers.PhoneNumberType.FIXED_LINE || g == i18n.phonenumbers.PhoneNumberType.MOBILE || g == i18n.phonenumbers.PhoneNumberType.FIXED_LINE_OR_MOBILE, "CO" == f && g == i18n.phonenumbers.PhoneNumberType.FIXED_LINE ? e = this.formatNationalNumberWithCarrierCode(a, i18n.phonenumbers.PhoneNumberUtil.COLOMBIA_MOBILE_TO_FIXED_LINE_PREFIX_) : "BR" == f && e ? e = 0 < a.getPreferredDomesticCarrierCodeOrDefault().length ? this.formatNationalNumberWithPreferredCarrierCode(a, "") : "" : 
    h && "HU" == f ? e = this.getNddPrefixForRegion(f, !0) + " " + this.format(a, i18n.phonenumbers.PhoneNumberFormat.NATIONAL) : d == i18n.phonenumbers.PhoneNumberUtil.NANPA_COUNTRY_CODE_ ? (b = this.getMetadataForRegion(b), e = this.canBeInternationallyDialled(a) && this.testNumberLength_(this.getNationalSignificantNumber(a), b) != i18n.phonenumbers.PhoneNumberUtil.ValidationResult.TOO_SHORT ? this.format(a, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL) : this.format(a, i18n.phonenumbers.PhoneNumberFormat.NATIONAL)) : 
    e = (f == i18n.phonenumbers.PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY || ("MX" == f || "CL" == f) && e) && this.canBeInternationallyDialled(a) ? this.format(a, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL) : this.format(a, i18n.phonenumbers.PhoneNumberFormat.NATIONAL);
  } else {
    if (h && this.canBeInternationallyDialled(a)) {
      return c ? this.format(a, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL) : this.format(a, i18n.phonenumbers.PhoneNumberFormat.E164);
    }
  }
  return c ? e : i18n.phonenumbers.PhoneNumberUtil.normalizeDiallableCharsOnly(e);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formatOutOfCountryCallingNumber = function(a, b) {
  if (!this.isValidRegionCode_(b)) {
    return this.format(a, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL);
  }
  var c = a.getCountryCodeOrDefault(), d = this.getNationalSignificantNumber(a);
  if (!this.hasValidCountryCallingCode_(c)) {
    return d;
  }
  if (c == i18n.phonenumbers.PhoneNumberUtil.NANPA_COUNTRY_CODE_) {
    if (this.isNANPACountry(b)) {
      return c + " " + this.format(a, i18n.phonenumbers.PhoneNumberFormat.NATIONAL);
    }
  } else {
    if (c == this.getCountryCodeForValidRegion_(b)) {
      return this.format(a, i18n.phonenumbers.PhoneNumberFormat.NATIONAL);
    }
  }
  var e = this.getMetadataForRegion(b), f = e.getInternationalPrefixOrDefault(), g = "";
  i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(i18n.phonenumbers.PhoneNumberUtil.SINGLE_INTERNATIONAL_PREFIX_, f) ? g = f : e.hasPreferredInternationalPrefix() && (g = e.getPreferredInternationalPrefixOrDefault());
  e = this.getRegionCodeForCountryCode(c);
  e = this.getMetadataForRegionOrCallingCode_(c, e);
  d = this.formatNsn_(d, e, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL);
  e = this.maybeGetFormattedExtension_(a, e, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL);
  return 0 < g.length ? g + " " + c + " " + d + e : this.prefixNumberWithCountryCallingCode_(c, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL, d, e);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formatInOriginalFormat = function(a, b) {
  if (a.hasRawInput() && !this.hasFormattingPatternForNumber_(a)) {
    return a.getRawInputOrDefault();
  }
  if (!a.hasCountryCodeSource()) {
    return this.format(a, i18n.phonenumbers.PhoneNumberFormat.NATIONAL);
  }
  switch(a.getCountryCodeSource()) {
    case i18n.phonenumbers.PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN:
      var c = this.format(a, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL);
      break;
    case i18n.phonenumbers.PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_IDD:
      c = this.formatOutOfCountryCallingNumber(a, b);
      break;
    case i18n.phonenumbers.PhoneNumber.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN:
      c = this.format(a, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL).substring(1);
      break;
    default:
      var d = this.getRegionCodeForCountryCode(a.getCountryCodeOrDefault()), e = this.getNddPrefixForRegion(d, !0);
      c = this.format(a, i18n.phonenumbers.PhoneNumberFormat.NATIONAL);
      if (null != e && 0 != e.length && !this.rawInputContainsNationalPrefix_(a.getRawInputOrDefault(), e, d) && (d = this.getMetadataForRegion(d), e = this.getNationalSignificantNumber(a), d = this.chooseFormattingPatternForNumber_(d.numberFormatArray(), e), null != d)) {
        e = d.getNationalPrefixFormattingRuleOrDefault();
        var f = e.indexOf("$1");
        0 >= f || (e = e.substring(0, f), e = i18n.phonenumbers.PhoneNumberUtil.normalizeDigitsOnly(e), 0 != e.length && (c = d.clone(), c.clearNationalPrefixFormattingRule(), c = this.formatByPattern(a, i18n.phonenumbers.PhoneNumberFormat.NATIONAL, [c])));
      }
  }
  d = a.getRawInputOrDefault();
  null != c && 0 < d.length && (e = i18n.phonenumbers.PhoneNumberUtil.normalizeDiallableCharsOnly(c), f = i18n.phonenumbers.PhoneNumberUtil.normalizeDiallableCharsOnly(d), e != f && (c = d));
  return c;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.rawInputContainsNationalPrefix_ = function(a, b, c) {
  a = i18n.phonenumbers.PhoneNumberUtil.normalizeDigitsOnly(a);
  if (goog.string.startsWith(a, b)) {
    try {
      return this.isValidNumber(this.parse(a.substring(b.length), c));
    } catch (d) {
    }
  }
  return !1;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.hasFormattingPatternForNumber_ = function(a) {
  var b = a.getCountryCodeOrDefault(), c = this.getRegionCodeForCountryCode(b);
  b = this.getMetadataForRegionOrCallingCode_(b, c);
  if (null == b) {
    return !1;
  }
  a = this.getNationalSignificantNumber(a);
  return null != this.chooseFormattingPatternForNumber_(b.numberFormatArray(), a);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formatOutOfCountryKeepingAlphaChars = function(a, b) {
  var c = a.getRawInputOrDefault();
  if (0 == c.length) {
    return this.formatOutOfCountryCallingNumber(a, b);
  }
  var d = a.getCountryCodeOrDefault();
  if (!this.hasValidCountryCallingCode_(d)) {
    return c;
  }
  c = i18n.phonenumbers.PhoneNumberUtil.normalizeHelper_(c, i18n.phonenumbers.PhoneNumberUtil.ALL_PLUS_NUMBER_GROUPING_SYMBOLS_, !0);
  var e = this.getNationalSignificantNumber(a);
  if (3 < e.length) {
    var f = c.indexOf(e.substring(0, 3));
    -1 != f && (c = c.substring(f));
  }
  f = this.getMetadataForRegion(b);
  if (d == i18n.phonenumbers.PhoneNumberUtil.NANPA_COUNTRY_CODE_) {
    if (this.isNANPACountry(b)) {
      return d + " " + c;
    }
  } else {
    if (null != f && d == this.getCountryCodeForValidRegion_(b)) {
      d = this.chooseFormattingPatternForNumber_(f.numberFormatArray(), e);
      if (null == d) {
        return c;
      }
      d = d.clone();
      d.setPattern("(\\d+)(.*)");
      d.setFormat("$1$2");
      return this.formatNsnUsingPattern_(c, d, i18n.phonenumbers.PhoneNumberFormat.NATIONAL);
    }
  }
  e = "";
  null != f && (e = f.getInternationalPrefixOrDefault(), e = i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(i18n.phonenumbers.PhoneNumberUtil.SINGLE_INTERNATIONAL_PREFIX_, e) ? e : f.getPreferredInternationalPrefixOrDefault());
  f = this.getRegionCodeForCountryCode(d);
  f = this.getMetadataForRegionOrCallingCode_(d, f);
  f = this.maybeGetFormattedExtension_(a, f, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL);
  return 0 < e.length ? e + " " + d + " " + c + f : this.prefixNumberWithCountryCallingCode_(d, i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL, c, f);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getNationalSignificantNumber = function(a) {
  if (!a.hasNationalNumber()) {
    return "";
  }
  var b = "" + a.getNationalNumber();
  return a.hasItalianLeadingZero() && a.getItalianLeadingZero() && 0 < a.getNumberOfLeadingZerosOrDefault() ? Array(a.getNumberOfLeadingZerosOrDefault() + 1).join("0") + b : b;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.prefixNumberWithCountryCallingCode_ = function(a, b, c, d) {
  switch(b) {
    case i18n.phonenumbers.PhoneNumberFormat.E164:
      return i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN + a + c + d;
    case i18n.phonenumbers.PhoneNumberFormat.INTERNATIONAL:
      return i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN + a + " " + c + d;
    case i18n.phonenumbers.PhoneNumberFormat.RFC3966:
      return i18n.phonenumbers.PhoneNumberUtil.RFC3966_PREFIX_ + i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN + a + "-" + c + d;
    default:
      return c + d;
  }
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formatNsn_ = function(a, b, c, d) {
  b = 0 == b.intlNumberFormatArray().length || c == i18n.phonenumbers.PhoneNumberFormat.NATIONAL ? b.numberFormatArray() : b.intlNumberFormatArray();
  b = this.chooseFormattingPatternForNumber_(b, a);
  return null == b ? a : this.formatNsnUsingPattern_(a, b, c, d);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.chooseFormattingPatternForNumber_ = function(a, b) {
  for (var c, d = a.length, e = 0; e < d; ++e) {
    c = a[e];
    var f = c.leadingDigitsPatternCount();
    if (0 == f || 0 == b.search(c.getLeadingDigitsPattern(f - 1))) {
      if (f = new RegExp(c.getPattern()), i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(f, b)) {
        return c;
      }
    }
  }
  return null;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.formatNsnUsingPattern_ = function(a, b, c, d) {
  var e = b.getFormatOrDefault(), f = new RegExp(b.getPattern()), g = b.getDomesticCarrierCodeFormattingRuleOrDefault();
  c == i18n.phonenumbers.PhoneNumberFormat.NATIONAL && null != d && 0 < d.length && 0 < g.length ? (b = g.replace(i18n.phonenumbers.PhoneNumberUtil.CC_PATTERN_, d), e = e.replace(i18n.phonenumbers.PhoneNumberUtil.FIRST_GROUP_PATTERN_, b), a = a.replace(f, e)) : (b = b.getNationalPrefixFormattingRuleOrDefault(), a = c == i18n.phonenumbers.PhoneNumberFormat.NATIONAL && null != b && 0 < b.length ? a.replace(f, e.replace(i18n.phonenumbers.PhoneNumberUtil.FIRST_GROUP_PATTERN_, b)) : a.replace(f, e));
  c == i18n.phonenumbers.PhoneNumberFormat.RFC3966 && (a = a.replace(new RegExp("^" + i18n.phonenumbers.PhoneNumberUtil.SEPARATOR_PATTERN_), ""), a = a.replace(new RegExp(i18n.phonenumbers.PhoneNumberUtil.SEPARATOR_PATTERN_, "g"), "-"));
  return a;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getExampleNumber = function(a) {
  return this.getExampleNumberForType(a, i18n.phonenumbers.PhoneNumberType.FIXED_LINE);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getExampleNumberForType = function(a, b) {
  if (!this.isValidRegionCode_(a)) {
    return null;
  }
  var c = i18n.phonenumbers.PhoneNumberUtil.getNumberDescByType_(this.getMetadataForRegion(a), b);
  try {
    if (c.hasExampleNumber()) {
      return this.parse(c.getExampleNumber(), a);
    }
  } catch (d) {
  }
  return null;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getExampleNumberForNonGeoEntity = function(a) {
  var b = this.getMetadataForNonGeographicalRegion(a);
  if (null != b && (b = goog.array.find([b.getMobile(), b.getTollFree(), b.getSharedCost(), b.getVoip(), b.getVoicemail(), b.getUan(), b.getPremiumRate()], function(a, b) {
    return a.hasExampleNumber();
  }), null != b)) {
    try {
      return this.parse("+" + a + b.getExampleNumber(), "ZZ");
    } catch (c) {
    }
  }
  return null;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.maybeGetFormattedExtension_ = function(a, b, c) {
  return a.hasExtension() && 0 != a.getExtension().length ? c == i18n.phonenumbers.PhoneNumberFormat.RFC3966 ? i18n.phonenumbers.PhoneNumberUtil.RFC3966_EXTN_PREFIX_ + a.getExtension() : b.hasPreferredExtnPrefix() ? b.getPreferredExtnPrefix() + a.getExtensionOrDefault() : i18n.phonenumbers.PhoneNumberUtil.DEFAULT_EXTN_PREFIX_ + a.getExtensionOrDefault() : "";
};
i18n.phonenumbers.PhoneNumberUtil.getNumberDescByType_ = function(a, b) {
  switch(b) {
    case i18n.phonenumbers.PhoneNumberType.PREMIUM_RATE:
      return a.getPremiumRate();
    case i18n.phonenumbers.PhoneNumberType.TOLL_FREE:
      return a.getTollFree();
    case i18n.phonenumbers.PhoneNumberType.MOBILE:
      return a.getMobile();
    case i18n.phonenumbers.PhoneNumberType.FIXED_LINE:
    case i18n.phonenumbers.PhoneNumberType.FIXED_LINE_OR_MOBILE:
      return a.getFixedLine();
    case i18n.phonenumbers.PhoneNumberType.SHARED_COST:
      return a.getSharedCost();
    case i18n.phonenumbers.PhoneNumberType.VOIP:
      return a.getVoip();
    case i18n.phonenumbers.PhoneNumberType.PERSONAL_NUMBER:
      return a.getPersonalNumber();
    case i18n.phonenumbers.PhoneNumberType.PAGER:
      return a.getPager();
    case i18n.phonenumbers.PhoneNumberType.UAN:
      return a.getUan();
    case i18n.phonenumbers.PhoneNumberType.VOICEMAIL:
      return a.getVoicemail();
    default:
      return a.getGeneralDesc();
  }
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getNumberType = function(a) {
  var b = this.getRegionCodeForNumber(a);
  b = this.getMetadataForRegionOrCallingCode_(a.getCountryCodeOrDefault(), b);
  if (null == b) {
    return i18n.phonenumbers.PhoneNumberType.UNKNOWN;
  }
  a = this.getNationalSignificantNumber(a);
  return this.getNumberTypeHelper_(a, b);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getNumberTypeHelper_ = function(a, b) {
  return this.isNumberMatchingDesc_(a, b.getGeneralDesc()) ? this.isNumberMatchingDesc_(a, b.getPremiumRate()) ? i18n.phonenumbers.PhoneNumberType.PREMIUM_RATE : this.isNumberMatchingDesc_(a, b.getTollFree()) ? i18n.phonenumbers.PhoneNumberType.TOLL_FREE : this.isNumberMatchingDesc_(a, b.getSharedCost()) ? i18n.phonenumbers.PhoneNumberType.SHARED_COST : this.isNumberMatchingDesc_(a, b.getVoip()) ? i18n.phonenumbers.PhoneNumberType.VOIP : this.isNumberMatchingDesc_(a, b.getPersonalNumber()) ? i18n.phonenumbers.PhoneNumberType.PERSONAL_NUMBER : 
  this.isNumberMatchingDesc_(a, b.getPager()) ? i18n.phonenumbers.PhoneNumberType.PAGER : this.isNumberMatchingDesc_(a, b.getUan()) ? i18n.phonenumbers.PhoneNumberType.UAN : this.isNumberMatchingDesc_(a, b.getVoicemail()) ? i18n.phonenumbers.PhoneNumberType.VOICEMAIL : this.isNumberMatchingDesc_(a, b.getFixedLine()) ? b.getSameMobileAndFixedLinePattern() || this.isNumberMatchingDesc_(a, b.getMobile()) ? i18n.phonenumbers.PhoneNumberType.FIXED_LINE_OR_MOBILE : i18n.phonenumbers.PhoneNumberType.FIXED_LINE : 
  !b.getSameMobileAndFixedLinePattern() && this.isNumberMatchingDesc_(a, b.getMobile()) ? i18n.phonenumbers.PhoneNumberType.MOBILE : i18n.phonenumbers.PhoneNumberType.UNKNOWN : i18n.phonenumbers.PhoneNumberType.UNKNOWN;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getMetadataForRegion = function(a) {
  if (null == a) {
    return null;
  }
  a = a.toUpperCase();
  var b = this.regionToMetadataMap[a];
  if (null == b) {
    b = new goog.proto2.PbLiteSerializer;
    var c = i18n.phonenumbers.metadata.countryToMetadata[a];
    if (null == c) {
      return null;
    }
    b = b.deserialize(i18n.phonenumbers.PhoneMetadata.getDescriptor(), c);
    this.regionToMetadataMap[a] = b;
  }
  return b;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getMetadataForNonGeographicalRegion = function(a) {
  return this.getMetadataForRegion("" + a);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isNumberMatchingDesc_ = function(a, b) {
  var c = a.length;
  return 0 < b.possibleLengthCount() && -1 == goog.array.indexOf(b.possibleLengthArray(), c) ? !1 : i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(b.getNationalNumberPatternOrDefault(), a);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isValidNumber = function(a) {
  var b = this.getRegionCodeForNumber(a);
  return this.isValidNumberForRegion(a, b);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isValidNumberForRegion = function(a, b) {
  var c = a.getCountryCodeOrDefault(), d = this.getMetadataForRegionOrCallingCode_(c, b);
  if (null == d || i18n.phonenumbers.PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY != b && c != this.getCountryCodeForValidRegion_(b)) {
    return !1;
  }
  c = this.getNationalSignificantNumber(a);
  return this.getNumberTypeHelper_(c, d) != i18n.phonenumbers.PhoneNumberType.UNKNOWN;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getRegionCodeForNumber = function(a) {
  if (null == a) {
    return null;
  }
  var b = a.getCountryCodeOrDefault();
  b = i18n.phonenumbers.metadata.countryCodeToRegionCodeMap[b];
  return null == b ? null : 1 == b.length ? b[0] : this.getRegionCodeForNumberFromRegionList_(a, b);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getRegionCodeForNumberFromRegionList_ = function(a, b) {
  for (var c = this.getNationalSignificantNumber(a), d, e = b.length, f = 0; f < e; f++) {
    d = b[f];
    var g = this.getMetadataForRegion(d);
    if (g.hasLeadingDigits()) {
      if (0 == c.search(g.getLeadingDigits())) {
        return d;
      }
    } else {
      if (this.getNumberTypeHelper_(c, g) != i18n.phonenumbers.PhoneNumberType.UNKNOWN) {
        return d;
      }
    }
  }
  return null;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getRegionCodeForCountryCode = function(a) {
  a = i18n.phonenumbers.metadata.countryCodeToRegionCodeMap[a];
  return null == a ? i18n.phonenumbers.PhoneNumberUtil.UNKNOWN_REGION_ : a[0];
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getRegionCodesForCountryCode = function(a) {
  a = i18n.phonenumbers.metadata.countryCodeToRegionCodeMap[a];
  return null == a ? [] : a;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getCountryCodeForRegion = function(a) {
  return this.isValidRegionCode_(a) ? this.getCountryCodeForValidRegion_(a) : 0;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getCountryCodeForValidRegion_ = function(a) {
  var b = this.getMetadataForRegion(a);
  if (null == b) {
    throw Error("Invalid region code: " + a);
  }
  return b.getCountryCodeOrDefault();
};
i18n.phonenumbers.PhoneNumberUtil.prototype.getNddPrefixForRegion = function(a, b) {
  var c = this.getMetadataForRegion(a);
  if (null == c) {
    return null;
  }
  c = c.getNationalPrefixOrDefault();
  if (0 == c.length) {
    return null;
  }
  b && (c = c.replace("~", ""));
  return c;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isNANPACountry = function(a) {
  return null != a && goog.array.contains(i18n.phonenumbers.metadata.countryCodeToRegionCodeMap[i18n.phonenumbers.PhoneNumberUtil.NANPA_COUNTRY_CODE_], a.toUpperCase());
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isAlphaNumber = function(a) {
  if (!i18n.phonenumbers.PhoneNumberUtil.isViablePhoneNumber(a)) {
    return !1;
  }
  a = new goog.string.StringBuffer(a);
  this.maybeStripExtension(a);
  return i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(i18n.phonenumbers.PhoneNumberUtil.VALID_ALPHA_PHONE_PATTERN_, a.toString());
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isPossibleNumber = function(a) {
  a = this.isPossibleNumberWithReason(a);
  return a == i18n.phonenumbers.PhoneNumberUtil.ValidationResult.IS_POSSIBLE || a == i18n.phonenumbers.PhoneNumberUtil.ValidationResult.IS_POSSIBLE_LOCAL_ONLY;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isPossibleNumberForType = function(a, b) {
  var c = this.isPossibleNumberForTypeWithReason(a, b);
  return c == i18n.phonenumbers.PhoneNumberUtil.ValidationResult.IS_POSSIBLE || c == i18n.phonenumbers.PhoneNumberUtil.ValidationResult.IS_POSSIBLE_LOCAL_ONLY;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.testNumberLength_ = function(a, b) {
  return this.testNumberLengthForType_(a, b, i18n.phonenumbers.PhoneNumberType.UNKNOWN);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.testNumberLengthForType_ = function(a, b, c) {
  var d = i18n.phonenumbers.PhoneNumberUtil.getNumberDescByType_(b, c), e = 0 == d.possibleLengthCount() ? b.getGeneralDesc().possibleLengthArray() : d.possibleLengthArray();
  d = d.possibleLengthLocalOnlyArray();
  if (c == i18n.phonenumbers.PhoneNumberType.FIXED_LINE_OR_MOBILE) {
    if (i18n.phonenumbers.PhoneNumberUtil.descHasPossibleNumberData_(i18n.phonenumbers.PhoneNumberUtil.getNumberDescByType_(b, i18n.phonenumbers.PhoneNumberType.FIXED_LINE))) {
      c = i18n.phonenumbers.PhoneNumberUtil.getNumberDescByType_(b, i18n.phonenumbers.PhoneNumberType.MOBILE), i18n.phonenumbers.PhoneNumberUtil.descHasPossibleNumberData_(c) && (e = e.concat(0 == c.possibleLengthCount() ? b.getGeneralDesc().possibleLengthArray() : c.possibleLengthArray()), goog.array.sort(e), 0 == d.length ? d = c.possibleLengthLocalOnlyArray() : (d = d.concat(c.possibleLengthLocalOnlyArray()), goog.array.sort(d)));
    } else {
      return this.testNumberLengthForType_(a, b, i18n.phonenumbers.PhoneNumberType.MOBILE);
    }
  }
  if (-1 == e[0]) {
    return i18n.phonenumbers.PhoneNumberUtil.ValidationResult.INVALID_LENGTH;
  }
  a = a.length;
  if (-1 < goog.array.indexOf(d, a)) {
    return i18n.phonenumbers.PhoneNumberUtil.ValidationResult.IS_POSSIBLE_LOCAL_ONLY;
  }
  b = e[0];
  return b == a ? i18n.phonenumbers.PhoneNumberUtil.ValidationResult.IS_POSSIBLE : b > a ? i18n.phonenumbers.PhoneNumberUtil.ValidationResult.TOO_SHORT : e[e.length - 1] < a ? i18n.phonenumbers.PhoneNumberUtil.ValidationResult.TOO_LONG : -1 < goog.array.indexOf(e, a, 1) ? i18n.phonenumbers.PhoneNumberUtil.ValidationResult.IS_POSSIBLE : i18n.phonenumbers.PhoneNumberUtil.ValidationResult.INVALID_LENGTH;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isPossibleNumberWithReason = function(a) {
  return this.isPossibleNumberForTypeWithReason(a, i18n.phonenumbers.PhoneNumberType.UNKNOWN);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isPossibleNumberForTypeWithReason = function(a, b) {
  var c = this.getNationalSignificantNumber(a), d = a.getCountryCodeOrDefault();
  if (!this.hasValidCountryCallingCode_(d)) {
    return i18n.phonenumbers.PhoneNumberUtil.ValidationResult.INVALID_COUNTRY_CODE;
  }
  var e = this.getRegionCodeForCountryCode(d);
  d = this.getMetadataForRegionOrCallingCode_(d, e);
  return this.testNumberLengthForType_(c, d, b);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isPossibleNumberString = function(a, b) {
  try {
    return this.isPossibleNumber(this.parse(a, b));
  } catch (c) {
    return !1;
  }
};
i18n.phonenumbers.PhoneNumberUtil.prototype.truncateTooLongNumber = function(a) {
  if (this.isValidNumber(a)) {
    return !0;
  }
  var b = a.clone(), c = a.getNationalNumberOrDefault();
  do {
    if (c = Math.floor(c / 10), b.setNationalNumber(c), 0 == c || this.isPossibleNumberWithReason(b) == i18n.phonenumbers.PhoneNumberUtil.ValidationResult.TOO_SHORT) {
      return !1;
    }
  } while (!this.isValidNumber(b));
  a.setNationalNumber(c);
  return !0;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.extractCountryCode = function(a, b) {
  var c = a.toString();
  if (0 == c.length || "0" == c.charAt(0)) {
    return 0;
  }
  for (var d, e = c.length, f = 1; f <= i18n.phonenumbers.PhoneNumberUtil.MAX_LENGTH_COUNTRY_CODE_ && f <= e; ++f) {
    if (d = parseInt(c.substring(0, f), 10), d in i18n.phonenumbers.metadata.countryCodeToRegionCodeMap) {
      return b.append(c.substring(f)), d;
    }
  }
  return 0;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.maybeExtractCountryCode = function(a, b, c, d, e) {
  if (0 == a.length) {
    return 0;
  }
  a = new goog.string.StringBuffer(a);
  var f;
  null != b && (f = b.getInternationalPrefix());
  null == f && (f = "NonMatch");
  f = this.maybeStripInternationalPrefixAndNormalize(a, f);
  d && e.setCountryCodeSource(f);
  if (f != i18n.phonenumbers.PhoneNumber.CountryCodeSource.FROM_DEFAULT_COUNTRY) {
    if (a.getLength() <= i18n.phonenumbers.PhoneNumberUtil.MIN_LENGTH_FOR_NSN_) {
      throw Error(i18n.phonenumbers.Error.TOO_SHORT_AFTER_IDD);
    }
    b = this.extractCountryCode(a, c);
    if (0 != b) {
      return e.setCountryCode(b), b;
    }
    throw Error(i18n.phonenumbers.Error.INVALID_COUNTRY_CODE);
  }
  if (null != b) {
    f = b.getCountryCodeOrDefault();
    var g = "" + f, h = a.toString();
    if (goog.string.startsWith(h, g) && (g = new goog.string.StringBuffer(h.substring(g.length)), h = b.getGeneralDesc(), h = new RegExp(h.getNationalNumberPatternOrDefault()), this.maybeStripNationalPrefixAndCarrierCode(g, b, null), g = g.toString(), !i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(h, a.toString()) && i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(h, g) || this.testNumberLength_(a.toString(), b) == i18n.phonenumbers.PhoneNumberUtil.ValidationResult.TOO_LONG)) {
      return c.append(g), d && e.setCountryCodeSource(i18n.phonenumbers.PhoneNumber.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN), e.setCountryCode(f), f;
    }
  }
  e.setCountryCode(0);
  return 0;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.parsePrefixAsIdd_ = function(a, b) {
  var c = b.toString();
  if (0 == c.search(a)) {
    var d = c.match(a)[0].length, e = c.substring(d).match(i18n.phonenumbers.PhoneNumberUtil.CAPTURING_DIGIT_PATTERN);
    if (e && null != e[1] && 0 < e[1].length && "0" == i18n.phonenumbers.PhoneNumberUtil.normalizeDigitsOnly(e[1])) {
      return !1;
    }
    b.clear();
    b.append(c.substring(d));
    return !0;
  }
  return !1;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.maybeStripInternationalPrefixAndNormalize = function(a, b) {
  var c = a.toString();
  if (0 == c.length) {
    return i18n.phonenumbers.PhoneNumber.CountryCodeSource.FROM_DEFAULT_COUNTRY;
  }
  if (i18n.phonenumbers.PhoneNumberUtil.LEADING_PLUS_CHARS_PATTERN_.test(c)) {
    return c = c.replace(i18n.phonenumbers.PhoneNumberUtil.LEADING_PLUS_CHARS_PATTERN_, ""), a.clear(), a.append(i18n.phonenumbers.PhoneNumberUtil.normalize(c)), i18n.phonenumbers.PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN;
  }
  c = new RegExp(b);
  i18n.phonenumbers.PhoneNumberUtil.normalizeSB_(a);
  return this.parsePrefixAsIdd_(c, a) ? i18n.phonenumbers.PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_IDD : i18n.phonenumbers.PhoneNumber.CountryCodeSource.FROM_DEFAULT_COUNTRY;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.maybeStripNationalPrefixAndCarrierCode = function(a, b, c) {
  var d = a.toString(), e = d.length, f = b.getNationalPrefixForParsing();
  if (0 == e || null == f || 0 == f.length) {
    return !1;
  }
  var g = new RegExp("^(?:" + f + ")");
  if (e = g.exec(d)) {
    f = new RegExp(b.getGeneralDesc().getNationalNumberPatternOrDefault());
    var h = i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(f, d), k = e.length - 1;
    b = b.getNationalPrefixTransformRule();
    if (null == b || 0 == b.length || null == e[k] || 0 == e[k].length) {
      if (h && !i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(f, d.substring(e[0].length))) {
        return !1;
      }
      null != c && 0 < k && null != e[k] && c.append(e[1]);
      a.set(d.substring(e[0].length));
    } else {
      d = d.replace(g, b);
      if (h && !i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_(f, d)) {
        return !1;
      }
      null != c && 0 < k && c.append(e[1]);
      a.set(d);
    }
    return !0;
  }
  return !1;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.maybeStripExtension = function(a) {
  var b = a.toString(), c = b.search(i18n.phonenumbers.PhoneNumberUtil.EXTN_PATTERN_);
  if (0 <= c && i18n.phonenumbers.PhoneNumberUtil.isViablePhoneNumber(b.substring(0, c))) {
    for (var d = b.match(i18n.phonenumbers.PhoneNumberUtil.EXTN_PATTERN_), e = d.length, f = 1; f < e; ++f) {
      if (null != d[f] && 0 < d[f].length) {
        return a.clear(), a.append(b.substring(0, c)), d[f];
      }
    }
  }
  return "";
};
i18n.phonenumbers.PhoneNumberUtil.prototype.checkRegionForParsing_ = function(a, b) {
  return this.isValidRegionCode_(b) || null != a && 0 < a.length && i18n.phonenumbers.PhoneNumberUtil.LEADING_PLUS_CHARS_PATTERN_.test(a);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.parse = function(a, b) {
  return this.parseHelper_(a, b, !1, !0);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.parseAndKeepRawInput = function(a, b) {
  if (!this.isValidRegionCode_(b) && 0 < a.length && a.charAt(0) != i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN) {
    throw Error(i18n.phonenumbers.Error.INVALID_COUNTRY_CODE);
  }
  return this.parseHelper_(a, b, !0, !0);
};
i18n.phonenumbers.PhoneNumberUtil.setItalianLeadingZerosForPhoneNumber_ = function(a, b) {
  if (1 < a.length && "0" == a.charAt(0)) {
    b.setItalianLeadingZero(!0);
    for (var c = 1; c < a.length - 1 && "0" == a.charAt(c);) {
      c++;
    }
    1 != c && b.setNumberOfLeadingZeros(c);
  }
};
i18n.phonenumbers.PhoneNumberUtil.prototype.parseHelper_ = function(a, b, c, d) {
  if (null == a) {
    throw Error(i18n.phonenumbers.Error.NOT_A_NUMBER);
  }
  if (a.length > i18n.phonenumbers.PhoneNumberUtil.MAX_INPUT_STRING_LENGTH_) {
    throw Error(i18n.phonenumbers.Error.TOO_LONG);
  }
  var e = new goog.string.StringBuffer;
  this.buildNationalNumberForParsing_(a, e);
  if (!i18n.phonenumbers.PhoneNumberUtil.isViablePhoneNumber(e.toString())) {
    throw Error(i18n.phonenumbers.Error.NOT_A_NUMBER);
  }
  if (d && !this.checkRegionForParsing_(e.toString(), b)) {
    throw Error(i18n.phonenumbers.Error.INVALID_COUNTRY_CODE);
  }
  d = new i18n.phonenumbers.PhoneNumber;
  c && d.setRawInput(a);
  a = this.maybeStripExtension(e);
  0 < a.length && d.setExtension(a);
  var f = this.getMetadataForRegion(b);
  a = new goog.string.StringBuffer;
  var g = 0, h = e.toString();
  try {
    g = this.maybeExtractCountryCode(h, f, a, c, d);
  } catch (k) {
    if (k.message == i18n.phonenumbers.Error.INVALID_COUNTRY_CODE && i18n.phonenumbers.PhoneNumberUtil.LEADING_PLUS_CHARS_PATTERN_.test(h)) {
      if (h = h.replace(i18n.phonenumbers.PhoneNumberUtil.LEADING_PLUS_CHARS_PATTERN_, ""), g = this.maybeExtractCountryCode(h, f, a, c, d), 0 == g) {
        throw k;
      }
    } else {
      throw k;
    }
  }
  0 != g ? (e = this.getRegionCodeForCountryCode(g), e != b && (f = this.getMetadataForRegionOrCallingCode_(g, e))) : (i18n.phonenumbers.PhoneNumberUtil.normalizeSB_(e), a.append(e.toString()), null != b ? (g = f.getCountryCodeOrDefault(), d.setCountryCode(g)) : c && d.clearCountryCodeSource());
  if (a.getLength() < i18n.phonenumbers.PhoneNumberUtil.MIN_LENGTH_FOR_NSN_) {
    throw Error(i18n.phonenumbers.Error.TOO_SHORT_NSN);
  }
  null != f && (b = new goog.string.StringBuffer, e = new goog.string.StringBuffer(a.toString()), this.maybeStripNationalPrefixAndCarrierCode(e, f, b), f = this.testNumberLength_(e.toString(), f), f != i18n.phonenumbers.PhoneNumberUtil.ValidationResult.TOO_SHORT && f != i18n.phonenumbers.PhoneNumberUtil.ValidationResult.IS_POSSIBLE_LOCAL_ONLY && f != i18n.phonenumbers.PhoneNumberUtil.ValidationResult.INVALID_LENGTH && (a = e, c && 0 < b.toString().length && d.setPreferredDomesticCarrierCode(b.toString())));
  c = a.toString();
  b = c.length;
  if (b < i18n.phonenumbers.PhoneNumberUtil.MIN_LENGTH_FOR_NSN_) {
    throw Error(i18n.phonenumbers.Error.TOO_SHORT_NSN);
  }
  if (b > i18n.phonenumbers.PhoneNumberUtil.MAX_LENGTH_FOR_NSN_) {
    throw Error(i18n.phonenumbers.Error.TOO_LONG);
  }
  i18n.phonenumbers.PhoneNumberUtil.setItalianLeadingZerosForPhoneNumber_(c, d);
  d.setNationalNumber(parseInt(c, 10));
  return d;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.buildNationalNumberForParsing_ = function(a, b) {
  var c = a.indexOf(i18n.phonenumbers.PhoneNumberUtil.RFC3966_PHONE_CONTEXT_);
  if (0 <= c) {
    var d = c + i18n.phonenumbers.PhoneNumberUtil.RFC3966_PHONE_CONTEXT_.length;
    if (a.charAt(d) == i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN) {
      var e = a.indexOf(";", d);
      0 < e ? b.append(a.substring(d, e)) : b.append(a.substring(d));
    }
    d = a.indexOf(i18n.phonenumbers.PhoneNumberUtil.RFC3966_PREFIX_);
    b.append(a.substring(0 <= d ? d + i18n.phonenumbers.PhoneNumberUtil.RFC3966_PREFIX_.length : 0, c));
  } else {
    b.append(i18n.phonenumbers.PhoneNumberUtil.extractPossibleNumber(a));
  }
  c = b.toString();
  d = c.indexOf(i18n.phonenumbers.PhoneNumberUtil.RFC3966_ISDN_SUBADDRESS_);
  0 < d && (b.clear(), b.append(c.substring(0, d)));
};
i18n.phonenumbers.PhoneNumberUtil.copyCoreFieldsOnly_ = function(a) {
  var b = new i18n.phonenumbers.PhoneNumber;
  b.setCountryCode(a.getCountryCodeOrDefault());
  b.setNationalNumber(a.getNationalNumberOrDefault());
  0 < a.getExtensionOrDefault().length && b.setExtension(a.getExtensionOrDefault());
  a.getItalianLeadingZero() && (b.setItalianLeadingZero(!0), b.setNumberOfLeadingZeros(a.getNumberOfLeadingZerosOrDefault()));
  return b;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isNumberMatch = function(a, b) {
  if ("string" == typeof a) {
    try {
      var c = this.parse(a, i18n.phonenumbers.PhoneNumberUtil.UNKNOWN_REGION_);
    } catch (g) {
      if (g.message != i18n.phonenumbers.Error.INVALID_COUNTRY_CODE) {
        return i18n.phonenumbers.PhoneNumberUtil.MatchType.NOT_A_NUMBER;
      }
      if ("string" != typeof b) {
        var d = this.getRegionCodeForCountryCode(b.getCountryCodeOrDefault());
        if (d != i18n.phonenumbers.PhoneNumberUtil.UNKNOWN_REGION_) {
          try {
            c = this.parse(a, d);
          } catch (h) {
            return i18n.phonenumbers.PhoneNumberUtil.MatchType.NOT_A_NUMBER;
          }
          c = this.isNumberMatch(c, b);
          return c == i18n.phonenumbers.PhoneNumberUtil.MatchType.EXACT_MATCH ? i18n.phonenumbers.PhoneNumberUtil.MatchType.NSN_MATCH : c;
        }
      }
      try {
        c = this.parseHelper_(a, null, !1, !1);
      } catch (h) {
        return i18n.phonenumbers.PhoneNumberUtil.MatchType.NOT_A_NUMBER;
      }
    }
  } else {
    c = a.clone();
  }
  if ("string" == typeof b) {
    try {
      var e = this.parse(b, i18n.phonenumbers.PhoneNumberUtil.UNKNOWN_REGION_);
      return this.isNumberMatch(a, e);
    } catch (g) {
      return g.message != i18n.phonenumbers.Error.INVALID_COUNTRY_CODE ? i18n.phonenumbers.PhoneNumberUtil.MatchType.NOT_A_NUMBER : this.isNumberMatch(b, c);
    }
  } else {
    e = b.clone();
  }
  c = i18n.phonenumbers.PhoneNumberUtil.copyCoreFieldsOnly_(c);
  e = i18n.phonenumbers.PhoneNumberUtil.copyCoreFieldsOnly_(e);
  if (c.hasExtension() && e.hasExtension() && c.getExtension() != e.getExtension()) {
    return i18n.phonenumbers.PhoneNumberUtil.MatchType.NO_MATCH;
  }
  d = c.getCountryCodeOrDefault();
  var f = e.getCountryCodeOrDefault();
  if (0 != d && 0 != f) {
    return c.equals(e) ? i18n.phonenumbers.PhoneNumberUtil.MatchType.EXACT_MATCH : d == f && this.isNationalNumberSuffixOfTheOther_(c, e) ? i18n.phonenumbers.PhoneNumberUtil.MatchType.SHORT_NSN_MATCH : i18n.phonenumbers.PhoneNumberUtil.MatchType.NO_MATCH;
  }
  c.setCountryCode(0);
  e.setCountryCode(0);
  return c.equals(e) ? i18n.phonenumbers.PhoneNumberUtil.MatchType.NSN_MATCH : this.isNationalNumberSuffixOfTheOther_(c, e) ? i18n.phonenumbers.PhoneNumberUtil.MatchType.SHORT_NSN_MATCH : i18n.phonenumbers.PhoneNumberUtil.MatchType.NO_MATCH;
};
i18n.phonenumbers.PhoneNumberUtil.prototype.isNationalNumberSuffixOfTheOther_ = function(a, b) {
  var c = "" + a.getNationalNumber(), d = "" + b.getNationalNumber();
  return goog.string.endsWith(c, d) || goog.string.endsWith(d, c);
};
i18n.phonenumbers.PhoneNumberUtil.prototype.canBeInternationallyDialled = function(a) {
  var b = this.getMetadataForRegion(this.getRegionCodeForNumber(a));
  if (null == b) {
    return !0;
  }
  a = this.getNationalSignificantNumber(a);
  return !this.isNumberMatchingDesc_(a, b.getNoInternationalDialling());
};
i18n.phonenumbers.PhoneNumberUtil.matchesEntirely_ = function(a, b) {
  var c = "string" == typeof a ? b.match("^(?:" + a + ")$") : b.match(a);
  return c && c[0].length == b.length ? !0 : !1;
};
i18n.phonenumbers.AsYouTypeFormatter = function(a) {
  this.DIGIT_PLACEHOLDER_ = "\u2008";
  this.DIGIT_PATTERN_ = new RegExp(this.DIGIT_PLACEHOLDER_);
  this.currentOutput_ = "";
  this.formattingTemplate_ = new goog.string.StringBuffer;
  this.currentFormattingPattern_ = "";
  this.accruedInput_ = new goog.string.StringBuffer;
  this.accruedInputWithoutFormatting_ = new goog.string.StringBuffer;
  this.ableToFormat_ = !0;
  this.isExpectingCountryCallingCode_ = this.isCompleteNumber_ = this.inputHasFormatting_ = !1;
  this.phoneUtil_ = i18n.phonenumbers.PhoneNumberUtil.getInstance();
  this.positionToRemember_ = this.originalPosition_ = this.lastMatchPosition_ = 0;
  this.prefixBeforeNationalNumber_ = new goog.string.StringBuffer;
  this.shouldAddSpaceAfterNationalPrefix_ = !1;
  this.extractedNationalPrefix_ = "";
  this.nationalNumber_ = new goog.string.StringBuffer;
  this.possibleFormats_ = [];
  this.defaultCountry_ = a;
  this.defaultMetadata_ = this.currentMetadata_ = this.getMetadataForRegion_(this.defaultCountry_);
};
i18n.phonenumbers.AsYouTypeFormatter.SEPARATOR_BEFORE_NATIONAL_NUMBER_ = " ";
i18n.phonenumbers.AsYouTypeFormatter.EMPTY_METADATA_ = new i18n.phonenumbers.PhoneMetadata;
i18n.phonenumbers.AsYouTypeFormatter.EMPTY_METADATA_.setInternationalPrefix("NA");
i18n.phonenumbers.AsYouTypeFormatter.CHARACTER_CLASS_PATTERN_ = /\[([^\[\]])*\]/g;
i18n.phonenumbers.AsYouTypeFormatter.STANDALONE_DIGIT_PATTERN_ = /\d(?=[^,}][^,}])/g;
i18n.phonenumbers.AsYouTypeFormatter.ELIGIBLE_FORMAT_PATTERN_ = new RegExp("^[" + i18n.phonenumbers.PhoneNumberUtil.VALID_PUNCTUATION + "]*(\\$\\d[" + i18n.phonenumbers.PhoneNumberUtil.VALID_PUNCTUATION + "]*)+$");
i18n.phonenumbers.AsYouTypeFormatter.NATIONAL_PREFIX_SEPARATORS_PATTERN_ = /[- ]/;
i18n.phonenumbers.AsYouTypeFormatter.MIN_LEADING_DIGITS_LENGTH_ = 3;
i18n.phonenumbers.AsYouTypeFormatter.prototype.getMetadataForRegion_ = function(a) {
  a = this.phoneUtil_.getCountryCodeForRegion(a);
  a = this.phoneUtil_.getRegionCodeForCountryCode(a);
  a = this.phoneUtil_.getMetadataForRegion(a);
  return null != a ? a : i18n.phonenumbers.AsYouTypeFormatter.EMPTY_METADATA_;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.maybeCreateNewTemplate_ = function() {
  for (var a = this.possibleFormats_.length, b = 0; b < a; ++b) {
    var c = this.possibleFormats_[b], d = c.getPatternOrDefault();
    if (this.currentFormattingPattern_ == d) {
      return !1;
    }
    if (this.createFormattingTemplate_(c)) {
      return this.currentFormattingPattern_ = d, this.shouldAddSpaceAfterNationalPrefix_ = i18n.phonenumbers.AsYouTypeFormatter.NATIONAL_PREFIX_SEPARATORS_PATTERN_.test(c.getNationalPrefixFormattingRule()), this.lastMatchPosition_ = 0, !0;
    }
  }
  return this.ableToFormat_ = !1;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.getAvailableFormats_ = function(a) {
  for (var b = this.isCompleteNumber_ && 0 < this.currentMetadata_.intlNumberFormatCount() ? this.currentMetadata_.intlNumberFormatArray() : this.currentMetadata_.numberFormatArray(), c = b.length, d = 0; d < c; ++d) {
    var e = b[d];
    (!this.currentMetadata_.hasNationalPrefix() || this.isCompleteNumber_ || e.getNationalPrefixOptionalWhenFormatting() || this.phoneUtil_.formattingRuleHasFirstGroupOnly(e.getNationalPrefixFormattingRuleOrDefault())) && this.isFormatEligible_(e.getFormatOrDefault()) && this.possibleFormats_.push(e);
  }
  this.narrowDownPossibleFormats_(a);
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.isFormatEligible_ = function(a) {
  return i18n.phonenumbers.AsYouTypeFormatter.ELIGIBLE_FORMAT_PATTERN_.test(a);
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.narrowDownPossibleFormats_ = function(a) {
  for (var b = [], c = a.length - i18n.phonenumbers.AsYouTypeFormatter.MIN_LEADING_DIGITS_LENGTH_, d = this.possibleFormats_.length, e = 0; e < d; ++e) {
    var f = this.possibleFormats_[e];
    if (0 == f.leadingDigitsPatternCount()) {
      b.push(this.possibleFormats_[e]);
    } else {
      var g = Math.min(c, f.leadingDigitsPatternCount() - 1);
      f = f.getLeadingDigitsPattern(g);
      0 == a.search(f) && b.push(this.possibleFormats_[e]);
    }
  }
  this.possibleFormats_ = b;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.createFormattingTemplate_ = function(a) {
  var b = a.getPatternOrDefault();
  if (-1 != b.indexOf("|")) {
    return !1;
  }
  b = b.replace(i18n.phonenumbers.AsYouTypeFormatter.CHARACTER_CLASS_PATTERN_, "\\d");
  b = b.replace(i18n.phonenumbers.AsYouTypeFormatter.STANDALONE_DIGIT_PATTERN_, "\\d");
  this.formattingTemplate_.clear();
  a = this.getFormattingTemplate_(b, a.getFormatOrDefault());
  return 0 < a.length ? (this.formattingTemplate_.append(a), !0) : !1;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.getFormattingTemplate_ = function(a, b) {
  var c = "999999999999999".match(a)[0];
  if (c.length < this.nationalNumber_.getLength()) {
    return "";
  }
  c = c.replace(new RegExp(a, "g"), b);
  return c = c.replace(RegExp("9", "g"), this.DIGIT_PLACEHOLDER_);
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.clear = function() {
  this.currentOutput_ = "";
  this.accruedInput_.clear();
  this.accruedInputWithoutFormatting_.clear();
  this.formattingTemplate_.clear();
  this.lastMatchPosition_ = 0;
  this.currentFormattingPattern_ = "";
  this.prefixBeforeNationalNumber_.clear();
  this.extractedNationalPrefix_ = "";
  this.nationalNumber_.clear();
  this.ableToFormat_ = !0;
  this.inputHasFormatting_ = !1;
  this.originalPosition_ = this.positionToRemember_ = 0;
  this.isExpectingCountryCallingCode_ = this.isCompleteNumber_ = !1;
  this.possibleFormats_ = [];
  this.shouldAddSpaceAfterNationalPrefix_ = !1;
  this.currentMetadata_ != this.defaultMetadata_ && (this.currentMetadata_ = this.getMetadataForRegion_(this.defaultCountry_));
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.inputDigit = function(a) {
  return this.currentOutput_ = this.inputDigitWithOptionToRememberPosition_(a, !1);
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.inputDigitAndRememberPosition = function(a) {
  return this.currentOutput_ = this.inputDigitWithOptionToRememberPosition_(a, !0);
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.inputDigitWithOptionToRememberPosition_ = function(a, b) {
  this.accruedInput_.append(a);
  b && (this.originalPosition_ = this.accruedInput_.getLength());
  this.isDigitOrLeadingPlusSign_(a) ? a = this.normalizeAndAccrueDigitsAndPlusSign_(a, b) : (this.ableToFormat_ = !1, this.inputHasFormatting_ = !0);
  if (!this.ableToFormat_) {
    if (!this.inputHasFormatting_) {
      if (this.attemptToExtractIdd_()) {
        if (this.attemptToExtractCountryCallingCode_()) {
          return this.attemptToChoosePatternWithPrefixExtracted_();
        }
      } else {
        if (this.ableToExtractLongerNdd_()) {
          return this.prefixBeforeNationalNumber_.append(i18n.phonenumbers.AsYouTypeFormatter.SEPARATOR_BEFORE_NATIONAL_NUMBER_), this.attemptToChoosePatternWithPrefixExtracted_();
        }
      }
    }
    return this.accruedInput_.toString();
  }
  switch(this.accruedInputWithoutFormatting_.getLength()) {
    case 0:
    case 1:
    case 2:
      return this.accruedInput_.toString();
    case 3:
      if (this.attemptToExtractIdd_()) {
        this.isExpectingCountryCallingCode_ = !0;
      } else {
        return this.extractedNationalPrefix_ = this.removeNationalPrefixFromNationalNumber_(), this.attemptToChooseFormattingPattern_();
      }
    default:
      if (this.isExpectingCountryCallingCode_) {
        return this.attemptToExtractCountryCallingCode_() && (this.isExpectingCountryCallingCode_ = !1), this.prefixBeforeNationalNumber_.toString() + this.nationalNumber_.toString();
      }
      if (0 < this.possibleFormats_.length) {
        var c = this.inputDigitHelper_(a), d = this.attemptToFormatAccruedDigits_();
        if (0 < d.length) {
          return d;
        }
        this.narrowDownPossibleFormats_(this.nationalNumber_.toString());
        return this.maybeCreateNewTemplate_() ? this.inputAccruedNationalNumber_() : this.ableToFormat_ ? this.appendNationalNumber_(c) : this.accruedInput_.toString();
      }
      return this.attemptToChooseFormattingPattern_();
  }
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.attemptToChoosePatternWithPrefixExtracted_ = function() {
  this.ableToFormat_ = !0;
  this.isExpectingCountryCallingCode_ = !1;
  this.possibleFormats_ = [];
  this.lastMatchPosition_ = 0;
  this.formattingTemplate_.clear();
  this.currentFormattingPattern_ = "";
  return this.attemptToChooseFormattingPattern_();
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.getExtractedNationalPrefix_ = function() {
  return this.extractedNationalPrefix_;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.ableToExtractLongerNdd_ = function() {
  if (0 < this.extractedNationalPrefix_.length) {
    var a = this.nationalNumber_.toString();
    this.nationalNumber_.clear();
    this.nationalNumber_.append(this.extractedNationalPrefix_);
    this.nationalNumber_.append(a);
    a = this.prefixBeforeNationalNumber_.toString();
    var b = a.lastIndexOf(this.extractedNationalPrefix_);
    this.prefixBeforeNationalNumber_.clear();
    this.prefixBeforeNationalNumber_.append(a.substring(0, b));
  }
  return this.extractedNationalPrefix_ != this.removeNationalPrefixFromNationalNumber_();
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.isDigitOrLeadingPlusSign_ = function(a) {
  return i18n.phonenumbers.PhoneNumberUtil.CAPTURING_DIGIT_PATTERN.test(a) || 1 == this.accruedInput_.getLength() && i18n.phonenumbers.PhoneNumberUtil.PLUS_CHARS_PATTERN.test(a);
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.attemptToFormatAccruedDigits_ = function() {
  for (var a = this.nationalNumber_.toString(), b = this.possibleFormats_.length, c = 0; c < b; ++c) {
    var d = this.possibleFormats_[c], e = d.getPatternOrDefault();
    if ((new RegExp("^(?:" + e + ")$")).test(a)) {
      return this.shouldAddSpaceAfterNationalPrefix_ = i18n.phonenumbers.AsYouTypeFormatter.NATIONAL_PREFIX_SEPARATORS_PATTERN_.test(d.getNationalPrefixFormattingRule()), a = a.replace(new RegExp(e, "g"), d.getFormat()), this.appendNationalNumber_(a);
    }
  }
  return "";
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.appendNationalNumber_ = function(a) {
  var b = this.prefixBeforeNationalNumber_.getLength();
  return this.shouldAddSpaceAfterNationalPrefix_ && 0 < b && this.prefixBeforeNationalNumber_.toString().charAt(b - 1) != i18n.phonenumbers.AsYouTypeFormatter.SEPARATOR_BEFORE_NATIONAL_NUMBER_ ? this.prefixBeforeNationalNumber_ + i18n.phonenumbers.AsYouTypeFormatter.SEPARATOR_BEFORE_NATIONAL_NUMBER_ + a : this.prefixBeforeNationalNumber_ + a;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.getRememberedPosition = function() {
  if (!this.ableToFormat_) {
    return this.originalPosition_;
  }
  for (var a = 0, b = 0, c = this.accruedInputWithoutFormatting_.toString(), d = this.currentOutput_.toString(); a < this.positionToRemember_ && b < d.length;) {
    c.charAt(a) == d.charAt(b) && a++, b++;
  }
  return b;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.attemptToChooseFormattingPattern_ = function() {
  var a = this.nationalNumber_.toString();
  return a.length >= i18n.phonenumbers.AsYouTypeFormatter.MIN_LEADING_DIGITS_LENGTH_ ? (this.getAvailableFormats_(a), a = this.attemptToFormatAccruedDigits_(), 0 < a.length ? a : this.maybeCreateNewTemplate_() ? this.inputAccruedNationalNumber_() : this.accruedInput_.toString()) : this.appendNationalNumber_(a);
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.inputAccruedNationalNumber_ = function() {
  var a = this.nationalNumber_.toString(), b = a.length;
  if (0 < b) {
    for (var c = "", d = 0; d < b; d++) {
      c = this.inputDigitHelper_(a.charAt(d));
    }
    return this.ableToFormat_ ? this.appendNationalNumber_(c) : this.accruedInput_.toString();
  }
  return this.prefixBeforeNationalNumber_.toString();
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.isNanpaNumberWithNationalPrefix_ = function() {
  if (1 != this.currentMetadata_.getCountryCode()) {
    return !1;
  }
  var a = this.nationalNumber_.toString();
  return "1" == a.charAt(0) && "0" != a.charAt(1) && "1" != a.charAt(1);
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.removeNationalPrefixFromNationalNumber_ = function() {
  var a = this.nationalNumber_.toString(), b = 0;
  if (this.isNanpaNumberWithNationalPrefix_()) {
    b = 1, this.prefixBeforeNationalNumber_.append("1").append(i18n.phonenumbers.AsYouTypeFormatter.SEPARATOR_BEFORE_NATIONAL_NUMBER_), this.isCompleteNumber_ = !0;
  } else {
    if (this.currentMetadata_.hasNationalPrefixForParsing()) {
      var c = new RegExp("^(?:" + this.currentMetadata_.getNationalPrefixForParsing() + ")");
      c = a.match(c);
      null != c && null != c[0] && 0 < c[0].length && (this.isCompleteNumber_ = !0, b = c[0].length, this.prefixBeforeNationalNumber_.append(a.substring(0, b)));
    }
  }
  this.nationalNumber_.clear();
  this.nationalNumber_.append(a.substring(b));
  return a.substring(0, b);
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.attemptToExtractIdd_ = function() {
  var a = this.accruedInputWithoutFormatting_.toString(), b = new RegExp("^(?:\\" + i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN + "|" + this.currentMetadata_.getInternationalPrefix() + ")");
  b = a.match(b);
  return null != b && null != b[0] && 0 < b[0].length ? (this.isCompleteNumber_ = !0, b = b[0].length, this.nationalNumber_.clear(), this.nationalNumber_.append(a.substring(b)), this.prefixBeforeNationalNumber_.clear(), this.prefixBeforeNationalNumber_.append(a.substring(0, b)), a.charAt(0) != i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN && this.prefixBeforeNationalNumber_.append(i18n.phonenumbers.AsYouTypeFormatter.SEPARATOR_BEFORE_NATIONAL_NUMBER_), !0) : !1;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.attemptToExtractCountryCallingCode_ = function() {
  if (0 == this.nationalNumber_.getLength()) {
    return !1;
  }
  var a = new goog.string.StringBuffer, b = this.phoneUtil_.extractCountryCode(this.nationalNumber_, a);
  if (0 == b) {
    return !1;
  }
  this.nationalNumber_.clear();
  this.nationalNumber_.append(a.toString());
  a = this.phoneUtil_.getRegionCodeForCountryCode(b);
  i18n.phonenumbers.PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY == a ? this.currentMetadata_ = this.phoneUtil_.getMetadataForNonGeographicalRegion(b) : a != this.defaultCountry_ && (this.currentMetadata_ = this.getMetadataForRegion_(a));
  this.prefixBeforeNationalNumber_.append("" + b).append(i18n.phonenumbers.AsYouTypeFormatter.SEPARATOR_BEFORE_NATIONAL_NUMBER_);
  this.extractedNationalPrefix_ = "";
  return !0;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.normalizeAndAccrueDigitsAndPlusSign_ = function(a, b) {
  if (a == i18n.phonenumbers.PhoneNumberUtil.PLUS_SIGN) {
    var c = a;
    this.accruedInputWithoutFormatting_.append(a);
  } else {
    c = i18n.phonenumbers.PhoneNumberUtil.DIGIT_MAPPINGS[a], this.accruedInputWithoutFormatting_.append(c), this.nationalNumber_.append(c);
  }
  b && (this.positionToRemember_ = this.accruedInputWithoutFormatting_.getLength());
  return c;
};
i18n.phonenumbers.AsYouTypeFormatter.prototype.inputDigitHelper_ = function(a) {
  var b = this.formattingTemplate_.toString();
  if (0 <= b.substring(this.lastMatchPosition_).search(this.DIGIT_PATTERN_)) {
    var c = b.search(this.DIGIT_PATTERN_);
    a = b.replace(this.DIGIT_PATTERN_, a);
    this.formattingTemplate_.clear();
    this.formattingTemplate_.append(a);
    this.lastMatchPosition_ = c;
    return a.substring(0, this.lastMatchPosition_ + 1);
  }
  1 == this.possibleFormats_.length && (this.ableToFormat_ = !1);
  this.currentFormattingPattern_ = "";
  return this.accruedInput_.toString();
};
