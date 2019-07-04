using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Media;
using TagBites.WinSchedulers;
using TagBites.WinSchedulers.Descriptors;

namespace TimeSchedulerCustomization
{
    public class SchedulerDataSource : TimeSchedulerDataSource
    {
        private readonly ResourceModel[] _resources;
        private readonly IDictionary<DateTime, IList<TaskModel>> _tasks = new Dictionary<DateTime, IList<TaskModel>>();

        public SchedulerDataSource()
        {
            _resources = new [] 
            {
                new ResourceModel("[A] Cutting Station"),
                new ResourceModel("[A] Materials"),
                new ResourceModel("[B] Preparation Station"),
                new ResourceModel("[B] Materials"),
                new ResourceModel("[C] Bonding Station"),
                new ResourceModel("[C] Materials"),
                new ResourceModel("[D] Testing Station"),
                new ResourceModel("[D] Materials"),
                new ResourceModel("[E] Painting/Lacquering Station"),
                new ResourceModel("[E] Materials"),
                new ResourceModel("[F] Decorating Station"),
                new ResourceModel("[F] Materials"),
                new ResourceModel("[G] Controlling Station"),
                new ResourceModel("[G] Materials"),
                new ResourceModel("[H] Packing Station"),
                new ResourceModel("[H] Materials")
            };
        }


        protected override TimeSchedulerResourceDescriptor CreateResourceDescriptor()
        {
            return new TimeSchedulerResourceDescriptor(typeof(ResourceModel));
        }
        protected override TimeSchedulerTaskDescriptor CreateTaskDescriptor()
        {
            return new TimeSchedulerTaskDescriptor(typeof(TaskModel), nameof(TaskModel.Resource), nameof(TaskModel.Interval))
            {
                ColorMember = nameof(TaskModel.Color),
                FontColorMember = nameof(TaskModel.FontColor),
                BorderColorMember = nameof(TaskModel.BorderColor),
                ProgressMember = nameof(TaskModel.Progress)
            };
        }

        public override IList<object> LoadResources() => _resources;
        public override void LoadContent(TimeSchedulerDataSourceView view)
        {
            var resources = view.Resources;
            var interval = view.Interval;
            var resourcesHashSet = resources.Cast<ResourceModel>().ToDictionary(x => x.ID);

            IList<TaskModel> GetTaskForDate(DateTime date)
            {
                if (!_tasks.ContainsKey(date))
                    _tasks.Add(date.Date, GenerateTasks(date).ToList());

                return _tasks[date];
            };

            // WorkTimes
            for (var i = 0; i <= (interval.End - interval.Start).TotalDays + 1; i++)
                foreach (var item in resources)
                {
                    var start = interval.Start.Date.AddTicks(TimeSpan.TicksPerDay * i + TimeSpan.TicksPerHour * 8);
                    var end = start.AddHours(8);

                    view.AddWorkTime(item, new DateTimeInterval(start, end), Colors.White);
                }

            for (var i = interval.Start; i < interval.End; i = i.AddDays(1))
            {
                var date = i.Date;
                var tasks = GetTaskForDate(date);
                // Tasks  
                foreach (var task in tasks)
                    if (interval.IntersectsWith(task.Interval) && resources.Contains(task.Resource))
                    {
                        view.AddTask(task);

                        // Connections
                        var nextTask = tasks.FirstOrDefault(x => x.Resource.ID == task.Resource.ID + 2 && x.Interval.Start > task.Interval.End)
                            ?? GetTaskForDate(i.AddDays(1).Date).FirstOrDefault(x => x.Resource.ID == task.Resource.ID + 2 && x.Interval.Start > task.Interval.End);
                        if(nextTask != null)
                            view.AddConnection(task, nextTask, true, Colors.DarkOrange);

                        // Markers
                        if(resourcesHashSet.ContainsKey(task.Resource.ID + 1))
                            view.AddMarker(resourcesHashSet[task.Resource.ID + 1], task.Interval.End + TimeSpan.FromHours(1), Colors.IndianRed);
                    }     
                
                // Interval markers
                if(i.DayOfWeek == DayOfWeek.Sunday)
                    view.AddIntervalMarker(new DateTimeInterval(date, date + TimeSpan.FromDays(1)), m_colors[1]);
            }

            // Graphs
            for (var j = 0; j < resources.Count; j++)
            {
                if (j % 2 == 0)
                    continue;

                var graph = new TimeSchedulerGraph(TimeSchedulerGraphType.StepWithFill, m_colors[j % m_colors.Length], 0, 2);

                var lastDate = DateTime.MinValue;
                graph.AddPoint(lastDate, 0);
                
                var random = new Random(j * 12361);
                var size = 1000;
                var step = view.Interval.Duration.Ticks / size;

                lastDate = view.Interval.Start;

                for (var i = 0; i < size; i++)
                    if (random.Next(0, 5) == 0)
                    {
                        graph.AddPoint(lastDate + TimeSpan.FromTicks(i * step), random.NextDouble()*2);
                    }

                if (lastDate < DateTime.MaxValue)
                    graph.AddPoint(DateTime.MaxValue, 0);

                view.AddGraph(resources[j], graph);
            }

            // Now markers
            view.AddMarker(DateTime.Now, Colors.DodgerBlue);
        }

        #region Data generation

        private readonly Random m_random = new Random();
        private readonly Color[] m_colors =
        {
            Color.FromRgb(178, 191, 229),
            Color.FromRgb(178,223, 229),
            Color.FromRgb(178, 229, 203),
            Color.FromRgb(184, 229, 178),
            Color.FromRgb(197, 178, 229),
            Color.FromRgb(216, 229, 178),
            Color.FromRgb(229, 178, 178),
            Color.FromRgb(229,178,197),
            Color.FromRgb(229, 178, 229),
            Color.FromRgb(229, 210, 178),
        };

        public IEnumerable<TaskModel> GenerateTasks(DateTime dateTime)
        {
            Color Lerp(Color color, Color to, float amount)
            {
                return Color.FromRgb(
                    (byte) (color.R + (to.R - color.R) * amount),
                    (byte) (color.G + (to.G - color.G) * amount),
                    (byte) (color.B + (to.B - color.B) * amount));
            }

            var id = 0;
            for (var k = 0; k < _resources.Length; k++)
            {
                if (k % 2 == 1)
                    continue;

                var resource = _resources[k];
                var count = 1;
                for (var j = 0; j < count; j++)
                {
                    var minutes = m_random.Next(60 * 20);
                    var length = m_random.Next(60, 3 * 60);

                    var groupId = m_random.Next(0, m_colors.Length - 1);
                    var color = m_colors[groupId];
                    var borderColor = Lerp(color, Colors.Black, 0.2f);
                    var fontColor = Color.FromRgb(110, 110, 110);

                    var interval = new DateTimeInterval(dateTime.AddMinutes(minutes), new TimeSpan(0, length, 0));
                    var maxConsumption = m_random.NextDouble() * 50;
                    var consumption = Math.Min(m_random.NextDouble() * 10, maxConsumption);
                    var sb = new StringBuilder();
                    sb.AppendLine($"Order: {groupId}/ZLP");
                    sb.AppendLine($"Workplace: {resource}");
                    sb.AppendLine($"Date: {interval} ({interval.Duration.TotalMinutes} Minutes)");
                    sb.AppendLine($"Planned quantity: {consumption:#,0.00} units / {maxConsumption:#,0.00} units  ({(consumption / maxConsumption) * 100:0.00}%)");

                    yield return new TaskModel()
                    {
                        Id = ++id,
                        Resource = resource,
                        Interval = interval,
                        Color = color,
                        BorderColor = borderColor,
                        FontColor = fontColor,
                        Progress = m_random.NextDouble(),
                        Text = sb.ToString()
                    };
                }
            }
        }

        #endregion

        #region Classes

        public class ResourceModel
        {
            private static int m_id = 0;
            public int ID { get; }
            public string Name { get; }

            public ResourceModel(string name)
            {
                ID = m_id++;
                Name = name;
            }

            public override string ToString() => Name;
        }
        public class TaskModel
        {
            public int Id { get; set; }
            public ResourceModel Resource { get; set; }
            public DateTimeInterval Interval { get; set; }
            public string Text { get; set; }
            public Color Color { get; set; }
            public Color FontColor { get; set; }
            public Color BorderColor { get; set; }
            public double Progress { get; set; }

            public TaskModel PreviousTask { get; set; }

            public override string ToString() => Text;
        }

        #endregion
    }
}
