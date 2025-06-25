module NTechNavigationTarget {
    //Based on https://github.com/neosmart/UrlBase64/blob/master/UrlBase64/UrlBase64.cs

    export function toUrlSafeBase64String<T>(data: T): string
    {
        let encoded = btoa(JSON.stringify(data)).replace('+', '-').replace('/', '_')
        while(encoded[encoded.length - 1] === '=') {
            encoded = encoded.substr(0, encoded.length - 1)
        }        
        return encoded;
    }

    export function fromUrlSafeBase64String<T>(data: string): T {
        if(!data) {
            return null
        }
        let decodeFirstPass = () =>
        {
            let decoded = ''
            for(let c of data) {
                if(c === '_') {
                    decoded += '/'
                } else if(c === '-') {
                    decoded += '+'
                } else {
                    decoded += c
                }
            }
            switch(decoded.length % 4) {
                case 2: return decoded + '=='
                case 3: return decoded + '='
                default: return decoded
            }
        }

        let d = decodeFirstPass()
        return JSON.parse(atob(d))
    }

    export function createCrossModuleNavigationTargetCode(targetName: string, targetContext: { [key: string]: string} ) : string
    {
        if (targetName == null)
            return null

        return "t-" + toUrlSafeBase64String({ targetName, targetContext })
    }
}