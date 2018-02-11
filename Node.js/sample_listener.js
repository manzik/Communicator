var fs=require("fs");

var Communicator = require("./Library/Communicator/index.js");

var communicator = new Communicator();

communicator.config({password: "youcanchangethis" });

communicator.listen(1234).then(() =>
{
    communicator.on("TestMessage", (obj) =>
    {
        console.log("A program from "+obj.lang+" "+obj.langversion+" on "+obj.os+" "+obj.osversion+" connected");
        if(obj.senderimg)
        {
        fs.writeFileSync(__dirname+"/ReceivedImage.png",obj.senderimg);
        console.log("An image was received and saved to: ReceivedImage.png");
        }
        if(obj.msg)
        {
        console.log("Received a message:");
        console.log(obj.msg);
        }
    });
    
});
