function b64DecodeUnicode(str) {
    //Undestroy euro signs and similar: https://stackoverflow.com/questions/30106476/using-javascripts-atob-to-decode-base64-doesnt-properly-decode-utf-8-strings
    return decodeURIComponent(Array.prototype.map.call(atob(str), function (c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
}

function parseUtf8Base64InitialData(d) {
    return JSON.parse(b64DecodeUnicode(d))
}

var uiLanguageSwitcher = (function () {
    var cookieName = 'ntechcustomerpageslangv1'
    var nonCookieLanguage = 'fi'
    function getCookie(name) {
        var v = document.cookie.match('(^|;) ?' + name + '=([^;]*)(;|$)');
        return v ? v[2] : null;
    }
    function setCookie(name, value, days) {
        var d = new Date;
        d.setTime(d.getTime() + 24 * 60 * 60 * 1000 * days);
        document.cookie = name + "=" + value + ";path=/;expires=" + d.toGMTString();
    }

    function getCurrentLanguage() {
        return getCookie(cookieName) || nonCookieLanguage
    }

    function setCurrentLanguage(lang) {
        setCookie(cookieName, lang)
    }

    function init(detectedUiLanguage) {
        if (detectedUiLanguage) {
            nonCookieLanguage = detectedUiLanguage
        }
        var langs = document.querySelectorAll('a[data-changetolanguage]')
        var i;
        var currentLang = getCurrentLanguage()
        for (i = 0; i < langs.length; i++) {
            if (langs[i].getAttribute('data-changetolanguage') == currentLang) {
                langs[i].className += ' selected'
            }
            (function () { //Form a new closure or attrLang will be shredded byt the broken scoping rules ... this language is so primitive....
                var attrLang = langs[i].getAttribute('data-changetolanguage')
                langs[i].addEventListener('click', function (e) {
                    e.preventDefault()
                    setCurrentLanguage(attrLang)
                    location.reload()
                })
            }())
        }
    }
    return { init: init }
})()