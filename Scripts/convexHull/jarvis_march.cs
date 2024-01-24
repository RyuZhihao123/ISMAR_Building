/*
	@ masphei
	email : masphei@gmail.com
*/
// --------------------------------------------------------------------------
// 2016-05-11 <oss.devel@searchathing.com> : created csprj and splitted Main into a separate file
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using UnityEngine;

namespace ConvexHull
{

    public class JarvisMatch
    {
        const int TURN_LEFT = 1;
        const int TURN_RIGHT = -1;
        const int TURN_NONE = 0;
        public int turn(Point p, Point q, Point r)
        {
            return ((q.getX() - p.getX()) * (r.getY() - p.getY()) - (r.getX() - p.getX()) * (q.getY() - p.getY())).CompareTo(0);
        }

        public float dist(Point p, Point q)
        {
            float dx = q.getX() - p.getX();
            float dy = q.getY() - p.getY();
            return dx * dx + dy * dy;
        }

        public Point nextHullPoint(List<Point> points, Point p)
        {
            Point q = p;
            int t;
            foreach (Point r in points)
            {
                t = turn(p, q, r);
                if (t == TURN_RIGHT || t == TURN_NONE && dist(p, r) > dist(p, q))
                    q = r;
            }
            return q;
        }

        public double getAngle(Point p1, Point p2)
        {
            float xDiff = p2.getX() - p1.getX();
            float yDiff = p2.getY() - p1.getY();
            return Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
        }

        public List<Vector3> convexHull(List<Vector3> points)
        {
            List<Point> values = new List<Point>();
            points.ForEach(p =>{
                values.Add(new Point(p.x, p.z));
            });


            List<Vector2> results2D = convexHull(values);
            List<Vector3> results3D = new List<Vector3>();
            results2D.ForEach(p =>
            {

                results3D.Add(new Vector3(p.x, points[0].y, p.y));
            });

            return results3D;
        }

        public List<Vector2> convexHull(List<Point> points)
        {
            //Console.WriteLine("# List of Point #");
            //foreach (Point value in points)
            //{
            //    Console.Write("(" + value.getX() + "," + value.getY() + ") ");
            //}
            //Console.WriteLine();
            //Console.WriteLine();
            List<Point> hull = new List<Point>();
            foreach (Point p in points)
            {
                if (hull.Count == 0)
                    hull.Add(p);
                else
                {
                    if (hull[0].getX() > p.getX())
                        hull[0] = p;
                    else if (hull[0].getX() == p.getX())
                        if (hull[0].getY() > p.getY())
                            hull[0] = p;
                }
            }
            Point q;
            int counter = 0;
            //Console.WriteLine("The lowest point is (" + hull[0].getX() + ", " + hull[0].getY() + ")");
            while (counter < hull.Count)
            {
                q = nextHullPoint(points, hull[counter]);
                if (q != hull[0])
                {
                    //Console.WriteLine("Next Point is (" + q.getX() + "," + q.getY() + ") compared to Point (" + hull[hull.Count - 1].getX() + "," + hull[hull.Count - 1].getY() + ") : " + getAngle(hull[hull.Count - 1], q) + " degrees");
                    hull.Add(q);
                }
                counter++;
            }
            //Console.WriteLine();
            //Console.WriteLine("# Convex Hull #");
            //foreach (Point value in hull)
            //{
            //    Console.Write("(" + value.getX() + "," + value.getY() + ") ");
            //}
            //Console.WriteLine();

            List<Vector2> results = new List<Vector2>();
            foreach (Point value in hull)
            {
                results.Add(new Vector3(value.getX(), value.getY()));
            }

            return results;
        }

    }

}