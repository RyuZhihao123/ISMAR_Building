using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Leap.Unity.Interaction;

public class MainEnter : MonoBehaviour
{

    TcpConnector m_tcpConnector;  // 通信管理器（已封装勿动） -- FX 2021-5-9 : 我就动，我不但动 我还改,诶，就是玩儿
    LeapController m_leapController;//LeapMotion 管理器（管理所有交互的封装类）

    StrokeType buildingType = StrokeType._RECTANGLE;//Temp!!!! 暂时先用键盘按键 管理两种绘制类型
    //StrokeType buildingType = StrokeType._FREEFORM;//Temp!!!! 暂时先用键盘按键 管理两种绘制类型

    RoofType roofType = RoofType._TRIANGLE;

    public static List<MassFacade> masses = new List<MassFacade>();
    List<Vector3> temp = new List<Vector3>();

    bool isMoveMass = true;
    public static int moveTargetMassID = 0; //上一个被抓取的massID

    void Start()
    {
        ////  [服务器] 通信管理器设置
        //m_tcpConnector = new TcpConnector();
        //m_tcpConnector.ConnectedToServer(); // 连接到服务器

        //// LeapMotion交互管理器设置
        m_leapController = new LeapController();
        m_leapController.Initialize();

        //打开文件
        //masses.AddRange(Util.ReadMassFile("C:/Users/vcc/Desktop/BuildingFile.txt"));

        ////Demo
        //MassFacade mass = new MassFacade(); //Building建模管理器
        //List<Stroke> fileStrokes = new List<Stroke>();

        //int CurID = 1;  // 默认5,14. 9, 11，25, 40是个问题 (40需要反向)

        ////fileStrokes = Util.ReadFromFile("C:/Users/vcc/Desktop/noheadtail_labeled_unit_data.xls");
        //fileStrokes = Util.ReadFromFile("C:/Users/vcc/Desktop/label_5-28-2.xls");


        //Stroke curStroke = fileStrokes[CurID];
        ////curStroke.type = StrokeType._RECTANGLE;
        //curStroke.type = StrokeType._FREEFORM;
        ////curStroke.type = StrokeType._CIRCLE;

        //Stroke heightStroke = new Stroke();
        //heightStroke.rawPts.Add(0.2f * Vector3.up);
        //heightStroke.rawPts.Add(Vector3.zero);


        //List<Stroke> buildingStrokes = new List<Stroke>();
        //buildingStrokes.Add(curStroke);
        //buildingStrokes.Add(heightStroke);

        //mass.Initialize(buildingStrokes);
        //masses.Add(mass);

        //temp = curStroke.rawPts;
        //tempType = curStroke.rawLabels;

        ////strokes.Add(curStroke);
        ////Debug.Log("temp curve count: " + temp.Count);

        //RoofType type = RoofType._TRAPEZIUM;//先假使是梯形顶
        //RoofType type = RoofType._TRIANGLE;//先假使是梯形顶
        //RoofType type = RoofType._HEMISPHERE;

        //mass.ConstructRoof(type, strokes);


    }


    void Update()
    {
        //暂时先用键盘进行管里
        UpdateKeyControl();

        // 检测绘制线条的操作
        bool isDoneSketchStroke = m_leapController.DoSketchStroke();

        // 检测用户是否有抓取操作来移动building
        m_leapController.DoMoveMasses(masses);
    }

    private void UpdateKeyControl()
    {



    }

    public void ButtonControlDrawing()
    {
        m_leapController.ButtonControlDrawing();
    }

    public void ButtonCancelDrawing()
    {
        m_leapController.ButtonCancelDrawing();  
    }

    //List<Stroke> strokes = new List<Stroke>();
    //MassFacade mass = new MassFacade();
    List<PointType> tempType = new List<PointType>();
    public void ButtonConstructBuilding()
    {
        /*
         * 加一个控制高度的新功能
          */

        MassFacade mass = new MassFacade();


        List<Stroke> strokes = new List<Stroke>();
        bool res = m_leapController.GetAllStrokes(strokes);


        if (res) //如果当前系统存在线条
        {
            strokes[0].type = buildingType;

            // [服务器] 把当前线条发送给服务器 (必须的)
            //List<Vector3> sendline = m_leapController.GetTcpStroke();
            //m_tcpConnector.SendToServer(sendline);

            ////// [服务器] 接收LSTM的返回值（线条类型：rectangle, ellipse, freeform）
            ////stroke.type = m_tcpConnector.RecieveLSTMmsg();
            ////Debug.Log("stroke type: " + stroke.type);

            ////// [服务器] 接收PointNet的返回值（corner straight, curve）（必须的）

            //if (strokes[0].type == StrokeType._FREEFORM)
            //{
            //    List<PointType> predPtsLabels = m_tcpConnector.RecievePointNetmsg(sendline.Count);
            //    //Util.tempSaveStrokesLabel(predPtsLabels, "C:/Users/vcc/Desktop/label.txt");
            //    strokes[0].rawLabels = predPtsLabels;
            //    //Debug.Log(string.Format("线条数目: {0}/{0}", stroke.rawPts.Count, stroke.rawLabels.Count));
            //}

            ////temp = strokes[0].rawPts;
            ////tempType = strokes[0].rawLabels;


            mass.Initialize(strokes);

            masses.Add(mass);
            MassFacadeFactory.StitchMass(masses, mass);

            moveTargetMassID = masses.Count - 1;
        }

    }



    public void ButtonConstructRoof()
    {
        var strokes = new List<Stroke>();
        bool res = m_leapController.GetAllStrokes(strokes);

        if (res)
        {
            //todo，网络预测屋顶类型
            Roof.SelectRoofToMass(masses, strokes).ConstructRoof(roofType, strokes);
            //masses[0].ConstructRoof(type, strokes);

        }

    }

    public void ButtonConstructDoor()
    {
        /*
         * 功能： 确定门的位置，
         * 实现： 随便画一坨线条，取均值点，然后和所有mass的所有窗户位置 进行匹配，找出距离最短的一个 进行位置的替换
         */
        var strokes = new List<Stroke>();
        bool res = m_leapController.GetAllStrokes(strokes);

        if (res)
        {
            MassFacadeFactory.ConstructDoor(masses, strokes);
        }


    }
    public void ButtonSaveMasses()
    {
        /*
         * 功能： 存储当前场景绘制的所有building，
         * 实现： 
         */
        Util.SaveMassFile(masses, "C:/Users/vcc/Desktop/BuildingFile.txt");

        //m_leapController.SaveLineRenderer();
    }

    public void SliderSetWindowSize()
    {
        float value = GameObject.Find("Dynamic UI Object/Workstation Base/Workstation Panel/Button Panel/WindowSizeSlider/Slider").GetComponent<InteractionSlider>().HorizontalSliderValue;
        masses[moveTargetMassID].windowSize = value;
        masses[moveTargetMassID].UpdateMass();

    }

    public void ButtonHasWindowPrefab()
    {
        masses[moveTargetMassID].isHasWindowPrefab = !masses[moveTargetMassID].isHasWindowPrefab;
        masses[moveTargetMassID].UpdateMass();
    }


    public void ButtonSetMatWall1()
    {
        masses[moveTargetMassID].curID_planeWall = 1;
        //masses[moveTargetMassID].curID_curveWall = 1;
        masses[moveTargetMassID].UpdateMass();
    }
    public void ButtonSetMatWall11()
    {
        masses[moveTargetMassID].curID_planeWall = 11;
        masses[moveTargetMassID].UpdateMass();
    }

    public void ButtonSetMatWall12()
    {
        masses[moveTargetMassID].curID_planeWall = 12;
        masses[moveTargetMassID].UpdateMass();
    }

    public void ButtonSetMatWall5()
    {
        masses[moveTargetMassID].curID_planeWall = 5;
        masses[moveTargetMassID].UpdateMass();
    }

    public void ButtonSetMatWall3()
    {
        masses[moveTargetMassID].curID_planeWall = 3;
        masses[moveTargetMassID].curID_curveWall = 4;

        masses[moveTargetMassID].UpdateMass();
    }

    public void ButtonSetMatWindowWall1()
    {
        masses[moveTargetMassID].curID_curveWall = 1;
        masses[moveTargetMassID].curID_planeWall = 7;
        masses[moveTargetMassID].UpdateMass();
    }

    public void ButtonSetPrefabWindow3()
    {
        masses[moveTargetMassID].curID_window = 3;
        masses[moveTargetMassID].UpdateMass();
    }
    public void ButtonSetPrefabWindow4()
    {
        masses[moveTargetMassID].curID_window = 4;
        masses[moveTargetMassID].UpdateMass();
    }

    public void ButtonSetPrefabWindow6()
    {
        masses[moveTargetMassID].curID_window = 6;
        masses[moveTargetMassID].UpdateMass();
    }

    public void ButtonSetMatRoof2()
    {
        masses[moveTargetMassID].curID_sideRoof = 2;
        masses[moveTargetMassID].UpdateMass();
    }

    public void ButtonSetPrefabDoor0()
    {
        PrefabConfig.curID_door = 0;
    }

    public void ButtonSetPrefabDoor1()
    {
        PrefabConfig.curID_door = 1;
    }

    public void ButtonCtlMove()
    {
        /*
         * 功能：building 移动控制块的管理
         */
        isMoveMass = !isMoveMass;

        masses.ForEach(mass =>
        {
            mass.moveCtrl.ctrlGo.SetActive(isMoveMass);
        });




    }

    private void OnDrawGizmos()
    {
        if (temp.Count != 0)
        {
            for(int i = 0; i < temp.Count; ++i)
            {
                Vector3 p = temp[i];
                if (tempType[i] == PointType._STARIGHT)
                {
                    Gizmos.color = Color.blue;

                }else if(tempType[i] == PointType._CORNER)
                {
                    Gizmos.color = Color.red;
                }
                else if (tempType[i] == PointType._CURVE)
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawSphere(p, 0.002f);
            }

            //Gizmos.color = Color.red;
            //temp.ForEach(p =>
            //{
                
            //    Gizmos.DrawSphere(p, 0.002f);

            //});
        }

        if (MassFacadeFactory.temp.Count != 0)
        {
            Gizmos.color = Color.blue;
            MassFacadeFactory.temp.ForEach(p =>
            {
                //Gizmos.DrawSphere(p+0.01f*Vector3.up, 0.002f);
                Gizmos.DrawSphere(p, 0.008f);


            });
        }

        if (MassFacade.temp.Count != 0)
        {
            Gizmos.color = Color.red;
            MassFacade.temp.ForEach(p =>
            {
                Gizmos.DrawSphere(p, 0.006f);


            });
        }

        //if (Roof.temp.Count != 0)
        //{
        //    Gizmos.color = Color.red;
        //    Roof.temp.ForEach(p =>
        //    {
        //        Gizmos.DrawSphere(p, 0.003f);

        //    });
        //}

    }
}
