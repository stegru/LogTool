using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace LogTool.LogProcessor
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Calendar
    {
        [JsonProperty]
        public string Title { get; }

        [JsonProperty]
        public string AccountId { get; }

        [JsonProperty]
        public string Id { get; }

        [JsonProperty]
        public Dictionary<string,object> ExtraData { get; set; }

        /// <summary>
        /// Color of the calendar
        /// </summary>
        public string ColorString
        {
            get
            {
                if (this.ExtraData.TryGetValue("color", out object colorValue))
                {
                    Match match = new Regex(@"^sRGB.*colorspace ([0-9.]+) ([0-9.]+) ([0-9.]+) ([0-9.]+)").Match(colorValue.ToString()!);
                    if (match.Success)
                    {
                        int r = (int)Math.Round(double.Parse(match.Groups[1].Value) * 0xff);
                        int g = (int)Math.Round(double.Parse(match.Groups[2].Value) * 0xff);
                        int b = (int)Math.Round(double.Parse(match.Groups[3].Value) * 0xff);
                        return string.Format("#{0:X2}{1:X2}{2:X2}", r,g,b);
                    }
                }

                return "#000000";
            }
        }

        public static Calendar FromLogEntry(string logEntry)
        {
            string[] args = logEntry.Trim().Split(',', 4, StringSplitOptions.TrimEntries);
            return new Calendar(args[0], args[1], args[2]);
        }

        public Calendar(string title, string accountId, string id)
        {
            this.Title = title;
            this.AccountId = accountId;
            this.Id = id;
        }
    }
}