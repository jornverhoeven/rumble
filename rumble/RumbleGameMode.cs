using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Constant;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Server.Managers;
using GrandTheftMultiplayer.Shared;
using GrandTheftMultiplayer.Shared.Math;
using System;
using System.Collections.Generic;

namespace rumble
{
    public partial class RumbleGameMode : Script
    {
        Dictionary<Client, Blip> playerBlips;
        Dictionary<string, TeleportData> teleports;

        IRepository repository;
        long updateTimer;

        Dictionary<string, PlayerData> playerData;

        public RumbleGameMode() {
            API.onResourceStart += OnResourceStart;
            API.onPlayerConnected += OnPlayerConnected;
            API.onPlayerDisconnected += OnPlayerDisconnected;
            API.onPlayerFinishedDownload += OnPlayerFinishedDownload;
            API.onUpdate += OnUpdateHandler;
        }

        private void OnResourceStart() {
            API.consoleOutput("Starting \"Rumble\"!");

            repository = new JSONRepository();
            repository.Init();

            teleports = repository.Load<string, TeleportData>();

            playerData = new Dictionary<string, PlayerData>(API.getMaxPlayers());

            playerBlips = new Dictionary<Client, Blip>();

            foreach (var player in API.getAllPlayers()) {
                OnPlayerConnected(player);
            }

            updateTimer = API.TickCount;
        }

        private void OnResourceStop() {
        }

        private void OnPlayerConnected(Client player) {
            PlayerData data = null;

            try {
                data = repository.Load<PlayerData>(player.socialClubName);
                data.LoggedIn = false; // Always set this to false.
            } catch (Exception e) {
                // Could not login.
            }

            if (data == null || data.Password == null) {
                // Not found.
                data = new PlayerData() {
                    SocialClubName = player.socialClubName,
                    Ip = player.address,
                    Skin = PedHash.Acult02AMY,
                    LoggedIn = false
                };
                player.setSkin(PedHash.Acult02AMY);
                API.sendChatMessageToPlayer(player,
                    new MessageBuilder("Welcome to the server ").Green(player.name).White(", ")
                    .Gray("please use ").Orange("/register <password>").Gray("to register.").ToString());
            } else  if (data.Ip == player.address) {
                // Auto login.
                API.sendChatMessageToPlayer(player, new MessageBuilder("Welcome back ").Green(player.name).White(".").ToString());

                LoginPlayer(player, data);

            } else {
                // Not logged in.
                API.sendChatMessageToPlayer(player,
                    new MessageBuilder("Welcome back ").Green(player.name).White(", ")
                    .Gray("please use ").Orange("/login <password>").Gray("to login.").ToString());
            }

            if (!playerData.ContainsKey(player.socialClubName)) {
                playerData.Add(player.socialClubName, data);
            }

            var players = API.shared.getAllPlayers();

            foreach (var p in players) {
                if (p == player) {
                    continue;
                }
                API.sendChatMessageToPlayer(p, "~g~" + player.name + " ~w~joined the server!");
            }
        }

        private void OnPlayerDisconnected(Client player, string reason) {
            API.sendChatMessageToAll("~g~" + player.name + " ~w~left the server!");

            if (playerData.ContainsKey(player.socialClubName)) {
                playerData.Remove(player.socialClubName);
            }
        }

        private void OnPlayerFinishedDownload(Client player) {

        }

        private void OnUpdateHandler() {
            if (API.getAllPlayers().Count == 0) {
                return;
            }

            if (API.TickCount - updateTimer > 500) {
                foreach(Client player in API.getAllPlayers()) {
                    if (!playerData.ContainsKey(player.socialClubName)) { 
                        continue;
                    }
                    if (playerData[player.socialClubName].LoggedIn == false) {
                        continue;
                    }

                    // Update new data
                    playerData[player.socialClubName].Position = player.position;
                    playerData[player.socialClubName].Rotation = player.rotation;
                    playerData[player.socialClubName].Armor = player.armor;

                    // Store data
                    repository.Save(player.socialClubName, playerData[player.socialClubName]);
                }

                if (teleports.Count > 0) {
                    repository.Save(teleports);
                }
            }
        }

        private void LoginPlayer(Client player, PlayerData data) {
            player.position = data.Position;
            player.setSkin(data.Skin);

            data.Ip = player.address;
            data.LoggedIn = true;
        }        
    }
    public class MessageBuilder {
        public string message;

        public MessageBuilder(string message = "") {
            this.message = message;
        }

        public MessageBuilder White(string text) {
            this.message += "~w~" + text;
            return this;
        }
        public MessageBuilder Green(string text) {
            this.message += "~g~" + text;
            return this;
        }
        public MessageBuilder Red(string text) {
            this.message += "~r~" + text;
            return this;
        }
        public MessageBuilder Blue(string text) {
            this.message += "~b~" + text;
            return this;
        }
        public MessageBuilder Yellow(string text) {
            this.message += "~y~" + text;
            return this;
        }
        public MessageBuilder Purple(string text) {
            this.message += "~p~" + text;
            return this;
        }
        public MessageBuilder Pink(string text) {
            this.message += "~q~" + text;
            return this;
        }
        public MessageBuilder Orange(string text) {
            this.message += "~o~" + text;
            return this;
        }
        public MessageBuilder Gray(string text) {
            this.message += "~c~" + text;
            return this;
        }
        public MessageBuilder DarkGray(string text) {
            this.message += "~m~" + text;
            return this;
        }
        public MessageBuilder Black(string text) {
            this.message += "~b~" + text;
            return this;
        }
        public MessageBuilder NewLine() {
            this.message += "~n~";
            return this;
        }

        public override string ToString() {
            return this.message;
        }
    }
    public class PlayerData {
        public string SocialClubName { get; set; }
        public bool LoggedIn { get; set; }
        public string Password { get; set; }
        public int Level { get; set; }
        public int Money { get; set; }
        public bool Jailed { get; set; }
        public uint JailTime { get; set; }
        public int Armor { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public string Ip { get; set; }
        public PedHash Skin { get; set; }
    }
    public class TeleportData {
        public string Name { get; set; }
        public string Owner { get; set; }
        public Vector3 Position { get; set; }
    }
}
