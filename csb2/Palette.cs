using System;
using System.Collections.Generic;

namespace Unity.TinyProfiling
{
	internal class Palette
	{
		readonly Dictionary<string, string> _colorCache = new Dictionary<string, string>();
		private readonly IEnumerator<string> _nextColor = NextColor().GetEnumerator();

		public string ColorFor(string label)
		{
			string result;
			if (_colorCache.TryGetValue(label, out result))
				return result;

			_nextColor.MoveNext();

			_colorCache[label] = _nextColor.Current;
			return _nextColor.Current;
		}

		static IEnumerable<string> NextColor()
		{
			while(true)
				foreach (var color in _staticColors)
					yield return color;
		}

		private static readonly string[] _staticColors =
		{
			//http://tools.medialab.sciences-po.fr/iwanthue/ is your friend
			"#561E23",
			"#73E62A",
			"#5AE9F6",
			"#6F65E5",
			"#EBA52E",
			"#E8A6C0",
			"#426817",
			"#E83923",
			"#D433AB",
			"#85E699",
			"#3B4875",
			"#DA3861",
			"#858775",
			"#61A7E4",
			"#D1DC66",
			"#E38C70",
			"#6D4F18",
			"#23361B",
			"#814298",
			"#49AA9B",
			"#B2AC6B",
			"#B8D5CC",
			"#D784DC",
			"#A12E19",
			"#59B74F",
			"#571542",
			"#D04ADE",
			"#DCB49A",
			"#567486",
			"#983052",
			"#292A2F",
			"#925140",
			"#355F54",
			"#E0418B",
			"#909D2F",
			"#5FE6CB",
			"#9A3078",
			"#D4E235",
			"#8C7BA2",
			"#D674A9",
			"#624F43",
			"#DC6B82",
			"#D7ADEC",
			"#B9EBB7",
			"#9ADB48",
			"#DF614F",
			"#A17E2C",
			"#578CE6",
			"#5453B0",
			"#A35A27",
			"#DBBF37",
			"#A37E5D",
			"#5B8F6B",
			"#99A9BB",
			"#9C83D5",
			"#4FAFBF",
			"#8D222D",
			"#E2B169",
			"#806673",
			"#45A562",
			"#DD6025",
			"#3B2815",
			"#D57F30",
			"#3C64A7",
			"#B2BAEA",
			"#77EA76",
			"#37213E",
			"#72436B",
			"#C8CEA6",
			"#ABDAEF",
			"#6A2B14",
			"#C6898E",
			"#79924C",
			"#DEDE92",
			"#366434",
			"#3D79A6",
			"#E03745",
			"#3E306F",
			"#8A4E5A",
			"#6BBA8B",
			"#2C4359",
			"#A565DC",
			"#51DD4D",
			"#739C96",
			"#A8DA82",
			"#DECECD",
			"#5D5B2C",
			"#7C5993",
			"#4CE7B2",
			"#BA86BC",
			"#7D8FC4",
			"#727DE9",
			"#C05F55",
			"#D863C8",
			"#D2B5D1",
			"#48E591",
			"#489724",
			"#AEEBD7",
			"#59ADD1",
			"#61D2D2"
		};
	}
}