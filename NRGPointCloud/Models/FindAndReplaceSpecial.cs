using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRG.Models
{    public enum FindAndReplaceAction
    { 
        Ignore,
        ReplaceWith,
        Remove
    }

    public class FindAndReplaceSpecial
    {

        public List<FindAndReplace> Finds = new List<FindAndReplace>(); 
    }

    public class FindAndReplace
    { 
        public string ID { get; set; } //just an identifyer - trivial
        public string FindString { get; set; }
        public string ReplaceString { get; set; }

        public FindAndReplaceAction Action { get; set; }

        public string DoAction(string inputString)
        {
            List<string> modifiedStrings = inputString.Split().ToList();
            
            if (string.IsNullOrWhiteSpace(inputString) == false && inputString.Length > 0)
            {
                if (inputString.ToUpper().Contains(FindString.ToUpper()))
                {
                    if (Action == FindAndReplaceAction.ReplaceWith)
                    { 
                        return inputString.Replace(FindString, ReplaceString);
                    }
                    else if (Action == FindAndReplaceAction.Remove)
                    {
                        return inputString.Replace(FindString, "");
                    }
                }
            }

            return inputString;
        }

        private string RemoveCommands(string inputString)
        {
            if (!string.IsNullOrWhiteSpace(inputString) && inputString.Contains( "/"))
            {
                int pos = inputString.IndexOf('/')-1;
                if (pos > 0)
                { 
                    string  switches = inputString.Substring( pos);

                }
            }

            return inputString;
        }

    }

}
