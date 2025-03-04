// Utils Copyright (c) 2023 Arosy
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupMonitor
{
    public static class Utils
    {
        public static string Bash(string cmd)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                throw new NotSupportedException("This platform is not supported.");
            }

            if (string.IsNullOrWhiteSpace(cmd))
            {
                throw new ArgumentNullException("Cannot execute NULL / Empty command.");
            }

            var escapedArgs = cmd.Replace("\"", "\\\"");
            var result = string.Empty;
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };


            try
            {
                process.Start();
                result = process.StandardOutput.ReadToEnd();

                if (string.IsNullOrWhiteSpace(result))
                {
                    result = string.Empty;
                }

                result = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch
            {

            }

            return result;
        }
    }
}
