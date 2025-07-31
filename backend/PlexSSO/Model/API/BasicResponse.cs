using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlexSSO.Model.API
{
    public class BasicResponse
    {
        public bool Success { get; }

        public BasicResponse(bool success)
        {
            Success = success;
        }

        public override string ToString()
        {
            var serialiserConfig = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            return JsonSerializer.Serialize<object>(this, serialiserConfig);
        }

        private bool Equals(BasicResponse other)
        {
            return Success == other.Success;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((BasicResponse)obj);
        }

        public override int GetHashCode()
        {
            return Success.GetHashCode();
        }
    }
}
