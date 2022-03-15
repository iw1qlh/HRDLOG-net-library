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
            var protocol = new HrdProtocol("TEST", "0000000000", "TestApp");

            // Check is HRDLOG.net is reachable
            var isReachable = await protocol.IsHostReachableAsync();
            Console.WriteLine($"IsHostReachable: {isReachable}");
            Assert.IsTrue(isReachable);

            if (isReachable)
            {
                try
                {
                    // Send QSO to HRDLOG.net
                    string adif = "<call:4>TEST <qso_date:8:d>20090113 <time_on:6>210237 <band:3>20m <mode:2>CW <rst_sent:3>599 <rst_rcvd:3>599 <station_callsign:11>IW1QLH/TEST <EOR>";
                    var response1 = await protocol.SendQsoAsync(HrdProtocol.Cmd.Insert, adif);
                    Console.WriteLine($"SendQso: {response1.Status}");
                    Assert.IsTrue((response1.Status == HrdProtocol.Status.Ok) || (response1.Status == HrdProtocol.Status.Dupe));

                    // Send ON-AIR status to HRDLOG.net
                    var response2 = await protocol.SendOnAirAsync(7100000, "USB", "TESTING");
                    Console.WriteLine($"OnAir: {response2}");
                    Assert.IsTrue(response2);
                }
                catch
                {
                    throw;
                }
            }

        }
    }
}