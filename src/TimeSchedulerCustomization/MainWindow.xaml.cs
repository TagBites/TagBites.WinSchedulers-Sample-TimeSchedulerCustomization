using System;
using System.Windows;
using TagBites.WinSchedulers;
using TagBites.WinSchedulers.Drawing;

namespace TimeSchedulerCustomization
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }


        private void Initialize()
        {
            SC_Scheduler.DataSource = new SchedulerDataSource();
            SC_Scheduler.ViewOptions.ExactTaskDates = true;
            SC_Scheduler.ViewOptions.InnerLines = true;
            SC_Scheduler.ViewOptions.FadeNotSelectedOrNotConnected = true;

            SC_Scheduler.BehaviorOptions.AutoRefreshPanel = true;
            SC_Scheduler.BehaviorOptions.AutoRefresh = TimeSpan.FromSeconds(30);

            SC_Scheduler.TimeScroller.Scale = TimeSpan.FromHours(12);
            SC_Scheduler.ResourceScroller.HeaderSize = 100;

            SC_Scheduler.VerticalTimeLine = false;

            SC_Scheduler.VisibleDateTimeInterval = new DateTimeInterval(DateTime.Now, new TimeSpan(10, 0, 0, 0));
            SC_Scheduler.ScrollTo(DateTime.Now, Alignment.Center);
        }
    }
}
