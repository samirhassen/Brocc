using System;

namespace nPreCredit
{
    public class RequireableClass<T> where T : class
    {
        private readonly T value;
        private readonly string context;

        public RequireableClass(T value, string context)
        {
            this.value = value;
            this.context = context;
        }

        public bool HasValue
        {
            get
            {
                return value != null;
            }
        }

        public T Required
        {
            get
            {
                if (!HasValue)
                    throw new Exception($"{context}: Missing required value");
                return value;
            }
        }

        public T RequiredOneOf(params T[] args)
        {
            if (!HasValue)
                throw new Exception($"{context}: Missing required value");
            foreach (var a in args)
            {
                if (value.Equals(a))
                    return value;
            }

            throw new Exception($"{context}: Required item has an unkown value");
        }

        public T Optional
        {
            get
            {
                return value;
            }
        }

        public T OptionalOneOf(params T[] args)
        {
            if (HasValue)
            {
                foreach (var a in args)
                {
                    if (value.Equals(a))
                        return value;
                }
                throw new Exception($"{context}: Optional item has an unkown value");
            }
            else
            {
                return Optional;
            }
        }
    }
}
