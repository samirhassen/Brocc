using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System;

namespace nPreCredit.Code
{
    public class ClientSqlOnetimeScriptRunner : ClientSqlOnetimeScriptRunnerBase
    {
        private readonly IKeyValueStoreService keyValueStore;
        private readonly Func<PreCreditContextExtended> createContext;
        private const string KeySpace = "ClientSqlOnetimeScript";

        public ClientSqlOnetimeScriptRunner(IKeyValueStoreService keyValueStore, Func<PreCreditContextExtended> createContext)
        {
            this.keyValueStore = keyValueStore;
            this.createContext = createContext;
        }

        protected override string CurrentModuleName => NEnv.CurrentServiceName;

        protected override bool HasBeenRun(ScriptModel script)
        {
            return keyValueStore.GetValue(script.Id, KeySpace) == "true";
        }

        protected override void RunAndFlagAsRun(ScriptModel script)
        {
            using (var context = createContext())
            {
                var tr = context.Database.BeginTransaction();
                try
                {
                    foreach (var scriptStatement in SplitIntoStatements(script.Script))
                    {
                        context.Database.ExecuteSqlCommand(scriptStatement);
                    }
                    KeyValueStoreService.SetValueComposable(context, script.Id, KeySpace, "true");
                    context.SaveChanges();
                    tr.Commit();
                }
                catch
                {
                    tr.Rollback();
                    throw;
                }


            }
        }
    }
}