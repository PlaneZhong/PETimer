# PETimer
1.双端通用：基于C#语言实现的高效便捷计时器，可运行在服务器（.net core/.net framework）以及Unity客户端环境中。

2.功能丰富：PETimer支持帧数定时以及时间定时。定时任务可循环、可替换、可取消。可使用独立线程计时（自行设定检测间隔），也可以使用外部驱动计时，比如使用MonoBehaviour中的Update()函数来驱动。

3.集成简单：只有一个PETimer.cs文件，只需实例化一个PETimer类，对接相应的API，便能整合进自己的游戏框架，实现便捷高效的定时回调服务。

### 技术支持QQ:1785275942

# 使用示意：

### 1.Unity当中使用
``` C#
//实例化计时类
PETimer pt = new PETimer();
//时间定时任务
pt.AddTimeTask(TimerTask, 500, PETimeUnit.Millisecond, 3);
//帧数定时任务
pt.AddFrameTask(FrameTask, 100, 3);

int tempID = pt.AddTimeTask((int tid) => {
    Debug.Log("定时等待替换......");
}, 1, PETimeUnit.Second, 0);

//定时任务替换
pt.ReplaceTimeTask(tempID, (int tid) => {
    Debug.Log("定时任务替换完成......");
}, 2, PETimeUnit.Second, 0);

//定时任务删除
pt.DeleteTimeTask(tempID);

//定时检测与处理由MonoBehaviour中的Update()函数来驱动
void Update() {
    pt.Update();
}
```

### 2.服务器中使用
第一种用法：运行线程检测并处理任务（类似于在Unity中使用）
``` C#
PETimer pt = new PETimer();
//必须在While循环中调用pt.Update()来驱动计时
while (true) {
    pt.Update();
}
```
第二种用法：独立线程检测并处理任务
``` C#
//在PETimer实例化时，传入检测间隔参数（单位毫秒）
PETimer pt = new PETimer(100);
```
关于定时任务的添加、替换、删除与Unity当中使用方法一致

### 3.可设置定时回调处理器
当定时任务的回调处理可通过设置处理Handle来覆盖默认的执行处理(一般用于独立线程计时)
``` C#
pt.SetHandle((Action<int> cb, int tid) => {
    //覆盖默认的回调处理
    //TODO
});
```

### 4.日志工具接口
通过SetLog(Action<string> log)接口，可以传入第三方的日志显示工具。（下面以Unity为例，实现在Unity编辑器控制台中输出日志信息）
``` C#
pt.SetLog((string info) => {
    Debug.Log("LogInfo:" + info);
});
```

### 5.其它常用API
``` C#
//获取本地DateTime
public DateTime GetLocalDate();
//获取年份
public int GetYear();
//获取月份
public int GetMonth();
//获取天数
public int GetDay();
//获取星期
public int GetWeek();
//获取自1970-1-1以来的毫秒总数
public double GetMillisecondsTime();
//获取当前时间字符串
public string GetLocalTimeStr();
```
