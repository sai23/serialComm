using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;

public class PortChat
{
    public static string portName { get; set; }
    public static string applicationPath{ get; set; }
    public static int baudRate { get; set; }
    public static Parity parity { get; set; }
    public static int dataBits { get; set; }
    public static StopBits stopBits { get; set; }
    public static SerialPort serialPort { get; set; }
    public static Handshake handshake { get; set; }

    public PortChat()
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(Path.Combine(Environment.CurrentDirectory, "Config.xml"));
        XmlNode myNodes = xmlDoc.SelectSingleNode("/Parent");
        portName = myNodes["PortName"].InnerText;
        baudRate = Convert.ToInt32(myNodes["BaudRate"].InnerText);
        parity = (Parity)Enum.Parse(typeof(Parity), myNodes["Parity"].InnerText, true);
        dataBits = Convert.ToInt16(myNodes["Databits"].InnerText);
        stopBits = (StopBits)Enum.Parse(typeof(StopBits), myNodes["StopBits"].InnerText, true);
        applicationPath = myNodes["ApplicationURL"].InnerText;
        
    }

    static bool _continue;
    static SerialPort _serialPort;

    public static void Main()
    {
        string name;
        string message;
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        Thread readThread = new Thread(Read);

        // Create a new SerialPort object with default settings.  
        _serialPort = new SerialPort();
        
        // Allow the user to set the appropriate properties.  
        _serialPort.PortName = portName;
        _serialPort.BaudRate =baudRate;
        _serialPort.Parity = parity;
        _serialPort.DataBits = dataBits;
        _serialPort.StopBits = stopBits;
        _serialPort.Handshake = handshake;
        
        // Set the read/write timeouts  
        _serialPort.ReadTimeout = 500;
        _serialPort.WriteTimeout = 500;

        _serialPort.Open();
        _continue = true;
        readThread.Start();

        Console.Write("Name: ");
        name = Console.ReadLine();

        Console.WriteLine("Type QUIT to exit");

        while (_continue)
        {
            message = Console.ReadLine();

            if (stringComparer.Equals("quit", message))
            {
                _continue = false;
            }
            else
            {
                _serialPort.WriteLine(
                    String.Format("<{0}>: {1}", name, message));
            }
        }

        readThread.Join();
        _serialPort.Close();
    }

    public static void Read()
    {
        while (_continue)
        {
            try
            {
                string message = _serialPort.ReadLine();
                string url = applicationPath + "/Home/Post?parameter=" + message;
                string details = CallRestMethod(url);

            }
            catch (TimeoutException) { }
        }
    }

    public static string SetPortName(string defaultPortName)
    {
        string portName;

        Console.WriteLine("Available Ports:");
        foreach (string s in SerialPort.GetPortNames())
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("COM port({0}): ", defaultPortName);
        portName = Console.ReadLine();

        if (portName == "")
        {
            portName = defaultPortName;
        }
        return portName;
    }

    public static int SetPortBaudRate(int defaultPortBaudRate)
    {
        string baudRate;

        Console.Write("Baud Rate({0}): ", defaultPortBaudRate);
        baudRate = Console.ReadLine();

        if (baudRate == "")
        {
            baudRate = defaultPortBaudRate.ToString();
        }

        return int.Parse(baudRate);
    }

    public static Parity SetPortParity(Parity defaultPortParity)
    {
        string parity;

        Console.WriteLine("Available Parity options:");
        foreach (string s in Enum.GetNames(typeof(Parity)))
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Parity({0}):", defaultPortParity.ToString());
        parity = Console.ReadLine();

        if (parity == "")
        {
            parity = defaultPortParity.ToString();
        }

        return (Parity)Enum.Parse(typeof(Parity), parity);
    }

    public static int SetPortDataBits(int defaultPortDataBits)
    {
        string dataBits;

        Console.Write("Data Bits({0}): ", defaultPortDataBits);
        dataBits = Console.ReadLine();

        if (dataBits == "")
        {
            dataBits = defaultPortDataBits.ToString();
        }

        return int.Parse(dataBits);
    }

    public static StopBits SetPortStopBits(StopBits defaultPortStopBits)
    {
        string stopBits;

        Console.WriteLine("Available Stop Bits options:");
        foreach (string s in Enum.GetNames(typeof(StopBits)))
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Stop Bits({0}):", defaultPortStopBits.ToString());
        stopBits = Console.ReadLine();

        if (stopBits == "")
        {
            stopBits = defaultPortStopBits.ToString();
        }

        return (StopBits)Enum.Parse(typeof(StopBits), stopBits);
    }

    public static Handshake SetPortHandshake(Handshake defaultPortHandshake)
    {
        string handshake;

        Console.WriteLine("Available Handshake options:");
        foreach (string s in Enum.GetNames(typeof(Handshake)))
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Handshake({0}):", defaultPortHandshake.ToString());
        handshake = Console.ReadLine();

        if (handshake == "")
        {
            handshake = defaultPortHandshake.ToString();
        }

        return (Handshake)Enum.Parse(typeof(Handshake), handshake);
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