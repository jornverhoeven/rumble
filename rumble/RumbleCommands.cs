using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Constant;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Server.Managers;
using GrandTheftMultiplayer.Shared;
using GrandTheftMultiplayer.Shared.Math;
using System;
using System.Linq;
using System.Collections.Generic;

namespace rumble {
    public partial class RumbleGameMode : Script {

        [Command("me", "Usage: /me [message]", GreedyArg = true)]
        public void Me(Client player, string message) {
            API.sendChatMessageToAll(new MessageBuilder().Purple(player.name + " " + message).ToString());
        }

        [Command("help")]
        public void Help(Client player) {
            API.sendChatMessageToPlayer(player,
                new MessageBuilder("Welcome to Rumble!").ToString());
        }

        [Command("skin", "Usage: /skin [model]")]
        public void ChangeSkinCommand(Client player, PedHash model) {
            API.setPlayerSkin(player, model);
            API.sendNativeToPlayer(player, Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, player.handle);

            playerData[player.socialClubName].Skin = model;
        }

        [Command("login", "Usage: /login [password]", SensitiveInfo = true, GreedyArg = true)]
        public void Login(Client player, string password) {
            // Already logged in.
            if (playerData[player.socialClubName].LoggedIn == true) {
                API.sendChatMessageToPlayer(player,
                    new MessageBuilder().Red("You are already logged in! No need to login again. ").ToString());
                return;
            }

            // Not registered.
            if (!repository.Exists<PlayerData>(player.socialClubName)) {
                API.sendChatMessageToPlayer(player,
                    new MessageBuilder().Red("This user is not yet registered! ")
                    .Gray("please use ").Orange("/register <password>").Gray("to register.").ToString());
                return;
            }

            // Login.
            PlayerData data = repository.Load<PlayerData>(player.socialClubName);
            if (API.shared.getHashSHA256(password) == data.Password) {
                LoginPlayer(player, data);
                playerData[player.socialClubName] = data;
                API.sendChatMessageToPlayer(player,
                    new MessageBuilder("Sucessfully logged in!").ToString());
            } else {
                API.sendChatMessageToPlayer(player,
                    new MessageBuilder().Red("Invalid credentials!").ToString());
            }
        }

        [Command("register", "Usage: /register [password]", SensitiveInfo = true, GreedyArg = true)]
        public void Register(Client player, string password) {
            // Already logged in.
            if (playerData[player.socialClubName].LoggedIn == true) {
                API.sendChatMessageToPlayer(player,
                    new MessageBuilder().Red("You are already logged in! No need to register again. ").ToString());
                return;
            }

            // File exists.
            if (repository.Exists<PlayerData>(player.socialClubName)) {
                API.sendChatMessageToPlayer(player,
                    new MessageBuilder().Red("This user is already registered! ")
                    .Gray("please use ").Orange("/login <password>").Gray("to login.").ToString());
                return;
            }

            // Register.
            PlayerData data = new PlayerData {
                SocialClubName = player.socialClubName,
                Ip = player.address,
                Position = player.position,
                Skin = PedHash.Acult02AMY,
                LoggedIn = false,
                Level = 1,
                Money = 0,
                Password = API.shared.getHashSHA256(password)
            };

            repository.Save(player.socialClubName, data);
            API.sendChatMessageToPlayer(player,
                new MessageBuilder("Sucessfully registered!")
                .Gray("please use ").Orange("/login <password>").Gray("to login.").ToString());
        }

        [Command("get", "Usage: /get [model]", Alias ="v")]
        public void GetCar(Client player, string model) {
            try {
                VehicleHash vehicleModel = getVehicleModel(model);
                Random r = new Random();
                API.createVehicle(vehicleModel, player.position + new Vector3(2.0, 0, 0), new Vector3(0, 0, 0), r.Next(0, 160), r.Next(0, 160));
            } catch (ArgumentException e) {
                API.sendChatMessageToPlayer(player, 
                    new MessageBuilder().Red("No vehicle model found for '"+model+"'.").ToString());
            }
        }

        [Command("tp", "Usage: /tp [player]")] 
        public void TeleportTo(Client player, Client target) {
            var pos = API.getEntityPosition(player.handle);
            API.createParticleEffectOnPosition("scr_rcbarry1", "scr_alien_teleport", pos, new Vector3(), 1f);
            API.setEntityPosition(player.handle, API.getEntityPosition(target.handle));
        }
        
        [Command("tele", "Usage: /get [name]")]
        public void Teleport(Client player, string name) {
            if (teleports.ContainsKey(name)) {
                player.position = teleports[name].Position;
                return;
            }
            API.sendChatMessageToPlayer(player,
                new MessageBuilder().Red("No such teleport found!").ToString());
        }

        [Command("teleshow", Alias = "teles")]
        public void TeleportShow(Client player) {
            List<Tuple<TeleportData, float>> ts = new List<Tuple<TeleportData, float>>();

            foreach (TeleportData teleport in teleports.Values) {
                if (teleport.Position.DistanceTo(player.position) < 10) {
                    ts.Add(Tuple.Create(teleport, teleport.Position.DistanceTo(player.position)));
                }
            }

            if (ts.Count > 0) {
                API.sendChatMessageToPlayer(player, new MessageBuilder("Found teleports:").ToString());

                ts = ts.OrderBy(t => t.Item2).ToList();
                foreach (Tuple<TeleportData, float> tele in ts) {
                    API.sendChatMessageToPlayer(player, new MessageBuilder().Orange(tele.Item1.Name).Gray("\t ["+tele.Item1.Owner+"] - " + tele.Item2.ToString() + "m").ToString());
                }
            }
        }

        [Command("telecreate", "Usage: /telecreate [name]", Alias = "telec")]
        public void TeleportCreate(Client player, string name) {
            if (teleports.ContainsKey(name)) {
                API.sendChatMessageToPlayer(player,
                new MessageBuilder().Red("Teleport already exists").ToString());
                return;
            }

            teleports.Add(name, new TeleportData() {
                Name = name,
                Owner = player.socialClubName,
                Position = player.position
            });
            API.sendChatMessageToPlayer(player, new MessageBuilder("Sucessfully created ").Green(name).ToString());
        }

        [Command("teledelete", Alias = "teled")]
        public void TeleportDelete(Client player) {
            TeleportData cur = null;

            foreach (TeleportData teleport in teleports.Values) {
                if (teleport.Position.DistanceTo(player.position) < 0.5 && teleport.Owner == player.socialClubName) {
                    cur = teleport;
                    break;
                }
            }
            if (cur != null) {
                teleports.Remove(cur.Name);
                API.sendChatMessageToPlayer(player, new MessageBuilder("Sucessfully deleted ").Green(cur.Name).ToString());
            } else {
                API.sendChatMessageToPlayer(player, new MessageBuilder().Red("No teleport near you!").ToString());
            }
        }

        private VehicleHash getVehicleModel(string model) {

            foreach (string vehicle in Enum.GetNames(typeof(VehicleHash))) {
                if (vehicle.ToLower().StartsWith(model.ToLower())) {
                    return API.vehicleNameToModel(vehicle);
                }
            }

            throw new ArgumentException("");
        }
    }
}
