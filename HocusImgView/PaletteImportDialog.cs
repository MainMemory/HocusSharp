using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HocusImgView
{
    public partial class PaletteImportDialog : Form
    {
        public PaletteImportDialog()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        public int Index
        {
            get
            {
                return (int)numericUpDown1.Value;
            }
            set
            {
                numericUpDown1.Value = value;
            }
        }
    }
}