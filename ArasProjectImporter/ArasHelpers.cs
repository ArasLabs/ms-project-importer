using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Aras.IOM;
using net.sf.mpxj;
using net.sf.mpxj.planner.schema;
using Task = net.sf.mpxj.Task;
using System.Threading;

//using java.util;

namespace ArasProjectImporter
{
    internal class ArasHelpers
    {
        // In Aras, there are WBS (folder), milestone, and activity types
        // This is for WBS (folder).  WbsActivityElement (milestone or activity) is a derived class
        public class WbsElement
        {
            public String ArasID;
            public string Name;
            public int OrigMppIndex;
            public String ParentMppID;
            public string SummaryMilestoneID;
            public List<WbsPredecessor> Preds;
        }

        public class WbsActivityElement : WbsElement
        {
            public string Description;
            public double WorkEst;
            public DateTime DateStart;
            public DateTime DateDueTarget;
            public double ExpectedDuration;
            public decimal PercentComplete;
            public bool IsMilestone;
            public bool IsSummaryMilestone;
            public List<WbsResourceAssignment> ResourceAssigns;
        }

        public class WbsPredecessor
        {
            public string PrecedenceType;
            public double LeadLag;
            public string RelatedArasID;
        }

        public class WbsResourceAssignment
        {
            public string AssignmentResourceName;
            public decimal AssignmentPrecentLoad;
            public double AssignmentWorkEst;            
        }

        //private class RollbackItem
        //{
        //    public string ItemType;
        //    public string ItemID;
        //}

        //private BackgroundWorker _backgroundWorker;

        //public ArasHelpers(BackgroundWorker backgroundWorker)
        //{
        //    _backgroundWorker = backgroundWorker;
        //}


        public static string ConvertDate(Innovator inn, java.util.Date msprojDate)
        {
            var cntx=inn.getI18NSessionContext();
            var dt = msprojDate.ToDateTime();
            //var dv = new DateTime(msprojDate.getYear(), msprojDate.getMonth(), msprojDate.getDay());
            return cntx.ConvertToNeutral(dt.ToShortDateString(),"date");
        }


        public int ParseTasks(Innovator inn, ref List<WbsElement> output, List<Task> tasks, int taskIndexIn, string parentID)
        {
            var taskIndex = taskIndexIn;
            var task = tasks[taskIndex];
            if (task.Summary)
            {
                // note this is recursive and can skip ahead on the index value returned
                taskIndex = ParseSummaryTasks(inn, ref output, tasks, taskIndex, parentID);
            }
            else // task or milestone
            {
                taskIndex = ParseTask(inn, ref output, tasks, taskIndex, parentID);
            }
            return taskIndex;
        }

        // note this is recursive and can skip ahead on the index value returned
        public int ParseSummaryTasks(Innovator inn, ref List<WbsElement> output, List<Task> tasks, int taskIndexIn, string parentID)
        {
            var taskIndex = taskIndexIn;
            var task = tasks[taskIndex];
            if (!task.Summary)
                throw new Exception("ParseSummaryTask Invalid param: Not a summary task");

            var wbse = new WbsElement
                {
                    ArasID = inn.getNewID(), 
                    Name = task.Name, 
                    ParentMppID = parentID,
                    OrigMppIndex = task.ID.intValue()
                };

            output.Add(wbse);

            ++taskIndex;

            foreach (Task child in task.ChildTasks.ToIEnumerable())
            {
                taskIndex = ParseTasks(inn, ref output, tasks, taskIndex, wbse.ArasID);
                //_backgroundWorker.ReportProgress(taskIndex);
            }

            // add a milestone to mark the end of this summary task
            var wbsMilestone = new WbsActivityElement
            {
                ArasID = inn.getNewID(),
                Name = task.Name + " completed",
                ParentMppID = wbse.ArasID,
                OrigMppIndex = task.ID.intValue(),
                Description = task.Notes,
                WorkEst = 0,
                DateStart = task.Finish.ToDateTime(),
                DateDueTarget = task.Finish.ToDateTime(),
                ExpectedDuration = 0,
                PercentComplete = 0,
                IsMilestone = true,
                IsSummaryMilestone = true
            };

            wbse.SummaryMilestoneID = wbsMilestone.ArasID;
            output.Add(wbsMilestone);

            //_backgroundWorker.ReportProgress(taskIndex);
            return taskIndex;
        }


        public int ParseTask(Innovator inn, ref List<WbsElement> output, List<Task> tasks, int taskIndex, string parentID)
        {
            var task = tasks[taskIndex];
            if (task.Summary)
                throw new Exception("ParseTask Invalid param: Is a summary task");

            var wbs = new WbsActivityElement
            {
                ArasID = inn.getNewID(),
                Name = task.Name,
                ParentMppID = parentID,
                OrigMppIndex = task.ID.intValue(),
                Description = task.Notes,
                //WorkEst = task.Milestone ? 0 : Math.Round(task.Work.Duration / 60, 0),
                WorkEst = task.Milestone ? 0 : Math.Round(task.Work.Duration, 0),
                DateStart = task.Milestone ? task.Finish.ToDateTime() : task.Start.ToDateTime(),
                DateDueTarget = task.Finish.ToDateTime(),
                ExpectedDuration = task.Milestone ? 0 : Math.Round(task.Duration.Duration, 0),
                PercentComplete = task.PercentageComplete.ToNullableDecimal() != null ? (decimal)task.PercentageComplete.ToNullableDecimal() : 0,
                IsMilestone = task.Milestone,
                IsSummaryMilestone = false
            };

            // dependencies can be forward so we have to resolve them in another iteration

            output.Add(wbs);
            //_backgroundWorker.ReportProgress(taskIndex + 1);
            return taskIndex + 1;
        }


        public void ParsePredecessors(List<WbsElement> output, Task origTask, ref WbsElement ele)
        {
            var preds = new List<WbsPredecessor>();

            // check if this is a newly created milestone
            var element = ele as WbsActivityElement;
            if (element != null)
            {
                if (element.IsSummaryMilestone)
                {
                    //Task prevNonSummary = null;
                    var milestoneIndx = output.Count - 1;

                    for (var pndx = milestoneIndx; pndx >= 0; --pndx)
                    {
                        var findThisTask = output[pndx];

                        if (findThisTask.ArasID == ele.ArasID)
                        {
                            --pndx; // skip the fake milestone
                            // find previous non-summary task
                            while (pndx >= 0)
                            {
                                var findNonSumTask = output[pndx];
                                if (findNonSumTask is WbsActivityElement)
                                {
                                    // found the one and only dependency
                                    var wbsPred = new WbsPredecessor
                                        {
                                            PrecedenceType = getPredType("FS"),
                                            LeadLag = 0,
                                            RelatedArasID = findNonSumTask.ArasID,
                                        };
                                    preds.Add(wbsPred);
                                    pndx = 0;
                                    break;
                                }
                                --pndx;
                            }

                        }
                    }
                }
                else // normal task
                {
                    foreach (Relation pred in origTask.Predecessors.ToIEnumerable())
                    {
                        var iPred = pred.TargetTask.ID.intValue();
                        var linkedWbs = (from o in output where o.OrigMppIndex == iPred select o).FirstOrDefault();
                        if (linkedWbs != null)
                        {
                            var wbsp = new WbsPredecessor
                            {
                                PrecedenceType = getPredType(pred.Type.toString()),
                                RelatedArasID = linkedWbs.SummaryMilestoneID != null ? linkedWbs.SummaryMilestoneID : linkedWbs.ArasID,
                                //LeadLag = Math.Round(pred.Lag.Duration / 60 / 8, 0)
                                LeadLag = Math.Round(pred.Lag.Duration, 0)
                            };
                            preds.Add(wbsp);
                        }
                    }                    
                }
            }
            else // WBS (folder)
            {
                foreach (Relation pred in origTask.Predecessors.ToIEnumerable())
                {
                    var iPred = pred.TargetTask.ID.intValue();
                    var linkedWbs = (from o in output where o.OrigMppIndex == iPred select o).FirstOrDefault();
                    if (linkedWbs != null)
                    {
                        var wbsp = new WbsPredecessor
                            {
                                PrecedenceType = getPredType(pred.Type.toString()),
                                //RelatedArasID = linkedWbs.ArasID,
                                RelatedArasID = linkedWbs.SummaryMilestoneID != null ? linkedWbs.SummaryMilestoneID : linkedWbs.ArasID,
                                //LeadLag = Math.Round(pred.Lag.Duration / 60 / 8, 0)
                                LeadLag = Math.Round(pred.Lag.Duration, 0)
                            };
                        preds.Add(wbsp);
                    }
                }
            }
            ele.Preds = preds;
        }


        public void ParseAssignments(List<WbsElement> output, Task origTask, ref WbsElement ele)
        {
            var resAssigns = new List<WbsResourceAssignment>();

            // check if this is a newly created milestone
            var element = ele as WbsActivityElement;
            if (element != null)
            {
                if (!element.IsSummaryMilestone)
                {
                    foreach (ResourceAssignment res in origTask.ResourceAssignments.ToIEnumerable())
                    {
                        try
                        {
                            if (res.Resource != null)
                            {
                                var wbsa = new WbsResourceAssignment
                                    {
                                        AssignmentResourceName = res.Resource.Name,
                                        AssignmentPrecentLoad =
                                            res.PercentageWorkComplete == null ? 0 : Math.Round((decimal)res.PercentageWorkComplete.ToNullableDecimal(), 0),
                                        AssignmentWorkEst =
                                            res.ActualWork != null ? Math.Round(res.ActualWork.Duration, 0) : 0
                                    };
                                resAssigns.Add(wbsa);
                            }

                        }
                        catch (Exception ex)
                        {
                            throw;
                        }

                    }
                    element.ResourceAssigns = resAssigns;
                }
            }
        }


        private string getPredType(string predType)
        {
            switch (predType)
            {
                case "FF":
                    return "Finish to Finish";
                case "FS":
                    return "Finish to Start";
                case "SF":
                    return "Start to Finish";
                case "SS":
                    return "Start to Start";
            }
            return "";
        }
    }
}
