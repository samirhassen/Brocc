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
        return i.filter(function (item, i, ar) { return ar.indexOf(item) === i; });
    }
    NTechLinq.distinct = distinct;
    function first(i, f) {
        var j = this.firstIndexOf(i, f);
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
        for (var j = 0; j < i.length; j++) {
            var v = i[j];
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
        for (var _i = 0, i_1 = i; _i < i_1.length; _i++) {
            var x = i_1[_i];
            if (f(x)) {
                return true;
            }
        }
        return false;
    }
    NTechLinq.any = any;
    function all(i, f) {
        return !any(i, function (x) { return !f(x); });
    }
    NTechLinq.all = all;
    function where(i, f) {
        var r = [];
        for (var _i = 0, i_2 = i; _i < i_2.length; _i++) {
            var x = i_2[_i];
            if (f(x)) {
                r.push(x);
            }
        }
        return r;
    }
    NTechLinq.where = where;
    function single(i, f) {
        var v = where(i, f);
        if (v.length !== 1) {
            throw new Error("Expected exactly one item but got: " + v.length);
        }
        return v[0];
    }
    NTechLinq.single = single;
})(NTechLinq || (NTechLinq = {}));
