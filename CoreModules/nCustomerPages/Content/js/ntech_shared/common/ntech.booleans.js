var NTechBooleans;
(function (NTechBooleans) {
    /**
     * Separates actually true or false from undefined, null, falsy, truthy
     **/
    function isExactlyTrueOrFalse(b) {
        return b === true || b === false;
    }
    NTechBooleans.isExactlyTrueOrFalse = isExactlyTrueOrFalse;
})(NTechBooleans || (NTechBooleans = {}));
//# sourceMappingURL=ntech.booleans.js.map