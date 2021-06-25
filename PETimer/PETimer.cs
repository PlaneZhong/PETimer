/****************************************************
	文件：PETimer.cs
	作者：Plane
	邮箱: 1785275942@qq.com
	日期：2019/01/24 8:26   	
	功能：计时器
*****************************************************/

using System;
using System.Collections.Generic;
using System.Timers;

public class PETimer {
    private Action<string> taskLog;
    private Action<Action<int>, int> taskHandle;
    private static readonly string lockTid = "lockTid";
    private DateTime startDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    private double nowTime;
    private Timer srvTimer;
    private int tid;
    private List<int> tidLst = new List<int>();
    private List<int> recTidLst = new List<int>();

    private static readonly string lockTime = "lockTime";
    private List<PETimeTask> tmpTimeLst = new List<PETimeTask>();
    private List<PETimeTask> taskTimeLst = new List<PETimeTask>();
    private List<int> tmpDelTimeLst = new List<int>();

    private int frameCounter;

    private static readonly string lockFrame = "lockFrame";
    private List<PEFrameTask> tmpFrameLst = new List<PEFrameTask>();
    private List<PEFrameTask> taskFrameLst = new List<PEFrameTask>();
    private List<int> tmpDelFrameLst = new List<int>();

    public PETimer(int interval = 0) {
        tidLst.Clear();
        recTidLst.Clear();

        tmpTimeLst.Clear();
        taskTimeLst.Clear();

        tmpFrameLst.Clear();
        taskFrameLst.Clear();

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

        DelTimeTask();
        DelFrameTask();

        if (recTidLst.Count > 0) {
            lock (lockTid) {
                RecycleTid();
            }
        }
    }
    private void DelTimeTask() {
        if (tmpDelTimeLst.Count > 0) {
            lock (lockTime) {
                for (int i = 0; i < tmpDelTimeLst.Count; i++) {
                    bool isDel = false;
                    int delTid = tmpDelTimeLst[i];
                    for (int j = 0; j < taskTimeLst.Count; j++) {
                        PETimeTask task = taskTimeLst[j];
                        if (task.tid == delTid) {
                            isDel = true;
                            taskTimeLst.RemoveAt(j);
                            recTidLst.Add(delTid);
                            //LogInfo("Del taskTimeLst ID:" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                            break;
                        }
                    }

                    if (isDel)
                        continue;

                    for (int j = 0; j < tmpTimeLst.Count; j++) {
                        PETimeTask task = tmpTimeLst[j];
                        if (task.tid == delTid) {
                            tmpTimeLst.RemoveAt(j);
                            recTidLst.Add(delTid);
                            //LogInfo("Del tmpTimeLst ID:" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                            break;
                        }
                    }
                }
                tmpDelTimeLst.Clear();
            }
        }
    }
    private void DelFrameTask() {
        if (tmpDelFrameLst.Count > 0) {
            lock (lockFrame) {
                for (int i = 0; i < tmpDelFrameLst.Count; i++) {
                    bool isDel = false;
                    int delTid = tmpDelFrameLst[i];
                    for (int j = 0; j < taskFrameLst.Count; j++) {
                        PEFrameTask task = taskFrameLst[j];
                        if (task.tid == delTid) {
                            isDel = true;
                            taskFrameLst.RemoveAt(j);
                            recTidLst.Add(delTid);
                            break;
                        }
                    }

                    if (isDel)
                        continue;

                    for (int j = 0; j < tmpFrameLst.Count; j++) {
                        PEFrameTask task = tmpFrameLst[j];
                        if (task.tid == delTid) {
                            tmpFrameLst.RemoveAt(j);
                            recTidLst.Add(delTid);
                            break;
                        }
                    }
                }
                tmpDelFrameLst.Clear();
            }
        }
    }
    private void CheckTimeTask() {
        if (tmpTimeLst.Count > 0) {
            lock (lockTime) {
                //加入缓存区中的定时任务
                for (int tmpIndex = 0; tmpIndex < tmpTimeLst.Count; tmpIndex++) {
                    taskTimeLst.Add(tmpTimeLst[tmpIndex]);
                }
                tmpTimeLst.Clear();
            }
        }

        //遍历检测任务是否达到条件
        nowTime = GetUTCMilliseconds();
        for (int index = 0; index < taskTimeLst.Count; index++) {
            PETimeTask task = taskTimeLst[index];
            if (nowTime.CompareTo(task.destTime) < 0) {
                continue;
            }
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

                //移除已经完成的任务
                if (task.count == 1) {
                    taskTimeLst.RemoveAt(index);
                    index--;
                    recTidLst.Add(task.tid);
                }
                else {
                    if (task.count != 0) {
                        task.count -= 1;
                    }
                    task.destTime += task.delay;
                }
            }
        }
    }
    private void CheckFrameTask() {
        if (tmpFrameLst.Count > 0) {
            lock (lockFrame) {
                //加入缓存区中的定时任务
                for (int tmpIndex = 0; tmpIndex < tmpFrameLst.Count; tmpIndex++) {
                    taskFrameLst.Add(tmpFrameLst[tmpIndex]);
                }
                tmpFrameLst.Clear();
            }
        }

        frameCounter += 1;
        //遍历检测任务是否达到条件
        for (int index = 0; index < taskFrameLst.Count; index++) {
            PEFrameTask task = taskFrameLst[index];
            if (frameCounter < task.destFrame) {
                continue;
            }
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

                //移除已经完成的任务
                if (task.count == 1) {
                    taskFrameLst.RemoveAt(index);
                    index--;
                    recTidLst.Add(task.tid);
                }
                else {
                    if (task.count != 0) {
                        task.count -= 1;
                    }
                    task.destFrame += task.delay;
                }
            }
        }
    }

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
                    LogInfo("Add Task TimeUnit Type Error...");
                    break;
            }
        }
        int tid = GetTid(); ;
        nowTime = GetUTCMilliseconds();
        lock (lockTime) {
            tmpTimeLst.Add(new PETimeTask(tid, callback, nowTime + delay, delay, count));
        }
        return tid;
    }
    public void DeleteTimeTask(int tid) {
        lock (lockTime) {
            tmpDelTimeLst.Add(tid);
            //LogInfo("TmpDel ID:" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
        }
        /*
         bool exist = false;

         for (int i = 0; i < taskTimeLst.Count; i++) {
             PETimeTask task = taskTimeLst[i];
             if (task.tid == tid) {
                 //taskTimeLst.RemoveAt(i);
                 for (int j = 0; j < tidLst.Count; j++) {
                     if (tidLst[j] == tid) {
                         //tidLst.RemoveAt(j);
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
                     //tmpTimeLst.RemoveAt(i);
                     for (int j = 0; j < tidLst.Count; j++) {
                         if (tidLst[j] == tid) {
                             //tidLst.RemoveAt(j);
                             break;
                         }
                     }
                     exist = true;
                     break;
                 }
             }
         }

         return exist;
         */
    }
    public bool ReplaceTimeTask(int tid, Action<int> callback, float delay, PETimeUnit timeUnit = PETimeUnit.Millisecond, int count = 1) {
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
                    LogInfo("Replace Task TimeUnit Type Error...");
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
    #endregion

    #region FrameTask
    public int AddFrameTask(Action<int> callback, int delay, int count = 1) {
        int tid = GetTid();
        lock (lockTime) {
            tmpFrameLst.Add(new PEFrameTask(tid, callback, frameCounter + delay, delay, count));
        }
        return tid;
    }
    public void DeleteFrameTask(int tid) {
        lock (lockFrame) {
            tmpDelFrameLst.Add(tid);
        }
        /*
        bool exist = false;

        for (int i = 0; i < taskFrameLst.Count; i++) {
            PEFrameTask task = taskFrameLst[i];
            if (task.tid == tid) {
                //taskFrameLst.RemoveAt(i);
                for (int j = 0; j < tidLst.Count; j++) {
                    if (tidLst[j] == tid) {
                        //tidLst.RemoveAt(j);
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
                    //tmpFrameLst.RemoveAt(i);
                    for (int j = 0; j < tidLst.Count; j++) {
                        if (tidLst[j] == tid) {
                            //tidLst.RemoveAt(j);
                            break;
                        }
                    }
                    exist = true;
                    break;
                }
            }
        }

        return exist;
        */
    }
    public bool ReplaceFrameTask(int tid, Action<int> callback, int delay, int count = 1) {
        PEFrameTask newTask = new PEFrameTask(tid, callback, frameCounter + delay, delay, count);

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
    #endregion

    public void SetLog(Action<string> log) {
        taskLog = log;
    }
    public void SetHandle(Action<Action<int>, int> handle) {
        taskHandle = handle;
    }

    public void Reset() {
        tid = 0;
        tidLst.Clear();
        recTidLst.Clear();

        tmpTimeLst.Clear();
        taskTimeLst.Clear();

        tmpFrameLst.Clear();
        taskFrameLst.Clear();

        taskLog = null;
        srvTimer.Stop();
    }

    public int GetYear() {
        return GetLocalDateTime().Year;
    }
    public int GetMonth() {
        return GetLocalDateTime().Month;
    }
    public int GetDay() {
        return GetLocalDateTime().Day;
    }
    public int GetWeek() {
        return (int)GetLocalDateTime().DayOfWeek;
    }
    public DateTime GetLocalDateTime() {
        DateTime dt = TimeZone.CurrentTimeZone.ToLocalTime(startDateTime.AddMilliseconds(nowTime));
        return dt;
    }
    public double GetMillisecondsTime() {
        return nowTime;
    }
    public string GetLocalTimeStr() {
        DateTime dt = GetLocalDateTime();
        string str = GetTimeStr(dt.Hour) + ":" + GetTimeStr(dt.Minute) + ":" + GetTimeStr(dt.Second);
        return str;
    }

    #region Tool Methonds
    private int GetTid() {
        lock (lockTid) {
            tid += 1;

            //安全代码，以防万一
            while (true) {
                if (tid == int.MaxValue) {
                    tid = 0;
                }

                bool used = false;
                for (int i = 0; i < tidLst.Count; i++) {
                    if (tid == tidLst[i]) {
                        used = true;
                        break;
                    }
                }
                if (!used) {
                    tidLst.Add(tid);
                    break;
                }
                else {
                    tid += 1;
                }
            }
        }

        return tid;
    }
    private void RecycleTid() {
        for (int i = 0; i < recTidLst.Count; i++) {
            int tid = recTidLst[i];

            for (int j = 0; j < tidLst.Count; j++) {
                if (tidLst[j] == tid) {
                    tidLst.RemoveAt(j);
                    break;
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

    class PETimeTask {
        public int tid;
        public Action<int> callback;
        public double destTime;//单位：毫秒
        public double delay;
        public int count;

        public PETimeTask(int tid, Action<int> callback, double destTime, double delay, int count) {
            this.tid = tid;
            this.callback = callback;
            this.destTime = destTime;
            this.delay = delay;
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
}

public enum PETimeUnit {
    Millisecond,
    Second,
    Minute,
    Hour,
    Day
}
