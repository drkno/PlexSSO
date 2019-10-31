using System;

namespace PlexSSO
{
    public class ValueType<T>
    {
        public T Value { get; }

        public ValueType(T token) {
            if (token == null) {
                throw new ArgumentException("Provided argument cannot be null");
            }
            Value = token;
        }

        public override bool Equals(object obj)
        {
            return Value.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
