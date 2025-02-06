using System;

namespace nCredit.Code.Sat
{
    public class SatExportItem
    {
        public int CustomerId { get; set; }
        public string CivicRegNr { get; set; }
        //Total count of credits (count)
        /*
         This is not the number of the instalments of customer’s credits.
         The credit can be overdue or undue. Credit with limit, account credit is one credit. If the balance of a credit with limit, account credit is zero, it is not an open credit.
         */
        public int Count { get; set; }

        /*Total amount of loans (c01)
         Only the capital of the person’s open credits is included here.
         No interests or other subsidiary costs relating to credits are included here.
         All undue balances and unpaid due balances are counted here. In credits with limit and account credits the capital of the open balance is counted, nothing else, such as the amount of the customer’s maximum limit.
         */
        public int ItemC01 { get; set; }

        /*Over 60 days unpaid loans (c03)
        This is the entire remaining capital and interests of the credit (no other expenses), an instalment according to the agreement of which is 61 days or more late from the due date at the moment of inquiry. This is not the amount of a late instalment. 
        */
        public int ItemC03 { get; set; }

        /*Monthly payments (c04)
         Instalments and interests in total for the current month – whether they have been paid or not. If the person has several credits, the monthly payments are summed up. (euro) NOTE! This item does not include the instalments that are late and unpaid from the payments of the previous month.
        */
        public int ItemC04 { get; set; }

        /*Monthly payments, next month (h14)
        Monthly instalments and interests of credits in total for the next month (euro). Total instalments and interests due in the next month (euro)
        NOTE! This item does not include the instalments that are
        late and unpaid from the payments of previous months. The
        meaning of this sum is to depict the payment facility the
        person needs for monthly instalments.
        Specifications: Instalments and interests according to the
        agreement falling due during the next calendar month that
        are known at the moment of inquiry are counted here.
        • instalments under debt collection from the previous
        months are not counted
        • in addition to interests, no other subsidiary costs included
        in the invoice are counted
        This is 0 euros, if the person has raised a single payment
        credit and the only instalment is sometime in the future.
        This is 0 euros, if the person has paid the whole capital and
        interests and amortizes now other expenses related to the credit. 
        In a credit with limit this is the instalment sum of the month of the moment 
        of inquiry according to the agreement (capital instalment + interest, no other 
        subsidiary costs of the credit); if the customer is in the position to amortize 
        the limit credit even more, this is the minimum instalment sum according to the agreement.
        */
        public int ItemH14 { get; set; }

        /*Number of unsecured credits (d11)
        Total number of unsecured credits at the moment of inquiry (pcs)
        Of unsecured credits, other than continuous credits of account and limit format are counted here.
        With the data supplier d11+e11+f11 has to be =Total count of credits
        */
        public int ItemD11 { get; set; }

        /*Sum of unsecured credits(d12)
        Total capital of open unsecured credits at the moment of inquiry (euro)
        Only the capital of open unsecured credits of the person are counted here. Interests or other subsidiary costs relating to credits are not counted here. All undue balances and all unpaid due balances are counted here.
        With the data supplier d12+e12+f12 has to be =c01
        */
        public int ItemD12 { get; set; }

        /*Number of credits with secure(e11)
        Count of secured credits at the moment of inquiry (pcs)
        The number of secured credits at the moment of inquiry is counted here
        With the data supplier d11+e11+f11 hs to be =Total count of credits
        */
        public int ItemE11 { get; set; }

        /*Sum of credits with secure(e12)
        Capital of open secured credits at the moment of inquiry in total (euro)
        Only the capital of the person’s open secured credits is counted here. No interests or other subsidiary costs relating to credits are counted here. All undue balances and all unpaid due balances are counted here.
        With the data supplier d12+e12+f12 has to be =c01
        */
        public int ItemE12 { get; set; }

        /*Number of account or card credits(f11)
        Number of card credits or credits with limit at the moment of inquiry in total (pcs)
        The number of unsecured credits with limit and card credits at the moment of inquiry is counted here
        With the data supplier d11+e11+f11 has to be =Total count of credits
        */
        public int ItemF11 { get; set; }

        /*Sum of account or card credits(f12)
        Capital of card credits or credits with limit at the moment of inquiry in total (pcs)
        Only the capital of the person’s open unsecured card credits or credits with limit are counted here. No interests or other subsidiary costs relating to credits are counted here. All undue balances and all unpaid due balances are counted here.
        With the data supplier d12+e12+f12 has to be =c01
        */
        public int ItemF12 { get; set; }

        /*Maximum limit of account or card credits(f13)
        Maximum credit limit of card credits or credits with limit (euro)
        Maximum limits of card credits or credits with limit are summed up here, whether there is open credit or not.
        */
        public int ItemF13 { get; set; }

        /*Granted credit in 12 months(h15)
        Number of accepted credits during 12 months (pcs)
        The number of all credits granted to the person during the past 12 months is counted here, whether they have been paid off or not.
        */
        public int ItemH15 { get; set; }

        /*Credits with joint or several liable debtors(h16)
        Number of credits with jointly or severally liable debtors (pcs)
        The number of all credits, in which there is a jointly or severally liable debtor, is counted here.
        */
        public int ItemH16 { get; set; }

        /*The granting date of the newest open credit(k11)
        The granting date of the newest open credit (date)
        If the person has several open credits, the granting date of the newest one is given here. If the person has only one credit, its granting date is given here.
        */
        public DateTime? ItemK11 { get; set; }

        /*
        Same as k11 but for oldest
        */
        public DateTime? ItemK12 { get; set; }

        /*Sum of gross annual income, disclosed by the debtor(t11)
        Gross annual income disclosed by the debtor in the application (eur)
        The sum of gross annual income disclosed by the person. If there are several income data, the newest one is disclosed. If monthly gross income is asked in the application, it is disclosed multiplied by 12.5.
        Income data can be disclosed in the application or recorded from a document provided by the person
        */
        public decimal? ItemT11 { get; set; }

        /*The date of disclosing of gross annual income(t12)
        The date of disclosing the debtor’s gross annual income
        The date of the sum of gross annual income disclosed by the person (credit application/granting date)
        */
        public DateTime? ItemT12 { get; set; }
    }
}