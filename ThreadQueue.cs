using System;
using System.Collections.Generic;
using System.Threading;

namespace Eric.Comminucate
{
    public class ThreadQueue
    { 
        bool IsActing;
       
        Queue<ThreadParams> threadPool;
        Thread publicThread;
        public ThreadQueue()
        { 
            IsActing = true;
            threadPool = new Queue<ThreadParams>(); 
            publicThread = new Thread(new ThreadStart(Run));//每个socket接入后启动的操控主线程
            publicThread.Start();
        }
        internal class ThreadParams
        {
            public Action<object> Action { get; set; }
            public object Parameter { get; set; }
        }
        private void Run()
        {
            while (true)
            {
                Thread.Sleep(100);
                    if (threadPool.Count > 0)
                        EnqueueThreadItem(threadPool.Dequeue());
            }
        }
        public void PutInNewItem(Action<object> action, object o)
        {
            ThreadParams param = new ThreadParams() { Action = action, Parameter = o };
            threadPool.Enqueue(param); 
        }
        private void EnqueueThreadItem(object threadParams)
        {
            ThreadParams param = threadParams as ThreadParams;
            //param.Action(param.Parameter);
            ThreadPool.QueueUserWorkItem(new WaitCallback(param.Action), param.Parameter);//由于客户端与服务端数据交互较频繁，用线程池进行每次通信任务的处理
            //Thread thread = new Thread(new ParameterizedThreadStart(param.Action));
            //thread.Start(param.Parameter);
            //thread.Join();
        }
    }
}
