import socket
import sys
import multiprocessing
import threading
import asyncore
import base64
import urllib2
import array
import math
from Crypto.Cipher import AES
from Crypto import Random
from types import *

chunk_size = 8192
class CommunicatorItem():
  def __init__(self,path,conn,encrypt,decrypt,encryptbuffer,decryptbuffer):
    self.path=path
    self.items=None
    self.value=None
    self.isready=False
    self.encrypt=encrypt
    self.decrypt=decrypt
    self.conn=conn
    self.encryptbuffer=encryptbuffer
    self.decryptbuffer=decryptbuffer
  def setdefaultreceiver(self,address,port):
    self.receiver=address
    self.port=port
  def get(self):
    self.conn.sendall(self.encrypt("get "+self.path))
    data = self.conn.recv(chunk_size)
    data=str(self.decrypt(data))
    type=data[:-(len(data)-data.index(" "))]
    req=data[data.index(" ")+1:]
    if(type=="object"):
      self.items=[]
      items=req.split(",")
      for i in xrange(0,len(items)):
        self.items.append(CommunicatorItem(self.path+"/"+items[i],self.conn,self.encrypt,self.decrypt,self.encryptbuffer,self.decryptbuffer))
    else:
      if(type=="string"):
        self.value=req
      else:
        if(type=="number"):
          if(self.isfloat(req)):
            self.value=float(req)
          else:
            self.value=int(req)
        else:
          if(type=="boolean"):
            self.value=req=="true"
          else:
            if(type=="buffer"):
              buff=bytearray([])
              while(True):
                self.conn.sendall(self.encrypt("getnextbuffchunk"))
                data = self.conn.recv(chunk_size*2)
                decdata = self.decryptbuffer(data)
                if(str(decdata).split(" ")[0]!="donebuffer"):
                  buff += decdata
                else:
                  self.decrypt(data)
                  break
              self.value=buffer(buff)
    self.isready=True

  def isfloat(self,value):
    try:
      int(value)
      return False
    except:
      return True
  def isarray(self,items):
    for i in xrange(0,len(items)):
      item=items[i]
      itemname=str(item.path.split("/")[-1])
      if(itemname!="!"):
        try:
          int(itemname)
        except:
          return False
        if(str(int(itemname))!=itemname):
          return False
      else:
        return True
    return True
  def getobject(self,items):
    result={}
    if(items==None):
      items=self.items
    for i in xrange(0,len(items)):
      item=items[i]
      itemname=str(item.path.split("/")[-1])
      if(item.items!=None):
        if(self.isarray(item.items)):
          res=[]
          for j in xrange(0,len(item.items)-1):
            res.append(item.items[j].value)
        else:
          res=self.getobject(item.items)
        result[itemname]=res
      else:
        result[itemname]=item.value
    return result
class CommunicatorTools():
  @staticmethod
  def findnotready(item):
    if(not item.isready):
      return item
    else:
      if(item.value==None):
        for i in xrange(0,len(item.items)):
          nr=CommunicatorTools.findnotready(item.items[i])
          if(nr!=False):
            return nr
    return False
class Communicator(asyncore.dispatcher):
  def __init__(self,password=None):
    asyncore.dispatcher.__init__(self)
    self.callbacks = {}
    self.listening = True
    self.password = password
    self.bs=16
  def setdefaultreceiver(self,receiver,port):
    self.receiver=receiver
    self.port=port
  def send(self,obj,msg_type):
    connectionthread = threading.Thread(target=self._send,args=(obj,msg_type,))
    connectionthread.daemon=True
    connectionthread.start()
  def _send(self,_obj,msg_type):

    global iv
    iv = buffer(bytearray([0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]))

    def __pad( text):
        text_length = len(text)
        amount_to_pad = AES.block_size - (text_length % AES.block_size)
        if amount_to_pad == 0:
            amount_to_pad = AES.block_size
        pad = chr(amount_to_pad)
        return text + pad * amount_to_pad

    def __unpad(text):
        pad = ord(text[-1])
        return text[:-pad]
    pad = lambda s: s + (16 - len(s) % 16) * chr(16 - len(s) % 16)
    def encryptbuffer(raw):
        global iv
        raw = pad(raw)
        cipher = AES.new(self.password, AES.MODE_CBC, iv)
        return base64.b64encode(cipher.encrypt(raw)) 

    def encrypt(raw):
        global iv
        newiv=Random.new().read(16)
        raw+=" "+",".join(list(map(lambda x: str(x),bytearray(newiv))))
        raw = __pad(raw)
        cipher = AES.new(self.password, AES.MODE_CBC, iv)
        iv=newiv
        
        return base64.b64encode(cipher.encrypt(raw)) 

    def decryptbuffer(enc):
        global iv
        enc = base64.b64decode(enc)
        cipher = AES.new(self.password, AES.MODE_CBC, iv )
        result= cipher.decrypt(enc)
        return result[:-16]
    def decrypt(enc):
        global iv
        enc = base64.b64decode(enc)
        cipher = AES.new(self.password, AES.MODE_CBC, iv )
        result= __unpad(cipher.decrypt(enc).decode("utf-8"))
        return result
    conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    conn.connect((self.receiver,self.port))
    obj={}
    obj["type"]=msg_type
    obj["val"]=_obj

    conn.sendall(encrypt("start"))
    while(True):
      data = conn.recv(chunk_size)
      data = decrypt(data)
      
      if(data=="close"):
        conn.close()
        return
      type=data[:-(len(data)-data.index(" "))]
      req=data[data.index(" "):]
      if(type=="get"):
        targetobj=obj
        paths=req.split("/")
        for i in xrange(0,len(paths)):
          if(len(paths[i])>0 and paths[i]!=" "):
            if(isinstance(targetobj,list)):
              targetobj=targetobj[int(paths[i])]
            else:
              targetobj=targetobj[paths[i]]
        if(isinstance(targetobj, int) or isinstance(targetobj, float) or isinstance(targetobj, long)):
          conn.sendall(encrypt("number "+str(targetobj)))
        elif(isinstance(targetobj, bool)):
          conn.sendall(encrypt("boolean "+str(targetobj).lower()))
        elif(isinstance(targetobj,list)):
          conn.sendall(encrypt("object "+",".join(str(v) for v in list(range(len(targetobj))))))
        elif(isinstance(targetobj, dict)):
                  address=""
                
                  for property in (targetobj).keys() :
                    address+=property+","
                  if(len(address)>0):
                    address=address[:-1]
                  conn.sendall(encrypt("object "+address))
        elif(isinstance(targetobj, buffer)):
                      targetobj=str(bytearray(targetobj)+bytearray([0]))
                      conn.sendall(encrypt("buffer "+str(len(targetobj))))
                      i=0
                     
                      data = conn.recv(chunk_size)
                      data = decrypt(data)
                      while(True):
                      
                        to=-max(len(targetobj)-(i+1)*chunk_size,0)
                        if(to==0):
                          to=None
                        conn.sendall(encryptbuffer(targetobj[min(len(targetobj),i*chunk_size):-max(len(targetobj)-(i+1)*chunk_size,0) if -max(len(targetobj)-(i+1)*chunk_size,0)!=0 else None]))
 
                        data = conn.recv(chunk_size)
                        data = decrypt(data)
                        if(len(targetobj)<(i+1)*chunk_size):
                          conn.sendall(encrypt("donebuffer"))
                          break
                        i+=1
        elif(isinstance(targetobj, str)):
                        conn.sendall(encrypt("string "+str(targetobj)))
            
              
      
  def on(self,type,callback):
    self.callbacks[type]=callback
  def onconnection(self,conn):
    global iv
    iv = buffer(bytearray([0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]))
    
    def __pad( text):
        text_length = len(text)
        amount_to_pad = AES.block_size - (text_length % AES.block_size)
        if amount_to_pad == 0:
            amount_to_pad = AES.block_size
        pad = chr(amount_to_pad)
        return text + pad * amount_to_pad

    def __unpad(text):
        pad = ord(text[-1])
        return text[:-pad]
    
    def encryptbuffer(raw):
        global iv
        raw = __pad(raw)
        cipher = AES.new(self.password, AES.MODE_CBC, iv)
        return base64.b64encode(cipher.encrypt(raw)) 

    def encrypt(raw):
        global iv
        raw = __pad(raw)
        cipher = AES.new(self.password, AES.MODE_CBC, iv)
        return base64.b64encode(cipher.encrypt(raw)) 

    def decryptbuffer(enc):
        global iv
        enc = base64.b64decode(enc)
        cipher = AES.new(self.password, AES.MODE_CBC, iv )
        result= cipher.decrypt(enc)
        return result[:-16]
    def decrypt(enc):
        global iv
        enc = base64.b64decode(enc)
        cipher = AES.new(self.password, AES.MODE_CBC, iv )
        result= __unpad(cipher.decrypt(enc).decode("utf-8"))
        newiv=result[result.rfind(" ")+1:]
        iv=buffer(bytearray(map(int,newiv.split(","))))
        return result[:-(len(result)-result.rfind(" "))]
    
    mainitem=CommunicatorItem("/",conn,encrypt,decrypt,encryptbuffer,decryptbuffer)
    data = conn.recv(chunk_size)
    str(decrypt(data))
    while(True):
      nr=CommunicatorTools.findnotready(mainitem)
      if(nr is False):
        break
      nr.get()
    obj=mainitem.getobject(None)
    conn.sendall(encrypt("close"))
    if(self.callbacks[obj["type"]]!=None):
      self.callbacks[obj["type"]](obj["val"])
    
  def _listen(self,port):
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    try:
      s.bind(("127.0.0.1", port))
    except socket.error as msg:
      print 'Bind failed. Error Code : ' + str(msg[0]) + ' Message ' + msg[1]
    
    s.listen(port)
    while(self.listening):
      conn, addr = s.accept()
      connectionthread = threading.Thread(target=self.onconnection,args=(conn,))
      connectionthread.daemon=True
      connectionthread.start()
  def listen(self,port):
    thread = threading.Thread(target=self._listen,args=(port,))
    thread.daemon=True
    thread.start()
    return thread

asyncore.loop()
