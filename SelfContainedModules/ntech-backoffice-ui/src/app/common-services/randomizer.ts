import { NTechMath } from './ntech.math';

export namespace Randomizer {
    export function anyOf(values: any[]) {
        return values[NTechMath.getRandomIntInclusive(0, values.length - 1)];
    }

    export function anyNumberBetween(min: number, max: number) {
        return NTechMath.getRandomIntInclusive(min, max);
    }

    /**
     *
     * @param min Min value
     * @param max Max value
     * @param dimension Dimension of the result, ex. 1000 gives even thousand, 100 even hundred etc.
     * @returns
     */
    export function anyEvenNumberBetween(min: number, max: number, dimension: number) {
        let value = NTechMath.getRandomIntInclusive(min, max);
        return Math.round(value / dimension) * dimension;
    }
}
