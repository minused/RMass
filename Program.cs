using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Serilog;

using WebSocketSharp;
using WebSocketSharp.Server;

using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using Logger = Serilog.Core.Logger;

namespace RMass
{
    internal class Program
    {
        private static Queue<Account> _accounts = null!;

        private static Config _config = null!;

        private static WebSocket       _socket   = null!;
        private static WebSocketServer _wsServer = null!;

        private static Logger _wsLogger = null!;

        private static async Task Main()
        {
            Console.Title = "RMass by ric";

            Log.Logger = LogCreator.Create("RMass");

            try
            {
                _config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync("Config.json"));
            }
            catch (Exception e)
            {
                Log.Error(e, "Arquivo {File} mal formatado.", "Config.json");
                Log.Debug("Aperte qualquer tecla para fechar o programa.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            _socket = new WebSocket("ws://191.233.255.227:1555/rmass") { Log = { Output = ( _, __ ) => { } } };
            _socket.OnMessage += OnWebSocketMessage;
            _socket.OnError += OnWebSocketError;
            _socket.OnOpen += OnWebSocketOpened;

            _wsLogger = LogCreator.Create("Servidor");

            _wsLogger.Information("Conectando ao servidor de autenticação...");

            // ReSharper disable once MethodHasAsyncOverload
            _socket.Connect();

            await Task.Delay(-1);
        }

        private static void OnWebSocketOpened( Object? sender, EventArgs e )
        {
            _wsLogger.Information("Conectado ao servidor!");
            _wsLogger.Debug("Autenticando conexão...");

            _socket.Send($"1|{_config.Token}");
        }

        private static void OnWebSocketError( Object? sender, ErrorEventArgs e )
        {
            _wsLogger.Error(e.Exception, "Houve um erro ao conectar.");
        }

        private static async void OnWebSocketMessage( Object? sender, MessageEventArgs e )
        {
            if (e.IsBinary)
            {
                _socket.CloseAsync(CloseStatusCode.Normal);

                return;
            }

            var split  = e.Data.Split('|');
            var header = split[0];

            if (header != "1") return;

            if (split[1] == "200")
            {
                var isPremium = Boolean.Parse(split[2]);

                if (isPremium)
                {
                    var myHwid    = GetUniqueId();
                    var remaining = split[3];

                    if (split[4] == "null")
                        _socket.Send($"2|{_config.Token}|{myHwid}");
                    else
                    {
                        if (split[4] != myHwid)
                        {
                            Log.Error("Este token foi registrado em outro computador!");

                            return;
                        }
                    }

                    Log.Information("Bem-vindo! Você tem {Remaining} dia(s) de assinatura restante(s).",
                                    Int32.Parse(remaining));

                    // ReSharper disable once MethodHasAsyncOverload
                    _socket.Close();

                    await InitializeAsync();
                }
                else
                    Log.Error("Sua assinatura terminou!");
            }
            else
                Log.Error("Token inválido.");
        }

        private static async Task InitializeAsync()
        {
            _accounts = new Queue<Account>();

            if (_config.RoomId == 0)
            {
                Log.Error("A ID de um quarto não pode ser ZERO. Edite o arquivo Config.json com as devidas ID's. Coloque ID 0 para não Respeitar/Acariciar os respectivos sujeitos.");

                Log.Debug("Aperte qualquer tecla para fechar o programa.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }


            Log.Information("Carregando contas...");

            var file = await File.ReadAllLinesAsync("Contas.txt");

            if (file.Length is 0)
            {
                Log.Error("O arquivo {TextFile} não pode estar vazio. Preencha linha por linha no seguinte formato: {Format}",
                          "Contas.txt", "email:senha");

                Log.Debug("Aperte qualquer tecla para fechar o programa.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            var accounts = file.Where(line => line.Contains(":")).ToArray();

            if (accounts.Count() is 0)
            {
                Log.Error("Não foi possível obter nenhuma conta. Certifique-se que exista contas no seguinte formato: {Format}",
                          "email:senha");

                Log.Debug("Aperte qualquer tecla para fechar o programa.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            Log.Information("{Count} conta(s) carregada(s).", accounts.Count());

            foreach (var account in accounts)
            {
                var acc = new Account(account);
                if (acc.IsValid()) _accounts.Enqueue(acc);
            }

            if (_accounts.Count == 0)
            {
                Log.Error("Não há nenhuma conta válida. Certifique-se que exista contas no seguinte formato: {Format}",
                          "email:senha");

                Log.Debug("Aperte qualquer tecla para fechar o programa.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            Log.Debug("{Count} conta(s) válida(s).", _accounts.Count);

            if (!Headers.TryLoadHeaders())
            {
                Log.Debug("As headers são necessárias! Aperte qualquer tecla para fechar o programa.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            _wsServer            = new WebSocketServer(27);
            _wsServer.Log.Output = ( _, __ ) => { };
            _wsServer.AddWebSocketService("/ric", () => new CaptchaService(_accounts, _config));
            _wsServer.Start();

            Log.Information("Servidor local iniciado, você já pode fazer captchas!");
        }

        private static String GetUniqueId()
        {
            return new
                SecurityIdentifier((Byte[]) new DirectoryEntry($"WinNT://{Environment.MachineName},Computer").Children.Cast<DirectoryEntry>().First().InvokeGet("objectSID"),
                                   0).AccountDomainSid.Value;
        }
    }

    internal class Account
    {
        public Account( String account )
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
        public String? Token    { get; set; }
    }
}