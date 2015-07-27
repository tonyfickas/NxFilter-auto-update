using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace nxFilter_auto_update
{
    class Download
    {
        #region GetFile
        public static void GetFile()
        {
            #region delete extract location
            try
            {
                //see if file already exists (Extract Location)
                if (Directory.Exists(Program.filePath))
                {
                    //delete it if it does (Extract Location)
                    Directory.Delete(Program.filePath, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            #endregion

            //download compressed file
            WebClient webClient = new WebClient();
            
            System.IO.Directory.CreateDirectory(Program.filePath);
            webClient.DownloadFile(Program.url, Program.filePath + Program.fileName);
        }
        #endregion

        #region MD5Hash
        public static string MD5Hash()
        {
            try
            {
                //get hash of file
                using (var md5 = MD5.Create())
                {
                    return BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(Program.filePath + Program.fileName))).Replace("-", "");
                }
            }
            catch (Exception)
            {
                return "error";
            }
        }
        #endregion

        #region GetFileHash
        public static string GetFileHash()
        {
            string returnString = "";
            //get has from website
            using (WebClient client = new WebClient())
            {
                returnString = client.DownloadString(Program.urlMD5);
            }

            //return hash (string)
            return returnString.Substring(0, Math.Min(returnString.Length, 32));
        }
        #endregion
    }
}
