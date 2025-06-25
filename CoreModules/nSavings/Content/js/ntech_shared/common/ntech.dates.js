var NTechDates;
(function (NTechDates) {
    //Support to avoid problems with timezones which tend dates to flip by one
    //Mirrors the class with the same name in c# in Ntech.Banking and is designed so serializiation works without timezone issues
    class DateOnly {
        constructor(yearMonthDay) {
            this.yearMonthDay = yearMonthDay;
        }
        static isValidDate(value) {
            if (isNullOrWhitespace(value))
                return true;
            return moment(value, "YYYY-MM-DD", true).isValid();
        }
        static parseDateOrNull(n) {
            if (isNullOrWhitespace(n) || !DateOnly.isValidDate(n)) {
                return null;
            }
            let d = moment(n, "YYYY-MM-DD", true);
            return DateOnly.create(d.year(), d.month() + 1, d.date());
        }
        static create(year, month, day) {
            return new DateOnly((year * 10000) + (month * 100) + day);
        }
        padTwo(input) {
            return (input < 10 ? '0' : '') + input.toString();
        }
        year() {
            var tmp = ((this.yearMonthDay - (this.yearMonthDay % 100)) / 100); // yyyymm
            return (tmp - (tmp % 100)) / 100;
        }
        month() {
            return ((this.yearMonthDay - (this.yearMonthDay % 100)) / 100) % 100;
        }
        day() {
            return this.yearMonthDay % 100;
        }
        toString() {
            return `${this.year().toString()}-${this.padTwo(this.month())}-${this.padTwo(this.day())}`;
        }
        asDate() {
            return new Date(this.year(), this.month() - 1, this.day());
        }
    }
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
