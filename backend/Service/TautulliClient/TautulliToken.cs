using System;

namespace PlexSSO.Service.TautulliClient
{
    public class TautulliToken : ValueType<string>
    {
        public TautulliToken(string uuid, string token) : base(token)
        {
            if (string.IsNullOrWhiteSpace(uuid))
            {
                throw new ArgumentException("Provided token cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Provided token cannot be null or empty");
            }

            UUID = uuid;
        }

        public string UUID { get; }
    }
}
