using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;
using System.Xml.Linq;

namespace RTS4.Environment {
    public class TerrainType {
        public string Name;
        public XColor Diffuse;

        public static readonly TerrainType White =
            new TerrainType() { Name = "White", Diffuse = XColor.White };
        public static readonly TerrainType Black =
            new TerrainType() { Name = "Black", Diffuse = XColor.Black };
	    public static readonly TerrainType BlackRock;
	    public static readonly TerrainType CityTileA;
	    public static readonly TerrainType CityTileAtlantis;
	    public static readonly TerrainType CityTileAtlantiscoral;
	    public static readonly TerrainType CityTileWaterPool;
	    public static readonly TerrainType CityTileWaterEdgeA;
	    public static readonly TerrainType CityTileWaterEdgeB;
	    public static readonly TerrainType CityTileWaterEdgeEndA;
	    public static readonly TerrainType CityTileWaterEdgeEndB;
	    public static readonly TerrainType CityTileWaterEdgeEndC;
	    public static readonly TerrainType CityTileWaterEdgeEndD;
	    public static readonly TerrainType CityTileWaterCornerA;
	    public static readonly TerrainType CityTileWaterCornerB;
	    public static readonly TerrainType CityTileWaterCornerC;
	    public static readonly TerrainType CityTileWaterCornerD;
	    public static readonly TerrainType CliffA;
	    public static readonly TerrainType CliffGreekA;
	    public static readonly TerrainType CliffGreekB;
	    public static readonly TerrainType CliffEgyptianA;
	    public static readonly TerrainType CliffEgyptianB;
	    public static readonly TerrainType CliffNorseA;
	    public static readonly TerrainType CliffNorseB;
	    public static readonly TerrainType coralA;
	    public static readonly TerrainType coralB;
	    public static readonly TerrainType coralC;
	    public static readonly TerrainType coralD;
	    public static readonly TerrainType coralE;
	    public static readonly TerrainType coralF;
	    public static readonly TerrainType DirtA;
	    public static readonly TerrainType EgyptianRoadA;
	    public static readonly TerrainType ForestfloorDeadPine;
	    public static readonly TerrainType ForestfloorOak;
	    public static readonly TerrainType ForestfloorGaia;
	    public static readonly TerrainType ForestfloorMarsh;
	    public static readonly TerrainType ForestfloorPalm;
	    public static readonly TerrainType ForestfloorPine;
	    public static readonly TerrainType ForestfloorPineSnow;
	    public static readonly TerrainType ForestfloorSavannah;
	    public static readonly TerrainType ForestfloorTundra;
	    public static readonly TerrainType GaiaCreepA;
	    public static readonly TerrainType GaiaCreepASand;
	    public static readonly TerrainType GaiaCreepASnow;
	    public static readonly TerrainType GaiaCreepB;
	    public static readonly TerrainType GaiaCreepBorder;
	    public static readonly TerrainType GaiaCreepBorderSand;
	    public static readonly TerrainType GaiaCreepBorderSnow;
        public static readonly TerrainType GrassA =
            new TerrainType() { Name = "GrassA", Diffuse = XColor.Green };
        public static readonly TerrainType GrassB =
            new TerrainType() { Name = "GrassB", Diffuse = XColor.Green.Modify(0.5f) };
        public static readonly TerrainType GrassDirt25 =
            new TerrainType() { Name = "GrassDirt25", Diffuse = XColor.Lerp(XColor.Green, XColor.Brown, 0.25f) };
        public static readonly TerrainType GrassDirt50 =
            new TerrainType() { Name = "GrassDirt50", Diffuse = XColor.Lerp(XColor.Green, XColor.Brown, 0.50f) };
        public static readonly TerrainType GrassDirt75 =
            new TerrainType() { Name = "GrassDirt75", Diffuse = XColor.Lerp(XColor.Green, XColor.Brown, 0.75f) };
	    public static readonly TerrainType GreekRoad_BurntA;
	    public static readonly TerrainType GreekRoad_BurntB;
        public static readonly TerrainType GreekRoadA =
            new TerrainType() { Name = "GreekRoadA", Diffuse = XColor.Lerp(XColor.LightGray, XColor.Brown, 0.3f) };
        public static readonly TerrainType Hades1 =
            new TerrainType() { Name = "Hades1", Diffuse = XColor.Gray.Modify(0.6f) };
        public static readonly TerrainType Hades2 =
            new TerrainType() { Name = "Hades2", Diffuse = XColor.Gray.Modify(0.2f) };
	    public static readonly TerrainType Hades3 =
            new TerrainType() { Name = "Hades3", Diffuse = XColor.Red.Modify(0.5f) };
        public static readonly TerrainType Hades4 =
            new TerrainType() { Name = "Hades4", Diffuse = XColor.Red.Modify(0.75f) };
        public static readonly TerrainType Hades5 =
            new TerrainType() { Name = "Hades5", Diffuse = XColor.Red.Modify(1) };
	    public static readonly TerrainType Hades6;
	    public static readonly TerrainType Hades7;
	    public static readonly TerrainType Hades8;
	    public static readonly TerrainType Hades9;
	    public static readonly TerrainType HadesCliff;
	    public static readonly TerrainType Hadesbuildable1;
	    public static readonly TerrainType Hadesbuildable2;
        public static readonly TerrainType IceA =
            new TerrainType() { Name = "IceA", Diffuse = XColor.White };
        public static readonly TerrainType IceB =
            new TerrainType() { Name = "IceB", Diffuse = XColor.White };
	    public static readonly TerrainType IceC;
	    public static readonly TerrainType MarshA;
	    public static readonly TerrainType MarshB;
	    public static readonly TerrainType MarshC;
	    public static readonly TerrainType MarshD;
	    public static readonly TerrainType MarshE;
	    public static readonly TerrainType MarshF;
	    public static readonly TerrainType MiningGround;
	    public static readonly TerrainType NorseRoadA;
	    public static readonly TerrainType OlympusA;
	    public static readonly TerrainType OlympusB;
	    public static readonly TerrainType OlympusC;
	    public static readonly TerrainType OlympusTile;
	    public static readonly TerrainType RiverSandyA;
	    public static readonly TerrainType RiverSandyB;
	    public static readonly TerrainType RiverSandyC;
	    public static readonly TerrainType RiverSandyShallowA;
	    public static readonly TerrainType RiverGrassyA;
	    public static readonly TerrainType RiverGrassyB;
	    public static readonly TerrainType RiverGrassyC;
	    public static readonly TerrainType RiverIcyA;
	    public static readonly TerrainType RiverIcyB;
	    public static readonly TerrainType RiverIcyC;
	    public static readonly TerrainType RiverMarshA;
	    public static readonly TerrainType RiverMarshB;
	    public static readonly TerrainType RiverMarshC;
        public static readonly TerrainType SandA =
            new TerrainType() { Name = "SandA", Diffuse = XColor.Yellow };
	    public static readonly TerrainType SandB;
	    public static readonly TerrainType SandC;
	    public static readonly TerrainType SandD;
	    public static readonly TerrainType SandDirt50;
	    public static readonly TerrainType SandDirt50b;
	    public static readonly TerrainType SavannahA;
	    public static readonly TerrainType SavannahB;
	    public static readonly TerrainType SavannahC;
	    public static readonly TerrainType SavannahD;
	    public static readonly TerrainType ShorelineAegeanA;
	    public static readonly TerrainType ShorelineAegeanB;
	    public static readonly TerrainType ShorelineAegeanC;
	    public static readonly TerrainType ShorelineAtlanticA;
	    public static readonly TerrainType ShorelineAtlanticB;
	    public static readonly TerrainType ShorelineAtlanticC;
	    public static readonly TerrainType ShorelineMediterraneanA;
	    public static readonly TerrainType ShorelineMediterraneanB;
	    public static readonly TerrainType ShorelineMediterraneanC;
	    public static readonly TerrainType ShorelineMediterraneanD;
	    public static readonly TerrainType ShorelineNorwegianA;
	    public static readonly TerrainType ShorelineNorwegianB;
	    public static readonly TerrainType ShorelineNorwegianC;
	    public static readonly TerrainType ShorelineRedSeaA;
	    public static readonly TerrainType ShorelineRedSeaB;
	    public static readonly TerrainType ShorelineRedSeaC;
	    public static readonly TerrainType ShorelineSandA;
	    public static readonly TerrainType ShorelineTundraA;
	    public static readonly TerrainType ShorelineTundraB;
	    public static readonly TerrainType ShorelineTundraC;
	    public static readonly TerrainType ShorelineTundraD;
	    public static readonly TerrainType SnowA;
	    public static readonly TerrainType SnowB;
	    public static readonly TerrainType SnowGrass25;
	    public static readonly TerrainType SnowGrass50;
	    public static readonly TerrainType SnowGrass75;
	    public static readonly TerrainType SnowSand25;
	    public static readonly TerrainType SnowSand50;
	    public static readonly TerrainType SnowSand75;
	    public static readonly TerrainType TundraGrassA;
	    public static readonly TerrainType TundraGrassB;
	    public static readonly TerrainType TundraRockA;
	    public static readonly TerrainType TundraRockB;
	    public static readonly TerrainType UnderwaterIceA;
	    public static readonly TerrainType UnderwaterIceB;
	    public static readonly TerrainType UnderwaterIceC;
	    public static readonly TerrainType UnderwaterRockA;
	    public static readonly TerrainType UnderwaterRockB;
	    public static readonly TerrainType UnderwaterRockC;
	    public static readonly TerrainType UnderwaterRockD;
	    public static readonly TerrainType UnderwaterRockE;
        public static readonly TerrainType UnderwaterRockF;
	    public static readonly TerrainType water;


    }
}
