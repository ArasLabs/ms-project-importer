using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using net.sf.mpxj;

namespace ArasProjectImporter
{
    class MppHelpers
    {
        public static Task getLastTaskInSummary(Task summaryTask)
        {
            Task last = null;

            foreach (Task child in summaryTask.ChildTasks.ToIEnumerable())
            {
                last = child;
            }
            return last;
        }
    }
}
