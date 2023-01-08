using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRG.Models
{
	public static class SearchingAndSorting
	{
        public static List<Point3D> BubbleSortSortPointsByDistance(Point3D origin, List<Point3D> model)
        {
            for (int c = 0; c < model.Count; c++)
            {
                for (int cnt = c + 1; cnt < model.Count; cnt++)
                {
                    if (model.ElementAt(cnt).X < model.ElementAt(c).X)
                    {
                        Swap(model, cnt, c);
                    }    
                }
            }
            return model;
        }


        public static List<Point3D> BubbleSortSortPointsByVector(Point3D origin, List<Point3D> model)
        {

            List<double> Distances = new List<double>();

            foreach (Point3D pt in model)
            {
                double dist = origin.DistanceTo(pt);
                Distances.Add(dist);
            }

            for (int c = 0; c < model.Count; c++)
            {
                for (int cnt = c + 1; cnt < model.Count; cnt++)
                {
                    if (Distances.ElementAt(cnt) < Distances.ElementAt(c))
                    {
                        Swap(model, cnt, c);
                        Swap(Distances, cnt, c);
                    }
                }
            }
            return model;
        }

        public static List<Point2D> BubbleSortSortPointsByVector(Point2D origin, List<Point2D> model)
        {

            List<double> Distances = new List<double>();

            foreach (Point2D pt in model)
            {
                double dist = origin.DistanceTo(pt);
                Distances.Add(dist);

            }

            for (int c = 0; c < model.Count; c++)
            {
                for (int cnt = c + 1; cnt < model.Count; cnt++)
                {
                    if (Distances.ElementAt(cnt) < Distances.ElementAt(c))
                    {
                        Swap(model, cnt, c);
                        Swap(Distances, cnt, c);
                    }
                }
            }
            return model;
        }


        public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }

        public static List<int> BubbleSortSortInts(List<int> intList)
        {
            for (int c = 0; c < intList.Count; c++)
            {
                for (int cnt = c + 1; cnt < intList.Count; cnt++)
                {
                    if (intList.ElementAt(cnt) < intList.ElementAt(c))
                    {
                        Swap(intList, cnt, c);
                    }
                }
            }
            return intList;
        }

    }
}
