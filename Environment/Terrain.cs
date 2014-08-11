using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment {
    public struct TerrainNode {
        public XReal Height;
        public TerrainType Type;
    }
    public class Terrain {

        private TerrainNode[,] tiles;

        public int Width { get { return tiles.GetLength(0); } }
        public int Height { get { return tiles.GetLength(1); } }

        public XVector3 Scale = new XVector3(2, (XReal)0.8f, 2);

        public Terrain() {
        }

        public void Resize(int width, int height) {
            var newTiles = new TerrainNode[width, height];
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    newTiles[x, y] = new TerrainNode() {
                        Height = 0,
                        Type = TerrainType.GrassA
                    };
                }
            }
            if (tiles != null) {
                int minWidth = Math.Min(width, Width),
                    minHeight = Math.Min(height, Height);
                for (int x = 0; x < minWidth; ++x) {
                    for (int y = 0; y < minHeight; ++y) {
                        newTiles[x, y] = tiles[x, y];
                    }
                }
            }
            tiles = newTiles;
        }

        public TerrainNode this[int x, int y] {
            get { return tiles[x, y]; }
            set { tiles[x, y] = value; }
        }


        public XReal GetHeightAt(XVector2 tpos) {
            int tx = (tpos.X / Scale.X).ToInt, ty = (tpos.Y / Scale.Z).ToInt;
            if (tx < 0) tx = 0; else if (tx > Width - 1) tx = Width - 1;
            if (ty < 0) ty = 0; else if (ty > Height - 1) ty = Height - 1;
            return tiles[tx, ty].Height * Scale.Y;
        }
    }
}
