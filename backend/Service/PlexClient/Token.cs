using System;

namespace PlexSSO.Service.PlexClient
{
    public class Token : ValueType<string>
    {
        public Token(string token) : base(token) {
            if (string.IsNullOrWhiteSpace(token)) {
                throw new ArgumentException("Provided token cannot be null or empty");
            }
        }
    }
}
