using System;
using System.Collections.Generic;

namespace LogTool.LogProcessor
{
    public class SyncQueue
    {
        public string AccountId { get; set; }
        public DateTime? LastSync { get; set; }
        public bool Synced => this.LastSync != null;

        public List<string> Items { get; set; }
    }
}