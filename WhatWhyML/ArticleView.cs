using IE.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IE
{
    public partial class ArticleView : Form
    {
        private List<TextBox> textBoxes = new List<TextBox>();
        private Article ar;
        private Annotation an;
        private Main m;
        private int[] i;

        public ArticleView(Main m, int[] i, Article ar, Annotation an)
        {
            InitializeComponent();

            this.Text = ar.Title;

            textBoxes.Add(textBox6);
            textBoxes.Add(textBox7);
            textBoxes.Add(textBox8);
            textBoxes.Add(textBox9);
            textBoxes.Add(textBox10);

            this.m = m;
            this.i = i;
            this.ar = ar;
            this.an = an;

            textBox1.Text = ar.Title;
            textBox2.Text = ar.Author;
            dateTimePicker1.Value = ar.Date;
            textBox4.Text = ar.Link;
            textBox5.Text = ar.Body;
            textBox6.Text = an.Who;
            textBox7.Text = an.When;
            textBox8.Text = an.Where;
            textBox9.Text = an.What;
            textBox10.Text = an.Why;
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if(btnEdit.Text.Equals("Edit"))
            {
                foreach (TextBox t in textBoxes)
                {
                    t.Enabled = true;
                }

                btnEdit.Text = "Save";
            }
            else
            {
                foreach (TextBox t in textBoxes)
                {
                    t.Enabled = false;
                }


                an.Who = textBox6.Text;
                an.When = textBox7.Text;
                an.Where = textBox8.Text;
                an.What = textBox9.Text;
                an.Why = textBox10.Text;

                m.saveChanges(i, an);

                btnEdit.Text = "Edit";
            }
        }
    }
}
