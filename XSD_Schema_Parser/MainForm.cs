using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using System.IO;

namespace XSD_Schema_Parser
{
    public partial class MainForm : Form
    {
       

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnParseXsd_Click( object sender, EventArgs e )
        {
            string strResult = "";
            txtResult.Text = "";

            if( string.IsNullOrEmpty( txtXsdSchema.Text ) )
                return;

            XsdParser xsdParser = new XsdParser();

            xsdParser.ParseXsd( txtXsdSchema.Text, ref strResult );

            txtResult.Text = strResult;
        }

        


    }
}
