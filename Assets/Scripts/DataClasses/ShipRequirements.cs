namespace STCommander
{
    public class ShipRequirements
    {
        public int Power { get; private set; }
        public int Crew { get; private set; }
        public int Slots { get; private set; }

        public ShipRequirements( int pwr, int crw, int slts ) {
            Power = pwr;
            Crew = crw;
            Slots = slts;
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipRequirements() { }
    }
}