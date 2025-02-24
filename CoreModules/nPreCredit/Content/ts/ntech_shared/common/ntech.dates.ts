
module NTechDates {
    //Support to avoid problems with timezones which tend dates to flip by one
    //Mirrors the class with the same name in c# in Ntech.Banking and is designed so serializiation works without timezone issues
    export class DateOnly {
        constructor(public yearMonthDay: number) {

        }        

        public static isValidDate(value: string) {
            if (isNullOrWhitespace(value))
                return true
            return moment(value, "YYYY-MM-DD", true).isValid()
        }

        public static parseDateOrNull(n: string): DateOnly {
            if (isNullOrWhitespace(n) || !DateOnly.isValidDate(n)) {
                return null
            }
            let d = moment(n, "YYYY-MM-DD", true)
            return DateOnly.create(d.year(), d.month()+1, d.date())
        }

        public static create(year: number, month: number, day: number) {
            return new DateOnly((year * 10000) + (month * 100) + day)
        }

        private padTwo(input: number) {
            return (input < 10 ? '0' : '') + input.toString()
        }

        year() : number {
            var tmp = ((this.yearMonthDay - (this.yearMonthDay % 100)) / 100); // yyyymm
            return (tmp - (tmp % 100)) / 100;
        }

        month() : number {
            return ((this.yearMonthDay - (this.yearMonthDay % 100)) / 100) % 100
        }  

        day() : number {
            return this.yearMonthDay % 100
        }

        toString() {
            return `${this.year().toString()}-${this.padTwo(this.month())}-${this.padTwo(this.day())}`
        }

        asDate() : Date {
            return new Date(this.year(), this.month() - 1, this.day())
        }
    }

    function isNullOrWhitespace(input: any) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }
}