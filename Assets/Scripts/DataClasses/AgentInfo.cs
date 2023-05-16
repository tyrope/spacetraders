namespace SpaceTraders
{
    public class AgentInfo
    {
        public class Data {

            public string accountId;
            public string symbol;
            public string headquarters;
            public int credits;
        }
        public Data data;

        public string accountId => data.accountId;
        public string symbol => data.symbol;
        public string headquarters => data.headquarters;
        public int credits => data.credits;

        public override string ToString() {
            return $"[{data.accountId}]{data.symbol} @ {data.headquarters} {data.credits}Cr";
        }
    }
}