using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CommandLine;
using Ionic.Zip;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;


namespace BackupMonitor
{
    internal class Program
    {
        private static CommandLineOptions _options;
        private static List<string> _sources;
        private static MqttClient _client;
        private static string _basePath;
        private static bool _isMqttEnabled;


        static void Main(string[] args)
        {
            var s = 4 * 4;

            //x2 = factor 4
            //x3 = 
            //x4

            Parser.Default.ParseArguments<CommandLineOptions>(args)
                          .WithParsed<CommandLineOptions>(o =>
                          {
                              _options = o;
                          });

            if (_options == null)
            {
                // exit silently, the parser prints a console message on its own.
                return;
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _basePath = "./backup";
            }
            else
            {
                _basePath = "/backup";

                if (!Directory.Exists(_basePath))
                {
                    Log("ERROR: cannot find /backup directory");
                    return;
                }

                if (!Directory.Exists("/output"))
                {
                    Log("ERROR: cannot find /output directory");
                    return;
                }
            }

            _sources = new List<string>();

            if (!string.IsNullOrWhiteSpace(_options.MqttHost))
            {
                _isMqttEnabled = true;
                ConnectMqtt();
            }

            while (true)
            {
                ConnectMqtt();
                LoadBackupSources();
                WriteBackupFile();

                // todo: check to upload

                _sources.Clear();

                if (_options.Interval > 0)
                { // auto-re-run
                    System.Threading.Thread.Sleep(_options.Interval * 1000);
                }
                else
                {
                    break;
                }
            }
        }


        static void ConnectMqtt()
        {
            if (!_isMqttEnabled)
            {
                return;
            }

            if (_client == null)
            {
                _client = new MqttClient(_options.MqttHost, _options.MqttPort, false, null, null, MqttSslProtocols.None);
            }

            if (!_client.IsConnected)
            {
                if (string.IsNullOrWhiteSpace(_options.MqttClientId))
                {
                    _options.MqttClientId = new Guid().ToString();
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(_options.MqttUsername))
                    {
                        _client.Connect(_options.MqttClientId, _options.MqttUsername, _options.MqttPassword);
                    }
                    else
                    {
                        _client.Connect(_options.MqttClientId);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Disabled MQTT due exception: {ex.Message}");

                    _isMqttEnabled = false;
                }
            }
        }
        static void LoadBackupSources()
        {
            if (!Directory.Exists(_basePath))
            {
                throw new DirectoryNotFoundException($"cannot find '{_basePath}' directory");
            }


            // gets top-level directories
            foreach (var dir in Directory.GetDirectories(_basePath))
            {
                _sources.Add(dir);
            }

            // gets top-level files
            foreach (var file in Directory.GetFiles(_basePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                _sources.Add(file);
            }

#if DEBUG
            Log($"found {_sources.Count} source(s)");
#endif
        }

        static void WriteBackupFile()
        {
            using (ZipFile zip = new ZipFile())
            {
                FileInfo backupInfo;
                FileInfo info;
                string name;
                string path;

                if (string.IsNullOrWhiteSpace(_options.Name))
                { // auto generate name
                    name = $"backup-{DateTime.Now.Ticks}";
                }
                else
                {
                    name = _options.Name;
                }

                if (_options.WithDate)
                {
                    name = $"{name}-{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}";
                }

                if (_options.WithTime)
                {
                    name = $"{name}-{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}";
                }

                if (_isMqttEnabled)
                {
                    _client.Publish($"monitor/{_options.Hostname}/backup/status", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new BackupSourceInfo(_sources.ToArray(),
                                                                                                                                                          $"{name}.zip",
                                                                                                                                                          !string.IsNullOrWhiteSpace(_options.Password),
                                                                                                                                                          "creating"))));
                }

                if (!string.IsNullOrWhiteSpace(_options.Password))
                {
                    zip.Password = _options.Password;
                }

                backupInfo = new FileInfo(_basePath);

                foreach(var file in _sources)
                {
                    info = new FileInfo(file);

                    if (!info.Exists)
                    {
                        if (!Directory.Exists(info.FullName))
                        { // maybe a dir ..
                            continue;
                        }
                    }

                    path = info.FullName.Replace(backupInfo.FullName, string.Empty);

                    if (Directory.Exists(info.FullName))
                    {
                        zip.AddDirectory(file, path);
                    }
                    else
                    {
                        zip.AddFile(file, path.Replace(info.Name, string.Empty));
                    }
                }




                zip.Save($"/output/{name}.zip");


                if (_isMqttEnabled)
                {
                    _client.Publish($"monitor/{_options.Hostname}/backup/status", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new BackupSourceInfo(_sources.ToArray(),
                                                                                                                                                          $"{name}.zip",
                                                                                                                                                          !string.IsNullOrWhiteSpace(_options.Password),
                                                                                                                                                          "saved",
                                                                                                                                                          new FileInfo($"/output/{name}.zip").Length))));
                }

            }
        }

        static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

    public struct BackupSourceInfo
    {
        public DateTime Date;
        public string Name;
        public bool IsEncrypted;
        public string Status;
        public string[] Paths;
        public long? SizeInBytes;

        public BackupSourceInfo(string[] paths, string name, bool isEncrypted, string status, long? sizeInBytes = 0)
        {
            this.Date = DateTime.Now;
            this.Paths = paths;
            this.Name = name;
            this.IsEncrypted = isEncrypted;
            this.Status = status;
            this.SizeInBytes = sizeInBytes;
        }
    }
}
