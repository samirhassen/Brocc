using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace System
{
    public static class DictionaryExtensions
    {
        public static TValue Opt<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key,
            bool removeAfter = false) where TValue : class
        {
            if (source == null || !source.TryGetValue(key, out var opt))
                return null;

            if (!removeAfter)
            {
                return opt;
            }

            var t = source[key];
            source.Remove(key);
            return t;
        }

        public static TValue? OptS<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) where TValue : struct
        {
            if (source == null || !source.TryGetValue(key, out var s))
                return null;
            return s;
        }

        public static TValue? OptSDefaultValue<TKey, TValue>(this IDictionary<TKey, TValue?> source, TKey key)
            where TValue : struct
        {
            if (source == null || !source.TryGetValue(key, out var value))
                return null;
            return value;
        }

        public static TValue Req<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) where TValue : class
        {
            if (source == null)
                return null;
            if (!source.TryGetValue(key, out var req))
                throw new Exception($"Missing key {key.ToString()}");
            return req;
        }

        public static TParsedValue ReqParse<TKey, TParsedValue>(this IDictionary<TKey, string> source, TKey key,
            Func<string, TParsedValue> parse)
        {
            try
            {
                return parse(source.Req(key));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse {key.ToString()}", ex);
            }
        }


        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key,
            TValue defaultValue, Func<TValue, TValue> update) where TValue : struct
        {
            if (source == null) return default(TValue);

            if (!source.ContainsKey(key))
                source[key] = defaultValue;
            else
                source[key] = update(source[key]);

            return source[key];
        }

        public static void AddOrReplaceFrom<TKey, TValue>(this IDictionary<TKey, TValue> source,
            IDictionary<TKey, TValue> incoming)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (incoming == null)
                return;

            foreach (var item in incoming)
                source[item.Key] = item.Value;
        }
    }

    public static class ArrayExtensions
    {
        public static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(this T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }
    }

    public static class ListExtensions
    {
        public static List<T> AddRangeCreating<T>(this List<T> source, IEnumerable<T> items)
        {
            if (source == null)
                source = new List<T>();

            source.AddRange(items);

            return source;
        }
    }

    public static class ExpandoObjectExtensions
    {
        public static ExpandoObject SetValues(this ExpandoObject source, Action<IDictionary<string, object>> set)
        {
            var d = source as IDictionary<string, object>;
            set(d);
            return source;
        }
    }

    public static class FileUtilities
    {
        public static bool TryParseDataUrl(string dataUrl, out string mimeType, out byte[] binaryData)
        {
            var result = Regex.Match(dataUrl, @"data:(?<type>.+?);base64,(?<data>.+)");
            if (!result.Success)
            {
                mimeType = null;
                binaryData = null;
                return false;
            }

            mimeType = result.Groups["type"].Value.Trim();
            binaryData = Convert.FromBase64String(result.Groups["data"].Value.Trim());
            return true;
        }

        public static void WithTempFile(Action<string> withFile, string prefix = null, string suffix = null)
        {
            var tmp = Path.Combine(Path.GetTempPath(),
                $"{prefix ?? "ntech-tempfile-"}{Guid.NewGuid().ToString()}{suffix ?? ".tmp"}");
            try
            {
                withFile(tmp);
            }
            finally
            {
                try
                {
                    File.Delete(tmp);
                }
                catch
                {
                    /*ignored*/
                }
            }
        }
    }

    public static class ExpressionExtensions
    {
        private class ParameterRebinder : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, ParameterExpression> map;

            public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
            }

            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map,
                Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                if (map.TryGetValue(p, out var replacement))
                {
                    p = replacement;
                }

                return base.VisitParameter(p);
            }
        }


        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second,
            Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first)            
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] })
                .ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.And);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.Or);
        }

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(
            TSource _,
            Expression<Func<TSource, TProperty>> propertyLambda)
        {
            var type = typeof(TSource);

            if (!(propertyLambda.Body is MemberExpression member))
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a field, not a property.");

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a property that is not from type {type}.");

            return propInfo;
        }
    }

    public static class ExceptionExtensions
    {
        public static string FormatException(this Exception ex)
        {
            var b = new StringBuilder();
            var guard = 0;
            while (ex != null && guard++ < 10)
            {
                b.AppendLine(ex.GetType().Name);
                b.AppendLine(ex.Message);
                b.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
            }

            return b.ToString();
        }
    }

    public static class DateTimeUtilities
    {
        public static DateTime? Max(DateTime? d1, DateTime? d2)
        {
            if (d1.HasValue && d2.HasValue)
            {
                return d1.Value > d2.Value ? d1 : d2;
            }

            return d1 ?? d2;
        }

        public static DateTime? ParseExact(string date, string format)
        {
            if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d))
                return d;
            return null;
        }
    }
}

namespace System.IO
{
    public static class Streams
    {
        public static List<string> ReadAllLines(this Stream s, Encoding encoding = null,
            bool closeStream = false)
        {
            var lines = new List<string>();
            using (var r = new StreamReader(s, encoding ?? Encoding.UTF8, true, 1024, !closeStream))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                    lines.Add(line);
            }

            return lines;
        }
    }
}

public static class StringExtensions
{
    public static bool IsOneOf(this string source, params string[] args)
    {
        return source != null && args.Any(a => a == source);
    }

    public static bool IsOneOfIgnoreCase(this string source, params string[] args)
    {
        if (source == null)
        {
            return false;
        }

        foreach (var a in args)
        {
            if (a.EqualsIgnoreCase(source))
                return true;
        }

        return false;
    }

    public static bool EqualsIgnoreCase(this string source, string otherString)
    {
        if (source == null)
            return otherString == null;
        else
            return source.Equals(otherString, StringComparison.OrdinalIgnoreCase);
    }

    public static List<string> ReadAllLines(this string source)
    {
        if (source == null)
            return new List<string>();

        var result = new List<string>();
        string line;
        var reader = new StringReader(source);
        while ((line = reader.ReadLine()) != null)
            result.Add(line);

        return result;
    }

    ///<summary>
    /// If the string is null or whitespace the return value is null otherwise it's the string trimmed.
    ///</summary>
    public static string NormalizeNullOrWhitespace(this string source) =>
        string.IsNullOrWhiteSpace(source) ? null : source.Trim();

    public static string ClipRight(this string source, int maxLength) =>
        source != null && source.Length > maxLength ? source.Substring(0, maxLength) : source;
}