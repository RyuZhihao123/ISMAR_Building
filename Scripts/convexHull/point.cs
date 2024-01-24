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

namespace ConvexHull
{

    public class Point
    {
        private float y;
        private float x;
        public Point(float _x, float _y)
        {
            x = _x;
            y = _y;
        }
        public float getX()
        {
            return x;
        }
        public float getY()
        {
            return y;
        }
    }
     
}