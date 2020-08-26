using System;

namespace com.sulake.habboair
{
    public class RC4
    {
        private readonly Object  _parseLock;
        private readonly Int32[] _table;
        private          Int32   _i, _j;

        public RC4( Byte[] key )
        {
            _table     = new Int32[256];
            _parseLock = new Object();

            Key = key;

            for (var i = 0; i < 256; i++) _table[i] = i;

            for (Int32 j = 0, x = 0; j < _table.Length; j++)
            {
                x += _table[j];
                x += key[j % key.Length];
                x %= _table.Length;
                Swap(j, x);
            }
        }

        public Byte[] Key { get; }

        public Byte[] Parse( Byte[] data )
        {
            lock (_parseLock)
            {
                var parsed = new Byte[data.Length];

                for (var k = 0; k < data.Length; k++)
                {
                    _i++;
                    _i %= _table.Length;
                    _j += _table[_i];
                    _j %= _table.Length;
                    Swap(_i, _j);

                    var rightXOR = _table[_i] + _table[_j];
                    rightXOR = _table[rightXOR % _table.Length];

                    parsed[k] = (Byte) (data[k] ^ rightXOR);
                }

                return parsed;
            }
        }

        public void RefParse( Byte[] data )
        {
            RefParse(data, 0, data.Length);
        }

        public void RefParse( Byte[] data, Int32 length )
        {
            RefParse(data, 0, length);
        }

        public void RefParse( Byte[] data, Int32 offset, Int32 length )
        {
            RefParse(data, offset, length, false);
        }

        public void RefParse( Byte[] data, Int32 offset, Int32 length, Boolean isPeeking )
        {
            lock (_parseLock)
            {
                var     i    = _i;
                var     j    = _j;
                Int32[] pool = null;

                if (isPeeking)
                {
                    pool = new Int32[_table.Length];
                    Array.Copy(_table, pool, pool.Length);
                }

                for (Int32 k = offset, l = 0; l < length; k++, l++)
                {
                    _i++;
                    _i %= _table.Length;
                    _j += _table[_i];
                    _j %= _table.Length;
                    Swap(_i, _j);

                    var rightXOR = _table[_i] + _table[_j];
                    rightXOR = _table[rightXOR % _table.Length];

                    data[k] ^= (Byte) rightXOR;
                }

                if (isPeeking)
                {
                    _i = i;
                    _j = j;
                    Array.Copy(pool, _table, _table.Length);
                }
            }
        }

        private void Swap( Int32 a, Int32 b )
        {
            var temp = _table[a];
            _table[a] = _table[b];
            _table[b] = temp;
        }
    }
}