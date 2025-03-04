var NTechDates;
(function (NTechDates) {
    //Support to avoid problems with timezones which tend dates to flip by one
    //Mirrors the class with the same name in c# in Ntech.Banking and is designed so serializiation works without timezone issues
    var DateOnly = /** @class */ (function () {
        function DateOnly(yearMonthDay) {
            this.yearMonthDay = yearMonthDay;
        }
        DateOnly.isValidDate = function (value) {
            if (isNullOrWhitespace(value))
                return true;
            return moment(value, "YYYY-MM-DD", true).isValid();
        };
        DateOnly.parseDateOrNull = function (n) {
            if (isNullOrWhitespace(n) || !DateOnly.isValidDate(n)) {
                return null;
            }
            var d = moment(n, "YYYY-MM-DD", true);
            return DateOnly.create(d.year(), d.month() + 1, d.date());
        };
        DateOnly.create = function (year, month, day) {
            return new DateOnly((year * 10000) + (month * 100) + day);
        };
        DateOnly.prototype.padTwo = function (input) {
            return (input < 10 ? '0' : '') + input.toString();
        };
        DateOnly.prototype.year = function () {
            var tmp = ((this.yearMonthDay - (this.yearMonthDay % 100)) / 100); // yyyymm
            return (tmp - (tmp % 100)) / 100;
        };
        DateOnly.prototype.month = function () {
            return ((this.yearMonthDay - (this.yearMonthDay % 100)) / 100) % 100;
        };
        DateOnly.prototype.day = function () {
            return this.yearMonthDay % 100;
        };
        DateOnly.prototype.toString = function () {
            return "".concat(this.year().toString(), "-").concat(this.padTwo(this.month()), "-").concat(this.padTwo(this.day()));
        };
        DateOnly.prototype.asDate = function () {
            return new Date(this.year(), this.month() - 1, this.day());
        };
        return DateOnly;
    }());
    NTechDates.DateOnly = DateOnly;
    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null)
            return true;
        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        }
        else {
            return false;
        }
    }
})(NTechDates || (NTechDates = {}));
