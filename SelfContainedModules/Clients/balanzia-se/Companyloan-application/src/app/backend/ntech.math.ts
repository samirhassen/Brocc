export namespace NTechMath {
    //Be aware that this can have some strange results due to floating point math. Dont use this if exact result are super important. See https://stackoverflow.com/questions/11832914/round-to-at-most-2-decimal-places-only-if-necessary
    export function roundToPlaces(value: number, places: number) {
        var multiplier = Math.pow(10, places)

        return (Math.round(value * multiplier) / multiplier)
    }

    export function sum<T>(items: T[], f: (item: T) => number) : number {
        let sum = 0
        for(let i of items) {
            sum += f(i)
        }
        return sum
    }

    export function map<TSource, TResult>(items: TSource[], f: (item: TSource) => TResult) : TResult[] {
        let r : TResult[] = []
        for(let i of items) {
            r.push(f(i))
        }
        return r
    }

    export function mapI<TSource, TResult>(items: TSource[], f: (item: TSource, index: number) => TResult) : TResult[] {
        let r : TResult[] = []
        let index = 0
        for(let i of items) {
            r.push(f(i, index))
            index++
        }
        return r
    }

    export function equals(n1: number, n2: number) {
        if(n1 === null && n2 === null) {
            return true
        }

        return Math.abs(n1 - n2) < Number.EPSILON
    }
}
