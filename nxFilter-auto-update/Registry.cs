using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxFilter_auto_update
{
    public class registry
    {
        #region Read
        public static string Read(string KeyName, string openSubKey = Program.subKey)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(Program.regBase, true);
            RegistryKey sk1 = rk.OpenSubKey(openSubKey);
            if (sk1 == null)
            {
                return "";
            }
            else
            {
                try
                {
                    if((string)sk1.GetValue(KeyName.ToLower()) != null)
                        return (string)sk1.GetValue(KeyName.ToLower());

                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }
        #endregion
        
        #region Write
        public static bool Write(string KeyName, object Value, string openSubKey = Program.subKey)
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(Program.regBase, true);
                RegistryKey sk1 = rk.CreateSubKey(openSubKey);

                // Save the value
                sk1.SetValue(KeyName.ToLower(), Value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }
}
