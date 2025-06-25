var NTechNavigationTarget;
(function (NTechNavigationTarget) {
    //Based on https://github.com/neosmart/UrlBase64/blob/master/UrlBase64/UrlBase64.cs
    function toUrlSafeBase64String(data) {
        let encoded = btoa(JSON.stringify(data)).replace('+', '-').replace('/', '_');
        while (encoded[encoded.length - 1] === '=') {
            encoded = encoded.substr(0, encoded.length - 1);
        }
        return encoded;
    }
    NTechNavigationTarget.toUrlSafeBase64String = toUrlSafeBase64String;
    function fromUrlSafeBase64String(data) {
        if (!data) {
            return null;
        }
        let decodeFirstPass = () => {
            let decoded = '';
            for (let c of data) {
                if (c === '_') {
                    decoded += '/';
                }
                else if (c === '-') {
                    decoded += '+';
                }
                else {
                    decoded += c;
                }
            }
            switch (decoded.length % 4) {
                case 2: return decoded + '==';
                case 3: return decoded + '=';
                default: return decoded;
            }
        };
        let d = decodeFirstPass();
        return JSON.parse(atob(d));
    }
    NTechNavigationTarget.fromUrlSafeBase64String = fromUrlSafeBase64String;
    function createCrossModuleNavigationTargetCode(targetName, targetContext) {
        if (targetName == null)
            return null;
        return "t-" + toUrlSafeBase64String({ targetName, targetContext });
    }
    NTechNavigationTarget.createCrossModuleNavigationTargetCode = createCrossModuleNavigationTargetCode;
})(NTechNavigationTarget || (NTechNavigationTarget = {}));
