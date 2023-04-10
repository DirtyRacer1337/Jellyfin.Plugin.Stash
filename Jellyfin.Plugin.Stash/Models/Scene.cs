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
        public Path paths;
        public List<Movies?> movies;
        public Studio? studio;
        public List<Tag> tags;
        public List<Performer> performers;
    }
}
