using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace LogTool.LogProcessor.Parser
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ParsedLog
    {
        [JsonProperty]
        public string HeaderLine { get; set; }

        [JsonProperty]
        public string SystemVersion { get; set; }

        [JsonProperty]
        public string LogFile { get; set; }

        [JsonProperty]
        public Dictionary<string, Account> Accounts { get; } = new Dictionary<string, Account>();

        public void ExportJson(string saveAs)
        {
            string json = JsonConvert.SerializeObject(this);
            File.WriteAllText(saveAs, json);
        }

    }
}