using Ex3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace Ex3.Controllers
{
    public class WebSimulatorController : Controller
    {
        private bool connected = false;
        // GET: WebSimulator
        public ActionResult Index()
        {
            return View();
        }
        //decides which option to run, becuase we have two options with the same Rest structure...
        [HttpGet]
        public ActionResult decide(string str, int num)
        {
            System.Net.IPAddress ipa;
            if (System.Net.IPAddress.TryParse(str, out ipa))
            {
                return displayPos(str, num);
            }
            return loadFlightData(str, num);
        }
        //gets current position of plane simulated of FlightGear and displays the position on the map
        public ActionResult displayPos(string ip, int port)
        {
            WebSimulatorModel model = WebSimulatorModel.getInstance();
            if (connected == true)
            {
                model.disconnect();
                connected = false;
            }
            if (!model.connect(ip, port))
            {
                return View("Error");
            }
            connected = true;
            Tuple<double, double> position = model.getPosition();
            if (position == null)
            {
                model.disconnect();
                connected = false;
                return View("Error");
            }
            //add lon and lat to the view beg in order to share it with the view.
            ViewBag.lon = position.Item1;
            ViewBag.lat = position.Item2;
            return View("FindLocationView");
        }
        //gets current position of the plane simulated on FlightGear each sec seconds
        [HttpGet]
        public ActionResult displayPosPerSec(string ip, int port, int sec)
        {
            WebSimulatorModel model = WebSimulatorModel.getInstance();
            if (connected == true)
            {
                model.disconnect();
                connected = false;
            }
            if (!model.connect(ip, port))
            {
                return View("Error");
            }
            connected = true;
            Tuple<double, double> position = model.getPosition();
            if (position == null)
            {
                model.disconnect();
                connected = false;
                return View("Error");
            }
            ViewBag.beginLon = position.Item1;
            ViewBag.beginLat = position.Item2;
            ViewBag.refreshRate = sec;
            return View("FindRouteView");
        }
        //convers inputed coordinates string into XML file, to be sent back to the requesting javaScript using Ajax and Jquery
        private string ToXML(string coordinates)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            XmlWriter writer = XmlWriter.Create(sb, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("coordinates");
            string[] temp = coordinates.Split(',');
            writer.WriteElementString("Lon", temp[0]);
            writer.WriteElementString("Lat", temp[1]);
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            return sb.ToString();
        }
        //return XML representation of current position of plane simulated on FlightGear
        [HttpPost]
        public string getPosition()
        {
            WebSimulatorModel model = WebSimulatorModel.getInstance();
            Tuple<double, double> position = model.getPosition();
            if (position == null)
            {
                model.disconnect();
                connected = false;
                return ToXML("200,200");
            }
            return ToXML(position.Item1.ToString() + "," + position.Item2.ToString());
        }
        //saves flight data of plane simulated on FlightGear for inputed number of seconds, and inputed number of data requests from server per second
        [HttpGet]
        public ActionResult saveFlightData(string ip, int port, int numSeconds, int perSecond, string filename)
        {
            WebSimulatorModel model = WebSimulatorModel.getInstance();
            if (connected == true)
            {
                model.disconnect();
                connected = false;
            }
            if (!model.connect(ip, port))
            {
                return View("Error");
            }
            connected = true;
            if (!model.saveFlightDetails(perSecond, numSeconds, filename))
            {
                return View("Error");
            }
            return View("Save");
        }
        //loads flight data from database and presents them on map
        public ActionResult loadFlightData(string filename, int perSec)
        {
            WebSimulatorModel model = WebSimulatorModel.getInstance();
            if (!model.loadFlightDetails(filename))
            {
                return View("Error");
            }
            Tuple<double, double> position = model.getPositionFromFile();
            if (position == null)
            {
                return View("Error");
            }
            ViewBag.beginLon = position.Item1;
            ViewBag.beginLat = position.Item2;
            ViewBag.refreshRate = perSec;
            return View("LoadedFromFileView");
        }
        //return XML representation of current position saved in the file already loaded
        [HttpPost]
        public string getPositionFromFile()
        {
            WebSimulatorModel model = WebSimulatorModel.getInstance();
            Tuple<double, double> position = model.getPositionFromFile();
            if (position == null)
            {
                model.disconnect();
                connected = false;
                return ToXML("200,200");
            }
            return ToXML(position.Item1.ToString() + "," + position.Item2.ToString());
        }
    }
}