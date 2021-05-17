using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json;

using RMass.Models;

using Serilog;

using WebSocketSharp.Server;

namespace RMass
{
    internal class Program
    {
        private const string PRODUCT_VERSION_API =
            "https://images.habbo.com/habbo-webgl-clients/205_3887bb9ab2bd85a393c1c2e5162dec1b/WebGL/habbo2020-global-prod/Build/habbo2020-global-prod.json";

        private static Queue<Account> _accounts = null!;

        private static Config _config = null!;

        private static WebSocketServer _wsServer = null!;

        private static async Task Main()
        {
            Console.Title = "RMass by ric";


            if (!File.Exists("accounts.txt"))
                await using (var f = File.Create("accounts.txt")) { }

            Log.Logger = LogCreator.Create("RMass");

            try
            {
                _config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync("Config.json"));
            }
            catch (Exception e)
            {
                Log.Error(e, "{File} file was wrong.", "Config.json");
                Log.Debug("Press any key to exit.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            var (product, success) = await TryGetModelByUrlTaskAsync<ProductVersionModel>(PRODUCT_VERSION_API);

            if (success) HabboConfig.ProductVersion = product.ProductVersion;
            else
            {
                Log.Error("Error loading habbo data!");
                Log.Debug("Press any key to exit.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }


            await InitializeAsync();

            await Task.Delay(-1);
        }

        private static async Task InitializeAsync()
        {
            _accounts = new Queue<Account>();

            // if (_config.RoomId == 0)
            // {
            //     Log.Error("A ID de um quarto não pode ser ZERO. Edite o arquivo Config.json com as devidas ID's. Coloque ID 0 para não Respeitar/Acariciar os respectivos sujeitos.");
            //
            //     Log.Debug("Aperte qualquer tecla para fechar o programa.");
            //     Console.ReadKey(true);
            //
            //     Environment.Exit(0);
            // }

            if (_config.UserId == 0)
            {
                Log.Error("User ID must be specified!");
                Log.Debug("Press any key to exit.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }


            Log.Information("Loading accounts...");

            var file = await File.ReadAllLinesAsync("accounts.txt");

            if (file.Length is 0)
            {
                Log.Error("The file {TextFile} cannot be empty. Set line by line in the format: {Format}",
                          "accounts.txt", "email:password");

                Log.Debug("Press any key to exit.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            var accounts = file.Where(line => line.Contains(":")).ToArray();

            if (accounts.Count() is 0)
            {
                Log.Error("Couldn't get any account. Make sure that there are accounts in the following format: {Format}",
                          "email:senha");

                Log.Debug("Press any key to exit.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            Log.Information("Loaded {Count} account(s).", accounts.Count());

            foreach (var account in accounts)
            {
                var acc = new Account(account);
                if (acc.IsValid()) _accounts.Enqueue(acc);
            }

            if (_accounts.Count == 0)
            {
                Log.Error("There is no valid account. Make sure that there are accounts in the following format: {Format}",
                          "email:senha");

                Log.Debug("Press any key to exit.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            Log.Debug("{Count} valid account(s).", _accounts.Count);


            _wsServer            = new WebSocketServer(27);
            _wsServer.Log.Output = (_, __) => { };
            _wsServer.AddWebSocketService("/ric", () => new CaptchaService(_accounts, _config));
            _wsServer.Start();

            Log.Information("Local server started, you now can make captchas!");
        }

        private static async Task<(T model, bool success)> TryGetModelByUrlTaskAsync<T>(string url)
        {
            try
            {
                var client = new WebClient
                {
                    Headers =
                    {
                        [HttpRequestHeader.UserAgent] =
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.190 Safari/537.36"
                    }
                };


                var data = await client.DownloadStringTaskAsync(url);

                var model = JsonConvert.DeserializeObject<T>(data);

                return (model, true);
            }
            catch
            {
                return (default, false);
            }
        }
    }

    internal class Account
    {
        public Account(String account)
        {
            var split = account.Split(':');

            if (split.Length != 2) return;

            Email    = split[0];
            Password = split[1];
        }

        public String Email    { get; set; } = null!;
        public String Password { get; set; } = null!;

        public Boolean IsValid()
        {
            return !String.IsNullOrWhiteSpace(Email) && !String.IsNullOrWhiteSpace(Password);
        }
    }

    internal class Config
    {
        public Int32   RoomId   { get; set; }
        public Int32   UserId   { get; set; }
        public String? UserName { get; set; }
        public Int32   PetId    { get; set; }
        public Int32   GuildId  { get; set; }
    }
}