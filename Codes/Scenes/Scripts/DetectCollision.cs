using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DetectCollision : MonoBehaviour
{
    /*
     *  射线与三角形的碰撞检测工具类
         */
    internal static void GetMassSideAndRoofTriFaces(List<MassFacade> masses, List<Vector3> rVertices, List<int> rTriangles)
    {
        /*
         * 功能：返回masses的facade与plane roof 面片的所有顶点和三角网格索引 
         * 参数： rVertices, rTriangles,是返回给上一层的参数
         * 
         * bug: 因为 facadeplane 与 facadecurve 存了前后两个面，所以返回的rVertices，与rTriangles有重复点
         */
        rVertices.Clear();
        rTriangles.Clear();

        for (int k = 0; k < masses.Count; ++k) 
        {
            MassFacade mass = masses[k];
            mass.GetWorldCoordBuildingMesh().ForEach(mesh =>
            {
                //if (go.name == "FacadePlane(Clone)" || go.name == "FacadeCurve(Clone)" || go.name == "PlaneRoof(Clone)") //找到building的侧边对象与planeroof对象
                //{
                //    Mesh mesh = go.GetComponent<MeshFilter>().mesh;

                    for (int i = 0; i < mesh.triangles.Length; ++i)
                    {
                        rTriangles.Add(rVertices.Count + mesh.triangles[i]);
                    }

                    rVertices.AddRange(mesh.vertices.ToList());
                //}

            });


            //mass.goes.ForEach(go =>
            //{
            //    if (go.name == "FacadePlane(Clone)" || go.name == "FacadeCurve(Clone)" || go.name == "PlaneRoof(Clone)") //找到building的侧边对象与planeroof对象
            //    {
            //        Mesh mesh = go.GetComponent<MeshFilter>().mesh;

            //        for (int i = 0; i < mesh.triangles.Length; ++i)
            //         {
            //            rTriangles.Add(rVertices.Count + mesh.triangles[i]);
            //         }

            //         rVertices.AddRange(mesh.vertices.ToList());
            //    }

            //});


        }
    }

    //internal static void GetAllTrianglePlaneRoofFaces(List<MassFacade> masses, List<Vector3> rVertices, List<int> rTriangles)
    //{
    //    /*
    //     * 功能：返回masses屋顶默认平面 的所有顶点和三角网格索引 
    //     * 参数： rVertices, rTriangles,是返回给上一层的参数
    //     * 
    //     * bug: 因为 facadeplane 与 facadecurve 存了前后两个面，所以返回的rVertices，与rTriangles有重复点
    //     */
    //    rVertices.Clear();
    //    rTriangles.Clear();

    //    for (int k = 0; k < masses.Count; ++k)
    //    {
    //        MassFacade mass = masses[k];
    //        mass.goes.ForEach(go =>
    //        {
    //            if (go.name == "PlaneRoof(Clone)") //找到building的侧边对象
    //            {
    //                Mesh mesh = go.GetComponent<MeshFilter>().mesh;

    //                for (int i = 0; i < mesh.triangles.Length; ++i)
    //                {
    //                    rTriangles.Add(rVertices.Count + mesh.triangles[i]);
    //                }

    //                rVertices.AddRange(mesh.vertices.ToList());
    //            }

    //        });
    //    }
    //}

    internal static List<Ray> GetAllRays(List<FacadeFace> m_faces)
    {
        /*
         * 功能：返回FacadeFace所有点能发射的所有射线，
         *       Top与bottom各顶点平行于xoz平面发出的四条射线 + bottom顶点向下发出的四条射线
         * 参数： 
         */

        //0.拿到不重复的facedes顶面的所有顶点

        List<Vector3> allTopPtsPos = new List<Vector3>();

        m_faces.ForEach(face =>
        {
            for (int i = 0; i < face.tops.Count - 1; ++i)
            {
                allTopPtsPos.Add(face.tops[i]);
            }
        });


        //1. 计算射线ray
        float height = m_faces[0].tops[0].y - m_faces[0].bottoms[0].y;//拿到facade的高度
        List<Ray> rays = new List<Ray>();

        for(int i = 0; i < allTopPtsPos.Count; ++i)
        {
            Vector3 p0 = allTopPtsPos[(i - 1) == -1 ? allTopPtsPos.Count - 1 : i - 1];
            Vector3 p1 = allTopPtsPos[i];
            Vector3 p2 = allTopPtsPos[(i+1) % allTopPtsPos.Count];

            //每个点发出四条射线
            Ray ray0 = new Ray(p1, (p2 - p1).normalized);
            Ray ray1 = new Ray(p1, (p1 - p0).normalized);

            Ray ray2 = new Ray(p1, (p1 - p2).normalized);
            Ray ray3 = new Ray(p1, (p0 - p1).normalized);

            rays.Add(ray0);
            rays.Add(ray1);
            rays.Add(ray2);
            rays.Add(ray3);

            //facade的bottom 顶点同样也能发射射线。
            //notice:!! 注意，这里为偷懒，由bottom发出的射线 直接 拷贝top， 如果bottom是与top不一样的形状，则该方法失效
            rays.Add(new Ray(ray0.origin - height * Vector3.up, ray0.direction));
            rays.Add(new Ray(ray1.origin - height * Vector3.up, ray1.direction));
            rays.Add(new Ray(ray2.origin - height * Vector3.up, ray2.direction));
            rays.Add(new Ray(ray3.origin - height * Vector3.up, ray3.direction));

        }


        //2.拿到底面点向下发出的射线
        List<Vector3> allBottomPtsPos = new List<Vector3>();

        m_faces.ForEach(face =>
        {
            for (int i = 0; i < face.bottoms.Count - 1; ++i)
            {
                allBottomPtsPos.Add(face.bottoms[i]);
            }
        });

        for (int i = 0; i < allBottomPtsPos.Count; ++i)
        {
            rays.Add(new Ray(allBottomPtsPos[i], -Vector3.up));
        }

        return rays;
    }

    public static bool GetMinDistRay(List<Ray> rays, List<Vector3> vertices, List<int> triangles, out Ray rRay, out float rDist)
    {

        /*
         * 功能：从已知射线rays中，找到与面片triangles 距离最短的射线，
         *     如果 该射线不存在（rays与面片不相交）， return false;
         *     
         *     如果存在该射线，则返回值射线存在rRay，射线长度存在rDist
         * 
         */
         //0.记录所有的 单个三角面片
        List<Vector3[]> allSingleTris = new List<Vector3[]>(); 
        for(int k = 0; k < triangles.Count; k += 3)
        {
            Vector3[] tri = new Vector3[3];
            tri[0] = vertices[triangles[k]];
            tri[1] = vertices[triangles[k+1]];
            tri[2] = vertices[triangles[k+2]];
            allSingleTris.Add(tri);

        }


        //1. 找到 所有射线 与 面片碰撞的最短距离
        // 1.1 找到单个射线与所有面片碰撞的最短距离
        List<float> minDists = new List<float>();
        rays.ForEach(ray =>
        {
            List<float> dists = CheckTriCollision(ray, allSingleTris);//得到ray与所有三角面片的距离
            float minDist = -1.0f;
            int j = 0;
            for (; j < dists.Count; ++j)  //先找到第一个大于0的距离 作为最小值假设
            {
                if (dists[j] > 0.0f)
                {
                    minDist = dists[j];
                    break;
                }

            }
            for (; j < dists.Count; ++j)
            {
                if (dists[j] <= -1.0f)
                    continue;
                minDist = Mathf.Min(minDist, dists[j]);
            }

            minDists.Add(minDist);
        });


        //1.2 找到所有射线与面片碰撞的最短距离
        float minD = -1.0f;
        int i = 0;
        for (; i < minDists.Count; ++i)  //先找到第一个大于0的距离 作为最小值假设
        {
            if (minDists[i] > 0.0f)
            {
                minD = minDists[i];
                break;
            }

        }
        for (; i < minDists.Count; ++i)
        {
            if (minDists[i] <= -1.0f)
                continue;
            minD = Mathf.Min(minD, minDists[i]);
        }

        //1.3 返回
        if(minD == -1.0f)
        {
            rRay = new Ray();
            rDist = 0.0f;
            return false;
        }

        rRay = rays[minDists.IndexOf(minD)];
        rDist = minD;

        return true;
    }


    /*
 *  ray与组成Mesh的Triangeles的碰撞检测
 * 
 */
    private static List<float> CheckTriCollision(Ray ray, List<Vector3[]> triangles)
    {
        List<float> vec_t = new List<float>();
        for (int i = 0; i < triangles.Count; ++i)
        {
            vec_t.Add(CheckSingleTriCollision(ray, triangles[i]));
        }


        return vec_t;
    }

    
    private static float CheckSingleTriCollision(Ray ray, Vector3[] triangle) 
    {
        /*
         * 功能：检测ray与单个三角面片 triangle的 相交情况
         *      如果不相交，返回-1.0f
         *      相交则返回 射线起始点到三角面片的距离
         */

        Vector3 E1 = triangle[1] - triangle[0];
        Vector3 E2 = triangle[2] - triangle[0];

        Vector3 P = Vector3.Cross(ray.direction, E2);
        float det = Vector3.Dot(P, E1);

        Vector3 T;
        if (det > 0)
        {
            T = ray.origin - triangle[0];
        }
        else
        {
            T = triangle[0] - ray.origin;
            det *= -1.0f;
        }

        if (det < 0.00001f) //表示射线与三角面所在的平面平行，返回不相交
            return -1.0f;

        /******* 相交则判断 交点是否落在三角形面内 *********/
        float u = Vector3.Dot(P, T);
        if (u < 0.0f || u > det)
            return -1.0f;

        Vector3 Q = Vector3.Cross(T, E1);
        float v = Vector3.Dot(Q, ray.direction);
        if (v < 0.0f || u + v > det)
            return -1.0f;

        float t = Vector3.Dot(Q, E2);
        if (t < 0.0f)
            return -1.0f;

        return t / det;
    }

    //internal static List<Ray> GetRaysFromBottomToDown(List<FacadeFace> m_faces)
    //{
    //    /*
    //    * 功能：返回FacadeFace的所有bottom点向下(即(0, -1, 0)方向) 发射的所有射线，
    //    * 参数： 
    //    */

    //    //0.拿到不重复的facedes 底面的所有顶点

    //    List<Vector3> allBottomPtsPos = new List<Vector3>();

    //    m_faces.ForEach(face =>
    //    {
    //        for (int i = 0; i < face.bottoms.Count - 1; ++i)
    //        {
    //            allBottomPtsPos.Add(face.bottoms[i]);
    //        }
    //    });


    //    //1. 计算射线ray
    //    List<Ray> rays = new List<Ray>();

    //    for (int i = 0; i < allBottomPtsPos.Count; ++i)
    //    {
    //        rays.Add(new Ray(allBottomPtsPos[i], -Vector3.up));
    //    }



    //    return rays;
    //}
}
