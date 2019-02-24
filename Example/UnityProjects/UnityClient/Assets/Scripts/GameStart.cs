/****************************************************
	文件：GameStart.cs
	作者：Plane
	邮箱: 1785275942@qq.com
	日期：2019/01/15 2:19   	
	功能：PETimer集成到Unity案例代码
*****************************************************/

using UnityEngine;

public class GameStart : MonoBehaviour {
    PETimer pt = new PETimer();

    int tempID = -1;
    private void Start() {
        //时间定时
        pt.AddTimeTask(TimerTask, 500, PETimeUnit.Millisecond, 3);
        //帧数定时
        pt.AddFrameTask(FrameTask, 100, 3);

        //定时替换/删除
        tempID = pt.AddTimeTask((int tid) => {
            Debug.Log("定时等待替换......");
        }, 1, PETimeUnit.Second, 0);
    }

    private void Update() {
        pt.Update();

        //定时替换
        if (Input.GetKeyDown(KeyCode.R)) {

            bool succ = pt.ReplaceTimeTask(tempID, (int tid) => {
                Debug.Log("定时等待删除......");
            }, 2, PETimeUnit.Second, 0);

            if (succ) {
                Debug.Log("替换成功");
            }
        }

        //定时删除
        if (Input.GetKeyDown(KeyCode.D)) {
            pt.DeleteTimeTask(tempID);
        }
    }

    void TimerTask(int tid) {
        Debug.Log("TimeTask:" + System.DateTime.UtcNow);
    }

    void FrameTask(int tid) {
        Debug.Log("FrameTask:" + System.DateTime.UtcNow);
    }
}