using UnityEngine;

namespace SpaceTraders
{
    public class Meta
    {
        public int total;
        public int page;
        public int limit;
        public int TotalPages => Mathf.CeilToInt(total / limit);
    }
}
