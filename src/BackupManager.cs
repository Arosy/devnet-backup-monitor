// BackupManager Copyright (c) 2025 Arosy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BackupMonitor.Templates;
using BackupMonitor.Templates.Backups;
using Ionic.Zip;


namespace BackupMonitor
{
    public class BackupManager
    {
        private DateTime? _lastRun;
        private List<string> _sources;
        private string _logName;
        private float _filesSaved;
        private long _lastLogTicks;
        private bool _compressing;
        private bool _working;


        public DefaultBackupConfiguration Config { get; private set; }

        public IReadOnlyList<string> Sources
        {
            get
            {
                return _sources;
            }
        }

        public BackupManager(DefaultBackupConfiguration config)
        {
            this.Config = config;
            

            _sources = new List<string>();

            if (config.Upload != null)
            {
                if (config.Upload.Host.StartsWith("$") && config.Upload.Host.EndsWith("$"))
                {
                    config.Upload.Host = Environment.GetEnvironmentVariable(config.Upload.Host);
                }

                if (config.Upload.Username.StartsWith("$") && config.Upload.Username.EndsWith("$"))
                {
                    config.Upload.Username = Environment.GetEnvironmentVariable(config.Upload.Username);
                }

                if (config.Upload.Password.StartsWith("$") && config.Upload.Password.EndsWith("$"))
                {
                    config.Upload.Password = Environment.GetEnvironmentVariable(config.Upload.Password);
                }

                if (config.Upload.Path.StartsWith("$") && config.Upload.Path.EndsWith("$"))
                {
                    config.Upload.Path = Environment.GetEnvironmentVariable(config.Upload.Path);
                }
            }

            if (config.Archive != null)
            {
                if (config.Archive.Password.StartsWith("$") && config.Archive.Password.EndsWith("$"))
                {
                    config.Archive.Password = Environment.GetEnvironmentVariable(config.Archive.Password);
                }
            }

            _logName = config.Name;
        }

        public bool IsTemplateValid()
        {
            if (string.IsNullOrWhiteSpace(this.Config.Name))
            {
                Log($"ERROR! template is missing 'name: <name>' value");
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.Config.Output))
            {
                Log($"ERROR! template is missing 'output: /<path>/<name>' value");
                return false;
            }

            if (this.Config.Inputs == null)
            {
                Log($"ERROR! there are no inputs defined.");
                return false;
            }

            if (this.Config.Archive != null)
            {
                if (string.IsNullOrWhiteSpace(this.Config.Archive.Password))
                {
                    Log($"ERROR! archive password is enabled, but no password is configured.");
                    return false;
                }
            }

            if (this.Config.Upload != null)
            {
                if (string.IsNullOrWhiteSpace(this.Config.Upload.Host))
                {
                    Log($"ERROR! upload is enabled, but no host is configured.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.Config.Upload.Username))
                {
                    Log($"ERROR! upload is enabled, but no user is configured.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.Config.Upload.Password))
                {
                    Log($"ERROR! upload is enabled, but no pass is configured.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.Config.Upload.Path))
                {
                    Log($"ERROR! upload is enabled, but no path is configured.");
                    return false;
                }
            }

            if (this.Config.Mqtt != null)
            {
                if (string.IsNullOrWhiteSpace(this.Config.Mqtt.Host))
                {
                    Log($"ERROR! mqtt is enabled, but no host is configured.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.Config.Mqtt.ClientID))
                {
                    Log($"ERROR! mqtt is enabled, but no ID is configured.");
                    return false;
                }

                if (this.Config.Mqtt.Port == 0)
                {
                    Log($"ERROR! mqtt is enabled, but no port is configured.");
                    return false;
                }
            }

            if (this.Config.Exclusions != null)
            {
                foreach(var input in this.Config.Inputs)
                {
                    if (this.Config.Exclusions.Contains(input))
                    {
                        Log($"ERROR! detected exclusion rule for input: '{input}'");
                        return false;
                    }
                }
            }

            return true;
        }


        public int Scan(bool validateTemplate = true)
        {
            var sourceInfo = default(FileInfo);
            

            if (_compressing)
            {
                throw new InvalidOperationException("cannot scan sources while compress is running");
            }

            if (validateTemplate && !this.IsTemplateValid())
            {
                throw new Exception("invalid template");
            }

            foreach(var input_path in this.Config.Inputs)
            {
                Log($"processing input path: '{input_path}'");
                if (File.Exists(input_path))
                { // single file
                    try
                    {
                        if (this.IsExcluded(input_path))
                        {
                            continue;
                        }

                        if (this.Config.Settings != null)
                        {
                            if (this.Config.Settings.TryRead)
                            {
                                sourceInfo = new FileInfo(input_path);
                                sourceInfo.OpenRead().Close();
                            }
                        }

                        _sources.Add(input_path);
                    }
                    catch (Exception ex)
                    {
                        Log($"an error occured when adding source: '{input_path}' with message: {ex.Message}");
                        continue;
                    }
                }
                else if (Directory.Exists(input_path))
                {              

                    foreach(var file in this.EnumerateFiles(input_path, "*.*", SearchOption.AllDirectories).Concat(Directory.GetFiles(input_path, "*.*", SearchOption.TopDirectoryOnly)))
                    {
                        try
                        {
                            if (this.IsExcluded(file))
                            {
                                continue;
                            }

                            if (this.Config.Settings != null)
                            {
                                if (this.Config.Settings.TryRead)
                                {
                                    sourceInfo = new FileInfo(file);
                                    sourceInfo.OpenRead().Close();
                                }
                            }

                            _sources.Add(file);
                        }
                        catch (Exception ex)
                        {
                            Log($"an error occured when adding: '{file}' with message: {ex.Message}");
                            continue;
                        }
                    }
                }
            }

            return _sources.Count;
        }
        public void Compress()
        {
            var path = default(string);
            var outPath = default(string);
            var outInfo = default(FileInfo);
            var useRelativePaths = false;


            if (_compressing)
            {
                throw new InvalidOperationException("cannot compress, while compression is running");
            }

            _compressing = true;

            try
            {
                using (var zip = new ZipFile())
                {
                    if (this.Config.Archive != null)
                    {
                        if (!string.IsNullOrWhiteSpace(this.Config.Archive.Password))
                        {
                            zip.Password = this.Config.Archive.Password;
                        }
                    }

                    if (this.Config.Settings != null)
                    {
                        if (!string.IsNullOrWhiteSpace(this.Config.Settings.PathMode))
                        {    
                            if (this.Config.Settings.PathMode.Equals("relative"))
                            {
                                useRelativePaths = true;
                            }
                        }
                    }


                    zip.ZipErrorAction = ZipErrorAction.InvokeErrorEvent;
                    zip.ZipError += OnZipError;
                    zip.SaveProgress += OnZipProgress;

                    foreach(var file in _sources)
                    {
                        path = file;

                        if (useRelativePaths)
                        {
                            foreach(var inp in this.Config.Inputs)
                            {
                                path = path.Replace(inp, string.Empty);
                            }
                        }

                        if (Directory.Exists(path))
                        {
                            zip.AddDirectory(path);
                        }
                        else
                        {
                            zip.AddFile(path);
                        }
                    }

                    outPath = Path.Combine(this.Config.Output, this.Config.Name);

                    if (string.IsNullOrWhiteSpace(outPath))
                    {
                        throw new InvalidOperationException("no valid output path");
                    }

                    outInfo = new FileInfo(outPath);

                    if (outInfo.Exists)
                    {
                        if (this.Config.Settings == null || !this.Config.Settings.Overwrite)
                        {
                            throw new InvalidOperationException("backup file already exists and overwrite is disabled");
                        }

                        outInfo.Delete();
                    }

                    Log("saving compressed archive, this may take a moment.");

                    zip.UseZip64WhenSaving = Zip64Option.AsNecessary;
                    zip.Save(outPath);
                }
            }
            finally
            {
                _compressing = false;
                _lastLogTicks = 0;
                _filesSaved = 0;
            }
        }
        public void Cleanup()
        {
            if (string.IsNullOrWhiteSpace(this.Config.Output))
            {
                throw new ArgumentException("item.Output is NULL");
            }

            var outInfo = new DirectoryInfo(this.Config.Output);
            if (outInfo.Exists)
            {
                foreach(var file in outInfo.GetFiles("*.tmp", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        Log($"trying to clean orphane file: '{file.FullName}'");
                        File.Delete(file.FullName);
                    }
                    catch (Exception ex)
                    {
                        Log($"cannot delete orphane file: '{ex.Message}'");
                    }
                }
            }
        }
        public bool Upload()
        {
            var connector = default(IArchiveConnector);


            if (this.Config.Upload == null)
            {
                return false;
            }

            switch(this.Config.Upload.Mode)
            {
                case "scp":
                    connector = new ArchiveScpConnector();
                    break;
            }

            connector.Initialize(this.Config.Upload);
            return connector.Transfer(Path.Combine(this.Config.Output, this.Config.Name)) == TransferStatus.Success;
        }
        public bool IsRunnable()
        {
            if (_working)
            {
                return false;
            }

            var waitForDays = new List<DayOfWeek>();
            var waitUntil = DateTime.Now;
            var time = DateTime.Now;


            // reset clock info so we start with 00:00 on given date
            waitUntil = new DateTime(time.Year, time.Month, time.Day);
            if (this.Config.Schedule != null)
            {
                if (this.Config.Schedule.Week != null)
                {
                    waitUntil = waitUntil.AddDays((double)this.Config.Schedule.Week * 7);
                }

                if (this.Config.Schedule.Hour != null)
                {
                    waitUntil = waitUntil.AddHours((double)this.Config.Schedule.Hour);
                }

                if (this.Config.Schedule.Minute != null)
                {
                    waitUntil = waitUntil.AddMinutes((double)this.Config.Schedule.Minute);
                }

                if (!string.IsNullOrWhiteSpace(this.Config.Schedule.Day))
                {
                    if (this.Config.Schedule.Day.Contains(";"))
                    {
                        foreach(var day in this.Config.Schedule.Day.Split(";"))
                        {
                            waitForDays.Add(Enum.Parse<DayOfWeek>(FirstCharToUpper(day)));
                        }
                    }
                }
            }

            if (waitUntil > time)
            { // wait..
                return false;
            }

            if (waitForDays.Count > 0)
            {
                if (!waitForDays.Contains(time.DayOfWeek))
                { // not the correct day to run..
                    return false;
                }
            }

            if (_lastRun != null)
            {
                var cmpTime = (DateTime)_lastRun;


                if (time.Year == cmpTime.Year && time.Month == cmpTime.Month && time.Day == cmpTime.Day)
                { // wait for next day
                    return false;
                }
            }

            return true;
        }
        public void RunAndAwait()
        {
            if (_working)
            {
                throw new InvalidOperationException("cannot run and wait while performing this operation already");
            }
            
            try
            {
                while(true)
                {
                    if (this.IsRunnable())
                    { // wait for given time
                        break;
                    }

                    Thread.Sleep(1000);
                }
                _working = true;

                //process
                Log($"cleaning orphane archives in path: {this.Config.Output} (remove *.tmp files)");
                this.Cleanup();
                Log($"scanning sources from: {this.Config.Inputs.Length} input path(s)");
                this.Scan();
                Log($"compressing sources to archive (password protected: {(this.Config.Archive != null && !string.IsNullOrWhiteSpace(this.Config.Archive.Password))})");
                this.Compress();

                if (this.Config.Upload != null)
                {
                    Log($"uploading local backup to configured archive: '{this.Config.Upload.Host}'");
                    if (this.Upload())
                    {
                        Log($"successfully uploaded backup file");
                    }
                    else
                    {
                        throw new InvalidOperationException($"couldn't upload backup: '{Path.Combine(this.Config.Output, this.Config.Name)}' to archive: '{this.Config.Upload.Host}'");
                    }
                }

                _lastRun = DateTime.Now;
            }
            finally
            {
                _working = false;
            }
        }

        bool IsExcluded(string filePath)
        {
            var inputInfo = default(FileInfo);
            var exclInfo = default(FileInfo);


            foreach(var exclusion in this.Config.Exclusions)
            {
                if (exclusion.Equals(filePath))
                { // when 1:1 match simply return..
                    return true;
                }

                if (filePath.Contains(exclusion))
                { // the excl. is shorter than file path:
                  // filePath: /tmp/some/dir
                  // excl: /some/dir
                    return true;
                }

                if (exclusion.Contains("*."))
                { // wildcard indicator, check file extension
                    inputInfo = new FileInfo(filePath);
                    exclInfo = new FileInfo(exclusion);
                    
                    if (inputInfo.Extension.Equals(exclInfo.Extension))
                    {
                        return true;
                    }
                }
                else if (exclusion.Contains(".*"))
                { // wildcard, check file name
                    inputInfo = new FileInfo(filePath);
                    exclInfo = new FileInfo(exclusion);
                    
                    if (inputInfo.Name.Equals(exclInfo.Name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOpt)
        {
            if (searchOpt == SearchOption.TopDirectoryOnly)
            {
                return Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
            }

            List<string> folders = new List<string>() { path };
            int folCount = 1;
            List<string> files = new List<string>() { };

            for (int i = 0; i < folCount; i++)
            {
                try
                {
                    if (this.IsExcluded(folders[i]))
                    {
                        continue;
                    }

                    foreach (var newDir in Directory.EnumerateDirectories(folders[i], "*", SearchOption.TopDirectoryOnly))
                    {
                        folders.Add(newDir);
                        folCount++;
                        try
                        {
                            if (this.IsExcluded(newDir))
                            {
                                continue;
                            }

                            foreach (var file in Directory.EnumerateFiles(newDir, searchPattern))
                            {
                                files.Add(file);
                            }
                        } catch (UnauthorizedAccessException)
                        {
                            // Failed to read a File, skipping it.
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Failed to read a Folder, skipping it.
                    continue;
                }
            }
            return files;
        }



        private void OnZipProgress(object sender, ZipProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Saving_AfterWriteEntry && e.BytesTransferred == e.TotalBytesToTransfer)
            {
                ++_filesSaved;

                if (Environment.TickCount64 >= (_lastLogTicks + 1000))
                {
                    var percent = Math.Round((double)(100 * _filesSaved) / e.EntriesTotal, 2);
                    Log($"saving compressed archive: {percent}%");

                    _lastLogTicks = Environment.TickCount64;
                }
            }
        }
        private void OnZipError(object sender, ZipErrorEventArgs e)
        {
            
            /*
            if (!string.IsNullOrWhiteSpace(e.FileName))
            {
                System.Threading.Interlocked.Increment(ref _fileStoreErrors);

                _failedFiles.Add(e.FileName);

                LogWarn($"cannot store file: '{e.FileName}'");

                // this will skip zipping this element after event handling
                e.CurrentEntry.ZipErrorAction = ZipErrorAction.Skip;
            }*/
        }

        static string FirstCharToUpper(string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input[0].ToString().ToUpper() + input.Substring(1);
            }
        }

        void Log(string message)
        {
            Console.WriteLine($"[{_logName}] {DateTime.Now} >> {message}");
        }
    }


}
