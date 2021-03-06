// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QueryTask.cs
//
// <OWNER>Microsoft</OWNER>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace System.Linq.Parallel
{
    // To disable exception marshaling (e.g. for debugging purposes), uncomment this symbol
    // or recompile PLINQ passing the symbol on the cmd-line, i.e. csc.exe ... /d:LET_...
    //#define LET_ASYNC_EXCEPTIONS_CRASH
    
    /// <summary>
    /// Simple abstract task representation, allowing either synchronous and asynchronous
    /// execution. Subclasses override the Work API to implement the logic.
    /// </summary>
    internal abstract class QueryTask
    {

        protected int m_taskIndex; // The unique id of this task.
        protected QueryTaskGroupState m_groupState; // State shared among the tasks.

        //-----------------------------------------------------------------------------------
        // Constructs a new task with the specified shared state.
        //

        protected QueryTask(int taskIndex, QueryTaskGroupState groupState)
        {
            Contract.Assert(groupState != null);
            m_taskIndex = taskIndex;
            m_groupState = groupState;
        }

        //-----------------------------------------------------------------------------------
        // A static function used by s_runTaskSynchronouslyDelegate, which is used by RunSynchronously
        //

        private static void RunTaskSynchronously(object o) 
        { 
            ((QueryTask)o).BaseWork(null);
        }

        //-----------------------------------------------------------------------------------
        // A static delegate used by RunSynchronously
        //

        private static Action<object> s_runTaskSynchronouslyDelegate = RunTaskSynchronously;

        //-----------------------------------------------------------------------------------
        // Executes the task synchronously (on the current thread).
        //

        internal Task RunSynchronously(TaskScheduler taskScheduler)
        {
            Contract.Assert(taskScheduler == TaskScheduler.Default, "PLINQ queries can currently execute only on the default scheduler.");
            TraceHelpers.TraceInfo("[timing]: {0}: Running work synchronously", DateTime.Now.Ticks, m_taskIndex);
            Task task = new Task(s_runTaskSynchronouslyDelegate, this, TaskCreationOptions.AttachedToParent);
            task.RunSynchronously(taskScheduler);
            return task;
        }

        //-----------------------------------------------------------------------------------
        // Executes the task asynchronously (elsewhere, unspecified).
        //

        private static Action<object> s_baseWorkDelegate = delegate(object o)
        {
            ((QueryTask)o).BaseWork(null);
        };

        internal Task RunAsynchronously(TaskScheduler taskScheduler)
        {
            Contract.Assert(taskScheduler == TaskScheduler.Default, "PLINQ queries can currently execute only on the default scheduler.");

            TraceHelpers.TraceInfo("[timing]: {0}: Queue work {1} to occur asynchronously", DateTime.Now.Ticks, m_taskIndex);
            return Task.Factory.StartNew(s_baseWorkDelegate, this, new CancellationToken(), TaskCreationOptions.AttachedToParent | TaskCreationOptions.PreferFairness, taskScheduler);
        }

        //-----------------------------------------------------------------------------------
        // Common function called regardless of sync or async execution.  Just wraps some
        // amount of tracing around the call to the real work API.
        //

        private void BaseWork(object unused)
        {
            Contract.Assert(unused == null);
            TraceHelpers.TraceInfo("[timing]: {0}: Start work {1}", DateTime.Now.Ticks, m_taskIndex);

#if !FEATURE_PAL && !SILVERLIGHT    // PAL doesn't support  eventing
            PlinqEtwProvider.Log.ParallelQueryFork(m_groupState.QueryId);
#endif

            try
            {
                Work();
            }
            finally
            {

#if !FEATURE_PAL && !SILVERLIGHT    // PAL doesn't support  eventing
                PlinqEtwProvider.Log.ParallelQueryJoin(m_groupState.QueryId);
#endif

            }

            TraceHelpers.TraceInfo("[timing]: {0}: End work {1}", DateTime.Now.Ticks, m_taskIndex);
        }

        //-----------------------------------------------------------------------------------
        // API that subclasses override to provide task-specific logic.
        //

        protected abstract void Work();

    }
}
