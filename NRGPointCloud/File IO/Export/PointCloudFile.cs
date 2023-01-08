using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using System.Runtime.Serialization.Formatters.Binary;

namespace NRG.Export
{
    [Serializable]
    public class PointCloudFile
    {
        #region Properties

        public string OctreePath;
        public Bounds BoundingBox;
        public Bounds TightBoundingBox;
        public OutPutFormat OutputFormat;
        public double Spacing;
        public double Scale;
        public int HierarchyStepSize = -1;
        public ulong NumAccepted = 0;
        public string Projection = "";

        #endregion

        #region Setup

        public PointCloudFile()
        {

        }

        #endregion
    }
}
