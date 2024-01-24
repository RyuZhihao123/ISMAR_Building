using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelauneyTriangulation : MonoBehaviour
{
    static private float EPSILON = 0.0000000001f;

    static public Mesh GetDelauneyTri(List<Vector2> pts)
    {
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();
        List<int> newTriangles = new List<int>();




        List<Vector2> res = new List<Vector2>();
        Triangulate(pts, res);

        res.ForEach(v =>
        {
            newVertices.Add(new Vector3(v.x, v.y, 0.0f));
        });

        //int count = 0;
        for(int i = 0; i < newVertices.Count; ++i)
        {
            newTriangles.Add(i);
            newUVs.Add(new Vector2(0.0f, 0.0f));//默认纹理
        }

        

        Mesh mesh = new Mesh();

        mesh.vertices = newVertices.ToArray();
        mesh.uv = newUVs.ToArray();
        mesh.triangles = newTriangles.ToArray();

        mesh.RecalculateNormals();

        return mesh;
    }

    static private bool Triangulate(List<Vector2> contour, List<Vector2> result)
    {
        /* 分配空间 */

        int n = contour.Count;
        if (n < 3) return false;


        int[] V = new int[n];

        /* 传进的数组要求是逆时针的 */

        if (isPolygonCounterClocked(contour))
            for (int v = 0; v < n; v++) V[v] = v;
        else
            for (int v = 0; v < n; v++) V[v] = (n - 1) - v;

        int nv = n;

        /*  remove nv-2 Vertices, creating 1 triangle every time */
        int count = 2 * nv;   /* 错误检测 */

        for (int m = 0, v = nv - 1; nv > 2;)
        {
            /* if we loop, it is probably a non-simple polygon */
            if (0 >= (count--))
            {
                // Debug.log(" ERROR::probable bad polygon!")

                return false;
            }

            /* three consecutive vertices in current polygon, <u,v,w> */
            int u = v; if (nv <= u) u = 0;     /* previous */
            v = u + 1; if (nv <= v) v = 0;     /* new v    */
            int w = v + 1; if (nv <= w) w = 0;     /* next     */

            if (Snip(contour, u, v, w, nv, V))
            {
                int a, b, c, s, t;

                /* true names of the vertices */
                a = V[u]; b = V[v]; c = V[w];

                /* output Triangle */
                result.Add(contour[a]);
                result.Add(contour[b]);
                result.Add(contour[c]);

                m++;

                /* remove v from remaining polygon */
                for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t]; nv--;

                /* resest error detection counter */
                count = 2 * nv;
            }
        }


       
        return true;
    }

    static private bool Snip(List<Vector2> contour, int u, int v, int w, int n, int[] V)
    {
        int p;
        float Ax, Ay, Bx, By, Cx, Cy, Px, Py;

        Ax = contour[V[u]].x;
        Ay = contour[V[u]].y;

        Bx = contour[V[v]].x;
        By = contour[V[v]].y;

        Cx = contour[V[w]].x;
        Cy = contour[V[w]].y;

        if (EPSILON > (((Bx - Ax) * (Cy - Ay)) - ((By - Ay) * (Cx - Ax))))
            return false;

        for (p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w))
                continue;
            Px = contour[V[p]].x;
            Py = contour[V[p]].y;
            if (InsideTriangle(Ax, Ay, Bx, By, Cx, Cy, Px, Py))
                return false;
        }

        return true;
    }

    static bool InsideTriangle(float Ax, float Ay,
                      float Bx, float By,
                      float Cx, float Cy,
                      float Px, float Py)

    {
        float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
        float cCROSSap, bCROSScp, aCROSSbp;

        ax = Cx - Bx; ay = Cy - By;
        bx = Ax - Cx; by = Ay - Cy;
        cx = Bx - Ax; cy = By - Ay;
        apx = Px - Ax; apy = Py - Ay;
        bpx = Px - Bx; bpy = Py - By;
        cpx = Px - Cx; cpy = Py - Cy;

        aCROSSbp = ax * bpy - ay * bpx;
        cCROSSap = cx * apy - cy * apx;
        bCROSScp = bx * cpy - by * cpx;

        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }

    static private bool isPolygonCounterClocked(List<Vector2> contour)
    { //判断传进来的多边形数组是否是 逆时针
        if (contour.Count < 3)
        {
            Debug.Log("contour.size is smaller than 3, it can not form a polygon");
            return false;
        }
        /*
          判断组成多边形(无论凹凸性)的点的顺序 是否为逆时针：
          1，找到x最大或y最大的点v1，因为该点必为凸点。
          2，找到两个矢量，v1-v0,与v2-v1, 叉乘这两个矢量，得到的值若为正值，则为逆时针，负为顺时针
        */
        int maxIndex = -1;
        float max = -1.0f;
        for (int i = 0; i < contour.Count; ++i)
            if (contour[i].x > max)
            { //找最大x的点，
                max = contour[i].x;
                maxIndex = i;
            }

        Vector2 v1, v2;
        if (maxIndex == 0)
        {
            v1 = (contour[0] - contour[contour.Count - 1]).normalized;
            v2 = (contour[1] - contour[0]).normalized;
        }
        else if (maxIndex == contour.Count - 1)
        {
            v1 = (contour[contour.Count - 1] - contour[contour.Count - 2]).normalized;
            v2 = (contour[0] - contour[contour.Count - 1]).normalized;
        }
        else
        {
            v1 = (contour[maxIndex] - contour[maxIndex - 1]).normalized;
            v2 = (contour[maxIndex + 1] - contour[maxIndex]).normalized;
        }

        float res = v1.x * v2.y - v2.x * v1.y;

        if (res > 0)
            return true;

        return false;
    }
}
