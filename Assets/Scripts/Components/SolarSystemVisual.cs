using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace STCommander
{
    public class SolarSystemVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public SolarSystem system;
        public GameObject[] models;
        public GameObject labelDeselected;
        public Transform labelSelected;

        private bool IsSelected;

        private readonly CancellationTokenSource AsyncCancelToken = new CancellationTokenSource();

        // Start is called before the first frame update
        private async void Start() {
            Transform visuals = transform.Find("Visuals");
            // Add always-on stuff.
            Instantiate(models[(int) system.type], visuals);
            gameObject.name = system.symbol;
            SetPosition();

            // Flat label (deselected) stuff.
            labelDeselected.GetComponentInChildren<TMP_Text>().text = system.symbol;

            // Upright label (selected) stuff.
            labelSelected.Find("Symbol").GetComponent<TMP_Text>().text = system.symbol;
            labelSelected.Find("Type").GetComponent<TMP_Text>().text = GetStarClass();
            labelSelected.Find("Faction").GetComponent<TMP_Text>().text = await GetSystemOwners();
        }

        void OnMouseDown() {
            Debug.Log("Selected system: " + system.symbol);
            MapManager.SelectSystem(system);
        }

        void OnDestroy() {
            AsyncCancelToken.Cancel();
        }

        void OnApplicationQuit() {
            OnDestroy();
        }

        public void ChangeSelect(bool isSelectedNow) {
            IsSelected = isSelectedNow;
            labelSelected.gameObject.SetActive(IsSelected);
            labelDeselected.SetActive(IsSelected == false);
            SetPosition();
        }

        public void SetPosition() {
            if(IsSelected) {
                gameObject.transform.position = new Vector3(0, 0.5f, 0);
                return;
            }

            // Anti-lagg.
            if(MapManager.GetZoom() > 2000 && !IsSelected) {
                labelDeselected.SetActive(false);
                transform.Find("Visuals").Find("HoloLight").gameObject.SetActive(false);
            } else if(MapManager.GetZoom() <= 2000 && !IsSelected) {
                labelDeselected.SetActive(true);
                transform.Find("Visuals").Find("HoloLight").gameObject.SetActive(true);
            }

            try {
                gameObject.transform.position = MapManager.GetWorldSpaceFromCoords(system.x, system.y);
            } catch(System.ArgumentOutOfRangeException) {
                // We're off the map? Just... stay where we are.
                Debug.LogError("SolarSystemVisual::SetPosition() -- Why are we setting the position of an off-map system?");
            }
        }

        private string GetStarClass() {
            int seed = 0;
            foreach(char c in system.symbol) {
                seed += c;
            }
            Random.InitState(seed);

            switch(system.type) {
                case SolarSystem.StarType.BLACK_HOLE:
                    return "Black Hole";
                case SolarSystem.StarType.BLUE_STAR:
                    return (Random.Range(0, 3)) switch
                    {
                        0 => "Class-B star",
                        1 => "Class-A star",
                        _ => "Class-F star",
                    };
                case SolarSystem.StarType.HYPERGIANT:
                    return "Class-O star";
                case SolarSystem.StarType.NEBULA:
                    return "Nebula";
                case SolarSystem.StarType.NEUTRON_STAR:
                    return (Random.Range(0, 2)) switch
                    {
                        0 => "Class I neutron",
                        _ => "Class II neutron",
                    };
                case SolarSystem.StarType.ORANGE_STAR:
                    return (Random.Range(0, 2)) switch
                    {
                        0 => "Class M star",
                        _ => "Class K star",
                    };
                case SolarSystem.StarType.RED_STAR:
                    return "Class-L star";
                case SolarSystem.StarType.UNSTABLE:
                    return "Class-Q anomaly";
                case SolarSystem.StarType.WHITE_DWARF:
                    return (Random.Range(0, 7)) switch
                    {
                        0 => "Class-DA star",
                        1 => "Class-DB star",
                        2 => "Class-DO star",
                        3 => "Class-DQ star",
                        4 => "Class-DZ star",
                        5 => "Class-DC star",
                        _ => "Class-DX star"
                    };
                case SolarSystem.StarType.YOUNG_STAR:
                    return "Class-G star";
                default:
                    throw new System.MissingFieldException("System visualising without known startype.");
            }
        }

        private async Task<string> GetSystemOwners() {
            List<string> owners = new List<string>();
            ServerResult res;
            if(system.factions == null) {
                (res, system) = await ServerManager.CachedRequest<SolarSystem>($"systems/{system.symbol}", new System.TimeSpan(1, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
                if(AsyncCancelToken.IsCancellationRequested) { return default; }
                if(res.result != ServerResult.ResultType.SUCCESS) {
                    // Who... owns... this?
                    Debug.LogError($"SolarSystemVisual:GetSystemOwners() - Didn't get a success for systems info refresh.");
                    return "ERR_OWNER_UNKNOWN";
                }
            }
            foreach(Faction f in system.factions) {
                if(f.name != null) {
                    owners.Add(f.name);
                } else {
                    Faction fac;
                    (res, fac) = await ServerManager.CachedRequest<Faction>($"factions/{f.symbol}", new System.TimeSpan(1, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
                    if(AsyncCancelToken.IsCancellationRequested) { return default; }
                    if(res.result != ServerResult.ResultType.SUCCESS) {
                        // Fall back to the symbol we know.
                        Debug.LogError($"SolarSystemVisual:GetSystemOwners() - Didn't get a success for faction {f.symbol}");
                        owners.Add(f.symbol);
                    }
                    owners.Add(fac.name);
                }
            }
            if(owners.Count > 0) {
                return "Claimed by: "+ string.Join(", ", owners);
            } else {
                return "Unclaimed";
            }
        }
    }
}
