﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

namespace BackupMonitor
{
    public class CommandLineOptions
    {
        [Option('n', "name", Required = false)]
        public string Name
        {
            get;
            set;
        }

        [Option('p', "password", Required = false)]
        public string Password
        {
            get;
            set;
        }


        [Option("interval", Required = false, HelpText = "Optionally specify an interval value in seconds to shedule reoccuring backup creations.")]
        public int Interval
        {
            get;
            set;
        }

        [Option("with-date", Required = false)]
        public bool WithDate
        {
            get;
            set;
        }

        [Option("with-time", Required = false)]
        public bool WithTime
        {
            get;
            set;
        }



        [Option("mqtt-host", Required = false)]
        public string MqttHost
        {
            get;
            set;
        }

        [Option("mqtt-port", Default = 1883, Required = false)]
        public ushort MqttPort
        {
            get;
            set;
        }

        [Option("mqtt-hostid", Required = false, HelpText = "A domain name like value to identify this instance in MQTT messages. (monitor/+/backup/#)")]
        public string Hostname
        {
            get;
            set;
        }

        [Option("mqtt-id", Required = false)]
        public string MqttClientId
        {
            get;
            set;
        }

        [Option("mqtt-user", Required = false)]
        public string MqttUsername
        {
            get;
            set;
        }

        [Option("mqtt-pass", Required = false)]
        public string MqttPassword
        {
            get;
            set;
        }
    }
}
