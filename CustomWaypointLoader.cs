using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DrakiaXYZ.Waypoints
{
    public class CustomWaypointLoader
    {
        // Singleton because I'm lazy
        private static CustomWaypointLoader instance = new CustomWaypointLoader();
        public static CustomWaypointLoader Instance { get { return instance; } }

        // The dictionary is [map][zone][patrol]
        public Dictionary<string, Dictionary<string, Dictionary<string, CustomPatrolWay>>> mapZoneWaypoints = new Dictionary<string, Dictionary<string, Dictionary<string, CustomPatrolWay>>>();

        public void loadData()
        {
            string customFolder = $"BepInEx\\plugins\\DrakiaXYZ-Waypoints\\custom\\";
            
            // Loop through all subfolders, and load data, assuming the filename is the map name
            foreach (String directory in Directory.GetDirectories(customFolder))
            {
                foreach (String file in Directory.GetFiles(directory, "*.json"))
                {
                    string mapName = getMapFromFilename(file);
                    Console.WriteLine($"Loading waypoints for {mapName}");
                    loadMapData(mapName, file);
                }
            }

            // This is meant for creation purposes only, we'll loop through all files in the "custom" folder, and 
            // strip anything after an underscore. This allows us to create "[mapname]_[date].json" files automatically
            foreach (String file in Directory.GetFiles(customFolder, "*.json"))
            {
                string mapName = getMapFromFilename(file);
                Console.WriteLine($"Loading development waypoints for {mapName}");
                loadMapData(mapName, file);
            }
        }

        private void loadMapData(string mapName, string file)
        {
            if (!mapZoneWaypoints.ContainsKey(mapName))
            {
                mapZoneWaypoints[mapName] = new Dictionary<string, Dictionary<string, CustomPatrolWay>>();
            }

            // We have to manually merge in our data, so multiple people can add waypoints to the same patrols
            Dictionary<string, Dictionary<string, CustomPatrolWay>> zoneWaypoints = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, CustomPatrolWay>>>(File.ReadAllText(file));
            foreach (string zoneName in zoneWaypoints.Keys)
            {
                // If the map already has this zone, merge in the patrols
                if (mapZoneWaypoints[mapName].ContainsKey(zoneName))
                {
                    foreach (string patrolName in zoneWaypoints[zoneName].Keys)
                    {
                        // If the patrol already exists, merge in the waypoints
                        if (mapZoneWaypoints[mapName][zoneName].ContainsKey(patrolName))
                        {
                            CustomPatrolWay existingPatrol = mapZoneWaypoints[mapName][zoneName][patrolName];
                            CustomPatrolWay newPatrol = zoneWaypoints[zoneName][patrolName];
                            // TODO: What do we do about mis-matched patrol data? Should we allow overrriding it? Who wins in the event of a conflict?
                            //       For now, we'll go with "Last to load wins"
                            existingPatrol.waypoints.AddRange(newPatrol.waypoints);
                            existingPatrol.blockRoles = newPatrol.blockRoles ?? existingPatrol.blockRoles;
                            existingPatrol.maxPersons = newPatrol.maxPersons ?? existingPatrol.maxPersons;
                            existingPatrol.patrolType = newPatrol.patrolType ?? existingPatrol.patrolType;
                        }
                        // If the patrol doesn't exist, copy the whole thing over
                        else
                        {
                            mapZoneWaypoints[mapName][zoneName][patrolName] = zoneWaypoints[zoneName][patrolName];
                        }
                    }
                }
                // If the zoneName key doesn't exist yet, we can just throw the whole thing in
                else
                {
                    mapZoneWaypoints[mapName][zoneName] = zoneWaypoints[zoneName];
                }
            }
        }

        public Dictionary<string, CustomPatrolWay> getMapZonePatrols(string map, string zone)
        {
            if (!mapZoneWaypoints.ContainsKey(map))
            {
                return null;
            }

            if (!mapZoneWaypoints[map].ContainsKey(zone))
            {
                return null;
            }

            return mapZoneWaypoints[map][zone];
        }

        private string getMapFromFilename(string file)
        {
            string fileWithoutExt = file.Substring(0, file.LastIndexOf('.'));
            string mapName = fileWithoutExt.Substring(fileWithoutExt.LastIndexOf('\\') + 1);
            if (mapName.Contains("_"))
            {
                mapName = mapName.Substring(0, mapName.IndexOf("_"));
            }
            return mapName;
        }
    }
}
