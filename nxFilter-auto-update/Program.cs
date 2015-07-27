using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ServiceProcess;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;

namespace nxFilter_auto_update
{
    class Program
    {
        #region define defaults
        //web address
        public static string url = "http://www.shallalist.de/Downloads/shallalist.tar.gz";
        public static string urlMD5 = "http://www.shallalist.de/Downloads/shallalist.tar.gz.md5";

        //local path for file
        public static string filePath = @"C:\nxfilter\shallalist1\";
        public static string fileName = "shallalist.tar.gz";

        //registry address ("Current User" key is used)
        public const string regBase = @"Software";
        public const string subKey = @"nxFilterAutoUpdate";
        #endregion
        
        #region Main
        static void Main(string[] args)
        {
            #region handle arguments
            //define force update
            string forceUpdate = "";

            //check for args passed
            if(args != null)
            {
                try
                {
                    //define args
                    forceUpdate = args[0];
                }
                catch (Exception)
                {
                    //default to none
                    forceUpdate = "";
                }
            }
            #endregion

            if ((registry.Read("md5").ToUpper() != Download.GetFileHash().ToUpper()) || forceUpdate == "f")
            {
                //if not administrator return
                #if DEBUG
                #else
                if (!IsAdministrator())
                    return;
                #endif

                //remove old download and download the new one
                Download.GetFile();

                //define NxFilter Service
                ServiceController sc = new ServiceController("NxFilter");
                
                //define status of update
                bool status = false;

                //try to extract files
                status = TarExtract();
                if (!status)
                    return;

                #region stop service
                //if server is running stop it before 
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    //stop service
                    sc.Stop();
                }

                //wait untill service is stopped 
                while (sc.Status != ServiceControllerStatus.Stopped)
                {
                    System.Threading.Thread.Sleep(1000);
                    sc = new ServiceController("NxFilter");
                }
                #endregion

                //update
                Update();

                registry.Write("md5", Download.MD5Hash());

                #region start service
                //start service
                while (sc.Status != ServiceControllerStatus.Running)
                {
                    sc = new ServiceController("NxFilter");

                    switch (sc.Status)
                    {
                        case ServiceControllerStatus.ContinuePending:
                            sc.Start();
                            break;
                        case ServiceControllerStatus.Paused:
                            sc.Start();
                            break;
                        case ServiceControllerStatus.PausePending:
                            sc.Start();
                            break;
                        case ServiceControllerStatus.StartPending:
                            break;
                        case ServiceControllerStatus.Stopped:
                            sc.Start();
                            break;
                        default:
                            break;
                    }
                    
                    //sleep for one secound (don't want to go to fast)
                    System.Threading.Thread.Sleep(1000);
                }
                #endregion
                
                #region delete extract location
                try
                {
                    //see if file already exists (Extract Location)
                    if (Directory.Exists(filePath))
                    {
                        //delete it if it does (Extract Location)
                        Directory.Delete(filePath, true);
                    }
                }
                catch (Exception)
                {
                }
                #endregion
            }
        }
        #endregion

        #region TarExtract
        public static bool TarExtract()
        {
            try
            {
                //define extracting options
                Ionic.Tar.Options tar = new Ionic.Tar.Options();

                //set overwrite to true
                tar.Overwrite = true;
                tar.Path = filePath;
                
                //extract shallalist.tar.gz
                var entries = Ionic.Tar.Extract(filePath + fileName, tar);

                //return true if extracting was success
                return true;

            }
            catch (Exception)
            {
                //return false if extracting failed
                return false;
            }
        }
        #endregion

        #region Update
        static void Update()
        {
            try
            {
                //define new process
                Process cmd = new Process();
                    
                //define update_sh.bat to be launched
                cmd.StartInfo.FileName = @"C:\nxfilter\bin\update_sh.bat";

                //define arguments for update_sh.bat
                cmd.StartInfo.Arguments = "/nxfilter/shallalist1/BL";

                //launch process
                cmd.Start();
                    
                //wait for update_sh.bat to exit
                cmd.WaitForExit();

                //sleep for one secound so program computer has time to catch up
                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception)
            {

            }
        }
        #endregion

        #region IsAdministrator
        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                    .IsInRole(WindowsBuiltInRole.Administrator);
        }
        #endregion
    }
}
