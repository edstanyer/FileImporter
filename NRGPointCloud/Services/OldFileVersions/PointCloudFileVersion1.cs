using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRG.Services.OldFileVersions
{
    [Serializable]
    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }

        public Point3D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Models.Point3D ToNewPoint3D()
        {
            return new Models.Point3D()
            {
                X = X,
                Y = Y,
                Z = Z
            };
        }
    }

    [Serializable]
    public class Bounds
    {
        public Point3D Min;
        public Point3D Max;
        public Point3D Size;

        public Bounds()
        {
            Min = new Point3D();
            Max = new Point3D();
            Size = new Point3D();
        }

        public Models.Bounds ToNewBounds()
        {
            return new Models.Bounds()
            {
                Min = Min.ToNewPoint3D(),
                Max = Max.ToNewPoint3D()
            };
        }
    }

    [Serializable]
    public class PointCloudFile
    {
        #region Properties

        public string octreePath;
        public Bounds boundingBox;
        public Bounds tightBoundingBox;
        public Models.OutPutFormat outputFormat;
        public double spacing;
        public double scale;
        public int hierarchyStepSize = -1;
        public ulong numAccepted = 0;
        public string projection = "";

        #endregion

        #region Setup

        public PointCloudFile()
        {

        }

        #endregion

        #region Methods

        public Export.PointCloudFile ToNewPointCloudFile()
        {
            return new Export.PointCloudFile
            {
                OctreePath = octreePath,
                BoundingBox = boundingBox.ToNewBounds(),
                TightBoundingBox = tightBoundingBox.ToNewBounds(),
                OutputFormat = outputFormat,
                Spacing = spacing,
                Scale = scale,
                HierarchyStepSize = hierarchyStepSize,
                NumAccepted = numAccepted,
                Projection = projection
            };
        }

        #endregion
    }
}
