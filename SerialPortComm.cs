using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Xml;

public class SerialPortComm : ServiceBase
{
    public static string portName { get; set; }
    public static int baudRate { get; set; }
    public static Parity parity { get; set; }
    public static int dataBits { get; set; }
    public static StopBits stopBits { get; set; }
    public static SerialPort serialPort { get; set; }
    public static Handshake handshake { get; set; }
    public static string windowSvcName { get; set; }
    public static string fileLocation { get; set; }
    public static string applicationUrl { get; set; }

    static bool _continue;
    static SerialPort _serialPort;
    
    public static void LoadConfigurations()
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(Path.Combine(Environment.CurrentDirectory, "Config.xml"));
        XmlNode myNodes = xmlDoc.SelectSingleNode("/Parent");
        portName = myNodes["PortName"].InnerText;
        baudRate = Convert.ToInt32(myNodes["BaudRate"].InnerText);
        parity = (Parity)Enum.Parse(typeof(Parity), myNodes["Parity"].InnerText, true);
        dataBits = Convert.ToInt16(myNodes["Databits"].InnerText);
        stopBits = (StopBits)Enum.Parse(typeof(StopBits), myNodes["StopBits"].InnerText, true);
        handshake  = (Handshake)Enum.Parse(typeof(Handshake), myNodes["Handshake"].InnerText, true);
        windowSvcName = myNodes["WindowsServiceName"].InnerText;
        fileLocation = myNodes["LoggerFile"].InnerText;
        applicationUrl = myNodes["ApplicationURL"].InnerText;

    }

    static void Main(string[] args)
    {
        LoadConfigurations();
        
        if (!Environment.UserInteractive)
            // running as service
            using (var service = new SerialPortComm())
                ServiceBase.Run(service);
        else
        {
            // running as console app
            Start(args);

            Console.WriteLine("Press any key to stop...");
            Console.ReadKey(true);

            Stop();
        }
    }

    protected override void OnStart(string[] args)
    {
        SerialPortComm.Start(args);
    }

    protected override void OnStop()
    {
        SerialPortComm.Stop();
    }

    private static void Start(string[] args)
    {
       
        string Path = fileLocation + "\\" + DateTime.Now.ToString("dd_MM_yyyy") + "_Logger.log";
        FileStream fileStream = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        using (StreamWriter sw = new StreamWriter(fileStream))
        {
            sw.WriteLine("StartLog: Log File Used: " + Path + "|" + DateTime.Now);

            sw.WriteLine("Loaded Configurations: ");
            sw.WriteLine("-portName: " + portName);
            sw.WriteLine("-baudRate: " + baudRate);
            sw.WriteLine("-parity: " + parity);
            sw.WriteLine("-dataBits: " + dataBits);
            sw.WriteLine("-stopBits: " + stopBits);
            sw.WriteLine("-handshake: " + handshake);
        }
        string message;
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        Thread readThread = new Thread(Read);

        // Create a new SerialPort object with default settings.  
        _serialPort = new SerialPort();
        
        // Allow the user to set the appropriate properties.  
        _serialPort.PortName = portName;
        _serialPort.BaudRate = baudRate;
        _serialPort.Parity = parity;
        _serialPort.DataBits = dataBits;
        _serialPort.StopBits = stopBits;
        _serialPort.Handshake = handshake;

        // Set the read/write timeouts  
        _serialPort.ReadTimeout = 500;
        _serialPort.WriteTimeout = 500;
        //sw.WriteLine("Opening Serial Port : PortName:"+ portName);
        _serialPort.Open();
        // sw.WriteLine("Opened Serial Port : PortName:" + portName);
        _continue = true;

        //sw.WriteLine("Reading the thread for logging the responsefor PortName:" + portName);
        readThread.Start();


        //sw.WriteLine("System should log the response return from Serial Port which is Configured\n");
        //sw.WriteLine("Type QUIT to exit..");

        while (_continue)
        {
            message = Console.ReadLine();

            if (stringComparer.Equals("quit", message))
            {
                _continue = false;
            }
        }

        readThread.Join();
        _serialPort.Close();
    }

    private static void Stop()
    {

        string Path = fileLocation + "\\" + DateTime.Now.ToString("dd_MM_yyyy") + "_Logger.log";
        FileStream fileStream = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        using (StreamWriter sr = new StreamWriter(Path))
        {
            sr.WriteLine("stopping...............");
        }


        // Create a new SerialPort object with default settings.  
        _serialPort = new SerialPort();

        // Allow the user to set the appropriate properties.  
        _serialPort.PortName = portName;
        _serialPort.BaudRate = baudRate;
        _serialPort.Parity = parity;
        _serialPort.DataBits = dataBits;
        _serialPort.StopBits = stopBits;
        _serialPort.Handshake = handshake;

        // Set the read/write timeouts  
        _serialPort.ReadTimeout = 500;
        _serialPort.WriteTimeout = 500;
        if (_serialPort.IsOpen)
        {
            _serialPort.Close();
        }
        using (StreamWriter sr = new StreamWriter(Path))
        {
            sr.WriteLine("stopped service successfully");
        }
    }


    public static void Read()
    {
        string Path = fileLocation + "\\" + DateTime.Now.ToString("dd_MM_yyyy") + "_Logger.log";
        using (StreamWriter sr = new StreamWriter(Path, true))
        {
            sr.WriteLine("Reading the logs");
        }

        while (_continue)
        {
            try
            {
                string message = _serialPort.ReadLine();
                if (!string.IsNullOrEmpty(message))
                {
                    using (StreamWriter sr = new StreamWriter(Path))
                    {
                        sr.WriteLine(message);
                        string url = applicationUrl + "/Home/Post?parameter="+ message;
                        string details = CallRestMethod(url);

                    }
                }
            }
            catch (System.TimeoutException ex)
            {
                using (StreamWriter sr = new StreamWriter(Path))
                {
                    sr.WriteLine(ex.Message + "\nStack Trace:" + ex.StackTrace.ToString());
                }
            }
        }
    }

    public static string CallRestMethod(string url)
    {
        
        HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
        webrequest.Method = "POST";
        webrequest.ContentType = "text/xml;charset=UTF-8";
        webrequest.Headers.Add("SOAPAction", "\"\"");

        String input = "<Envelope xmlns=\"http://schemas.xmlsoap.org/soap/envelope/\"><Header/><Body><login xmlns=\"urn:partner.soap.sforce.com\"></Body></Envelope>";
        ASCIIEncoding encoding = new ASCIIEncoding();
        byte[] byte1 = encoding.GetBytes(input);

        webrequest.GetRequestStream().Write(byte1, 0, byte1.Length);

        /*Stream newStream = webrequest.GetRequestStream();
        newStream.Write(byte1, 0, byte1.Length);
        newStream.Close();*/

        Console.WriteLine(webrequest.Headers);

        HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();
        Encoding enc = System.Text.Encoding.GetEncoding("utf-8");
        StreamReader responseStream = new StreamReader(webresponse.GetResponseStream(), enc);
        string result = string.Empty;
        result = responseStream.ReadToEnd();
        webresponse.Close();
        return result;
    }

}