var fraudCheckSharedData = (function () {
    var translations = [
        {
            'key': 'OtherApprovedLoanRecentlyCheck',
            'value': 'New loan within 24 months?'
        },
        {
            'key': 'SameAddressCheck',
            'value': 'Applicant has same address'
        },
        {
            'key': 'EmploymentCheck',
            'value': 'Is person self-employed?'
        },
        {
            'key': 'PhoneCheck',
            'value': 'Phone control'
        },
        {
            'key': 'SameEmailCheck',
            'value': 'Applicant has same email address'
        },
        {
            'key': 'SameAccountNrCheck',
            'value': 'Applicant has same account number'
        },
        {
            key: 'lastName',
            value: 'Last Name'
        },
        {
            key: 'addressStreet',
            value: 'Address - Street'
        },
        {
            key: 'addressZipcode',
            value: 'Address - Zip'
        },
        {
            key: 'addressCity',
            value: 'Address - City'
        },
        {
            key: 'addressCountry',
            value: 'Address - Country'
        },
        {
            key: 'civicRegNr',
            value: 'Civic regnr'
        }
    ];
    function translateApp(app) {
        app.fraudControlViewItems.forEach(function (item, index) {
            item.Locked = true;
            var translation = translations.filter(function (translationItem) { return translationItem.key === item.Key; })[0];
            if (translation) {
                item.FriendlyKey = translation.value;
            }
            else {
                item.FriendlyKey = item.Key;
            }
        });
        if (app.customerModel && app.customerModel.sensitive) {
            app.customerModel.sensitive.forEach(function (item, index) {
                item.Locked = true;
                var translation = translations.filter(function (translationItem) { return translationItem.key === item.Name; })[0];
                if (translation) {
                    item.FriendlyName = translation.value;
                }
                else {
                    item.FriendlyName = item.Name;
                }
            });
        }
    }
    return { translateApp: translateApp };
})();
