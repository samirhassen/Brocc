using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NTech.Services.Infrastructure
{
    public abstract class ClientSqlOnetimeScriptRunnerBase
    {
        public class ScriptModel
        {
            public string Id { get; set; }
            public string TargetModuleName { get; set; }
            public string Script { get; set; }
        }

        protected abstract string CurrentModuleName { get; }
        protected abstract bool HasBeenRun(ScriptModel script);
        protected abstract void RunAndFlagAsRun(ScriptModel script);

        public void RunScriptsForClient()
        {
            foreach (var script in GetScriptsForClient())
            {
                if (!script.TargetModuleName.Equals(CurrentModuleName, StringComparison.OrdinalIgnoreCase) || HasBeenRun(script))
                    continue;

                Log.Information($"Executing sql onetime script '{script.Id}'");
                RunAndFlagAsRun(script);
            }
        }
        /// <summary>
        /// If the scripts are executed using just plain SqlConnection which seems highly likely
        /// multiple statements are not supported so this allows them to be used anyway by splitting the script up
        /// into multiple separate calls in the same transaction. If the execution is implemented using say SMO
        /// this is not needed.
        /// </summary>
        protected List<string> SplitIntoStatements(string rawScript)
        {
            List<string> statements = new List<string>();
            using (var r = new StringReader(rawScript))
            {
                string line;
                string currentStatement = "";
                while ((line = r.ReadLine()) != null)
                {
                    if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentStatement.Length > 0)
                            statements.Add(currentStatement.TrimEnd());
                        currentStatement = "";
                    }
                    else
                    {
                        currentStatement += line + Environment.NewLine;
                    }
                }
                if (currentStatement.Length > 0)
                    statements.Add(currentStatement.TrimEnd());

                return statements;
            }
        }

        private List<ScriptModel> GetScriptsForClient()
        {
            var scripts = new List<ScriptModel>();
            var di = NTechEnvironment.Instance.ClientResourceDirectory("ntech.clientonetimesqlscripts.folder",
                "OnetimeSqlScripts", false);
            if (!di.Exists)
                return scripts;

            string Req(FileInfo f, XDocument d, string name)
            {
                var v = d?.Descendants()?.FirstOrDefault(x => x.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;
                if (string.IsNullOrWhiteSpace(v))
                    throw new Exception($"'{name}' is missing from onetime sql script '{f.FullName}'");
                return v?.Trim();
            }

            foreach (var file in di.GetFiles("*.xml"))
            {
                var d = XDocument.Load(file.FullName);
                scripts.Add(new ScriptModel
                {
                    Id = Req(file, d, "Id"),
                    TargetModuleName = Req(file, d, "TargetModuleName"),
                    Script = Req(file, d, "Script")
                });
            }

            return scripts;
        }
    }
}