using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using NRG.Services;
using System.ComponentModel;

namespace NRG.Export
{
    [Serializable]
    public class PointWriter
    {
        #region Properties

        public string file;
        public int numPoints = 0;

        #endregion

        #region Setup

        public virtual void CloseWriter()
        {

        }

        public virtual void Close()
        {
            return;
        }

        #endregion

        #region Methods

        public virtual void Write(CloudPoint point)
        {
            return;
        }

        public virtual void Write(double x, double y, double z, byte r, byte g, byte b, ushort intensity)
        {
            return;
        }

        public virtual void WriteAllPoints(List<CloudPoint> points)
        {
            return;
        }

        public virtual void WriteAllPoints(List<CodedPoint> points)
        {
            return;
        }

        /// <summary>
        /// Writes a list of points to file using the color type provided
        /// </summary>
        /// <param name="points">A List of <see cref="CloudPoint"/>s containing x,y,z and r,g,b values</param>
        /// <param name="modelBounds">The bounds of the model to export. Can be null if color type is not heightmap</param>
        /// <param name="colorType">The colorType of the export, can be RGB, Heightmap, Grayscale or Intensity anything else is exported as RGB</param>
        /// <param name="intensityConverter">The intensity converter object used by the model to export. Can be null if color type is not intensity</param>
        public virtual void WriteAllPoints(List<CloudPoint> points, Bounds modelBounds, ColorType colorType, IntensityConverter intensityConverter)
        {

        }

        public virtual void WriteAllPoints(BindingList<DTMPoint> points)
        {

        }

        public virtual void WriteLinePoint(double x, double y, double z, string name)
        {
            return;
        }

        public virtual void WriteByteArray(byte[] byteArray)
        {

        }

        public virtual void WriteBounds(Bounds bounds)
        {

        }

        #endregion
    }
}
