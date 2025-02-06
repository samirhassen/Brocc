using System;
using System.Collections.Generic;

namespace NTech.Banking.PluginApis.AlterApplication
{
    public class AdditionalQuestionsDocumentModel
    {
        public DateTimeOffset? AnswerDate { get; set; }
        public List<Item> Items { get; set; }

        public class Item
        {
            public int? ApplicantNr { get; set; }
            public int? CustomerId { get; set; }
            public bool IsCustomerQuestion { get; set; }
            public string QuestionGroup { get; set; }
            public string QuestionCode { get; set; }
            public string AnswerCode { get; set; }
            public string QuestionText { get; set; }
            public string AnswerText { get; set; }
        }
    }
}