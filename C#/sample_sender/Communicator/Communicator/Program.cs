using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Communicator;

public class Message
{
    public String os = "";
    public String osversion = "";
    public String lang = "";
    public String langversion = "";
    public String msg = "";
    public byte[] senderimg= new byte[] { };
}

namespace Communicator
{
    

    class Program
    {



        
        static void Main(string[] args)
        {
            CommunicatorOptions co = new CommunicatorOptions();
            co.password = "youcanchangethis";
            
            Communicator communicator = new Communicator(co);

            communicator.SetDefaultReceiver("127.0.0.1", 1234);

            Message msg = new Message();

            OperatingSystem os_info = System.Environment.OSVersion;

            msg.lang = "C#";
            msg.langversion = Environment.Version.ToString();
            msg.os = "Windows "+GetOsName(os_info);
            msg.osversion = os_info.VersionString;

            msg.msg = "Hello! ;)";

            msg.senderimg= File.ReadAllBytes("logo image\\c# logo.png"); ;

            communicator.Send(msg,"TestMessage");

            Console.ReadKey();
        }

        //http://csharphelper.com/blog/2017/10/get-the-computers-operating-system-in-c/
        // Return the OS name.
        public static string GetOsName(OperatingSystem os_info)
        {
            string version =
                os_info.Version.Major.ToString() + "." +
                os_info.Version.Minor.ToString();
            switch (version)
            {
                case "10.0": return "10/Server 2016";
                case "6.3": return "8.1/Server 2012 R2";
                case "6.2": return "8/Server 2012";
                case "6.1": return "7/Server 2008 R2";
                case "6.0": return "Server 2008/Vista";
                case "5.2": return "Server 2003 R2/Server 2003/XP 64-Bit Edition";
                case "5.1": return "XP";
                case "5.0": return "2000";
            }
            return "Unknown";
        }
    }
}

