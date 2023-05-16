using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceTraders
{
    public class CacheHandler {
        public enum RETURNCODE { UNKNOWN_ERROR = -1, SUCCESS, NOT_FOUND, EXPIRED }

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

            public (RETURNCODE, string) GetContents() {
                DateTime ExpiryDate = new DateTime(createdAt).Add(lifespan);
                if(DateTime.Compare(DateTime.Now, ExpiryDate) > 0) {
                    return (RETURNCODE.EXPIRED, null);
                }
                return (RETURNCODE.SUCCESS, contents);
            }
        }

        public static RETURNCODE Save( string name, string payload, TimeSpan lifespan ) {
            List<string> pathSegments = new List<string>();
            // Split endpoint names
            foreach(string segment in name.Split('/')) {
                pathSegments.Add(segment);
            }

            // Split sector/system/waypoint ID into folders
            name = pathSegments[pathSegments.Count-1];
            pathSegments.RemoveAt(pathSegments.Count-1);
            foreach(string segment in name.Split('-')) {
                pathSegments.Add(segment);
            }

            name = pathSegments[pathSegments.Count-1];
            pathSegments.RemoveAt(pathSegments.Count-1);

            string filePath = Path.Combine(Application.persistentDataPath, string.Join(Path.DirectorySeparatorChar, pathSegments), name + ".json");
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
