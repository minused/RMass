using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using com.sulake.habboair;

namespace RMass
{
    internal class HabboConnection : IDisposable
    {
        private readonly KeyExchange                   _keyExchange;
        private readonly Random                        _rand;
        private readonly String                        _sso;
        private          TaskCompletionSource<Boolean> _connected = null!;

        private RC4 _crypto = null!;

        private HNode _server = null!;

        public HabboConnection( String sso )
        {
            _sso = sso;

            _keyExchange =
                new KeyExchange(65537,
                                "bd214e4f036d35b75fee36000f24ebbef15d56614756d7afbd4d186ef5445f758b284647feb773927418ef70b95387b80b961ea56d8441d410440e3d3295539a3e86e7707609a274c02614cc2c7df7d7720068f072e098744afe68485c6297893f3d2ba3d7aaaaf7fa8ebf5d7af0ba2d42e0d565b89d332de4cf898d666096ce61698de0fab03a8a5e12430cb427c97194cbd221843d162c9f3acf74da1d80ebc37fde442b68a0814dfea3989fdf8129c120a8418248d7ee85d0b79fa818422e496d6fa7b5bd5db77e588f8400cda1a8d82efed6c86b434bafa6d07dfcc459d35d773f8dfaf523dfed8fca45908d0f9ed0d4bceac3743af39f11310eaf3dff45");

            _rand = new Random();
        }

        public void Dispose()
        {
            _keyExchange?.Dispose();
            _server?.Dispose();
        }

        public Boolean IsConnected()
        {
            return _server.IsConnected;
        }

        public async Task<Boolean> TryConnectAsync()
        {
            _connected = new TaskCompletionSource<Boolean>();
            Start:

            try
            {
                _server = new HNode();

                await _server.ConnectAsync("game-br.habbo.com", 30000);
                await _server.SendPacketAsync(Headers.ReleaseVersion, Headers.Variables.Production, "FLASH", 1, 0);
                await _server.SendPacketAsync(Headers.InitCrypto);

                HandlePacket(await _server.ReceivePacketAsync());
            }
            catch
            {
                goto Start;
            }

            return await _connected.Task;
        }

        private async void HandlePacket( HMessage hmessage )
        {
            try
            {
                if (hmessage.Header == Headers.Ping)
                    await SendPong();
                else if (hmessage.Header == Headers.InGenerateSecretKey)
                    await GenerateSecretKey(hmessage.ReadString());
                else if (hmessage.Header == Headers.VerifyPrimes)
                    await VerifyPrimes(hmessage.ReadString(), hmessage.ReadString());

                HandlePacket(await _server.ReceivePacketAsync());
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public async Task VerifyPrimes( String prime, String generator )
        {
            _keyExchange.VerifyDHPrimes(prime, generator);
            _keyExchange.Padding = PKCSPadding.RandomByte;
            await SendToServer(HMessage.Construct(Headers.OutGenerateSecretKey, _keyExchange.GetPublicKey()));
        }

        private async Task SendPong()
        {
            await SendToServerCrypto(HMessage.Construct(Headers.Pong));
        }

        private async Task SendToServer( Byte[] parse )
        {
            try
            {
                await _server.SendAsync(parse);
            }
            catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            _server.Disconnect();
            _connected.TrySetResult(false);
        }

        public async Task GenerateSecretKey( String publicKey )
        {
            _crypto = new RC4(_keyExchange.GetSharedKey(publicKey));

            await SendToServer(_crypto.Parse(HMessage.Construct(Headers.ClientVariables, 401,
                                                                Headers.Variables.ClientVariables1,
                                                                Headers.Variables.ClientVariables2)));

            await SendToServer(_crypto.Parse(HMessage.Construct(Headers.MachineID, "", "~" + GenMid(),
                                                                "WIN/32,0,0,403")));

            await SendToServer(_crypto.Parse(HMessage.Construct(Headers.SSOTicket, _sso, _rand.Next(3000, 5000))));
            await SendToServer(_crypto.Parse(HMessage.Construct(Headers.RequestUserData)));

            _connected.TrySetResult(true);
        }

        private async Task SendToServerCrypto( Byte[] data )
        {
            _crypto.RefParse(data);

            await SendToServer(data);
        }

        private static String GenMid( Int32 length = 32 )
        {
            using var rngProvider = new RNGCryptoServiceProvider();

            var bytes = new Byte[length];
            rngProvider.GetBytes(bytes);

            using var md5 = MD5.Create();

            var md5Hash = md5.ComputeHash(bytes);

            var sb = new StringBuilder();
            foreach (var data in md5Hash) sb.Append(data.ToString("x2"));

            return sb.ToString();
        }

        public async Task Scratch( Int32 petId )
        {
            await SendToServerCrypto(HMessage.Construct(Headers.ScratchPet, petId));
        }

        public async Task Respect( Int32 id )
        {
            await SendToServerCrypto(HMessage.Construct(Headers.RoomUserGiveRespect, id));
        }

        public async Task LoadRoom( Int32 room )
        {
            await SendToServerCrypto(HMessage.Construct(Headers.RequestRoomLoad, room, String.Empty, -1));
        }

        public async Task AddFriend( String username )
        {
            await SendToServerCrypto(HMessage.Construct(Headers.FriendRequest, username));
        }

        public async Task JoinGuild( Int32 guildId )
        {
            await SendToServerCrypto(HMessage.Construct(Headers.RequestGuildJoin, guildId));
        }
    }
}