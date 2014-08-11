using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;
using System.Diagnostics;

namespace RTS4.Environment.XScript.RMS {
    public class RMAreaCreator {

        public struct Node {
            public int Weight;
            public int Extra;
            public bool IsPositive {
                get {
                    return Weight >= 45 && Weight != int.MaxValue;
                }
            }
        }

        public Node[,] Map;
        XRectangle activeArea;

        public Func<XVector2, XPoint2> MetersToTiles;

        public RMAreaCreator(int xSize, int ySize, Func<XVector2, XPoint2> metersToTiles) {
            Map = new Node[xSize, ySize];
            MetersToTiles = metersToTiles;
        }

        static readonly XPoint2[] propogOffs = new[] {
            new XPoint2(-1, 0), new XPoint2(1, 0), new XPoint2(0, -1), new XPoint2(0, 1),
            new XPoint2(-1, -1), new XPoint2(1, -1), new XPoint2(-1, 1), new XPoint2(1, 1),
        };
        const float root2 = 1.4142135623730950488016887242097f;
        //static readonly float[] propogCost = new[] { 1, 1, 1, 1, root2, root2, root2, root2 };
        static readonly int[] propogCost = new[] { 70, 70, 70, 70, 99, 99, 99, 99 };
        public void AddArea(XPoint2 pos, int size, XReal coherence, Func<XPoint2, bool> validityCheck) {
            int origSize = size;
            List<XPoint2> stack = new List<XPoint2>();
            stack.Add(pos);
            int at = 0;
            for (int x = 0; x < Map.GetLength(0); ++x) {
                for (int y = 0; y < Map.GetLength(1); ++y) {
                    Map[x, y].Weight = int.MaxValue;
                    Map[x, y].Extra = 0;
                }
            }
            Debug.Assert(pos.X >= 0 && pos.Y >= 0 &&
                pos.X < Map.GetLength(0) && pos.Y < Map.GetLength(1),
                "Out of bounds");
            Map[pos.X, pos.Y] = new Node() { Weight = 70 };
            activeArea = new XRectangle(pos.X, pos.Y, 1, 1);
            while (size > 0 && stack.Count > 0) {
                float minV = float.MaxValue;
                for (int i = 0; i < stack.Count; ++i) {
                    var p = stack[i];
                    var node = Map[p.X, p.Y];
                    float weight = Math.Abs(node.Weight) + node.Extra;
                    if (weight < minV) { minV = weight; at = i; }
                }
                var pnt = stack[at];
                --size;
                stack.RemoveAt(at--);
                int val = Map[pnt.X, pnt.Y].Weight;
                activeArea = activeArea.ExpandToInclude(pnt);
                const float NoiseScale = 30;
                for (int p = 0; p < propogOffs.Length; ++p) {
                    XPoint2 pnt2 = pnt + propogOffs[p];
                    if (pnt2.X >= 0 && pnt2.Y >= 0 && pnt2.X < Map.GetLength(0) && pnt2.Y < Map.GetLength(1)) {
                        if (!validityCheck(pnt2)) continue;
                        if (Map[pnt2.X, pnt2.Y].Weight <= val + propogCost[p]) continue;
                        int extraW = 0;
                        for (int p2 = 0; p2 < propogOffs.Length; ++p2) {
                            XPoint2 delta = propogOffs[p2];
                            float noise = (SimplexNoise.simplex_noise_3octaves(pnt2.X / NoiseScale,
                                (delta.X + delta.Y * 2) * 5,
                                pnt2.Y / NoiseScale) / 2 - 0.5f);
                            extraW = Math.Max(
                                extraW,
                                (int)(20 * 70 * (noise - 0.0f) * (1 - coherence) / 0.5f)
                            );//origSize / 46
                        }

                        if (!stack.Contains(pnt2)) {
                            if (Map[pnt2.X, pnt2.Y].IsPositive) size++;
                            stack.Add(pnt2);
                        }

                        Map[pnt2.X, pnt2.Y].Weight = val + propogCost[p] * Math.Sign(val);
                        if (extraW > Map[pnt2.X, pnt2.Y].Extra) Map[pnt2.X, pnt2.Y].Extra = extraW;
                    }
                }
            }
            for (int s = 0; s < stack.Count; ++s) {
                var pnt = stack[s];
                Map[pnt.X, pnt.Y].Weight = int.MinValue;
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
            for (int y = range.Top; y < range.Bottom; ++y) {
                for (int x = range.Left; x < range.Right; ++x) {
                    int ave = 0, cnt = 0;
                    for (int dy = -amount; dy <= amount; ++dy) {
                        for (int dx = -amount; dx <= amount; ++dx) {
                            if (dx * dx + dy * dy > amount * amount) continue;
                            int weight = 100 -
                                (100 * (int)(dx * dx + dy * dy) / (amount * amount));
                            int px = x + dx, py = y + dy;
                            if (px >= 0 && py >= 0 && px < sizeX && py < sizeY) {
                                ave += (map[px, py].IsPositive ? 70 : 0) * weight;
                                cnt += weight;
                            }
                        }
                    }
                    newMap[x, y].Weight = ave / cnt;
                    if (newMap[x, y].IsPositive) {
                        Debug.Assert(activeArea.Contains(new XPoint2(x, y)),
                            "A blurred area should never be larger than the previous area (in terms of bounding box)");
                    }
                }
            }
            activeArea = range;
            Map = newMap;
        }

        void CalculateDistances(int minDistance, int maxDistance) {
            var map = Map;
            XRectangle range = activeArea;
            XRectangle mapRect = new XRectangle(0, 0, Map.GetLength(0), Map.GetLength(1));
            range.Inflate(maxDistance);
            range.RestrictToWithin(mapRect);
            List<XPoint2> stack = new List<XPoint2>();
            for (int y = range.Top; y < range.Bottom; ++y) {
                for (int x = range.Left; x < range.Right; ++x) {
                    int minDist = int.MaxValue;
                    for (int o = 0; o < propogOffs.Length; ++o) {
                        XPoint2 off = propogOffs[o];
                        if (!mapRect.Contains(new XPoint2(x + off.X, y + off.Y))) continue;
                        if (map[x + off.X, y + off.Y].IsPositive != map[x, y].IsPositive) {
                            int newCost = propogCost[o];
                            minDist = Math.Min(minDist, newCost);
                        }
                    }
                    if (minDist < int.MaxValue) {
                        stack.Add(new XPoint2(x, y));
                        map[x, y].Weight = (map[x, y].IsPositive ? 70 : 70 - minDist);
                    } else {
                        map[x, y].Weight = (map[x, y].IsPositive ? maxDistance * 70 : minDistance * 70);
                    }
                }
            }
            while (stack.Count > 0) {
                int count = stack.Count;
                for (int s = 0; s < count; ++s) {
                    var pnt = stack[s];
                    var node = map[pnt.X, pnt.Y];
                    for (int o = 0; o < propogOffs.Length; ++o) {
                        var newP = pnt + propogOffs[o];
                        if (!mapRect.Contains(newP)) continue;
                        var tnode = map[newP.X, newP.Y];
                        if (node.IsPositive != tnode.IsPositive) continue;
                        int newCost = node.Weight + (node.IsPositive ? propogCost[o] : -propogCost[o]);
                        if (Math.Abs(newCost) < Math.Abs(tnode.Weight)) {
                            map[newP.X, newP.Y].Weight = newCost;
                            stack.Add(newP);
                        }
                    }
                }
                stack.RemoveRange(0, count);
            }
        }

        public void WithAreaTiles(Action<XPoint2, int> onTile, int minDistance, int maxDistance) {
            var map = Map;
            XRectangle range = activeArea;
            //range.Inflate(maxDistance);
            range.RestrictToWithin(new XRectangle(0, 0, Map.GetLength(0), Map.GetLength(1)));
            CalculateDistances(minDistance - 1, maxDistance + 1);
            for (int y = range.Top; y < range.Bottom; ++y) {
                for (int x = range.Left; x < range.Right; ++x) {
                    int dist = map[x, y].Weight / 70;
                    if (dist >= minDistance) {
                        onTile(new XPoint2(x, y), dist);
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
            int sizeX = Map.GetLength(0), sizeY = Map.GetLength(1);
            XRectangle range = area.ActiveArea;
            range.RestrictToWithin(new XRectangle(pnt.X, pnt.Y, 1, 1));
            range.Inflate(minDist);
            range.RestrictToWithin(new XRectangle(0, 0, sizeX, sizeY));
            int minDist2 = int.MaxValue;
            for (int x = range.Left; x < range.Right; ++x) {
                for (int y = range.Top; y < range.Bottom; ++y) {
                    int dx = x - pnt.X, dy = y - pnt.Y;
                    int dist2 = dx * dx + dy * dy;
                    if (dist2 > minDist * minDist) continue;
                    var tile = Map[x, y];
                    if (tile != null)
                        while (tile != area && tile.Parent != null) tile = tile.Parent;
                    if (tile == area) {
                        if (dist2 < minDist2) minDist2 = dist2;
                    }
                }
            }
            return minDist2;
        }

    }

}
