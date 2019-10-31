namespace PlexSSO.Model
{
    public class BasicResponse
    {
        public bool Success { get; }

        public BasicResponse(bool success)
        {
            Success = success;
        }
    }
}
