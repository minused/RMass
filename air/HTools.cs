using System;
using System.Security.Cryptography;
using System.Text;

namespace com.sulake.habboair
{
    public static class HabboTools
    {
        public static String ToMD5( Byte[] data )
        {
            var sb = String.Empty;

            for (var i = 0; i < data.Length; i++)
                sb += data[i].ToString("x2");

            return sb;
        }

        public static String MachineHash()
        {
            var _loc8_ = "Mozilla/5.0 (Android; U; pt-BR) AppleWebKit/533.19.4 (KHTML, like Gecko) AdobeAIR/30.0";
            var _loc3_ = "Algerian,Almanac MT,Arial,Arial Black,Impact,Calibri";
            var md5    = MD5.Create();

            {
                var hash = ToMD5(md5.ComputeHash(Encoding.UTF8.GetBytes(_loc8_
                                                                      + "#"
                                                                      + 2
                                                                      + "#"
                                                                      + DateTime.Now
                                                                      + "#"
                                                                      + new Random().Next(0, 5)
                                                                      + "#"
                                                                      + _loc3_)));

                return "~" + hash;
            }
        }
    }
}