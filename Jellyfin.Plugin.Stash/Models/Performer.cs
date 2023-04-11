using System;
using System.Collections.Generic;

namespace Stash.Models
{
    public struct Performer
    {
        public string id;
        public string name;
        public string image_path;
        public List<string> alias_list;
        public DateTime? birthdate;
        public DateTime? death_date;
        public string country;
    }
}
