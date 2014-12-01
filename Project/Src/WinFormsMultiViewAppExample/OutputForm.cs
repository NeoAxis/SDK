using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Engine.Utils;

namespace WinFormsMultiViewAppExample
{
	public partial class OutputForm : DockContent
	{
		public OutputForm()
		{
			InitializeComponent();
		}

		public void Clear()
		{
			richTextBox1.Text = "";
		}

		public void Print( string text )
		{
			richTextBox1.Text += text + "\r\n";
			richTextBox1.Select( richTextBox1.Text.Length, 0 );
			richTextBox1.ScrollToCaret();
		}

		private void richTextBox1_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			if( e.KeyCode == Keys.F4 && e.Control )
				Hide();
		}

		public RichTextBox RichTextBox
		{
			get { return richTextBox1; }
		}
	}
}

