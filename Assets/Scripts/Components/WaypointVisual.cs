using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

namespace STCommander
{
    public class WaypointVisual : OrbitalVisual
    {
        public GameObject[] models;
        public GameObject DeselectedLabel;
        public Transform SelectedLabel;
        public Waypoint waypoint;

        private Transform parentOrbit = null;
        private int SatelliteIndex = 0;
        private string WaypointSymbolEnd => waypoint.symbol.Split('-')[2];

        private bool IsSelected = false;
        private readonly CancellationTokenSource AsyncCancelToken = new CancellationTokenSource();

        protected override float OrbitalPeriod => parentOrbit == null ? base.OrbitalPeriod : base.OrbitalPeriod * 2f;

        // Start is called before the first frame update
        protected override void Start() {
            gameObject.name = waypoint.symbol;
            Instantiate(models[(int) waypoint.type], transform.Find("Visuals"));
            SetLabelInfo();

            // Are we orbiting the selected object, or another waypoint?
            SetParentOrbit();
            if(parentOrbit == null) {
                OrbitalAltitude = new Vector2(waypoint.x, waypoint.y).magnitude;
            } else {
                OrbitalAltitude = 3f + SatelliteIndex;
            }
            base.Start();
            SetPosition();
        }

        void OnMouseDown() {
            Debug.Log("Selected waypoint: " + waypoint.symbol);
            mapManager.SelectWaypoint(waypoint);
        }

        protected override void Update() {
            base.Update();
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
                    (res, lf) = await ServerManager.CachedRequest<List<Faction>>($"factions?limit=20", new TimeSpan(1, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
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
                        tempValue ??= $"Charted by:\n{waypoint.chart.submittedBy}";
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
            if(mapManager.SelectedSystem != null) {
                // Main star is the middle point.
                foreach(Waypoint wp in mapManager.SelectedSystem.waypoints) {
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
                if(mapManager.SelectedWaypoint == waypoint) {
                    return; // We are the middle point.
                }
                Waypoint.Orbital o;
                for(int i = 0; i < mapManager.SelectedWaypoint.orbitals.Length; i++) {
                    o = mapManager.SelectedWaypoint.orbitals[i];
                    if(o.symbol == waypoint.symbol) {
                        parentOrbit = transform.parent.Find(mapManager.SelectedWaypoint.symbol);
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

            if(parentOrbit == null) {
                // We're orbiting the main star; align around 0.
                if(float.IsNaN(OrbitalAltitude) || float.IsNaN(OrbitalAngle)) {
                    Debug.LogError($"Trying to set a position of a waypoint to NaN.\n{waypoint.symbol} Time: {OrbitTime}/{OrbitalPeriod} - Alt: {OrbitalAltitude}.");
                    return;
                }
                transform.position = OrbitalPosition;
            } else {
                // We're orbiting another body; align around it.
                transform.position = parentOrbit.position + OrbitalPosition;
            }
        }
    }
}
