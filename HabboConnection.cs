using System;
using System.Linq;
using System.Threading.Tasks;

using Sulakore.Cryptography;
using Sulakore.Cryptography.Ciphers;
using Sulakore.Network;
using Sulakore.Network.Protocol;

namespace RMass
{
    internal class HabboConnection : IDisposable
    {
        private readonly HKeyExchange                  _keyExchange;
        private readonly Random                        _rand;
        private readonly String                        _sso;
        private          TaskCompletionSource<Boolean> _connected = null!;

        private string _hexKey = string.Empty;

        private HNode                      _server = null!;
        private TaskCompletionSource<bool> _starGemCompletionSource;

        private int duckets;

        public HabboConnection(String sso)
        {
            _sso = sso;

            _keyExchange =
                new HKeyExchange(65537,
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
                _hexKey = GetRandomHexNumber();

                _server               = await HNode.ConnectAsync("game-fr.habbo.com", 30001);
                _server.ReceiveFormat = HFormat.EvaWire;
                _server.SendFormat    = HFormat.EvaWire;
                _server.IsWebSocket   = true;

                await _server.UpgradeWebSocketAsClientAsync();

                await _server.SendAsync(Header.GetOutgoingHeader("Hello"), _hexKey, "UNITY1", 0, 0);

                await _server.SendAsync(Header.GetOutgoingHeader("InitDhHandshake"));

                HandlePacket(await _server.ReceiveAsync());
            }
            catch
            {
                goto Start;
            }

            return await _connected.Task;
        }

        private async void HandlePacket(HPacket hmessage)
        {
            try
            {
                if (hmessage.Id == Header.GetIncomingHeader("Ping"))
                    await SendPong();

                else if (hmessage.Id == Header.GetIncomingHeader("DhCompleteHandshake"))
                    await CryptConnectionAsync(hmessage.ReadUTF8());

                else if (hmessage.Id == Header.GetIncomingHeader("DhInitHandshake"))
                    await VerifyPrimesAsync(hmessage.ReadUTF8(), hmessage.ReadUTF8());

                else if (hmessage.Id == Header.GetIncomingHeader("Ok"))
                    _connected.TrySetResult(true);

                else if (hmessage.Id == Header.GetIncomingHeader("ActivityPointNotification"))
                {
                    duckets = hmessage.ReadInt32();
                    if (_starGemCompletionSource != null) _starGemCompletionSource.TrySetResult(true);
                }

                HandlePacket(await _server.ReceiveAsync());
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public async Task VerifyPrimesAsync(String prime, String generator)
        {
            _keyExchange.VerifyDHPrimes(prime, generator);
            _keyExchange.Padding = PKCSPadding.RandomByte;

            await _server.SendAsync(Header.GetOutgoingHeader("CompleteDhHandshake"), _keyExchange.GetPublicKey());
        }

        private async Task SendPong()
        {
            await _server.SendAsync(Header.GetOutgoingHeader("Pong"));
        }

        public void Disconnect()
        {
            _connected.TrySetResult(false);
        }

        public async Task CryptConnectionAsync(String publicKey)
        {
            var nonce = GetNonce(_hexKey);

            var chacha    = new byte[32];
            var sharedKey = _keyExchange.GetSharedKey(publicKey);

            Buffer.BlockCopy(sharedKey, 0, chacha, 0, sharedKey.Length);

            _server.Decrypter = new ChaCha20(chacha, nonce);
            _server.Encrypter = new ChaCha20(chacha, nonce);

            await SendStuffAsync();
        }

        private async Task SendStuffAsync()
        {
            await _server.SendAsync(Header.GetOutgoingHeader("GetIdentityAgreementTypes"));

            await _server.SendAsync(Header.GetOutgoingHeader("VersionCheck"), 0, HabboConfig.ProductVersion, "");


            await _server.SendAsync(Header.GetOutgoingHeader("UniqueMachineId"), GetRandomHexNumber(76).ToLower(),
                                    "n/a", "Chrome 90", "n/a");


            await _server.SendAsync(Header.GetOutgoingHeader("LoginWithTicket"), _sso, 0);
        }

        public async Task<int> SendStarGemAsync(int userId)
        {
            _starGemCompletionSource = new TaskCompletionSource<bool>();

            await StarGem(userId, 1);

            await _starGemCompletionSource.Task;

            if (duckets < 1) return 1;

            await Task.Delay(400);

            await StarGem(userId, duckets);

            return duckets + 1;
        }

        private async Task StarGem(int userId, int quantity)
        {
            await _server.SendAsync(1505, 0, userId, quantity);
        }

        public async Task Scratch(Int32 petId)
        {
            await _server.SendAsync(Header.GetOutgoingHeader(""));
        }

        public async Task Respect(Int32 id)
        {
            await _server.SendAsync(Header.GetOutgoingHeader(""));
        }

        public async Task LoadRoom(Int32 room)
        {
            await _server.SendAsync(Header.GetOutgoingHeader("FlatOpc"), 0, room, "", -1, -1);
        }

        public async Task AddFriend(String username)
        {
            await _server.SendAsync(Header.GetOutgoingHeader("RequestFriend"), username);
        }

        public async Task JoinGuild(Int32 guildId)
        {
            await _server.SendAsync(Header.GetOutgoingHeader(""));
        }

        private string GetRandomHexNumber(int digits = 24)
        {
            var buffer = new byte[digits / 2];
            _rand.NextBytes(buffer);
            var result = string.Concat(buffer.Select(x => x.ToString("X2")).ToArray());

            if (digits % 2 == 0)
                return result;

            return result + _rand.Next(16).ToString("X");
        }

        private byte[] GetNonce(string str)
        {
            var nonce                         = string.Empty;
            for (var i = 0; i < 8; i++) nonce += str.Substring(i * 3, 2);

            return Convert.FromHexString(nonce);
        }
    }
}