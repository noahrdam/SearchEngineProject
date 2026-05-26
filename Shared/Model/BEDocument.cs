using System;
namespace Shared.Model;
    public class BEDocument
    {
        public int Id { get; set; }

        public string Url { get; set; }

        public DateTime IdxTime { get; set; }

        public DateTime CreationTime { get; set; }

        public string? Content { get; set; }
    }

