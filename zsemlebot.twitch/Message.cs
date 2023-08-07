using System;
using System.Collections.Generic;

namespace zsemlebot.twitch
{
    public class Message
    {
        public IReadOnlyDictionary<string, Tag> Tags { get; }
        public string? Source { get; }
        public string Command { get; }
        public string Params { get; }

        public string? SourceUserName { get; }
        public int? SourceUserId { get; }

        public Message(string? source, string command, string parameters, IReadOnlyDictionary<string, Tag> tags)
        {
            Source = source;
            Command = command;
            Params = parameters;
            Tags = tags;

            SourceUserName = GetUserName(source);
            SourceUserId = GetUserId(tags);
        }

        private static string? GetUserName(string? source)
        {
            if (source == null)
            {
                return null;
            }

            var tokens = source.Split('!');
            if (tokens.Length == 0)
            {
                return source;
            }

            return tokens[0];
        }

        private static int? GetUserId(IReadOnlyDictionary<string, Tag> tags)
        {
            if (!tags.TryGetValue("user-id", out var tag))
            {
                return null;
            }

            if (!int.TryParse(tag.Value, out var result))
            {
                return null;
            }

            return result;
        }
    }
}
