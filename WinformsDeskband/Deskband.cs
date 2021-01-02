using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinformsDeskband
{
    [ComVisible(true)]
    [Guid("0F872327-7521-434B-8233-9AC87F37DB9B")]
    [CSDeskBand.CSDeskBandRegistration(Name = "iSearch (Winforms)", ShowDeskBand = false)]
    public class Deskband : CSDeskBand.CSDeskBandWin
    {
        private static Control _control;

        public Deskband()
        {
            Options.MinHorizontalSize = new Size(100, 30);
            _control = new MainWindow(this);
        }

        protected override Control Control => _control;
    }
}
