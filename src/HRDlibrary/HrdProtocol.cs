using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace HRDLibrary
{
    public class HrdProtocol
    {
        public enum Cmd { Insert, Delete, Update };
        public enum Response { Ok, Dupe, Error, UnknownUser };

        const string HOST = "robot.hrdlog.net";

        private readonly string callsign;
        private readonly string uploadCode;

        public HrdProtocol(string Callsign, string UploadCode)
        {
            callsign = Callsign;
            uploadCode = UploadCode;
        }

        public async Task<Response> SendQsoAsync(Cmd Command, string Adif, string QsoKey = null)
        {
            Response result = Response.Error;

            string reply = "insert";

            using (HttpClient wc = new HttpClient())
            {
                wc.Timeout = new TimeSpan(0, 0, 5);

                List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
                data.Add(new KeyValuePair<string, string>("Callsign", callsign));
                data.Add(new KeyValuePair<string, string>("Code", uploadCode));
                data.Add(new KeyValuePair<string, string>("App", "WinLog365"));

                switch (Command)
                {
                    case Cmd.Insert:
                        if (string.IsNullOrEmpty(Adif))
                            throw new ArgumentNullException("Adif");
                        data.Add(new KeyValuePair<string, string>("ADIFData", Adif));
                        reply = "insert";
                        break;
                    case Cmd.Delete:
                        if (string.IsNullOrEmpty(QsoKey))
                            throw new ArgumentNullException("QsoKey");
                        data.Add(new KeyValuePair<string, string>("Cmd", "DELETE"));
                        data.Add(new KeyValuePair<string, string>("ADIFKey", QsoKey));
                        reply = "delete";
                        break;
                    case Cmd.Update:
                        if (string.IsNullOrEmpty(Adif))
                            throw new ArgumentNullException("Adif");
                        if (string.IsNullOrEmpty(QsoKey))
                            throw new ArgumentNullException("QsoKey");
                        data.Add(new KeyValuePair<string, string>("Cmd", "UPDATE"));
                        data.Add(new KeyValuePair<string, string>("ADIFKey", QsoKey));
                        data.Add(new KeyValuePair<string, string>("ADIFData", Adif));
                        reply = "update";
                        break;
                }

                FormUrlEncodedContent content = new FormUrlEncodedContent(data);
                HttpResponseMessage response = await wc.PostAsync(new Uri($"http://{HOST}/NewEntry.aspx"), content);
                if (!response.IsSuccessStatusCode)
                    throw new Exception("Server error");
                using (XmlReader reader = XmlReader.Create(await response.Content.ReadAsStreamAsync()))
                {
                    XDocument doc = XDocument.Load(reader);
                    XNamespace ns = "http://xml.hrdlog.com";
                    XElement entry = doc.Root.Element(ns + "NewEntry");

                    if (entry != null)
                    {
                        XElement insert = entry.Element(ns + reply);
                        XElement error = entry.Element(ns + "error");
                        if ((insert != null) && (error == null))
                        {
                            bool dupe = !int.TryParse(insert.Value, out int nRec) || (nRec == 0);
                            if (dupe)
                            {
                                result = Response.Dupe;
                            }
                            else
                            {
                                result = Response.Ok;
                            }
                        }
                        else if (error != null)
                        {
                            if (error.Value != "Unknown user")
                                result = Response.Error;
                            else
                                result = Response.UnknownUser;
                        }
                    }
                }

            }

            return result;

        }

        public async Task<bool> SendOnAirAsync(long Frequency, string Mode, string Rig)
        // Azimuth (optional)
        // Lat (optional): Latitude
        // Long (optional): Longitude
        // Status (optional): a message who will be written in HRDLOG.net - Public chat
        // Station (optional)
        {
            bool result = false;

            using (HttpClient wc = new HttpClient())
            {
                wc.Timeout = new TimeSpan(0, 0, 5);

                List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
                data.Add(new KeyValuePair<string, string>("Frequency", Frequency.ToString()));
                data.Add(new KeyValuePair<string, string>("Mode", Mode));
                data.Add(new KeyValuePair<string, string>("Radio", Rig));
                data.Add(new KeyValuePair<string, string>("Callsign", callsign));
                data.Add(new KeyValuePair<string, string>("Code", uploadCode));
                data.Add(new KeyValuePair<string, string>("App", "WinLog365"));

                FormUrlEncodedContent content = new FormUrlEncodedContent(data);
                HttpResponseMessage response = await wc.PostAsync(new Uri($"http://{HOST}/OnAir.aspx"), content);
                if (!response.IsSuccessStatusCode)
                    throw new Exception("Server error");
                using (XmlReader reader = XmlReader.Create(await response.Content.ReadAsStreamAsync()))
                {
                    XDocument doc = XDocument.Load(reader);
                    XNamespace ns = "http://xml.hrdlog.com";
                    XElement entry = doc.Root.Element(ns + "OnAir");

                    if (entry != null)
                    {
                        XElement insert = entry.Element(ns + "insert");
                        XElement error = entry.Element(ns + "error");
                        if ((insert != null) && (error == null))
                        {
                            result = true;
                        }
                    }
                }

            }

            return result;

        }




    }
}