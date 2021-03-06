﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class WebSimulatorController : Controller
    {
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
            if (!model.if_connected())
            {
                model.disconnect();
            }
            if (!model.connect(ip, port))
            {
                return View("Error");
            }
            Tuple<double, double> position = model.getPosition();
            if (position == null)
            {
                model.disconnect();
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
            if (!model.if_connected())
            {
                model.disconnect();
            }
            if (!model.connect(ip, port))
            {
                return View("Error");
            }
            Tuple<double, double> position = model.getPosition();
            if (position == null)
            {
                model.disconnect();
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
                return ToXML("200,200"); //failure
            }
            return ToXML(position.Item1.ToString() + "," + position.Item2.ToString());
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
                return ToXML("200,200"); //failure/end
            }
            return ToXML(position.Item1.ToString() + "," + position.Item2.ToString());
        }
        //save flight details of current plane simulated on FlightGear to given filename
        [HttpGet]
        public ActionResult saveFlightData(string ip, int port, int perSecond, int numSeconds, string filename)
        {
            if (filename == "")
            {
                return View("Error");
            }
            WebSimulatorModel model = WebSimulatorModel.getInstance();
            if (!model.if_connected())
            {
                model.disconnect();
            }
            if (!model.connect(ip, port))
            {
                return View("Error");
            }
            model.saveModeOn();
            model.setFileName(filename);
            Tuple<double, double> position = model.getPosition();
            if (position == null)
            {
                model.disconnect();
                model.saveModeOff();
                return View("Error");
            }
            model.setFileName(filename);
            ViewBag.beginLon = position.Item1;
            ViewBag.beginLat = position.Item2;
            ViewBag.refreshRate = 1000 * perSecond;
            ViewBag.timeout = (1000 * numSeconds) + 100; //give an extra tenth of a second
            return View("SaveToFileView");
        }
        //return XML representation of current position and save current flight details
        [HttpPost]
        public string writeSavedDetails()
        {
            WebSimulatorModel model = WebSimulatorModel.getInstance();
            if (!model.writeToFile())
            {
                return ToXML("200,200"); //failure
            }
            return ToXML("300,300"); //success
        }
    }
}