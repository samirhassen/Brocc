using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NTech.Services.Infrastructure
{
    public class NTechNavigationTarget
    {
        private readonly TargetType t;
        private readonly string value;

        private NTechNavigationTarget(TargetType t, string value)
        {
            this.t = t;
            this.value = value;
        }

        private enum TargetType
        {
            Url,
            TargetCode,
            None
        }

        public static NTechNavigationTarget CreateFromTargetCode(string backTargetCode)
        {
            if (!string.IsNullOrWhiteSpace(backTargetCode))
                return new NTechNavigationTarget(TargetType.TargetCode, backTargetCode);
            else
                return new NTechNavigationTarget(TargetType.None, null);
        }

        public void Do(Action<string> handleBackUrl, Action<string> handleBackTargetCode, Action handleNone)
        {
            Using<object>(
                x => { handleBackUrl(x); return null; },
                x => { handleBackTargetCode(x); return null; },
                () => { handleNone(); return null; }
            );
        }

        public T Using<T>(Func<string, T> handleBackUrl, Func<string, T> handleBackTargetCode, Func<T> handleNone)
        {
            switch (t)
            {
                case TargetType.None: return handleNone();
                case TargetType.TargetCode: return handleBackTargetCode(value);
                case TargetType.Url: return handleBackUrl(value);
                default: throw new NotImplementedException();
            }
        }

        public string GetBackTargetOrNull()
        {
            return t == TargetType.TargetCode ? value : null;
        }

        public static NTechNavigationTarget CreateCrossModuleNavigationTarget(string targetName, Dictionary<string, string> targetContext)
        {
            return CreateFromTargetCode(CreateCrossModuleNavigationTargetCode(targetName, targetContext));
        }

        public static string CreateCrossModuleNavigationTargetCode(string targetName, Dictionary<string, string> targetContext)
        {
            if (targetName == null)
                return null;

            return "t-" + Urls.ToUrlSafeBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { targetName, targetContext })));
        }

        public static bool IsValidCrossModuleNavigationTargetCode(string targetCode)
        {
            return TryParseCrossModuleNavigationTargetCode(targetCode, out var _, out var __);
        }

        public static bool TryParseCrossModuleNavigationTargetCode(string targetCode, out string targetName, out Dictionary<string, string> targetContext)
        {
            targetName = null;
            targetContext = null;
            if (targetCode == null)
                return false;
            if (!targetCode.StartsWith("t-"))
                return false;

            try
            {
                var bytes = Urls.FromUrlSafeBase64String(targetCode.Substring(2));
                var d = JsonConvert.DeserializeAnonymousType(Encoding.UTF8.GetString(bytes), new { targetName = (string)null, targetContext = (Dictionary<string, string>)null });
                targetName = d?.targetName;
                targetContext = d?.targetContext;

                return targetName != null;
            }
            catch
            {
                return false;
            }
        }
    }
}