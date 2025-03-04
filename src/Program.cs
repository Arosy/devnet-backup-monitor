using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using BackupMonitor.Templates;
using BackupMonitor.Templates.Backups;
using CommandLine;
using Ionic.Zip;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


namespace BackupMonitor
{
    internal class Program
    {
        // by default it references the templates dir in repository root directory.
        private static string TEMPLATES_DIR = "../../../../templates";


        static void Main(string[] args)
        {
            var deserializer = new DeserializerBuilder().Build();
            var template = default(DefaultBackupConfiguration);
            var bkpMgrs = new List<BackupManager>();
            var keepRunning = false;


            if (File.Exists(".docker"))
            {
                TEMPLATES_DIR = "/templates";
            }

            foreach(var file in Directory.GetFiles(TEMPLATES_DIR, "*.yml", SearchOption.TopDirectoryOnly))
            {
                try
                {            
                    Log($"trying to parse backup template: '{file}'");
                    template = deserializer.Deserialize<DefaultBackupConfiguration>(File.ReadAllText(file));
                    Log($"successfully loaded backup template: '{template.Name}'");
                }
                catch (Exception ex)
                {
                    Log($"cannot parse template: {ex.Message}");
                    continue;
                }

                bkpMgrs.Add(new BackupManager(template));
            }

            Log($"loaded {bkpMgrs.Count} backup templates");
            while(true)
            {
                foreach(var bkp in bkpMgrs)
                {
                    if (bkp.IsRunnable())
                    {
                        try
                        {
                            Log($"processing template: '{bkp.Config.Name}'");
                            bkp.RunAndAwait();
                            Log($"successfully processed template: '{bkp.Config.Name}'");
                        }
                        catch (Exception ex)
                        {
                            Log($"an error occured while processing template: '{ex.Message}'");
                        }
                    }

                    if (bkp.Config.Schedule != null)
                    {
                        keepRunning = true;
                    }
                }

                if (!keepRunning)
                { // exit the application
                    break;
                }

                Thread.Sleep(1000);
            }

            // ensure that we get out of child-threads, etc.
            Environment.Exit(0);
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
