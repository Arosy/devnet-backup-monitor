// BackupFileInfo Copyright (c) 2023 Arosy
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace BackupMonitor
{
    public class BackupFileInfo
    {
        /// <summary>
        /// Returns the date time for when this info was created.
        /// </summary>
        public DateTime Date
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the actual file name for the backup file.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the top-level sources that were used to build this backup file.
        /// </summary>
        public string[] Paths
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the current status for the backup file. If its set to <see cref="BackupStatus.Error"/> you
        /// should check the <see cref="Error"/> property for further information.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public BackupStatus Status
        {
            get;
            set;
        }

        /// <summary>
        /// If the file is already written to disk this property will return its file size.
        /// </summary>
        public long? SizeInBytes
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies wether the backup file is actually encrypted with a password or not.
        /// </summary>
        public bool IsEncrypted
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates if backup files are configured for automatic upload to remote hosts.
        /// </summary>
        public bool IsAutoTransfer
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates if backup procedures are running on loop instead of one time execution.
        /// </summary>
        public bool IsAutoRun
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the most recent error which has occured.
        /// </summary>
        public string Error
        {
            get;
            set;
        }


        public BackupFileInfo(string name, string[] paths, bool isEncrypted, bool isAutoTransfer, bool isAutoRun)
        {
            this.Date = DateTime.Now;
            this.Name = name;
            this.Paths = paths;

            this.IsEncrypted = isEncrypted;
            this.IsAutoTransfer = isAutoTransfer;
            this.IsAutoRun = isAutoRun;
        }
    }
}
