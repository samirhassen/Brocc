using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit
{
    public class CreditApplicationComment : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public string ApplicationNr { get; set; }
        public string EventType { get; set; }
        public string Attachment { get; set; }
        public DateTimeOffset CommentDate { get; set; }
        public int CommentById { get; set; }
        public string CommentText { get; set; }

        public static string CleanCommentText(string input)
        {
            //The reason behind replacing ”:
            //1. When JsonConvert serializes something like { "text" : "abc "text" efg"} it will replace "text" with \"text\" to prevent JSON.parse from blowing up
            //2. This would not be a problem with ” except that window.atob seems to replace ” with " which will cause something like
            //   JSON.parse('{ "text" : "abc "text" efg"}') instead of one of the working versions
            //   JSON.parse('{ "text" : "abc \"text\" efg"}') or JSON.parse('{ "text" : "abc ”text” efg"}')
            //It's seems unlikely that the user actually cares about having ” vs " so just replacing it seems like the simplest solution.
            return input?.Replace("”", "\"");
        }
    }
}