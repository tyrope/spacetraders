using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace SpaceTraders
{
    public class CacheHandler {
        public enum RETURNCODE { UNKNOWN_ERROR = -1, SUCCESS, NOT_FOUND, EXPIRED }

        private class CachedItem {
            private string contents;
            private long createdAt;
            private TimeSpan lifespan;

            public CachedItem( string payload, TimeSpan duration ) {
                contents = payload;
                createdAt = DateTime.Now.Ticks;
                lifespan = duration;
            }

            public void Update( string payload, TimeSpan duration ) {
                contents = payload;
                createdAt = DateTime.Now.Ticks;
                lifespan = duration;
            }

            public (RETURNCODE, string) GetContents() {
                if(DateTime.Compare(DateTime.Now, new DateTime(createdAt).Add(lifespan)) >= 0) {
                    return (RETURNCODE.EXPIRED, null);
                }
                return (RETURNCODE.SUCCESS, contents);
            }
        }

        public static RETURNCODE Save( string name, string payload, TimeSpan lifespanInTicks ) {
            string fileName = Path.Combine(Application.persistentDataPath, name + ".json");
            CachedItem cache;
            if(File.Exists(fileName)) {
                cache = JsonConvert.DeserializeObject<CachedItem>(File.ReadAllText(fileName));
                cache.Update(payload, lifespanInTicks);
            } else {
                cache = new CachedItem(payload, lifespanInTicks);
            }
            File.WriteAllText(fileName, JsonConvert.SerializeObject(cache));
            return RETURNCODE.SUCCESS;
        }

        public static (RETURNCODE, string) Load( string name ) {
            string fileName = Path.Combine(Application.persistentDataPath, name + ".json");
            if(File.Exists(fileName) == false) {
                return (RETURNCODE.NOT_FOUND, null);
            }
            CachedItem cache = JsonConvert.DeserializeObject<CachedItem>(File.ReadAllText(fileName));
            return cache.GetContents();
        }
    }
}
