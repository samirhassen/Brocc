using System;

namespace nDocument.Pdf
{
    [Serializable]
    public class NTechHtmlToPdfTemplateException : Exception
    {
        public string RenderLogText { get; set; }

        public NTechHtmlToPdfTemplateException() { }

        public NTechHtmlToPdfTemplateException(string message) : base(message) { }

        public NTechHtmlToPdfTemplateException(string message, string renderLogText) : base(message)
        {
            this.RenderLogText = renderLogText;
        }

        public NTechHtmlToPdfTemplateException(string message, Exception inner) : base(message, inner) { }

        protected NTechHtmlToPdfTemplateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
