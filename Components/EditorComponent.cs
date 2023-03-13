using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Components
{
    public class EditorComponent : MonoBehaviour, IDisposable
    {
        private static EditorComponent instance;
        private GameWorld gameWorld;
        private Player player;
        private IBotGame botGame;

        private GUIContent guiContent;
        private GUIStyle guiStyle;

        // Used to only update the nearest zone periodically to avoid performance issues
        private float lastLocationUpdate;
        private float locationUpdateFrequency = 0.5f;

        private BotZone currentZone;
        private float distanceToZone;
        private NavMeshHit navMeshHit;

        // Dictionary is [zone][patrol]
        public Dictionary<string, Dictionary<string, CustomPatrolWay>> zoneWaypoints = new Dictionary<string, Dictionary<string, CustomPatrolWay>>();

        private EditorComponent()
        {
            // Empty
        }

        public static EditorComponent Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EditorComponent();
                }

                return instance;
            }
        }

        public static bool Instantiated
        {
            get
            {
                return instance != null;
            }
        }

        public void Dispose()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var editorComponentObject = gameWorld.GetComponent<EditorComponent>();
            Destroy(editorComponentObject);
            instance = null;
            GC.SuppressFinalize(this);
        }

        public void Awake()
        {
            // Setup access to game objects
            gameWorld = Singleton<GameWorld>.Instance;
            botGame = Singleton<IBotGame>.Instance;
            player = gameWorld.MainPlayer;
        }

        public void OnGUI()
        {
            if (Time.time > (lastLocationUpdate + locationUpdateFrequency))
            {
                UpdateLocation();
                lastLocationUpdate = Time.time;
            }

            // Setup GUI objects if they're not setup yet
            if (guiStyle == null)
            {
                guiStyle = new GUIStyle(GUI.skin.box);
                guiStyle.alignment = TextAnchor.MiddleRight;
                guiStyle.fontSize = 36;
                guiStyle.margin = new RectOffset(3, 3, 3, 3);
            }

            if (guiContent == null)
            {
                guiContent = new GUIContent();
            }

            // Build the data to show in the GUI
            string guiText = "Waypoint Editor\n";
            guiText += "-----------------------\n";

            guiText += $"Current Zone: {currentZone.NameZone}\n";
            guiText += $"Distance To Nearest Waypoint: {distanceToZone}\n";
            if (navMeshHit.hit)
            {
                guiText += $"Inside of Navmesh\n";
            }
            else
            {
                guiText += "Outside of Navmesh\n";
            }

            // Draw the GUI
            guiContent.text = guiText;
            Vector2 guiSize = guiStyle.CalcSize(guiContent);
            Rect guiRect = new Rect(
                Screen.width - guiSize.x - 5f,
                Screen.height - guiSize.y - 30f,
                guiSize.x,
                guiSize.y);

            GUI.Box(guiRect, guiContent, guiStyle);
        }

        public void Update()
        {
            if (Input.GetKeyDown(WaypointsPlugin.AddWaypointKey.Value.MainKey))
            {
                
            }
        }

        private void UpdateLocation()
        {
            Vector3 currentPosition = player.Position;
            currentZone = botGame.BotsController.GetClosestZone(currentPosition, out distanceToZone);

            NavMesh.SamplePosition(currentPosition, out navMeshHit, 10f, NavMesh.AllAreas);
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<EditorComponent>();
            }
        }

        public static void Disable()
        {
            if (Instantiated)
            {
                Instance.Dispose();
            }
        }
    }
}
