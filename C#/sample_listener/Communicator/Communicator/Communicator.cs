using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Communicator;
using System.Runtime.Remoting;

namespace CTCServer
{

    class Client
    {
        //Stores the TcpClient
        private TcpClient client;

        //Stores the StreamWriter. Used to write to client
        private StreamWriter writer;

        //Stores the StreamReader. Used to recive data from client
        private StreamReader reader;

        //Defines if the client shuld look for incoming data
        private bool listen = true;

        //Stores clientID. ClientID = clientCount on connection time
        public int id;

        //Event to pass recived data to the server class
        public delegate void internalGotDataFromCTCHandler(object sender, string msg);
        public event internalGotDataFromCTCHandler internalGotDataFromCTC;

        //Constructor
        public Client(TcpClient client, int id)
        {
            //Assain members
            this.client = client;
            this.id = id;

            //Init the StreamWriter
            writer = new StreamWriter(this.client.GetStream());
            reader = new StreamReader(this.client.GetStream());

            new Thread(() =>
            {
                Listen(reader);
            }).Start();
        }

        //Reads data from the connection and fires an event wih the recived data
        public void Listen(StreamReader reader)
        {
            //While we should look for new data
            while (listen)
            {
                //Read whole lines. This will read from start until \r\n" is recived!
                string input = reader.ReadLine();

                //If input is null the client disconnected. Tell the user about that and close connection.
                if (input == null)
                {
                    //Inform user
                    input = "Client with ID " + this.id + " disconnceted.";
                    internalGotDataFromCTC(this, input);

                    //Close
                    Close();

                    //Exit thread.
                    return;
                }

                internalGotDataFromCTC(this, input);
            }
        }

        //Sends the string "data" to the client
        public void Send(string data)
        {
            //Write and flush data
            writer.WriteLine(data);
            writer.Flush();
        }

        //Closes the connection
        public void Close()
        {
            //Stop listening
            listen = false;

            //Close streamwriter FIRST
            writer.Close();

            //Then close connection
            client.Close();
        }
    }
}

namespace T1.CoreUtils.Utilities
{
    public static class CryptoUtility
    {

        public static string Encrypt(string input, string passphrase, byte[] iv)
        {
            byte[] key, o;
            DeriveKeyAndIV(RawBytesFromString(passphrase), null, 1, out key, out o);
            return Convert.ToBase64String(EncryptStringToBytes(input, Encoding.UTF8.GetBytes(passphrase), iv));
        }

        public static string Decrypt(string inputBase64, string passphrase, byte[] iv)
        {
            byte[] key, o;
            DeriveKeyAndIV(RawBytesFromString(passphrase), null, 1, out key, out o);
            return DecryptStringFromBytes(Convert.FromBase64String(inputBase64), Encoding.UTF8.GetBytes(passphrase), iv);
        }

        public static string EncryptBytes(byte[] bytes, string passphrase, byte[] IV)
        {
            byte[] Key, o;
            DeriveKeyAndIV(RawBytesFromString(passphrase), null, 1, out Key, out o);
            Key = Encoding.UTF8.GetBytes(passphrase);
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;

            using (RijndaelManaged cipher = new RijndaelManaged())
            {
                cipher.Key = Key;
                cipher.IV = IV;
                cipher.FeedbackSize = 128;


                ICryptoTransform encryptor = cipher.CreateEncryptor(cipher.Key, cipher.IV);


                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(bytes, 0, bytes.Length);
                        csEncrypt.Close();

                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            return Convert.ToBase64String(encrypted);
        }

        public static byte[] DecryptBytes(string inputBase64, string passphrase, byte[] IV)
        {
            byte[] Key, o;
            DeriveKeyAndIV(RawBytesFromString(passphrase), null, 1, out Key, out o);
            Key = Encoding.UTF8.GetBytes(passphrase);
            byte[] cipherText = Convert.FromBase64String(inputBase64);

            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");



            using (var cipher = new RijndaelManaged())
            {
                cipher.Key = Key;
                cipher.IV = IV;
                cipher.FeedbackSize = 128;


                ICryptoTransform decryptor = cipher.CreateDecryptor(cipher.Key, cipher.IV);

                var output = new MemoryStream();
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        var buffer = new byte[8192];
                        var read = csDecrypt.Read(buffer, 0, buffer.Length);
                        while (read > 0)
                        {
                            output.Write(buffer, 0, read);
                            read = csDecrypt.Read(buffer, 0, buffer.Length);
                        }
                        csDecrypt.Flush();
                        return output.ToArray();
                    }
                }

            }


        }

        public static byte[] RawBytesFromString(string input)
        {
            var ret = new List<Byte>();

            foreach (char x in input)
            {
                var c = (byte)((ulong)x & 0xFF);
                ret.Add(c);
            }

            return ret.ToArray();
        }

        private static void DeriveKeyAndIV(byte[] data, byte[] salt, int count, out byte[] key, out byte[] iv)
        {
            List<byte> hashList = new List<byte>();
            byte[] currentHash = new byte[0];

            int preHashLength = data.Length + ((salt != null) ? salt.Length : 0);
            byte[] preHash = new byte[preHashLength];

            System.Buffer.BlockCopy(data, 0, preHash, 0, data.Length);
            if (salt != null)
                System.Buffer.BlockCopy(salt, 0, preHash, data.Length, salt.Length);

            MD5 hash = MD5.Create();
            currentHash = hash.ComputeHash(preHash);

            for (int i = 1; i < count; i++)
            {
                currentHash = hash.ComputeHash(currentHash);
            }

            hashList.AddRange(currentHash);

            while (hashList.Count < 48)
            {
                preHashLength = currentHash.Length + data.Length + ((salt != null) ? salt.Length : 0);
                preHash = new byte[preHashLength];

                System.Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
                System.Buffer.BlockCopy(data, 0, preHash, currentHash.Length, data.Length);
                if (salt != null)
                    System.Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + data.Length, salt.Length);

                currentHash = hash.ComputeHash(preHash);

                for (int i = 1; i < count; i++)
                {
                    currentHash = hash.ComputeHash(currentHash);
                }

                hashList.AddRange(currentHash);
            }
            hash.Clear();
            key = new byte[32];
            iv = new byte[16];
            hashList.CopyTo(0, key, 0, 32);
            hashList.CopyTo(32, iv, 0, 16);
        }

        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;

            using (RijndaelManaged cipher = new RijndaelManaged())
            {
                cipher.Key = Key;
                cipher.IV = IV;
                cipher.Padding = PaddingMode.PKCS7;
                cipher.Mode = CipherMode.CBC;


                ICryptoTransform encryptor = cipher.CreateEncryptor(cipher.Key, cipher.IV);


                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {


                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }



            return encrypted;

        }

        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");


            string plaintext = null;


            using (var cipher = new RijndaelManaged())
            {
                cipher.Key = Key;
                cipher.IV = IV;
                cipher.Padding = PaddingMode.PKCS7;
                cipher.Mode = CipherMode.CBC;


                ICryptoTransform decryptor = cipher.CreateDecryptor(cipher.Key, cipher.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {

                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {


                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
    }
}

namespace Communicator
{


    public class CommunicatorOptions
    {
        public string password = "DefaultPassword";

        public string receiver = "127.0.0.1";
        public int port = 1235;
        public CommunicatorOptions()
        {

        }
    }

    public class StateObject
    {

        public Socket workSocket = null;

        public const int BufferSize = 1024;

        public byte[] buffer = new byte[BufferSize];

        public StringBuilder sb = new StringBuilder();
    }



    public class CommunicatorTools
    {
        public static string[] GetPropertiesFromObject(dynamic inobj)
        {
            string[] properties, sourceproperties;

            if (!(inobj is ExpandoObject))
                properties = ((object)inobj).GetType()
    .GetFields(BindingFlags.Public | BindingFlags.NonPublic |
         BindingFlags.Static | BindingFlags.Instance |
         BindingFlags.DeclaredOnly)
    .ToList()
    .Select(f => f.Name).ToArray();
            else
                properties = ((IDictionary<string, object>)inobj).Keys.ToArray();
            return properties;
        }

        public static dynamic GetFromObjectByProperty(dynamic obj, string property)
        {
            dynamic target;
            if (obj is ExpandoObject)
                target = ((IDictionary<string, object>)obj)[property];
            else
                target = ((object)obj).GetType()
.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
     BindingFlags.Static | BindingFlags.Instance |
     BindingFlags.DeclaredOnly)
.ToList()
.Find(f => f.Name == property).GetValue(obj);
            return target;
        }

        public static int ValueType(dynamic target)
        {
            if (target is sbyte
            || target is byte
            || target is short
            || target is ushort
            || target is int
            || target is uint
            || target is long
            || target is ulong
            || target is float
            || target is double
            || target is decimal)
                return 1;
            else
                if (target is string)
                return 2;
            else
                if (target is bool)
                return 3;
            else
                if (target is byte[])
                return 4;
            else
                if (target is Object[] || target is sbyte
            || target is byte[]
            || target is short[]
            || target is ushort[]
            || target is int[]
            || target is uint[]
            || target is long[]
            || target is ulong[]
            || target is float[]
            || target is double[]
            || target is decimal[])
                return 5;
            return -1;
        }


        public static void CopyToObject(dynamic inobj, object sourceobj)
        {
            string[] properties, sourceproperties;

            properties = CommunicatorTools.GetPropertiesFromObject(inobj);
            sourceproperties = CommunicatorTools.GetPropertiesFromObject(sourceobj);

            properties = new List<string>(properties).Intersect(new List<string>(sourceproperties)).ToArray();

            for (int i = 0; i < properties.Length; i++)
            {
                string property = properties[i];
                dynamic targetsource = CommunicatorTools.GetFromObjectByProperty(sourceobj, property);
                dynamic targetin = CommunicatorTools.GetFromObjectByProperty(inobj, property);

                if (CommunicatorTools.ValueType(targetin) > 0 && (CommunicatorTools.ValueType(targetin) == CommunicatorTools.ValueType(targetsource) || CommunicatorTools.ValueType(targetsource) == 5))
                {

                    if (CommunicatorTools.ValueType(targetsource) > 0)
                    {
                        dynamic arr = null;
                        if (targetsource.GetType().IsArray)
                        {
                            arr = Array.CreateInstance((targetin).GetType().GetElementType(), ((dynamic)targetsource).Length);
                            for (int j = 0; j < arr.Length; j++)
                            {
                                arr.SetValue(Convert.ChangeType(targetsource[j], (targetin).GetType().GetElementType()), j);
                            }
                        }
                        if (inobj is ExpandoObject)
                            ((IDictionary<string, object>)inobj)[property] = targetsource;
                        else
                            ((object)inobj).GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                 BindingFlags.Static | BindingFlags.Instance |
                 BindingFlags.DeclaredOnly)
            .ToList()
            .Find(f => f.Name == property).SetValue(inobj, targetsource.GetType().IsArray ? arr : (targetsource is double ? Convert.ChangeType(targetsource, Convert.GetTypeCode(targetin)) : targetsource));
                    }
                    else
                        CopyToObject(targetin, targetsource);
                }
            }
        }
    }
    public class Communicator
    {
        public class CommunicatorConnection
        {
            public byte[] iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public CommunicatorConnection()
            {

            }

            public string Encrypt(string str, string pass, bool sender)
            {
                if (sender)
                {
                    byte[] newiv = new byte[16];
                    Random r = new Random();
                    r.NextBytes(newiv);

                    str = str + " " + String.Join(",", newiv.Select((x) => { return Convert.ToString(x); }).ToArray());

                    string res = T1.CoreUtils.Utilities.CryptoUtility.Encrypt(str, pass, this.iv);
                    this.iv = newiv;
                    return res;
                }
                else
                    return T1.CoreUtils.Utilities.CryptoUtility.Encrypt(str, pass, this.iv);
            }
            public string Decrypt(string str, string pass, bool sender)
            {
                if (!sender)
                {
                    str = T1.CoreUtils.Utilities.CryptoUtility.Decrypt(str, pass, this.iv);

                    string invstr = str.Substring(str.LastIndexOf(" ") + 1);
                    str = str.Substring(0, str.LastIndexOf(" "));

                    this.iv = invstr.Split(',').Select((x) => { return Convert.ToByte(x); }).ToArray();

                    return str;
                }
                else
                    return T1.CoreUtils.Utilities.CryptoUtility.Decrypt(str, pass, this.iv);
            }
        }
        public class CommunicatorItem
        {
            public int ChunkSize = 8192;
            public string path = "";
            public bool IsReady = false;
            public Dictionary<string, CommunicatorItem> items = new Dictionary<string, CommunicatorItem>();
            public object value = null;
            public string name = null;
            public Socket socket = null;
            private string password = null;
            public CommunicatorItem()
            {

            }
            public CommunicatorItem(Socket client, string pass)
            {
                socket = client;
                password = pass;
            }

            public CommunicatorItem addchild(string key)
            {
                var ci = new CommunicatorItem(socket, password);
                ci.path = path + "/" + key;
                items.Add(key, ci);
                return ci;
            }

            public void ready()
            {
                IsReady = true;
            }
            public bool ByteArrayToFile(string fileName, byte[] byteArray)
            {
                try
                {
                    using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(byteArray, 0, byteArray.Length);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception caught in process: {0}", ex);
                    return false;
                }
            }
            public void getitem(CommunicatorConnection cc)
            {
                socket.Send(Encoding.UTF8.GetBytes(cc.Encrypt("get " + path, password, false)));

                byte[] data = new byte[ChunkSize];
                string res = "";

                res = "";
                int size = socket.Receive(data);

                for (int i = 0; i < size; i++)
                    res += Convert.ToChar(data[i]);

                res = cc.Decrypt(res, password, false);

                data = null;


                string type = res.Substring(0, res.IndexOf(' '));
                string value = res.Substring(res.IndexOf(' ') + 1);

                if (type == "object")
                {
                    if (value.Length > 0)
                    {
                        string[] values = value.Split(',');
                        for (int i = 0; i < values.Length; i++)
                        {
                            addchild(values[i]);
                        }
                    }
                    ready();
                }
                else
                    if (type == "string")
                {
                    this.value = value;
                    ready();
                }
                else
                     if (type == "boolean")
                {
                    this.value = value;
                    ready();
                }
                else
                    if (type == "number")
                {
                    this.value = Convert.ToDouble(value);
                    ready();
                }
                else
                    if (type == "buffer")
                {

                    byte[] buffer = new byte[Convert.ToInt32(value)];
                    int i = 0;
                    do
                    {
                        if (i * ChunkSize >= buffer.Length)
                            break;
                        socket.Send(Encoding.UTF8.GetBytes(cc.Encrypt("getnextbuffchunk", password, false)));

                        data = new byte[ChunkSize * 2];

                        size = socket.Receive(data);



                        res = "";

                        for (int j = 0; j < size; j++)
                            res += Convert.ToChar(data[j]);
                        data = null;
                        var bytes = T1.CoreUtils.Utilities.CryptoUtility.DecryptBytes(res, password, cc.iv);
                        Array.Copy(bytes, 0, buffer, i++ * ChunkSize, Math.Min(ChunkSize, bytes.Length));
                    }
                    while (true);
                    this.value = buffer;
                    File.WriteAllBytes("output.png", buffer);
                    buffer = null;
                    ready();
                }

            }

            public dynamic ToObject()
            {
                return ToObject(this);
            }
            private int isarray(Dictionary<string, CommunicatorItem> items)
            {
                if (items == null)
                    return -1;
                int i = 0;
                foreach (var item in items)
                {
                    try
                    {
                        if (!(item.Key == "!" || Convert.ToInt32(item.Key).ToString() == item.Key))
                        {
                            return -1;
                        }
                    }
                    catch
                    {
                        return -1;
                    }
                    i++;
                }
                return i;
            }
            public dynamic ToObject(dynamic obj)
            {
                dynamic x;
                if ((obj).GetType() != typeof(CommunicatorItem))
                    return obj;
                if (obj.items.Count > 0)
                {
                    x = new ExpandoObject();
                    int arraysize = isarray(obj.items);
                    if (arraysize > -1)
                    {
                        List<object> arr = new List<object>();
                        foreach (var item in obj.items)
                        {
                            if (item.Key != "!")
                            {
                                arr.Add(ToObject(item.Value.value));
                            }
                        }
                        x = arr.ToArray();
                    }
                    else
                    {
                        foreach (var item in obj.items)
                        {
                            ((IDictionary<string, object>)x)[(string)item.Key] = ToObject(item.Value);
                        }
                    }
                }
                else
                {

                    x = obj.value;
                }
                return x;
            }
        }

        public int ChunkSize = 8192;
        private Dictionary<string, Action<object>> Callbacks = new Dictionary<string, Action<object>>();
        public void On(string _event, Action<object> callback)
        {
            Callbacks.Add(_event, callback);
        }
        public static CommunicatorItem FindNotReady(CommunicatorItem item)
        {
            if (!item.IsReady)
                return item;
            else
                foreach (var ci in item.items)
                {
                    CommunicatorItem nr = FindNotReady(ci.Value);
                    if (nr != null)
                        return nr;
                }
            return null;
        }


        private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments.  
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException("cipherText");
            }
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }

            string plaintext = null;

            using (var rijAlg = new RijndaelManaged())
            {
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.FeedbackSize = 128;

                rijAlg.Key = key;
                rijAlg.IV = iv;

                var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                try
                {
                    using (var msDecrypt = new MemoryStream(cipherText))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {

                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                plaintext = srDecrypt.ReadToEnd();

                            }

                        }
                    }
                }
                catch
                {
                    plaintext = "keyError";
                }
            }

            return plaintext;
        }

        public static CommunicatorOptions options = new CommunicatorOptions();

        public Communicator(CommunicatorOptions co)
        {
            options = co;
        }
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public void SetDefaultReceiver(string receiver, int port)
        {
            options.receiver = receiver;
            options.port = port;
        }


        public void Send(dynamic obj)
        {
            string type = "";
            if (obj is string)
                type = "string";
            else
                if (obj is byte[])
                type = "buffer";
            else
                if (obj is float || obj is double || obj is int)
                type = "number";
            if (obj is object)
                type = "object";

            Send(obj, type);
        }
        private static Random random = new Random();
        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        Dictionary<string, CommunicatorConnection> connections = new Dictionary<string, CommunicatorConnection>();
        public void Send(dynamic sendobj, string type)
        {
            dynamic obj = new ExpandoObject();
            obj.type = type;
            obj.val = sendobj;

            new Task(() =>
            {
                TcpClient client = new TcpClient();
                client.Connect(options.receiver, options.port);

                NetworkStream clientStream = client.GetStream();
                string key = RandomString(12);
                CommunicatorConnection Connection = new CommunicatorConnection();
                Connection.iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                connections.Add(key, Connection);
                int connectionsindex = connections.Count - 1;
                byte[] senddata = Encoding.UTF8.GetBytes(Connection.Encrypt("start", options.password, true));
                clientStream.Write(senddata, 0, senddata.Length);
                clientStream.Flush();
                senddata = null;
                byte[] sendingbuffer = null;
                int sendingbuffer_i = 0;

                while (true)
                {
                    byte[] inMessage = new byte[ChunkSize];
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = clientStream.Read(inMessage, 0, ChunkSize);
                    }
                    catch (Exception e) { /*Catch exceptions and handle them here*/ }

                    string res = Connection.Decrypt(Encoding.ASCII.GetString(inMessage, 0, bytesRead), options.password, true);
                    inMessage = null;
                    if (res == "close")
                        break;
                    else
                        if (res == "getnextbuffchunk")
                    {

                        if ((sendingbuffer_i) * ChunkSize > sendingbuffer.Length)
                        {
                            senddata = Encoding.UTF8.GetBytes(Connection.Encrypt("donebuffer", options.password, true));

                            clientStream.Write(senddata, 0, senddata.Length);
                            clientStream.Flush();

                            continue;
                        }
                        senddata = new byte[Math.Min(ChunkSize, sendingbuffer.Length - sendingbuffer_i * ChunkSize)];
                        Array.Copy(sendingbuffer, sendingbuffer_i * ChunkSize, senddata, 0, Math.Min(ChunkSize, sendingbuffer.Length - sendingbuffer_i * ChunkSize));
                        senddata = Encoding.UTF8.GetBytes(T1.CoreUtils.Utilities.CryptoUtility.EncryptBytes(senddata, options.password, Connection.iv));
                        clientStream.Write(senddata, 0, senddata.Length);
                        clientStream.Flush();
                        sendingbuffer_i++;
                        continue;
                    }

                    string reqtype = res.Substring(0, res.IndexOf(' '));
                    string req = res.Substring(res.IndexOf(' ') + 1);
                    // Console.WriteLine(req);
                    if (reqtype == "get")
                    {
                        dynamic reqobj = obj;
                        string[] paths = req.Split('/');
                        for (int i = 0; i < paths.Length; i++)
                        {
                            if (paths[i] != "")
                            {
                                if (((object)reqobj).GetType().IsArray)
                                    reqobj = reqobj[Convert.ToInt32(paths[i])];
                                else
                                    if (reqobj is ExpandoObject)
                                    reqobj = ((IDictionary<string, object>)reqobj)[paths[i]];
                                else
                                    reqobj = ((object)reqobj).GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Static | BindingFlags.Instance |
                         BindingFlags.DeclaredOnly)
            .ToList()
            .Find(f => f.Name == paths[i]).GetValue(reqobj);
                            }
                        }
                        if (reqobj is string)
                        {
                            senddata = Encoding.UTF8.GetBytes(Connection.Encrypt("string " + reqobj, options.password, true));

                            clientStream.Write(senddata, 0, senddata.Length);
                            clientStream.Flush();

                            senddata = null;
                        }
                        else
                        if (
                        reqobj is sbyte
            || reqobj is byte
            || reqobj is short
            || reqobj is ushort
            || reqobj is int
            || reqobj is uint
            || reqobj is long
            || reqobj is ulong
            || reqobj is float
            || reqobj is double
            || reqobj is decimal)
                        {
                            senddata = Encoding.UTF8.GetBytes(Connection.Encrypt("number " + reqobj.ToString(), options.password, true));

                            clientStream.Write(senddata, 0, senddata.Length);
                            clientStream.Flush();

                            senddata = null;
                        }
                        else
                        if (reqobj is bool)
                        {
                            senddata = Encoding.UTF8.GetBytes(Connection.Encrypt("boolean " + reqobj ? "true" : "false", options.password, true));

                            clientStream.Write(senddata, 0, senddata.Length);
                            clientStream.Flush();

                            senddata = null;
                        }
                        else
                            if (reqobj is byte[])
                        {
                            sendingbuffer = reqobj;

                            sendingbuffer_i = 0;

                            senddata = Encoding.UTF8.GetBytes(Connection.Encrypt("buffer " + (reqobj.Length).ToString(), options.password, true));

                            clientStream.Write(senddata, 0, senddata.Length);
                            clientStream.Flush();
                        }
                        else
                        if (((object)reqobj).GetType().IsArray)
                        {
                            string str = "";
                            for (int i = 0; i < ((Array)reqobj).Length; i++)
                                str +=  i.ToString()+",";
                            if(str.Length>0)
                            str = str.Substring(0, str.Length - 1);
                            senddata = Encoding.UTF8.GetBytes(Connection.Encrypt("object !," +str , options.password, true));

                            clientStream.Write(senddata, 0, senddata.Length);
                            clientStream.Flush();

                            senddata = null;
                        }
                        else
                        if (reqobj is object)
                        {

                            string[] properties;
                            string str = "";
                            if (!(reqobj is ExpandoObject))
                                properties = ((object)reqobj).GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Static | BindingFlags.Instance |
                         BindingFlags.DeclaredOnly)
            .ToList()
            .Select(f => f.Name).ToArray();
                            else
                                properties = ((IDictionary<string, object>)reqobj).Keys.ToArray();

                            foreach (var p in properties)
                                str += p + ",";
                            str = str.Substring(0, str.Length - 1);
                            senddata = Encoding.UTF8.GetBytes(Connection.Encrypt("object " + str, options.password, true));

                            clientStream.Write(senddata, 0, senddata.Length);
                            clientStream.Flush();

                            senddata = null;
                        }
                    }
                    else
                    {

                    }
                }
                connections.Remove(key);

                client.Close();
            }).Start();
        }

        private dynamic ToDynamic<T>(T obj)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (var propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic))
            {
                var currentValue = propertyInfo.GetValue(obj);
                expando.Add(propertyInfo.Name, currentValue);
            }
            return expando as ExpandoObject;
        }
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public bool listening = false;
        private List<CTCServer.Client> clients = new List<CTCServer.Client>();
        public void unlisten()
        {
            listening = false;
        }
        public void listen(int Port)
        {
            listening = true;
            new Task(() =>
            {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

                TcpListener listener = new TcpListener(ipAddress, Port);

                listener.Start();
                while (listening)
                {

                    Socket client = listener.AcceptSocket();

                    var childSocketThread = new Thread(() =>
                    {
                        string key = RandomString(12);
                        CommunicatorConnection Connection = new CommunicatorConnection();
                        Connection.iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        connections.Add(key, Connection);
                        int connectionsindex = connections.Count - 1;

                        CommunicatorItem mainitem = new CommunicatorItem(client, options.password);
                        byte[] data = new byte[8192];
                        string res = "";

                        res = "";
                        int size = client.Receive(data);

                        for (int i = 0; i < size; i++)
                            res += Convert.ToChar(data[i]);

                        res = Connection.Decrypt(res, options.password, false);

                        //Console.WriteLine(res);
                        CommunicatorItem notreadyitem = FindNotReady(mainitem);
                        do
                        {
                            notreadyitem.getitem(Connection);
                            notreadyitem = FindNotReady(mainitem);
                        }
                        while (notreadyitem != null);
                        data = null;
                        dynamic result = mainitem.ToObject();
                        for (int i = 0; i < Callbacks.Count; i++)
                        {
                            if (Callbacks.ElementAt(i).Key == ((IDictionary<string, object>)result)["type"].ToString())
                            {
                                Action<object> cb = null;
                                Callbacks.TryGetValue((string)((IDictionary<string, object>)result)["type"], out cb);

                                cb((ExpandoObject)result.val);
                            }
                        }
                        client.Send(Encoding.UTF8.GetBytes(Connection.Encrypt("close", options.password, false)));
                        client.Close();
                        client.Dispose();

                        connections.Remove(key);
                    });
                    childSocketThread.Start();

                }
            }).Start();
        }
    }
}
