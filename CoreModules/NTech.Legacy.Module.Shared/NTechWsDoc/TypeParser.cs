using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.NTechWsDoc
{
    /// <summary>
    /// Parse a request or response type into a structure suitable for generating documentation
    /// </summary>
    public class TypeParser
    {
        private int subTypeCounter = 0;

        private string CreateNewSubtypeName()
        {
            return $"Type{++subTypeCounter}";
        }

        private Dictionary<string, string> compoundTypeNameByDotNetTypeName = new Dictionary<string, string>();
        private Dictionary<string, CompoundType> compoundTypeByName = new Dictionary<string, CompoundType>();

        public Dictionary<string, CompoundType> CompoundTypeByName
        {
            get
            {
                return this.compoundTypeByName;
            }
        }

        public CompoundType ParseType(Type t, string preferredTypeName)
        {
            var ut = Unpack(t);
            if (ut.IsPrimitive || ut.IsArray)
                throw new Exception("the basetype cannot be primitive or an array");
            if (!compoundTypeNameByDotNetTypeName.ContainsKey(t.FullName))
                compoundTypeNameByDotNetTypeName[t.FullName] = preferredTypeName;

            int depthGuard = 0;
            return ReflectTypeI(ut, depthGuard);
        }

        private class UnpackedType
        {
            public Type ActualType { get; set; }
            public bool IsPrimitive { get; set; }
            public bool IsArray { get; set; }
            public bool IsNullable { get; set; }
        }

        private UnpackedType Unpack(Type t)
        {
            if (t.FullName == "Newtonsoft.Json.Linq.JObject")
            {
                //TODO: Add a decorator to properties that want to present an alernative type for documentation instead of hardcoding it here
                //Also make it so we can directly set  what to show so for instance we want CustomData: <any> or something like that
                return Unpack(typeof(Dictionary<string, object>));
            }
            var isArray = false;
            var isNullable = false;
            var actualType = t;

            var arrayType = GetArrayUnderlyingTypeOrNull(actualType);
            if (arrayType != null)
            {
                actualType = arrayType;
                isArray = true;
            }

            var nullableType = Nullable.GetUnderlyingType(actualType);
            if (nullableType != null)
            {
                actualType = nullableType;
                isNullable = true;
            }

            return new UnpackedType
            {
                ActualType = actualType,
                IsArray = isArray,
                IsNullable = isNullable,
                IsPrimitive = IsPrimitive(actualType)
            };
        }

        private static ISet<string> AdditionalPrimitiveTypes = new HashSet<string>()
        {
            "System.String","System.Decimal", "System.Int32", "System.Int64", "System.DateTime", "System.DateTimeOffset", "System.Boolean"
        };

        private bool IsPrimitive(Type t)
        {
            return t.IsPrimitive || AdditionalPrimitiveTypes.Contains(t.FullName);
        }

        private Type GetArrayUnderlyingTypeOrNull(Type t)
        {
            if (t.FullName == "System.String")
                return null;

            //From: https://stackoverflow.com/questions/906499/getting-type-t-from-ienumerablet

            // short-circuit if you expect lots of arrays 
            if (t.IsArray)
                return t.GetElementType();

            // type is IEnumerable<T>;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return t.GetGenericArguments()[0];

            // type implements/extends IEnumerable<T>;
            return t
                .GetInterfaces()
                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(x => x.GenericTypeArguments[0])
                .FirstOrDefault();
        }

        private CompoundType ReflectTypeI(UnpackedType t, int depthGuard)
        {
            if (depthGuard >= 15)
                throw new Exception("Reflected type stopped at depth 15 to prevent an infinite loop. Cycle in the type maybe?");

            string ctName;
            if (compoundTypeNameByDotNetTypeName.ContainsKey(t.ActualType.FullName))
                ctName = compoundTypeNameByDotNetTypeName[t.ActualType.FullName];
            else
            {
                ctName = CreateNewSubtypeName();
                compoundTypeNameByDotNetTypeName[t.ActualType.FullName] = ctName;
            }

            if (compoundTypeByName.ContainsKey(ctName))
                return compoundTypeByName[ctName];

            var ct = new CompoundType
            {
                Name = ctName,
                CompoundProperties = new List<CompoundProperty>(),
                PrimtiveProperties = new List<PrimtiveProperty>()
            };
            var properties = t.ActualType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Select(x => new
            {
                Name = x.Name,
                Type = Unpack(x.PropertyType)
            }).ToList();
            foreach (var p in properties.Where(x => x.Type.IsPrimitive))
            {
                var pp = new PrimtiveProperty
                {
                    Name = p.Name,
                    IsArray = p.Type.IsArray,
                    IsNullable = p.Type.IsNullable
                };
                var pt = p.Type.ActualType;

                if (pt.FullName == "System.String")
                    pp.TypeCode = PrimitiveTypeCode.String.ToString();
                else if (pt.FullName == "System.Decimal")
                    pp.TypeCode = PrimitiveTypeCode.Decimal.ToString();
                else if (pt.FullName == "System.Int32")
                    pp.TypeCode = PrimitiveTypeCode.Int.ToString();
                else if (pt.FullName == "System.Int64")
                    pp.TypeCode = PrimitiveTypeCode.Int.ToString();
                else if (pt.FullName == "System.DateTime")
                    pp.TypeCode = PrimitiveTypeCode.Date.ToString();
                else if (pt.FullName == "System.DateTimeOffset")
                    pp.TypeCode = PrimitiveTypeCode.Date.ToString();
                else if (pt.FullName == "System.Boolean")
                    pp.TypeCode = PrimitiveTypeCode.Boolean.ToString();
                else
                    throw new Exception($"Unsupported primitive type '{pt.FullName}' in [{t.ActualType.FullName}].{p.Name}");

                ct.PrimtiveProperties.Add(pp);
            }

            foreach (var p in properties.Where(x => !x.Type.IsPrimitive))
            {
                ct.CompoundProperties.Add(new CompoundProperty
                {
                    Name = p.Name,
                    IsArray = p.Type.IsArray,
                    Type = ReflectTypeI(p.Type, ++depthGuard)
                });
            }

            compoundTypeByName[ct.Name] = ct;

            return ct;
        }
    }
}
