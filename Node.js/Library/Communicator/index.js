const EventEmitter = require('events');
var net = require('net');
var crypto = require('crypto')

class Communicator extends EventEmitter
{
    constructor(settings)
    {
        super();
        class CommunicatorItem
        {
            constructor(path, socket, decrypt, encrypt, decryptBuffer,cs)
            {
                this.path = path;
                this.isready = false;
                //this.socket = socket;
                var self = this;
                function isarray(obj)
                {
                    var arr = Object.keys(obj);
                    for (var i = 0; i < arr.length; i++)
                        if (arr[i]!=="!"&&String(parseInt(arr[i])) !== arr[i])
                            return false;
                    return true;
                }
                this.toObject = (obj) =>
                {
                    if (obj === undefined)
                        return self.toObject(self.items);
                    else
                    {
                        var obje = {};
                        for (var key in obj)
                            if (obj[key].data === undefined)
                            {
                                obje[key] = self.toObject(obj[key].items);
                                if (isarray(obje[key]))
                                {
                                    var arr = [];
                                    for (var keyj in obje[key])
                                    {
                                        arr.push(obje[key][keyj]);
                                    }
                                    obje[key] = arr;
                                }
                            }
                            else
                                obje[key] = obj[key].data;
                        
                        return obje;
                    }
                }
                this.get = () =>
                {
                    return new Promise((resolve, reject) =>
                    {
                        socket.write(encrypt("get " + path));
                        var buffchunk = false;
                        socket.once("data", (data) =>
                        {
                            var dec = "";
                            try
                            {
                                var dec = decrypt(data);
                            }
                            catch (e)
                            {

                            }
                            var type = dec.substring(0, dec.indexOf(" "));
                            dec = dec.substring(dec.indexOf(" ") + 1);
                            if (type == "object")
                            {
                                self.items = {};
                                var items = dec.split(",");
                                for (var i = 0; i < items.length; i++)
                                    if (items[i] != "!" && items[i].length>0)
                                    self.items[items[i]] = new CommunicatorItem(path + "/" + items[i], socket, decrypt, encrypt, decryptBuffer,cs);
                                self.isready = true;
                                resolve(self);
                            }
                            else
                                if (type == "boolean")
                                {
                                    self.items = false;
                                    self.data = dec == "true";
                                    self.isready = true;
                                    resolve(self);
                                }
                                else
                                    if (type == "string")
                                    {
                                        self.items = false;
                                        self.data = dec;
                                        self.isready = true;
                                        resolve(self);
                                    }
                                    else
                                        if (type == "number")
                                        {
                                            self.items = false;
                                            self.data = parseFloat(dec);
                                            self.isready = true;
                                            resolve(self);
                                        }
                                        else
                                            if (type == "buffer")
                                            {

                                                var buffchunks = Buffer.allocUnsafe(parseInt(dec));
                                                var i=0;
                                                function getnextbuff(i)
                                                {
                                                    socket.write(encrypt("getnextbuffchunk"),()=>{});
                                                    socket.once("data", (data) =>
                                                    {
                                                        
                                                        var dec = "";
                                                        if(data.length<=128)
                                                        try
                                                        {
                                                            dec = decrypt(data);
                                                        }
                                                        catch (e) { }
                                                        if (dec == "donebuffer")
                                                        {
                                                            self.items = false;
                                                            self.data = buffchunks;
                                                            self.isready = true;
                                                            buffchunks = false;

                                                            resolve(self);
                                                        }
                                                        else
                                                        {
                                                            decryptBuffer(data).copy(buffchunks,i*cs);
                                                            getnextbuff(++i);
                                                        }
                                                    });
                                                }
                                                getnextbuff(i);
                                            }
                        });
                    });

                };
                this.getType = () =>
                {
                    return new Promise((resolve, reject) =>
                    {
                        socket.write(encrypt("gettype " + path));
                        socket.once("data", (data) =>
                        {
                            var dec = decrypt(data);
                            var type = dec.substring(0, dec.indexOf(" "));
                            dec = dec.substring(dec.indexOf(" ") + 1);
                            if (type == "type")
                                resolve(dec);
                        });
                    });
                }
                this.items = undefined;
            }
        }

        var self = this;
        this.settings = {};

        this.config = function (settings)
        {
            self.settings = settings;
        }
        if (settings)
            this.config(settings);

        
        var algorithm = 'aes-128-cbc';

        function encryptBufferbyiv(buffer, iv)
        {
            var password = self.settings.password;

            var cipher = crypto.createCipheriv(algorithm, password, iv)
            var crypted = Buffer.concat([cipher.update(buffer), cipher.final()]);
            return crypted.toString("base64");
        }

        function decryptBufferbyiv(buffer, iv)
        {
            var password = self.settings.password;
            var decipher = crypto.createDecipheriv(algorithm, password, iv)
            var dec = Buffer.concat([new Buffer(decipher.update(String(buffer), "base64")), new Buffer(decipher.final())]);

            return dec;
        }

        function encryptbyiv(str, iv)
        {
            var password = self.settings.password;

            var cipher = crypto.createCipheriv(algorithm, password, iv)
            var resstr = cipher.update(str, 'utf8', 'base64')
            resstr += cipher.final('base64');
            return resstr;
        }

        function decryptbyiv(data, iv)
        {
            var password = self.settings.password;
            var decipher = crypto.createDecipheriv(algorithm, password, iv)
            var dec = decipher.update(String(data), 'base64', 'utf8');
            dec += decipher.final('utf8');

            return dec;
        }

        function listen(port, hostname_or_callback)
        {




            return new Promise((resolve, reject) =>
            {
                var port;
                var hostname;
                var callback;
                for (var i = 0; i < arguments.length; i++)
                {
                    switch (typeof (arguments[i]))
                    {
                        case "number":
                            port = arguments[i];
                            break;
                        case "string":
                            hostname = arguments[i];
                            break;
                        case "function":
                            callback = arguments[i];
                            break;
                    }
                }
                function checkobjs(obj)
                {
                    for (var key in obj)
                    {
                        if (!obj[key].data)
                        {
                            if (!obj[key].isready)
                                return (obj[key]);
                            else
                            {
                                var childscheck = checkobjs(obj[key].items);
                                if (childscheck)
                                    return childscheck;
                            }
                        }
                    }
                    return false;
                }

                var server = net.createServer(function (socket)
                {
                    var iv = new Buffer.alloc(16);

                    function encrypt(str)
                    {
                        return encryptbyiv(str, iv);
                    }

                    function decrypt(str)
                    {
                        
                        var str = decryptbyiv(str, iv);
                        var newiv = new Buffer(str.substring(str.lastIndexOf(" ") + 1).split(","));;
                        if (newiv.length != 16)
                            return str;
                        str = str.substring(0, str.lastIndexOf(" "))
                        iv = newiv;
                        return str;
                    }

                    function decryptBuffer(str)
                    {
                        return decryptBufferbyiv(str, iv);
                    }

                    function encryptBuffer(str)
                    {
                        return encryptBufferbyiv(str, iv);
                    }

                    var currreq = { command: "", command_details: "", obj: {} }

                    var mainobj = new CommunicatorItem("", socket, decrypt, encrypt, decryptBuffer,self.settings.chunk_size || 8192);
                    var nextobj = mainobj;
                    var donebyitem = false;;
                    socket.on('data', function (data)
                    {
                        var dec = "";
                        if (donebyitem)
                            return;
                        try
                        {
                            var dec = decrypt(data);
                        }
                        catch (e) { }
                        function getitems(obj)
                        {
                            donebyitem = true;
                            obj.get().then((item) =>
                            {
                                donebyitem = false;
                                var notready = checkobjs(mainobj.items);
                                if (notready)
                                    getitems(notready)
                                else
                                {
                                    var obj = mainobj.toObject();
                                    self.emit(obj.type, obj.val);
                                    socket.write(encrypt("close"));
                                    socket.end();
                                    //socket.on("close",()=>{socket.destroy();})
                                }
                            });
                        }
                        if (dec == "start")
                            getitems(mainobj);
                    });
                    self.server = server;

                });

                server.listen(port, hostname || '127.0.0.1', () =>
                {
                    if (callback)
                        callback();
                    resolve();
                });
                self.server = server;
            });
        }

        this.listen = listen;

        function unlisten()
        {
            if (self.server.close)
                self.server.close();
        }

        this.unlisten = unlisten;

        this.setdefaultreceiver = (address, port) =>
        {
            self.settings.default_receiver = { address, port };
        };
        

        function send(item, options = {})
        {


            var type = options.type || typeof (item);

            var obj = { type: type, val: item }
            return new Promise((resolve, reject) =>
            {
                var receiver = options.receiver;
                var port = options.port;

                var customreceiver = (receiver && port);
                var client = new net.Socket();
                var chunksize = self.settings.chunk_size || 8192;
                client.connect(customreceiver ? receiver : self.settings.default_receiver.port, customreceiver ? port : self.settings.default_receiver.address, function ()
                {
                    var iv = new Buffer.alloc(16);

                    function encrypt(str)
                    {
                        var newiv = crypto.randomBytes(16);
                        var deced = encryptbyiv(str + " " + Array.apply(null, Array.from(newiv)).join(","), iv);
                        iv = newiv;
                        return deced;
                    }

                    function decrypt(str)
                    {
                        var str = decryptbyiv(str, iv);
                        return str;
                    }

                    function decryptBuffer(str)
                    {
                        return decryptBufferbyiv(str, iv);
                    }

                    function encryptBuffer(str)
                    {
                        return encryptBufferbyiv(str, iv);
                    }

                    var sendingbuffer = {};
                    client.write(encrypt("start"));
                    var arr=[];
                    client.on("data", (data) =>
                    {
                        
                        data = decrypt(String(data));
                        if (data == "close")
                        {
                            client.end();
                            client.once("close", () => { resolve() });
                            return;
                        }
                        if (data == "getnextbuffchunk")
                        {
                            
                            var x=(Date.now())
                            if (sendingbuffer.i * chunksize < sendingbuffer.len)
                            {
                                client.write(encryptBuffer(sendingbuffer.buff.slice(sendingbuffer.i * chunksize, Math.min(((sendingbuffer.i) + 1) * chunksize, sendingbuffer.len))),()=>{arr.push(Date.now()-x)});
                                sendingbuffer.i++;
                            }
                            else
                            {
                                sendingbuffer = {};
                                var sum=0;
                                arr.map((x)=>{sum+=x});
                                client.write(encrypt("donebuffer"));
                            }
                        }
                        if (data.substring(0, data.indexOf(" ")) == "get")
                        {
                            var req = data.substring(data.indexOf(" ") + 1);
                            var path = req.split("/");
                            var targetobj = obj;
                            for (var i = 0; i < path.length; i++)
                                if (path[i] != "")
                                    targetobj = targetobj[path[i]];
                            if (Buffer.isBuffer(targetobj))
                            {
                                client.write(encrypt("buffer " + targetobj.byteLength));
                                sendingbuffer.i = 0;
                                sendingbuffer.buff = targetobj;
                                sendingbuffer.len=targetobj.length;

                            }
                            else
                                if (typeof (targetobj) == "object" || targetobj.constructor === Array)
                                {
                                    if (targetobj.constructor === Array)
                                        targetobj["!"] = true;
                                    client.write(encrypt("object " + Object.keys(targetobj).map((x) => { return x.split(",").join(",,"); }).join(",")));
                                }
                                else
                                    if (typeof (targetobj) == "boolean")
                                    {
                                        client.write(encrypt("boolean " + targetobj));
                                    }
                                    else
                                        if (typeof (targetobj) == "string")
                                        {
                                            client.write(encrypt("string " + targetobj));
                                        }
                                        else
                                            if (typeof (targetobj) == "number")
                                            {
                                                client.write(encrypt("number " + targetobj));
                                            }

                        }
                        else
                            if (data.substring(0, data.indexOf(" ")) == "iv")
                            {
                                var req = data.substring(data.indexOf(" ") + 1);

                                iv = new Buffer(req.split(",").map((x) => { return parseInt(x); }).splice(0, 16));
                                client.write(encrypt("gotiv"));
                            }
                            else
                                if (data.substring(0, data.indexOf(" ")) == "gettype")
                                {
                                    var req = data.substring(data.indexOf(" ") + 1);
                                    var path = req.split("/");
                                    var targetobj = obj;
                                    for (var i = 0; i < path.length; i++)
                                        if (path[i] != "")
                                            targetobj = targetobj[path[i]];
                                    client.write(encrypt("type " + typeof (targetobj)));
                                }
                    });
                });
            });

        }

        this.send = send;
    }
}
module.exports = Communicator;