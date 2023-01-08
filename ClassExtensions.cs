using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

namespace FileImporter
{
    internal static class ClassExtensions
    {

        /// <summary>
        /// Added by ES:13.12.22
        /// Extends string class for file name operations
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>True is the filename isn't null or empty and exists on the system - False otherwise</returns>
        public static bool ValidFile(this string filename)
        {

            if (String.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// ToDouble added by ES:29.12.22 - extends string class to convert string values to doubles
        /// </summary>
        /// <param name="value">string to convert</param>
        /// <returns>string converted to double if valid, 0 otherwise</returns>
        public static double ToDouble(this string value)
        { 
            double d  = 0;

            if (double.TryParse(value.Trim(), out d))
            {
                return d;
            }
            else
            {
                return 0;
            }
            
        }

        /// <summary>
        /// ToInt added by ES:30.12.22 - extends string class to convert string values to integers
        /// </summary>
        /// <param name="value"></param>
        /// <returns>string converted to integer if valid, other wise zero</returns>
        public static int ToInt(this string value)
        {
            int i = 0;

            if (int.TryParse(value.Trim(), out i))
            {
                return i;
            }
            else
            {
                return 0;
            }

        }

        /// <summary>
        /// ToBool added by ES:30.12.22 - extends string class to convert to boolean
        /// </summary>
        /// <param name="value">string to convert</param>
        /// <returns><see langword="true"/>if the string contains 'true'. Trims spaces before checking</returns>
        public static bool ToBool(this string value)
        {
            return value.Trim().ToUpper().Contains("TRUE");
        }
    }
}
