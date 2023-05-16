using System.Collections.Generic;

namespace SpaceTraders
{
    public class ServerResponse<T>
    {
        public List<T> data;
        public Meta meta;

        public override string ToString() {
            return $"Server Response with {data.Count}/{meta.total} results. (Limit: {meta.limit}, page {meta.page}/{meta.TotalPages}";
        }
    }
}
