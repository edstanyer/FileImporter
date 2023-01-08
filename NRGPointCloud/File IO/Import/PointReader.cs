using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using NRG.Import;

namespace NRG.Import
{
    public class PointReader
    {
        #region Setup

        public PointReader()
        {

        }

        public virtual void Close()
        {
            return;
        }

        #endregion

        #region Methods

        public virtual bool ReadNextPoint(ref double x, ref double y, ref double z, ref byte r, ref byte g, ref byte b, ref ushort intensity)
        {
            return true;
        }

        public virtual bool ReadNextPoint(ref CloudPoint point)
        {
            return true;
        }

        public virtual Bounds GetBounds()
        {
            return null;
        }

        public virtual ulong NumPoints()
        {
            return 0;
        }

        public virtual Point3D GetScale()
        {
            return new Point3D(0.001, 0.001, 0.001);
        }

        #endregion
    }
}
