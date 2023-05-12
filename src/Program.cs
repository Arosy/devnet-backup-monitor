using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using CommandLine;
using Ionic.Zip;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;


namespace BackupMonitor
{
    internal class Program
    {
        private static char[] ALLOWED_NAME_SYMBOLS = new char[] { '[', ']', '(', ')', '-', '_', '.' };
        private static CommandLineOptions _options;
        private static List<string> _sources;
        private static MqttClient _client;
        private static string _basePath;
        private static bool _isMqttEnabled;
        private static bool _autorun;

        private static DirectoryInfo _outputInfo;
        private static DirectoryInfo _backupInfo;


        static void Main(string[] args)
        {
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


            Initialize();

            if (!string.IsNullOrWhiteSpace(_options.MqttHost))
            {
                _isMqttEnabled = true;
                ConnectMqtt();
            }

            if (_options.Interval > 0)
            {
                _autorun = true;
                Log("autorun feature is enabled! this instance will stay open until its closed manually.");
            }

            while (true)
            {
                ConnectMqtt();
                LoadBackupSources();
                WriteBackupFile();

                // todo: check to upload

                _sources.Clear();

                if (_autorun)
                { // auto-re-run
                    System.Threading.Thread.Sleep(_options.Interval * 1000);
                }
                else
                {
                    break;
                }
            }
        }


        static void Initialize()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                LogError("this program is not designed to run outside a docker container !", true);
                return;
            }

            _sources = new List<string>();
            _backupInfo = new DirectoryInfo("/backup");
            _outputInfo = new DirectoryInfo("/output");

            if (!_backupInfo.Exists)
            {
                LogError("cannot find '/backup' directory, is it mounted correctly?", true);
                return;
            }

            if (!_outputInfo.Exists)
            {
                LogError("cannot find '/output' directory, is it mounted correctly?", true);
                return;
            }
        }
        static void ConnectMqtt()
        {
            if (!_isMqttEnabled)
            {
                LogWarn("MQTT is not configured, skipping connectiong attempt.");
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
                    Log($"generated random MQTT client id: '{_options.MqttClientId}' for this session.");
                }

                try
                {
                    Log($"MQTT client is trying to connect to broker: '{_options.MqttHost}:{_options.MqttPort}'");

                    if (!string.IsNullOrWhiteSpace(_options.MqttUsername))
                    {
                        Log($"connecting with username: '{_options.MqttUsername}' (Password? >> {(string.IsNullOrWhiteSpace(_options.MqttPassword) ? "NO" : "YES")}");
                        _client.Connect(_options.MqttClientId, _options.MqttUsername, _options.MqttPassword);
                    }
                    else
                    {
                        _client.Connect(_options.MqttClientId);
                    }

                    Log($"MQTT client has connected successfully.");
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
            // gets top-level directories
            foreach (var dir in Directory.GetDirectories(_backupInfo.FullName))
            {
                _sources.Add(dir);
            }

            // gets top-level files
            foreach (var file in Directory.GetFiles(_backupInfo.FullName, "*.*", SearchOption.TopDirectoryOnly))
            {
                _sources.Add(file);
            }

            Log($"found {_sources.Count} top level sources to process.");
        }

        static void WriteBackupFile()
        {
            var time = DateTime.Now;


            using (ZipFile zip = new ZipFile())
            {
                FileInfo info;
                string name;
                string path;


                if (string.IsNullOrWhiteSpace(_options.Name))
                { // auto generate name
                    name = $"backup";

                    // when default this should guarantee unique files.
                    _options.WithDate = true;
                    _options.WithTime = true;
                }
                else
                {
                    name = _options.Name;
                }

                if (_options.WithDate)
                {
                    name = $"{name}-{time.Year}-{time.Month}-{time.Day}";
                }

                if (_options.WithTime)
                {
                    name = $"{name}-{time.Hour}-{time.Minute}-{time.Second}";
                }

                if (_isMqttEnabled)
                {
                    try
                    {
                        _client.Publish($"monitor/{_options.Hostname}/backup/status", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new BackupSourceInfo(_sources.ToArray(),
                                                                                                                                                              $"{name}.zip",
                                                                                                                                                              !string.IsNullOrWhiteSpace(_options.Password),
                                                                                                                                                              "creating"))));
                    }
                    catch (Exception ex)
                    {
                        LogError($"cannot publish mqtt message: '{ex.Message}'");
                    }
                }

                if (!string.IsNullOrWhiteSpace(_options.Password))
                {
                    zip.Password = _options.Password;
                }
                else
                {
                    LogWarn($"creating unprotected backup file: '{name}' (no password was specified)");
                }

                if (_backupInfo == null)
                {
                    LogError("the backup info is NULL", true);
                    return;
                }

                _backupInfo.Refresh();
                if (!_backupInfo.Exists)
                {
                    LogError("the backup directory does not exist!", true);
                    return;
                }

                if (_sources == null)
                {
                    LogError("the sources are NULL", true);
                    return;
                }

                // sanitize the name to ensure only valid symbols are used.
                var invalidChars = new StringBuilder();
                var tmpName = new StringBuilder();
                foreach(var @char in name.ToCharArray())
                {
                    if (!char.IsLetter(@char) && !char.IsNumber(@char) && !ALLOWED_NAME_SYMBOLS.Any(x => x.Equals(@char)))
                    {
                        invalidChars.Append(@char);
                        continue;
                    }

                    tmpName.Append(@char);
                }

                if (!string.IsNullOrWhiteSpace(invalidChars.ToString()))
                {
                    name = tmpName.ToString();
                    LogWarn($"the backup name contains invalid symbols: '{invalidChars}' and was renamed to: '{tmpName}' (they will be replaced with empty chars)");
                }

                foreach(var item in _sources)
                {
                    info = new FileInfo(item);

                    if (!info.Exists)
                    {
                        if (!Directory.Exists(info.FullName))
                        { // maybe a dir ..
                            LogWarn($"cannot find directory or file: '{info.FullName}' despite it was specified as source.");
                            continue;
                        }
                    }

                    path = info.FullName.Replace(_backupInfo.FullName, string.Empty);

                    if (Directory.Exists(info.FullName))
                    {
                        zip.AddDirectory(item, path);
                        Log($"added directory: {item}");
                    }
                    else
                    {
                        zip.AddFile(item, path.Replace(info.Name, string.Empty));
                        Log($"added file: {item}");
                    }
                }

                zip.Save($"/output/{name}.zip");
                Log($"saved backup file: '{name}.zip'");
                
                if (_isMqttEnabled)
                {
                    try
                    {
                        _client.Publish($"monitor/{_options.Hostname}/backup/status", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new BackupSourceInfo(_sources.ToArray(),
                                                                                                                                                              $"{name}.zip",
                                                                                                                                                              !string.IsNullOrWhiteSpace(_options.Password),
                                                                                                                                                              "saved",
                                                                                                                                                              new FileInfo($"/output/{name}.zip").Length))));
                    }
                    catch (Exception ex)
                    {
                        LogError($"cannot publish mqtt message: '{ex.Message}'");
                    }
                }

            }
        }


        static void LogWarn(string message)
        {
            var oldColor = Console.ForegroundColor;


            Console.ForegroundColor = ConsoleColor.Yellow;
            Log($"WARN: {message}");
            Console.ForegroundColor = oldColor;
        }
        static void LogError(string message, bool isFatal = false)
        {
            var oldColor = Console.ForegroundColor;


            Console.ForegroundColor = ConsoleColor.Red;
            Log($"ERROR: {message}");
            Console.ForegroundColor = oldColor;

            if (isFatal)
            {
                Environment.Exit(1);
            }
        }
        static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now} >> {message}");
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
