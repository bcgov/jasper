using System;
using System.Collections.Generic;


namespace Scv.Api.Models.Assignment
{
    public class Assignment
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public string Location { get; set; }
    }
}

