using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace com.sulake.habboair
{
    public class HNode : IDisposable
    {
        private static readonly Dictionary<Int32, TcpListener> _listeners;

        static HNode()
        {
            _listeners = new Dictionary<Int32, TcpListener>();
        }

        public HNode() : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) { }

        public HNode( Socket client )
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            if (client.RemoteEndPoint != null) EndPoint = new HotelEndPoint((IPEndPoint) client.RemoteEndPoint);

            Client         = client;
            Client.NoDelay = true;
        }

        public Boolean IsConnected => Client.Connected;

        public Socket        Client   { get; }
        public HotelEndPoint EndPoint { get; private set; }

        public String     Username       { get; set; }
        public String     Password       { get; set; }
        public IPEndPoint SOCKS5EndPoint { get; set; }

        public RC4     Encrypter    { get; set; }
        public Boolean IsEncrypting { get; set; }

        public RC4     Decrypter    { get; set; }
        public Boolean IsDecrypting { get; set; }

        public void Dispose()
        {
            Dispose(true);
        }

        private async Task<Boolean> ConnectAsync()
        {
            var connected = true;

            try
            {
                var endPoint = SOCKS5EndPoint ?? EndPoint;
                var result   = Client.BeginConnect(endPoint, null, null);
                await Task.Factory.FromAsync(result, Client.EndConnect).ConfigureAwait(false);

                if (!Client.Connected) return connected = false;

                if (SOCKS5EndPoint != null)
                {
                    await SendAsync(new Byte[]
                        {
                            0x05, // Version 5
                            0x02, // 2 Authentication Methods Present
                            0x00, // No Authentication
                            0x02  // Username + Password
                        })
                        .ConfigureAwait(false);

                    var response = await ReceiveAsync(2).ConfigureAwait(false);

                    if (response?.Length != 2 || response[1] == 0xFF) return connected = false;

                    var    index   = 0;
                    Byte[] payload = null;

                    if (response[1] == 0x02) // Username + Password Required
                    {
                        index            = 0;
                        payload          = new Byte[Byte.MaxValue];
                        payload[index++] = 0x01;

                        // Username
                        payload[index++] = (Byte) Username.Length;
                        var usernameData = Encoding.Default.GetBytes(Username);
                        Buffer.BlockCopy(usernameData, 0, payload, index, usernameData.Length);
                        index += usernameData.Length;

                        // Password
                        payload[index++] = (Byte) Password.Length;
                        var passwordData = Encoding.Default.GetBytes(Password);
                        Buffer.BlockCopy(passwordData, 0, payload, index, passwordData.Length);
                        index += passwordData.Length;

                        await SendAsync(payload, index).ConfigureAwait(false);
                        response = await ReceiveAsync(2).ConfigureAwait(false);

                        if (response?.Length != 2 || response[1] != 0x00) return connected = false;
                    }

                    index            = 0;
                    payload          = new Byte[255];
                    payload[index++] = 0x05;
                    payload[index++] = 0x01;
                    payload[index++] = 0x00;

                    payload[index++] = (Byte) (EndPoint.AddressFamily == AddressFamily.InterNetwork ? 0x01 : 0x04);

                    // Destination Address
                    var addressBytes = EndPoint.Address.GetAddressBytes();
                    Buffer.BlockCopy(addressBytes, 0, payload, index, addressBytes.Length);
                    index += (UInt16) addressBytes.Length;

                    var portData = BitConverter.GetBytes((UInt16) EndPoint.Port);

                    if (BitConverter.IsLittleEndian)
                        // Big-Endian Byte Order
                        Array.Reverse(portData);

                    Buffer.BlockCopy(portData, 0, payload, index, portData.Length);
                    index += portData.Length;

                    await SendAsync(payload, index);
                    response = await ReceiveAsync(Byte.MaxValue);

                    if (response?.Length < 2 || response[1] != 0x00) return connected = false;
                }
            }
            catch
            {
                return connected = false;
            }
            finally
            {
                if (!connected) Disconnect();
            }

            return IsConnected;
        }

        public Task<Boolean> ConnectAsync( IPEndPoint endpoint )
        {
            EndPoint = endpoint as HotelEndPoint;
            if (EndPoint == null) EndPoint = new HotelEndPoint(endpoint);

            return ConnectAsync();
        }

        public Task<Boolean> ConnectAsync( String host, Int32 port )
        {
            return ConnectAsync(HotelEndPoint.Parse(host, port));
        }

        public Task<Boolean> ConnectAsync( IPAddress address, Int32 port )
        {
            return ConnectAsync(new HotelEndPoint(address, port));
        }

        public Task<Boolean> ConnectAsync( IPAddress[] addresses, Int32 port )
        {
            return ConnectAsync(new HotelEndPoint(addresses[0], port));
        }

        public async Task<HMessage> ReceivePacketAsync()
        {
            var lengthBlock = await AttemptReceiveAsync(4, 3).ConfigureAwait(false);

            if (lengthBlock == null)
            {
                Disconnect();

                return null;
            }

            var body = await AttemptReceiveAsync(BigEndian.ToInt32(lengthBlock, 0), 3).ConfigureAwait(false);

            if (body == null)
            {
                Disconnect();

                return null;
            }

            var data = new Byte[4 + body.Length];
            Buffer.BlockCopy(lengthBlock, 0, data, 0, 4);
            Buffer.BlockCopy(body, 0, data, 4, body.Length);

            return new HMessage(data);
        }

        public Task<Int32> SendPacketAsync( HMessage packet )
        {
            return SendAsync(packet.ToBytes());
        }

        public Task<Int32> SendPacketAsync( String signature )
        {
            return SendAsync(HMessage.ToBytes(signature));
        }

        public Task<Int32> SendPacketAsync( UInt16 id, params Object[] values )
        {
            return SendAsync(HMessage.Construct(id, values));
        }

        public Task<Int32> SendAsync( Byte[] buffer )
        {
            return SendAsync(buffer, buffer.Length);
        }

        public Task<Int32> SendAsync( Byte[] buffer, Int32 size )
        {
            return SendAsync(buffer, 0, size);
        }

        public Task<Int32> SendAsync( Byte[] buffer, Int32 offset, Int32 size )
        {
            return SendAsync(buffer, offset, size, SocketFlags.None);
        }

        public Task<Byte[]> ReceiveAsync( Int32 size )
        {
            return ReceiveBufferAsync(size, SocketFlags.None);
        }

        public Task<Int32> ReceiveAsync( Byte[] buffer )
        {
            return ReceiveAsync(buffer, buffer.Length);
        }

        public Task<Int32> ReceiveAsync( Byte[] buffer, Int32 size )
        {
            return ReceiveAsync(buffer, 0, size);
        }

        public Task<Int32> ReceiveAsync( Byte[] buffer, Int32 offset, Int32 size )
        {
            return ReceiveAsync(buffer, offset, size, SocketFlags.None);
        }

        public async Task<Byte[]> AttemptReceiveAsync( Int32 size, Int32 attempts )
        {
            var totalBytesRead     = 0;
            var data               = new Byte[size];
            var nullBytesReadCount = 0;

            do
            {
                var bytesLeft = data.Length - totalBytesRead;
                var bytesRead = await ReceiveAsync(data, totalBytesRead, bytesLeft).ConfigureAwait(false);

                if (IsConnected && bytesRead > 0)
                {
                    nullBytesReadCount =  0;
                    totalBytesRead     += bytesRead;
                }
                else if (!IsConnected || ++nullBytesReadCount >= attempts) return null;
            } while (totalBytesRead != data.Length);

            return data;
        }

        public Task<Byte[]> PeekAsync( Int32 size )
        {
            return ReceiveBufferAsync(size, SocketFlags.Peek);
        }

        public Task<Int32> PeekAsync( Byte[] buffer )
        {
            return PeekAsync(buffer, buffer.Length);
        }

        public Task<Int32> PeekAsync( Byte[] buffer, Int32 size )
        {
            return PeekAsync(buffer, 0, size);
        }

        public Task<Int32> PeekAsync( Byte[] buffer, Int32 offset, Int32 size )
        {
            return ReceiveAsync(buffer, offset, size, SocketFlags.Peek);
        }

        protected async Task<Byte[]> ReceiveBufferAsync( Int32 size, SocketFlags socketFlags )
        {
            var buffer = new Byte[size];
            var read   = await ReceiveAsync(buffer, 0, size, socketFlags).ConfigureAwait(false);

            if (read == -1) return null;

            var trimmedBuffer = new Byte[read];
            Buffer.BlockCopy(buffer, 0, trimmedBuffer, 0, read);

            return trimmedBuffer;
        }

        protected async Task<Int32> SendAsync( Byte[] buffer, Int32 offset, Int32 size, SocketFlags socketFlags )
        {
            if (!IsConnected) return -1;

            if (IsEncrypting && Encrypter != null) buffer = Encrypter.Parse(buffer);
            var sent                                      = -1;

            try
            {
                var result = Client.BeginSend(buffer, offset, size, socketFlags, null, null);
                sent = await Task.Factory.FromAsync(result, Client.EndSend).ConfigureAwait(false);
            }
            catch
            {
                sent = -1;
            }

            return sent;
        }

        protected async Task<Int32> ReceiveAsync( Byte[] buffer, Int32 offset, Int32 size, SocketFlags socketFlags )
        {
            if (!IsConnected) return -1;

            if (buffer == null)
                throw new NullReferenceException("Buffer cannot be null.");

            if (buffer.Length == 0 || size == 0) return 0;

            var read = -1;

            try
            {
                var result = Client.BeginReceive(buffer, offset, size, socketFlags, null, null);
                read = await Task.Factory.FromAsync(result, Client.EndReceive).ConfigureAwait(false);
            }
            catch
            {
                read = -1;
            }

            if (read > 0 && IsDecrypting && Decrypter != null)
                Decrypter.RefParse(buffer, offset, read, socketFlags.HasFlag(SocketFlags.Peek));

            return read;
        }

        public void Disconnect()
        {
            if (IsConnected)
                try
                {
                    Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, false);
                    Client.Shutdown(SocketShutdown.Both);
                    Client.Disconnect(false);
                }
                catch { }
        }

        protected virtual void Dispose( Boolean disposing )
        {
            if (disposing)
            {
                if (IsConnected)
                    try
                    {
                        Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                        Client.Shutdown(SocketShutdown.Both);
                    }
                    catch { }

                Client.Close();
            }
        }

        public static void StopListeners( Int32? port = null )
        {
            foreach (var listener in _listeners.Values)
            {
                if (port != null)
                    if (port != ((IPEndPoint) listener.LocalEndpoint).Port)
                        continue;

                listener.Stop();
            }
        }

        public static async Task<HNode> AcceptAsync( Int32 port )
        {
            TcpListener listener = null;

            if (!_listeners.TryGetValue(port, out listener))
            {
                listener = new TcpListener(IPAddress.Any, port);
                _listeners.Add(port, listener);
            }

            try
            {
                listener.Start();
                var client = await listener.AcceptSocketAsync().ConfigureAwait(false);

                return new HNode(client);
            }
            finally
            {
                listener.Stop();
                if (_listeners.ContainsKey(port)) _listeners.Remove(port);
            }
        }

        public static Task<HNode> ConnectNewAsync( String host, Int32 port )
        {
            return ConnectNewAsync(HotelEndPoint.Parse(host, port));
        }

        public static async Task<HNode> ConnectNewAsync( IPEndPoint endpoint )
        {
            HNode remote = null;

            try
            {
                remote = new HNode();
                await remote.ConnectAsync(endpoint).ConfigureAwait(false);
            }
            catch
            {
                remote = null;
            }
            finally
            {
                if (!remote?.IsConnected ?? false) remote = null;
            }

            return remote;
        }

        public static Task<HNode> ConnectNewAsync( IPAddress address, Int32 port )
        {
            return ConnectNewAsync(new HotelEndPoint(address, port));
        }

        public static Task<HNode> ConnectNewAsync( IPAddress[] addresses, Int32 port )
        {
            return ConnectNewAsync(new HotelEndPoint(addresses[0], port));
        }
    }
}