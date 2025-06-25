var NTechLinq;
(function (NTechLinq) {
    function select(i, f) {
        if (!i) {
            return null;
        }
        return i.map(f);
    }
    NTechLinq.select = select;
    function distinct(i) {
        if (!i) {
            return null;
        }
        return i.filter((item, i, ar) => ar.indexOf(item) === i);
    }
    NTechLinq.distinct = distinct;
    function first(i, f) {
        let j = this.firstIndexOf(i, f);
        if (j < 0) {
            return null;
        }
        return i[j];
    }
    NTechLinq.first = first;
    function firstIndexOf(i, f) {
        if (!i) {
            return -1;
        }
        for (let j = 0; j < i.length; j++) {
            let v = i[j];
            if (f(i[j])) {
                return j;
            }
        }
        return -1;
    }
    NTechLinq.firstIndexOf = firstIndexOf;
    function any(i, f) {
        if (!i) {
            return false;
        }
        for (let x of i) {
            if (f(x)) {
                return true;
            }
        }
        return false;
    }
    NTechLinq.any = any;
    function all(i, f) {
        return !any(i, x => !f(x));
    }
    NTechLinq.all = all;
    function where(i, f) {
        let r = [];
        for (let x of i) {
            if (f(x)) {
                r.push(x);
            }
        }
        return r;
    }
    NTechLinq.where = where;
    function single(i, f) {
        let v = where(i, f);
        if (v.length !== 1) {
            throw new Error("Expected exactly one item but got: " + v.length);
        }
        return v[0];
    }
    NTechLinq.single = single;
})(NTechLinq || (NTechLinq = {}));
