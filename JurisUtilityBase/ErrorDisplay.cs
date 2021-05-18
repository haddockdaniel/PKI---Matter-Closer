﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace JurisUtilityBase
{
    public partial class ErrorDisplay : Form
    {
        private List<ErrorLog> errorList = new List<ErrorLog>();
        public ErrorDisplay(List<ErrorLog> log)
        {
            InitializeComponent();
            errorList = log.ToList();
        }

        public void showErrors()
        {
            string allText = "";
            foreach (ErrorLog entry in errorList)
                allText = allText + entry.message;
            richTextBox1.Text = allText;

        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "";
            this.Close();

        }

        private void buttonPrint_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetText(richTextBox1.Text);
        }
    }
}
