// ArchiveScpConnector Copyright (c) 2023 Arosy
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BackupMonitor.Templates.Backups;


namespace BackupMonitor.Templates
{
    public class ArchiveScpConnector : IArchiveConnector
    {
        private string _address;
        private string _username;
        private string _password;
        private string _path;
        private ushort _port;


        public string LastError
        {
            get;
            private set;
        }

        public void Initialize(IBackupUpload options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("cannot initialize with NULL options.");
            }

            if (string.IsNullOrWhiteSpace(options.Host))
            {
                throw new ArgumentNullException("cannot initialize with NULL endpoint.");
            }

            if (string.IsNullOrWhiteSpace(options.Username))
            {
                throw new ArgumentNullException("cannot initialize with NULL username.");
            }

            if (string.IsNullOrWhiteSpace(options.Path))
            {
                throw new ArgumentNullException("cannot initialize with NULL path.");
            }

            if (options.Host.Contains(':'))
            {
                var tmp = options.Host.Split(':');


                _address = tmp[0];
                _port = ushort.Parse(tmp[1]);
            }
            else
            {
                _address = options.Host;
                _port = 23;
            }

            if (_port <= 0)
            {
                throw new InvalidOperationException("cannot assign the port correctly, was the provided endpoint ok?");
            }

            _username = options.Username;
            _password = options.Password;
            _path = options.Path;

            if (!_path.EndsWith("/"))
            {
                _path = $"{_path}/";
            }
        }

        public TransferStatus Transfer(string file)
        {
            string result;


            if (string.IsNullOrWhiteSpace(file))
            {
                this.LastError = "cannot upload NULL/Empty file.";
                return TransferStatus.Failure;
            }

            if (!File.Exists(file))
            {
                this.LastError = $"cannot upload: '{file}', because it does not exist.";
                return TransferStatus.Failure;
            }

            result = Utils.Bash($"sshpass -p {_password} scp -o StrictHostKeyChecking=no -p{_port} {file} {_username}@{_address}:{_path}");

            if (string.IsNullOrWhiteSpace(result) || (!result.Contains("Permission denied") && !result.ToLower().Contains("error")))
            {
                return TransferStatus.Success;
            }

            // if result was not empty, it should contain the error itself.
            this.LastError = result;
            return TransferStatus.Failure;
        }
    }
}
