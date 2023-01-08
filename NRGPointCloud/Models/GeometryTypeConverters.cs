using System;
using System.Drawing.Design;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace NRG.Models
{
    public class LineConverter : ExpandableObjectConverter
    {
        #region Override Methods

        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
        {
            if (destinationType == typeof(Line))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType)
        {
            if (destinationType == typeof(System.String) && value is Line)
            {
                Line l = (Line)value;
                return "ID: " + l.ID + ", Code: " + l.Code;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                try
                {
                    string s = (string)value;
                    int colon = s.IndexOf(':');
                    int comma = s.IndexOf(',');

                    if (colon != -1 && comma != -1)
                    {
                        string id = s.Substring(colon + 1, (comma - colon - 1));
                        colon = s.IndexOf(':', comma + 1);
                        comma = s.IndexOf(',', comma + 1);

                        string code = s.Substring(colon + 1, (comma - colon - 1));
                        Line l = new Line(code, Convert.ToInt32(id));
                        return l;
                    }
                }
                catch
                {
                    throw new ArgumentException("Can not convert '" + (string)value + "' to type Line");
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }

    public class ZonePolygonConverter : ExpandableObjectConverter
    {
        #region Override Methods

        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
        {
            if (destinationType == typeof(ZonePolygon))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType)
        {
            if(destinationType == typeof(System.String) && value is ZonePolygon)
            {
                var p = (ZonePolygon)value;
                return "ID: " + p.ID;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if(value is string)
            {
                try
                {
                    string s = (string)value;
                    int colon = s.IndexOf(':');
                    int comma = s.IndexOf(',');

                    if(colon != -1 && comma != -1)
                    {
                        string id = s.Substring(colon + 1, (comma - colon - 1));
                        var p = new ZonePolygon(Convert.ToInt32(id));
                        return p;
                    }
                }
                catch
                {
                    throw new ArgumentException("Can not convert '" + (string)value + "' to type ZonePolygon");
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }

    public class Point2DConverter : ExpandableObjectConverter
    {
        #region Override Methods

        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
        {
            if (destinationType == typeof(Point2D))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
        {
            if (sourceType == typeof(Point2D))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType)
        {
            if(destinationType == typeof(System.String) && value is Point2D)
            {
                Point2D p = (Point2D)value;
                return "X: " + p.X + ", Y: " + p.Y;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if(value is string)
            {
                try
                {
                    string s = (string)value;
                    int colon = s.IndexOf(":");
                    int comma = s.IndexOf(",");

                    if(colon != -1 && comma != -1)
                    {
                        string x = s.Substring(colon + 1, (comma - colon - 1));
                        colon = s.IndexOf(':', comma + 1);
                        comma = s.IndexOf(',', comma + 1);

                        string y = s.Substring(colon + 1, (comma - colon - 1));
                        Point2D p = new Point2D(Convert.ToDouble(x), Convert.ToDouble(y));
                        return p;
                    }
                }
                catch
                {
                    throw new ArgumentException("Can not convert '" + (string)value + "' to type Point2D");
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }

    public class Point3DConverter : ExpandableObjectConverter
    {
        #region Override Methods

        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
        {
            if (destinationType == typeof(Point3D))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType)
        {
            if (destinationType == typeof(System.String) && value is Point3D)
            {
                Point3D p = (Point3D)value;
                return "X: " + p.X + ", Y: " + p.Y + ", Z: " + p.Z;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                try
                {
                    string s = (string)value;
                    int colon = s.IndexOf(':');
                    int comma = s.IndexOf(',');

                    if (colon != -1 && comma != -1)
                    {
                        string x = s.Substring(colon + 1, (comma - colon - 1));
                        colon = s.IndexOf(':', comma + 1);
                        comma = s.IndexOf(',', comma + 1);

                        string y = s.Substring(colon + 1, (comma - colon - 1));
                        colon = s.IndexOf(':', comma + 1);
                        comma = s.IndexOf(',', comma + 1);

                        string z = s.Substring(colon + 1, (comma - colon - 1));
                        Point3D p = new Point3D(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
                        return p;
                    }
                }
                catch
                {
                    throw new ArgumentException("Can not convert '" + (string)value + "' to type Point3D");
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }

    public class CodedPointConverter : ExpandableObjectConverter
    {
        #region Override Methods

        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
        {
            if (destinationType == typeof(CodedPoint))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType)
        {
            if(destinationType == typeof(System.String) && value is CodedPoint)
            {
                CodedPoint p = (CodedPoint)value;
                return "X: " + p.X + ", Y: " + p.Y + ", Z: " + p.Z;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if(value is string)
            {
                try
                {
                    string s = (string)value;
                    int colon = s.IndexOf(':');
                    int comma = s.IndexOf(',');

                    if(colon != -1 && comma != -1)
                    {
                        string x = s.Substring(colon + 1, (comma - colon - 1));
                        colon = s.IndexOf(':', comma + 1);
                        comma = s.IndexOf(',', comma + 1);

                        string y = s.Substring(colon + 1, (comma - colon - 1));
                        colon = s.IndexOf(':', comma + 1);
                        comma = s.IndexOf(',', comma + 1);

                        string z = s.Substring(colon + 1, (comma - colon - 1));
                        CodedPoint p = new CodedPoint(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
                        return p;
                    }
                }
                catch
                {
                    throw new ArgumentException("Can not convert '" + (string)value + "' to type CodedPoint");
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}
