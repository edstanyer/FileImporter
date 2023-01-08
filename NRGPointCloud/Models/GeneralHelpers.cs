using NRG.Import;
using NRG.MathsHelpers;
using NRG.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRG.Models
{
    public class GeneralHelpers
    {

        
        
        /// <summary>
        /// Model import added by ES:15.06.22 - general container for anything we may wish to import
        /// Currently used for MX and XML imports.
        /// </summary>
        
        public class ModelImport
        {
            public List<DTM> Models = new List<DTM>();
            public List<Alignment> Alignments = new List<Alignment>();
        }

    }
}
