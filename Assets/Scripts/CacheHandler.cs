using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceTraders
{
    public class CacheHandler {
        public enum ReturnCode { UNKNOWN_ERROR = -1, SUCCESS, NOT_FOUND, EXPIRED }

        public class CachedItem {
            public string contents;
            public long createdAt;
            public TimeSpan lifespan;

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

            public (ReturnCode, string) GetContents() {
                DateTime ExpiryDate = new DateTime(createdAt).Add(lifespan);
                if(DateTime.Compare(DateTime.Now, ExpiryDate) > 0) {
                    return (ReturnCode.EXPIRED, null);
                }
                return (ReturnCode.SUCCESS, contents);
            }
        }

        public static ReturnCode Save( string name, string payload, TimeSpan lifespan ) {
            List<string> pathSegments = new List<string>();
            // Split endpoint names
            foreach(string segment in name.Split('/')) {
                pathSegments.Add(segment);
            }

            name = pathSegments[^1];
            pathSegments.RemoveAt(pathSegments.Count-1);

            // No more systems.json.json!
            if(name.EndsWith(".json") == false) name += ".json";

            string filePath = Path.Combine(Application.persistentDataPath, string.Join(Path.DirectorySeparatorChar, pathSegments), name);
            CachedItem cache;
            if(File.Exists(filePath)) {
                cache = JsonConvert.DeserializeObject<CachedItem>(File.ReadAllText(filePath));
                cache.Update(payload, lifespan);
            } else {
                cache = new CachedItem(payload, lifespan);

                // Make sure the directory exists...
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, string.Join(Path.DirectorySeparatorChar, pathSegments)));
            }

            File.WriteAllText(filePath, JsonConvert.SerializeObject(cache));
            return ReturnCode.SUCCESS;
        }

        public static (ReturnCode, string) Load( string name ) {
            string fileName = Path.Combine(Application.persistentDataPath, name + ".json");
            if(File.Exists(fileName) == false) {
                return (ReturnCode.NOT_FOUND, null);
            }
            CachedItem cache = JsonConvert.DeserializeObject<CachedItem>(File.ReadAllText(fileName));
            return cache.GetContents();
        }
    }
}
