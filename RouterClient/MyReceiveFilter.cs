using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ClientEngine;
using SuperSocket.ProtoBase;


namespace RouterClient
{
    class MyReceiveFilter : BeginEndMarkReceiveFilter<StringPackageInfo>
    {
        private readonly static byte[] BeginMark = Encoding.ASCII.GetBytes(@"<reply>");
        //new byte[] { (byte)@"<cmd>" };
        private readonly static byte[] EndMark = Encoding.ASCII.GetBytes(@"</reply>");
        public MyReceiveFilter()
        : base(BeginMark, EndMark) // two vertical bars as package terminator
        {
        }
        //StringPackageInfo
        public override StringPackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            //var line = Encoding.ASCII.GetString(bufferStream.Buffers[0].Array, 0, bufferStream.Buffers[0].Count);
            var line = bufferStream.ReadString(int.Parse(bufferStream.Length.ToString()), Encoding.UTF8);

            BasicStringParser m_Parser = new BasicStringParser(";", ",");

            //BasicStringParser m_Parser = new BasicStringParser("@", ",");


            //StringPackageInfo si = new StringPackageInfo(line.ToString(), m_Parser);
            StringPackageInfo si = new StringPackageInfo(line.Substring(7, line.Length - 15), m_Parser);

            return si;

        }
        // other code you need implement according yoru protocol details


    }
    //class MyReceiveFilter : BeginEndMarkReceiveFilter<StringPackageInfo>
    //{
    //    public MyReceiveFilter()
    //        : base(BeginMark, EndMark)// two vertical bars as package terminator
    //    {
    //    }

    //    //new byte[] { (byte)'!' };
    //    private readonly static byte[] BeginMark = Encoding.ASCII.GetBytes(@"<cmd>");
    //    //new byte[] { (byte)@"<cmd>" };
    //    private readonly static byte[] EndMark = Encoding.ASCII.GetBytes(@"</cmd>");
    //    //new byte[] { 0x5d, 0x5d };

    //    private BasicRequestInfoParser m_Parser = new BasicRequestInfoParser(":", ",");


    //    protected override StringPackageInfo ProcessMatchedRequest(byte[] readBuffer, int offset, int length)
    //    {
    //        if (length < 20)
    //        {
    //            Console.WriteLine("Ignore request");
    //            return NullRequestInfo;
    //        }

    //        var line = Encoding.ASCII.GetString(readBuffer, offset, length);
    //        return m_Parser.ParseRequestInfo(line.Substring(5, line.Length - 11));
    //    }

    //    public override StringPackageInfo ResolvePackage(IBufferStream bufferStream)
    //    {
    //        return null;

    //    }

    //}
}
