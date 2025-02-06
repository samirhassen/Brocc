export function createMortgagePropertyIdFromCollateralItems(
    getCollateralItem: (name: string) => string,
    includeObjectTypeLabel: boolean
) {
    let v = (n: string) => getCollateralItem(n) ?? `Missing ${n}`;
    let createRawId = () => {
        if (v('objectTypeCode') === 'seBrf') {
            return `${v('objectAddressMunicipality')} ${v('seBrfName')} ${v('seBrfApartmentNr')}`;
        } else if (v('objectTypeCode') === 'seFastighet') {
            return v('objectId');
        } else {
            return v('objectTypeCode');
        }
    };
    let propertyId = createRawId();
    if (includeObjectTypeLabel) {
        return getCollateralItem('objectTypeCode') === 'seBrf' ? `BRF: ${propertyId}` : propertyId;
    } else {
        return propertyId;
    }
}
