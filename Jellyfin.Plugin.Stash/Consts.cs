namespace Stash
{
    public static class Consts
    {
        public const string SceneSearchQuery = @"
        query {{
          findScenes({0}) {{
            scenes {{
              id
              title
              date
              paths {{
                screenshot
              }}
            }}
          }}
        }}
        ";

        public const string SceneQuery = @"
        query {{
          findScene(id: {0}) {{
            id
            title
            details
            date
            paths {{
              screenshot
            }}
            studio {{
              name
              parent_studio {{
                name
              }}
            }}
            tags {{
              name
            }}
            performers {{
              id
              name
              image_path
            }}
          }}
        }}
        ";

        public const string PerformerSearchQuery = @"
        query {{
          findPerformers({0}) {{
            performers {{
              id
              name
              image_path
              birthdate
            }}
          }}
        }}
        ";

        public const string PerformerQuery = @"
        query {{
          findPerformer(id: {0}) {{
            id
            name
            image_path
            alias_list
            birthdate
            death_date
          }}
        }}
        ";
    }
}
