using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.XScript.RMS {
    public class RMInflueceMap {

        int sizeX, sizeY;
        public List<float[,]> Maps = new List<float[,]>();

        public RMInflueceMap(int xSize, int ySize) {
            sizeX = xSize;
            sizeY = ySize;
        }

        public void EnsureMaps(int levels) {
            for (int l = 0; l < levels; ++l)
                if (l >= Maps.Count) Maps.Add(new float[sizeX, sizeY]);
                else {
                    if (Maps[l].GetLength(0) < sizeX ||
                        Maps[l].GetLength(1) < sizeY)
                        Maps[l] = new float[sizeX, sizeY];
                }
        }

        public void AddArea(XPoint2 pos, int size, float coherence) {
            EnsureMaps(1);
            var map = Maps[0];
            int fringe = (int)Math.Round(
                (size * (1 - coherence))
            );
            XRectangle range = new XRectangle(pos.X, pos.Y, 0, 0).
                Inflate(size + fringe).
                RestrictToWithin(
                    new XRectangle(0, 0, sizeX, sizeY)
                );
            range = new XRectangle(0, 0, sizeX, sizeY);
            List<TerrainType> types = new List<TerrainType>();
            float noiseRnd = pos.X + pos.Y * 10;
            for (int x = range.Left; x < range.Right; ++x) {
                for (int y = range.Top; y < range.Bottom; ++y) {
                    int dist2 = (x - pos.X) * (x - pos.X) + (y - pos.Y) * (y - pos.Y);
                    float noise =
                        SimplexNoise.simplex_noise_2octaves(
                            (float)x / (size + 2), noiseRnd, (float)y / (size + 2)
                        ) / 2 - 0.5f;
                    map[x, y] = (1 - (float)Math.Sqrt(dist2) / size) +
                        (noise - 0.5f) * (1 - coherence) * 2;
                }
            }
        }

        public void BlurArea(int amount) {
            if (amount == 0) return;
            var map = Maps[0];
            float[,] newMap = new float[sizeX, sizeY];
            XRectangle range = new XRectangle(0, 0, sizeX, sizeY);
            for (int x = range.Left; x < range.Right; ++x) {
                for (int y = range.Top; y < range.Bottom; ++y) {
                    float ave = 0, cnt = 0;
                    for (int dx = -amount; dx <= amount; ++dx) {
                        for (int dy = -amount; dy <= amount; ++dy) {
                            if (dx * dx + dy * dy > amount * amount) continue;
                            float weight = 1 - (float)Math.Sqrt(
                                (float)(dx * dx + dy * dy) / (amount * amount)
                            );
                             int px = x + dx, py = y + dy;
                            if (px >= 0 && py >= 0 && px < sizeX && py < sizeY) {
                                ave += map[px, py] * weight;
                                cnt += weight;
                            }
                        }
                    }
                    newMap[x, y] = ave / cnt;
                }
            }
            Maps[0] = newMap;
        }

        public void WithAreaTiles(Action<XPoint2, int> onTile) {
            var map = Maps[0];
            XRectangle range = new XRectangle(0, 0, sizeX, sizeY);
            for (int x = range.Left; x < range.Right; ++x) {
                for (int y = range.Top; y < range.Bottom; ++y) {
                    float weight = map[x, y];
                    //if (weight > 0)
                    {
                        onTile(new XPoint2(x, y), (int)(weight * 20));
                    }
                }
            }
        }

    }
}
