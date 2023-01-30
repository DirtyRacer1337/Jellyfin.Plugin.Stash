using System;
using System.Collections.Generic;

namespace Stash.Models
{
    public struct Scene
    {
        public string id;
        public string title;
        public string details;
        public DateTime? date;
        public Paths paths;
        public Studio? studio;
        public List<Tags> tags;
        public List<Performer> performers;
    }
}
