// ArchiveHostOptions Copyright (c) 2023 Arosy
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

namespace BackupMonitor
{
    public class ArchiveHostOptions
    {
        /// <summary>
        /// The address:port combo for the remote host.
        /// </summary>
        public string Endpoint
        {
            get;
            private set;
        }

        /// <summary>
        /// The username required for authentification at the remote host.
        /// </summary>
        public string Username
        {
            get;
            private set;
        }

        /// <summary>
        /// Optionally a password which will be used when authentificating at the remote host.
        /// </summary>
        public string Password
        {
            get;
            private set;
        }

        /// <summary>
        /// The relative path on the archive host used to put the backup files to the correct
        /// directory.
        /// </summary>
        public string Path
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint">The endpoint which is an address:port combo for the remote host.</param>
        /// <param name="username">The username required for authentification.</param>
        /// <param name="password">Optionally provide a password.</param>
        /// <param name="path">The base path on the remote host.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ArchiveHostOptions(string endpoint, string username, string password = "", string path = "/")
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException("cannot initialize with NULL endpoint.");
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException("cannot initialize with NULL username.");
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("cannot initialize with NULL path.");
            }

            this.Endpoint = endpoint;
            this.Username = username;
            this.Password = password;
            this.Path = path;
        }
    }
}
