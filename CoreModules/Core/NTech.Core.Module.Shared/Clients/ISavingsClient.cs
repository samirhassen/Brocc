using System;
using System.Collections.Generic;

namespace NTech.Core.Module.Shared.Clients
{
    public interface ISavingsClient
    {
        CreateSavingsCommentResponse CreateComment(string savingsAccountNr, string commentText, string eventType,
           bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId);
    }

    public class CreateSavingsCommentResponse
    {
        public int Id { get; set; }
        public CommentModel comment { get; set; }

        public class CommentModel
        {
            public DateTimeOffset CommentDate { get; set; }
            public string CommentText { get; set; }
            public List<string> ArchiveLinks { get; set; }
            public string DisplayUserName { get; set; }
            public int? CustomerSecureMessageId { get; set; }
        }
    }
}
