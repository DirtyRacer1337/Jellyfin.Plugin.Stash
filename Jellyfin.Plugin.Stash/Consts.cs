namespace Stash
{
    public static class Consts
    {
        public const string SceneSearchQuery = @"query{{findScenes({0}){{scenes{{id,title,date,paths{{screenshot}}}}}}}}";

        public const string SceneQuery = @"query{{findScene(id:{0}){{id,title,details,date,paths{{screenshot}}studio{{name,parent_studio{{name,image_path}},image_path}}tags{{name}}performers{{id,name,disambiguation,image_path}}}}}}";

        public const string PerformerSearchQuery = @"query{{findPerformers({0}){{performers{{id,name,disambiguation,image_path,birthdate}}}}}}";

        public const string PerformerQuery = @"query{{findPerformer(id:{0}){{id,name,details,disambiguation,image_path,alias_list,birthdate,death_date,country,tags{{name}}}}}}";

        public const string StudiosSearchQuery = @"query{{findStudios({0}){{studios{{id,name,image_path}}}}}}";

        public const string StudioQuery = @"query{{findStudio(id:{0}){{id,name,image_path}}}}";
    }
}
