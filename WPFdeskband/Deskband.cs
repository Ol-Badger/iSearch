using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CSDeskBand;
using CSDeskBand.ContextMenu;

namespace iSearchWPF
{
    [ComVisible(true)]
    [Guid("02AF4F83-AB00-4D1A-A248-1A4691F2616D")]
    [CSDeskBandRegistration(Name = "iSearch (WPF)")]
    public class Deskband : CSDeskBandWpf
    {
        public Deskband()
        {
            //Options.ContextMenuItems = ContextMenuItems;
        }
        protected override UIElement UIElement => new MainWindow();

        /*
        private List<DeskBandMenuItem> ContextMenuItems
        {
            get
            {
                var action = new DeskBandMenuAction("Action - Toggle submenu");
                var separator = new DeskBandMenuSeparator();
                var submenuAction = new DeskBandMenuAction("Submenu Action - Toggle checkmark");
                var submenu = new DeskBandMenu("Submenu")
                {
                    Items = { submenuAction }
                };

                action.Clicked += (sender, args) => submenu.Enabled = !submenu.Enabled;
                submenuAction.Clicked += (sender, args) => submenuAction.Checked = !submenuAction.Checked;

                return new List<DeskBandMenuItem>() { action, separator, submenu };
            }
        }
        */
    }
}
