using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using BackupMonitor.Templates;
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
        private static bool _autorun;

        private static bool _isUploadEnabled;
        private static bool _isMqttEnabled;

        private static int _lastDayRun;

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

            if (!string.IsNullOrWhiteSpace(_options.ArchiveType) && !_options.ArchiveType.ToLower().Equals("none"))
            {
                _isUploadEnabled = true;
                Log("upload feature is enabled!");
            }

            if (_options.Interval > 0)
            {
                _autorun = true;
                Log("autorun feature is enabled! this instance will stay open until its closed manually.");
            }


            while (true)
            {
                if (!string.IsNullOrWhiteSpace(_options.RunAtTime))
                {
                    var targetTime = DateTime.Parse(_options.RunAtTime);
                    var currentTime = DateTime.Now;
                    var deltaTime = default(TimeSpan);


                    Log($"waiting for the specified run at time: {targetTime.Hour}:{targetTime.Minute}:{targetTime.Second}");

                    while (true)
                    {
                        currentTime = DateTime.Now;
                        deltaTime = targetTime.Subtract(currentTime);

                        if (deltaTime.TotalSeconds <= 0 && _lastDayRun != currentTime.Day)
                        {
                            _lastDayRun = currentTime.Day;
                            break;
                        }

                        // sleep for a moment
                        System.Threading.Thread.Sleep(1000);
                    }

                    _autorun = true;
                    _options.Interval = 1;
                }

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

            // ensure that we get out of child-threads, etc.
            Environment.Exit(0);
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

            if (string.IsNullOrWhiteSpace(_options.MqttHost))
            {
                LogError("cannot connect to MQTT broker with no host specified.");
                _isMqttEnabled = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.Hostname))
            {
                LogError("cannot publish MQTT messages with no hostname specified to identify this instance.");
                _isMqttEnabled = false;
                return;
            }

            if (_options.MqttPort <= 0)
            {
                LogError("cannot connect to MQTT broker with no port specified.");
                _isMqttEnabled = false;
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
                    _options.MqttClientId = Guid.NewGuid().ToString();
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
            // todo: --exclude=*.sock;*.tmp
            foreach (var file in Directory.GetFiles(_backupInfo.FullName, "*.*", SearchOption.AllDirectories))
            {
                _sources.Add(file);
            }

            Log($"found {_sources.Count} sources to process.");
        }
        static void WriteBackupFile()
        {
            var time = DateTime.Now;


            using (ZipFile zip = new ZipFile())
            {
                BackupFileInfo backupInfo;
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



                backupInfo = new BackupFileInfo($"{name}.zip",
                                                _sources.ToArray(),
                                                !string.IsNullOrWhiteSpace(_options.Password),
                                                _isUploadEnabled,
                                                _autorun);

                if (_isMqttEnabled)
                {
                    backupInfo.Status = BackupStatus.FileCreating;
                    PublishMQTT($"monitor/{_options.Hostname}/backup/status", backupInfo);
                }

                foreach (var item in _sources)
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
                        try
                        {
                            File.OpenRead(item).Close();
                        }
                        catch
                        {
                            LogWarn($"cannot open file: '{item}' for copy, going to skip this item ..");
                            continue;
                        }

                        zip.AddFile(item, path.Replace(info.Name, string.Empty));
                        Log($"added file: {item}");
                    }
                }

                Log("saving backup file, hang tight ..");
                zip.Save($"/output/{name}.zip");
                Log($"saved backup file: '{name}.zip'");
                
                if (_isMqttEnabled)
                {
                    backupInfo.Status = BackupStatus.FileCreated;
                    backupInfo.SizeInBytes = new FileInfo($"/output/{name}.zip").Length;

                    PublishMQTT($"monitor/{_options.Hostname}/backup/status", backupInfo);
                }

                // push upload
                if (_isUploadEnabled)
                {
                    try
                    {
                        UploadBackupFile(name, backupInfo);
                    }
                    catch (Exception ex)
                    {
                        LogError($"an error occured while uploading backup file: '{ex.Message}'");
                    }
                }
            }
        }
        static void UploadBackupFile(string name, BackupFileInfo backupInfo)
        {
            IArchiveConnector connector;
            TransferStatus status;


            if (!_isUploadEnabled)
            {
                LogWarn("upload to archive is not configured, skipping connectiong attempt.");
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("cannot upload backup with name NULL/Empty.");
            }

            if (string.IsNullOrWhiteSpace(_options.ArchiveType))
            {
                _isUploadEnabled = false;
                LogWarn("there is no archive type specified, disabled upload feature for this instance.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.ArchiveUsername))
            {
                _isUploadEnabled = false;
                LogWarn("there is no username for the archive specified, disabled upload feature for this instance.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.ArchivePath))
            {
                _options.ArchivePath = "~/";
            }

            // PUSH MQTT
            if (_isMqttEnabled)
            {
                backupInfo.Status = BackupStatus.FileUploading;
                PublishMQTT($"monitor/{_options.Hostname}/backup/status", backupInfo);
            }

            switch (_options.ArchiveType.ToLower())
            {
                case "scp":
                    connector = new ArchiveScpConnector();
                    /*
                    if (string.IsNullOrWhiteSpace(_options.ArchivePassword))
                    {
                        _isUploadEnabled = false;
                        LogError("cannot upload with scp when no password is specified.");
                        return;
                    }



                    Log(Bash($"sshpass -p {_options.ArchivePassword} scp -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -p23 /output/{name}.zip {_options.ArchiveUsername}@{_options.ArchiveEndpoint}:{_options.ArchivePath}"));*/
                    break;

                default:
                    throw new NotSupportedException($"'{_options.ArchiveType.ToLower()}' is not a valid archive type.");
            }

            // no matter the connector type, the initialization is the same for all
            connector.Initialize(new ArchiveHostOptions(_options.ArchiveEndpoint,
                                                        _options.ArchiveUsername,
                                                        _options.ArchivePassword,
                                                        _options.ArchivePath));

            Log("trying to upload backup file");

            // hope for the best and try to upload the file
            status = connector.Transfer($"/output/{name}.zip");

            // PUSH MQTT
            if (_isMqttEnabled)
            {
                if (status == TransferStatus.Success)
                {
                    backupInfo.Status = BackupStatus.FileUploaded;
                    backupInfo.Error = string.Empty;
                }
                else
                {
                    backupInfo.Status = BackupStatus.Error;
                    backupInfo.Error = connector.LastError;
                }

                PublishMQTT($"monitor/{_options.Hostname}/backup/status", backupInfo);
            }

            if (status == TransferStatus.Success)
            {
                Log("the file upload was successful.");
            }
            else
            {
                LogError(connector.LastError);

                if (!_autorun)
                { // indicate error exit when not looping
                  // the error could be temp. only ..
                    Environment.Exit(1);
                }
            }
        }




        static void PublishMQTT(string topic, object data)
        {
            string jsonData;


            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentNullException("cannot publish to NULL topic.");
            }

            if (data == null)
            {
                throw new ArgumentNullException("cannot publish NULL data.");
            }

            if (!_isMqttEnabled)
            {
                return;
            }

            try
            {
                jsonData = JsonConvert.SerializeObject(data);
            }
            catch (Exception ex)
            {
                LogError($"cannot publish mqtt message: '{ex.Message}'");
                return;
            }

            if (string.IsNullOrWhiteSpace(jsonData))
            {
                throw new InvalidOperationException($"cannot publish empty mqtt message");
            }

            try
            {
                _client.Publish(topic, Encoding.UTF8.GetBytes(jsonData));
            }
            catch (Exception ex)
            {
                LogError($"cannot publish mqtt message: '{ex.Message}'");
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
}
