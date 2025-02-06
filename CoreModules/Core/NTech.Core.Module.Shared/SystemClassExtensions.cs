using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace System
{
    public static class DictionaryExtensions
    {
        public static TValue Opt<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, bool removeAfter = false) where TValue : class
        {
            if (source == null || !source.ContainsKey(key))
                return null;

            if (!removeAfter)
            {
                return source[key];
            }
            else
            {
                var t = source[key];
                source.Remove(key);
                return t;
            }
        }

        public static TValue? OptS<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) where TValue : struct
        {
            if (source == null || !source.ContainsKey(key))
                return null;
            else
                return source[key];
        }

        public static TValue? OptSDefaultValue<TKey, TValue>(this IDictionary<TKey, TValue?> source, TKey key) where TValue : struct
        {
            if (source == null || !source.ContainsKey(key))
                return new TValue?();
            else
                return source[key];
        }

        public static TValue Req<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) where TValue : class
        {
            if (source == null)
                return null;
            else if (!source.ContainsKey(key))
                throw new Exception($"Missing key {key.ToString()}");
            else
                return source[key];
        }

        public static TParsedValue ReqParse<TKey, TParsedValue>(this IDictionary<TKey, string> source, TKey key, Func<string, TParsedValue> parse)
        {
            try
            {
                return parse(source.Req(key));
            }
            catch(Exception ex)
            {
                throw new Exception($"Failed to parse {key.ToString()}", ex);
            }
        }


        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, TValue defaultValue, Func<TValue, TValue> update) where TValue : struct
        {
            if (source == null) return default(TValue);

            if (!source.ContainsKey(key))
                source[key] = defaultValue;
            else
                source[key] = update(source[key]);

            return source[key];
        }
        
        public static void AddOrReplaceFrom<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> incoming)
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
            var result = System.Text.RegularExpressions.Regex.Match(dataUrl, @"data:(?<type>.+?);base64,(?<data>.+)");
            if (!result.Success)
            {
                mimeType = null;
                binaryData = null;
                return false;
            }
            else
            {
                mimeType = result.Groups["type"].Value.Trim();
                binaryData = Convert.FromBase64String(result.Groups["data"].Value.Trim());
                return true;
            }
        }

        public static void WithTempFile(Action<string> withFile, string prefix = null, string suffix = null)
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"{prefix ?? "ntech-tempfile-"}{Guid.NewGuid().ToString()}{suffix ?? ".tmp"}");
            try
            {
                withFile(tmp);
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(tmp);
                }
                catch { /*ignored*/ }
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

            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                ParameterExpression replacement;

                if (map.TryGetValue(p, out replacement))
                {
                    p = replacement;
                }

                return base.VisitParameter(p);
            }
        }


        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first)            
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.And);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.Or);
        }

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(
                               TSource _,
                               Expression<Func<TSource, TProperty>> propertyLambda)
        {
            Type type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a property that is not from type {1}.",
                    propertyLambda.ToString(),
                    type));

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
                if (d1.Value > d2.Value)
                    return d1;
                else
                    return d2;
            }
            else if (d1.HasValue)
                return d1;
            else
                return d2;
        }

        public static DateTime? ParseExact(string date, string format)
        {
            DateTime d;
            if (DateTime.TryParseExact(date, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out d))
                return d;
            else
                return null;
        }
    }
}

namespace System.IO
{
    public static class Streams
    {
        public static List<string> ReadAllLines(this Stream s, System.Text.Encoding encoding = null, bool closeStream = false)
        {
            var lines = new List<string>();
            using (var r = new System.IO.StreamReader(s, encoding ?? Text.Encoding.UTF8, true, 1024, !closeStream))
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
        if (source == null)
        {
            return false;
        }
        foreach (var a in args)
        {
            if (a == source)
                return true;
        }
        return false;
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

