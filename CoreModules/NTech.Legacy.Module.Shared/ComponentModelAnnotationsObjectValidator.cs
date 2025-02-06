using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Services.Infrastructure
{
    public class ComponentModelAnnotationsObjectValidator
    {
        private static void ValidateObject(object instance, Action<ValidationErrorItem> addValidationError, string prefixPath, int depth, int? listIndex)
        {
            Func<string, string> getFullName = name => prefixPath == "" ? name : $"{prefixPath}.{name}";

            if (depth > 20)
                throw new Exception($"Depth {depth} reached. Aborting with suspected cycle in the object graph.");

            var validationErrors = new List<ValidationResult>();
            var propertyNamesWithErrors = new HashSet<string>();
            if (!Validator.TryValidateObject(instance, new ValidationContext(instance), validationErrors, true))
            {
                var errors = validationErrors.SelectMany(x => ExpandMemberNames(x.MemberNames, "<Unknown>").Select(y => new ValidationErrorItem
                {
                    FirstMessage = x.ErrorMessage,
                    Name = y,
                    Path = getFullName(y),
                    ListFirstErrorIndex = listIndex,
                    ListErrorCount = listIndex.HasValue ? 1 : 0
                })).ToList();
                errors.ForEach(addValidationError);
                errors.ForEach(x => propertyNamesWithErrors.Add(x.Name));
            }

            var nestedProperties = instance
                .GetType()
                .GetProperties()
                .Where(x => x.CanRead && x.GetIndexParameters().Length == 0 && !(x.PropertyType == typeof(string)) && !x.PropertyType.IsValueType)
                .ToList();

            foreach (var r in nestedProperties)
            {
                var propertyInstance = r.GetValue(instance);

                if (propertyInstance == null) continue;

                if (IsEnumerable(propertyInstance))
                {
                    var i = 0;
                    foreach (object propertyInstanceListItem in (propertyInstance as System.Collections.IEnumerable))
                    {
                        var innerPrefixPath = getFullName(r.Name);
                        ValidateObject(propertyInstanceListItem, addValidationError, innerPrefixPath, depth + 1, i);
                        i++;
                    }
                }
                else
                    ValidateObject(r.GetValue(instance), addValidationError, getFullName(r.Name), depth + 1, null);
            }
        }

        //If the person who created the valdiation errors was lazy and didnt specify a member it will be hidden if we dont do this.
        private static IEnumerable<string> ExpandMemberNames(IEnumerable<string> memberNames, string defaultName) =>
            memberNames.Any() ? memberNames : new[] { defaultName };

        private static bool IsEnumerable(object myProperty)
        {
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(myProperty.GetType())
                || typeof(IEnumerable<>).IsAssignableFrom(myProperty.GetType()))
                return true;

            return false;
        }

        public static List<ValidationErrorItem> Validate<T>(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException("item");

            if (IsEnumerable(instance))
                throw new Exception("Initial container item cannot be enumerable");

            var d = new Dictionary<string, ValidationErrorItem>();

            Action<ValidationErrorItem> addItem = x =>
            {
                if (!d.ContainsKey(x.Path))
                    d[x.Path] = x;
                else
                {
                    var old = d[x.Path];
                    old.ListErrorCount += 1;
                }
            };
            ValidateObject(instance, addItem, "", 0, null);

            return d.Values.ToList();
        }

        public static void ValidateAndThrowOnError<T>(T instance, bool isUserFacingError = false, string errorCode = "validationError", string errorMessagePrefix = null)
        {
            var errors = Validate(instance);
            if (!errors.Any())
                return;

            throw new NTechWebserviceMethodException(ToErrorMessage(errors, errorMessagePrefix: errorMessagePrefix))
            {
                ErrorCode = errorCode,
                IsUserFacing = isUserFacingError,
                ErrorHttpStatusCode = isUserFacingError ? 400 : 500
            };
        }

        public static string ToErrorMessage(List<ValidationErrorItem> errors, string errorMessagePrefix = null)
        {
            var msgs = errors.Select(x =>
                   $"{x.Path}{(x.ListFirstErrorIndex.HasValue ? $"[{x.ListFirstErrorIndex.Value}]" : "")}");
            return $"{errorMessagePrefix ?? "Missing required properties: "}{string.Join(", ", msgs)}";
        }

        public class ValidationErrorItem
        {
            public string Path { get; set; }
            public string Name { get; set; }
            public string FirstMessage { get; set; }
            public int? ListFirstErrorIndex { get; set; }
            public int ListErrorCount { get; set; }
        }
    }
}