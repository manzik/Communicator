import java.io.FileOutputStream;
import java.io.IOException;

public class Listener {

	public static class message
	{
		String os;
		String osversion;
		String lang;
		String langversion;
		String msg=null;
		byte[] senderimg=null;
	}
	public static void main(String[] args) {
		CommunicatorOptions co=new CommunicatorOptions();
		co.password="youcanchangethis";
		
	Communicator communicator=new Communicator(co);
	
	communicator.Listen(1234);
	
	communicator.On("TestMessage", new Communicator.Callback <CommunicatorObject>() {

		@Override
		public void call(CommunicatorObject obj) {
			message msg=new message();
			obj.CopyToObject(msg);
			System.out.println("A program from "+msg.lang+" "+msg.langversion+" on "+msg.os+" "+msg.osversion+" connected");
			if(msg.senderimg!=null)
			{
				try (FileOutputStream fos = new FileOutputStream("ReceivedImage.png")) {
					   fos.write(msg.senderimg);
					   fos.close();
					} catch (IOException e) {
						// TODO Auto-generated catch block
						e.printStackTrace();
					}
				System.out.println("An image was received and saved to: ReceivedImage.png");
			}
			if(msg.msg!=null)
			{
				System.out.println("Received a message:");
				System.out.println(msg.msg);
			}
		}
	});
		// TODO Auto-generated method stub
		
	}

}
