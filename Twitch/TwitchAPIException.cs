using System;

namespace StreamMarkers.Twitch
{
    public class TwitchAPIException: Exception
    {
        public TwitchAPIException() {}
        public TwitchAPIException(string message): base(message) {}
        public TwitchAPIException(string message, Exception inner) : base(message, inner) {}
    }
}