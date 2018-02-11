using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Communicator;

// value for properties that you want to use later should be declared in class
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

            communicator.listen(1234);

            communicator.On("TestMessage", data => {

                Message msg = new Message();

                CommunicatorTools.CopyToObject(msg, data);
                

                Console.WriteLine("A program from " + msg.lang + " " + msg.langversion + " on " + msg.os + " " + msg.osversion + " connected");

                File.WriteAllBytes("ReceivedImage.png", msg.senderimg);

                Console.WriteLine("An image was received and saved to: ReceivedImage.png");

                Console.WriteLine("Received a message:");

                Console.WriteLine(msg.msg);
            });

            Console.ReadKey();
        }
    }
}
