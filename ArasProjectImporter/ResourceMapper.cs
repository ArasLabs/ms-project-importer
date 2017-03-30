using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aras.IOM;

namespace ArasProjectImporter
{
    public partial class ResourceMapper : Form
    {
        public Innovator Inn { get; set; }
        public bool IsTemplate { get; set; }
        public List<string> IdentityNamesAML { get; set; }
        public List<string> ResourceNamesMsProj { get; set; }
        public Dictionary<string, string> ImportMap = new Dictionary<string, string>();

        public ResourceMapper()
        {
            InitializeComponent();
        }

        public void Init()
        {
            RefreshIndentiesAML();
            RefreshMppNames();
        }

        public void UpdateMap()
        {
            ImportMap = new Dictionary<string, string>();
            var seps = new [] {" -> "};
            foreach (var itm in lbMapped.Items)
            {
                var spl = itm.ToString().Split(seps, StringSplitOptions.RemoveEmptyEntries);
                ImportMap.Add(spl[0], spl[1]);
            }
        }

        private void RefreshMppNames()
        {
            lbNamesProject.Items.Clear();
            foreach (var s in ResourceNamesMsProj)
                if ((lbNamesAML.FindStringExact(s) == ListBox.NoMatches) && (!ImportMap.Keys.Contains(s)))
                    lbNamesProject.Items.Add(s);
        }

        private void RefreshIndentiesAML()
        {
            if (IsTemplate)
            {
                lblRoleType.Text = "Aras Roles";
                var roleNames = GetListRoleNames();
                IdentityNamesAML = roleNames;

                lbNamesAML.Items.Clear();
                foreach (var s in roleNames)
                    lbNamesAML.Items.Add(s);
            }
            else
            {
                btnCreate.Enabled = false; // don't create new users.
                lblRoleType.Text = "Aras Identities";
                var aliasNames = GetAliasIdentityNames();
                IdentityNamesAML = aliasNames;

                lbNamesAML.Items.Clear();
                foreach (var s in aliasNames)
                    lbNamesAML.Items.Add(s);
            }
        }

        private List<string> GetAliasIdentityNames()
        {
            var identityNames = new List<string>();
            var query = @"<AML>" +
                        "<Item action='get' type='Identity' select='keyed_name'>" +
                        "<and>" +
                        "<is_alias condition='eq'>1</is_alias>" +
                        "<classification condition='is null' />" +
                        "</and>" +
                        "</Item>" +
                        "</AML>";

            var itmsAML = Inn.applyAML(query);

            if (itmsAML != null)
            {
                var count = itmsAML.getItemCount();

                for (var i = 0; i < count; i++)
                {
                    var itm = itmsAML.getItemByIndex(i);
                    var name = itm.getProperty("keyed_name");
                    identityNames.Add(name);
                }
                identityNames.Sort();
            }
            return identityNames;
        }

        private List<string> GetListRoleNames()
        {
            var roleNames = new List<string>();
            var query = @"<AML>" +
                        "<Item action='get' type='List' select='name'>" +
                        "<keyed_name condition='eq'>Project Role</keyed_name>" +
                        "<Relationships>" +
                        "<Item action='get' type='Value' select='value' orderBy='value'>" +
                        "</Item>" +
                        "</Relationships>" +
                        "</Item>" +
                        "</AML>";

            var itmsAML = Inn.applyAML(query);

            if (itmsAML != null)
            {
                var count = itmsAML.getItemCount();
                if (1 == count)
                {
                    var rels = itmsAML.getRelationships();

                    for (var x = 0; x < rels.getItemCount(); x++)
                    {
                        var z = rels.getItemByIndex(x);
                        var name = z.getProperty("value");
                        roleNames.Add(name);
                    }
                    roleNames.Sort();
                }
            }
            return roleNames;
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (lbNamesProject.SelectedIndex != -1)
            {
                var selName = lbNamesProject.SelectedItem.ToString();
                if (!IdentityNamesAML.Contains(selName))
                {
                    if (IsTemplate)
                    {
                        var listRoles = Inn.newItem("List", "edit");
                        listRoles.setAttribute("where", "keyed_name='Project Role'");
                        var listValue = Inn.newItem("Value", "add");
                        listValue.setProperty("value", selName);
                        listRoles.addRelationship(listValue);
                        var itm = listRoles.apply();
                    }
                    //else - don't allow the creation of identities. they must be authorized (licensed) users.
                }

                RefreshIndentiesAML();
                RefreshMppNames();
            }
        }

        private void lbNamesProject_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnCreate.Enabled = false;
            if ((lbNamesProject.SelectedIndex != -1) && IsTemplate) // don't allow on-the-fly creation of alias identities
            {
                var projName = lbNamesProject.SelectedItem.ToString();
                if (ListBox.NoMatches == lbNamesAML.FindStringExact(projName))
                    btnCreate.Enabled = true; 
            }

            btnAdd.Enabled = ((lbNamesProject.SelectedIndex != -1) && (lbNamesAML.SelectedIndex != -1));
        }

        private void lbNamesAML_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAdd.Enabled = ((lbNamesProject.SelectedIndex != -1) && (lbNamesAML.SelectedIndex != -1));
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if ((lbNamesProject.SelectedIndex != -1) && (lbNamesAML.SelectedIndex != -1))
            {
                var amlName = lbNamesAML.SelectedItem.ToString();
                if (lbNamesProject.SelectedIndices.Count > 1)
                {
                    foreach (var itm in lbNamesProject.SelectedItems)
                        AddMap(itm.ToString(), amlName);
                }
                else
                {
                    var projName = lbNamesProject.SelectedItem.ToString();
                    AddMap(projName, amlName);
                }
                UpdateMap();
                RefreshMppNames();           
            }
       }

        private void AddMap(string projName, string amlName)
        {
            var newStr = projName + " -> " + amlName;
            if (ListBox.NoMatches == lbMapped.FindStringExact(newStr))
            {
                lbMapped.Items.Add(projName + " -> " + amlName);
            }
        }

        private void lbMapped_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemove.Enabled = (lbMapped.SelectedIndex != -1);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            lbMapped.Items.RemoveAt(lbMapped.SelectedIndex);
            UpdateMap();
            RefreshMppNames();
        }
    }
}
