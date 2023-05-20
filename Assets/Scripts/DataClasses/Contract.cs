using System;

namespace STCommander
{
    public class Contract
    {
        public enum ContractStatus { OFFERED, ACCEPTED, LATE, FULFILLED, EXPIRED }
        public class Terms
        {
            public class Payment
            {
                public int onAccepted;
                public int onFulfilled;
                public override string ToString() => $"{onAccepted:n0}cr up front, {onFulfilled:n0}cr on delivery.";
            }
            public class Delivery
            {
                public string tradeSymbol;
                public string destinationSymbol;
                public int unitsRequired;
                public int unitsFulfilled;
                public override string ToString() => $"{unitsFulfilled}/{unitsRequired} {tradeSymbol}→{destinationSymbol}";
            }
            public DateTime deadline;
            public Payment payment;
            public Delivery[] deliver;

            public override string ToString() {
                string ret = $"Complete before: {deadline.ToString("yy-MM-dd'T'HH:mm:ss")}\n";
                foreach(Delivery delivery in deliver) {
                    ret += delivery + "\n";
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

        public override string ToString() => $"{id}\n{GetStatus()} {type} contract issued by {factionSymbol}: {terms}.";
    }
}
