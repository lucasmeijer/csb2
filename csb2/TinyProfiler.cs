using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading;
using NiceIO;

namespace Unity.TinyProfiling
{
	public class TinyProfiler
	{
		[ThreadStatic] private static List<TimedSection> ts_sections;
		[ThreadStatic] private static Stack<int> ts_openSections;

		static bool _started;
		private static Thread s_startingThread;
		static DateTime s_startTimeOfFirstSection;
	
		static NPath s_filename;
		static string s_reportTitle;
		private static List<ThreadContext> s_threadContexts = new List<ThreadContext>();

		public class ThreadContext
		{
			public List<TimedSection> Sections;
			public Stack<int> OpenSections;
			public int ThreadID;
			public string ThreadName;
		}

		public struct TimedSection
		{
			public string Label;
			public string Details;
			public double Start;
			public double Duration;
			public int Parent;

			public string Summary { get { return Label + " " + Details + " (" + Duration + "ms)"; }}
		}

		struct TimedSectionHandle : IDisposable
		{
			internal int index;

			public void Dispose()
			{
				CloseSection(index);
			}
		}

		public static IDisposable Section(string label, string details = "")
		{
			if (ts_sections == null)
				InitializeProfilerForCurrentThread();

			if (!_started)
			{
				Start();
			}

			var parent = ts_openSections.Count == 0 ? -1 : ts_openSections.Peek();
			var section = new TimedSection() {Label = label, Details = details, Start = GetTimeOffset(), Parent = parent};
			var index = ts_sections.Count;
			ts_sections.Add(section);

			ts_openSections.Push(index);
			return new TimedSectionHandle { index = index};
		}

		public static ReadOnlyCollection<ThreadContext> CaptureSnapshot()
		{
			return new List<ThreadContext>(s_threadContexts).AsReadOnly();
		}

		private static void InitializeProfilerForCurrentThread()
		{
			ts_sections = new List<TimedSection>(5000);
			ts_openSections = new Stack<int>(50);
			lock (s_threadContexts)
				s_threadContexts.Add(new ThreadContext() {OpenSections = ts_openSections, Sections = ts_sections, ThreadID = Thread.CurrentThread.ManagedThreadId, ThreadName = Thread.CurrentThread.Name});
		}

		private static void Start()
		{
			_started = true;
			s_startingThread = Thread.CurrentThread;
			s_startTimeOfFirstSection = DateTime.Now;
		}

		private static void Finish()
		{
			if (s_filename != null)
				WriteGraph();
			s_filename = null;
			_started = false;
			foreach(var context in s_threadContexts)
			{
				context.OpenSections.Clear();
				context.Sections.Clear();
			}
		}

		private static void CloseSection(int index)
		{
			var last = ts_openSections.Pop();
			if (last != index)
				throw new ArgumentException("TimedSection being closed is not the most recently opened");

			var section = ts_sections[index];
			section.Duration = GetTimeOffset() - section.Start;
			ts_sections[index] = section;

			if (ts_openSections.Count == 0 && s_startingThread == Thread.CurrentThread)
				Finish();
		}

		static internal double GetTimeOffset()
		{
			return (DateTime.Now - s_startTimeOfFirstSection).TotalMilliseconds;
		}

		static void WriteGraph()
		{
			var assembly = typeof(TinyProfiler).Assembly;
			using (Stream resFilestream = assembly.GetManifestResourceStream("csb2.SVGPan.js"))
			{
				byte[] ba = new byte[resFilestream.Length];
				resFilestream.Read(ba, 0, ba.Length);
				File.WriteAllBytes(s_filename.Parent.Combine("SVGPan.js").ToString(),ba);
			}
			Console.WriteLine("Writing ProfileReport to: "+s_filename);
			s_filename.WriteAllText(new GraphMaker().MakeGraph(s_threadContexts, s_reportTitle));
		}

		public static void ConfigureOutput(NPath reportFileName, string reportTitle)
		{
			s_filename = reportFileName;
			s_reportTitle = reportTitle;
		}
	}
}
