using System;
using System.Text.Json.Serialization;

namespace PlexSSO.Model.Types
{
    [JsonConverter(typeof(NameIdentifierConverter))]
    public class UserIdentifier : ValueType<string>
    {
        public UserIdentifier(in string userIdentifier)
            : base(userIdentifier)
        {
            if (string.IsNullOrWhiteSpace(userIdentifier))
            {
                throw new ArgumentException("Provided user identifier cannot be null or empty");
            }
        }
    }

    public class NameIdentifierConverter : ValueTypeConverter<string, UserIdentifier>
    {
    }
}
