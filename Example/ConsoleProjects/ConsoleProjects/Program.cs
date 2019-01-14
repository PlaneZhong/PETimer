/****************************************************
	文件：Program.cs
	作者：Plane
	邮箱: 1785275942@qq.com
	日期：2019/01/14 16:30   	
	功能：PETimer控制台工程案例代码
*****************************************************/

using System;

namespace ConsoleProjects {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Test Start!");
            //Test1();
            Test2();
        }

        //第一种用法：单线程添加任务，单线程处理任务
        static void Test1() {
            PETimer pt = new PETimer();
            pt.SetLog((string info) => {
                Console.WriteLine("LogInfo:" + info);
            });

            pt.AddTimeTask((int tid) => {
                Console.WriteLine("UTCLocalNow:" + DateTime.Now);
            }, 2, PETimeUnit.Second, 5);

            while (true) {
                pt.Update();
            }
        }

        //第二种用法：多线程
        static void Test2() {
            PETimer pt = new PETimer(10);
            pt.SetLog((string info) => {
                Console.WriteLine("LogInfo:" + info);
            });

            pt.AddTimeTask((int tid) => {
                Console.WriteLine("UTCLocalNow:" + DateTime.Now);
            }, 2, PETimeUnit.Second, 5);

            while (true) {
            }
        }
    }
}
