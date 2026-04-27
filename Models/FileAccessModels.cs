using System;

namespace GLC_EXPRESS.Models
{
    public class StoredFileAccessLogRecord
    {
        public StoredFileAccessLogRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            AccessedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string Kind { get; set; }

        public string StoredPath { get; set; }

        public string FileName { get; set; }

        public string Username { get; set; }

        public string Action { get; set; }

        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public DateTime AccessedAtUtc { get; set; }
    }
}