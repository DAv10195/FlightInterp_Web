using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;

namespace WebApplication1.Models
{   //model of our web application
    public class WebSimulatorModel
    {
        private const int buffSize = 64; //buffer size constant
        private TcpClient channel;
        private NetworkStream strm;
        private ASCIIEncoding enc;
        private byte[] reader;
        private byte[] writer;
        private bool connected;
        private Dictionary<string, string> getPaths;
        //for file reading/writing
        private const string virtualPath = "~/App_Data/{0}";
        private List<Tuple<double, double>> loadedFromFile;
        private int indexFromFile;
        private List<Tuple<double, double, double, double>> toBeSaved;
        private string filename;
        private bool toSave;
        //instance
        private static WebSimulatorModel instance = null;
        //get instance
        public static WebSimulatorModel getInstance()
        {
            if (instance == null)
            {
                instance = new WebSimulatorModel();
            }
            return instance;
        }
        //CTOR
        private WebSimulatorModel()
        {
            channel = null;
            strm = null;
            enc = new ASCIIEncoding();
            reader = new byte[buffSize];
            writer = new byte[buffSize];
            connected = false;
            getPaths = new Dictionary<string, string>();
            getPaths.Add("Lat", "get /position/latitude-deg\r\n");
            getPaths.Add("Lon", "get /position/longitude-deg \r\n");
            getPaths.Add("Throttle", "get /controls/engines/current-engine/throttle\r\n");
            getPaths.Add("Rudder", "get /controls/flight/rudder\r\n");
            loadedFromFile = null;
            indexFromFile = 0;
            toBeSaved = new List<Tuple<double, double, double, double>>();
            filename = "";
            toSave = false;
        }
        //connect to FlightGear simulator
        public bool connect(string ip, int port)
        {
            try
            {
                IPAddress ipa;
                if (!IPAddress.TryParse(ip, out ipa))
                {
                    return false;
                }

                channel = new TcpClient();
                channel.Connect(new IPEndPoint(ipa, port));
                strm = channel.GetStream();
                connected = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        //disconnects from FlightGear
        public void disconnect()
        {
            if (strm != null) { strm.Close(); }
            if (channel != null) { channel.Close(); }
            connected = false;
        }
        //returns if model is connected to FlightGear or not
        public bool if_connected()
        {
            return connected;
        }
        //get the value asked for from the message
        private double parseMessage(string message)
        {
            int length = message.Length, i = 0, j = 0;
            double toRet;
            for (; i < length; ++i) //find first ' character
            {
                if (message.ElementAt(i) == '\'')
                {
                    break;
                }
            }
            if (i == length - 1)   //case not found...
            {
                return Double.NaN;
            }
            j = i + 1;
            for (; j < length; ++j) //find second ' character
            {
                if (message.ElementAt(j) == '\'')
                {
                    break;
                }
            }
            if (j == length - 1)   //case not found...
            {
                return Double.NaN;
            }
            message = message.Substring(i + 1, j - i - 1);  //+1 and -1 to get rid of ' character
            if (Double.TryParse(message, out toRet))
            {
                return toRet;
            }
            return Double.NaN;
        }
        //returns coordinates of the plane simulated on FlightGear
        public Tuple<double, double> getPosition()
        {
            try
            {
                writer = enc.GetBytes(getPaths["Lat"]);
                strm.Write(writer, 0, writer.Length);
                strm.Flush();
                strm.Read(reader, 0, reader.Length);
                double lat = parseMessage(enc.GetString(reader));
                if (lat == Double.NaN)
                {
                    return null;
                }
                Array.Clear(reader, 0, reader.Length);
                Array.Clear(writer, 0, writer.Length);
                writer = enc.GetBytes(getPaths["Lon"]);
                strm.Write(writer, 0, writer.Length);
                strm.Flush();
                strm.Read(reader, 0, reader.Length);
                double lon = parseMessage(enc.GetString(reader));
                if (lon == Double.NaN)
                {
                    return null;
                }
                Array.Clear(reader, 0, reader.Length);
                Array.Clear(writer, 0, writer.Length);
                if (toSave == true)
                {
                    double throttle = measureThrottleOrRudder("Throttle");
                    double rudder = measureThrottleOrRudder("Rudder");
                    if (throttle == Double.NaN || rudder == Double.NaN)
                    {
                        return null;
                    }
                    toBeSaved.Add(new Tuple<double, double, double, double>(lon, lat, throttle, rudder));
                }
                return new Tuple<double, double>(lon, lat);
            }
            catch (Exception)
            {
                return null;
            }
        }
        //get throttle value from FlightGear
        private double measureThrottleOrRudder(string whatToMeasure)
        {
            string path;
            if (!getPaths.TryGetValue(whatToMeasure, out path))
            {
                return Double.NaN;
            }
            try
            {
                writer = enc.GetBytes(path);
                strm.Write(writer, 0, writer.Length);
                strm.Flush();
                strm.Read(reader, 0, reader.Length);
                double throttle = parseMessage(enc.GetString(reader));
                if (throttle == Double.NaN)
                {
                    return Double.NaN;
                }
                Array.Clear(reader, 0, reader.Length);
                Array.Clear(writer, 0, writer.Length);
                return throttle;
            }
            catch (Exception)
            {
                return Double.NaN;
            }
        }
        //load list of Flight coordinates saved in the given file path
        public bool loadFlightDetails(string file_name)
        {
            string path = HttpContext.Current.Server.MapPath(String.Format(virtualPath, file_name));
            if (!File.Exists(path))
            {
                return false;
            }
            StreamReader fs;
            try
            {
                fs = new StreamReader(path);
            }
            catch (Exception)
            {
                return false;
            }
            string line;
            string[] vals;
            List<Tuple<double, double>> fromFile = new List<Tuple<double, double>>();
            try
            {
                while ((line = fs.ReadLine()) != null)
                {
                    vals = line.Split(',');
                    if (vals.Length < 2)
                    {
                        return false;
                    }
                    double lon, lat;
                    if (!Double.TryParse(vals[0], out lon) || !Double.TryParse(vals[1], out lat))
                    {
                        return false;
                    }
                    fromFile.Add(new Tuple<double, double>(lon, lat));
                }
                fs.Close();
            }
            catch (Exception)
            {
                fs.Close();
                return false;
            }
            loadedFromFile = fromFile;
            return true;
        }
        //get current position of plane simulated on FlightGear saved in the file we were instructed to open and save its content
        //in loadFlightDetails
        public Tuple<double, double> getPositionFromFile()
        {
            if (loadedFromFile == null || indexFromFile == loadedFromFile.Count)
            {
                indexFromFile = 0;
                loadedFromFile = null;
                return null;
            }
            Tuple<double, double> toRet = loadedFromFile[indexFromFile];
            indexFromFile++;
            return toRet;
        }
        //turn on saving mode on
        public void saveModeOn()
        {
            toSave = true;
        }
        //turn on saving mode on
        public void saveModeOff()
        {
            toSave = false;
        }
        //setter for filename where flight details will be saved to
        public void setFileName(string toSet)
        {
            if (toSet == null || toSet == "")
            {
                return;
            }
            filename = toSet;
        }
        //write all saved data to file
        public bool writeToFile()
        {
            bool first_time = true;
            if (filename == "")
            {
                toBeSaved.Clear();
                toSave = false;
                return false;
            }
            string path = HttpContext.Current.Server.MapPath(String.Format(virtualPath, filename));
            FileStream f;   //open or overwrite file and start writing to it
            try
            {
                f = File.Create(path);
            }
            catch (Exception)
            {
                toBeSaved.Clear();
                toSave = false;
                return false;
            }
            StreamWriter fs;
            try
            {
                fs = new StreamWriter(f);
            }
            catch (Exception)
            {
                toBeSaved.Clear();
                toSave = false;
                f.Close();
                return false;
            }
            try
            {
                foreach (Tuple<double, double, double, double> elem in toBeSaved)
                {
                    if (first_time == true)
                    {
                        fs.Write((elem.Item1).ToString() + "," + (elem.Item2).ToString() + "," + (elem.Item3).ToString() + "," + (elem.Item4).ToString());
                        first_time = false;
                    }
                    else
                    {
                        fs.Write("\n" + (elem.Item1).ToString() + "," + (elem.Item2).ToString() + "," + (elem.Item3).ToString() + "," + (elem.Item4).ToString());
                    }
                }
            }
            catch (Exception)
            {
                toBeSaved.Clear();
                toSave = false;
                fs.Close();
                return false;
            }
            fs.Close();
            toBeSaved.Clear();
            toSave = false;
            return true;
        }
    }
}