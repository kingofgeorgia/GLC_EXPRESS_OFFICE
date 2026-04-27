using System;
using System.Collections.Generic;

namespace GLC_EXPRESS.Models
{
    public class HomeInquiryCommentRecord
    {
        public string Author { get; set; }

        public string Text { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class HomeInquiryRecord
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Messenger { get; set; }

        public string Direction { get; set; }

        public string CargoType { get; set; }

        public string ClientComment { get; set; }

        public string Source { get; set; }

        public string Status { get; set; }

        public string AssignedManager { get; set; }

        public List<HomeInquiryCommentRecord> Comments { get; set; }

        public string AttachmentPath { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}