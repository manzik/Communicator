import java.io.*;
import java.lang.reflect.Array;
import java.lang.reflect.Field;
import java.net.ServerSocket;
import java.net.Socket;
import java.security.GeneralSecurityException;
import java.security.InvalidAlgorithmParameterException;
import java.security.InvalidKeyException;
import java.security.KeyException;
import java.security.NoSuchAlgorithmException;
import java.security.SecureRandom;
import java.util.*;
import java.util.regex.Pattern;

import javax.crypto.BadPaddingException;
import javax.crypto.Cipher;
import javax.crypto.IllegalBlockSizeException;
import javax.crypto.NoSuchPaddingException;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;

class CommunicatorOptions {
    public String defaultreceiver;
    public String password;
    public String receiver;
    public int defaultport;
}

class CommunicatorObject
{
    public boolean IsVariable=false;
    public Map<String,CommunicatorObject> properties=new HashMap<String,CommunicatorObject>();
    public Object value=null;
    public void CopyToObject(Object obj) {
        try

        {
            CopyToObject(this, obj);
        }
        catch (IllegalAccessException e)
        {

        }
    }
    private void CopyToObject(CommunicatorObject co,Object obj) throws IllegalAccessException {
            Object[] properties=co.properties.keySet().toArray();
            List<String> valueproperties=new ArrayList<String>();
            List<String> objectproperties=new ArrayList<String>();
            for(int i=0;i<properties.length;i++)
            {

                CommunicatorObject property=co.properties.get(properties[i]);
                if(property.value!=null)
                {
                    valueproperties.add((String)properties[i]);
                }
                else
                    objectproperties.add((String)properties[i]);
            }

            for (Field field : obj.getClass().getDeclaredFields()) {
                field.setAccessible(true); // You might want to set modifier to public first.
                if(valueproperties.contains(field.getName()))
                {
                    Object valuee=co.properties.get(field.getName()).value;



                    if(field.get(obj) instanceof Float)
                        valuee=(float)valuee;
                    else
                    if(field.get(obj) instanceof Long)
                        valuee=(long)valuee;
                    else
                    if(field.get(obj) instanceof Short)
                        valuee=(short)valuee;
                    else
                    if(field.get(obj) instanceof Byte)
                        valuee=(byte)valuee;
                    if(field.getType().isArray()&&valuee.getClass().isArray()&&!valuee.getClass().equals(byte[].class))
                    {
                        Object arr=Array.newInstance(field.getType().getComponentType(),((Object[])valuee).length);
                        for(int i=0;i<((Object[]) valuee).length;i++)
                        {
                            Object retval=((Object[]) valuee)[i];

                            if(field.getType().equals(double[].class))
                                ((double[])arr)[i]=(double)retval;
                            else
                            if(field.getType().equals(float[].class))
                                ((float[])arr)[i]=((Double)retval).floatValue();
                            else
                            if(field.getType().equals(int[].class))
                                ((int[])arr)[i]=((Double)retval).intValue();
                            else
                            if(field.getType().equals(short[].class))
                                ((short[])arr)[i]=((Double)retval).shortValue();
                            else
                            if(field.getType().equals(byte[].class))
                                ((byte[])arr)[i]=(byte)((Double)retval).byteValue();
else
                            if(field.getType().equals(String[].class))
                                ((String[])arr)[i]=(String.valueOf(retval));
                            else
                            if(field.getType().equals(boolean[].class))
                                ((boolean[])arr)[i]=(boolean)retval;
                            else
                            if(field.getType().equals(byte[][].class))
                                ((byte[][])arr)[i]=(byte[])retval;


                        }

                        valuee=arr;
                    }

                    field.set(obj,valuee);
                }
                else
                    if(objectproperties.contains(field.getName()))
                    {
                        CopyToObject(co.properties.get(field.getName()),field.get(obj));
                    }
            }
    }
}
class Communicator {
    private class CommunicatorMessage
    {
        public String type;
        public Object val;
        CommunicatorMessage(Object _val,String _type)
        {
            type=_type;
            val=_val;
        }
    }
List<Class> validtypes=Arrays.asList(new Class[]{double.class,String.class,int.class,short.class,byte.class,float.class,boolean.class});
    List<Class> validarraytypes=Arrays.asList(new Class[]{double[].class,String[].class,int[].class,short[].class,byte[].class,float[].class,boolean[].class});
    public void Send(final Object newobj,final String type){
        new Thread(new Runnable() {
            public void run() {
        try

            {
                Object obj = new CommunicatorMessage(newobj, type);


                Socket socket = new Socket(options.receiver, options.defaultport);
                CommunicatorItem mainitem = new CommunicatorItem("/");
                CommunicatorConnection cc = new CommunicatorConnection(socket, true, mainitem);
                mainitem.connection = cc;
                cc.sendmsg("start");
                while (true) {
                    String out = cc.getmsg();

                    if (out.equals("close")) {
                        break;
                    }

                    String typee = out.substring(0, out.indexOf(" "));
                    String data = out.substring(out.indexOf(" ") + 1);

                    if (typee.equals("get")) {
                        Object targetobj = obj;
                        String[] paths = data.split("/");
                        for (int i = 0; i < paths.length; i++) {
                            if (paths[i].length() > 0) {
                                boolean isvalidarr = false;
                                for (int j = 0; j < validarraytypes.size(); j++)
                                    if (validarraytypes.get(j).equals(targetobj.getClass())) {
                                        isvalidarr = true;
                                        break;
                                    }

                                if (isvalidarr) {
                                    targetobj = Arrays.asList((Object[]) targetobj).get(Integer.valueOf(paths[i]));
                                } else
                                {
                                	try
                                	{
                                    targetobj = targetobj.getClass().getField(paths[i]).get(targetobj);
                                	}catch(java.lang.NoSuchFieldException e)
                                	{
                                		throw new Exception("All members in object should be declared as public in order to be accessible. comment out this line if you want to skip private objects.");
                                	}
                                }
                            }
                        }
                        if (targetobj.getClass().equals(byte[].class)) {
                            byte[] buffer = (byte[]) targetobj;
                            byte[] sendingbuffer = new byte[ChunkSize];
                            cc.sendmsg("buffer " + buffer.length);
                            int i = 0;
                            while (true) {
                                cc.getmsg();
                                if (i * ChunkSize >= buffer.length) {
                                    cc.sendmsg("donebuffer");
                                    break;
                                }
                                sendingbuffer = Arrays.copyOfRange(buffer, i * ChunkSize, Math.min(buffer.length, (i + 1) * ChunkSize));
                                i++;
                                cc.sendbuff(sendingbuffer);
                            }
                        } else if (targetobj instanceof Double || targetobj instanceof Float || targetobj instanceof Long || targetobj instanceof Short || targetobj instanceof Byte) {
                            cc.sendmsg("number " + String.valueOf(targetobj));
                        } else if (targetobj instanceof String) {
                            cc.sendmsg("string " + targetobj);
                        } else if (targetobj instanceof Boolean) {
                            cc.sendmsg("boolean " + (targetobj.equals(true) ? "true" : "false"));
                        } else if (validarraytypes.contains(targetobj.getClass())) {
                            String res = "object !,";
                            int size = Arrays.asList((Object[]) targetobj).size();
                            for (int i = 0; i < size; i++)
                                res += String.valueOf(i) + ",";
                            res = res.substring(0, res.length() - 1);
                            cc.sendmsg(res);
                        } else {
                            String res = "object ";
                            for (Field field : targetobj.getClass().getDeclaredFields()) {
                                if (!field.isSynthetic())
                                    res += field.getName() + ",";
                            }
                            if (res.length() > 0)
                                res = res.substring(0, res.length() - 1);

                            cc.sendmsg(res);
                        }
                    }
                }
                socket.close();
            }
        catch(
            Exception e)

            {
                e.printStackTrace();
            }
        }
        }).run();
    }
    public void SetDefaultReceiver(String receiver,int port)
    {
        options.defaultreceiver=receiver;
        options.defaultport=port;
    }
    private int ChunkSize=8192;
    public boolean listening=false;
    private static byte[] toByte(String hexString) {
        int len = hexString.length() / 2;
        byte[] result = new byte[len];
        for (int i = 0; i < len; i++) {
            result[i] = Integer.valueOf(hexString.substring(2 * i, 2 * i + 2), 16).byteValue();
        }
        return result;
    }
    Map<String,Callback> callbacks=new HashMap<String,Callback>();

    public void On(String event,Callback Callback)
    {
        callbacks.put(event,Callback);
    }
    public CommunicatorOptions options;

    public Communicator(CommunicatorOptions co) {
        options = co;
    }


    public static abstract class Callback<TArg> {
        public abstract void call(TArg val);
    }

    public void Listen(int port) {
        listening=true;
        try {
        final ServerSocket ssock=new ServerSocket(port);


            new Thread(new Runnable() {
                public void run() {
                    try {
                    while (listening) {

                        final Socket sock = ssock.accept();
                        new Thread(new Runnable() {
                            public void run() {
                                try {
                                    CommunicatorItem mainitem = new CommunicatorItem("/");
                                    CommunicatorConnection cc = new CommunicatorConnection(sock, false, mainitem);
                                    mainitem.connection = cc;
                                    cc.getmsg();
                                    CommunicatorItem notready = FindNotReady(mainitem);
                                    do {
                                        notready.getitems();

                                        notready = FindNotReady(mainitem);

                                    }
                                    while (notready != null);

                                    cc.sendmsg("close");
                                    sock.close();
                                    CommunicatorObject co = cc.mainitem.ToCommunicatorObject();

                                    if (callbacks.get(co.properties.get("type").value) != null) {
                                        callbacks.get(co.properties.get("type").value).call(co.properties.get("val"));
                                    }


                                } catch (Exception e) {
                                    System.out.println(e.getMessage());
                                }
                            }
                        }).start();

                    }
                    }
                    catch(Exception e)
                    {

                    }
                }}).start();
        }
        catch (Exception e)
        {

        }
    }
    private class CommunicatorItem
    {

        private boolean isready=false;
        public Object value=null;
        private CommunicatorConnection connection;
        private String path="";

        CommunicatorItem(String _path)
        {
            path=_path;
        }
        public void getitems() throws GeneralSecurityException, IOException {
            connection.sendmsg("get "+this.path);

            String msg=connection.getmsg();
            String type=msg.substring(0,msg.indexOf(" "));
            String req=msg.substring(msg.indexOf(" ")+1);
            if(type.equals("object"))
            {
                String[] reqitems=req.split(",");
                this.items=new CommunicatorItem[reqitems.length];
                for(int i=0;i<reqitems.length;i++)
                {
                    this.items[i]=new CommunicatorItem(this.path+"/"+reqitems[i]);
                    this.items[i].connection=connection;
                }
            }
            else
                if(type.equals("string"))
                {
                    this.value=req.toString();
                }
                else
                    if(type.equals("number"))
                    {
                        this.value=Double.valueOf(req);
                    }
                    else
                        if(type.equals("boolean"))
                        {
                            this.value=req=="true";
                        }
                        else
                            if(type.equals("buffer"))
                            {
                                byte[] buffer=new byte[Integer.valueOf(req)];

                                connection.sendmsg("getnextbuffchunk");
                                int i=0;
                                    byte[] chunkdata=connection.getbuff();
                                    while (!(new String(chunkdata, "UTF-8")).equals("donebuffer"))
                                    {
                                        System.arraycopy(chunkdata,0,buffer,i*ChunkSize,Math.min(ChunkSize,buffer.length-i*ChunkSize));
                                        connection.sendmsg("getnextbuffchunk");
                                        chunkdata=connection.getbuff();
                                        i++;
                                    }

                                this.value=buffer;

                            }
            this.isready=true;
        }
        private boolean isarray(CommunicatorObject co)
        {
            Object[] set=co.properties.keySet().toArray();
            for(int i=0;i<set.length;i++)
            {
                if(((String)set[i]).equals("!"))
                    return true;
                else
                if(! isDigitsOnly((String)set[i]))
                    return false;
            }
            return true;
        }
        // https://stackoverflow.com/a/39532109
        public boolean isDigitsOnly(CharSequence str) {
            final int len = str.length();
            for (int i = 0; i < len; i++) {
                if (!Character.isDigit(str.charAt(i))) {
                    return false;
                }
            }
            return true;
        }
        public CommunicatorObject ToCommunicatorObject()
        {
            return ToCommunicatorObject(this);
        }
        public CommunicatorObject ToCommunicatorObject(CommunicatorItem item)
        {
            CommunicatorObject CO=new CommunicatorObject();
            if(item.items!=null)
            {
                for(int i=0;i<item.items.length;i++)
                {
                    CommunicatorItem ci=item.items[i];
                    String cipath=ci.path;
                    CO.properties.put(cipath.substring(cipath.lastIndexOf("/")+1,cipath.length()),ToCommunicatorObject(ci));
                }

                if(isarray(CO))
                {
                    Object[] arr=new Object[CO.properties.keySet().size()-1];

                    for(int i=0;i<arr.length;i++)
                    {
                        arr[i]=CO.properties.get(String.valueOf(i)).value;
                    }

                    CO.properties.clear();
                    CO.value=arr;
                }
            }
            else
                CO.value=item.value;

            return CO;
        }
        public CommunicatorItem[] items=null;
    }
    private CommunicatorItem FindNotReady(CommunicatorItem item)
    {
        if(!item.isready)
            return item;
        else
            if(item.items!=null)
            {
                for(int i=0;i<item.items.length;i++) {
                    CommunicatorItem notready = FindNotReady(item.items[i]);
                    if(notready!=null)
                        return notready;
                }
            }
            return null;
    }
    private class CommunicatorConnection {
        byte[] iv = new byte[]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        boolean issender=false;
        Socket socket;
        CommunicatorItem mainitem=null;
        CommunicatorConnection(Socket sc,boolean sender,CommunicatorItem mi)
        {
            socket=sc;
            issender=sender;
            mainitem=mi;
        }
        private final String characterEncoding = "UTF-8";
        private final String cipherTransformation = "AES/CBC/PKCS5Padding";
        private final String aesEncryptionAlgorithm = "AES";
        public String getmsg() throws GeneralSecurityException, IOException {
            String dec="";
                DataInputStream dis = new DataInputStream(socket.getInputStream());
                byte[] bytes = new byte[ChunkSize];
                dis.read(bytes);
                String str = new String(bytes, "UTF-8");
                dec = this.decrypt(str, options.password);

            return dec;
        }
        public byte[] getbuff() throws IOException, GeneralSecurityException {
            DataInputStream dis = new DataInputStream(socket.getInputStream());
            byte[] bytes = new byte[ChunkSize*2];
            dis.read(bytes);
            byte[] result=decryptbuffer(new String(bytes, "UTF-8"),options.password);
            String strres=((new String(result, "UTF-8")));
            if(strres.length()>"donebuffer".length())
            {
                if(strres.substring(0,"donebuffer".length()).equals("donebuffer"))
                {
                    this.decrypt(new String(bytes, "UTF-8"), options.password);

                    result=("donebuffer").getBytes();
                }
            }
            /*
            if((new String(result, "UTF-8")).substring(0,(new String(result, "UTF-8")).indexOf(" ")>-1?(new String(result, "UTF-8")).indexOf(" "):0).equals("donebuffer"))
            {
                this.decrypt((new String(result, "UTF-8")), options.password);
            }
            */
            return result;
        }
        public void sendbuff(byte[] buffer) throws IOException, GeneralSecurityException {
            DataOutputStream dis = new DataOutputStream(socket.getOutputStream());
            byte[] bytes=encryptbuffer(buffer,options.password);
            bytes = Base64.getEncoder().encodeToString(bytes).getBytes("UTF-8");
            dis.write(bytes);
        }
        public void sendmsg(String msg) throws NoSuchPaddingException, InvalidAlgorithmParameterException, IOException, IllegalBlockSizeException, BadPaddingException, NoSuchAlgorithmException, InvalidKeyException {
                DataOutputStream dis = new DataOutputStream(socket.getOutputStream());
                msg=encrypt(msg,options.password);
                byte[] bytes = msg.getBytes("UTF-8");
                dis.write(bytes);
        }
        public byte[] decrypt(byte[] cipherText, byte[] key, byte[] initialVector) throws NoSuchAlgorithmException, NoSuchPaddingException, InvalidKeyException, InvalidAlgorithmParameterException, IllegalBlockSizeException, BadPaddingException {
            Cipher cipher = Cipher.getInstance(cipherTransformation);
            SecretKeySpec secretKeySpecy = new SecretKeySpec(key, aesEncryptionAlgorithm);
            IvParameterSpec ivParameterSpec = new IvParameterSpec(initialVector);
            cipher.init(Cipher.DECRYPT_MODE, secretKeySpecy, ivParameterSpec);
            cipherText = cipher.doFinal(cipherText);
            return cipherText;
        }

        public byte[] encrypt(byte[] plainText, byte[] key, byte[] initialVector) throws NoSuchAlgorithmException, NoSuchPaddingException, InvalidKeyException, InvalidAlgorithmParameterException, IllegalBlockSizeException, BadPaddingException {
            Cipher cipher = Cipher.getInstance(cipherTransformation);
            SecretKeySpec secretKeySpec = new SecretKeySpec(key, aesEncryptionAlgorithm);
            IvParameterSpec ivParameterSpec = new IvParameterSpec(initialVector);
            cipher.init(Cipher.ENCRYPT_MODE, secretKeySpec, ivParameterSpec);
            plainText = cipher.doFinal(plainText);
            return plainText;
        }

        private byte[] getKeyBytes(String key) throws UnsupportedEncodingException {
            byte[] keyBytes = new byte[16];
            byte[] parameterKeyBytes = key.getBytes(characterEncoding);
            System.arraycopy(parameterKeyBytes, 0, keyBytes, 0, Math.min(parameterKeyBytes.length, keyBytes.length));
            return keyBytes;
        }

        public String encrypt(String plainText, String key) throws UnsupportedEncodingException, InvalidKeyException, NoSuchAlgorithmException, NoSuchPaddingException, InvalidAlgorithmParameterException, IllegalBlockSizeException, BadPaddingException {

            byte[] keyBytes = getKeyBytes(key);
            byte[] newiv=null;
            if(issender)
            {
                newiv=new byte[16];
                SecureRandom random = new SecureRandom();
                random.nextBytes(newiv);

                plainText+=" ";

                for(int i=0;i<16;i++)
                    plainText+=String.valueOf(newiv[i]+0)+",";

                plainText=plainText.substring(0,plainText.length()-1);
            }
            byte[] plainTextbytes = plainText.getBytes(characterEncoding);
            String result= Base64.getEncoder().encodeToString(encrypt(plainTextbytes, keyBytes, iv));
            if(issender)
            iv=newiv;
            return result;
        }
        public byte[] encryptbuffer(byte[] buffer,String key) throws NoSuchPaddingException, InvalidAlgorithmParameterException, NoSuchAlgorithmException, IllegalBlockSizeException, BadPaddingException, InvalidKeyException, UnsupportedEncodingException {
            byte[] keyBytes = getKeyBytes(key);
            return encrypt(buffer,keyBytes,iv);
        }
        public byte[] decryptbuffer(String encryptedText,String key) throws KeyException, GeneralSecurityException, GeneralSecurityException, InvalidAlgorithmParameterException, IllegalBlockSizeException, BadPaddingException, IOException
        {
            byte[] cipheredBytes = Base64.getDecoder().decode(encryptedText.trim());
            byte[] keyBytes = getKeyBytes(key);
            byte[] result=decrypt(cipheredBytes, keyBytes, iv);

            return result;
        }

        public String decrypt(String encryptedText, String key) throws KeyException, GeneralSecurityException, GeneralSecurityException, InvalidAlgorithmParameterException, IllegalBlockSizeException, BadPaddingException, IOException {
            byte[] cipheredBytes = Base64.getDecoder().decode(encryptedText.trim());
            byte[] keyBytes = getKeyBytes(key);
            String result=new String(decrypt(cipheredBytes, keyBytes, iv), characterEncoding);
            if(!issender)
            {
                String[] ivstr=result.substring(result.lastIndexOf(' ')+1).split(Pattern.quote(","));
                for(int i=0;i<ivstr.length;i++)
                    iv[i]=(byte)(Integer.valueOf(ivstr[i])+0);
                result=result.substring(0,result.lastIndexOf(' '));
            }
            return result;
        }
    }


}
