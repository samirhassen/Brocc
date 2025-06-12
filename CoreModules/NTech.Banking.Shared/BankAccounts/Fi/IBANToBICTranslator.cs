using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace NTech.Banking.BankAccounts.Fi
{
    public class IBANToBICTranslator
    {
        private class Rule
        {
            public string Prefix { get; set; }
            public string BankName { get; set; }
            public string BIC { get; set; }
        }

        private List<Rule> rules;

        private static XDocument LoadEmbeddedRulesFile()
        {
            return XDocuments.Parse(DefaultIbanToBicTable);
        }

        public IBANToBICTranslator() : this(LoadEmbeddedRulesFile())
        {
        }

        public IBANToBICTranslator(XDocument rulesDocument)
        {
            rules = new List<Rule>();
            foreach (var bankElement in rulesDocument.Descendants().Where(x => x.Name.LocalName == "Bank"))
            {
                var bankName = bankElement.Descendants().Where(x => x.Name.LocalName == "Name").Single().Value;
                var bic = bankElement.Descendants().Where(x => x.Name.LocalName == "BIC").Single().Value;
                var codes = bankElement.Descendants().Where(x => x.Name.LocalName == "Codes").Single().Value;

                foreach (var code in codes.Split(',').Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var parts = code.Split('-').Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    if (parts.Count == 1)
                    {
                        var nr = int.Parse(parts[0]);
                        AddRule(nr, bankName, bic);
                    }
                    else if (parts.Count == 2)
                    {
                        var nrFrom = int.Parse(parts[0]);
                        var nrTo = int.Parse(parts[1]);
                        if (nrFrom > nrTo)
                            throw new Exception("Badly formatted bic translation file. Invalid entry: " + code);
                        for (var nr = nrFrom; nr <= nrTo; nr++)
                        {
                            AddRule(nr, bankName, bic);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        private void AddRule(int nr, string bankName, string bic)
        {
            var prefix = nr.ToString();

            var overLapItems = rules.Where(x => x.Prefix.StartsWith(prefix)).ToList();
            if (overLapItems.Any())
            {
                throw new Exception($"Invalid entry in bic file for bank: {bankName}. Rule for {bankName} overlaps with other rules: {string.Join(",", overLapItems.Select(x => x.BankName))}");
            }

            rules.Add(new Rule
            {
                Prefix = prefix,
                BankName = bankName,
                BIC = bic
            });
        }

        public string InferBic(IBANFi iban)
        {
            var bankNr = iban.NormalizedValue.Substring(4, 3);
            var hit = rules.SingleOrDefault(x => bankNr.StartsWith(x.Prefix));
            if (hit == null)
                throw new Exception($"Missing BIC for bank with nr {bankNr}");
            return hit.BIC;
        }

        public string InferBankName(IBANFi iban)
        {
            var bankNr = iban.NormalizedValue.Substring(4, 3);
            var hit = rules.SingleOrDefault(x => bankNr.StartsWith(x.Prefix));
            if (hit == null)
                throw new Exception($"Missing BankName for bank with nr {bankNr}");
            return hit.BankName;
        }

        public const string DefaultIbanToBicTable = @"<IbanToBic>
	<Bank>
		<Name>Aktia Pankki</Name>
		<BIC>HELSFIHH</BIC>
		<Codes>405, 497</Codes>
	</Bank>
	<Bank>
		<Name>POP Pankit (POP)</Name>
		<BIC>POPFFI22</BIC>
		<Codes>470-478</Codes>
	</Bank>
	<Bank>
		<Name>Bonum Pankki</Name>
		<BIC>POPFFI22</BIC>
		<Codes>479</Codes>
	</Bank>
	<Bank>
		<Name>Citibank</Name>
		<BIC>CITIFIHX</BIC>
		<Codes>713</Codes>
	</Bank>
	<Bank>
		<Name>Danske Bank</Name>
		<BIC>DABAFIHH</BIC>
		<Codes>8</Codes>
	</Bank>
	<Bank>
		<Name>Danske Bank</Name>
		<BIC>DABAFIHX</BIC>
		<Codes>34</Codes>
	</Bank>
	<Bank>
		<Name>DNB Bank ASA, Finland Branch</Name>
		<BIC>DNBAFIHX</BIC>
		<Codes>37</Codes>
	</Bank>
	<Bank>
		<Name>Handelsbanken</Name>
		<BIC>HANDFIHH</BIC>
		<Codes>31</Codes>
	</Bank>
	<Bank>
		<Name>Holvi</Name>
		<BIC>HOLVFIHH</BIC>
		<Codes>799</Codes>
	</Bank>
	<Bank>
		<Name>Nordea Pankki (Nordea)</Name>
		<BIC>NDEAFIHH</BIC>
		<Codes>1-2</Codes>
	</Bank>
	<Bank>
		<Name>Pohjola Pankki (OP Ryhmän pankkien keskusrahalaitos)</Name>
		<BIC>OKOYFIHH</BIC>
		<Codes>5</Codes>
	</Bank>
	<Bank>
		<Name>Skandinaviska Enskilda Banken (SEB)</Name>
		<BIC>ESSEFIHX</BIC>
		<Codes>33</Codes>
	</Bank>
	<Bank>
		<Name>S-Pankki</Name>
		<BIC>SBANFIHH</BIC>
		<Codes>39</Codes>
	</Bank>
	<Bank>
		<Name>S-Pankki</Name>
		<BIC>SBANFIHH</BIC>
		<Codes>36</Codes>
	</Bank>
	<Bank>
		<Name>Swedbank</Name>
		<BIC>SWEDFIHH</BIC>
		<Codes>38</Codes>
	</Bank>
	<Bank>
		<Name>Säästöpankkien Keskuspankki, Säästöpankit (Sp) ja Oma Säästöpankki</Name>
		<BIC>ITELFIHH</BIC>
		<Codes>715, 400, 402, 403, 406-408, 410-412,414-421,423-432,435-452,454-464,483-493,495-496</Codes>
	</Bank>
	<Bank>
		<Name>Ålandsbanken (ÅAB)</Name>
		<BIC>AABAFI22</BIC>
		<Codes>6</Codes>
	</Bank>
</IbanToBic>";
    }
}