using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupMonitor
{
    internal class FileInfoEx
    {
        private FileInfo _info;
        private string _virtualPath;


        public string VirtualPath
        {
            get
            {
                return _virtualPath;
            }
        }
        public string FullPath
        {
            get
            {
                return _info.FullName;
            }
        }


        public FileInfoEx(FileInfo info, string pathToExclude)
        {
            if (info == null)
            {
                throw new ArgumentNullException("cannot create from NULL info.");
            }

            _info = info;
            _virtualPath = info.FullName.Replace(pathToExclude, string.Empty);
        }
    }
}
