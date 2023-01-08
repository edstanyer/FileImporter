using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRG.Models
{
    class NRGProject
    {
        public string ProjectNumber{ get; set; }
        public string ProjectName { get; set; }
        public string ProjectPath { get; set; }


        public bool ParseLine(string line)
        {
            int num = 0;
            int numname = 0;
            string[] arr= line.Split('=');
            arr.GetUpperBound(num);



            if (num >1)
            {
                string[] name = arr[0].Split(' ');
                name.GetUpperBound(numname);
                if (numname > 0)
                {

                }

            }
            return false;
        }

    }
}
