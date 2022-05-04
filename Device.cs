﻿using System.IO.Ports;

namespace bellatrix
{
    internal class Device
    {
        public string PortName { get; set; }
        public SerialPort PortConnection { get; set; }

        public string? IMEI { get; set; }
        public string? SerialNo { get; set; }
        public string? ModelNo { get; set; }
        public string? Carrier { get; set; }

        internal Device(string portname)
        {
            PortName = portname;
            PortConnection = new(portname);
        }
    }
}