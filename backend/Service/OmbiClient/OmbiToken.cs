using System;

namespace PlexSSO.Service.OmbiClient
{
    public class OmbiToken : ValueType<string>
    {
        public OmbiToken(string token) : base(token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Provided token cannot be null or empty");
            }
        }
    }
}
