using System;
using GrandTheftMultiplayer.Server.API;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace rumble {
    public class JSONRepository : Script, IRepository {
        public void Init() {
            if (!Directory.Exists("PlayerData/")) {
                API.consoleOutput("creating data folder");
                Directory.CreateDirectory("PlayerData/");
            }
        }

        public T Load<T>(string id) {
            if (!File.Exists(GetFileName(typeof(T).Name, id))) {
                API.consoleOutput("Could not find file '" + GetFileName(typeof(T).Name, id) + "'");
                return default(T);
            }

            string json = File.ReadAllText(GetFileName(typeof(T).Name, id));
            T data = API.shared.fromJson(json).ToObject<T>();

            return data;
        }

        public List<T> Load<T>() {
            if (!File.Exists(GetFileName("data", typeof(T).Name))) {
                API.consoleOutput("Could not find file '" + GetFileName("data", typeof(T).Name) + "'");
                return new List<T>();
            }

            string json = File.ReadAllText(GetFileName("data", typeof(T).Name));

            return JsonConvert.DeserializeObject<List<T>>(json);
        }

        public Dictionary<K, T> Load<K, T>() {
            if (!File.Exists(GetFileName("data", typeof(T).Name))) {
                API.consoleOutput("Could not find file '" + GetFileName("data", typeof(T).Name) + "'");
                return new Dictionary<K, T>();
            }

            string json = File.ReadAllText(GetFileName("data", typeof(T).Name));

            return JsonConvert.DeserializeObject<Dictionary<K, T>>(json);
        }

        public void Save<T>(string id, T data) {
            var json = API.shared.toJson(data);
            File.WriteAllText(GetFileName(typeof(T).Name, id), json);
        }

        public void Save<T>(List<T> data) {
            var json = API.shared.toJson(data);
            File.WriteAllText(GetFileName("data", typeof(T).Name), json);
        }

        public void Save<K, T>(Dictionary<K, T> data) {
            var json = API.shared.toJson(data);
            File.WriteAllText(GetFileName("data", typeof(T).Name), json);
        }

        public bool Exists<T>(string id) {
            return File.Exists(GetFileName(typeof(T).Name, id));
        }

        public string GetFileName(string type, string id) {
            return Path.Combine(type, id + ".json");
        }
    }
}
