using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

namespace STCommander
{
    public class WaypointVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public GameObject[] models;
        public GameObject DeselectedLabel;
        public Transform SelectedLabel;
        public Waypoint waypoint;

        private Transform parentOrbit = null;
        private int SatelliteIndex = 0;
        private string WaypointSymbolEnd => waypoint.symbol.Split('-')[2];

        private float OrbitalAltitude;
        private float OrbitTime;
        private bool IsSelected = false;
        private readonly CancellationTokenSource AsyncCancelToken = new CancellationTokenSource();

        // Start is called before the first frame update
        void Start() {
            Instantiate(models[(int) waypoint.type], transform.Find("Visuals"));
            gameObject.name = waypoint.symbol;
            SetLabelInfo();

            // Are we orbiting the selected object, or another waypoint?
            SetParentOrbit();

            float nowInSeconds = (float) DateTime.Now.Subtract(DateTime.MinValue).TotalSeconds;

            if(parentOrbit == null) {
                OrbitalAltitude = new Vector2(waypoint.x, waypoint.y).magnitude;
            } else {
                OrbitalAltitude = 3f + SatelliteIndex;
            }
            OrbitTime = nowInSeconds % GetOrbitalPeriod();
            SetPosition();
        }

        void OnMouseDown() {
            Debug.Log("Selected waypoint: " + waypoint.symbol);
            MapManager.SelectWaypoint(waypoint);
        }

        private void Update() {
            OrbitTime += Time.deltaTime;
            if(OrbitTime >= GetOrbitalPeriod()) { OrbitTime %= GetOrbitalPeriod(); }
            SetPosition();
        }

        private async void SetLabelInfo() {
            // Deselected
            DeselectedLabel.GetComponentInChildren<TMP_Text>().text = WaypointSymbolEnd;

            // Selected
            ServerResult res;
            string tempValue;

            Transform gen = SelectedLabel.Find("General");
            gen.Find("Symbol").GetComponent<TMP_Text>().text = WaypointSymbolEnd;
            tempValue = waypoint.type.ToString();
            gen.Find("Type").GetComponent<TMP_Text>().text = tempValue[0].ToString() + tempValue.Replace('_', ' ').Substring(1).ToLower();
            if(waypoint.faction == null) {
                tempValue = "Unclaimed";
            } else if(waypoint.faction.name != null) {
                tempValue = "Claimed by:\n" + waypoint.faction.name;
            } else {
                Faction f;
                (res, f) = await ServerManager.CachedRequest<Faction>($"factions/{waypoint.faction.symbol}", new TimeSpan(1, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
                if(res.result != ServerResult.ResultType.SUCCESS) {
                    Debug.LogError("WaypointVisual:SetLabelInfo() - Failed to fetch claimant info. Display symbol instead of name.");
                    tempValue = "Claimed by:\n" + waypoint.faction.symbol;
                } else {
                    waypoint.faction = f;
                    tempValue = "Claimed by:\n" + f.name;
                }
            }
            gen.Find("Faction").GetComponent<TMP_Text>().text = tempValue;

            if(waypoint.chart == null) {
                tempValue = "Uncharted";
            } else {
                if(waypoint.chart.submittedBy == waypoint.faction.symbol) {
                    // Easy!
                    tempValue = "Charted by:\n" + waypoint.faction.name;
                } else {
                    // We gotta grab the Faction info.
                    List<Faction> lf;
                    (res, lf) = await ServerManager.CachedRequest<List<Faction>>($"factions", new TimeSpan(1, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
                    if(res.result != ServerResult.ResultType.SUCCESS) {
                        Debug.LogError($"WaypointVisual:SetLabelInfo() - Failed to fetch charter info. Display symbol ({waypoint.chart.submittedBy}) instead of name.");
                        tempValue = "Charted by:\n" + waypoint.faction.symbol;
                    } else {
                        tempValue = null;
                        foreach(Faction f in lf) {
                            if(f.symbol == waypoint.chart.submittedBy) {
                                tempValue = "Charted by:\n" + f.name;
                                break;
                            }
                        }
                        tempValue = tempValue != null ? tempValue : $"Charted by:\n{waypoint.chart.submittedBy}";
                    }
                }
            }
            gen.Find("Chart").GetComponent<TMP_Text>().text = tempValue;

            List<string> traits = new List<string>();
            if(waypoint.traits == null) {
                // Fetch!
                Waypoint wp;
                (res, wp) = await ServerManager.CachedRequest<Waypoint>($"systems/{waypoint.systemSymbol}/waypoints/{waypoint.symbol}", new TimeSpan(1, 0, 0), RequestMethod.GET, AsyncCancelToken);
                if(res.result != ServerResult.ResultType.SUCCESS) {
                    Debug.LogError("WaypointVisual:SetLabelInfo() - Failed to fetch Waypoint info. No traits can be displayed.");
                } else {
                    waypoint = wp;
                }
            }

            foreach(Trait t in waypoint.traits) {
                traits.Add(t.name);
            }
            SelectedLabel.Find("Traits").GetComponent<TMP_Text>().text = string.Join('\n', traits);
        }

        public void SetSelected( bool select ) {
            IsSelected = select;
            DeselectedLabel.SetActive(IsSelected == false);
            SelectedLabel.gameObject.SetActive(IsSelected);
        }

        private void SetParentOrbit() {
            if(MapManager.SelectedSystem != null) {
                // Main star is the middle point.
                foreach(Waypoint wp in MapManager.SelectedSystem.waypoints) {
                    if(wp.orbitals != null) {
                        for(int i = 0; i < wp.orbitals.Length; i++) {
                            if(wp.orbitals[i].symbol == waypoint.symbol) {
                                parentOrbit = transform.parent.Find(wp.symbol);
                                SatelliteIndex = i;
                                return;
                            }
                        }
                    }
                }
            } else {
                // A waypoint is the middle point.
                if(MapManager.SelectedWaypoint == waypoint) {
                    return; // We are the middle point.
                }
                Waypoint.Orbital o;
                for(int i = 0; i < MapManager.SelectedWaypoint.orbitals.Length; i++) {
                    o = MapManager.SelectedWaypoint.orbitals[i];
                    if(o.symbol == waypoint.symbol) {
                        parentOrbit = transform.parent.Find(MapManager.SelectedWaypoint.symbol);
                        SatelliteIndex = i;
                        break;
                    }
                }
            }
        }

        private void SetPosition() {
            if(IsSelected) {
                return; // We're the middle point; don't orbit.
            }
            float rot = (OrbitTime / GetOrbitalPeriod()) * 360f;
            float scaledAltitude = OrbitalAltitude * MapManager.GetMapScale();
            if(parentOrbit == null) {
                // We're orbiting the main star; align around 0.
                if(float.IsNaN(scaledAltitude) || float.IsNaN(rot)) {
                    Debug.LogError($"Trying to set a position of a waypoint to NaN.\n{waypoint.symbol} Time: {OrbitTime}/{GetOrbitalPeriod()} - Alt: {OrbitalAltitude} * {MapManager.GetMapScale()}.");
                    return;
                }
                transform.position = new Vector3(scaledAltitude * Mathf.Sin(rot), 0, scaledAltitude * Mathf.Cos(rot));
            } else {
                // We're orbiting another body; align around it.
                transform.position = parentOrbit.position +
                    new Vector3(scaledAltitude * Mathf.Sin(rot), 0, scaledAltitude * Mathf.Cos(rot));
            }
        }
        private float GetOrbitalPeriod() {
            return Mathf.Sqrt(4 * Mathf.Pow(Mathf.PI, 2) * Mathf.Pow(OrbitalAltitude * MapManager.GetMapScale(), 3) / 6.67430e-11f) / (parentOrbit == null ? 500f : 250f);
        }
    }
}
