using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;
using mattatz.Triangulation2DSystem;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

/// <summary>
///  一些辅助函数：主要是网格和几何的显示和构造。、
///  辅助函数（PCA，）
/// </summary>

public class Util
{
    // 生成弧面FacadeFace的网格
    public static Mesh GetCurveFacadeMesh(FacadeFace face, float defaultWindowSize = 3.0f)
    {


        // 获得基本信息
        Vector2 faceSize = GetFacadeSize(face);

        int xCellNum = Math.Max(1, (int)(faceSize.x / defaultWindowSize));  // 窗户的水平数量
        int yCellNum = Math.Max(1, (int)(faceSize.y / defaultWindowSize));  // 窗户的竖直数量
        float realWinW = faceSize.x / xCellNum;  // 窗户的真正宽度
        float realWinH = faceSize.y / yCellNum;  // 窗户的真正高度

        // 生成Mesh
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangles = new List<int>();

        float len1 = 0.0f;
        for (int i = 0; i < face.tops.Count - 1; ++i)
        {
            float len2 = len1 + Vector3.Distance(face.tops[i], face.tops[i + 1]);

            int id1 = i;
            int id2 = i + 1;

            Vector3 v00 = face.bottoms[id2];
            Vector3 v10 = face.bottoms[id1];
            Vector3 v01 = face.tops[id2];
            Vector3 v11 = face.tops[id1];

            //Vector2 t00 = new Vector2(len2 / realWinW, 0.0f);
            //Vector2 t10 = new Vector2(len1 / realWinW, 0.0f);
            //Vector2 t01 = new Vector2(len2 / realWinW, faceSize.y / realWinH);
            //Vector2 t11 = new Vector2(len1 / realWinW, faceSize.y / realWinH);

            //Vector2 t00 = new Vector2(len2 / defaultWindowSize, 0.0f);
            //Vector2 t10 = new Vector2(len1 / defaultWindowSize, 0.0f);
            //Vector2 t01 = new Vector2(len2 / defaultWindowSize, faceSize.y / defaultWindowSize);
            //Vector2 t11 = new Vector2(len1 / defaultWindowSize, faceSize.y / defaultWindowSize);

            Vector2 t00 = new Vector2(len2 / defaultWindowSize, 0.0f);
            Vector2 t10 = new Vector2(len1 / defaultWindowSize, 0.0f);
            Vector2 t01 = new Vector2(len2 / defaultWindowSize, faceSize.y / realWinH);
            Vector2 t11 = new Vector2(len1 / defaultWindowSize, faceSize.y / realWinH);

            // 把这个面片放进来哦
            AddQuad(triangles, vertices, uv, v00, v10, v01, v11, t00, t10, t01, t11, vertices.Count);

            len1 = len2;
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uv.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }



    public static void GetWindowPrefabs(FacadeFace face, List<GameObject> coms, float defaultWindowSize=3.0f)
    {
        /*
         * 功能：生成 window的预制件，并放置
         */
        // 生成窗户的components
        Vector2 faceSize = GetFacadeSize(face);

        int yNum = Math.Max(1, (int)(faceSize.y / defaultWindowSize));  // 窗户的竖直数量; //分割的层数，如3层，那就四条线
        float realH = faceSize.y / yNum;
        List<Vector3> leftPts = new List<Vector3>(); //存分层的左边点
        List<Vector3> rightPts = new List<Vector3>();//分层的右边点

        for (int i = 0; i <= yNum; ++i)
        {
            float prop = (float)i / yNum;
            leftPts.Add(face.bottoms[0] + prop * (face.tops[0] - face.bottoms[0]));
            rightPts.Add(face.bottoms[1] + prop * (face.tops[1] - face.bottoms[1]));

        }
        Vector3 dirX = (rightPts[0] - leftPts[0]).normalized;
        Vector3 norm = Vector3.Cross(face.bottoms[0] - face.tops[0], face.tops[1] - face.tops[0]).normalized;
        Vector3 dirY = Vector3.Cross(face.tops[1] - face.tops[0], norm).normalized;

        for (int i = 1; i < leftPts.Count; ++i)
        {
            int xNum = Math.Max(1, (int)((leftPts[i] - rightPts[i]).magnitude / defaultWindowSize));
            float realW = (leftPts[i] - rightPts[i]).magnitude / xNum;
            for (int j = 0; j < xNum; ++j)
            {
                Vector3 center = leftPts[i] + (j + 0.5f) * realW * dirX;
                //Vector3 center = leftPts[i] + (j + 0.5f) * defaultWindowSize * dirX;

                center += 0.4f * realH * dirY;

                GameObject go = GameObjectManager.GetWindow();
                go.GetComponent<Transform>().position = center;
                go.GetComponent<Transform>().localScale *= realH * 0.6f;

                Vector3 angle = Quaternion.FromToRotation(new Vector3(0.0f, 0.0f, -1.0f), norm).eulerAngles;

                go.GetComponent<Transform>().rotation = Quaternion.Euler(angle.x, angle.y, 0.0f); //禁止z轴旋转

                coms.Add(go);
            }
        }
    }

    // 生成平面FacadeFace的网格
    public static Mesh GetPlaneFacadeMesh(FacadeFace face, float defaultWindowSize = 3.0f)
    {
        // 获得基本信息
        Vector2 faceSize = GetFacadeSize(face);

        int xCellNum = Math.Max(1, (int)(faceSize.x / defaultWindowSize));  // 窗户的水平数量
        int yCellNum = Math.Max(1, (int)(faceSize.y / defaultWindowSize));  // 窗户的竖直数量
        float realWinW = faceSize.x / xCellNum;  // 窗户的真正宽度
        float realWinH = faceSize.y / yCellNum;  // 窗户的真正高度

        // 生成Mesh
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangles = new List<int>();

        Vector3 v00 = face.bottoms[1];
        Vector3 v10 = face.bottoms[0];
        Vector3 v01 = face.tops[1];
        Vector3 v11 = face.tops[0];

        Vector2 t00 = new Vector2(faceSize.x / realWinW, 0.0f);
        Vector2 t10 = new Vector2(0.0f, 0.0f);
        Vector2 t01 = new Vector2(faceSize.x / realWinW, faceSize.y / realWinH);
        Vector2 t11 = new Vector2(0.0f, faceSize.y / realWinH);

        //Vector2 t00 = new Vector2((v10-v00).magnitude / defaultWindowSize, 0.0f);
        //Vector2 t10 = new Vector2(0.0f, 0.0f);
        //Vector2 t01 = new Vector2(Vector3.Dot(v01 - v10, (v00 - v10).normalized) / defaultWindowSize, faceSize.y / defaultWindowSize);
        //Vector2 t11 = new Vector2(Vector3.Dot(v11-v10, (v00-v10).normalized)/defaultWindowSize, faceSize.y / defaultWindowSize);

        // 把这个面片放进来哦
        AddQuad(triangles, vertices, uv, v00, v10, v01, v11, t00, t10, t01, t11, 0);

        mesh.vertices = vertices.ToArray();
        mesh.uv = uv.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();




        return mesh;
    }

    public static void AddQuad(List<int> triangles, List<Vector3> vertices, List<Vector2> uvs,
        Vector3 v00, Vector3 v10, Vector3 v01, Vector3 v11,
        Vector3 t00, Vector3 t10, Vector3 t01, Vector3 t11,
        int curTriNum)
    {
        // 加入正面
        vertices.Add(v00); vertices.Add(v10); vertices.Add(v01); vertices.Add(v11);
        uvs.Add(t00); uvs.Add(t10); uvs.Add(t01); uvs.Add(t11);

        SetQuadID(triangles, curTriNum + 0, curTriNum + 1, curTriNum + 2, curTriNum + 3);

        // 加入背面
        vertices.Add(v10); vertices.Add(v00); vertices.Add(v11); vertices.Add(v01);
        uvs.Add(t10); uvs.Add(t00); uvs.Add(t11); uvs.Add(t01);
        SetQuadID(triangles, curTriNum + 4, curTriNum + 5, curTriNum + 6, curTriNum + 7);
    }

    private static void AddQuad(List<int> triangles, List<Vector3> vertices, List<Vector2> uvs,
        Vector3 v00, Vector3 v10, Vector3 v01, Vector3 v11,
        int curTriNum)
    {
        // 加入正面
        vertices.Add(v00); vertices.Add(v10); vertices.Add(v01); vertices.Add(v11);
        uvs.Add(new Vector2(0.0f, 0.0f)); uvs.Add(new Vector2(1.0f, 0.0f)); uvs.Add(new Vector2(0.0f, 1.0f)); uvs.Add(new Vector2(1.0f, 1.0f));
        SetQuadID(triangles, curTriNum + 0, curTriNum + 1, curTriNum + 2, curTriNum + 3);

        // 加入背面
        vertices.Add(v10); vertices.Add(v00); vertices.Add(v11); vertices.Add(v01);
        uvs.Add(new Vector2(0.0f, 0.0f)); uvs.Add(new Vector2(1.0f, 0.0f)); uvs.Add(new Vector2(0.0f, 1.0f)); uvs.Add(new Vector2(1.0f, 1.0f));
        SetQuadID(triangles, curTriNum + 4, curTriNum + 5, curTriNum + 6, curTriNum + 7);
    }

    private static void SetQuadID(List<int> triangles, int v00, int v10, int v01, int v11)
    {
        triangles.Add(v00);
        triangles.Add(v01);
        triangles.Add(v10);

        triangles.Add(v10);
        triangles.Add(v01);
        triangles.Add(v11);
    }

    // 获取Facade的尺寸
    public static Vector2 GetFacadeSize(FacadeFace face)
    {
        float len = 0.0f;

        for (int i = 0; i < face.tops.Count - 1; ++i)
        {
            len += Vector3.Distance(face.tops[i], face.tops[i + 1]);
        }
        float height = Vector3.Distance(face.tops[0], face.bottoms[0]);

        return new Vector2(len, height);
    }



    

    public static List<Vector3> DoUnitStroke(Stroke stroke)
    {
        List<Vector3> unitPoses = new List<Vector3>();
        stroke.rawPts.ForEach(p =>
        {
            unitPoses.Add(p);
        });



        Vector3 center = Vector3.zero;

        unitPoses.ForEach(i => { center += i; });  // 求中点center
        center /= unitPoses.Count;

        for (int i = 0; i < unitPoses.Count; ++i)
            unitPoses[i] -= center;

        float maxDelta = 0.0f;

        unitPoses.ForEach(i => {
            maxDelta = Mathf.Max(Mathf.Abs(i.x), maxDelta);
            maxDelta = Mathf.Max(Mathf.Abs(i.y), maxDelta);
            maxDelta = Mathf.Max(Mathf.Abs(i.z), maxDelta);
        });


        Matrix4x4 scaleMat = Matrix4x4.Scale(new Vector3(1.0f / maxDelta, 1.0f / maxDelta, 1.0f / maxDelta));
        for (int i = 0; i < unitPoses.Count; ++i)
            unitPoses[i] = scaleMat.MultiplyPoint(unitPoses[i]);



        return unitPoses;
    }
    public static Stroke GetSamplerLineRenderer(LineRenderer line, float offset = 0.005f)
    {

        Vector3[] linePos = new Vector3[line.positionCount];
        line.GetPositions(linePos);

        Stroke stroke = new Stroke();
        stroke.rawPts = linePos.ToList();

        stroke = Util.GetSamplerStroke(stroke, 0.005f);

        return stroke;
    }


    public static Stroke GetSamplerStroke(Stroke stroke, float offset = 0.005f)
    {
        /*
         * 功能：对传进来的 stroke按照offset间隔进行采样
         */

        
        Stroke ss = new Stroke();
        ss.type = stroke.type;
        

        Vector3 lastPos = stroke.rawPts[0];
        ss.rawPts.Add(lastPos);
        for (int i = 1; i < stroke.rawPts.Count; ++i)
        {
            Vector3 p = stroke.rawPts[i];
            if (Vector3.Distance(p, lastPos) >= offset)
            {
                ss.rawPts.Add(p);
                
                lastPos = p;
            }
        }

        return ss;
    }

    internal static void GetStairPrefab(FacadeFace face, List<GameObject> goes, float windowSize)
    {
        /*
         * 生成楼梯
         */

        // 获得基本信息
        Vector2 faceSize = GetFacadeSize(face);

        int xCellNum = Math.Max(1, (int)(faceSize.x / windowSize));  // 窗户的水平数量
        int yCellNum = Math.Max(1, (int)(faceSize.y / windowSize));  // 窗户的竖直数量
        float realWinW = faceSize.x / xCellNum;  // 窗户的真正宽度
        float realWinH = faceSize.y / yCellNum;  // 窗户的真正高度


        int yNum = Math.Max(1, (int)(faceSize.y / windowSize));  // 窗户的竖直数量; //分割的层数，如3层，那就四条线
        float realH = faceSize.y / yNum;


        Vector3 dirX = (face.bottoms[1] - face.bottoms[0]).normalized;
        Vector3 norm = Vector3.Cross(face.bottoms[0] - face.tops[0], face.tops[1] - face.tops[0]).normalized;
        Vector3 dirY = Vector3.Cross(face.tops[1] - face.tops[0], norm).normalized;

        //生成楼梯的components
        for (int y = 0; y < yCellNum - 1; y++)
        {
            GameObject stair = GameObjectManager.GetStair();
            Vector3 center1 = face.bottoms[0] + (0.0f + 0.5f) * realWinW * dirX;
            center1 -= y * realWinH * dirY;
            center1 += 0.02f * norm;

            stair.GetComponent<Transform>().position = center1;
            Vector3 ls = stair.GetComponent<Transform>().localScale;
            stair.GetComponent<Transform>().localScale = new Vector3(ls.x * realWinH * 0.2f, ls.y * realWinH * 0.3f, ls.z * realWinH * 0.2f);
            stair.GetComponent<Transform>().rotation = Quaternion.FromToRotation(new Vector3(0.0f, 0.0f, -1.0f), norm);

            goes.Add(stair);

        }


    }

    public static List<Vector3> GetSamplerCurve(List<Vector3> linePoses, float offset = 0.005f)
    {
        /*
         * 功能：对传进来的 line 按照offset间隔进行采样
         */

        List<Vector3> res = new List<Vector3>();
        //Stroke ss = new Stroke();
        //ss.type = stroke.type;


        Vector3 lastPos = linePoses[0];
        res.Add(lastPos);
        for (int i = 1; i < linePoses.Count; ++i)
        {
            Vector3 p = linePoses[i];
            if (Vector3.Distance(p, lastPos) >= offset)
            {
                res.Add(p);

                lastPos = p;
            }
        }
        if(res[res.Count-1] != linePoses[linePoses.Count - 1])//把line的最后一个点也放进采样点集合中
        {
            res.Add(linePoses[linePoses.Count - 1]);
        }
        if(res.Count == 2) //至少保证返回三个点
        {
            res.Insert(1,linePoses[linePoses.Count / 2]);
        }


        return res;
    }




    public static void SetCubicMesh(List<int> triangles, List<Vector3> vertices, List<Vector2> uv,
        Vector3 p1, Vector3 p2, Vector3 radi1, Vector3 radi2, Vector3 up)
    {
        // front
        Vector3 v00 = p2 + radi2 - up;
        Vector3 v10 = p1 + radi1 - up;
        Vector3 v01 = p2 + radi2 + up;
        Vector3 v11 = p1 + radi1 + up;
        AddQuad(triangles, vertices, uv, v00, v10, v01, v11, vertices.Count);
        // back
        v00 = p2 - radi2 - up;
        v10 = p1 - radi1 - up;
        v01 = p2 - radi2 + up;
        v11 = p1 - radi1 + up;
        AddQuad(triangles, vertices, uv, v00, v10, v01, v11, vertices.Count);

        // top
        v00 = p2 - radi2 + up;
        v10 = p1 - radi1 + up;
        v01 = p2 + radi2 + up;
        v11 = p1 + radi1 + up;
        AddQuad(triangles, vertices, uv, v00, v10, v01, v11, vertices.Count);

        // bottom
        v00 = p2 - radi2 - up;
        v10 = p1 - radi1 - up;
        v01 = p2 + radi2 - up;
        v11 = p1 + radi1 - up;
        AddQuad(triangles, vertices, uv, v00, v10, v01, v11, vertices.Count);
    }

    // 计算UV坐标，值得注意的是，输入的vertices的Y轴坐标将被摒弃
    public static List<Vector2> CalculateUVs(List<Vector3> vertices)
    {
        // 配置参数
        float defaultSize = 0.07f;

        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;

        vertices.ForEach(p =>
        {
            minX = Math.Min(minX, p.x);
            minZ = Math.Min(minZ, p.z);
            maxX = Math.Max(maxX, p.x);
            maxZ = Math.Max(maxZ, p.z);
        });

        float lenX = maxX - minX, lenZ = maxZ - minZ;

        List<Vector2> uvs = new List<Vector2>();



        for(int i=0; i<vertices.Count; ++i)
        {
            Vector3 p = vertices[i];
            float dx = p.x - minX;
            float dz = p.z - minZ;

            uvs.Add(new Vector2(dx / defaultSize, dz / defaultSize));
        }

        return uvs;

    }



    public static List<Vector3> GetPCADirFromStroke(Stroke stroke)
    {
        /*
         * 返回该stroke的两个归一化pca方向，
         * notice: 默认stroke经过平面化处理，且平行于xOy平面
         *
         */
        float avgX = 0.0f;
        float avgZ = 0.0f;

        //step 1: 求零均值化后的矩阵X
        stroke.rawPts.ForEach(p =>
        {
            avgX += p.x;
            avgZ += p.z;
        });
        avgX /= stroke.rawPts.Count;
        avgZ /= stroke.rawPts.Count;

        List<float> XValues = new List<float>();
        stroke.rawPts.ForEach(p =>
        {
            XValues.Add(p.x - avgX);
            XValues.Add(p.z - avgZ);

        });


        var mat = Matrix<float>.Build;
        var X = mat.Dense(2, stroke.rawPts.Count, XValues.ToArray());
        //Debug.Log(X);

        //step 2：求协方差矩阵C
        var C = (X * X.Transpose()) / stroke.rawPts.Count;

        //step 3: 求C的特征向量evd
        Matrix<float> evd = C.Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Symmetric).EigenVectors;
        //Debug.Log(evd);

        //step 4: 返回pca的两个向量
        List<Vector3> dirs = new List<Vector3>();
        dirs.Add(new Vector3(evd[0, 0], 0.0f, evd[1, 0]).normalized);
        dirs.Add(new Vector3(evd[0, 1], 0.0f, evd[1, 1]).normalized);

        //Debug.Log(new Vector3(evd[0, 0], 0.0f, evd[1, 0]));
        //Debug.Log(new Vector3(evd[0, 1], 0.0f, evd[1, 1]));


        return dirs;
    }

    public static List<Vector3> GetMinAreaRect(List<Vector3> convexPoints)
    {
        /*
         * 旋转卡壳算法 
         *  求凸包最小面积的外接矩形
         *  https://blog.csdn.net/weixin_39916966/article/details/104731800
         *  
         * 返回：矩形的四个顶点  
         */
        List<Vector3> res = new List<Vector3>();

        if (convexPoints.Count < 2)
        {
            Debug.Log("ERROR: GetMinAreaRect");
            return res;
        }

        // 1.设定初始轴
        int id = 0;
        Vector3 initAxis = (convexPoints[id + 1] - convexPoints[id]).normalized;
        List<float> areas = new List<float>(); //存矩形面积集合
        List<List<Vector3>> rects = new List<List<Vector3>>(); //存矩形四个点

        while (true)
        {
            // 2.寻找多边形四个端点
            Vector3 originPos = convexPoints[id], nextPos = convexPoints[id + 1];
            Vector3 xAxis = (nextPos - originPos).normalized;


            if (Vector3.Dot(initAxis, xAxis) < 0)//只用旋转90度 就可找到 最小面积外接矩形
                break;

            Vector3 yAxis = Vector3.Cross(Vector3.up, xAxis).normalized;
            Vector3 minX = nextPos;
            //Vector3 minY = originPos;
            Vector3 maxX = nextPos;
            Vector3 maxY = nextPos;

            float lenMinX = Vector3.Dot((nextPos - originPos), xAxis);
            float lenMaxX = Vector3.Dot((nextPos - originPos), xAxis);
            float lenMaxY = Mathf.Abs(Vector3.Dot((nextPos - originPos), yAxis));


            //  2.1 找到一组 端点
            for (int i = 0; i < convexPoints.Count; ++i)
            {
                Vector3 p = convexPoints[i];
                if (p.Equals(originPos)) //跳过自身点
                    continue;

                float tminX = Vector3.Dot((p - originPos), xAxis);
                float tmaxX = Vector3.Dot((p - originPos), xAxis);
                float tmaxY = Mathf.Abs(Vector3.Dot((p - originPos), yAxis));

                if (tminX < lenMinX)
                {
                    lenMinX = tminX;
                    minX = p;
                }
                if (tmaxX > lenMaxX)
                {
                    lenMaxX = tmaxX;
                    maxX = p;
                }
                if (tmaxY > lenMaxY)
                {
                    lenMaxY = tmaxY;
                    maxY = p;
                }

            }

            //2.2 记录该组端点 及 外接矩形面积
            Vector3 a = originPos + Vector3.Dot((maxX - originPos), xAxis) * xAxis;
            Vector3 b = a + Vector3.Dot((maxY - originPos), yAxis) * yAxis;
            Vector3 d = originPos + Vector3.Dot((minX - originPos), xAxis) * xAxis;
            Vector3 c = d + Vector3.Dot((maxY - originPos), yAxis) * yAxis;

            List<Vector3> rect = new List<Vector3>();
            rect.Add(a);
            rect.Add(b);
            rect.Add(c);
            rect.Add(d);
            rects.Add(rect);

            float area = (b - a).magnitude * (d - a).magnitude;
            areas.Add(area);

            // 2.3 开始下一组循环
            id++; //notice: 因为id的关系，可能存在内存泄漏
        }

        // 2.4 找到面积最小的外接矩形；
        float minArea = areas[0];
        areas.ForEach(area =>
        {
            minArea = Mathf.Min(area, minArea);
        });

        res = rects[areas.IndexOf(minArea)];

        //res.Add(a);
        //res.Add(b);
        //res.Add(c);
        //res.Add(d);



        return res;
    }

    // 读取数据
    public static List<Stroke> ReadFromFile(string file)
    {
        //StreamReader sr = new StreamReader(@"C:/Users/vcc/Desktop/noheadtail_labeled_unit_data.xls");
        StreamReader sr = new StreamReader(file);

        var strokes = new List<Stroke>();

        while (true)
        {
            string line1 = sr.ReadLine();
            string line2 = sr.ReadLine();

            if (line1 == null || line2 == null)
                break;

            var item1 = new List<String>(Regex.Split(line1, "\\s+"));
            var item2 = new List<String>(Regex.Split(line2, "\\s+"));

            item1.Remove("");
            item2.Remove("");

            Stroke cur = new Stroke();

            for (int i = 0; i < item2.Count; ++i)
            {
                cur.rawPts.Add(new Vector3(
                    1.5f*(float)System.Convert.ToDouble(item1[3 * i + 0]),
                    1.3f+(float)System.Convert.ToDouble(item1[3 * i + 1]),
                    1.5f * (float)System.Convert.ToDouble(item1[3 * i + 2])));

                cur.rawLabels.Add((PointType)System.Convert.ToInt32(item2[i]));
            }

            strokes.Add(cur);
        }
        return strokes;
    }

    public static void tempSaveStrokesLabel(List<PointType> predPtsLabels, string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        FileStream fileStream = new FileStream(filePath, FileMode.Create);
        StreamWriter writer = new StreamWriter(fileStream);

        predPtsLabels.ForEach(p =>
        {
            writer.Write(" " + (int)p);
        });



        writer.Close();
        fileStream.Close();
    }

    public static void tempDebug(string value, string filePath)
    {
        //if (File.Exists(filePath))
        //{
        //    File.Delete(filePath);
        //}

        FileStream fileStream = new FileStream(filePath, FileMode.Append);
        StreamWriter writer = new StreamWriter(fileStream);

        //predPtsLabels.ForEach(p =>
        //{
            writer.WriteLine(value);
        //});



        writer.Close();
        fileStream.Close();
    }

    public static void  SaveMassFile(List<MassFacade> masses, string filePath)
    {
        /*
         * 功能:将当前的所有 masses 存储起来
         */


        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        FileStream fileStream = new FileStream(filePath, FileMode.Create);
        StreamWriter writer = new StreamWriter(fileStream);

        masses.ForEach(mass =>
        {
            writer.WriteLine("mass");

            //0. 存该mass的 m_faces
            mass.GetWorldCoordFacadeFaces().ForEach(face =>
            {
                writer.Write("top ");
                for(int i = 0; i < face.tops.Count; ++i)
                {
                    Vector3 p = face.tops[i];
                    writer.Write(p.x + " " + p.y + " " + p.z + " ");

                }
                writer.Write("\n");

                writer.Write("bottom ");
                for (int i = 0; i < face.bottoms.Count; ++i)
                {
                    Vector3 p = face.bottoms[i];
                    writer.Write(p.x + " " + p.y + " " + p.z + " ");

                }
                writer.Write("\n");
            });

            //1. 存该mass的 buildingHeight
            writer.WriteLine("buildingHeight " + mass.buildingHeight);

            //2. 存该mass的 buildingType
            writer.WriteLine("buildingType " + (int)mass.buildingType);

            //3. 存该mass的材质属性
            writer.WriteLine("curID_curveWall " + mass.curID_curveWall);
            writer.WriteLine("curID_planeRoof " + mass.curID_planeRoof);
            writer.WriteLine("curID_planeWall " + mass.curID_planeWall);
            writer.WriteLine("curID_sideRoof " + mass.curID_sideRoof);

            writer.WriteLine("curID_door " + mass.curID_door);
            writer.WriteLine("curID_window " + mass.curID_window);
            writer.WriteLine("windowSize " + mass.windowSize);
            writer.WriteLine("isHasWindowPrefab " + mass.isHasWindowPrefab);

            writer.WriteLine("endmass");



        });

        writer.Close();
        fileStream.Close();
    }

    public static List<MassFacade> ReadMassFile(string filePath)
    {
        /*
         * 功能:读取 masses
         */
        float s = 1.0f;

        StreamReader reader = new StreamReader(filePath);

        List<MassFacade> masses = new List<MassFacade>();
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if(line == "mass")
            {
                //0. 读取相关信息
                MassFacade mass = new MassFacade();
                while ((line = reader.ReadLine()) != "endmass")
                {
                    string[] items1 = line.Trim().Split(' ');
                    if (items1[0] == "buildingHeight")
                    {
                        mass.buildingHeight = float.Parse(items1[1]);
                    }
                    else if(items1[0] == "buildingType")
                    {
                        mass.buildingType = (StrokeType)int.Parse(items1[1]);
                    }
                    else if(items1[0] == "top")
                    {

                        FacadeFace face = new FacadeFace();
                        //top
                        for (int i = 1; i < items1.Length; i += 3)
                        {
                            //Debug.Log(items1[i]);
                            Vector3 p = new Vector3(s*float.Parse(items1[i]), s*float.Parse(items1[i + 1]), s * float.Parse(items1[i + 2]));
                            face.tops.Add(p);
                        }

                        //bottom
                        string line2 = reader.ReadLine();
                        string[] items2 = line2.Trim().Split(' ');
                        for (int i = 1; i < items2.Length; i += 3)
                        {
                            Vector3 p = new Vector3(s * float.Parse(items2[i]), s * float.Parse(items2[i + 1]), s * float.Parse(items2[i + 2]));
                            face.bottoms.Add(p);
                        }

                        mass.m_faces.Add(face);
                    }
                    else if (items1[0] == "curID_curveWall") //读材质
                    {
                        mass.curID_curveWall = int.Parse(items1[1]);
                        mass.curID_planeRoof = int.Parse(reader.ReadLine().Trim().Split(' ')[1]);
                        mass.curID_planeWall = int.Parse(reader.ReadLine().Trim().Split(' ')[1]);
                        mass.curID_sideRoof = int.Parse(reader.ReadLine().Trim().Split(' ')[1]);

                        mass.curID_door = int.Parse(reader.ReadLine().Trim().Split(' ')[1]);
                        mass.curID_window = int.Parse(reader.ReadLine().Trim().Split(' ')[1]);

                    }else if(items1[0] == "windowSize")
                    {
                        mass.windowSize = float.Parse(items1[1]);
                    }else if(items1[0] == "isHasWindowPrefab")
                    {
                        mass.isHasWindowPrefab = bool.Parse(items1[1]);
                    }




                }


                //1. 构造mass的网格
                mass.UpdateMass();

                masses.Add(mass);
            }
        }


        reader.Close();

        return masses;
    }
}