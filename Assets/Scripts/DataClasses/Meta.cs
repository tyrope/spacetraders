using UnityEngine;

namespace SpaceTraders
{
    public class Meta
    {
        public int total;
        public int page;
        public int limit;
        public int TotalPages => Mathf.CeilToInt((float) total / (float)limit);

        public override string ToString() => $"{Mathf.Min(total, limit)} result{total > 1: \"s\"} (Page {page}/{TotalPages})";
    }
}
