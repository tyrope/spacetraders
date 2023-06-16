using System;
using System.Collections.Generic;
using System.Threading;

namespace STCommander
{
    public class ShipNavigation
    {
        public enum Status { IN_TRANSIT, IN_ORBIT, DOCKED };
        public enum FlightMode { DRIFT, STEALTH, CRUISE, BURN };
        public class Route
        {
            public Waypoint destination;
            public Waypoint departure;
            public string departureTime;
            public int departureTimestamp => UnityEngine.Mathf.RoundToInt((float) (DateTime.Parse(departureTime) - DateTime.UnixEpoch).TotalSeconds);
            public string arrival;
            public int arrivalTimestamp => UnityEngine.Mathf.RoundToInt((float) (DateTime.Parse(arrival) - DateTime.UnixEpoch).TotalSeconds);
            public string DestSymbol => destination?.symbol;
            public string DeptSymbol => departure?.symbol;
            internal DateTime ETA => DateTime.Parse(arrival);
            internal TimeSpan TotalFlightTime => ETA - DateTime.Parse(departureTime);
        }
        public string systemSymbol;
        public string waypointSymbol;
        public Route route;
        public Status status;
        public FlightMode flightMode;
        internal TimeSpan CurrentFlightTime {
            get {
                if(route.ETA - DateTime.UtcNow < TimeSpan.Zero) { return TimeSpan.Zero; } // Pre-flight.
                if(route.ETA - DateTime.UtcNow > route.TotalFlightTime) { // Post-flight.
                    return route.TotalFlightTime;
                }
                return route.ETA - DateTime.UtcNow;
            }
        }
        public float FractionFlightComplete {
            get {
                if(route.TotalFlightTime.Ticks == 0) { // Null distance.
                    return 1f;
                }
                return (float) (CurrentFlightTime / route.TotalFlightTime);
            }
        }

        public ShipNavigation( string shipSymbol ) {
            // This does an async thingy synchronously. Disgusting but hey ho.
            List<object> fields = DatabaseManager.instance.SelectQuery("SELECT systemSymbol,waypointSymbol,destination,departure,departureTime,arrival,status,flightMode "
                    + $"FROM ShipNav WHERE shipSymbol='{shipSymbol}' LIMIT 1;", System.Threading.CancellationToken.None).Result[0];
            systemSymbol = (string) fields[0];
            waypointSymbol = (string) fields[1];

            route = new Route();
            route.destination = Waypoint.GetWaypointFromSymbol((string) fields[2], CancellationToken.None).Result;
            route.departure = Waypoint.GetWaypointFromSymbol((string) fields[3], CancellationToken.None).Result;
            route.departureTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(fields[4])).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            route.arrival = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(fields[5])).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            status = Enum.Parse<Status>((string) fields[6]);
            flightMode = Enum.Parse<FlightMode>((string) fields[7]);
        }

        public override string ToString() {
            return status switch
            {
                Status.DOCKED => $"DOCKED @ {waypointSymbol}",
                Status.IN_ORBIT => $"ORBITING {waypointSymbol}",
                Status.IN_TRANSIT => $"{route.DeptSymbol}→{route.DestSymbol} ({route.ETA:HH:mm:ss})",
                _ => "ERR_INVALID_NAV_STATUS",
            };
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipNavigation() { }
    }
}