using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Common;

/*
 * rmSetAreaSmoothDistance: blurs the influence map
 * rmSetAreaCoherence: lerps the influence map to circles instead of noise
 * rmSetAreaHeightBlend: fades the height out around edges
 */

namespace Environment.XScript.RMS {
    public class XRandomMap {
        // More details about a similar API available from
        // http://tournament.jpn.org/modules/xpwiki/397.html

        public int cMapSize { get; set; }
        public int cNumberNonGaiaPlayers { get { return players.Count - 1; } }
        public int cNumberPlayers { get { return players.Count; } }
        public int cNumberTeams { get { return 2; } }

        public float SeaLevel { get; set; }
        public int MapSizeX { get { return world.Terrain.Width; } }
        public int MapSizeZ { get { return world.Terrain.Height; } }
        public const float MetersPerTile = 2;

        Random rand;
        World world;

        List<RMPlayer> players = new List<RMPlayer>();
        List<RMArea> areas = new List<RMArea>();
        List<RMClass> classes = new List<RMClass>();
        List<RMConstraint> constraints = new List<RMConstraint>();

        ////////////// class RandomMap //////////////
        // http://aomcodereference.googlecode.com/svn/trunk/doc/aom/scripting/xs/rm/RandomMap.html
        public int rmClassID(string name) {
            for (int i = 0; i < classes.Count; ++i) if (classes[i].Name == name) return i;
            return -1;
        }
        public int rmDefineClass(string classname) {
            classes.Add(new RMClass(classname));
            return classes.Count - 1;
        }
        // From AOE3?
        //public void rmDefineConstant(string name, int value) { }

        public float rmRandFloat(float min, float max) { return (float)rand.NextDouble() * (max - min) + min; }
        public int rmRandInt(int min, int max) { return rand.Next(min, max); }

        public float rmGetSeaLevel() { return SeaLevel; }
        public void rmSetSeaLevel(float level) { SeaLevel = level; }
        public void rmSetSeaType(string name) { }

        public void rmSetGaiaCiv(long civ) { }

        public void rmSetLightingSet(string name) { }

        public void rmSetMapSize(int x, int z) {
            world.Terrain.Resize((int)(x / MetersPerTile), (int)(z / MetersPerTile));
        }

        public void rmTerrainInitialize(string baseTerrain, float height) {
            var type = TerrainType.FromString(baseTerrain);
            Debug.Assert(type != null,
                "Unable to find " + baseTerrain + " terrain type");
            for(int x=0;x<world.Terrain.Width;++x) {
                for(int y=0;y<world.Terrain.Height;++y) {
                    world.Terrain[x, y] = new TerrainNode() { Height = height, Type = type };
                }
            }
        }


        ////////////// class Converter //////////////
        // http://aomcodereference.googlecode.com/svn/trunk/doc/aom/scripting/xs/rm/Converter.html
        public int rmAreaFractionToTiles(float fraction) { return (int)Math.Round(fraction * (MapSizeX * MapSizeX)); }
        public float rmAreaTilesToFraction(int tiles) { return (float)tiles / (MapSizeX * MapSizeX); }

        public float rmXTilesToFraction(int x) { return (float)x / MapSizeX; }
        public float rmZTilesToFraction(int z) { return (float)z / MapSizeZ; }
        public float rmXTilesToMeters(int x) { return (float)x * MetersPerTile; }
        public float rmZTilesToMeters(int z) { return (float)z * MetersPerTile; }
        public int rmXFractionToTiles(float x) { return (int)Math.Round(x * MapSizeX); }
        public int rmZFractionToTiles(float z) { return (int)Math.Round(z * MapSizeZ); }
        public float rmXFractionToMeters(float x) { return x * MapSizeX * MetersPerTile; }
        public float rmZFractionToMeters(float z) { return z * MapSizeZ * MetersPerTile; }
        public float rmXMetersToFraction(float x) { return x / MapSizeX / MetersPerTile; }
        public float rmZMetersToFraction(float z) { return z / MapSizeZ / MetersPerTile; }
        public int rmXMetersToTiles(float x) { return (int)Math.Round(x / MetersPerTile); }
        public int rmZMetersToTiles(float z) { return (int)Math.Round(z / MetersPerTile); }

        public XVector2 tileToFrac(XVector2 tile) { return tile / new XVector2(MapSizeX, MapSizeZ); }
        public XVector2 fracToMeters(XVector2 frac) { return frac * new XVector2(MapSizeX, MapSizeZ) * MetersPerTile; }

        public float rmDegreesToRadians(float degrees) { return degrees * (float)Math.PI / 180; }
        public float rmRadiansToDegrees(float radians) { return radians / 180 * (float)Math.PI; }


        ////////////// class Area //////////////
        // http://aomcodereference.googlecode.com/svn/trunk/doc/aom/scripting/xs/rm/Area.html
        /**Adds a class for an area's cliff edge to avoid.*/
        public void rmAddAreaCliffEdgeAvoidClass(int areaID, int avoidID, float minDist) { }

        /**Add specified constraint to an area.*/
        public bool rmAddAreaConstraint(int areaID, int constraintID) {
            if (constraintID >= 0) areas[areaID].Constraints.Add(constraints[constraintID]);
            return true;
        }

        /**Adds an area influence point.*/
        public void rmAddAreaInfluencePoint(int areaID, float xFraction, float zFraction) { }

        /**
         * Adds an area influence segment. You may want an area to grow towards specific points or lines.
         * A circular area placed at the center of the map with an influence point of 1, 1 will produce a peninsula that protrudes towards 12 oâ€™clock. 
         * Influence points and segments can be useful in getting areas, such as rivers, to extend beyond the edge of the map. 
         */
        public void rmAddAreaInfluenceSegment(int areaID, float xFraction1, float zFraction1, float xFraction2, float zFraction2) { }

        /**
         * Add an unit type that the specified area removes.
         * Sometimes you may want an area to clean itself of objects, such as removing trees from ice.
         * This will only work if the objects are already placed before the area, which is the reverse of how most ES maps are generated.
         * You can reference specific units or abstract types, such as "unit" and "building."
         */
        public void rmAddAreaRemoveType(int areaID, string typeName) { }

        /**
         * Adds a terrain layer to an area. Terrain layers allow you to place a border of one or more textures around an area.
         * For example, you can have grassDirt50 and grassDirt75 around an area of grass. You can specify multiple layers for an area, 
         * as long as the minDistance for one starts where the maxDistance for another leaves off. 
         * Because different textures overlap one another you may need to experiment with distances to get a good effect.
         * Here is an example:
         * <code>
         * rmSetAreaTerrainType(areaID, "GrassA");
         * rmAddAreaTerrainLayer(areaID, "SnowGrass75", 13, 20);
         * rmAddAreaTerrainLayer(areaID, "SnowGrass50", 6, 13);
         * rmAddAreaTerrainLayer(areaID, "SnowGrass25", 0, 6);
         * </code>
         */
        public void rmAddAreaTerrainLayer(int areaID, string terrain, float minDist, float maxDist) {
            areas[areaID].TerrainLayers.Add(new RMArea.TerrainLayer() {
                TerrainType = terrain,
                MinDist = minDist,
                MaxDist = maxDist,
            });
        }

        /**Adds a terrain replacement rule to the area. If you place an area with no terrain specified, it will use the terrain of the parent area (including the base map).
         * However, specifying terrain replacement will paint an area only when another texture is present.
         * This command is most useful with connections, where you want to replace water with land where a connection goes across a river, or replace rock with snow for mountain passes.
         */
        public void rmAddAreaTerrainReplacement(int areaID, string terrainTypeName, string newTypeName) { }

        /**Add given area to specified class.*/
        public bool rmAddAreaToClass(int areaID, int classID) {
            classes[classID].Areas.Add(areas[areaID]);
            return true;
        }

        /**Gets the area ID for given area name.*/
        public int rmAreaID(string name) {
            int i = areas.Count - 1;
            for (; i >= 0 && string.Compare(areas[i].Name, name, true) != 0; --i) ;
            return i;
        }

        /**Creates an area and returns the areaID.*/
        public int rmCreateArea(string name, int parentAreaID = -1) {
            RMArea area = new RMArea() { Name = name };
            if (parentAreaID >= 0) area.Parent = areas[parentAreaID];
            int id = areas.Count;
            areas.Add(area);
            return id;
        }

        /**
         * Simultaneously builds all unbuilt areas. This does not include connections.
         * @see #rmBuildArea(int)
         */
        public void rmBuildAllAreas() {
            // TODO: Should not just do them 1 by 1, should create influence maps
            // and build them all at the same time
            for (int a = 0; a < areas.Count; ++a) {
                if (!areas[a].Built) rmBuildArea(a);
            }
        }

        /**
         * Builds the specified area. Actually builds the area.
         * Choosing when to use this command can have a big effect on your map. 
         * For example, if you place each player area one by one, the first few will have enough room to build,
         * but if after 11 areas, area 12 still needs to be placed, it might have run out of space because the others were to greedy.
         * To avoid this, build all player area's at the same time, so that the script can try to find a fair balance between all areas.
         * @see #rmBuildAllAreas()
         */
        public bool rmBuildArea(int areaID) {
            //if (areaID != 128) return;
            var area = areas[areaID];
            for (int t = 0; t < 100 && float.IsNaN(area.Position.X); ++t) {
                float tAreaX = (float)rand.NextDouble() * MapSizeX * MetersPerTile,
                    tAreaY = (float)rand.NextDouble() * MapSizeX * MetersPerTile;
                bool ok = true;
                for (int c = 0; c < area.Constraints.Count; ++c) {
                    var constraint = area.Constraints[c];
                    if (!constraint.IsPointOk(tAreaX, tAreaY)) { ok = false; break; }
                }
                if (ok) area.Position = fracToMeters(new XVector2(tAreaX, tAreaY));
            }
            int areaX = rmXMetersToTiles(area.Position.X), areaY = rmZMetersToTiles(area.Position.Y);
            float areaSizeF = rmXMetersToFraction(
                (area.MinSize + (area.MaxSize - area.MinSize) * (float)rand.NextDouble())
            );
            /*int areaSize = 1 + (int)Math.Sqrt(rmAreaFractionToTiles(
                (area.MinSize + (area.MaxSize - area.MinSize) * (float)rand.NextDouble())
            ));*/
            int areaSize = 1 + (int)Math.Sqrt(rmAreaFractionToTiles(areaSizeF));
            List<TerrainType> types = new List<TerrainType>();
            for (int l = 0; l < area.TerrainLayers.Count; ++l) {
                types.Add(TerrainType.FromString(area.TerrainLayers[l].TerrainType));
            }
            var influence = new RMInflueceMap(world.Terrain.Width, world.Terrain.Height);
            influence.AddArea(new XPoint2(areaX, areaY), areaSize, area.Coherence);
            influence.BlurArea((int)(area.InfluenceBlurDistance / MetersPerTile));
            influence.WithAreaTiles(delegate(XPoint2 pnt, int dist) {
                int x = pnt.X, y = pnt.Y;
                var terrain = world.Terrain[x, y];
                float heightBlendTiles = area.HeightBlendDistance / MetersPerTile;
                if (!float.IsNaN(area.BaseHeight)) {
                    if (dist < -heightBlendTiles / 2) {
                        // Out of range: Do nothing
                    } else if (dist < heightBlendTiles / 2) {
                        float lerp = (float)dist / heightBlendTiles + 0.5f;
                        terrain.Height += (area.BaseHeight - terrain.Height) *
                            (lerp * lerp * (2 - lerp) * (2 - lerp));
                    } else
                        terrain.Height = area.BaseHeight;
                }
                if (types.Count > 0 && dist >= 0) {
                    TerrainType type = types[0];
                    for (int t = 1; t < area.TerrainLayers.Count; ++t) {
                        if (area.TerrainLayers[t].MinDist <= dist &&
                            dist < area.TerrainLayers[t].MaxDist && types[t] != null) {
                            type = types[t];
                            break;
                        }
                    }
                    terrain.Type = type;
                }
                if (area.CliffType != null) {
                    if (dist >= -2 && dist < 2) {
                        terrain.Type = TerrainType.GreekCliff;
                    }
                }
                world.Terrain[x, y] = terrain;
            });
            /*Random rand2 = new Random(areaID);
            for (int x = range.Left; x < range.Right; ++x) {
                for (int y = range.Top; y < range.Bottom; ++y) {
                    int dist2 = (x - areaX) * (x - areaX) + (y - areaY) * (y - areaY);
                    float noise = (SimplexNoise.simplex_noise_2octaves((float)x / 20, 0, (float)y / 20) - 1) / 2;
                    int rnd = (int)(noise * areaSize);
                    int size = areaSize - rnd;
                    int smoothSize = size + areaSmooth;
                    if (dist2 < smoothSize * smoothSize) {
                        var terrain = world.Terrain[x, y];
                        if (!float.IsNaN(area.BaseHeight)) {
                            float over = dist2 > size * size ?
                                ((float)Math.Sqrt(dist2) - size) / areaSmooth :
                                0;
                            terrain.Height += (area.BaseHeight - terrain.Height) * (1 - over);
                        }
                        if (types.Count > 0 && dist2 < size * size) {
                            int dist = size - (int)Math.Sqrt(dist2);
                            TerrainType type = types[0];
                            for (int t = 1; t < area.TerrainLayers.Count; ++t) {
                                if (area.TerrainLayers[t].MinDist <= dist &&
                                    dist <= area.TerrainLayers[t].MaxDist && types[t] != null)
                                {
                                    type = types[t];
                                    break;
                                }
                            }
                            terrain.Type = type;
                        }
                        world.Terrain[x, y] = terrain;
                    }
                }
            }*/
            area.Built = true;
            return true;
        }

        /**Paints the terrain for a specified area.*/
        public void rmPaintAreaTerrain(int areaID) { }

        /**Sets the base height for an area.*/
        public void rmSetAreaBaseHeight(int areaID, float height) {
            areas[areaID].BaseHeight = height;
        }

        /**
         * Set cliff edge parameters for an area. Determines whether there should be pathable ramps or not connecting the top of the cliff to the surrounding area. 
         * @param count Number of cliff edges to create. The count times the size should not be more than 1.0. Defaults to 1. 
         * @param size This specifies how much of the area's outline should be turned into cliff edges. It should be between 0.0 and 1.0. Set to 1.0 to make the whole area surrounded. Defaults to 0.5. 
         * @param variance The variance to use for the size. Defaults to 0.0. 
         * @param spacing Spacing modifier. This should be between 0.0 and 1.0. The smaller this is, the closer together the cliff edges will be. Defaults to 1.0. 
         * @param mapEdge Specifies where the cliff edge should be in relation to the map edge. Set to 0 for any, 1 to be away from the map edge, or 2 to be close to the map edge. Defaults to 0.
         */
        public void rmSetAreaCliffEdge(int areaID, int count, float size, float variance, float spacing, int mapEdge) { }

        /**
         * Set an area's cliff height.
         * @param val Make positive for raised cliffs and negative for lowered cliffs. Defaults to 4.0.
         * @param variance The variance to use for the height.
         * @param ramp This is used to determine how quickly the height ramps up to the cliff height (it refers to steepness in this context, not pathable ramps to reach the top of a cliff). Defaults to 0.5.
         */
        public void rmSetAreaCliffHeight(int areaID, float val, float variance, float ramp) {
            areas[areaID].BaseHeight = val;
        }

        /**
         * Set cliff painting options for an area.
         * Determines how a cliff is painted with impassable and passable textures.
         * @param paintGround Specifies if the ground should be painted or just left whatever it already is. Defaults true.
         * @param paintSide Specifies if the cliff sides should be painted. Defaults true.
         * @param paintOutsideEdge Specifies if the outside cliff edge should be painted. This is the area between the cliff side and the ground. Defaults true.
         * @param minSideHeight Specifies the minimum height that a cliff tile must be sloped before treating it as a cliff side. Set to 0 to have the minimum amount of cliff sides painted. Defaults to 1.5.
         * @param paintInsideEdge Specifies if the inside cliff edge should be painted. This is the area between the cliff side and the ground. Defaults true.
         */
        public void rmSetAreaCliffPainting(int areaID, bool paintGround, bool paintOutsideEdge, bool paintSide, float minSideHeight, bool paintInsideEdge) { }

        /**Sets the cliff type for an area.*/
        public void rmSetAreaCliffType(int areaID, string cliffName) {
            areas[areaID].CliffType = cliffName;
            areas[areaID].HeightBlendDistance = 2;
        }

        /**Sets area coherence (0-1).*/
        public void rmSetAreaCoherence(int areaID, float coherence) {
            areas[areaID].Coherence = coherence;
        }

        /**Sets the forest type for an area.*/
        public void rmSetAreaForestType(int areaID, string forestName) { }

        /**
         * Sets how smoothly area height blends into surroundings. Corresponds to the smooth tool in the Scenario Editor. 
         * Usually a heightBlend of 0 will leave geometric-looking jagged edges. A heightBlend of 1 will smooth smaller areas.
         * A heightBlend of 2 will smooth larger areas or areas of disproportionate heights. Anything above 2 may flatten an area completely.
         */
        public void rmSetAreaHeightBlend(int areaID, int heightBlend) {
            areas[areaID].HeightBlendDistance = heightBlend * 5;
        }

        /**Set the area location.*/
        public void rmSetAreaLocation(int areaID, float xFraction, float zFraction) {
            areas[areaID].Position = fracToMeters(new XVector2(xFraction, zFraction));
        }

        /**Set the area location to player's location.*/
        public void rmSetAreaLocPlayer(int areaID, int playerID) {
            areas[areaID].Position = players[playerID].Position;
        }

        /**Set the area location to team's location.*/
        public void rmSetAreaLocTeam(int areaID, int teamID) { }

        /**Sets maximum blob distance.*/
        public void rmSetAreaMaxBlobDistance(int areaID, float dist) { }

        /**Sets maximum number of area blobs. An area can be placed with multiple blobs. Blobs are placed independently, using the minimum and maximum distances below. 
         * Areas made with a single blob will be circular. Areas made with multiple blobs can be come long and sinuous.*/
        public void rmSetAreaMaxBlobs(int areaID, int blobs) { }

        /**Sets minimum blob distance.*/
        public void rmSetAreaMinBlobDistance(int areaID, float dist) { }

        /**Sets minimum number of area blobs.*/
        public void rmSetAreaMinBlobs(int areaID, int blobs) { }

        /**Set the area size to a min/max fraction of the map.*/
        public void rmSetAreaSize(int areaID, float minFraction, float maxFraction) {
            areas[areaID].MinSize = rmXFractionToMeters(minFraction);
            areas[areaID].MaxSize = rmXFractionToMeters(maxFraction);
        }

        /**Sets area edge smoothing distance (distance is number of neighbouring points to consider in each direction).*/
        public void rmSetAreaSmoothDistance(int areaID, int smoothDistance) {
            areas[areaID].InfluenceBlurDistance = smoothDistance;
        }

        /**Specifies if the area should vary the terrain layer edges. Usually, variance in terrain layers looks better, 
         * but sometimes you might want to turn it off. Defaults to true.*/
        public void rmSetAreaTerrainLayerVariance(int areaID, bool variance) {
            areas[areaID].TerrainLayerVariance = variance;
        }

        /**
         * Sets the terrain type for an area.
         * Even if your area does not place special terrain, 
         * it can be helpful to temporarily paint the area with a distinct texture, 
         * such as "Black" or "SnowA", to see where and if it is actually getting placed.
         */
        public void rmSetAreaTerrainType(int areaID, string terrainTypeName) {
            areas[areaID].TerrainLayers.Add(new RMArea.TerrainLayer() {
                TerrainType = terrainTypeName,
            });
        }

        /**
         * Sets whether the area build process will warn if it fails. 
         * It is very easy to over-constrain areas to the point where there is no room for them.
         * This can cause two problems: the map may take a long time to generate, or if you are in debug mode, 
         * the debugger will pop up and generation will stop. 
         * Sometimes you want to catch these errors, 
         * but when you are done with your map it is a good idea to set SetAreaWarnFailure to false.
         *
         */
        public void rmSetAreaWarnFailure(int areaID, bool warn) { }

        /**Sets the water type for an area.*/
        public void rmSetAreaWaterType(int areaID, string waterName) { }


        ////////////// class Connection //////////////
        // http://aomcodereference.googlecode.com/svn/trunk/src/aom/scripting/xs/rm/Connection.java
        public int cConnectAreas { get; set; }
        public int cConnectPlayers { get; set; }
        public int cConnectAllies { get; set; }
        public int cConnectEnemies { get; set; }

        /**
	     * Adds an area to the connection. This is only valid if you set the connection type is set to cConnectAreas. 
	     * You must specify this while defining the area, after the connection is defined, and before building the connection.
	     */
        public void rmAddConnectionArea(int connectionID, int areaID) { }

        /**Add specified constraint to a connection.*/
        public bool rmAddConnectionConstraint(int connectionID, int constraintID) { return false; }

        /**Add specified constraint for a connection end point.*/
        public bool rmAddConnectionEndConstraint(int connectionID, int constraintID) { return false; }

        /**Add specified constraint for a connection start point.*/
        public bool rmAddConnectionStartConstraint(int connectionID, int constraintID) { return false; }

        /**Adds a terrain replacement rule to the connection.*/
        public void rmAddConnectionTerrainReplacement(int connectionID, string terrainTypeName, string newTypeName) { }

        /**Adds the connection to specified class.*/
        public void rmAddConnectionToClass(int connectionID, int classID) { }

        /**Builds the given connection.*/
        public void rmBuildConnection(int connectionID) { }

        /**Creates an connection.*/
        public void rmCreateConnection(string name) { }

        /**Sets the base height of a connection.*/
        public void rmSetConnectionBaseHeight(int connectionID, float width) { }

        /**Sets the base terrain cost for a connection.*/
        public void rmSetConnectionBaseTerrainCost(int connectionID, float cost) { }

        /**Sets area coherence (0.0-1.0).*/
        public void rmSetConnectionCoherence(int connectionID, float width) { }

        /**Sets how smoothly connection height blends into surroundings.*/
        public void rmSetConnectionHeightBlend(int connectionID, float width) { }

        /**
         * Sets the position variance of a connection. The connection will normally start at the area's position, but this allows it to vary from that position.
         * You can set this to -1 for it to pick completely random positions within the starting and ending areas. 
         * This command is often needed when specifying multiple connections (for example, one within a team and another between teams) so that the connections do not overlap.
         */
        public void rmSetConnectionPositionVariance(int connectionID, float variance) { }

        /**Sets connection edge smoothing distance (distance is number of neighboring points to consider in each direction).*/
        public void rmSetConnectionSmoothDistance(int connectionID, float width) { }

        /**Sets the terrain cost for a connection.*/
        public void rmSetConnectionTerrainCost(int connectionID, string terrainTypeName, float cost) { }

        /**
         * Sets the connection type.
         * @param connectionType This command determines which players are connected. The valid values are: 
         * <li>cConnectAreas: This is the default that is used if you don't call rmSetConnectionType. You have to specify each area to be connected by calling rmAddConnectionArea().</li> 
         * <li>cConnectPlayers: Connect all player areas.</li>
         * <li>cConnectAllies: Connect all ally player areas.</li> 
         * <li>cConnectEnemies: Connect enemy player areas.</li>
         * 
         * @param connectAll Set this parameter to true if you want all of the areas to get connected to all of the other areas.
         * Set it to false to have the areas connected sequentially where the first area gets connected to the second area, 
         * the second area gets connected to the third area, etc.
         * 
         * @param connectPercentage You can use this parameter to reduce the number of connections that are generated.
         * For example, if you set it to 0.5, then half of the connections will get generated. The ones that are generated are randomly chosen. 
         * Some ES maps with connections connect all players when player number is small (<6) and uses a connection percentage on larger maps, 
         * otherwise so many connections can get placed that the barrier (like water or rock) is obscured.
         */
        public void rmSetConnectionType(int connectionID, int connectionType, bool connectAll, float connectPercentage) { }

        /**Sets whether a connection warns on failure.*/
        public void rmSetConnectionWarnFailure(int connectionID, bool warn) { }

        /**Sets the width of a connection.*/
        public void rmSetConnectionWidth(int connectionID, float width, float variance) { }


        ////////////// class Constraint //////////////
        // http://aomcodereference.googlecode.com/svn/trunk/src/aom/scripting/xs/rm/Constraint.java
        /**Gets constraint ID for given constraint name.*/
        public int rmConstraintID(string name) {
            for (int i = 0; i < constraints.Count; ++i) if (constraints[i].Name == name) return i;
            return -1;
        }

        /**Make a constraint that forces something to remain within an area. Returns its ID.*/
        // not used
        //public int rmCreateAreaConstraint(string name, int areaID) { return rmCreateAreaMaxDistanceConstraint(name, areaID, 0); }

        /**Make a constraint that forces something to stay away from an area. Returns its ID.*/
        public int rmCreateAreaDistanceConstraint(string name, int areaID, float distance) {
            constraints.Add(new RMConstraintAreas(name) {
                MinDistance = distance,
                AreaProvider = new [] { areas[areaID] },
            });
            return constraints.Count;
        }

        /**Make a constraint that forces something to remain within a given distance from the areaID. Returns its ID.*/
        // not used
        //public int rmCreateAreaMaxDistanceConstraint(string name, int areaID, float distance) { return -1; }

        /**Make an area overlap constraint. This prevents areas from overlapping. Returns its ID.*/
        // not used
        //public int rmCreateAreaOverlapConstraint(string name, int areaID) { return -1; }

        /**Make a box constraint and forces something to remain in it. Returns its ID.*/
        public int rmCreateBoxConstraint(string name, float startX, float startZ, float endX, float endZ, float bufferFraction) {
            // NOTE: Unsure of use of bufferFraction, documentation unclear
            constraints.Add(new RMConstraintBox(name) {
                Box = new XRectangleF(startX - bufferFraction, startZ - bufferFraction,
                    endX - startX + bufferFraction * 2, endZ - startZ + bufferFraction * 2),
            });
            return constraints.Count - 1;
        }

        /**Make a class distance constraint taht forces something to stay away from everything in the given class. Returns its ID.*/
        public int rmCreateClassDistanceConstraint(string name, int classID, float distance) {
            constraints.Add(new RMConstraintAreas(name) {
                MinDistance = distance,
                AreaProvider = classes[classID].Areas,
            });
            return constraints.Count - 1;
        }

        /**Make a constraint that forces something to remain within an area's cliff edge. Returns its ID.*/
        // not used
        //public int rmCreateCliffEdgeConstraint(string name, int areaID) { return -1; }

        /**Make an area cliff edge distance constraint. Returns its ID.*/
        // not used
        //public int rmCreateCliffEdgeDistanceConstraint(string name, int areaID, float distance) { return -1; }

        /**Make an area cliff edge max distance constraint. Returns its ID.*/
        public int rmCreateCliffEdgeMaxDistanceConstraint(string name, int areaID, float distance) {
            constraints.Add(new RMConstraintEdge(name) {
                MinDistance = 0,
                MaxDistance = distance,
                Area = areas[areaID],
            });
            return constraints.Count - 1;
        }

        /**Make a constraint that forces something to remain within an area's cliff ramp edge. Returns its ID.*/
        public int rmCreateCliffRampConstraint(string name, int areaID) { return -1; }

        /**Make an area cliff ramp edge distance constraint. Returns its ID.*/
        // not used
        //public int rmCreateCliffRampDistanceConstraint(string name, int areaID, float distance) { return -1; }

        /**Make an area cliff ramp edge max distance constraint. Returns its ID.*/
        // not used
        //public int rmCreateCliffRampMaxDistanceConstraint(string name, int areaID, float distance) { return -1; }

        /**Make a constraint that forces something to remain within an area's edge. Returns its ID.*/
        // not used
        //public int rmCreateEdgeConstraint(string name, int areaID) { return -1; }

        /**Make an area edge distance constraint and returns its ID*/
        public int rmCreateEdgeDistanceConstraint(string name, int areaID, float distance) {
            constraints.Add(new RMConstraintEdge(name) {
                MinDistance = distance,
                MaxDistance = float.MaxValue,
                Area = areas[areaID],
            });
            return constraints.Count - 1;
        }

        /**Make an area edge max distance constraint. Returns its ID.*/
        // not used
        //public int rmCreateEdgeMaxDistanceConstraint(string name, int areaID, float distance) { return -1; }

        /**Make a constraint to avoid terrain with certain a passability.*/
        public int rmCreateTerrainDistanceConstraint(string name, string type, bool passable, float distance) { return -1; }

        /**Make a constraint to be close to terrain with certain a passability.*/
        public int rmCreateTerrainMaxDistanceConstraint(string name, string type, bool passable, float distance) { return -1; }

        /**Make a type distance constraint.*/
        public int rmCreateTypeDistanceConstraint(string name, string className, float distance) { return -1; }


        ////////////// class FairLoc //////////////
        // http://aomcodereference.googlecode.com/svn/trunk/src/aom/scripting/xs/rm/FairLoc.java
        /**Adds some fairLoc placement info.*/
        public int rmAddFairLoc(string unitName, bool forward, bool inside, float minPlayerDist, float maxPlayerDist, float locDist, float edgeDist, bool playerArea, bool teamArea) { return -1; }

	    /**Add specified constraint to a fairLoc placement.*/
        public bool rmAddFairLocConstraint(int fairLocID, int constraintID) { return false; }
	
	    /**Gets a player's fairLoc x fraction.*/
        public float rmFairLocXFraction(int playerID, int index) { return -1; }

	    /**Gets a player's fairLoc z fraction.*/
        public float rmFairLocZFraction(int playerID, int index) { return -1; }

	    /**Gets a player's number of fairLocs.*/
        public int rmGetNumberFairLocs(int playerID) { return -1; }
	
	    /**Places down a fairLoc and returns whether or not it was succesful.*/
        public bool rmPlaceFairLocs() { return false; }
	
	    /**Resets fairLoc placement info.*/
        public void rmResetFairLocs() { }


        ////////////// class ObjectDef //////////////
        // http://aomcodereference.googlecode.com/svn/trunk/src/aom/scripting/xs/rm/ObjectDef.java
        /**Set the maximum distance for the object definition (in meters).*/
        public void rmSetObjectDefMaxDistance(int defID, float dist) { }

	    /**Set the minimum distance for the object definition (in meters).*/
        public void rmSetObjectDefMinDistance(int defID, float dist) { }
	
	    /**Add specified constraint to given object def.*/
        public bool rmAddObjectDefConstraint(int defID, int constraintID) { return false; }

	    /**Add item to object definition.*/
        public void rmAddObjectDefItem(int defID, string unitName, int count, float clusterDistance) { }

	    /**Add given object def to specified class.*/
        public bool rmAddObjectDefToClass(int objectDefID, int classID) { return false; }
	
	    /**Creates an object definition. Returns its ID. */
        public int rmCreateObjectDef(string name) { return -1; }
	
	    /**Returns the number of units placed by this objectDefID.*/
        public int rmGetNumberUnitsPlaced(int objectDefID) { return -1; }
	
	    /**Returns a unit ID that was placed by the objectDefID.*/
        public int rmGetUnitPlaced(int objectDefID, int index) { return -1; }

	    /**Returns the unit ID of a given player that was placed by the objectDefID.*/
        public int rmGetUnitPlacedOfPlayer(int objectDefID, int playerID) { return -1; }
	
	    /**Place object definition for the player at the given area's location.*/
        public void rmPlaceObjectDefAtAreaLoc(int defID, int playerID, int areaID, long placeCount) { }

	    /**Place object definition at specific location for given player.*/
        public void rmPlaceObjectDefAtLoc(int defID, int playerID, float xFraction, float zFraction, long placeCount) { }

	    /**Place object definition for the player at the location of a random area in the given class.*/
        public void rmPlaceObjectDefAtRandomAreaOfClass(int defID, int playerID, int classID, long placeCount) { }

	    /**Place object definition for the player in the given area.*/
        public void rmPlaceObjectDefInArea(int defID, int playerID, int areaID, long placeCount) { }

	    /**Place object definition for the player in a random area in the given class.*/
        public void rmPlaceObjectDefInRandomAreaOfClass(int defID, int playerID, int classID, long placeCount) { }

	    /**Place object definition per player.*/
        public void rmPlaceObjectDefPerPlayer(int defID, bool playerOwned, long placeCount) { }
	
	    /**If off, some objects placed will automatically convert to Mother Nature. (e.g. gold mines).*/
        public void rmSetIgnoreForceToGaia(bool val) { }
	

        ////////////// class Player //////////////
        // http://aomcodereference.googlecode.com/svn/trunk/src/aom/scripting/xs/rm/Player.java
        /**
	     * Adds to a player's resource amount.
	     * @param resourceName 
	     * <li>"food"</li>
	     * <li>"wood"</li>
	     * <li>"gold"</li>
	     * <li>"favor"</li>
	     */
        public void rmAddPlayerResource(int playerID, string resourceName, float amount) { }
	
	    /**Gets the number of players on the given team*/
        public int rmGetNumberPlayersOnTeam(int teamID) { return -1; }

	    /**
	     * Gets the civilization of the specified player.
	     * 
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivZeus
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivHades
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivPoseidon
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivIsis
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivRa
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivSet
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivLoki
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivThor
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivOdin
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivGaia
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivKronos
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivOuranos
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivGreek
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivNorse
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivEgyptian
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivAtlantean
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivRandom
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCivNature
	     */
        public int rmGetPlayerCiv(int playerID) { return -1; }

	    /**
	     * Gets the culture of the specified player.
	     * 
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCultureGreek
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCultureEgyptian
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCultureNorse
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCultureAtlantean
	     * @see aom.scripting.xs.ai.ArtificialIntelligence#cCultureNature
	     */
        public int rmGetPlayerCulture(int playerID) { return -1; }

	    /**Gets a player's nickname.*/
        public string rmGetPlayerName(int playerID) { return null; }

	    /**Gets the team the specified player is on.*/
        public int rmGetPlayerTeam(int playerID) { return -1; }

        /**
         * Sets one player location. You can use this to place players anywhere.
         * Once a player is placed, it won't be repositioned by any future calls to the various rmPlacePlayers functions.
         */
        public void rmPlacePlayer(int playerID, float xFraction, float zFraction) {
            players[playerID].Position = fracToMeters(new XVector2(xFraction, zFraction));
        }

	    /**
	     * Makes a circle of player locations. Places players in a circle. 
	     * Variation is determined by the difference between the min and max. 
	     * Angle variation determines whether players are equidistant or can be slightly closer or farther apart.
	     */
        public void rmPlacePlayersCircular(float minFraction, float maxFraction, float angleVariation) { }

	    /**
	     * Makes a line of player locations. Sometimes you will want players to be placed in a line.
	     * Anatolia places each team on a line, while Vinlandsaga places all players in a line. 
	     * Using a line placement is not easy because there may not be enough room for player areas or resources. 
	     * X and Z determine the starting and ending locations of the line.
	     * DistVariation determines how far from the line player areas can vary, and spacingVariation determines how much space there is among points along the line where players are placed.
	     */
        public void rmPlacePlayersLine(float x1, float z1, float x2, float z2, float distVariation, float spacingVariation) {
            XVector2 p1 = new XVector2(x1, z1), p2 = new XVector2(x2, z2);
            for (int p = 1; p < players.Count; ++p) {
                players[p].Position =
                    fracToMeters(XVector2.Lerp(p1, p2, (float)(p - 1) / (players.Count - 1)));
            }
        }

        /**
         * Makes a square of player locations. Places players in a square, which automatically adjusts to a rectangle for rectangular maps.
         * Unlike the circle, variance here is determined by a plus or minus (the distVariation) off of the mean distance. 
         * SpacingVariation determines whether players are equidistant or can be slightly closer or farther apart.
         */
        public void rmPlacePlayersSquare(float dist, float distVariation, float spacingVariationfloat) {
            XVector2 min = new XVector2(MapSizeX * dist/2, MapSizeZ * dist/2);
            XVector2 max = new XVector2(MapSizeX - min.X, MapSizeZ - min.Y);
            RMPolygon poly = new RMPolygon() {
                Points = new[] {
                    new XVector2(min.X, min.Y), new XVector2(min.X, max.Y),
                    new XVector2(max.X, max.Y), new XVector2(max.X, min.Y)
                }
            };
            float polyLen = poly.Length;
            float off = (float)rand.NextDouble();
            for (int p = 1; p < players.Count; ++p) {
                players[p].Position =
                    fracToMeters(tileToFrac(poly.GetPointAlongSurface(
                        ((p - 1 + off) / (players.Count - 1)) * polyLen)
                    ));
            }
        }

        /**Manually sets a player's starting location.*/
        public void rmSetPlayerLocation(int playerID, float xFraction, float zFraction) {
            players[playerID].Position = fracToMeters(new XVector2(xFraction, zFraction));
        }

	    /**Gets a player's start location x fraction.*/
        public float rmPlayerLocXFraction(int playerID) { return rmXMetersToFraction(players[playerID].Position.X); }

	    /**Gets a player's start location z fraction.*/
        public float rmPlayerLocZFraction(int playerID) { return rmXMetersToFraction(players[playerID].Position.Y); }
	
	    /**
	     * When placing players in a circle or square, this command allows you to skip part of the circle or square, 
	     * in essence removing a slice from the pie (maybe you want to fit an ocean in there like in Sea of Worms).
	     * The default for fromPercent is 0, and the default for toPercent is 1. That means use the whole circle or square. 
	     * You can pass in something like 0.25 and 0.50 to have the players placed from 25% in to 50% in along the circle or square.
	     * For circular placement, 0 is at 9h00, 0.25 is at 12h00, 0.5 is at 3h00, and 0.75 is at 6h00.
	     * For square placement (think of the square as a line that follows a square), 0 is at 6h00, 0.25 is at 9h00, 0.5 is at 12h00, and 0.75 is at 3h00.
	     */
        public void rmSetPlacementSection(float fromPercent, float toPercent) { }

	    /**
	     * Sets the team to place. Use this before calling the various rmPlacePlayers functions, 
	     * and only players on the specified team will get placed. Remember: the first teamID is 0, the second is 1, etc.
	     * Pass in -1 for the teamID to place all teams (or actually all players that haven't been placed yet).
	     */
        public void rmSetPlacementTeam(int teamID) { }

	    /**Sets a player's 'official' area.*/
        public void rmSetPlayerArea(int playerID, int areaID) {
            players[playerID].Area = areas[areaID];
        }

	    /** 
	     * Sets the area of the map to use for player placement.
	     * Use this command if, for example, you want to place players in one quadrant of a map.
	     */
        public void rmSetPlayerPlacementArea(float minX, float minZ, float maxX, float maxZ) { }

	    /**
	     * Sets a player's resource amount.
	     * @param resourceName 
	     * <li>"food"</li>
	     * <li>"wood"</li>
	     * <li>"gold"</li>
	     * <li>"favor"</li>
	     */
        public void rmSetPlayerResource(int playerID, string resourceName, float amount) { }
	
	    /**Sets a team's 'official' area.*/
        public void rmSetTeamArea(int teamID, int areaID) { }

	    /**
	     * Sets the team spacing modifier. Normally, all players are placed equidistant. 
	     * This command allows you to force team members closer together. 
	     * Values of 0.3-0.5 return the best results. 
	     * Values less than 0.25 may not provide enough space for starting resources.
	     */
        public void rmSetTeamSpacingModifier(float modifier) { }
	
	    /**
	     * Multiplies a player's resource amount by the given factor.
	     * @param resourceName 
	     * <li>"food"</li>
	     * <li>"wood"</li>
	     * <li>"gold"</li>
	     * <li>"favor"</li>
	     */
        public void rmMultiplyPlayerResource(int playerID, string resourceName, float factor) { }




        public XRandomMap(int seed, World _world) {
            cMapSize = 1;
            players.Add(new RMPlayer() { Name = "Gaia", Team = 0 });
            players.Add(new RMPlayer() { Name = "Player 1", Team = 1 });
            players.Add(new RMPlayer() { Name = "Player 2", Team = 2 });
            //cNumberPlayers = 10;
            //cNumberNonGaiaPlayers = 9;
            //cNumberTeams = 2;

            rand = new Random(seed);
            world = _world;
        }


    }
}
