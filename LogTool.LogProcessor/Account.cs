using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace LogTool.LogProcessor
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Account
    {
        [JsonProperty]
        public string Name { get; }

        [JsonProperty]
        public string Id { get; }

        [JsonProperty]
        public Dictionary<string, Calendar> Calendars { get; } = new Dictionary<string, Calendar>();

        [JsonProperty]
        public SyncQueue SyncQueue { get; set; }

        [JsonProperty]
        public Dictionary<string,object> ExtraData { get; set; }

        public string Icon
        {
            get
            {
                // Use the favion from the domain of the api endpoint.
                if (this.ExtraData.TryGetValue("serverURL", out object serverUrl))
                {
                    if (Uri.TryCreate(serverUrl.ToString(), UriKind.Absolute, out Uri uri))
                    {
                        Match match = new Regex(@".*(\b[^.]+\....?)$").Match(uri.Host);
                        string host = match.Success ? match.Groups[1].Value : uri.Host;
                        return $"https://icons.duckduckgo.com/ip3/{host}.ico";
                    }
                }

                return "https://icons.duckduckgo.com/ip3/example.com.ico";
            }
        }

        /// <summary>Create an account from an account log entry</summary>
        public static Account FromLogEntry(string logEntry)
        {
            string[] args = logEntry.Trim().Split(',', 3, StringSplitOptions.TrimEntries);
            return new Account(args[0], args[1]);
        }

        public Account(string name, string id)
        {
            this.Name = name;
            this.Id = id;
        }

        public void AddCalendar(Calendar calendar)
        {
            this.Calendars[calendar.Id] =  calendar;
        }
    }
}