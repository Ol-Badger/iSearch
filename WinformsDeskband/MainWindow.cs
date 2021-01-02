using System.Windows.Forms;

namespace WinformsDeskband
{
    public partial class MainWindow: UserControl
    {
        public MainWindow(CSDeskBand.CSDeskBandWin w)
        {
            InitializeComponent();
            tbSearchBox.GotFocus += (o, e) => w.UpdateFocus(true);
            
        }

        protected override bool ProcessKeyPreview(ref Message m)
        {
            if ((Keys)m.WParam == Keys.Tab)
            {
                var selected = SelectNextControl(ActiveControl, true, true, true, true);
                return true;
            }

            return base.ProcessKeyPreview(ref m);
        }

        private void tbSearchBox_MouseLeave(object sender, System.EventArgs e)
        {

        }

        private void tbSearchBox_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void tbSearchBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void tbSearchBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {

        }

        private void reloadiniToolStripMenuItem_Click(object sender, System.EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, System.EventArgs e)
        {

        }

        private void helpToolStripMenuItem_Click(object sender, System.EventArgs e)
        {

        }
    }
}
