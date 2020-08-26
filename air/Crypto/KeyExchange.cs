using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace com.sulake.habboair
{
    public class KeyExchange : IDisposable
    {
        private const    Int32  BLOCK_SIZE = 256;
        private readonly Random _numberGenerator;

        private KeyExchange()
        {
            _numberGenerator = new Random();
        }

        public KeyExchange( Int32 rsaKeySize ) : this()
        {
            RSA = new RSACryptoServiceProvider(rsaKeySize);
            var keys = RSA.ExportParameters(true);
            Modulus         = new BigInteger(ReverseNull(keys.Modulus));
            Exponent        = new BigInteger(ReverseNull(keys.Exponent));
            PrivateExponent = new BigInteger(ReverseNull(keys.D));
            GenerateDHPrimes(256);
            GenerateDHKeys(DHPrime, DHGenerator);
        }

        public KeyExchange( Int32 exponent, String modulus ) : this(exponent, modulus, String.Empty) { }

        public KeyExchange( Int32 exponent, String modulus, String privateExponent ) : this()
        {
            var keys = new RSAParameters();
            Exponent      = new BigInteger(exponent);
            keys.Exponent = Exponent.ToByteArray();
            Array.Reverse(keys.Exponent);
            Modulus      = BigInteger.Parse("0" + modulus, NumberStyles.HexNumber);
            keys.Modulus = Modulus.ToByteArray();
            Array.Reverse(keys.Modulus);

            if (!String.IsNullOrWhiteSpace(privateExponent))
            {
                PrivateExponent = BigInteger.Parse("0" + privateExponent, NumberStyles.HexNumber);
                keys.D          = PrivateExponent.ToByteArray();
                Array.Reverse(keys.D);

                GenerateDHPrimes(256);
                GenerateDHKeys(DHPrime, DHGenerator);
            }

            RSA = new RSACryptoServiceProvider();
            RSA.ImportParameters(keys);
        }

        public BigInteger               Modulus         { get; }
        public BigInteger               Exponent        { get; }
        public BigInteger               PrivateExponent { get; }
        public RSACryptoServiceProvider RSA             { get; }
        public BigInteger               DHPublic        { get; private set; }
        public BigInteger               DHPrivate       { get; private set; }
        public BigInteger               DHPrime         { get; private set; }
        public BigInteger               DHGenerator     { get; private set; }
        public Boolean                  CanDecrypt      => PrivateExponent != BigInteger.Zero;
        public PKCSPadding              Padding         { get; set; } = PKCSPadding.MaxByte;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual String Encrypt( Func<BigInteger, BigInteger> calculator, BigInteger value )
        {
            var valueData = Encoding.UTF8.GetBytes(value.ToString());
            valueData = PKCSPad(valueData);
            Array.Reverse(valueData);
            var paddedInteger     = new BigInteger(valueData);
            var calculatedInteger = calculator(paddedInteger);
            var paddedData        = calculatedInteger.ToByteArray();
            Array.Reverse(paddedData);
            var encryptedValue = BytesToHex(paddedData).ToLower();

            return encryptedValue.StartsWith("00") ? encryptedValue.Substring(2) : encryptedValue;
        }

        protected virtual BigInteger Decrypt( Func<BigInteger, BigInteger> calculator, String value )
        {
            var signed        = BigInteger.Parse("0" + value, NumberStyles.HexNumber);
            var paddedInteger = calculator(signed);
            var valueData     = paddedInteger.ToByteArray();
            Array.Reverse(valueData);
            valueData = PKCSUnpad(valueData);

            return BigInteger.Parse(Encoding.UTF8.GetString(valueData));
        }

        public virtual BigInteger CalculatePublic( BigInteger value )
        {
            return BigInteger.ModPow(value, Exponent, Modulus);
        }

        public virtual BigInteger CalculatePrivate( BigInteger value )
        {
            return BigInteger.ModPow(value, PrivateExponent, Modulus);
        }

        protected virtual void Dispose( Boolean disposing )
        {
            if (disposing)
                RSA.Dispose();
        }

        protected Byte[] HexToBytes( String value )
        {
            var data = new Byte[value.Length / 2];

            for (var i = 0; i < value.Length; i += 2)
                data[i / 2] = Convert.ToByte(value.Substring(i, 2), 16);

            return data;
        }

        protected String BytesToHex( Byte[] value )
        {
            return BitConverter.ToString(value).Replace("-", String.Empty);
        }

        protected BigInteger RandomInteger( Int32 bitSize )
        {
            var integerData = new Byte[bitSize / 8];
            _numberGenerator.NextBytes(integerData);
            integerData[integerData.Length - 1] &= 0x7f;

            return new BigInteger(integerData);
        }

        public virtual String GetSignedP()
        {
            return Sign(DHPrime);
        }

        public virtual String GetSignedG()
        {
            return Sign(DHGenerator);
        }

        public virtual String GetPublicKey()
        {
            return CanDecrypt ? Sign(DHPublic) : Encrypt(DHPublic);
        }

        public virtual Byte[] GetSharedKey( String publicKey )
        {
            var aKey      = CanDecrypt ? Decrypt(publicKey) : Verify(publicKey);
            var sharedKey = BigInteger.ModPow(aKey, DHPrivate, DHPrime).ToByteArray();
            Array.Reverse(sharedKey);

            return sharedKey;
        }

        public virtual void VerifyDHPrimes( String p, String g )
        {
            DHPrime = Verify(p);

            if (DHPrime <= 2)
                throw new ArgumentException("P cannot be less than, or equal to 2.\r\n" + DHPrime, nameof(DHPrime));

            DHGenerator = Verify(g);

            if (DHGenerator >= DHPrime)
                throw new ArgumentException($"G cannot be greater than, or equal to P.\r\n{DHPrime}\r\n{DHGenerator}",
                                            nameof(DHGenerator));

            GenerateDHKeys(DHPrime, DHGenerator);
        }

        protected String Sign( BigInteger value )
        {
            return Encrypt(CalculatePrivate, value);
        }

        protected BigInteger Verify( String value )
        {
            return Decrypt(CalculatePublic, value);
        }

        protected String Encrypt( BigInteger value )
        {
            return Encrypt(CalculatePublic, value);
        }

        protected BigInteger Decrypt( String value )
        {
            return Decrypt(CalculatePrivate, value);
        }

        protected virtual Byte[] PKCSPad( Byte[] data )
        {
            var buffer       = new Byte[BLOCK_SIZE - 1];
            var dataStartPos = buffer.Length - data.Length;
            buffer[0] = (Byte) Padding;

            //
            Buffer.BlockCopy(data, 0, buffer, dataStartPos, data.Length);
            // var _loc7_:int = param2 - 1;
            var paddingEndPos = dataStartPos - 1;
            var isRandom      = Padding == PKCSPadding.RandomByte;

            for (var i = 1; i < paddingEndPos; i++)
                buffer[i] = (Byte) (isRandom ? _numberGenerator.Next(1, 256) : Byte.MaxValue);

            return buffer;
        }

        protected virtual Byte[] PKCSUnpad( Byte[] data )
        {
            Padding = (PKCSPadding) data[0];
            var position = 0;
            while (data[position++] != 0) ;
            var buffer = new Byte[data.Length - position];
            Buffer.BlockCopy(data, position, buffer, 0, buffer.Length);

            return buffer;
        }

        protected virtual void GenerateDHPrimes( Int32 bitSize )
        {
            DHPrime     = RandomInteger(bitSize);
            DHGenerator = RandomInteger(bitSize);

            if (DHGenerator > DHPrime)
            {
                var tempG = DHGenerator;
                DHGenerator = DHPrime;
                DHPrime     = tempG;
            }
        }

        protected virtual void GenerateDHKeys( BigInteger p, BigInteger g )
        {
            DHPrivate = RandomInteger(256);
            DHPublic  = BigInteger.ModPow(g, DHPrivate, p);
        }

        private Byte[] ReverseNull( Byte[] data )
        {
            var isNegative = false;
            var newSize    = data.Length;

            if (data[0] > 127)
            {
                newSize    += 1;
                isNegative =  true;
            }

            var reversed = new Byte[newSize];

            for (var i = 0; i < data.Length; i++)
                reversed[i] = data[data.Length - (i + 1)];

            if (isNegative)
                reversed[reversed.Length - 1] = 0;

            return reversed;
        }
    }
}