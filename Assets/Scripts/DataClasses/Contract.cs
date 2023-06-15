using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STCommander
{
    public class Contract : IDataClass
    {
        public static readonly Dictionary<string, Contract> Instances = new Dictionary<string, Contract>();

        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge, CancellationToken cancel ) {
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;

            string contractID = "";
            if(endpoint.Trim('/') != "my/contracts") {
                // We're asking for a specific contract.
                contractID = $" AND id='{endpoint.Split('/')[^0]}' LIMIT 1";
            }

            List<List<object>> contracts = await DatabaseManager.instance.SelectQuery("SELECT id, factionSymbol, type, deadline, onAccepted, onFulfilled, accepted, fulfilled, deadlineToAccept FROM Contract WHERE lastEdited<" + highestUnixTimestamp + contractID, cancel);
            if(contracts.Count == 0) {
                return null;
            }
            List<IDataClass> ret = new List<IDataClass>();
            List<List<object>> deliverGoods;
            foreach(List<object> p in contracts) {
                deliverGoods = await DatabaseManager.instance.SelectQuery($"SELECT tradeSymbol, destinationSymbol, unitsRequired, unitsFulfilled FROM ContractDeliverGood WHERE contractId = '{p[0]}';", cancel);
                ret.Add(new Contract(p, deliverGoods));
            }
            return ret;
        }

        public async Task<bool> SaveToCache( CancellationToken cancel ) {
            string query = "BEGIN TRANSACTION;\n"; // This is going to be a big update. Do not let anybody else interfere.

            // ContractDeliverGood: contractId (TEXT NOT NULL), tradeSymbol (TEXT NOT NULL), destinationSymbol (TEXT NOT NULL), unitsRequired (INTEGER NOT NULL), unitsFulfilled (INTEGER NOT NULL)
            query += "INSERT INTO ContractDeliverGood (contractId, tradeSymbol, destinationSymbol, unitsRequired, unitsFulfilled) VALUES ";
            foreach(Terms.Deliver good in terms.deliver) {
                query += $"('{id}','{good.tradeSymbol}', '{good.destinationSymbol}', {good.unitsRequired}, {good.unitsFulfilled}),";
            }
            query = query[0..^1] + "ON CONFLICT(contractId, tradeSymbol, destinationSymbol) DO UPDATE SET unitsFulfilled=excluded.unitsFulfilled;\n"; // Replace last comma with the conflict clause.

            // Contract: id (TEXT NOT NULL), factionSymbol (TEXT NOT NULL), type (TEXT NOT NULL), deadline (INTEGER NOT NULL), onAccepted (INTEGER NOT NULL),
            // onFulfilled (INTEGER NOT NULL), accepted (INTEGER NOT NULL), fulfilled (INTEGER NOT NULL), deadlineToAccept (INTEGER NOT NULL), lastEdited (INTEGER NOT NULL)
            query += "INSERT INTO Contract (id, factionSymbol, type, deadline, onAccepted, onFulfilled, accepted, fulfilled, deadlineToAccept, lastEdited) VALUES ";
            query += $"('{id}','{factionSymbol}','{type}',{terms.deadline}, {terms.payment.onAccepted}, {terms.payment.onFulfilled}, {(accepted ? 1 : 0)},{(fulfilled ? 1 : 0)},{(deadlineToAccept - DateTime.UnixEpoch).TotalSeconds},STRFTIME('%s'));\n";

            query += "COMMIT;\n"; // Send it!
            return await DatabaseManager.instance.WriteQuery(query, cancel) > 0;
        }

        public Contract( List<object> p, List<List<object>> deliverGoods) {
            //id, factionSymbol, type, deadline, onAccepted, onFulfilled, accepted, fulfilled, deadlineToAccept, lastEdited
            id = (string) p[0];
            factionSymbol = (string) p[1];
            type = Enum.Parse<ContractType>((string) p[2]);
            terms.deadline = DateTime.UnixEpoch + TimeSpan.FromSeconds(Convert.ToInt32(p[3]));
            terms.payment.onAccepted = Convert.ToInt32(p[4]);
            terms.payment.onFulfilled = Convert.ToInt32(p[5]);
            accepted = Convert.ToInt32(p[6]) == 1;
            fulfilled = Convert.ToInt32(p[7]) == 1;
            deadlineToAccept = DateTime.UnixEpoch + TimeSpan.FromSeconds(Convert.ToInt32(p[8]));
            foreach(List<object> good in deliverGoods) {
                terms.deliver.Add(new Terms.Deliver(good));
            }

            Instances.Add(id, this);
        }


        public enum ContractStatus { OFFERED, ACCEPTED, LATE, FULFILLED, EXPIRED }
        public class Terms
        {
            public class Payment
            {
                public int onAccepted;
                public int onFulfilled;

                public override string ToString() => $"{onAccepted:n0}cr up front, {onFulfilled:n0}cr on delivery.";
            }
            public class Deliver
            {
                public string tradeSymbol;
                public string destinationSymbol;
                public int unitsRequired;
                public int unitsFulfilled;

                public Deliver( List<object> good ) {
                    tradeSymbol = (string) good[1];
                    destinationSymbol = (string) good[2];
                    unitsRequired = Convert.ToInt32(good[3]);
                    unitsFulfilled = Convert.ToInt32(good[4]);
                }
                public Deliver() {}

                public override string ToString() => $"{unitsFulfilled}/{unitsRequired} {tradeSymbol}→{destinationSymbol}";
            }

            public DateTime deadline;
            public Payment payment;
            public List<Deliver> deliver;

            public override string ToString() {
                string ret = $"Complete before: {deadline.ToString("yy-MM-dd'T'HH:mm:ss")}\n";
                foreach(Deliver d in deliver) {
                    ret += d + "\n";
                }
                return ret += payment;
            }
        }
        public enum ContractType { PROCUREMENT, TRANSPORT, SHUTTLE }

        public string id;
        public string factionSymbol;
        public ContractType type;
        public Terms terms;
        public bool accepted;
        public bool fulfilled;
        public DateTime deadlineToAccept;


        public ContractStatus GetStatus() {
            if(accepted == false) {
                if(DateTime.UtcNow >= deadlineToAccept) {
                    return ContractStatus.EXPIRED;
                } else {
                    return ContractStatus.OFFERED;
                }
            }
            if(fulfilled) { return ContractStatus.FULFILLED; }

            if(DateTime.UtcNow > terms.deadline) { return ContractStatus.LATE; }

            return ContractStatus.ACCEPTED;
        }

        public override string ToString() => $"{id}\n{GetStatus()} {type} contract issued by {factionSymbol}:\n{terms}.";

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public Contract() { }
    }
}
