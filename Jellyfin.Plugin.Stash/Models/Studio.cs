namespace Stash.Models
{
    public struct Studio
    {
        public string id;
        public string name;
        public ParentStudio? parent_studio;
        public string image_path;
    }
}
