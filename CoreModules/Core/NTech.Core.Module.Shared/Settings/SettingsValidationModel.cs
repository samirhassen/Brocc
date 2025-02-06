using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;

namespace NTech.Core.Module.Shared.Settings
{
    internal class SettingsValidationModel
    {
        private readonly Dictionary<string, Action<ValidationContext>> validatorsBySettingName = 
            new Dictionary<string, Action<ValidationContext>>();

        public void SetValidationRules(string settingName, Action<ValidationContext> validate) =>
            validatorsBySettingName[settingName] = validate;

        public List<string> Validate(string settingName, Dictionary<string, string> values)
        {
            var context = new ValidationContext(values);
            try
            {
                var validate = validatorsBySettingName?.OptVal(settingName);
                validate?.Invoke(context);
            }
            catch(NTechCoreWebserviceException ex)
            {
                if (ex.ErrorCode == ValidationErrorCode)
                    context.AddErrorIf(true, ex.Message);
                else
                    throw;
            }
            return context.ValidationErrors;
        }

        private const string ValidationErrorCode = "settingsValidationError";

        public class ValidationContext
        {
            private readonly Dictionary<string, string> values;
            public List<string> ValidationErrors = new List<string>();

            public ValidationContext(Dictionary<string, string> values)
            {
                this.values = values;
            }

            private NTechCoreWebserviceException Err(string text) => new NTechCoreWebserviceException(text) { ErrorCode = ValidationErrorCode };

            public string GetString(string name)
            {
                var v = values?.Opt(name);
                if (v == null) throw Err($"Missing {v}");
                return v;
            }

            public int GetInt(string name)
            {
                var v = GetString(name);
                if(!int.TryParse(v, out var intValue))
                    throw Err($"Invalid int {name}");
                return intValue;
            }
            public bool GetBool(string name) => GetString(name) == "true";


            public void AddErrorIf(bool isError, string errorMessage)
            {
                if (isError) ValidationErrors.Add(errorMessage);
            }
        }
    }
}
