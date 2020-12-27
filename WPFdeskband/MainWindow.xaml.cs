using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Threading;
using csLib;
using iSearch;

namespace iSearchWPF
{
	/// <summary>
	/// Interaction logic for UserControl2.xaml
	/// </summary>
	public partial class MainWindow : UserControl
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

		#region Context Menu
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

	}
}
