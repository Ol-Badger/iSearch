using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using csLib;

namespace iSearch
{
	public partial class MainWindow
	{
		private bool SearchUnavailable;
		private readonly System.Timers.Timer keyTimer;
		private aSearch search;
		public MainWindow()
		{
			InitializeComponent();

			DispatcherTimer minuteTimer = new DispatcherTimer();
			minuteTimer.Tick += minuteTimer_Tick;
			minuteTimer.Interval = TimeSpan.FromMinutes(1.0);
			minuteTimer.Start();

			//keyTimer exists to prevent multiple searches within [interval]
			keyTimer = new System.Timers.Timer(100) {AutoReset = false};
			keyTimer.Elapsed += keyTimer_Elapsed;

			ShowTime();
			search = new aSearch();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			SetPosition();

			//following code (along with code in Window styles region) inhibits showing in task switcher
			WindowInteropHelper wndHelper = new WindowInteropHelper(this);
			int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
			exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
			SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
		}

		private void SetPosition()
		{
			int offsetHeight = search.pp.ReadInt("Options", "OffsetHeight", 0);
			int offsetWidth = search.pp.ReadInt("Options", "OffsetWidth", 0);

			Left = (int)SystemParameters.PrimaryScreenWidth - offsetWidth;
			Top = (int)SystemParameters.PrimaryScreenHeight - offsetHeight;
		}

		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

		#region Context Menu
		private void OnExitClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void OnAboutClick(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Version: " + AssemblyAccessors.AssemblyVersion, "iSearch");
		}

		private void OnHelpClick(object sender, RoutedEventArgs e)
		{
			Process.Start("iSearch.chm");
		}

		private void OnReloadClick(object sender, RoutedEventArgs e)
		{
			search = new aSearch();
			SetPosition();
		}
		#endregion

		#region Time/Timer
		private void minuteTimer_Tick(object sender, EventArgs e)
		{
			if (!tbSearchBox.IsKeyboardFocused)
				ShowTime();
		}

		private void keyTimer_Elapsed(object sendeer, EventArgs e)
		{
			SearchUnavailable = false;
		}

		private void ShowTime()
		{
			DateTime dt = DateTime.Now.ToLocalTime();
			tbSearchBox.Text = $"{dt.ToShortDateString()}  {dt.ToShortTimeString()}";
		}
		#endregion

		#region focus/click events
		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (SearchUnavailable)
				return;
			if (e.Key == Key.F1)
				Process.Start("iSearch.chm");
			if (e.Key != Key.Return) return;
			SearchUnavailable = true;
			keyTimer.Start();
			search.Search(tbSearchBox.Text, e.KeyboardDevice.IsKeyDown(Key.LeftCtrl));
		}

		private void tbSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Down || e.Key == Key.Up)
			{
				tbSearchBox.Text = search.GetStringFromHistory(e);
			}
		}

		private void tbSearchBox_MouseLeave(object sender, MouseEventArgs e)
		{
			ShowTime();
		}

		private void tbSearchBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				tbSearchBox.Text = "";
				//e.Handled = true;
			}
		}

		private void tbSearchBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			thePopup.IsOpen = true;
		}
		#endregion

		#region calendar
		private void OnDateClick(object sender, SelectionChangedEventArgs e)
		{
			thePopup.IsOpen = false;
		}

		private void OnPopupClosed(object sender, EventArgs eventArgs)
		{
			ShowTime();
			//DateTime sel = theCalendar.SelectedDate ?? DateTime.Today;
			//textBox1.Text = sel.ToShortDateString();
		}

		/*
		private void OnCalendarMenuClick(object sender, RoutedEventArgs e)
		{
			thePopup.IsOpen = true;
		}
		 */
		#endregion

		#region Window styles
		//Following code found in
		//http://stackoverflow.com/questions/357076/best-way-to-hide-a-window-from-the-alt-tab-program-switcher/551847#551847
		//posting by Franci Penov 
		[Flags]
		private enum ExtendedWindowStyles
		{
			// ...
			WS_EX_TOOLWINDOW = 0x00000080,
			// ...
		}

		private enum GetWindowLongFields
		{
			// ...
			GWL_EXSTYLE = (-20),
			// ...
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

		private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
		{
			int error;
			IntPtr result /*= IntPtr.Zero*/;
			// Win32 SetWindowLong doesn't clear error on success
			SetLastError(0);

			if (IntPtr.Size == 4)
			{
				// use SetWindowLong
				Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
				error = Marshal.GetLastWin32Error();
				result = new IntPtr(tempResult);
			}
			else
			{
				// use SetWindowLongPtr
				result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
				error = Marshal.GetLastWin32Error();
			}

			if ((result == IntPtr.Zero) && (error != 0))
			{
				throw new System.ComponentModel.Win32Exception(error);
			}

			return result;
		}

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
		private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
		private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

		private static int IntPtrToInt32(IntPtr intPtr) => unchecked((int)intPtr.ToInt64());

		[DllImport("kernel32.dll", EntryPoint = "SetLastError")]
		private static extern void SetLastError(int dwErrorCode);
		#endregion
	}
}
