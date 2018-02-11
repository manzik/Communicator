import imp
import multiprocessing
import time
import communicator
import csv
import os.path

def onmsg(obj):

  print "A program from "+obj["lang"]+" "+obj["langversion"]+" on "+obj["os"]+" "+obj["osversion"]+" connected"

  if(obj["senderimg"]):
    newimg=open("ReceivedImage.png","wb")
    newimg.write(obj["senderimg"])  
    print ("An image was received and saved to: ReceivedImage.png")
  
  if(obj["msg"]):
    print ("Received a message:")
    print (obj["msg"])
  
communicator = communicator.Communicator("youcanchangethis")


communicator.on("TestMessage",onmsg)
communicator.listen(1234)

input("Listening.. \n")