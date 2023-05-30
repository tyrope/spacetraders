using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class Contract : IDataClass
    {
        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge ) {
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;
            List<List<object>> contracts = await DatabaseManager.instance.SelectQuery("SELECT Contract.id, Contract.factionSymbol, Contract.type, Contract.accepted, Contract.fulfilled, "
                + "Contract.deadlineToAccept, ContractTerms.deadline, ContractPayment.onAccepted, ContractPayment.onFulfilled FROM Contract, ContractTerms, ContractPayment WHERE "
                + "Contract.terms=ContractTerms.rowid AND Terms.payment=ContractPayment.rowid AND Contract.lastEdited<" + highestUnixTimestamp);
            if(contracts.Count == 0) {
                Debug.Log($"IDataClass::LoadFromCache() -- No results.");
                return null;
            }
            List<IDataClass> ret = new List<IDataClass>();
            List<List<object>> deliverGoods;
            foreach(List<object> p in contracts) {
                deliverGoods = await DatabaseManager.instance.SelectQuery("SELECT ContractDeliverGood.rowid, ContractDeliverGood.tradeSymbol, ContractDeliverGood.destinationSymbol, ContractDeliverGood.unitsRequired, "
                    + "ContractDeliverGood.unitsFulfilled FROM Contract, ContractDeliverGood, ContractDeliverGood_ContractTerms_relationship WHERE"
                    + " ContractDeliverGood_ContractTerms_relationship.good = ContractDeliverGood.rowid AND ContractDeliverGood_ContractTerms_relationship.terms = Contract.terms AND Contract.id=" + p[0]);
                ret.Add(new Contract(p, deliverGoods));
            }
            return ret;
        }

        public async Task<bool> SaveToCache() {
            string query = "BEGIN TRANSACTION;"; // This is going to be a big update. Do not let anybody else interfere.

            // First, our delivery goods.
            query += $"REPLACE INTO ContractDeliverGood (rowid, tradeSymbol, destinationSymbol, unitsRequired, unitsFulfilled) VALUES";
            foreach(Terms.Deliver good in terms.deliver) {
                query += $"({good.rowid}, {good.tradeSymbol}, {good.destinationSymbol}, {good.unitsRequired}, {good.unitsFulfilled}),";
            }
            query = query[0..^1] + ";"; // Replace last comma with a semicolon.

            query += "COMMIT;"; // Send it!
            return await DatabaseManager.instance.WriteQuery(query) > 0;
        }

        public Contract( List<object> p, List<List<object>> deliverGoods) {
            id = (string) p[0];
            factionSymbol = (string) p[1];
            type = Enum.Parse<ContractType>((string) p[2]);
            accepted = (int) p[3] == 1 ? true : false;
            fulfilled = (int) p[4] == 1 ? true : false;
            deadlineToAccept = DateTime.UnixEpoch + TimeSpan.FromSeconds((int) p[5]);
            terms.deadline = DateTime.UnixEpoch + TimeSpan.FromSeconds((int) p[6]);
            terms.payment.onAccepted = (int) p[7];
            terms.payment.onFulfilled = (int) p[8];
            foreach(List<object> good in deliverGoods) {
                terms.deliver.Add(new Terms.Deliver(good));
            }
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
                protected internal int rowid;
                public string tradeSymbol;
                public string destinationSymbol;
                public int unitsRequired;
                public int unitsFulfilled;

                public Deliver( List<object> good ) {
                    rowid = (int) good[0];
                    tradeSymbol = (string) good[1];
                    destinationSymbol = (string) good[2];
                    unitsRequired = (int) good[3];
                    unitsFulfilled = (int) good[4];
                }

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
    }
}
