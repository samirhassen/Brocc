export namespace NTechMath {
    //Be aware that this can have some strange results due to floating point math. Dont use this if exact result are super important. See https://stackoverflow.com/questions/11832914/round-to-at-most-2-decimal-places-only-if-necessary
    export function roundToPlaces(value: number, places: number) {
        var multiplier = Math.pow(10, places);

        return Math.round(value * multiplier) / multiplier;
    }

    export function sum<T>(items: T[], f: (item: T) => number): number {
        let sum = 0;
        for (let i of items) {
            sum += f(i);
        }
        return sum;
    }

    export function map<TSource, TResult>(items: TSource[], f: (item: TSource) => TResult): TResult[] {
        let r: TResult[] = [];
        for (let i of items) {
            r.push(f(i));
        }
        return r;
    }

    export function mapI<TSource, TResult>(items: TSource[], f: (item: TSource, index: number) => TResult): TResult[] {
        let r: TResult[] = [];
        let index = 0;
        for (let i of items) {
            r.push(f(i, index));
            index++;
        }
        return r;
    }

    export function orderBy<TSource>(items: TSource[], f: (x: TSource) => number, descending?: boolean) {
        let directionMultiplier = descending ? -1 : 1;
        let compareFn = (x1: TSource, x2: TSource) => {
            let n1: number = f(x1);
            let n2: number = f(x2);
            if (n1 > n2) {
                return directionMultiplier * 1;
            }

            if (n1 < n2) {
                return directionMultiplier * -1;
            }

            return 0;
        };
        let result: TSource[] = items.map((x) => x); //Make a copy since .sort is inplace
        result.sort(compareFn);
        return result;
    }

    export function equals(n1: number, n2: number) {
        if (n1 === null && n2 === null) {
            return true;
        }

        return Math.abs(n1 - n2) < Number.EPSILON;
    }

    export function getRandomIntInclusive(min: number, max: number) {
        min = Math.ceil(min);
        max = Math.floor(max);
        return Math.floor(Math.random() * (max - min + 1)) + min; //The maximum is inclusive and the minimum is inclusive
    }

    export function isAnyOf(code: string, values: any[]) {
        return values.indexOf(code) >= 0;
    }
}
