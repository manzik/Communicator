import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;


public class Sender {
	public static class message
	{
		public String os;
		public String osversion;
		public String lang;
		public String langversion;
		public String msg;
		public byte[] senderimg;
	}
	public static void main(String[] args) throws IOException {
		// TODO Auto-generated method stub
		CommunicatorOptions co=new CommunicatorOptions();
		co.password="youcanchangethis";
		
	Communicator communicator=new Communicator(co);
	
	communicator.SetDefaultReceiver("127.0.0.1", 1234);
	
	message msg=new message();
	
	msg.lang="Java";
	msg.langversion=Runtime.class.getPackage().getImplementationVersion();
	msg.os=System.getProperty("os.name");
	msg.os=msg.os.substring(0,msg.os.indexOf(" "));
	msg.osversion=System.getProperty("os.version").toString();
	msg.msg="Hello! ;)";
	msg.senderimg=Files.readAllBytes(Paths.get(new File("logo image/java logo.png").getAbsolutePath()));

	communicator.Send((Object)msg, "TestMessage");
	
	System.out.print("Message was sent");
	
	}

}
