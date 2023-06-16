using System.Threading;

namespace STCommander
{
    public class ShipRequirements
    {
        private int _rowid;
        public int Rowid {
            get {
                if(_rowid < 1) {
                    SaveRowId();
                }
                return _rowid;
            }
        }
        public int Power { get; private set; }
        public int Crew { get; private set; }
        public int Slots { get; private set; }

        public ShipRequirements( int pwr, int crw, int slts, int rowid = -1 ) {
            Power = pwr;
            Crew = crw;
            Slots = slts;
            _rowid = rowid;
        }

        public void SaveRowId() {
            if(_rowid < 0) { return; } // No need to save.

            // ShipRequirements: power (INTEGER), crew (INTEGER), slots (INTEGER)
            int rows = DatabaseManager.instance.WriteQuery($"INSERT INTO ShipRequirements (power, crew, slots) VALUES ({Power},{Crew},{Slots});", CancellationToken.None).Result;
            if(rows == 1) {
                _rowid = DatabaseManager.instance.GetLatestRowid(CancellationToken.None).Result;
            } else {
                UnityEngine.Debug.LogError("Failed to insert data");
            }
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipRequirements() { }
    }
}
