
module NTechBooleans {
    /**
     * Separates actually true or false from undefined, null, falsy, truthy
     **/
    export function isExactlyTrueOrFalse(b: boolean) {
        return b === true || b === false
    }    
}