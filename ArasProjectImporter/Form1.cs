using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Aras.IOM;
using net.sf.mpxj.reader;
using net.sf.mpxj;

namespace ArasProjectImporter
{

    public partial class Form1 : Form
    {
        static HttpServerConnection _conn = null;
        static Innovator _inn = null;
        static string _password = Properties.Settings.Default.Password;
        static string _url = Properties.Settings.Default.Url;
        static string _user = Properties.Settings.Default.User;
        static string _db = Properties.Settings.Default.Database;
        static string _filename = Properties.Settings.Default.Filename;
        static bool _isTemplate = true;
        private static string _projectTitle;
        static ProjectFile _mpx = null;

        private static int _progressMax = 100;
        private static string _message = "";
        public static string _backgroundErrorMsg;

        private static List<ArasHelpers.WbsElement> _wbsOutput;
        static Dictionary<string, string> _importMap;

        private static string _innovProjectNumber = "";

        public Form1()
        {
            InitializeComponent();

            txtUser.Text = _user;
            txtUrl.Text = _url;
            cmbDatabase.Text = _db;
            lblFileName.Text = _filename;
            txtPassword.Text = _password;
            rbTemplate.Checked = _isTemplate;
            rbProject.Checked = !_isTemplate;
        }


        private void btnLogin_Click(object sender, EventArgs e)
        {
            var oldCursor = Cursor;
            Cursor = Cursors.WaitCursor;

            _user = txtUser.Text;
            _password = txtPassword.Text;
            _url = txtUrl.Text;
            _db = cmbDatabase.Text;

            //write config file
            SaveConfig();

            _conn = IomFactory.CreateHttpServerConnection(_url, _db, _user, _password);

            Item login_result = _conn.Login();

            Cursor = oldCursor;

            if (login_result.isError())
            {
                //if already connected the logout of previous connection
                if (_conn != null)
                {
                    _conn.Logout();
                }

                //get details of error
                string error_str = login_result.getErrorString();


                //Interpret message string  - remove header text before : symbol
                int pos = error_str.IndexOf(':') + 1;
                if (pos > 0)
                {
                    error_str = error_str.Substring(pos);
                }
                //If error contains keyword clean up message text
                if (error_str.Contains("Authentication"))
                {
                    error_str = "Invalid user or password";
                }

                if (error_str.Contains("Database"))
                {
                    error_str = "Database not available";
                }

                MessageBox.Show("Login failed!\r\n\r\n" + error_str, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblFileName.Enabled = true;
            btnLogin.Enabled = false;
            btnFileOpen.Enabled = true;
            btnLoad.Enabled = true;

            txtUrl.Enabled = false;
            txtUser.Enabled = false;
            txtPassword.Enabled = false;
            cmbDatabase.Enabled = false;
            btnGetDatabases.Enabled = false;

            //Get innovator object
            _inn = IomFactory.CreateInnovator(_conn);

        }


        private void btnFileOpen_Click(object sender, EventArgs e)
        {
            var s = openFileDialog1.ShowDialog();
            if (s == DialogResult.OK)
            {
                _filename = openFileDialog1.FileName;
                lblFileName.Text = _filename;
            }
            btnLoad.Enabled = true;
        }


        private void btnLoad_Click(object sender, EventArgs e)
        {
            ProjectReader reader = ProjectReaderUtility.getProjectReader(_filename);

            var oldCursor = Cursor;
            Cursor = Cursors.WaitCursor;

            _mpx = reader.read(_filename);

            Cursor = oldCursor;

            _projectTitle = System.IO.Path.GetFileName(_mpx.ProjectFilePath);

            btnFileOpen.Enabled = false;
            btnLoad.Enabled = false;
            lblFileName.Enabled = false;

            txtProjectName.Text = _projectTitle;

            ParseMppFile();

            rbProject.Enabled = true;
            rbTemplate.Enabled = true;
            btnResources.Enabled = true;
            btnImport.Enabled = true;
            txtProjectName.Enabled = true;
        }

        private void ParseMppFile()
        {
            try
            {
                _wbsOutput = new List<ArasHelpers.WbsElement>();
                var ahelper = new ArasHelpers();

                var tasks = _mpx.AllTasks.ToTaskList();
                var wbsRoot = _inn.getNewID();

                for (var i = 0; i < tasks.Count; i++)
                    i = ahelper.ParseTasks(_inn, ref _wbsOutput, tasks, i, wbsRoot);

                // parse dependencies.  has to be another iteration because dependencies can be forward references

                foreach (var wbs in _wbsOutput)
                {
                    var ele = wbs; // looks weird but needed to pass by reference
                    var origID = ele.OrigMppIndex;
                    var origTask = (from ts in tasks where ts.ID.intValue() == origID select ts).FirstOrDefault();
                    if (origTask != null)
                        ahelper.ParsePredecessors(_wbsOutput, origTask, ref ele);
                }


                // parse assignments
                foreach (var wbs in _wbsOutput)
                {
                    var ele = wbs; // looks weird but needed to pass by reference
                    var origID = ele.OrigMppIndex;
                    var origTask = (from ts in tasks where ts.ID.intValue() == origID select ts).FirstOrDefault();
                    if (origTask != null)
                        ahelper.ParseAssignments(_wbsOutput, origTask, ref ele);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("MPP Parser error:" + ex.Message, "Failed", MessageBoxButtons.OK);
                Application.Exit();
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            btnImport.Enabled = false;
            progressBar1.Visible = true;
            lblCount.Visible = true;
            Size = new Size(635, 300);

            rbProject.Enabled = false;
            rbTemplate.Enabled = false;
            _isTemplate = rbTemplate.Checked;

            backgroundWorker1.RunWorkerAsync();
        }


        private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar1.Maximum = _progressMax;
            progressBar1.Value = e.ProgressPercentage;
            lblMessages.Text = _message;
            lblCount.Text = e.ProgressPercentage.ToString() + " of " + _progressMax.ToString();
            this.Update();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            lblCount.Text = "";
            progressBar1.Value = 0;
            if (!string.IsNullOrEmpty(_backgroundErrorMsg))
            {
                lblMessages.Text = "Import Failed";
                MessageBox.Show("Import error:" + _backgroundErrorMsg, "Failed", MessageBoxButtons.OK);
            }
            else
            {
                lblMessages.Text = "Import Completed";
                MessageBox.Show("Import completed", "Done", MessageBoxButtons.OK);
            }
            Application.Exit();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            ProjectHeader header = _mpx.ProjectHeader;

            try
            {
                _progressMax = _wbsOutput.Count - 1;
                _message = "Creating WBS structure in Aras...";
                backgroundWorker1.ReportProgress(0);

                // create top WBS
                var wbsRoot = _inn.getNewID();
                var projectID = _inn.getNewID();

                CreateProject(wbsRoot, projectID, header);

                CreateWBS(wbsRoot);

                CreateAssignments();

                CreatePredecessors();

                if (!_isTemplate)
                {
                    _progressMax = _wbsOutput.Count - 1;
                    _message = "Scheduling project in Aras...";
                    backgroundWorker1.ReportProgress(0);

                    var sProj = _inn.newItem("Project");
                    sProj.setID(projectID);
                    var resProj = sProj.apply("Schedule Project");
                    if (resProj.isError())
                        throw new Exception("Error scheduling project :" +
                                            resProj.getErrorDetail());

                    backgroundWorker1.ReportProgress(_wbsOutput.Count - 1);
                    Thread.Sleep(300);
                }

            }
            catch (Exception ex)
            {
                _backgroundErrorMsg = ex.Message;
            }
        }

        private void CreateProject(string wbsRoot, string projectID, ProjectHeader header)
        {
            var topWbs = _inn.newItem();
            topWbs.setID(wbsRoot);
            topWbs.setType("WBS Element");
            topWbs.setProperty("name", _projectTitle);
            topWbs.setProperty("is_top", "1");
            topWbs.setAction("add");

            ArasCommit(topWbs, "Top WBS", null);

            var iProject = _inn.newItem();
            iProject.setID(projectID);

            if (!_isTemplate) // project
            {
                iProject.setType("Project");
                _innovProjectNumber = _inn.getNextSequence("Project Number");
                iProject.setProperty("date_start_target", ArasHelpers.ConvertDate(_inn, header.StartDate));
                iProject.setProperty("date_due_target", ArasHelpers.ConvertDate(_inn, header.FinishDate));
                iProject.setProperty("project_number", _innovProjectNumber);
                iProject.setProperty("scheduling_type", "Forward");
                iProject.setProperty("scheduling_method", "7DC85B0668134E949B9212D7CE199265");
                iProject.setProperty("update_method", "6E1133AB87A44D529DF5F9D1FD740100");
                iProject.setProperty("scheduling_mode", "1");
                iProject.setProperty("project_update_mode", "1");
            }
            else // template
            {
                iProject.setType("Project Template");
            }
            iProject.setProperty("name", _projectTitle);
            iProject.setProperty("wbs_id", wbsRoot);
            iProject.setAction("add");
            ArasCommit(iProject, "Top Project", null);
        }

        private void CreateWBS(string wbsRoot)
        {
            for (var indx = 0; indx < _wbsOutput.Count; indx++)
            {
                var wbs = _wbsOutput[indx];
                if (wbs is ArasHelpers.WbsActivityElement)
                {
                    var wbse = wbs as ArasHelpers.WbsActivityElement;
                    var id = wbse.ArasID;
                    Item newWBS = _inn.newItem("Activity2", "add");

                    newWBS.setID(id);

                    newWBS.setProperty("name", wbse.Name);
                    if (indx > 0)
                        newWBS.setProperty("prev_item", _wbsOutput[indx - 1].ArasID);
                    else
                        newWBS.setProperty("prev_item", wbsRoot);

                    if (!_isTemplate) // project
                        newWBS.setProperty("proj_num", _innovProjectNumber);

                    newWBS.setProperty("description", wbse.Description);

                    if (wbse.IsMilestone)
                    {
                        newWBS.setProperty("is_milestone", "1");
                        newWBS.setProperty("work_est", "0");
                        newWBS.setProperty("expected_duration", "0");
                    }
                    else
                    {
                        newWBS.setProperty("is_milestone", "0");
                        newWBS.setProperty("work_est", wbse.WorkEst.ToString());
                        newWBS.setProperty("expected_duration", wbse.ExpectedDuration.ToString());
                    }

                    ArasCommit(newWBS, indx.ToString() + " Item", null);

                    Item newRel = _inn.newItem("WBS Activity2", "add");

                    if (indx > 0)
                        newRel.setProperty("source_id", wbs.ParentMppID);
                    else
                        newRel.setProperty("source_id", wbsRoot);

                    newRel.setProperty("related_id", id);

                    ArasCommit(newRel, indx.ToString() + " Rel", null);
                }
                else // wbs (folder) only
                {
                    var id = wbs.ArasID;
                    var newWBS = _inn.newItem("WBS Element", "add");
                    newWBS.setID(id);

                    newWBS.setProperty("name", wbs.Name);
                    if (indx > 0)
                        newWBS.setProperty("prev_item", _wbsOutput[indx - 1].ArasID);
                    else
                        newWBS.setProperty("prev_item", wbsRoot);

                    ArasCommit(newWBS, indx.ToString() + " Item", null);

                    var newRel = _inn.newItem("Sub WBS", "add");
                    if (indx > 0)
                        newRel.setProperty("source_id", wbs.ParentMppID);
                    else
                        newRel.setProperty("source_id", wbsRoot);

                    newRel.setProperty("related_id", id);

                    ArasCommit(newRel, indx.ToString() + " Rel", null);
                }

                backgroundWorker1.ReportProgress(indx);
            }
        }

        private void CreatePredecessors()
        {
            _progressMax = _wbsOutput.Count - 1;
            _message = "Creating task predecessors in Aras...";
            backgroundWorker1.ReportProgress(0);

            for (var indx = 0; indx < _wbsOutput.Count; indx++)
            {
                var wbs = _wbsOutput[indx];
                //var id = wbs.ArasID;

                foreach (var pred in wbs.Preds)
                {
                    var newPred = _inn.newItem("Predecessor", "add");
                    newPred.setProperty("precedence_type", pred.PrecedenceType);
                    newPred.setProperty("lead_lag", pred.LeadLag.ToString());
                    newPred.setProperty("source_id", wbs.ArasID);
                    newPred.setProperty("related_id", pred.RelatedArasID);
                    ArasCommit(newPred, indx.ToString() + " Predecessor " + indx.ToString(), null);
                }
                backgroundWorker1.ReportProgress(indx);
            }
        }

        private string getArasResourceName(string mppName)
        {
            if ((_importMap != null) && (_importMap.ContainsKey(mppName)))
                return _importMap[mppName];

            return mppName;
        }

        private void CreateAssignments()
        {
// now create the assignments
            _progressMax = _wbsOutput.Count - 1;
            _message = "Creating task resource assignments in Aras...";
            backgroundWorker1.ReportProgress(0);

            for (var indx = 0; indx < _wbsOutput.Count; indx++)
            {
                var wbs = _wbsOutput[indx];
                if (wbs is ArasHelpers.WbsActivityElement)
                {
                    var wbse = wbs as ArasHelpers.WbsActivityElement;
                    if ((wbse.ResourceAssigns != null) && (wbse.ResourceAssigns.Count > 0))
                    {
                        foreach (var res in wbse.ResourceAssigns)
                        {
                            string assetID = null;
                            if (!_isTemplate)
                            {
                                // try to find the user
                                var usr = _inn.newItem("User", "get");
                                var alias = _inn.newItem("Alias", "get");
                                usr.setProperty("keyed_name", getArasResourceName(res.AssignmentResourceName));
                                usr.setAttribute("select", "id");
                                alias.setAttribute("select", "id,related_id");
                                usr.addRelationship(alias);
                                var result = usr.apply();
                                if (!result.isError())
                                    assetID =
                                        result.getRelationships("Alias").getItemByIndex(0).getProperty("related_id");
                            }

                            // create the assignment item
                            var assign = _inn.newItem("Activity2 Assignment", "add");
                            assign.setProperty("source_id", wbse.ArasID);
                            assign.setProperty("percent_load", res.AssignmentPrecentLoad.ToString());
                            assign.setProperty("work_est", res.AssignmentWorkEst.ToString());

                            if ((!_isTemplate) && (!string.IsNullOrEmpty(assetID)))
                            {
                                assign.setProperty("related_id", assetID);
                            }
                            else
                            {
                                assign.setProperty("role", getArasResourceName(res.AssignmentResourceName));
                            }

                            if (wbse.PercentComplete == 100)
                                ArasCommit(assign, indx.ToString() + " Asst", "Complete");
                            else
                                ArasCommit(assign, indx.ToString() + " Asst", null);
                        }

                        // adding assignments re-opens a task. check for complete
                        if (wbse.PercentComplete == 100)
                        {
                            var prom = _inn.newItem("Activity2", "promoteItem");
                            prom.setID(wbse.ArasID);
                            prom.setProperty("state", "Complete");
                            var result = prom.apply();
                            if (result.isError())
                            {
                                throw new Exception("Error completing task at " + indx.ToString() + " :" +
                                                    result.getErrorDetail());
                            }
                        }
                    }
                }
                backgroundWorker1.ReportProgress(indx);
            }
        }


        public void ArasCommit(Item itm, string rownum, string state)
        {
            var result = itm.apply();
            if (result.isError())
            {
                throw new Exception("Error at " + rownum + ". - " + result.getErrorDetail());
            }
            if (state != null)
            {
                if (result.getProperty("state") != state)
                {
                    var prom = _inn.newItem(itm.getType(), "promoteItem");
                    prom.setID(itm.getID());
                    prom.setProperty("state", state);
                    result = prom.apply();
                    if (result.isError())
                    {
                        throw new Exception("Error at " + rownum + ". - " + result.getErrorDetail());
                    }
                }
            }
        }

       
        private void SaveConfig()
        {
            Properties.Settings.Default.User = txtUser.Text;
            Properties.Settings.Default.Password = txtPassword.Text;
            Properties.Settings.Default.Database = cmbDatabase.Text;
            Properties.Settings.Default.Url = txtUrl.Text;
            Properties.Settings.Default.Filename = lblFileName.Text;
            Properties.Settings.Default.IsTemplate = rbTemplate.Checked;
            Properties.Settings.Default.Save();
        }

        private void btnGetDatabases_Click(object sender, EventArgs e)
        {
            var oldCursor = Cursor;
            Cursor = Cursors.WaitCursor;

            cmbDatabase.Items.Clear();

            _url = txtUrl.Text;

            //get dbs from test connection
            try
            {
                _conn = IomFactory.CreateHttpServerConnection(_url, _db, _user, _password);
                string[] databases = _conn.GetDatabases();
                for (int i = 0; i < databases.Length; i++)
                {
                    cmbDatabase.Items.Add(databases[i]);
                }
                Cursor = oldCursor;
            }
            catch (Exception err)
            {
                Cursor = oldCursor;
                MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
            if (_conn != null)
            {
                _conn.Logout();
            }
        }

        private void btnResources_Click(object sender, EventArgs e)
        {
            var dlg = new ResourceMapper();
            dlg.Inn = _inn;
            dlg.IsTemplate = rbTemplate.Checked;

            // get all the resource names from MS Project and provide to Dlg
            var resourceNames = new List<string>();
            var tasks = _mpx.AllTasks.ToTaskList();

            foreach (var tsk in tasks)
            {
                foreach (ResourceAssignment res in tsk.ResourceAssignments.ToIEnumerable())
                {
                    if ((res.Resource != null) && (!string.IsNullOrEmpty(res.Resource.Name)))
                    {
                        if (!resourceNames.Contains(res.Resource.Name))
                        resourceNames.Add(res.Resource.Name);
                    }
                }
            }
            resourceNames.Sort();
            dlg.ResourceNamesMsProj = resourceNames;

            dlg.Init();

            var result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
                _importMap = dlg.ImportMap;
        }

        private void txtProjectName_TextChanged(object sender, EventArgs e)
        {
            _projectTitle = txtProjectName.Text;
        }

    }
}
