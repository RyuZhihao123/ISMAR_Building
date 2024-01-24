using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class MassRoof
{

}

public class MassFacade{

    // 几何控制信息
    public List<FacadeFace> m_faces = new List<FacadeFace>();   // 编辑的时候，
    public float buildingHeight;

    // 网格对象
    public GameObject parentGo; //父组件，该mass下所有的GameObject都挂靠在该对象下
    public List<GameObject> goes = new List<GameObject>();      // Facade自身网格
    public List<GameObject> components = new List<GameObject>();  //只放窗户

    //屋顶
    public StrokeType buildingType = new StrokeType(); //building的类型，free，rectangle or ellipse，
    private List<FacadeFace> roofTopfaces = new List<FacadeFace>();//存储三角剖分梯形屋顶的 顶面所用。 

    //抓取移动控制信息
    public MoveController moveCtrl = new MoveController();//通过手势移动的 move控制器
    public static List<Vector3> temp = new List<Vector3>();

    /*** 摩天大楼 ***/
    ////该mass的材质属性
    //public int curID_planeRoof = 2;
    //public int curID_sideRoof = 0;

    //public int curID_planeWall = 5;
    //public int curID_curveWall = 2;

    //public int curID_window = 2;
    //public int curID_door = 1;

    ////窗户属性
    //public float windowSize = 0.1f;
    //public bool isHasWindowPrefab = false;

    ///*** 乡间小屋 ***/
    public int curID_planeRoof = 2;
    public int curID_sideRoof = 2;

    public int curID_planeWall = 6;
    public int curID_curveWall = 2;

    public int curID_window = 4;
    public int curID_door = 1;

    //窗户属性
    public float windowSize = 0.06f;
    public bool isHasWindowPrefab = true;

    // 初始化：输入线条（只有第一根哦），高度 
    // FX 2021-5-10: 太骚气了，还 “第一根哦”，你搞第二根半价呢
    //public void Initialize(Stroke stroke, float height)
    //{
    //    parentGo =  GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_parent));

    //    this.height = height;

    //    m_faces = MassFacadeFactory.GetMass(stroke, this.height);
    //    buildingType = stroke.type;


    //    // 默认的roof（false表示生成默认roof）
    //    //m_faces.ForEach(face => { face.roofLabels = Enumerable.Repeat(false, face.tops.Count).ToList(); });



    //    UpdateMass(); //更新网格 及 移动控制块
    //    //UpdateDefaultRoof();//更新默认屋顶

    //}
    //internal List<GameObject> GetComponets()
    //{
    //    return this.components;
    //}

    internal void Initialize(List<Stroke> strokes)
    {
        parentGo = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_parent));


        this.buildingHeight = 0.1f; //默认高度

        if (strokes.Count > 1)
        { //
            this.buildingHeight = MassFacadeFactory.GetBuildingHeight(strokes); //如果存在 第二根 第三根线条，则用来赋予高度

        }

    
        buildingType = strokes[0].type;
        //Util.tempDebug("1", "C:/Users/vcc/Desktop/Debug.txt");
        m_faces = MassFacadeFactory.GetMass(strokes, this.buildingHeight);
        //Util.tempDebug("2", "C:/Users/vcc/Desktop/Debug.txt");

        UpdateMass(); //更新网格 及 移动控制块
        //Util.tempDebug("6", "C:/Users/vcc/Desktop/Debug.txt");
    }

    public void UpdateMass()
    {
        UpdateMaterial();//更新材质
        UpdateMesh();  // 更新网格
        //UpdateDefaultRoof();//更新默认屋顶
        UpdateController(); //更新控制块信息
    }

    private void UpdateMaterial()
    {
        MaterialConfig.curID_curveWall = this.curID_curveWall;
        MaterialConfig.curID_planeRoof = this.curID_planeRoof;
        MaterialConfig.curID_planeWall = this.curID_planeWall;
        MaterialConfig.curID_sideRoof = this.curID_sideRoof;

        PrefabConfig.curID_door = this.curID_door;
        PrefabConfig.curID_window = this.curID_window;
    }

    public void UpdateController()
    {
        //0. 拿到 facade顶面的中心位置
        Vector3 avrPos = Vector3.zero;
        int count = 0;

        List<FacadeFace> worldFaces = GetWorldCoordFacadeFaces();
        worldFaces.ForEach(face =>
        {
            for (int i = 0; i < face.tops.Count - 1; ++i)
            {
                avrPos += face.tops[i];
                count++;
            }
        });
        //Debug.Log(avrPos + " :" + count);
        avrPos /= count;
        avrPos += 0.12f * Vector3.up;


        //1. 赋予中心位置
        moveCtrl.Initialize(avrPos);


    }

    public List<FacadeFace> GetWorldCoordFacadeFaces()
    {
        /*
         * 功能: 将该mass的m_faces 统统处理为 世界坐标系下的绝对坐标。
         * 
         */


        List<FacadeFace> worldFaces = new List<FacadeFace>();

        m_faces.ForEach(face =>
        {
            FacadeFace newFace = new FacadeFace();

            for(int i = 0; i < face.tops.Count; ++i)
            {
                newFace.tops.Add(parentGo.transform.TransformPoint(face.tops[i]));
                newFace.bottoms.Add(parentGo.transform.TransformPoint(face.bottoms[i]));

            }
            //if(m_faces.IndexOf(face) == 0)
            //{
            //    Debug.Log("face: " + face.tops[0].x + " : " + face.tops[0].y + " : " + face.tops[0].z);
            //}

            worldFaces.Add(newFace);

        });


        return worldFaces;

    }

    public List<Vector3> GetWorldCoordWindowPos()
    {
        /*
         * 功能：得到该mass的 所有窗户的 世界坐标系下的 中心坐标
         */

        List<Vector3> poses = new List<Vector3>();

        components.ForEach(win =>
        {
            poses.Add(parentGo.transform.TransformPoint(win.transform.localPosition));

        });

        return poses;
    }

    public List<Mesh> GetWorldCoordBuildingMesh()
    {
        /*
         * 功能：返回 组成该building主体的，"FacadePlane(Clone)" || go.name == "FacadeCurve(Clone)" || go.name == "PlaneRoof(Clone)" 
         *       的世界坐标下的mesh，
         *       供碰撞检测使用
         */
        List<Mesh> worldMeshes = new List<Mesh>();

        this.goes.ForEach(go =>
        {
            if (go.name == "FacadePlane(Clone)" || go.name == "FacadeCurve(Clone)" || go.name == "PlaneRoof(Clone)") //找到building的侧边对象与planeroof对象
            {
                Vector3[] newVertices = go.GetComponent<MeshFilter>().mesh.vertices;
                int[] newTriangles = go.GetComponent<MeshFilter>().mesh.triangles;


                for(int i = 0; i < newVertices.Length; ++i)
                {
                    newVertices[i] = parentGo.transform.TransformPoint( newVertices[i]);
                }
                Mesh newMesh = new Mesh();

                newMesh.vertices = newVertices;
                newMesh.triangles = newTriangles;

                worldMeshes.Add(newMesh);

                //for (int i = 0; i < mesh.triangles.Length; ++i)
                //{
                //    rTriangles.Add(rVertices.Count + mesh.triangles[i]);
                //}

                //rVertices.AddRange(mesh.vertices.ToList());
            }

        });

        return worldMeshes;
    }


    public void ConstructRoof(RoofType type, List<Stroke> strokes)
    {
        if(strokes.Count == 0)
        {
            Debug.Log("ERROR: ConstructRoof");
            return;
        }

        // 0.去除默认roof 与 railing，或者是旧屋顶
        if (parentGo.transform.Find("PlaneRoof(Clone)") != null) // 0.0 去除默认屋顶
        {

            goes.Remove(parentGo.transform.Find("PlaneRoof(Clone)").gameObject);
            goes.Remove(parentGo.transform.Find("Railing(Clone)").gameObject);


            UnityEngine.Object.Destroy(parentGo.transform.Find("PlaneRoof(Clone)").gameObject);
            UnityEngine.Object.Destroy(parentGo.transform.Find("Railing(Clone)").gameObject);
            //parentGo.transform.Find("PlaneRoof(Clone)").gameObject.SetActive(false);
            //parentGo.transform.Find("Railing(Clone)").gameObject.SetActive(false);
        }



        if (parentGo.transform.Find("Roof(Clone)") != null) // 0.1 去除旧屋顶
        {
            goes.Remove(parentGo.transform.Find("Roof(Clone)").gameObject);
            UnityEngine.Object.Destroy(parentGo.transform.Find("Roof(Clone)").gameObject);
        }

        // 1. 根据 rooftype 选择新屋顶

        GameObject roof = null;

        if (type == RoofType._TRIANGLE)
        {
            roof = Roof.GetTriangleRoof(buildingType, GetWorldCoordFacadeFaces(), strokes);

        }
        else if(type == RoofType._TRAPEZIUM)
        {
            roof = Roof.GetTrapeziumRoof(buildingType, GetWorldCoordFacadeFaces(), strokes, roofTopfaces);

            // 添加栏杆
            goes.Add(Roof.GetDefaultRailing(roofTopfaces, parentGo));
            //goes[goes.Count].transform.parent = parentGo.transform;

            // 添加平面屋顶
            goes.Add(Roof.GetDefaultRoof(roofTopfaces, parentGo));
            //goes[goes.Count].transform.parent = parentGo.transform;

            //roofTop = Roof.GetTrapzium
        }
        else if(type == RoofType._HEMISPHERE)
        {
            roof = Roof.GetHemishpereRoof(buildingType, GetWorldCoordFacadeFaces(), strokes);
        }



        // 2.添加进当前meshes
        roof.transform.parent = parentGo.transform;
        goes.Add(roof);



    }



    // 更新网格数据（编辑完直接调用）
    private void UpdateMesh()
    {
        // 重置（网格本身和附加组件）
        goes.ForEach(i => { GameObject.DestroyImmediate(i); });
        goes.Clear();
        components.ForEach(i => { GameObject.DestroyImmediate(i); });
        components.Clear();

        GameObject.Destroy(parentGo);
        parentGo = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_parent));

        // 遍历每个面 FacadeFace
        //float windowSize = UnityEngine.Random.Range(0.03f, 0.05f);
        //float windowSize = 0.1f;

        for (int m = 0; m < m_faces.Count; ++m)
        {
            FacadeFace face = m_faces[m];

            if (face.tops.Count < 2)
                continue;
            
            if(face.tops.Count > 2)  // 弯曲弧面
            {
                //Mesh mesh = Util.GetCurveFacadeMesh(face, UnityEngine.Random.Range(0.02f, 0.03f));
                Mesh mesh = Util.GetCurveFacadeMesh(face, windowSize);

                GameObject go = GameObjectManager.GetCurveFacade(mesh);
                
                goes.Add(go);
            }
            if(face.tops.Count == 2) // 直面
            {
                // 先根据这个facadeface生成mesh（并且把窗户的预制体放到components里面）
                ////Mesh mesh = Util.GetPlaneFacadeMesh(face, components, UnityEngine.Random.Range(0.02f, 0.03f));  
                //bool isHasDoor = false;
                //if (m == 0)
                //    isHasDoor = true;
                Mesh mesh = Util.GetPlaneFacadeMesh(face, windowSize);
                Util.GetWindowPrefabs(face, components, windowSize);
                components.ForEach(com => { com.SetActive(isHasWindowPrefab); });//默认窗户配件不激活

                Util.GetStairPrefab(face, goes, windowSize);//激活楼梯

                // 从cofig里头实例化一个prefab，并且替换掉默认的mesh
                GameObject go = GameObjectManager.GetPlaneFacade(mesh);

                goes.Add(go);
            }
            // 生成边界的填充物
            //components.Add(GameObjectManager.GetFacadeMarginBar(face));
        }
        //Debug.Log("m_faces count: " + m_faces.Count);
        Util.tempDebug("7", "C:/Users/vcc/Desktop/Debug.txt");
        //添加栏杆
        goes.Add(Roof.GetDefaultRailing(m_faces, parentGo));
        Util.tempDebug("9", "C:/Users/vcc/Desktop/Debug.txt");
        // 添加平面屋顶
        goes.Add(Roof.GetDefaultRoof(m_faces, parentGo));
        Util.tempDebug("8", "C:/Users/vcc/Desktop/Debug.txt");
        //设置 所有对象的父组件
        for (int i = 0; i < goes.Count; ++i)
        {
            goes[i].transform.parent = parentGo.transform;
        }

        components.ForEach(go =>
        {
            go.transform.parent = parentGo.transform;
        });
    }

    //public void UpdateDefaultRoof()
    //{
    //    goes.Add(Roof.GetDefaultRailing(m_faces, parentGo));
    //    // 添加平面屋顶
    //    goes.Add(Roof.GetDefaultRoof(m_faces, parentGo));
    //}

    public void MoveRoof(Vector3 offset, Transform trans = null)
    {
        /*
         * 功能：移动building的屋顶，这是一个写的很糟糕的函数，破坏了代码框架，
         *    用处是 当用户用手
         */
    }

}


public class Mass: MonoBehaviour
{
    //MassFacade mass = new MassFacade();
    //List<Stroke> strokes = new List<Stroke>();

    //int CurID = 14;  // 默认14. 9, 11，25, 40是个问题 (40需要反向)

    private void Start()
    {
        //strokes = Util.ReadFromFile();

        //Stroke curStroke = strokes[CurID];

        //mass.Initialize(curStroke, 0.1f);
    }

    private void OnDrawGizmos()
    {
        //if (strokes.Count != 0)
        //{

        //    Stroke curstroke = strokes[CurID];

        //    for (int i = 0; i < curstroke.rawPts.Count; ++i)
        //    {
        //        Vector3 offset = new Vector3(0.0f, 0.05f, 0.0f);
        //        if (i != curstroke.rawPts.Count - 1)
        //            Gizmos.DrawLine(curstroke.rawPts[i] + offset, curstroke.rawPts[i + 1] + offset);

        //        if (curstroke.rawLabels[i] == PointType._CORNER)
        //            Gizmos.DrawWireSphere(curstroke.rawPts[i] + offset, 0.002f);
        //        if (curstroke.rawLabels[i] == PointType._STARIGHT)
        //            Gizmos.DrawCube(curstroke.rawPts[i] + offset, new Vector3(0.002f, 0.002f, 0.002f));
        //        if (curstroke.rawLabels[i] == PointType._CURVE)
        //            Gizmos.DrawSphere(curstroke.rawPts[i] + offset, 0.002f);

        //    }
        //}
    }
}