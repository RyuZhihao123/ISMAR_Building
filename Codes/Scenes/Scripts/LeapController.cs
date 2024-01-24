using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using System.Linq;
using System;
using Leap.Unity.Interaction;
using System.IO;

public class LeapController : MonoBehaviour
{
    /************   绘制状态用参数   *************/
    private LeapProvider provider;
    private Frame frame;

    GameObject drawingStateSphere;//一个sphere，当绘制手势正确后，出现在食指与拇指中间，表示系统开始记录stroke数据

    bool isAllowDrawing = false; //是否允许现在开始绘制stroke, 默认 不允许绘制，点击按钮开启绘制状态
    GameObject drawStrokeObj; //开始绘制按钮的颜色控制，以表示现在处于哪个绘制状态，default/Active/Warning

    /************    UI按钮用参数      ***************/
    Color buttonDefaultColor = new Color(207.0f / 255.0f, 207.0f / 255.0f, 207.0f / 255.0f);
    Color buttonActiveColor = new Color(0.0f, 255.0f, 0.0f);
    Color buttonWarningColor = new Color(255.0f, 0.0f, 0.0f);

    List<LineRenderer> strokeLineRenderers = new List<LineRenderer>();//将画过的所有线条存起来，实现Cancel线条的功能


    // -------------- 绘制三维线条 ----------------//

    //List<Vector3> m_stroke = new List<Vector3>();   // 当前绘制的线条数据
    LineRenderer m_strokeLineRenderer;    // 可视化LineRenderer

    Vector3 lastPos = Vector3.zero;

    public void Initialize()
    {
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;

        m_strokeLineRenderer = ((GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Drawing/Stroke"))).GetComponent<LineRenderer>();
        m_strokeLineRenderer.startWidth = m_strokeLineRenderer.endWidth = 0.002f;
        m_strokeLineRenderer.positionCount = 0;
        //GameObject father = m_strokeLineRenderer.transform.parent.gameObject;
        drawingStateSphere = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Drawing/DrawingStateSphere"));
        drawingStateSphere.GetComponent<MeshRenderer>().enabled = false; //默认 不可视

        drawStrokeObj = GameObject.Find("Dynamic UI Object/Workstation Base/Workstation Panel/Button Panel/DrawStroke/Cube (1)");
    }

    internal void DoMoveMasses(List<MassFacade> masses)
    {
        masses.ForEach(mass =>
        {
            MoveController ctrl = mass.moveCtrl;
            //得在mass那加一个 抓取的状态
            if (ctrl.ctrlGo.GetComponent<InteractionBehaviour>().isGrasped)
            {
                Vector3 curPos = ctrl.ctrlGo.transform.position;
                Quaternion curQuat = ctrl.ctrlGo.transform.rotation;

                Vector3 offsetPos = curPos - ctrl.lastPos;
                float offsetAngle = curQuat.eulerAngles.y - ctrl.lastRotateY;

                //移动mass
                mass.parentGo.transform.RotateAround(curPos, Vector3.up, offsetAngle);
                mass.parentGo.transform.position += offsetPos;

                ctrl.lastPos = curPos;
                ctrl.lastRotateY = curQuat.eulerAngles.y;

                ctrl.isGraping = true;
            }else if (ctrl.isGraping) // 这个条件是 抓取完后 释放的一瞬间，生成新的mass（对facade的绝对位置坐标产生变化）
            {
                ctrl.isGraping = false;
                MainEnter.moveTargetMassID = masses.IndexOf(mass);//获取被抓取的mass id，以方便对其的材质进行更改
                ////调整facade的坐标
                //mass.m_faces.ForEach(face =>
                //{
                //    for (int i = 0; i < face.tops.Count; ++i)
                //    {
                //        face.tops[i] = mass.parentGo.transform.TransformPoint(face.tops[i]);
                //        face.bottoms[i] = mass.parentGo.transform.TransformPoint(face.bottoms[i]);
                //    }
                //});


                //mass.UpdateMass();//更新网格
                MassFacadeFactory.StitchMass(masses, mass);//检查拼接状况
                mass.UpdateController();
            }

            
        });
    }

    public bool DoSketchStroke()
    {
        if (!isAllowDrawing)
        {
            return false;
        }

        float recordThreshold = 0.012f;

        frame = provider.CurrentFrame;
        foreach(Hand hand in frame.Hands)
        {
            if (hand.IsRight)
            {
                List<Finger> fingers = hand.Fingers;
                Finger thumb = fingers[0];//拇指
                Finger indexFinger = fingers[1]; //食指
                Vector3 vec3_ThumbToIndex = (indexFinger.TipPosition - thumb.TipPosition).ToVector3();

                if (vec3_ThumbToIndex.magnitude < 0.04f) //如果食指和拇指合起来则画线
                {
                    Vector3 midpos = (indexFinger.TipPosition + thumb.TipPosition).ToVector3() / 2.0f;

                    drawingStateSphere.transform.position = midpos;
                    drawingStateSphere.GetComponent<MeshRenderer>().enabled = true;

                    if (Vector3.Distance(lastPos, midpos) > recordThreshold)
                    {
                        m_strokeLineRenderer.positionCount += 1;
                        m_strokeLineRenderer.SetPosition(m_strokeLineRenderer.positionCount - 1, midpos);//设置顶点位置 

                        lastPos = midpos;
                    }

                }
                else
                {
                    if (m_strokeLineRenderer.positionCount != 0)
                    {
                        drawingStateSphere.GetComponent<MeshRenderer>().enabled = false;

                        strokeLineRenderers.Add(m_strokeLineRenderer);

                        /****************** 松手，如果有数据，保存上一条的数据 **********************/
                        m_strokeLineRenderer = ((GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Drawing/Stroke"))).GetComponent<LineRenderer>();
                        m_strokeLineRenderer.startWidth = m_strokeLineRenderer.endWidth = 0.002f;
                        m_strokeLineRenderer.positionCount = 0;
                    }

                }
            }
        }

        return true;
    }

    //private float samplerOffset = 0.03f;

    public Stroke GetLastStroke()
    {
        /*
         * 功能：对已经绘制的上一条线条 并采样，并以Stroke的形式 进行返回，便于building的制作。
         */
        Stroke stroke = new Stroke();

        if(strokeLineRenderers.Count == 0)
        {
            Debug.Log("ERROR:: GetLastStroke");

            return stroke;
        }

        LineRenderer line = strokeLineRenderers[strokeLineRenderers.Count - 1];
        stroke = Util.GetSamplerLineRenderer(line, 0.005f);


        ///******** 临时处理 返回freeform形式的stroke **********/
        ////stroke.type = StrokeType._FREEFORM;
        //stroke.type = StrokeType._RECTANGLE;
        ////stroke.type = StrokeType._CIRCLE;

        for (int i = 0; i < stroke.rawPts.Count; ++i)
        {
            stroke.rawLabels.Add(PointType._CURVE);
        }



        return stroke;
    }

    //public List<Stroke> GetLastTwoStrokes()
    //{
    //    /*
    //     * 功能：返回已经绘制的采样过的上两条stroke，并以Stroke的形式 进行返回，便于building的制作。
    //     *      其中，第一根stroke默认处理buliding的外形，第二根stroke确定building的高度
    //     */
    //    List<Stroke> strokes = new List<Stroke>();

    //    if (strokeLineRenderers.Count <= 1)
    //    {
    //        Debug.Log("ERROR:: GetLastTwoStroke");

    //        return strokes;
    //    }

    //    for (int i = 2; i >= 1; --i)
    //    {
    //        Stroke stroke = new Stroke();

    //        LineRenderer line = strokeLineRenderers[strokeLineRenderers.Count - i];
    //        stroke = Util.GetSamplerLineRenderer(line, 0.005f);

    //        strokes.Add(stroke);
    //    }


    //    /******** 默认生成线条的 rawlabel都是曲线 **********/
    //    //防止日后的一些函数可能会用到，rawlabel，所以先赋值好了
    //    strokes.ForEach(stroke =>
    //    {
    //        for (int i = 0; i < stroke.rawPts.Count; ++i)
    //        {
    //            stroke.rawLabels.Add(PointType._CURVE);
    //        }

    //    });

    //    return strokes;
    //}

    public bool GetAllStrokes(List<Stroke> strokes)
    {
        if (strokeLineRenderers.Count == 0)
        {
            Debug.Log("ERROR:: GetAllStrokes");
            return false;
        }

        strokeLineRenderers.ForEach(line =>
        {
            Stroke stroke = new Stroke();

            stroke = Util.GetSamplerLineRenderer(line, 0.005f);
            strokes.Add(stroke);

        });


        /******** 默认生成线条的 rawlabel都是曲线 **********/
        //防止日后的一些函数可能会用到，rawlabel，所以先赋值好了
        strokes.ForEach(stroke =>
        {
            for (int i = 0; i < stroke.rawPts.Count; ++i)
            {
                stroke.rawLabels.Add(PointType._CURVE);
            }

        });


        return true;
    }

    public List<Vector3> GetTcpStroke() 
    {
        /*
         * 功能：将已经绘制的上一条 stroke 使用Tcp传给网络进行类型识别
         * 实现：移动stroke至坐标原点，并放缩至单位尺寸
         */
        if (strokeLineRenderers.Count == 0)
        {
            Debug.Log("ERROR:: GetTcpStroke");
        }

        LineRenderer line = strokeLineRenderers[strokeLineRenderers.Count - 2];

        Stroke stroke = new Stroke();
        stroke = Util.GetSamplerLineRenderer(line, 0.005f);

        return Util.DoUnitStroke(stroke);

    }

    public void ButtonCancelDrawing()
    {
        /*
         * 功能：撤销上一条绘制的stroke
         */

        if (strokeLineRenderers.Count == 0)
            return;

        LineRenderer line = strokeLineRenderers[strokeLineRenderers.Count - 1];
        strokeLineRenderers.RemoveLast();

        GameObject.Destroy(line.transform.gameObject);//销毁lineRenderer组件所属的GameObject

        
    }


    public void ButtonControlDrawing()
    {
        /* 
         * 功能：控制 stroke drawing 绘制状态的开启
         */

        if (this.isAllowDrawing)
        {
            this.DefaultDrawing();
        }
        else
        {
            this.ActiveDrawing();
        }
    }

    private void ActiveDrawing()
    {
        this.isAllowDrawing = true;
        drawStrokeObj.GetComponent<MeshRenderer>().material.color = buttonActiveColor;
    }

    private void DefaultDrawing()
    {
        this.isAllowDrawing = false;
        drawStrokeObj.GetComponent<MeshRenderer>().material.color = buttonDefaultColor;
    }

    private void WarningDrawing()
    {
        /*
         * todo....暂时保留
         * 功能：这是一个警告动作，系统每画一个线条就会进入warning状态，
         *   该状态中，用户无法绘制新的线条，除非使用左手做出该动作，以解除warning状态。
         *   这是为了 防止LeapMotion对手势识别的不精确，以免在不需要画线时而画线。
         */
        this.isAllowDrawing = false;
        drawStrokeObj.GetComponent<MeshRenderer>().material.color = buttonWarningColor;
    }

    public void SaveLineRenderer()
    {
        /*
         * 用于提前画线 然后标定数据
         */
        string nowtime = Time.time.ToString();
        string path = "C:\\Users\\vcc\\Desktop\\line-2021-5-12.txt";


        if (File.Exists(path))
        {
            File.Delete(path);
        }
        FileStream fileStream = new FileStream(path, FileMode.Create);
        StreamWriter writer = new StreamWriter(fileStream);


        //写入zh 的所有线条


        for (int i = 0; i < this.strokeLineRenderers.Count; ++i)
        {
            Vector3[] poses = new Vector3[strokeLineRenderers[i].GetComponent<LineRenderer>().positionCount];
            this.strokeLineRenderers[i].GetComponent<LineRenderer>().GetPositions(poses);
            List<Vector3> line = poses.ToList();
            if (line.Count <= 6)
                continue;

            writer.Write("1 ");

            foreach (Vector3 pos in line)
            {
                writer.Write(pos.x + " " + pos.y + " " + pos.z + " ");
            }
            writer.Write("\n");
            writer.Write("\n");
        }


        writer.Close();
        fileStream.Close();
    }

}
