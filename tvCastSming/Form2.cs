using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace tvCastSming
{
    public partial class Form2 : Form
    {
        private Form1 m_parent;
        public Form2()
        {
            InitializeComponent();
        }
        public Form2(Form1 parent)
        {
            InitializeComponent();
            m_parent = parent;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
            m_parent.CallHideMainProcess(0);
            m_parent.StartProcess();
        }
    }
}
