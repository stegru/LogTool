using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace LogTool.LogProcessor.Parser
{
    /// <summary>
    /// Parses a log file.
    /// </summary>
    public class LogParser
    {
        public string LogFile { get; }
        private ParsedLog log;

        private LogParser(string logFile)
        {
            this.LogFile = logFile;
        }

        public static IEnumerable<ParsedLog> ProcessPath(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);

            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                // It's a directory - process all files inside it.
                foreach (string file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    ParsedLog parsedLog = LogParser.ParseFile(file);
                    if (parsedLog != null)
                    {
                        yield return parsedLog;
                    }
                }
            }
            else if (Path.GetExtension(path).ToLower() == ".zip")
            {
                // zip file - extract to a temporary directory, and process it.
                string tempDir = Path.Combine(Path.GetTempPath(),
                    "logparser-" + RandomNumberGenerator.GetInt32(int.MaxValue).ToString("X"));
                ZipFile.ExtractToDirectory(path, tempDir);

                foreach (ParsedLog parsedLog in LogParser.ProcessPath(tempDir))
                {
                    yield return parsedLog;
                }

                Directory.Delete(tempDir, true);
            }
            else
            {
                // normal file - try to parse it
                ParsedLog parsedLog = LogParser.ParseFile(path);
                if (parsedLog != null)
                {
                    yield return parsedLog;
                }
            }
        }

        public static ParsedLog ParseFile(string logFile)
        {
            ParsedLog parsedLog;
            switch (Path.GetExtension(logFile).ToLower())
            {
                case ".log":
                    LogParser parser = new LogParser(logFile);
                    parsedLog = parser.ParseFile();
                    break;

                case ".json":
                    parsedLog = JsonConvert.DeserializeObject<ParsedLog>(File.ReadAllText(logFile));

                    if (parsedLog != null)
                    {
                        parsedLog.LogFile = logFile;
                    }

                    break;

                default:
                    parsedLog = null;
                    break;
            }

            return parsedLog;
        }

        /// <summary>Returns the log entries.</summary>
        private Queue<string> GetLogEntries()
        {
            // Read the whole file, and split it into pieces delimited by a timestamp.
            string content = File.ReadAllText(this.LogFile);
            Regex logEntry = new Regex(@"\n+[0-9/]{10} [0-9:]{12}  ");

            return new Queue<string>(logEntry.Split(content));
        }

        /// <summary>Parses a log file.</summary>
        public ParsedLog ParseFile()
        {
            this.log = new ParsedLog();
            this.log.LogFile = this.LogFile;

            Queue<string> logEntries = this.GetLogEntries();
            Regex getLogItem = new Regex(@"^(?<item>[^:]+):\s*(?<value>.*)?", RegexOptions.Singleline);

            this.log.HeaderLine = logEntries.Dequeue();

            while (logEntries.Count > 0)
            {
                string logEntry = logEntries.Dequeue();

                Match match = getLogItem.Match(logEntry);
                string item = match.Groups["item"].Value;
                string value = match.Groups["value"].Value;

                switch (item)
                {
                    case "System version":
                        this.log.SystemVersion = value;
                        break;
                    case "Accounts":
                        this.ReadAccounts(this.ReadGroup(logEntries));
                        break;
                    case "Calendars":
                        this.ReadCalendars(this.ReadGroup(logEntries));
                        break;
                    case "Sync queues":
                        this.ReadSyncQueues(value);
                        break;
                    case "Verbose sources":
                        this.ReadVerboseItems(logEntries, false);
                        break;
                    case "Verbose calendars":
                        this.ReadVerboseItems(logEntries, true);
                        break;

                }


                Console.WriteLine("# [" + logEntry + "]");
                Console.WriteLine($"= '{item}' '{value}'");
            }

            return this.log;
        }

        /// <summary>
        /// Reads the next log entries which are part of a group (where they start with a tab).
        /// </summary>
        private IEnumerable<string> ReadGroup(Queue<string> logEntries)
        {
            while (logEntries.TryPeek(out string entry) && entry.StartsWith("\t"))
            {
                yield return logEntries.Dequeue().Trim();
            }
        }

        /// <summary>
        /// Read the accounts - called after seeing an "Accounts:" entry.
        /// </summary>
        private void ReadAccounts(IEnumerable<string> logEntries)
        {
            foreach (string logEntry in logEntries)
            {
                Account account = Account.FromLogEntry(logEntry);
                this.log.Accounts[account.Id] = account;
            }
        }

        /// <summary>
        /// Read the calendars - called after seeing an "Calenders:" entry.
        /// </summary>
        private void ReadCalendars(IEnumerable<string> logEntries)
        {
            foreach (string logEntry in logEntries)
            {
                Calendar calendar = Calendar.FromLogEntry(logEntry);
                this.log.Accounts[calendar.AccountId].AddCalendar(calendar);
            }
        }

        /// <summary>
        /// Parses the sync queues log entry
        /// </summary>
        private void ReadSyncQueues(string value)
        {
            Regex getQueue = new Regex(@"

                # source handler name and account id
                ^(?<name>\w+)\s+\/\s+(?<account>\S+)\s

                # owner and last sync
                \(
                  (?<owner>[^,]+),\s+
                  last\ssync:\s(?<last>[^,]+)
                  [^)]+
                \):\s+

                # the queue
                <FBSyncQueue:[^(]+\(\s+

                  # optional log data
                  (""
                    (?<log>(\\.|[^""\\]+)+)
                  ""(\s*,\s*)?)*

                \s*^\)>(\n|$)

            ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);

            MatchCollection matches = getQueue.Matches(value);

            foreach (Match match in matches)
            {
                SyncQueue sync = new SyncQueue();
                sync.AccountId = match.Groups["account"].Value;
                sync.LastSync = DateTime.TryParse(match.Groups["last"].Value, out DateTime lastSync)
                    ? lastSync
                    : null;
                sync.Items = match.Groups["log"].Captures
                    .Select(c => c.Value.Replace("\\n", "\n").Replace("\\", ""))
                    .ToList();

                this.log.Accounts[sync.AccountId].SyncQueue = sync;
            }
        }

        private void ReadVerboseItems(Queue<string> logEntries, bool calendars)
        {
            // Gets the json-like data from the log entry
            Regex getVerboseSource = new Regex(@"^<\w+:\s0x[0-9a-f]+> (\{.*)", RegexOptions.Singleline);

            // Collect the following entries that match the regex
            while (logEntries.TryPeek(out string entry))
            {
                Match match = getVerboseSource.Match(entry);
                if (!match.Success)
                {
                    break;
                }

                logEntries.Dequeue();

                Dictionary<string,object> data = this.ParseJsonLike(match.Groups[1].Value);

                string id = data["identifier"].ToString();

                if (calendars)
                {
                    string sourceId = data["sourceIdentifier"].ToString();
                    this.log.Accounts[sourceId].Calendars[id].ExtraData = data;
                }
                else
                {
                    this.log.Accounts[id].ExtraData = data;
                }

            }

        }

        private Dictionary<string, object> ParseJsonLike(string content)
        {
            // Massage it into json
            string json = content.Replace(";\n", ",\n").Replace("=", ":").Replace("(", "[").Replace(")", "]");
            // Quote unquoted non-number values
            json = new Regex(@"(?<key>^\s+(\w+|""[^""]*"")\s:\s)(?<value>(?!"")(?![0-9]+,)[^, ]+),$", RegexOptions.Multiline).Replace(json, "${key}\"${value}\",");
            // fix the \U escsape code
            json = json.Replace("\\U", "\\u");

            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        }

    }
}