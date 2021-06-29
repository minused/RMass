using System;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using Logger = Serilog.Core.Logger;

namespace RMass
{
    internal class CaptchaService : WebSocketBehavior
    {
        private readonly Queue<Account> _accounts;
        private readonly Config         _config;
        private readonly Logger         _logger;

        public CaptchaService(Queue<Account> accounts, Config config)
        {
            _accounts = accounts;
            _config   = config;

            _logger = LogCreator.Create("CaptchaService");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.IsBinary)
            {
                Context.WebSocket.Close();

                return;
            }

            if (string.IsNullOrWhiteSpace(e.Data)) return;

            _logger.Information("Captcha received.");

            if (_accounts.Count == 0)
            {
                Serilog.Log.Information("All accounts were used.");
                Serilog.Log.Debug("Press any key to exit.");
                Console.ReadKey(true);

                Environment.Exit(0);
            }

            var currentAccount = _accounts.Dequeue();

            var habboManager = new HabboManager(++Helper.CurrentId, _config);
            habboManager.HandleAccount(e.Data, currentAccount);
        }
    }
}