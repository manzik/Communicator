import imp
import multiprocessing
import time
import communicator
import csv
import os.path
import platform


communicator = communicator.Communicator("youcanchangethis")
communicator.setdefaultreceiver("127.0.0.1",1234)

message="Hello! ;)"

OS={
  
  "name" : platform.system(),
  "version" : platform.release()
}

Lang={
  "name" : "Python",
  "version" : platform.python_version()
}

sendingImage=buffer(open("./logo image/python logo.png", "rb").read())

communicator.send({"msg":message,"os":OS["name"],"osversion":OS["version"],"lang":Lang["name"],"langversion":Lang["version"],"senderimg":sendingImage  },"TestMessage")
  
print ("Message was sent")

raw_input()