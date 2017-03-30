namespace ArasProjectImporter
{
    partial class ResourceMapper
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lbNamesAML = new System.Windows.Forms.ListBox();
            this.lblRoleType = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbNamesProject = new System.Windows.Forms.ListBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.lbMapped = new System.Windows.Forms.ListBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnCreate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbNamesAML
            // 
            this.lbNamesAML.FormattingEnabled = true;
            this.lbNamesAML.Location = new System.Drawing.Point(135, 47);
            this.lbNamesAML.Name = "lbNamesAML";
            this.lbNamesAML.Size = new System.Drawing.Size(120, 212);
            this.lbNamesAML.TabIndex = 0;
            this.lbNamesAML.SelectedIndexChanged += new System.EventHandler(this.lbNamesAML_SelectedIndexChanged);
            // 
            // lblRoleType
            // 
            this.lblRoleType.AutoSize = true;
            this.lblRoleType.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRoleType.Location = new System.Drawing.Point(135, 13);
            this.lblRoleType.Name = "lblRoleType";
            this.lblRoleType.Size = new System.Drawing.Size(97, 13);
            this.lblRoleType.TabIndex = 1;
            this.lblRoleType.Text = "To Aras Identity";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(117, 35);
            this.label2.TabIndex = 2;
            this.label2.Text = "From MS Project Resource Name";
            // 
            // lbNamesProject
            // 
            this.lbNamesProject.FormattingEnabled = true;
            this.lbNamesProject.Location = new System.Drawing.Point(9, 47);
            this.lbNamesProject.Name = "lbNamesProject";
            this.lbNamesProject.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbNamesProject.Size = new System.Drawing.Size(120, 212);
            this.lbNamesProject.TabIndex = 3;
            this.lbNamesProject.SelectedIndexChanged += new System.EventHandler(this.lbNamesProject_SelectedIndexChanged);
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(262, 109);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 4;
            this.btnAdd.Text = "Add >>";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Enabled = false;
            this.btnRemove.Location = new System.Drawing.Point(383, 276);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(75, 23);
            this.btnRemove.TabIndex = 5;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // lbMapped
            // 
            this.lbMapped.FormattingEnabled = true;
            this.lbMapped.Location = new System.Drawing.Point(343, 47);
            this.lbMapped.Name = "lbMapped";
            this.lbMapped.Size = new System.Drawing.Size(158, 212);
            this.lbMapped.TabIndex = 6;
            this.lbMapped.SelectedIndexChanged += new System.EventHandler(this.lbMapped_SelectedIndexChanged);
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(178, 276);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 7;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(259, 276);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnCreate
            // 
            this.btnCreate.Enabled = false;
            this.btnCreate.Location = new System.Drawing.Point(9, 276);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(120, 23);
            this.btnCreate.TabIndex = 9;
            this.btnCreate.Text = "Create in Aras";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // ResourceMapper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(513, 313);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lbMapped);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.lbNamesProject);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblRoleType);
            this.Controls.Add(this.lbNamesAML);
            this.Name = "ResourceMapper";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Resource Mapper";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lbNamesAML;
        private System.Windows.Forms.Label lblRoleType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox lbNamesProject;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.ListBox lbMapped;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnCreate;
    }
}