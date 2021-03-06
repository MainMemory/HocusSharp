﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HocusText
{
    static class Program
    {
        internal static string[] Arguments { get; private set; }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Arguments = args;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
