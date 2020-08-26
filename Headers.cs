using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

using Serilog.Core;

using Sulakore.Habbo;
using Sulakore.Habbo.Messages;

namespace RMass
{
    internal static class Headers
    {
        private static readonly Logger Logger;

        static Headers()
        {
            Logger = LogCreator.Create("HeaderManager");
        }

        public static UInt16 RoomUserGiveRespect { get; set; }
        public static UInt16 RequestRoomLoad     { get; set; }
        public static UInt16 ScratchPet          { get; set; }
        public static UInt16 RequestGuildJoin    { get; set; }
        public static UInt16 FriendRequest       { get; set; }
        public static UInt16 SSOTicket           { get; set; }

        // ReSharper disable once InconsistentNaming
        public static UInt16 MachineID            { get; set; }
        public static UInt16 Ping                 { get; set; }
        public static UInt16 Pong                 { get; set; }
        public static UInt16 OutGenerateSecretKey { get; set; }
        public static UInt16 InGenerateSecretKey  { get; set; }
        public static UInt16 InitCrypto           { get; set; }
        public static UInt16 ReleaseVersion       { get; set; }
        public static UInt16 VerifyPrimes         { get; set; }
        public static UInt16 ClientVariables      { get; set; }
        public static UInt16 RequestUserData      { get; set; }

        public static Variables Variables { get; set; } = null!;

        public static Boolean TryLoadHeaders()
        {
            Logger.Information("Tentando atualizar headers...");

            using var wc = new WebClient
            {
                Headers =
                {
                    ["User-Agent"] =
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.97 Safari/537.36"
                }
            };

            StreamReader reader;

            try
            {
                reader = new StreamReader(wc.OpenRead("https://www.habbo.com.br/gamedata/external_variables/1")!);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Não foi possível atualizar as headers.");

                return false;
            }

            try
            {
                String line;

                var dict = new Dictionary<String, String>();

                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.Contains("=")) continue;

                    var split = line.Split('=');
                    dict[split[0]] = split[1];
                }

                var release = dict["flash.client.url"].Split('/')[4];

                if (!Directory.Exists("swf"))
                {
                    var swfdir = Directory.CreateDirectory("swf");
                    swfdir.Attributes = FileAttributes.Hidden;
                }

                var path = Path.Combine("swf", $"{release}.swf");

                if (!File.Exists(path))
                {
                    wc.DownloadFile($"https:{dict["flash.client.url"]}Habbo.swf", path);

                    var unused = new FileInfo(path) { Attributes = FileAttributes.Hidden };
                }

                using (var game = new HGame(path))
                {
                    game.Disassemble();
                    game.GenerateMessageHashes();

                    var incoming = new Incoming();
                    var outgoing = new Outgoing();

                    incoming.Load(game, "Hashes.ini");
                    outgoing.Load(game, "Hashes.ini");

                    RoomUserGiveRespect  = outgoing.RoomUserGiveRespect;
                    RequestRoomLoad      = outgoing.RequestRoomLoad;
                    ScratchPet           = outgoing.ScratchPet;
                    RequestGuildJoin     = outgoing.RequestGuildJoin;
                    FriendRequest        = outgoing.FriendRequest;
                    SSOTicket            = outgoing.SSOTicket;
                    MachineID            = outgoing.MachineID;
                    OutGenerateSecretKey = outgoing.GenerateSecretKey;
                    ReleaseVersion       = outgoing.ReleaseVersion;
                    InitCrypto           = outgoing.InitCrypto;
                    ClientVariables      = outgoing.ClientVariables;
                    RequestUserData      = outgoing.RequestUserData;
                    Pong                 = outgoing.Pong;

                    Ping                = incoming.Ping;
                    VerifyPrimes        = incoming.VerifyPrimes;
                    InGenerateSecretKey = incoming.GenerateSecretKey;
                }

                var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("user-agent",
                                                                         "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.80 Safari/537.36");

                var resp = httpClient.GetAsync("https://www.habbo.com.br/gamedata/external_variables/0").Result;


                Variables = new Variables
                {
                    Production       = release,
                    ClientVariables1 = $"https:{dict["flash.client.url"]}",
                    ClientVariables2 = resp.Headers.GetValues("Location").First()
                };

                httpClient.Dispose();

                Logger.Information("Headers atualizadas.");

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Falha ao atualizar as headers.");

                return false;
            }
        }
    }

    public class Variables
    {
        public String Production       { get; set; }
        public String ClientVariables1 { get; set; }
        public String ClientVariables2 { get; set; }
    }
}