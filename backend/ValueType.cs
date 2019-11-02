using System;

namespace PlexSSO
{
    public abstract class ValueType<T>
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
            var other = obj as ValueType<T>;
            if (other != null) {
                return Value.Equals(other.Value);
            }
            return Value.Equals(obj);
        }

        public static bool operator ==(ValueType<T> a, ValueType<T> b)
        {
            return a.Value.Equals(b.Value);
        }

        public static bool operator !=(ValueType<T> a, ValueType<T> b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
