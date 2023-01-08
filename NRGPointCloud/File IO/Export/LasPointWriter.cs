using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRG.Models;
using laszip.net;

namespace NRG.Export
{
    public class LASPointWriter : PointWriter
    {
        #region Properties

        public Bounds bounds;
        public laszip_dll writer = null;
        public laszip_header header;
        public laszip_point point;
        public double[] coordinates = new double[3];

        #endregion

        #region Setup

        public LASPointWriter(string file, Bounds b, double scale)
        {
            this.file = file;
            this.bounds = b;
            numPoints = 0;

            writer = laszip_dll.laszip_create();
            writer.laszip_clean();

            writer.header = new laszip_header();
            writer.header.version_major = 1;
            writer.header.version_minor = 2;
            writer.header.header_size = 227;
            writer.header.offset_to_point_data = 227;
            writer.header.point_data_format = 2;
            writer.header.min_x = bounds.Min.X;
            writer.header.min_y = bounds.Min.Y;
            writer.header.min_z = bounds.Min.Z;
            writer.header.max_x = bounds.Max.X;
            writer.header.max_y = bounds.Max.Y;
            writer.header.max_z = bounds.Max.Z;
            writer.header.x_offset = bounds.Min.X;
            writer.header.y_offset = bounds.Min.Y;
            writer.header.z_offset = bounds.Min.Z;
            writer.header.x_scale_factor = scale;
            writer.header.y_scale_factor = scale;
            writer.header.z_scale_factor = scale;
            writer.header.point_data_record_length = 26;
            writer.header.number_of_point_records = 111;

            bool compress = file.ToUpper().EndsWith(".LAZ");

            writer.laszip_open_writer(file, compress);
        }

        public override void Close()
        {
            if (writer != null)
            {
                writer.laszip_close_writer();
                writer = null;

                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Write))
                {
                    var binWriter = new BinaryWriter(fs);
                    binWriter.Seek(107, SeekOrigin.Begin);
                    binWriter.Write(numPoints);
                    binWriter.Close();
                    binWriter = null;
                }
            }
        }

        #endregion

        #region Methods

        public override void Write(CloudPoint point)
        {
            coordinates[0] = point.X;
            coordinates[1] = point.Y;
            coordinates[2] = point.Z;
            writer.laszip_set_coordinates(coordinates);
            this.point = new laszip_point();
            this.point.rgb[0] = point.R;
            this.point.rgb[1] = point.G;
            this.point.rgb[2] = point.B;

            this.point.intensity = point.Intensity;

            writer.laszip_set_point(this.point);
            writer.laszip_write_point();

            numPoints++;
        }

        #endregion
    }
}
