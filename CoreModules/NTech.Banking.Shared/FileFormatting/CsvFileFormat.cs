using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NTech.Banking.FileFormatting
{
    public class CsvFileFormat
    {
        public abstract class CsvElement
        {

            /// <summary>
            /// Returns a list of all property names that are defined on the derived object. 
            /// </summary>
            /// <returns></returns>
            public IEnumerable<string> GetProperties()
            {
                var type = this.GetType();
                var properties = type.GetProperties();

                // Print all properties not decorated with IgnorePrint. 
                return properties.Where(p => !Attribute.IsDefined(p, typeof(IgnorePrintAttribute))).Select(prop => prop.Name).ToList();
            }

            /// <summary>
            /// Return all properties as a comma-separated string for printing. 
            /// </summary>
            /// <returns></returns>
            public string GetOrderedPropertyTitles(string valueDelimiter)
            {
                return string.Join(valueDelimiter, this.GetProperties());
            }

            /// <summary>
            /// Return all values as a comma-separated string, in the same order as the properties in the title-row. 
            /// </summary>
            /// <returns></returns>
            public string GetOrderedValues(string valueDelimiter)
            {
                var properties = this.GetProperties();
                var values = properties.Select(GetValue).ToList();

                string GetValue(string prop)
                {
                    var current = this.GetType().GetProperty(prop)?.GetValue(this, null);

                    // Add more custom type formatting other than default here when needed. 
                    switch (current)
                    {
                        case null:
                            return string.Empty;
                        case decimal currentDecimal:
                            return currentDecimal.ToString(CultureInfo.InvariantCulture);
                        case DateTime currentDateTime:
                            return currentDateTime.ToString("yyyy-MM-dd");
                        default:
                            return current.ToString();
                    }

                }

                return string.Join(valueDelimiter, values);
            }

        }


        /// <summary>
        /// Decorate with this attribute when the fiels should not be printed in export-files.
        /// Ex. when holding temporary values from database. 
        /// </summary>
        public class IgnorePrintAttribute : Attribute
        { }

        public class CsvWriter
        {

            /// <summary>
            /// Delimiter when printing titles/values in the csv. Ex. "RunDate;LoanId". 
            /// </summary>
            private readonly string _valueDelimiter;

            private readonly string _fileName;
            private readonly string _tempFileName = Path.Combine(Path.GetTempPath(), $"tmp_csvwriter_{Guid.NewGuid()}.csv");

            public CsvWriter(string fileName, string valueDelimiter = ";")
            {
                _valueDelimiter = valueDelimiter;
                _fileName = fileName;
            }

            /// <summary>
            /// Returns the name of a tempfile created on the server. 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="elements"></param>
            /// <returns></returns>
            public string GenerateFile<T>(IEnumerable<T> elements) where T : CsvElement
            {
                // Created a StreamWriter in the background with Utf8NoBom specified. 
                using (var fs = System.IO.File.CreateText(_tempFileName))
                {
                    // Print titles on the first row of the file. 
                    var titles = elements.First().GetOrderedPropertyTitles(_valueDelimiter);
                    fs.WriteLine(titles);

                    // Print values for every sequential row in the file. 
                    foreach (var element in elements)
                    {
                        fs.WriteLine(element.GetOrderedValues(_valueDelimiter));
                    }
                }

                return _tempFileName;
            }

        }
    }
}
