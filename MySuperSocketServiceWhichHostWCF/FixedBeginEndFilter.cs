using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.Facility.Protocol;
using SuperSocket.SocketBase.Protocol;

namespace MyRouteService
{
    class FixedBeginEndFilter : BeginEndMarkReceiveFilter<StringRequestInfo>
    {
        //new byte[] { (byte)'!' };
        private readonly static byte[] BeginMark = Encoding.ASCII.GetBytes(@"<cmd>");
        //new byte[] { (byte)@"<cmd>" };
        private readonly static byte[] EndMark = Encoding.ASCII.GetBytes(@"</cmd>");
        //new byte[] { 0x5d, 0x5d };

        private BasicRequestInfoParser m_Parser = new BasicRequestInfoParser(";", ",");

        public FixedBeginEndFilter()
            : base(BeginMark, EndMark)
        {

        }

        protected override StringRequestInfo ProcessMatchedRequest(byte[] readBuffer, int offset, int length)
        {
            if (length < 20)
            {
                Console.WriteLine("Ignore request");
                return NullRequestInfo;
            }

            var line = Encoding.ASCII.GetString(readBuffer, offset, length);
            return m_Parser.ParseRequestInfo(line.Substring(5, line.Length - 11));
        }

    }
}
