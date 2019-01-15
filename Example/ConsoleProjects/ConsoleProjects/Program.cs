/****************************************************
	文件：Program.cs
	作者：Plane
	邮箱: 1785275942@qq.com
	日期：2019/01/14 16:30   	
	功能：PETimer控制台工程案例代码
*****************************************************/

using System;
using System.Threading;
using System.Collections.Generic;

namespace ConsoleProjects {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Test Start!");
            //Test1();
            Test2();
        }

        //第一种用法：运行线程检测并处理任务
        static void Test1() {
            //运行线程驱动计时
            PETimer pt = new PETimer();
            pt.SetLog((string info) => {
                Console.WriteLine("LogInfo:" + info);
            });

            pt.AddTimeTask((int tid) => {
                Console.WriteLine("Process线程ID:{0}", Thread.CurrentThread.ManagedThreadId.ToString());
            }, 10, PETimeUnit.Millisecond, 0);

            while (true) {
                pt.Update();
            }
        }

        //第二种用法：独立线程检测并处理任务
        static void Test2() {
            Queue<TaskPack> tpQue = new Queue<TaskPack>();
            //独立线程驱动计时
            PETimer pt = new PETimer(5);
            pt.SetLog((string info) => {
                Console.WriteLine("LogInfo:" + info);
            });

            pt.AddTimeTask((int tid) => {
                Console.WriteLine("Process线程ID:{0}", Thread.CurrentThread.ManagedThreadId.ToString());
            }, 10, PETimeUnit.Millisecond, 0);

            //设置回调处理器
            pt.SetHandle((Action<int> cb, int tid) => {
                if (cb != null) {
                    tpQue.Enqueue(new TaskPack(tid, cb));
                }
            });
            while (true) {
                if (tpQue.Count > 0) {
                    TaskPack tp = tpQue.Dequeue();
                    tp.cb(tp.tid);
                }
            }
        }
    }

    //任务数据包
    class TaskPack {
        public int tid;
        public Action<int> cb;
        public TaskPack(int tid, Action<int> cb) {
            this.tid = tid;
            this.cb = cb;
        }
    }
}