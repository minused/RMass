using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Serilog.Core;

namespace RMass
{
    internal class HabboManager
    {
        private readonly Config _config;
        private readonly short _currentId;
        private readonly Logger _logger;

        public HabboManager(short id, Config config)
        {
            _config    = config;
            _currentId = id;

            _logger = LogCreator.Create("HabboManager");
        }

        public async void HandleAccount([NotNull] string token, [NotNull] Account currentAccount)
        {
            var habbo = new Habbo(token, _currentId);

            _logger.Information("[Acc #{Id}] Trying log in...", habbo.Id);

            if (!await habbo.TryLoginAsync(currentAccount.Email, currentAccount.Password)) return;
            if (!await habbo.TryLoadAvatarsAsync()) return;

            for (short i = 0; i < habbo.Count; i++)
            {
                _logger.Information("[Acc #{Id}] Trying select avatar ({Index1}/{Index2})...", habbo.Id, i + 1,
                    habbo.Count);

                if (!await habbo.TrySelectAvatarAsync(habbo[i])) continue;

                _logger.Information("[Acc #{Id} - {AvatarName}] Trying to get SSO Token...", habbo.Id, habbo[i].Name);

                if (!await habbo.TryGetSsoTokenAsync()) continue;

                var bot = new HabboConnection(habbo.CurrentSso);


                if (!await bot.TryConnectAsync())
                {
                    _logger.Error("[Acc #{Id} - {AvatarName}] Disconnected.", habbo.Id, habbo[i].Name);
                }
                else
                {
                    // await Task.Delay(400);

                    // await bot.LoadRoom(_config.RoomId);
                    //
                    await Task.Delay(500);

                    _logger.Information("[Acc #{Id} - {AvatarName}] Sending stars...", habbo.Id, habbo[i].Name);

                    var duckets = await bot.SendStarGemAsync(_config.UserId);

                    _logger.Information("[Acc #{Id} - {AvatarName}] Sent {duckets} stars!", habbo.Id, habbo[i].Name,
                        duckets);


                    // _logger.Information("[Conta #{Id} - {AvatarName}] Entrando no quarto...", habbo.Id, habbo[i].Name);
                    //
                    // await bot.LoadRoom(_config.RoomId);
                    //
                    // await Task.Delay(500);
                    //
                    // if (!String.IsNullOrWhiteSpace(_config.UserName))
                    // {
                    //     if (!bot.IsConnected())
                    //     {
                    //         _logger.Error("[Conta #{Id} - {AvatarName}] Conexão com o servidor perdida.", habbo.Id,
                    //                       habbo[i].Name);
                    //
                    //         break;
                    //     }
                    //
                    //     _logger.Information("[Conta #{Id} - {AvatarName}] Adicionando usuário...", habbo.Id,
                    //                         habbo[i].Name);
                    //
                    //     await bot.AddFriend(_config.UserName);
                    //     await Task.Delay(500);
                    // }
                    //
                    // if (_config.GuildId != 0)
                    // {
                    //     if (!bot.IsConnected())
                    //     {
                    //         _logger.Error("[Conta #{Id} - {AvatarName}] Conexão com o servidor perdida.", habbo.Id,
                    //                       habbo[i].Name);
                    //
                    //         break;
                    //     }
                    //
                    //     _logger.Information("[Conta #{Id} - {AvatarName}] Entrando no grupo...", habbo.Id,
                    //                         habbo[i].Name);
                    //
                    //     await bot.JoinGuild(_config.GuildId);
                    //     await Task.Delay(500);
                    // }

                    // if (_config.UserId != 0)
                    //     for (var j = 0; j < 3; j++)
                    //     {
                    //         if (!bot.IsConnected())
                    //         {
                    //             _logger.Error("[Conta #{Id} - {AvatarName}] Conexão com o servidor perdida.", habbo.Id,
                    //                           habbo[i].Name);
                    //
                    //             break;
                    //         }
                    //
                    //         _logger.Information("[Conta #{Id} - {AvatarName}] Respeitando usuário ({Index1}/{Index2}).",
                    //                             habbo.Id, habbo[i].Name, j + 1, 3);
                    //
                    //         await bot.Respect(_config.UserId);
                    //         await Task.Delay(500);
                    //     }

                    // if (_config.PetId != 0)
                    //     for (var j = 0; j < 3; j++)
                    //     {
                    //         if (!bot.IsConnected())
                    //         {
                    //             _logger.Error("[Conta #{Id} - {AvatarName}] Conexão com o servidor perdida.", habbo.Id,
                    //                           habbo[i].Name);
                    //
                    //             break;
                    //         }
                    //
                    //         _logger.Information("[Conta #{Id} - {AvatarName}] Acariciando mascote ({Index1}/{Index2}).",
                    //                             habbo.Id, habbo[i].Name, j + 1, 3);
                    //
                    //         await bot.Scratch(_config.PetId);
                    //         await Task.Delay(500);
                    //     }
                }

                _logger.Information("[Acc #{Id}] All avatars were used.", habbo.Id);

                habbo.Dispose();
                bot.Dispose();
            }
        }
    }
}