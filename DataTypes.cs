using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileImporter
{
    public class DataTypes
    {
        /// <summary>
        /// Polar Obs added by ES:02.01.23 - standard measurment data from total station
        /// </summary>
        public class PolarObservation
        {
            public double HorizontalAngle = 0;
            public double VerticalAngle = 0;
            public double SlopeDistance = 0;  
            public double TargetHeight = 0;    
        }


        /// <summary>
        /// RectangularCoordinate - Added by ES:02.01.23. 
        /// Distance from origin along the X and Y axis (otherwise known as orthogonal system)
        /// </summary>
        public class RectangularCoordinate
        {
            public double X  { get; set; }
            public double Y  {get; set;}

            public RectangularCoordinate()
            { 
            
            }

            public RectangularCoordinate(double x, double y)
            {
                X = x;
                Y = y;  
            }

        }

        /// <summary>
        /// TripletCoordinate - added by ES:02.01.23 - 3rd dimension to RectangularCoordinate
        /// </summary>
        public class TripletCoordinate : RectangularCoordinate
        {
            public double Z { get; set; }

            public  TripletCoordinate(double x, double y, double z)
            {
                X = x;   
                Y = y;  
                Z = z;  
            }

        }


    }
}


