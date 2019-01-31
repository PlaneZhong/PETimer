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
    private Timer srvTimer;
    private static readonly string lockTid = "lockTid";
    private static readonly string lockTmpTimeLst = "lockTmpTime";
    private static readonly string lockTmpFrameLst = "lockTmpFrame";

    private DateTime startDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    private int tid;
    private List<int> tidLst = new List<int>();
    private List<int> recTidLst = new List<int>();

    private double nowTime;
    private List<PETimeTask> tmpTimerLst = new List<PETimeTask>();
    private List<PETimeTask> taskTimerLst = new List<PETimeTask>();
    private List<int> tmpDelTimerLst = new List<int>();

    private int frameCounter = 0;
    private List<PEFrameTask> tmpFramerLst = new List<PEFrameTask>();
    private List<PEFrameTask> taskFramerLst = new List<PEFrameTask>();
    private List<int> tmpDelFrameLst = new List<int>();

    private static readonly string lockTmpDelTimeLst = "lockTmpDelTime";
    private static readonly string lockTmpDelFrameLst = "lockTmpDelFrame";
    #endregion

    #region Main Functions
    public PETimer(int interval = 0) {
        tidLst.Clear();
        recTidLst.Clear();
        tmpTimerLst.Clear();
        taskTimerLst.Clear();
        tmpFramerLst.Clear();
        taskFramerLst.Clear();

        if (interval != 0) {
            srvTimer = new Timer(interval) {
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

        if (recTidLst.Count > 0) {
            RecyleTid();
        }
    }
    private void DelTimeTask() {
        bool exist = false;
        for (int i = 0; i < taskTimerLst.Count; i++) {
            PETimeTask task = taskTimerLst[i];
            if (task.tid == tid) {
                taskTimerLst.RemoveAt(i);
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
            for (int i = 0; i < tmpTimerLst.Count; i++) {
                PETimeTask task = tmpTimerLst[i];
                if (task.tid == tid) {
                    tmpTimerLst.RemoveAt(i);
                    for (int j = 0; j < tidLst.Count; j++) {
                        if (tidLst[j] == tid) {
                            tidLst.RemoveAt(j);
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }
    private void DelFrameTask() {
        bool exist = false;
        for (int i = 0; i < taskFramerLst.Count; i++) {
            PEFrameTask task = taskFramerLst[i];
            if (task.tid == tid) {
                taskFramerLst.RemoveAt(i);
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
            for (int i = 0; i < tmpFramerLst.Count; i++) {
                PEFrameTask task = tmpFramerLst[i];
                if (task.tid == tid) {
                    tmpFramerLst.RemoveAt(i);
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
        return exist;
    }

    private void CheckTimeTask() {
        if (tmpTimerLst.Count > 0) {
            lock (lockTmpTimeLst) {
                for (int tmpIndex = 0; tmpIndex < tmpTimerLst.Count; tmpIndex++) {
                    taskTimerLst.Add(tmpTimerLst[tmpIndex]);
                }
                tmpTimerLst.Clear();
            }
        }

        nowTime = GetUTCMilliseconds();
        for (int index = 0; index < taskTimerLst.Count; index++) {
            PETimeTask task = taskTimerLst[index];
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
                        taskTimerLst.RemoveAt(index);
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
        if (tmpFramerLst.Count > 0) {
            lock (lockTmpFrameLst) {
                for (int tmpIndex = 0; tmpIndex < tmpFramerLst.Count; tmpIndex++) {
                    taskFramerLst.Add(tmpFramerLst[tmpIndex]);
                }
                tmpFramerLst.Clear();
            }
        }

        frameCounter += 1;
        for (int index = 0; index < taskFramerLst.Count; index++) {
            PEFrameTask task = taskFramerLst[index];
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
                        taskFramerLst.RemoveAt(index);
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
            tmpTimerLst.Add(new PETimeTask(tid, callback, nowTime + delay, delay, count));
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
        for (int i = 0; i < taskTimerLst.Count; i++) {
            if (taskTimerLst[i].tid == tid) {
                taskTimerLst[i] = newTask;
                isRep = true;
                break;
            }
        }
        if (!isRep) {
            for (int i = 0; i < tmpTimerLst.Count; i++) {
                if (tmpTimerLst[i].tid == tid) {
                    tmpTimerLst[i] = newTask;
                    isRep = true;
                    break;
                }
            }
        }

        return isRep;
    }
    public bool DeleteTimeTask(int tid) {
        lock () {
            tmpDelTimerLst.Add(tid);
        }

        bool exist = false;
        for (int i = 0; i < taskTimerLst.Count; i++) {
            PETimeTask task = taskTimerLst[i];
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
            for (int i = 0; i < tmpTimerLst.Count; i++) {
                PETimeTask task = tmpTimerLst[i];
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
        return exist;
    }
    #endregion

    #region FrameTask
    public int AddFrameTask(Action<int> callback, int frame, int count = 1) {
        int tid = GetTid();
        lock (lockTmpFrameLst) {
            tmpFramerLst.Add(new PEFrameTask(tid, callback, frameCounter + frame, frame, count));
        }
        tidLst.Add(tid);
        return tid;
    }
    public bool ReplaceFrameTask(int tid, Action<int> callback, int frame, int count = 1) {
        PEFrameTask newTask = new PEFrameTask(tid, callback, frameCounter + frame, frame, count);
        bool isRep = false;
        for (int i = 0; i < taskFramerLst.Count; i++) {
            if (taskFramerLst[i].tid == tid) {
                taskFramerLst[i] = newTask;
                isRep = true;
                break;
            }
        }
        if (!isRep) {
            for (int i = 0; i < tmpFramerLst.Count; i++) {
                if (tmpFramerLst[i].tid == tid) {
                    tmpFramerLst[i] = newTask;
                    isRep = true;
                    break;
                }
            }
        }

        return isRep;
    }
    public bool DeleteFrameTask(int tid) {
        bool exist = false;
        for (int i = 0; i < taskFramerLst.Count; i++) {
            PEFrameTask task = taskFramerLst[i];
            if (task.tid == tid) {
                taskFramerLst.RemoveAt(i);
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
            for (int i = 0; i < tmpFramerLst.Count; i++) {
                PEFrameTask task = tmpFramerLst[i];
                if (task.tid == tid) {
                    tmpFramerLst.RemoveAt(i);
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

        tmpTimerLst.Clear();
        taskTimerLst.Clear();

        tmpFramerLst.Clear();
        taskFramerLst.Clear();

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
