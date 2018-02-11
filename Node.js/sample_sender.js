var os=require("os");
var fs=require("fs");

var Communicator = require("./Library/Communicator/index.js");

var communicator = new Communicator();

communicator.config({ password: "youcanchangethis"});

communicator.setdefaultreceiver("127.0.0.1", 1234);

var osnames={"win32":"Windows","drawin":"MacOS","linux":"Linux"};

var OS=
{
  platform:osnames[os.platform()],
  version:os.release()
};

var Lang=
{
  name:"Node.js",
  version:process.versions.node
}

var message="Hello! ;)";

var sendingImage=fs.readFileSync("./logo image/nodejs logo.png");

communicator.send({msg:message,os:OS.platform,osversion:OS.version,lang:Lang.name,langversion:Lang.version,senderimg:sendingImage}, { type: "TestMessage"}).then(() =>
{
  console.log("Message was sent"); 
})