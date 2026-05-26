using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace BetterGps
{
    // Tells Space Engineers to load this class automatically with the session
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class BetterGpsMod : MySessionComponentBase
    {
        private Service service;
        private Chat chat;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            // Code runs when the world loads
            service = new Service();
            chat = new Chat(service);

            MyAPIGateway.Utilities.MessageEntered += chat.Handle;
            MyAPIGateway.Utilities.ShowMessage("BetterGps", "Mod Loaded Successfully!");
        }

        public override void UpdateAfterSimulation()
        {
            // Code runs every single frame (60 times per second)
        }

        protected override void UnloadData()
        {
            // Clean up resources here when exiting to main menu
            MyAPIGateway.Utilities.MessageEntered -= chat.Handle;
        }
    }

    public class Chat
    {
        private class LastSearch
        {
            public string[] query;
            public Distance distance;
        }

        private Service service;
        private LastSearch lastSearch = new LastSearch();

        public void Handle(string message, ref bool sendToOthers)
        {
            if (!message.StartsWith("/bgps"))
            {
                return;
            }

            // Drop prefix and uppercase everything
            string[] args = message.Split(' ').Skip(1).Select(e => e.ToUpper()).ToArray();
            ChatCommand command = GetEnumValue<ChatCommand>(args[0]);
            if (command == null) // TODO: Validate this
            {
                return;
            }

            // Drop command
            args = args.Skip(1).ToArray();

            // Execute command
            switch (command)
            {
                case ChatCommand.HELP:
                {
                    service.Help();
                    break;
                }
                case ChatCommand.SEARCH:
                {
                    Search(args);
                    break;
                }
                case ChatCommand.COLOR:
                {
                    Color(args);
                    break;
                }
                case ChatCommand.SHOW:
                {
                    Show(args);
                    break;
                }
            }
            ;
        }

        public void Search(string[] messageArgs)
        {
            if (messageArgs.Length != 2)
            {
                return;
            }

            Distance distance = GetEnumValue<Distance>(messageArgs[0]);
            string[] query = parseCsv(messageArgs[1]);
            lastSearch.distance = distance;
            lastSearch.query = query;

            service.Search(query, distance);
        }

        public void Color(string[] messageArgs)
        {
            if (messageArgs.Length != 2)
            {
                return;
            }

            string[] query = parseCsv(messageArgs[1]);
            Color color = GetEnumValue<Color>(messageArgs[0]);

            service.Color(query, color);
        }

        public void Show(string[] messageArgs)
        {
            if (messageArgs.Length != 1)
            {
                return;
            }

            Toggle value = GetEnumValue<Toggle>(messageArgs[0]);

            service.Show(value, lastSearch.distance, lastSearch.query);
        }

        private string[] parseCsv(string value)
        {
            var splitValues = value.Split(',');
            return Array.FindAll(splitValues, e => e != "");
        }

        private T GetEnumValue<T>(string data)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), data, ignoreCase: true);
            }
            catch
            {
                return default(T);
            }
        }

        public Chat(Service service)
        {
            this.service = service;
        }
    }

    public class Service
    {
        public void Help()
        {
            SendPersonalMessage(
                "\n/bgps help\n"
                    + "    Shows this help screen\n"
                    + "/bgps search {near/far/all} {comma separated search}\n"
                    + "    search through your gps markers and show the nearest, farthest, or all that match the list of values provided.\n"
                    + "/bgps show {on/off}\n"
                    + "    Toggle on or off the current markers\n"
                    + "/bgps color {comma separated search} {red/orange/yellow/green/blue/indigo/violet}\n"
                    + "    Set a query of markers to one of the predefined colors"
            );
        }

        public void Search(string[] queries, Distance range)
        {
            var queryMarker = new Dictionary<string, IMyGps>();
            var queryDistance = new Dictionary<string, double>();

            foreach (var marker in GetGpsMarkers())
            {
                SetShowOnHud(marker, false);
                double nextDistance = GetDistance(marker);

                foreach (var query in queries)
                {
                    // Ignore non-matching markers
                    if (!DoesMatchQuery(marker, query))
                    {
                        continue;
                    }

                    // Add any missing query keys to track results
                    if (!queryMarker.ContainsKey(query))
                    {
                        queryMarker.Add(query, marker);
                        queryDistance.Add(query, nextDistance);
                    }

                    // Track which markders match the search
                    double currDistance = queryDistance[query];
                    switch (range)
                    {
                        case Distance.NEAR:
                            if (nextDistance <= currDistance)
                            {
                                queryDistance[query] = nextDistance;
                                queryMarker[query] = marker;
                            }
                            continue;
                        case Distance.FAR:
                            if (nextDistance >= currDistance)
                            {
                                queryDistance[query] = nextDistance;
                                queryMarker[query] = marker;
                            }
                            continue;
                        case Distance.ALL:
                            SetShowOnHud(marker, true);
                            continue;
                    }
                }
            }

            if (range == Distance.ALL)
            {
                // Matches already set to true
                return;
            }

            foreach (IMyGps marker in queryMarker.Values)
            {
                SetShowOnHud(marker, true);
            }
        }

        public void Show(Toggle value, Distance lastSerachDistance, string[] lastSearchArgs)
        {
            switch (value)
            {
                case Toggle.ON:
                    if (lastSearchArgs == null || lastSerachDistance == null)
                    {
                        return;
                    }
                    Search(lastSearchArgs, lastSerachDistance);
                    break;
                case Toggle.OFF:
                    foreach (var marker in GetGpsMarkers())
                    {
                        SetShowOnHud(marker, false);
                    }
                    break;
            }
        }

        public void Color(string[] query, Color color) { }

        private bool DoesMatchQuery(IMyGps marker, string query)
        {
            string fullString = marker.Name + marker.Description;
            return fullString.ToUpper().Contains(query);
        }

        private double GetDistance(IMyGps marker)
        {
            return Math.Round(
                Vector3D.Distance(
                    MyAPIGateway.Session.LocalHumanPlayer.GetPosition(),
                    marker.Coords
                ),
                2
            );
        }

        private void SetShowOnHud(IMyGps marker, bool value)
        {
            if (marker.ShowOnHud == value)
            {
                return;
            }

            marker.ShowOnHud = value;
            MyAPIGateway.Session.GPS.ModifyGps(
                MyAPIGateway.Session.LocalHumanPlayer.IdentityId,
                marker
            );
        }

        private List<IMyGps> GetGpsMarkers()
        {
            List<IMyGps> gpsMarkers = new List<IMyGps>();
            MyAPIGateway.Session.GPS.GetGpsList(
                MyAPIGateway.Session.LocalHumanPlayer.IdentityId,
                gpsMarkers
            );
            return gpsMarkers;
        }

        private void SendPersonalMessage(string message)
        {
            MyVisualScriptLogicProvider.SendChatMessage(
                message,
                "Better GPS",
                MyAPIGateway.Session.LocalHumanPlayer.IdentityId
            );
        }
    }

    public enum ChatCommand
    {
        HELP,
        SEARCH,
        COLOR,
        SHOW,
    }

    public enum Distance
    {
        NEAR,
        FAR,
        ALL,
    }

    public enum Toggle
    {
        OFF,
        ON,
    }

    public enum Color
    {
        RED,
        ORANGE,
        YELLOW,
        GREEN,
        BLUE,
        INDIGO,
        VIOLET,
    }
}
