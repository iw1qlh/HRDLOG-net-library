using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace HRDLibrary
{
    public class HrdProtocol
    {
        public enum Cmd { Insert, Delete, Update };
        public enum Status { Ok, Dupe, Error, UnknownUser };       

        private readonly string callsign;
        private readonly string uploadCode;
        private readonly string appName;
        private readonly string host;

        /// <summary>Inizializes a new instance of the HrdProtocol class</summary>
        /// <param name="Callsign">The user callsign</param>
        /// <param name="UploadCode">The upload code received via email after the registration</param>
        /// <param name="AppName">The application name</param>
        public HrdProtocol(string Callsign, string UploadCode, string AppName, string Host = "robot.hrdlog.net")
        {
            callsign = Callsign;
            uploadCode = UploadCode;
            appName = AppName;
            host = Host;
        }

        /// <summary>Check if HRDLOG.net is reachable</summary>
        /// <returns>True when the host is reachable</returns>
        public async Task<bool> IsHostReachableAsync()
        {

            // PING SERVER
            IPStatus status = IPStatus.Unknown;
            try
            {
                Ping ping = new Ping();                
                PingReply pr = await ping.SendPingAsync(host, 5000);
                status = pr.Status;
            }
            catch { }

            return (status == IPStatus.Success);

        }

        /// <summary>Send a QSO to HRDLOG.net</summary>
        /// <param name="Command">The command to execute</param>
        /// <param name="Adif">The QSO to add in ADIF format</param>
        /// <param name="QsoKey">The key of QSO to delete or update in ADIF format. The fields Call, QSO_Date and Time_On are mandatory</param>
        /// <returns>The server reply</returns>
        public async Task<HrdResponse> SendQsoAsync(Cmd Command, string Adif, string QsoKey = null)
        {
            HrdResponse result = new HrdResponse()
            {
                Status = Status.Error
            };

            string reply = "insert";

            using (HttpClient wc = new HttpClient())
            {
                wc.Timeout = new TimeSpan(0, 0, 5);

                List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
                data.Add(new KeyValuePair<string, string>("Callsign", callsign));
                data.Add(new KeyValuePair<string, string>("Code", uploadCode));
                data.Add(new KeyValuePair<string, string>("App", appName));

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
                HttpResponseMessage response = await wc.PostAsync(new Uri($"http://{host}/NewEntry.aspx"), content);
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
                            result.Message = insert.Value;
                            bool dupe = !int.TryParse(insert.Value, out int nRec) || (nRec == 0);
                            if (dupe)
                            {
                                result.Status = Status.Dupe;
                            }
                            else
                            {
                                result.Status = Status.Ok;
                            }
                        }
                        else if (error != null)
                        {
                            result.Message = error.Value;
                            if (error.Value != "Unknown user")
                                result.Status = Status.Error;
                            else
                                result.Status = Status.UnknownUser;
                        }
                    }
                }

            }

            return result;

        }

        /// <summary>Send the ONAIR status to HRDLOG.net</summary>
        /// <param name="Frequency">The frequency [Hz]</param>
        /// <param name="Mode">The transceiver mode</param>
        /// <param name="Rig">The transceiver name</param>
        /// <returns>True when the status was successfully sent</returns>
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
                data.Add(new KeyValuePair<string, string>("App", appName));

                FormUrlEncodedContent content = new FormUrlEncodedContent(data);
                HttpResponseMessage response = await wc.PostAsync(new Uri($"http://{host}/OnAir.aspx"), content);
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

        public class HrdResponse
        {
            public Status Status { get; set; }
            public string Message { get; set; }
        }

    }
}