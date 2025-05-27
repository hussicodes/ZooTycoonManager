using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public class Tile
    {
        public bool Walkable;
        public int TextureIndex;
        public bool HasTree;

        public Tile()
        {
            Walkable = true;
            TextureIndex = 0;
            HasTree = false;
        }

        public Tile(bool walkable, int textureIndex = 0)
        {
            Walkable = walkable;
            TextureIndex = textureIndex;
        }
    }
}
