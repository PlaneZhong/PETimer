/****************************************************
	文件：PETimer.cs
	作者：Plane
	邮箱: 1785275942@qq.com
	日期：2018/12/23 13:14   	
	功能：计时器(支持时间定时与帧定时)
*****************************************************/

using System;
using System.Timers;
using System.Collections.Generic;
using System.Threading;

public enum PETimeUnit {
    Millisecond,
    Second,
    Minute,
    Hour,
    Day
}

public class PETimer {
    class PETimeTask {
        public int tid;
        public Action<int> callback;
        public double destTime;
        public double value;
        public int count;

        public PETimeTask(int tid, Action<int> callback, double destTime, double value, int count) {
            this.tid = tid;
            this.callback = callback;
            this.destTime = destTime;
            this.value = value;
            this.count = count;
        }
    }
    class PEFrameTask {
        public int tid;
        public Action<int> callback;
        public int destFrame;
        public int delay;
        public int count;

        public PEFrameTask(int tid, Action<int> callback, int destFrame, int delay, int count) {
            this.tid = tid;
            this.callback = callback;
            this.destFrame = destFrame;
            this.delay = delay;
            this.count = count;
        }
    }
    #region Data Area
    private Action<string> taskLog;
    private Action<Action<int>, int> taskHandle;
    private System.Timers.Timer srvTimer;
    private static readonly string lockTid = "lockTid";

    private DateTime startDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    private int tid;
    private List<int> tidLst = new List<int>();
    private List<int> recTidLst = new List<int>();

    private double nowTime;

    private static readonly string lockTmpTimeLst = "lockTmpTime";
    private List<PETimeTask> tmpTimeLst = new List<PETimeTask>();
    private List<PETimeTask> taskTimeLst = new List<PETimeTask>();
    private static readonly string lockTmpDelTimeLst = "lockTmpDelTime";
    private List<int> tmpDelTimeLst = new List<int>();

    private int frameCounter = 0;

    private static readonly string lockTmpFrameLst = "lockTmpFrame";
    private List<PEFrameTask> tmpFrameLst = new List<PEFrameTask>();
    private List<PEFrameTask> taskFrameLst = new List<PEFrameTask>();
    private static readonly string lockTmpDelFrameLst = "lockTmpDelFrame";
    private List<int> tmpDelFrameLst = new List<int>();

    #endregion

    #region Main Functions
    public PETimer(int interval = 0) {
        tidLst.Clear();
        recTidLst.Clear();
        tmpTimeLst.Clear();
        taskTimeLst.Clear();
        tmpFrameLst.Clear();
        taskFrameLst.Clear();

        if (interval != 0) {
            srvTimer = new System.Timers.Timer(interval) {
                AutoReset = true
            };
            srvTimer.Elapsed += (object sender, ElapsedEventArgs args) => {
                Update();
            };
            srvTimer.Start();
        }
    }
    public void Update() {
        CheckTimeTask();
        CheckFrameTask();

        DelTimeTask();
        DelFrameTask();

        if (recTidLst.Count > 0) {
            RecyleTid();
        }
    }
    private void DelTimeTask() {
        if (tmpDelTimeLst.Count > 0) {
            lock (lockTmpDelTimeLst) {
                for (int i = 0; i < tmpDelTimeLst.Count; i++) {
                    int tid = tmpDelTimeLst[i];
                    for (int j = 0; j < taskTimeLst.Count; j++) {
                        PETimeTask task = taskTimeLst[j];
                        if (task.tid == tid) {
                            taskTimeLst.RemoveAt(j);
                            Console.WriteLine("Del1线程ID:{0}", Thread.CurrentThread.ManagedThreadId.ToString());
                            recTidLst.Add(tid);
                            break;
                        }
                    }
                }

                for (int i = 0; i < tmpDelTimeLst.Count; i++) {
                    int tid = tmpDelTimeLst[i];
                    for (int j = 0; j < tmpTimeLst.Count; j++) {
                        PETimeTask task = tmpTimeLst[j];
                        if (task.tid == tid) {
                            tmpTimeLst.RemoveAt(j);
                            Console.WriteLine("Del2线程ID:{0}", Thread.CurrentThread.ManagedThreadId.ToString());
                            recTidLst.Add(tid);
                            break;
                        }
                    }
                }
            }
        }
    }
    private void DelFrameTask() {
        if (tmpDelFrameLst.Count > 0) {
            lock (lockTmpDelFrameLst) {
                for (int i = 0; i < tmpDelFrameLst.Count; i++) {
                    int tid = tmpDelFrameLst[i];
                    for (int j = 0; j < taskFrameLst.Count; j++) {
                        PEFrameTask task = taskFrameLst[j];
                        if (task.tid == tid) {
                            taskFrameLst.RemoveAt(j);
                            recTidLst.Add(tid);
                            break;
                        }
                    }
                }

                for (int i = 0; i < tmpDelFrameLst.Count; i++) {
                    int tid = tmpDelFrameLst[i];
                    for (int j = 0; j < tmpFrameLst.Count; j++) {
                        PEFrameTask task = tmpFrameLst[j];
                        if (task.tid == tid) {
                            tmpFrameLst.RemoveAt(j);
                            recTidLst.Add(tid);
                            break;
                        }
                    }
                }
            }
        }
    }

    private void CheckTimeTask() {
        if (tmpTimeLst.Count > 0) {
            lock (lockTmpTimeLst) {
                for (int tmpIndex = 0; tmpIndex < tmpTimeLst.Count; tmpIndex++) {
                    taskTimeLst.Add(tmpTimeLst[tmpIndex]);
                }
                tmpTimeLst.Clear();
            }
        }

        nowTime = GetUTCMilliseconds();
        for (int index = 0; index < taskTimeLst.Count; index++) {
            PETimeTask task = taskTimeLst[index];
            if (nowTime.CompareTo(task.destTime) < 0)
                continue;
            else {
                Action<int> cb = task.callback;
                try {
                    if (taskHandle != null) {
                        taskHandle(cb, task.tid);
                    }
                    else {
                        if (cb != null) {
                            cb(task.tid);
                        }
                    }
                }
                catch (Exception e) {
                    LogInfo(e.ToString());
                }
                if (task.count == 1) {
                    try {
                        taskTimeLst.RemoveAt(index);
                        index--;
                        recTidLst.Add(task.tid);
                    }
                    catch (Exception e) {
                        LogInfo(e.ToString());
                    }
                }
                else {
                    if (task.count != 0) {
                        task.count -= 1;
                    }
                    task.destTime = task.destTime + task.value;
                }
            }
        }
    }
    private void CheckFrameTask() {
        if (tmpFrameLst.Count > 0) {
            lock (lockTmpFrameLst) {
                for (int tmpIndex = 0; tmpIndex < tmpFrameLst.Count; tmpIndex++) {
                    taskFrameLst.Add(tmpFrameLst[tmpIndex]);
                }
                tmpFrameLst.Clear();
            }
        }

        frameCounter += 1;
        for (int index = 0; index < taskFrameLst.Count; index++) {
            PEFrameTask task = taskFrameLst[index];
            if (frameCounter.CompareTo(task.destFrame) < 0)
                continue;
            else {
                Action<int> cb = task.callback;
                try {
                    if (taskHandle != null) {
                        taskHandle(cb, task.tid);
                    }
                    else {
                        if (cb != null) {
                            cb(task.tid);
                        }
                    }
                }
                catch (Exception e) {
                    LogInfo(e.ToString());
                }
                if (task.count == 1) {
                    try {
                        taskFrameLst.RemoveAt(index);
                        index--;
                        recTidLst.Add(task.tid);
                    }
                    catch (Exception e) {
                        LogInfo(e.ToString());
                    }
                }
                else {
                    if (task.count != 0) {
                        task.count -= 1;
                    }
                    task.destFrame = task.destFrame + task.delay;
                }
            }
        }
    }
    #endregion

    #region Public API
    #region TimeTask
    public int AddTimeTask(Action<int> callback, double delay, PETimeUnit timeUnit = PETimeUnit.Millisecond, int count = 1) {
        if (timeUnit != PETimeUnit.Millisecond) {
            switch (timeUnit) {
                case PETimeUnit.Second:
                    delay = delay * 1000;
                    break;
                case PETimeUnit.Minute:
                    delay = delay * 1000 * 60;
                    break;
                case PETimeUnit.Hour:
                    delay = delay * 1000 * 60 * 60;
                    break;
                case PETimeUnit.Day:
                    delay = delay * 1000 * 60 * 60 * 24;
                    break;
                default:
                    LogInfo("add task timeunit type error...");
                    break;
            }
        }
        int tid = GetTid();
        nowTime = GetUTCMilliseconds();
        lock (lockTmpTimeLst) {
            tmpTimeLst.Add(new PETimeTask(tid, callback, nowTime + delay, delay, count));
        }
        return tid;
    }
    public bool ReplaceTimeTask(int tid, Action<int> callback, double delay, PETimeUnit timeUnit = PETimeUnit.Millisecond, int count = 1) {
        if (timeUnit != PETimeUnit.Millisecond) {
            switch (timeUnit) {
                case PETimeUnit.Second:
                    delay = delay * 1000;
                    break;
                case PETimeUnit.Minute:
                    delay = delay * 1000 * 60;
                    break;
                case PETimeUnit.Hour:
                    delay = delay * 1000 * 60 * 60;
                    break;
                case PETimeUnit.Day:
                    delay = delay * 1000 * 60 * 60 * 24;
                    break;
                default:
                    LogInfo("replace task timeunit type error...");
                    break;
            }
        }
        nowTime = GetUTCMilliseconds();
        PETimeTask newTask = new PETimeTask(tid, callback, nowTime + delay, delay, count);

        bool isRep = false;
        for (int i = 0; i < taskTimeLst.Count; i++) {
            if (taskTimeLst[i].tid == tid) {
                taskTimeLst[i] = newTask;
                isRep = true;
                break;
            }
        }
        if (!isRep) {
            for (int i = 0; i < tmpTimeLst.Count; i++) {
                if (tmpTimeLst[i].tid == tid) {
                    tmpTimeLst[i] = newTask;
                    isRep = true;
                    break;
                }
            }
        }

        return isRep;
    }
    public bool DeleteTimeTask(int tid) {
        lock (lockTmpDelTimeLst) {
            tmpDelTimeLst.Add(tid);
            Console.WriteLine("TmpDel线程ID:{0}", Thread.CurrentThread.ManagedThreadId.ToString());
        }

        bool exist = false;
        /*
        for (int i = 0; i < taskTimeLst.Count; i++) {
            PETimeTask task = taskTimeLst[i];
            if (task.tid == tid) {
                for (int j = 0; j < tidLst.Count; j++) {
                    if (tidLst[j] == tid) {
                        break;
                    }
                }
                exist = true;
                break;
            }
        }
        if (!exist) {
            for (int i = 0; i < tmpTimeLst.Count; i++) {
                PETimeTask task = tmpTimeLst[i];
                if (task.tid == tid) {
                    for (int j = 0; j < tidLst.Count; j++) {
                        if (tidLst[j] == tid) {
                            break;
                        }
                    }
                    exist = true;
                    break;
                }
            }
        }
        */
        return exist;
    }
    #endregion

    #region FrameTask
    public int AddFrameTask(Action<int> callback, int frame, int count = 1) {
        int tid = GetTid();
        lock (lockTmpFrameLst) {
            tmpFrameLst.Add(new PEFrameTask(tid, callback, frameCounter + frame, frame, count));
        }
        return tid;
    }
    public bool ReplaceFrameTask(int tid, Action<int> callback, int frame, int count = 1) {
        PEFrameTask newTask = new PEFrameTask(tid, callback, frameCounter + frame, frame, count);
        bool isRep = false;
        for (int i = 0; i < taskFrameLst.Count; i++) {
            if (taskFrameLst[i].tid == tid) {
                taskFrameLst[i] = newTask;
                isRep = true;
                break;
            }
        }
        if (!isRep) {
            for (int i = 0; i < tmpFrameLst.Count; i++) {
                if (tmpFrameLst[i].tid == tid) {
                    tmpFrameLst[i] = newTask;
                    isRep = true;
                    break;
                }
            }
        }

        return isRep;
    }
    public bool DeleteFrameTask(int tid) {
        lock (lockTmpDelFrameLst) {
            tmpDelFrameLst.Add(tid);
        }

        bool exist = false;
        /*
        for (int i = 0; i < taskFrameLst.Count; i++) {
            PEFrameTask task = taskFrameLst[i];
            if (task.tid == tid) {
                taskFrameLst.RemoveAt(i);
                for (int j = 0; j < tidLst.Count; j++) {
                    if (tidLst[j] == tid) {
                        tidLst.RemoveAt(j);
                        break;
                    }
                }
                exist = true;
                break;
            }
        }
        if (!exist) {
            for (int i = 0; i < tmpFrameLst.Count; i++) {
                PEFrameTask task = tmpFrameLst[i];
                if (task.tid == tid) {
                    tmpFrameLst.RemoveAt(i);
                    for (int j = 0; j < tidLst.Count; j++) {
                        if (tidLst[j] == tid) {
                            tidLst.RemoveAt(j);
                            break;
                        }
                    }
                    exist = true;
                    break;
                }
            }
        }
        */
        return exist;
    }
    #endregion

    public void SetLog(Action<string> log) {
        taskLog = log;
    }
    public void SetHandle(Action<Action<int>, int> handle) {
        taskHandle = handle;
    }
    public void Reset() {
        tidLst.Clear();
        recTidLst.Clear();

        tmpTimeLst.Clear();
        taskTimeLst.Clear();

        tmpFrameLst.Clear();
        taskFrameLst.Clear();

        tid = 0;
        frameCounter = 0;
        taskLog = null;
        taskHandle = null;
        srvTimer.Stop();
    }

    public DateTime GetLocalDate() {
        //dt = DateTime.Now;//不使用Now属性，而是通过计算获取，利于调试
        DateTime dt = TimeZone.CurrentTimeZone.ToLocalTime(startDateTime.AddMilliseconds(nowTime));
        return dt;
    }
    public int GetYear() {
        return GetLocalDate().Year;
    }
    public int GetMonth() {
        return GetLocalDate().Month;
    }
    public int GetDay() {
        return GetLocalDate().Day;
    }
    public int GetWeek() {
        return (int)GetLocalDate().DayOfWeek;
    }
    public double GetMillisecondsTime() {
        return nowTime;
    }
    public string GetLocalTimeStr() {
        DateTime dt = GetLocalDate();
        string str = GetTimeStr(dt.Hour) + ":" + GetTimeStr(dt.Minute) + ":" + GetTimeStr(dt.Second);
        return str;
    }
    #endregion

    #region Tool Methonds
    private int GetTid() {
        lock (lockTid) {
            tid += 1;

            //安全代码，以防万一
            if (tid == int.MaxValue) {
                while (true) {
                    bool used = false;
                    for (int i = 0; i < tidLst.Count; i++) {
                        if (tid == tidLst[i]) {
                            used = true;
                            break;
                        }
                    }
                    if (!used) {
                        break;
                    }
                }
                tid = 0;
            }
            tidLst.Add(tid);
        }
        return tid;
    }
    private void RecyleTid() {
        for (int i = 0; i < recTidLst.Count; i++) {
            int tid = recTidLst[i];
            lock (lockTid) {
                for (int j = 0; j < tidLst.Count; j++) {
                    if (tidLst[j] == tid) {
                        tidLst.RemoveAt(j);
                        break;
                    }
                }
            }
        }
        recTidLst.Clear();
    }

    private void LogInfo(string info) {
        if (taskLog != null) {
            taskLog(info);
        }
    }
    private double GetUTCMilliseconds() {
        TimeSpan ts = DateTime.UtcNow - startDateTime;
        return ts.TotalMilliseconds;
    }
    private string GetTimeStr(int time) {
        if (time < 10) {
            return "0" + time;
        }
        else {
            return time.ToString();
        }
    }
    #endregion
}