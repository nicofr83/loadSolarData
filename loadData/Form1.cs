using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace loadData
{
    public partial class Form1 : Form
    {
        loadData ld = new loadData();

        public Form1()
        {
            InitializeComponent();
        }

        private void rdXml_CheckedChanged(object sender, EventArgs e)
        {
            if (rdXml.Checked)
                rdCsv.Checked = false;
            else
                rdCsv.Checked = true;
        }

        private void rdCsv_CheckedChanged(object sender, EventArgs e)
        {
            if (rdCsv.Checked)
                rdXml.Checked = false;
            else
                rdXml.Checked = true;
        }

        private void btBrowseDirs_Click(object sender, EventArgs e)
        {
            if (fBrowse.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txDirectoryRoot.Text = fBrowse.SelectedPath;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            rdXml.Checked = true;
        }

        private void btGo_Click(object sender, EventArgs e)
        {
            ld.run(txDirectoryRoot.Text, (rdXml.Checked) ? "xml" : "csv", lbResult);
        }
    }
}
