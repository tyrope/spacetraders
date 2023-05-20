namespace STCommander
{
    public class AgentInfo
    {
        public string accountId;
        public string symbol;
        public string headquarters;
        public int credits;

        public override string ToString() {
            return $"[{accountId}]{symbol} @ {headquarters} {credits}Cr";
        }
    }
}
