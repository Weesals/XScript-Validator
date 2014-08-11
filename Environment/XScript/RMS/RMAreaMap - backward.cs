using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using System.Diagnostics;

namespace Environment.XScript.RMS {
    public class RMAreaCreator {

        public struct Node {
            public float Weight;
            public float Extra;
        }

        public Node[,] Map;
        XRectangle activeArea;

        public Func<XVector2, XPoint2> MetersToTiles;

        public RMAreaCreator(int xSize, int ySize, Func<XVector2, XPoint2> metersToTiles) {
            Map = new Node[xSize, ySize];
            MetersToTiles = metersToTiles;
        }

        static XPoint2[] propogOffs = new[] {
            new XPoint2(-1, 0), new XPoint2(1, 0), new XPoint2(0, -1), new XPoint2(0, 1),
            new XPoint2(-1, -1), new XPoint2(1, -1), new XPoint2(-1, 1), new XPoint2(1, 1),
        };
        public void AddArea(XPoint2 pos, int size, float coherence, Func<XPoint2, bool> validityCheck) {
            List<XPoint2> stack = new List<XPoint2>();
            stack.Add(pos);
            int at = 0;
            int loopAt = stack.Count;
            Map[pos.X, pos.Y] = new Node() { Weight = 100000 };
            List<XPoint2> tmp = new List<XPoint2>();
            activeArea = new XRectangle(pos.X, pos.Y, 1, 1);
            while (size > 0) {
                float maxV = float.MinValue;
                for (int i = 0; i < stack.Count; ++i) {
                    var p = stack[i];
                    var node = Map[p.X, p.Y];
                    Debug.Assert(node.Weight > 0,
                        "Weight < 0 means the tile is already accepted!");
                    float weight = node.Weight + (1 + node.Extra);
                    if (weight > maxV) { maxV = weight; at = i; }
                }
                var pnt = stack[at];
                {
                    --size;
                    stack.RemoveAt(at--);
                    --loopAt;
                    float val = Map[pnt.X, pnt.Y].Weight;
                    Debug.Assert(val > 0,
                        "Tile must have a positive weight to start with!");
                    Map[pnt.X, pnt.Y].Weight = -1;
                    activeArea = activeArea.ExpandToInclude(pnt);
                    const float NoiseScale = 30;
                    /*if (coherence < 1) {
                        float noise = (SimplexNoise.simplex_noise_3octaves(pnt.X / NoiseScale, 0, pnt.Y / NoiseScale) / 2 - 0.5f);
                        val *= 1 + (noise - 0.5f) * (1 - coherence) / 0.5f;
                    }*/
                    tmp.Clear();
                    for (int p = 0; p < propogOffs.Length; ++p) {
                        XPoint2 pnt2 = pnt + propogOffs[p];
                        if (pnt2.X >= 0 && pnt2.Y >= 0 && pnt2.X < Map.GetLength(0) && pnt2.Y < Map.GetLength(1)) {
                            if (!validityCheck(pnt2)) continue;
                            if (Map[pnt2.X, pnt2.Y].Weight < 0) continue;
                            tmp.Add(pnt2);
                        }
                    }
                    for (int t = 0; t < tmp.Count; ++t) {
                        var pnt2 = tmp[t];
                        XPoint2 delta = pnt2 - pnt;
                        float noise = (SimplexNoise.simplex_noise_3octaves(pnt2.X / NoiseScale,
                            (delta.X + delta.Y * 2) * 5,
                            pnt2.Y / NoiseScale) / 2 - 0.5f);
                        float tVal = val - (float)Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                        //tVal *= 1 + (noise - 0.5f) * (1 - coherence) / 0.3f;
                        Map[pnt2.X, pnt2.Y].Weight = Math.Max(Map[pnt2.X, pnt2.Y].Weight, tVal);
                        Map[pnt2.X, pnt2.Y].Extra = Math.Max(
                            Map[pnt2.X, pnt2.Y].Extra,
                            (noise - 0.0f) * (1 - coherence) / 0.5f * 50
                        );
                        if (!stack.Contains(pnt2)) stack.Add(pnt2);
                    }
                /*} else {
                    stack.RemoveAt(at--);
                    --loopAt;*/
                }
                if (stack.Count == 0) break;
                /*at += 1;
                while (at >= loopAt) {
                    at -= loopAt;
                    loopAt = stack.Count;
                }*/
            }
        }

        public void BlurArea(int amount) {
            if (amount == 0) return;
            var map = Map;
            int sizeX = Map.GetLength(0), sizeY = Map.GetLength(1);
            Node[,] newMap = new Node[sizeX, sizeY];
            XRectangle range = activeArea.
                Inflate(1).
                RestrictToWithin(new XRectangle(0, 0, sizeX, sizeY));
            for (int x = range.Left; x < range.Right; ++x) {
                for (int y = range.Top; y < range.Bottom; ++y) {
                    float ave = 0, cnt = 0;
                    for (int dx = -amount; dx <= amount; ++dx) {
                        for (int dy = -amount; dy <= amount; ++dy) {
                            if (dx * dx + dy * dy > amount * amount) continue;
                            float weight = 1 - //(float)Math.Sqrt
                                ((float)(dx * dx + dy * dy) / (amount * amount));
                            int px = x + dx, py = y + dy;
                            if (px >= 0 && py >= 0 && px < sizeX && py < sizeY) {
                                ave += (map[px, py].Weight < 0 ? -1 : 0) * weight;
                                cnt += weight;
                            }
                        }
                    }
                    newMap[x, y].Weight = ave / cnt;
                    if (newMap[x, y].Weight < -0.5f) {
                        Debug.Assert(activeArea.Contains(new XPoint2(x, y)),
                            "A blurred area should never be larger than the previous area (in terms of bounding box)");
                    }
                }
            }
            activeArea = range;
            Map = newMap;
        }

        public void WithAreaTiles(Action<XPoint2, int> onTile) {
            var map = Map;
            XRectangle range = activeArea;
            for (int x = range.Left; x < range.Right; ++x) {
                for (int y = range.Top; y < range.Bottom; ++y) {
                    float weight = map[x, y].Weight;
                    if (weight < -0.5f)
                    {
                        onTile(new XPoint2(x, y), 10);//(int)(-weight * 20));
                    }
                }
            }
        }

    }
    public class RMAreaMap {

        public RMArea[,] Map;

        public Func<XVector2, XPoint2> MetersToTiles;

        public RMAreaMap(int xSize, int ySize, Func<XVector2, XPoint2> metersToTiles) {
            Map = new RMArea[xSize, ySize];
            MetersToTiles = metersToTiles;
        }

        public void SetTileArea(XPoint2 pnt, RMArea area) {
            Map[pnt.X, pnt.Y] = area;
        }

        public int GetDistanceSquaredToArea(XPoint2 pnt, RMArea area, int minDist) {
            Debug.Assert(area.Built,
                "GetDistanceSquaredToArea() does not work for unbuilt areas");
            XRectangle range = area.ActiveArea;
            range.RestrictToWithin(new XRectangle(pnt.X, pnt.Y, 1, 1));
            range.Inflate(minDist);
            int minDist2 = int.MaxValue;
            for (int x = range.Left; x < range.Right; ++x) {
                for (int y = range.Top; y < range.Bottom; ++y) {
                    int dx = x - pnt.X, dy = y - pnt.Y;
                    int dist2 = dx * dx + dy * dy;
                    if (dist2 > minDist * minDist) continue;
                    if (Map[x, y] == area) {
                        if (dist2 < minDist2) minDist2 = dist2;
                    }
                }
            }
            return minDist2;
        }

    }

}
