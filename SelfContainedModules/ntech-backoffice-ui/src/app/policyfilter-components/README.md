The reason for having this as it's own module rather than directly in unsecured-loan-application is to allow us to use policy filters for future products like standard mortage loans.

The ui should then be largely the same but the set of rules might be different and the datasources will for sure be different.

We want to be able to pull variables from an existing application for testing purposes and connect accept/reject rates and reports and such in the future so
we dont want a hard coupling between the exakt product data source and the ui.
