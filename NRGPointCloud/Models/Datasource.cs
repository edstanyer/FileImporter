using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NRG.Models
{
    public abstract class Datasource
    {
        #region Properties

        public string Name { get; set; }
        public string FilePath { get; set; }
        public bool SaveRequired { get; set; }
        public bool IsEmpty { get; }
        public string FileSize { get { return GetFileSizeString(); } }
        

        #endregion

        #region Setup

        public Datasource()
        {
            
        }

        #endregion

        #region Methods

        private string GetFileSizeString()
        {
            if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath)) 
            {
                long fileSize = new FileInfo(FilePath).Length;
                return NRG.Services.FileIO.SizeSuffix(fileSize);

            }
            else
            {
                return "?";
            }
        }
        public bool IsTheSameAs(Datasource datasource)
        {
            if (FilePath != null && Name != null && FilePath == datasource.FilePath && Name == datasource.Name)
                return true;

           

            return false;

            
        }

        #endregion
    }
}
