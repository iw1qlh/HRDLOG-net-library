using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace HRDLibrary.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var protocol = new HrdProtocol("TEST", "0000000000");

            var response1 = await protocol.SendQsoAsync(HrdProtocol.Cmd.Insert, "<call:4>TEST <qso_date:8:d>20090113 <time_on:6>210237 <band:3>20m <mode:2>CW <rst_sent:3>599 <rst_rcvd:3>599 <station_callsign:11>IW1QLH/TEST <EOR>");
            Console.WriteLine($"SendQso: {response1}");
            Assert.IsTrue((response1 == HrdProtocol.Response.Ok) || (response1 == HrdProtocol.Response.Dupe));

            var response2 = await protocol.SendOnAirAsync(7100000, "USB", "TESTING");
            Console.WriteLine($"OnAir: {response2}");
            Assert.IsTrue(response2);

        }
    }
}