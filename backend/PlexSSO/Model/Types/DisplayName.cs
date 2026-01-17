using System;
using System.Text.Json.Serialization;

namespace PlexSSO.Model.Types
{
    [JsonConverter(typeof(DisplayNameConverter))]
    public class DisplayName : ValueType<string>
    {
        public DisplayName(in string displayName)
            : base(displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Provided display name cannot be null or empty");
            }
        }

        public DisplayName(in string displayName, in Username username)
            : this(string.IsNullOrWhiteSpace(displayName) ? username.Value : displayName)
        {
        }
    }

    public class DisplayNameConverter : ValueTypeConverter<string, DisplayName>
    {
    }
}
