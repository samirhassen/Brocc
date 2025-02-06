using System;

namespace nPreCredit
{
    public class RequireableStruct<T> where T : struct
    {
        private readonly T? value;
        private readonly string context;

        public RequireableStruct(T? value, string context)
        {
            this.value = value;
            this.context = context;
        }

        public bool HasValue
        {
            get
            {
                return value.HasValue;
            }
        }

        public T Required
        {
            get
            {
                if (!value.HasValue)
                    throw new Exception($"{context}: Missing required value");
                return value.Value;
            }
        }

        public T? Optional
        {
            get
            {
                return value;
            }
        }
    }
}
