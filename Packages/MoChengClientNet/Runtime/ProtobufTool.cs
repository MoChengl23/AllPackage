using Pb;
using System;
using Google.Protobuf;
using System.IO;
namespace KCPNET
{
    public class ProtobufTool
    {
        public static PbMessage  ByteToPb(byte[] bytes)
        {
            PbMessage pbMessage = new PbMessage();
            try{
                pbMessage = PbMessage.Parser.ParseFrom(bytes);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return pbMessage;
        }
        public static byte[] PbToByte(PbMessage pbMessage)
        {
            byte[] bytes;
            using (MemoryStream stream = new MemoryStream())
            {

                pbMessage.WriteTo(stream);
                //动态地获得转码后 byte的长度
                bytes = stream.ToArray();
                 
            }
            return bytes;
            
        }

    }
    
}

//  dotnet build -o D:\1\Dll 