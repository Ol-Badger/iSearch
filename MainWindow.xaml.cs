using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using iSearch;

namespace iSearch
{
	public partial class MainWindow
	{
		private bool SearchUnavailable;
		private readonly System.Timers.Timer keyTimer;
		public MainWindow()
		{
			InitializeComponent();

			DispatcherTimer minuteTimer = new DispatcherTimer();
			minuteTimer.Tick += minuteTimer_Tick;
			minuteTimer.Interval = new TimeSpan(0, 1, 0);
			minuteTimer.Start();

			//keyTimer exists to prevent multiple searches within [interval]
			keyTimer = new System.Timers.Timer(100) {AutoReset = false};
			keyTimer.Elapsed += keyTimer_Elapsed;

			ShowTime();
		}

		private void OnExitClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			int offsetHeight = 70;
			int offsetWidth = (int)Width + 0;
			Left = (int)SystemParameters.PrimaryScreenWidth - offsetWidth;
			Top = (int)SystemParameters.PrimaryScreenHeight - offsetHeight;

			//following code (along with code at bottom of file) inhibits showing in task switcher
			WindowInteropHelper wndHelper = new WindowInteropHelper(this);
			int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
			exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
			SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
		}
		
		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (SearchUnavailable)
				return;
			if (e.Key != Key.Return) return;
			SearchUnavailable = true;
			keyTimer.Start();
			aSearch.Search(tbSearchBox.Text, e.KeyboardDevice.IsKeyDown(Key.LeftCtrl));
		}

		private void tbSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Down || e.Key == Key.Up)
			{
				tbSearchBox.Text = aSearch.GetStringFromHistory(e);
			}
		}

		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

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
			tbSearchBox.Text = String.Format("{0}  {1}", dt.ToShortDateString(), dt.ToShortTimeString());
		}

		#region focus events
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
		#endregion

		private void tbSearchBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			thePopup.IsOpen = true;
		}


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


		//Following code found in
		//http://stackoverflow.com/questions/357076/best-way-to-hide-a-window-from-the-alt-tab-program-switcher/551847#551847
		//posting by Franci Penov 

		#region Window styles
		[Flags]
		public enum ExtendedWindowStyles
		{
			// ...
			WS_EX_TOOLWINDOW = 0x00000080,
			// ...
		}

		public enum GetWindowLongFields
		{
			// ...
			GWL_EXSTYLE = (-20),
			// ...
		}

		[DllImport("user32.dll")]
		public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

		public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
		{
			int error = 0;
			IntPtr result = IntPtr.Zero;
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

		private static int IntPtrToInt32(IntPtr intPtr)
		{
			return unchecked((int)intPtr.ToInt64());
		}

		[DllImport("kernel32.dll", EntryPoint = "SetLastError")]
		public static extern void SetLastError(int dwErrorCode);


		#endregion
	}
}
