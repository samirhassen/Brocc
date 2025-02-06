using ICSharpCode.SharpZipLib.Zip;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace nDocument.Pdf
{
    public class NTechHtmlToPdfTemplateCompiler
    {
        private readonly IStaticHtmlToPdfConverter staticHtmlToPdfConverter;
        private readonly IHtmlTemplateLogger htmlTemplateLogger;
        private readonly Func<Dictionary<string, object>> getCommonContext;

        public NTechHtmlToPdfTemplateCompiler(IStaticHtmlToPdfConverter staticHtmlToPdfConverter, IHtmlTemplateLogger htmlTemplateLogger, Func<Dictionary<string, object>> getCommonContext)
        {
            this.staticHtmlToPdfConverter = staticHtmlToPdfConverter;
            this.htmlTemplateLogger = htmlTemplateLogger;
            this.getCommonContext = getCommonContext;
        }

        private string CreateTempFolder()
        {
            var tmpPath = Path.Combine(Path.GetTempPath(), "nDocumentRender-" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tmpPath);
            return tmpPath;
        }

        public CompiledTemplate CompileFromZipfile(byte[] zipfile, string skinningPath)
        {
            var tmpPath = CreateTempFolder();

            var fs = new FastZip();
            using (var ms = new MemoryStream(zipfile))
            {
                fs.ExtractZip(ms, tmpPath, FastZip.Overwrite.Never, null, null, null, false, true);
            }

            return new CompiledTemplate(this, tmpPath, true, htmlTemplateLogger, null, skinningPath, getCommonContext);
        }

        public CompiledTemplate CompileFromZipfile(string templateZipFile, string skinningPath)
        {
            var tmpPath = CreateTempFolder();

            var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();
            fs.ExtractZip(templateZipFile, tmpPath, null);

            return new CompiledTemplate(this, tmpPath, true, htmlTemplateLogger, null, skinningPath, getCommonContext);
        }

        public CompiledTemplate CompileFromExistingFolder(string folder, string correlationId, string skinningPath)
        {
            return new CompiledTemplate(this, folder, false, htmlTemplateLogger, correlationId, skinningPath, getCommonContext);
        }

        public class CompiledTemplate : IDisposable
        {
            private readonly NTechHtmlToPdfTemplateCompiler compiler;
            private readonly string templatePath;
            private readonly bool wipeTemplatePath;
            private readonly IHtmlTemplateLogger htmlTemplateLogger;
            private readonly string templateLogCorrelationId;
            private readonly string skinningPath;
            private readonly Func<Dictionary<string, object>> getCommonContext;

            internal CompiledTemplate(NTechHtmlToPdfTemplateCompiler compiler, string templatePath, bool wipeTemplatePath, IHtmlTemplateLogger htmlTemplateLogger, string templateLogCorrelationId, string skinningPath, Func<Dictionary<string, object>> getCommonContext)
            {
                this.templateLogCorrelationId = templateLogCorrelationId ?? Guid.NewGuid().ToString();
                this.compiler = compiler;
                this.templatePath = templatePath;
                this.wipeTemplatePath = wipeTemplatePath;
                this.htmlTemplateLogger = htmlTemplateLogger;
                this.skinningPath = skinningPath;
                this.getCommonContext = getCommonContext;

                //Replace includes
                var templateFile = Path.Combine(this.templatePath, "template.html");
                var text = ReplaceIncudes(File.ReadAllText(templateFile), 0);
                text = ReplaceSkinningFiles(text, 0);

                File.WriteAllText(templateFile, text);

                htmlTemplateLogger?.OnCompiledTemplatePath(templateLogCorrelationId, this.templatePath);
            }

            private string ReplaceIncudes(string text, int nestingLevel)
            {
                if (nestingLevel > 20)
                    throw new Exception("Aborting due to nested template more than 10 deep. Suspected infinite loop");
                var re = new Regex(@"\[\[\[include\:([^\]]+)\]\]\]");
                text = re.Replace(text, m =>
                {
                    var replacementFile = Path.Combine(this.templatePath, m.Groups[1].Value);
                    if (!File.Exists(replacementFile))
                        throw new Exception("File missing: " + m.Groups[1].Value);
                    return ReplaceIncudes(File.ReadAllText(replacementFile), nestingLevel++);
                });
                return text;
            }

            /// <summary>
            /// Will replace something like:
            /// <img src="[[[skinning:img/pdf-logo.png|img/menu-header-logo.png|img/logo.png]]]">
            /// With:
            /// <img src="img/logo.png">
            /// Using the first in the list of skinning files that exist.
            /// So in the example above we will ust pdf-logo.png from skinning if it exists, otherwise menu-header-logo.png if that exist and lastly fall back on using the local file
            /// And also copy the file img/menu-header-logo.png from the skinning folder to img/logo.png in the template folder overwriting any current file with that name.
            /// </summary>
            private string ReplaceSkinningFiles(string text, int nestingLevel)
            {
                if (nestingLevel > 20)
                    throw new Exception("Aborting due to nested template more than 10 deep. Suspected infinite loop");
                var re = new Regex(@"\[\[\[skinning\:([^\]]+)\]\]\]");
                text = re.Replace(text, m =>
                {
                    var parts = m.Groups[1].Value.Split('|');
                    if (parts.Length < 2)
                        return "";

                    var skinningParts = parts.Take(parts.Length - 1).ToList();
                    var localPart = parts.Last();

                    var templateFile = Path.Combine(this.templatePath, localPart);
                    foreach (var skinningPart in skinningParts)
                    {
                        var skinningFile = Path.Combine(this.skinningPath, skinningPart);
                        if (File.Exists(skinningFile))
                        { 
                            Directory.CreateDirectory(Path.GetDirectoryName(templateFile));
                            File.Copy(skinningFile, templateFile, true);
                            return localPart;
                        }
                    }
                    return localPart;
                });
                return text;
            }

            public void RenderToFile(Dictionary<string, object> context, string targetFileName, Action<string> logSink = null)
            {
                var key = Guid.NewGuid().ToString();

                htmlTemplateLogger?.OnRenderToFileBegin(templateLogCorrelationId, key);

                var templateFile = Path.Combine(this.templatePath, "template.html");

                var renderedTemplateFile = Path.Combine(this.templatePath, string.Format("rendered-template-{0}.html", key));
                var logFile = Path.Combine(this.templatePath, string.Format("log-{0}.txt", key));
                try
                {
                    var commonContext = this.getCommonContext();
                    var extendedContext = ExtendContext(context, commonContext);

                    var w1 = Stopwatch.StartNew();
                    Nustache.Core.Render.FileToFile(templateFile, extendedContext, renderedTemplateFile);
                    htmlTemplateLogger?.OnRenderedHtmlFile(templateLogCorrelationId, key, renderedTemplateFile, extendedContext, w1.Elapsed);

                    w1.Restart();
                    var result = this.compiler.staticHtmlToPdfConverter.TryRenderToTempFile(renderedTemplateFile, targetFileName, logFile);
                    htmlTemplateLogger?.OnRenderedPdfFile(templateLogCorrelationId, key, targetFileName, logFile, w1.Elapsed);
                    w1.Stop();

                    if (!result)
                    {
                        string logText = null;
                        if (File.Exists(logFile))
                            logText = File.ReadAllText(logFile);
                        throw new NTechHtmlToPdfTemplateException("Failed to render pdf: " + logText, logText);
                    }
                    else if (logSink != null && File.Exists(logFile))
                    {
                        logSink(File.ReadAllText(logFile));
                    }
                    htmlTemplateLogger?.OnRenderToFileEnd(templateLogCorrelationId, key);
                }
                finally
                {
                    try { if (File.Exists(logFile)) File.Delete(logFile); } catch {  /*ignored*/ }
                    try { if (File.Exists(renderedTemplateFile)) File.Delete(renderedTemplateFile); } catch {  /*ignored*/ }
                }
            }

            #region IDisposable Support

            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // dispose managed state (managed objects).
                    }

                    // free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // set large fields to null.
                    try
                    {
                        if (wipeTemplatePath && Directory.Exists(templatePath))
                        {
                            Directory.Delete(templatePath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        NLog.Warning("({Operation}) Failed to delete: {templatePath}. Type: {exceptionType}", "CompiledTemplate.Dispose", templatePath, ex?.GetType()?.Name);
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            #endregion IDisposable Support
        }

        private static Dictionary<string, object> ExtendContext(Dictionary<string, object> d, Dictionary<string, object> commonContext)
        {
            var cd = new Dictionary<string, object>();
            d.ToList().ForEach(x => cd.Add(x.Key, x.Value));

            //Extend with common context (things like commonClientData that contains the clients name, adress and similar that is available to all templates)
            if (commonContext != null)
            {
                foreach (var key in commonContext.Keys)
                {
                    if (!cd.ContainsKey(key))
                        cd[key] = commonContext[key];
                }
            }

            /*All items that exist and are "non empty" (in the way an enduser would consider empty) will have a "has_<item>" defined to
            make things like this easier:
            {{#has_kittens}}
            <ul>
                {{#kittens}}
                    <li>
                        {{tailLength}} meters
                    </li>
                {{/kittens}}
            </ul>
            {{/has_kittens}}
            {{^has_kittens}}
            <span>No kittens sadly</span>
            {{/has_kittens}}

            Specifically getting rid of the ul for a list that is empty but still present required wierd hacks like kittens.0 without this extension
            */
            var newItems = new List<KeyValuePair<string, object>>();
            Action<string> add = n => newItems.Add(new KeyValuePair<string, object>("has_" + n, true));

            foreach (var item in cd)
            {
                if (item.Key.StartsWith("has_"))
                    continue;

                if (item.Value == null)
                    continue;

                //string
                var s = item.Value as string;
                if (s != null && !string.IsNullOrWhiteSpace(s))
                {
                    add(item.Key);
                    continue;
                }

                //list|array or similar
                var en = item.Value as System.Collections.IEnumerable;
                if (en != null && en.GetEnumerator().MoveNext())
                {
                    add(item.Key);
                    continue;
                }
            }

            newItems.ForEach(x =>
            {
                if (!cd.ContainsKey(x.Key))
                    cd.Add(x.Key, x.Value);
            });

            return cd;
        }
    }
}