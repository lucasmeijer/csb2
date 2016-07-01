using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Unity.TinyProfiling
{
	internal class GraphMaker
	{
		private double millisecondsToPixels;
		readonly Palette _palette = new Palette();
		private const int _heightForSection = 15;
		private const int YOfTopSection = 20;
		private int YTopOfCurrentThread;

		public string MakeGraph(List<TinyProfiler.ThreadContext> threadContexts, string title)
		{
			var longestTime = threadContexts.SelectMany(t=>t.Sections).Max(s => s.Start + s.Duration);

			millisecondsToPixels = 2000 / longestTime;

			var sb = new StringBuilder();
			sb.Append(Header(title + " " + longestTime + "ms"));
			
			YTopOfCurrentThread = 80;

			foreach(var threadContext in threadContexts.Where(t=>t.Sections.Any()))
				sb.Append(EmitSingleThread(threadContext));

			sb.Append("</g>");
			sb.Append(@"<rect x=""20"" y=""20"" rx=""5"" ry=""5"" width=""550"" height=""30"" style=""fill:white;stroke:black;stroke-width:2;""/>");

			sb.Append(@"<text text-anchor="""" x=""25"" y=""40"" font-size=""16"" font-family=""Verdana"" fill=""rgb(0,0,0)"" id=""details"" > </text></svg>");

			return sb.ToString();
		}

		private StringBuilder EmitSingleThread(TinyProfiler.ThreadContext threadContext)
		{
			var timedSections = threadContext.Sections;
			int maxParents = MaxParents(timedSections);
			var height = YOfTopSection + _heightForSection*(maxParents+1);

			var sb = new StringBuilder();

			sb.AppendLine(string.Format(@"<rect x=""0.0"" y=""{0}"" width=""2000.0"" height=""{1}"" fill=""url(#background)""  />", YTopOfCurrentThread,height));
			sb.AppendLine(string.Format(@"<text text-anchor=""left"" x=""0"" y=""{0}"" font-size=""12"" font-family=""Verdana"" fill=""rgb(0,0,0)""  >ThreadID: {1} {2}</text>", YTopOfCurrentThread+12,threadContext.ThreadID, threadContext.ThreadName));

			for (int i = 0; i != timedSections.Count; i++)
				sb.Append(SectionRect(timedSections, i));

			YTopOfCurrentThread += height + 50;

			return sb;
		}

		private int MaxParents(List<TinyProfiler.TimedSection> sections)
		{
			int max = 0;
			for (int i = 0; i != sections.Count; i++)
				max = Math.Max(max, NumberOfParents(sections, i));
			return max;
		}

		private string SectionRect(List<TinyProfiler.TimedSection> sections, int index)
		{
			var y = YTopOfCurrentThread + YOfTopSection + NumberOfParents(sections, index) * _heightForSection;
			var displaySection = sections[index];

			var sb = new StringBuilder();
			sb.AppendLine();
			sb.AppendLine(string.Format(@"<rect class=""func_g"" onmouseover=""s('{4}')"" onmouseout=""c()"" x=""{0}"" y=""{1}"" width=""{2}"" height=""15.0"" fill=""{3}"" rx=""0"" ry=""0"" />", Scale(displaySection.Start).ToString(CultureInfo.InvariantCulture), y, Scale(displaySection.Duration).ToString(CultureInfo.InvariantCulture), _palette.ColorFor(displaySection.Label), Clean(displaySection.Summary)));

			var fontSizeFor = FontSizeFor(displaySection.Duration);
			if (fontSizeFor > 2)
				sb.AppendLine(string.Format(@"<text class=""func_text"" text-anchor=""top"" x=""{0}"" y=""{1}"" font-size=""{2}"" font-family=""Verdana"" fill=""rgb(0,0,s0)"">{3}</text>", Scale(displaySection.Start).ToString(CultureInfo.InvariantCulture), y + 13, fontSizeFor.ToString(CultureInfo.InvariantCulture), displaySection.Label));
			return sb.ToString();
		}

		private string Clean(string details)
		{
			if (details.Contains("<")) details = details.Replace("<", "");
			if (details.Contains(">")) details = details.Replace(">", "");
			return details;
		}

		private double FontSizeFor(double duration)
		{
			var units = Scale(duration);

			//scale=20  -> fontsize=2
			//scale=200 -> fontsize=10

			float scaleA = 20f;
			float fontA = 2f;
			float scaleB = 200f;
			float fontB = 10f;

			double f = (units - scaleA) / (scaleB - scaleA);
			if (f > 1) f = 1;
			
			return fontA + f*(fontB - fontA);
		}

		private int NumberOfParents(List<TinyProfiler.TimedSection> sections, int index)
		{
			var parent = sections[index].Parent;
			if (parent == -1)
				return 0;

			return NumberOfParents(sections, parent) + 1;
		}
	
		private double Scale(double milliseconds)
		{
			return milliseconds*millisecondsToPixels;
		}

		string Header(string title)
		{
			return string.Format(@"<?xml version=""1.0"" standalone=""no""?>
<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 1.1//EN"" ""http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"">
<svg version=""1.1"" width=""2000"" height=""800"" onload=""init(evt)"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"">
<defs >
    <linearGradient id=""background"" y1=""0"" y2=""1"" x1=""0"" x2=""0"" >
        <stop stop-color=""#eeeeee"" offset=""5%"" />
        <stop stop-color=""#eeeeb0"" offset=""95%"" />
    </linearGradient>
</defs>
<style type=""text/css"">
    .func_g:hover {{ fill:rgb(220,80,80); }}
    .func_text {{ pointer-events:none; }}
</style>
<script type=""text/ecmascript"">
<![CDATA[
    var details;
    function init(evt) {{ details = document.getElementById(""details"").firstChild; }}
    function s(info) {{ details.nodeValue = info; }}
    function c() {{ details.nodeValue = ' '; }}
]]>
</script>
<script xlink:href=""SVGPan.js""/>

<g id=""viewport"" class=""chart"" width=""2000"">

<text text-anchor=""middle"" x=""1000"" y=""30"" font-size=""17"" font-family=""Verdana"" fill=""rgb(0,0,0)""  >{0}</text>
", title);
		}
	}
}
