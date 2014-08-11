using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RTS4.Common;

namespace RTS4.Environment.Resources {
    class Content {

        static List<TerrainType> terrainTypes = new List<TerrainType>();
        static List<WaterType> waterTypes = new List<WaterType>();

        static Content() {
            try {
                XDocument terrDoc = XDocument.Load("Content/terraintypes.xml");
                foreach (var el in terrDoc.Element("terraintypes").Elements("type")) {
                    string name = el.Attribute("name").Value;
                    XColor diffuse = XColor.Parse(el.Attribute("colour").Value);
                    terrainTypes.Add(new TerrainType() { Name = name, Diffuse = diffuse });
                }
                XDocument waterDoc = XDocument.Load("Content/watertypes.xml");
                foreach (var el in waterDoc.Element("watertypes").Elements("type")) {
                    string name = el.Attribute("name").Value;
                    XAttribute edgeXml = el.Attribute("edge"), bottomXml = el.Attribute("bottom"), depthXml = el.Attribute("depth");
                    waterTypes.Add(new WaterType() {
                        Name = name,
                        EdgeTerrain = TerrainFromString(edgeXml.Value),
                        BottomTerrain = TerrainFromString(bottomXml.Value),
                    });
                }
            } catch { }
        }

        public static TerrainType TerrainFromString(string terrain) {
            // Get it from the XML data loaded
            for (int t = 0; t < terrainTypes.Count; ++t) {
                if (terrainTypes[t].Name == terrain) return terrainTypes[t];
            }

            // Try to reflect it
            var typeVar = typeof(TerrainType).GetField(terrain.Replace(" ", ""));
            if (typeVar != null)
                return typeVar.GetValue(null) as TerrainType;

            // Unable to find it, print an error
            Console.WriteLine("Unable to find terrain type " + terrain);
            return null;
        }

        public static WaterType WaterFromString(string terrain) {
            // Get it from the XML data loaded
            for (int w = 0; w < waterTypes.Count; ++w) {
                if (waterTypes[w].Name == terrain) return waterTypes[w];
            }

            // Unable to find it, print an error
            Console.WriteLine("Unable to find terrain type " + terrain);
            return null;
        }

    }
}
