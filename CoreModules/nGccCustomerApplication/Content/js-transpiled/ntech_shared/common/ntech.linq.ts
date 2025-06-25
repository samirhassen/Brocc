
module NTechLinq {
    export function select<TInput, TOutput>(i: TInput[], f: (x: TInput) => TOutput): TOutput[] {
        if(!i) {
            return null
        }
        return i.map(f)
    }

    export function distinct<TInput>(i: TInput[]): TInput[] {
        if (!i) {
            return null
        }
        return i.filter((item, i, ar) => ar.indexOf(item) === i)
    }

    export function first<TInput>(i: TInput[], f: (x: TInput) => boolean): TInput {
        let j = this.firstIndexOf(i, f)
        if (j < 0) {
            return null
        }
        return i[j]
    }

    export function firstIndexOf<TInput>(i: TInput[], f: (x: TInput) => boolean): number {
        if (!i) {
            return -1
        }
        for (let j = 0; j < i.length; j++) {
            let v = i[j]
            if (f(i[j])) {
                return j
            }
        }
        return -1
    }

    export function any<TInput>(i: TInput[], f: (x: TInput) => boolean): boolean {
        if (!i) {
            return false
        }

        for (let x of i) {
            if (f(x)) {
                return true
            }
        }
        return false
    }

    export function all<TInput>(i: TInput[], f: (x: TInput) => boolean): boolean {
        return !any(i, x => !f(x))
    }

    export function where<TInput>(i: TInput[], f: (x: TInput) => boolean): TInput[] {
        let r = []
        for (let x of i) {
            if (f(x)) {
                r.push(x)
            }
        }
        return r
    }


    export function single<TInput>(i: TInput[], f: (x: TInput) => boolean): TInput {
        let v = where(i, f)
        if (v.length !== 1) {
            throw new Error("Expected exactly one item but got: " + v.length)
        }
        return v[0]
    }
}