using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRG.Services
{

    public static class StringHandling
    {
        public static string QUOTE ()
        {
            return "\"";
        }

        public static string WrapWithQuotes(string input)
        { 
            return QUOTE() + input + QUOTE();
        }

        /// <summary>
        /// Added by ES:11.01.21 - gets a string representation of a number to the correct number of decimals
        /// </summary>
        /// <param name="NumberString"></param>
        /// <param name="DecimalPlaces"></param>
        /// <returns>Number string in correct format</returns>
        public static string DecimalPrecisionOfString(string NumberString, int DecimalPlaces = 0)
        {

            double workingVal = 0;
            if (NumberString == "" || NumberString == null)
            {
                return ""; //shit in, shit out 
            }
            else if (double.TryParse(NumberString, out workingVal))
            {
                string precString = "0";
                if (DecimalPlaces > 0)
                {
                    string format = "";
                    format = format.PadRight(DecimalPlaces, '0');
                    return workingVal.ToString(precString + "." + format); //this should now be correct
                }
                else
                {
                    return workingVal.ToString("0"); //no decimals

                }

            }
            else
            {
                return NumberString;  //input is not numeric, so pass back input      
            }


        }

    }
}
