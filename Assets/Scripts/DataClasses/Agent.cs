﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class Agent : IDataClass
    {
        public string accountId;
        public string symbol;
        public string headquarters;
        public int credits;
        public string StartingFaction;

        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge ) {
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;
            List<List<object>> result = await DatabaseManager.instance.SelectQuery($"SELECT accountId, symbol, headquarters, credits, startingFaction FROM Agent WHERE lastEdited<{highestUnixTimestamp}");
            if(result.Count != 1) {
                Debug.LogError($"Agent::LoadFromCache() -- Wrong amount of results: {result.Count}.");
                return null;
            }
            return result[0] == null ? default : new List<IDataClass>() { new Agent(result[0]) };
        }

        public async Task<bool> SaveToCache() {
            return await DatabaseManager.instance.WriteQuery(
                $"INSERT INTO Agent (accountId, symbol, headquarters, credits, startingFaction, lastEdited) VALUES ('"
                + $"{accountId}', '{symbol}', '{headquarters}', {credits}, '{StartingFaction}', unixepoch(now))"
                + "ON CONFLICT(symbol) DO UPDATE SET credits=excluded.credits, lastEdited=excluded.lastEdited;") > 0;
        }

        private Agent( List<object> fields ) {
            accountId = (string) fields[0];
            symbol = (string) fields[1];
            headquarters = (string) fields[2];
            credits = (int) fields[3];
            StartingFaction = (string) fields[4];
        }

        public override string ToString() {
            return $"[{accountId}]{symbol} @ {headquarters} {credits}Cr";
        }
    }
}
