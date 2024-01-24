using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
///  基本的数据类型
/// </summary>


// 线条类型(enum)
public enum StrokeType
{
    _ILLEGAL = -1,

    _CIRCLE = 1,
    _RECTANGLE = 2,
    _FREEFORM = 3,
    _PILLAR = 4,
    _STAIR = 5,
    _DOOR = 6,
    _SHADE = 7
    

}

//屋顶类型(enum)
public enum RoofType 
{
    _PLANE = -1,
    _TRIANGLE = 0,
    _HEMISPHERE = 1,
    _TRAPEZIUM  =2
}

// 顶点类型（enum）
public enum PointType
{
    _CORNER = 0,
    _STARIGHT = 1,
    _CURVE = 2
}

//通过手势抓取的移动控制块信息
public class MoveController
{

    public GameObject ctrlGo; //移动控制器
    //public Vector3 ctrlInitPos; //控制器的初始状态信息（位置，旋转）
    //public Quaternion ctrlInitQuat;
    public float lastRotateY;
    public Vector3 lastPos;

    public bool isGraping = false;//是否正处于抓取状态
    public void Initialize(Vector3 pos)
    {
        if (ctrlGo != null)
        {
            GameObject.Destroy(ctrlGo);
        }
        ctrlGo = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_moveController));

        //ctrlInitPos = pos;
        //ctrlInitQuat = new Quaternion();

        ctrlGo.transform.position = pos; //不能直接赋予 ctrlInitTrans, 因为会“引用”
        ctrlGo.transform.rotation = new Quaternion();

        lastRotateY = 0.0f;
        lastPos = pos;
    }

}

//// Facade类型（enum）
//public enum FacadeType
//{
//    _CORNER = 0,
//    _STARIGHT = 1,
//    _CURVE = 2
//}

// 手绘线条（Stroke）
public class Stroke
{
    public StrokeType type;    // 线条类型（FreeForm还是Rectangle这些）

    public List<Vector3> rawPts = new List<Vector3>(); // 原始顶点

    public List<PointType> rawLabels = new List<PointType>();     // 原始的顶点对应的Labels
}

// 一个面（tops的数目=2表示平面，>2表示弯曲面）
public class FacadeFace
{
    public List<Vector3> tops = new List<Vector3>();    // 顶面
    public List<Vector3> bottoms = new List<Vector3>(); // 底面

    //public List<bool> roofLabels = new List<bool>();    // roof（False-默认roof，True-手绘roof）
    //public FacadeType type;//这段facadeFace的类型，straight or curve
}

// Stroke中的一个段（Segment）,仅仅用在（Freeform）中
public class Segment
{
    public PointType type;       // 这一段segment的类型（Straight or Curve）
    public List<Vector3> pts = new List<Vector3>();   // 这一段线条对应的
}

/// <summary>
///  Prefab和材质相关的东西哦
/// </summary>
public class PrefabConfig
{
    // 默认prefab，顶多修改其材质，但是路径不会变
    public static string p_facadeCurve = "Prefabs/FacadeCurve";  // 弧面的Facade
    public static string p_facadePlane = "Prefabs/FacadePlane";  // 弧面的Facade
    public static string p_facadePlaneRoof = "Prefabs/PlaneRoof"; // 默认的平面屋顶
    public static string p_facadeRoof = "Prefabs/Roof"; // 手工设置的屋顶


    public static string p_ralling = "Prefabs/Railing";  // 栏杆
    public static string p_stair = "Prefabs/Stair/Stairs1";  // 楼梯
    //public static string p_door = "Prefabs/Door/door02";  // 门

    public static string p_facadeMarginBar = "Prefabs/FacadeMarginBar";  // Facade面之间的填充物
    public static string p_parent = "Prefabs/Drawing/BuildingMass";//管理mass所有组件的父组件
    public static string p_moveController = "Prefabs/Drawing/MoveController";//mass的移动控件


    // 装饰品的prefab们（窗户和门）
    public static string[] p_windows = new string[] { "Prefabs/Window/1", "Prefabs/Window/win01", "Prefabs/Window/win02","Prefabs/Window/win07", "Prefabs/Window/win04", "Prefabs/Window/win05", "Prefabs/Window/win06" };
    public static string[] p_doors = new string[] { "Prefabs/Door/door0", "Prefabs/Door/door02" };

    // 当前设置！
    public static int curID_window = 4;
    public static int curID_door = 1;

}

public class MaterialConfig
{
    // 材质路径
    public static string[] p_matPlaneRoof = new string[] { "Textures/planeroof/1", "Textures/planeroof/2", "Textures/planeroof/3" };  //屋顶
    // 平面的墙壁纹理
    public static string[] p_matPlaneWall = new string[] { "Textures/wall/1", "Textures/wall/1", "Textures/wall/2", "Textures/wall/3","Textures/wall/4","Textures/wall/5",
                                               "Textures/wall/6", "Textures/window/1", "Textures/window/2", "Textures/window/3" ,
                                                 "Textures/wall/7","Textures/wall/11","Textures/wall/12","Textures/wall/13"};
    // 弧面的墙壁纹理
    public static string[] p_matCurveWall = new string[] { "Textures/window/1", "Textures/window/1", "Textures/window/2", "Textures/window/3", "Textures/wall/3", "Textures/wall/13" };
    public static string[] p_matSideRoof = new string[] { "Textures/roof/0","Textures/roof/1", "Textures/roof/2" };


    // 设置
    public static int curID_planeRoof = 2;
    public static int curID_sideRoof = 2;

    public static int curID_planeWall = 3;
    public static int curID_curveWall = 2;
}

public class GameObjectManager
{

    public static GameObject GetCurveFacade(Mesh mesh)
    {
        GameObject go = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_facadeCurve));
        go.GetComponent<MeshRenderer>().material = Resources.Load<Material>(
            MaterialConfig.p_matCurveWall[MaterialConfig.curID_curveWall]);
        go.GetComponent<Transform>().position = Vector3.zero;
        go.GetComponent<MeshFilter>().mesh = mesh;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;


        return go;

        
    }






    public static GameObject GetPlaneFacade(Mesh mesh)
    {
        GameObject go = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_facadePlane));
        go.GetComponent<MeshRenderer>().material = Resources.Load<Material>(
            MaterialConfig.p_matPlaneWall[MaterialConfig.curID_planeWall]);
        go.GetComponent<Transform>().position = Vector3.zero;
        go.GetComponent<MeshFilter>().mesh = mesh;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;

        return go;
    }

    public static GameObject GetPlaneRoof(Mesh mesh, GameObject parentGo=null)
    {
        GameObject go = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_facadePlaneRoof));
        go.GetComponent<MeshRenderer>().material = Resources.Load<Material>(MaterialConfig.p_matPlaneRoof[MaterialConfig.curID_planeRoof]); 

        go.GetComponent<Transform>().position = Vector3.zero;
        go.GetComponent<MeshFilter>().mesh = mesh;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        if (parentGo != null)
            go.transform.parent = parentGo.transform;

        return go;
    }
    public static GameObject GetRoof(Mesh mesh)
    {
        GameObject go = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_facadeRoof));
        go.GetComponent<MeshRenderer>().material = Resources.Load<Material>(MaterialConfig.p_matSideRoof[MaterialConfig.curID_sideRoof]);

        go.GetComponent<Transform>().position = Vector3.zero;
        go.GetComponent<MeshFilter>().mesh = mesh;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;


        return go;
    }



    public static GameObject GetRailing(Mesh mesh, GameObject parentGo=null)
    {
        GameObject go = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_ralling)); ;
        go.GetComponent<Transform>().position = Vector3.zero;
        go.GetComponent<MeshFilter>().mesh = mesh;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        if (parentGo != null)
            go.transform.parent = parentGo.transform;

        return go;
    }

    public static GameObject GetFacadeMarginBar(FacadeFace face)
    {
        Vector3 center = (face.tops[0] + face.bottoms[0]) / 2.0f;
        float length = Vector3.Distance(face.tops[0], face.bottoms[0]);
        GameObject go = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_facadeMarginBar));
        go.GetComponent<Transform>().position = center;
        go.GetComponent<Transform>().localScale += new Vector3(0, length / 2.0f - 1.0f, 0);


        return go;
    }

    

    public static GameObject GetWindow()
    {
        GameObject go = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_windows[PrefabConfig.curID_window]));

        return go;
    }

    public static GameObject GetStair()
    {
        GameObject go = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_stair));

        return go;
    }

    public static GameObject GetDoor()
    {
        GameObject go = GameObject.Instantiate((GameObject)Resources.Load(PrefabConfig.p_doors[PrefabConfig.curID_door]));

        return go;
    }
}