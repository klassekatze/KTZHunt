using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{
		public class SpriteHUDLCD
		{
			static Dictionary<string, Color> ColorList = new Dictionary<string, Color> { { "aliceblue", Color.AliceBlue }, { "antiquewhite", Color.AntiqueWhite }, { "aqua", Color.Aqua }, { "aquamarine", Color.Aquamarine }, { "azure", Color.Azure }, { "beige", Color.Beige }, { "bisque", Color.Bisque }, { "black", Color.Black }, { "blanchedalmond", Color.BlanchedAlmond }, { "blue", Color.Blue }, { "blueviolet", Color.BlueViolet }, { "brown", Color.Brown }, { "burlywood", Color.BurlyWood }, { "badetblue", Color.CadetBlue }, { "chartreuse", Color.Chartreuse }, { "chocolate", Color.Chocolate }, { "coral", Color.Coral }, { "cornflowerblue", Color.CornflowerBlue }, { "cornsilk", Color.Cornsilk }, { "crimson", Color.Crimson }, { "cyan", Color.Cyan }, { "darkblue", Color.DarkBlue }, { "darkcyan", Color.DarkCyan }, { "darkgoldenrod", Color.DarkGoldenrod }, { "darkgray", Color.DarkGray }, { "darkgreen", Color.DarkGreen }, { "darkkhaki", Color.DarkKhaki }, { "darkmagenta", Color.DarkMagenta }, { "darkoliveGreen", Color.DarkOliveGreen }, { "darkorange", Color.DarkOrange }, { "darkorchid", Color.DarkOrchid }, { "darkred", Color.DarkRed }, { "darksalmon", Color.DarkSalmon }, { "darkseagreen", Color.DarkSeaGreen }, { "darkslateblue", Color.DarkSlateBlue }, { "darkslategray", Color.DarkSlateGray }, { "darkturquoise", Color.DarkTurquoise }, { "darkviolet", Color.DarkViolet }, { "deeppink", Color.DeepPink }, { "deepskyblue", Color.DeepSkyBlue }, { "dimgray", Color.DimGray }, { "dodgerblue", Color.DodgerBlue }, { "firebrick", Color.Firebrick }, { "floralwhite", Color.FloralWhite }, { "forestgreen", Color.ForestGreen }, { "fuchsia", Color.Fuchsia }, { "gainsboro", Color.Gainsboro }, { "ghostwhite", Color.GhostWhite }, { "gold", Color.Gold }, { "goldenrod", Color.Goldenrod }, { "gray", Color.Gray }, { "green", Color.Green }, { "greenyellow", Color.GreenYellow }, { "doneydew", Color.Honeydew }, { "hotpink", Color.HotPink }, { "indianred", Color.IndianRed }, { "indigo", Color.Indigo }, { "ivory", Color.Ivory }, { "khaki", Color.Khaki }, { "lavender", Color.Lavender }, { "lavenderblush", Color.LavenderBlush }, { "lawngreen", Color.LawnGreen }, { "lemonchiffon", Color.LemonChiffon }, { "lightblue", Color.LightBlue }, { "lightcoral", Color.LightCoral }, { "lightcyan", Color.LightCyan }, { "lightgoldenrodyellow", Color.LightGoldenrodYellow }, { "lightgray", Color.LightGray }, { "lightgreen", Color.LightGreen }, { "lightpink", Color.LightPink }, { "lightsalmon", Color.LightSalmon }, { "lightseagreen", Color.LightSeaGreen }, { "lightskyblue", Color.LightSkyBlue }, { "lightslategray", Color.LightSlateGray }, { "lightsteelblue", Color.LightSteelBlue }, { "lightyellow", Color.LightYellow }, { "lime", Color.Lime }, { "limegreen", Color.LimeGreen }, { "linen", Color.Linen }, { "magenta", Color.Magenta }, { "maroon", Color.Maroon }, { "mediumaquamarine", Color.MediumAquamarine }, { "mediumblue", Color.MediumBlue }, { "mediumorchid", Color.MediumOrchid }, { "mediumpurple", Color.MediumPurple }, { "mediumseagreen", Color.MediumSeaGreen }, { "mediumslateblue", Color.MediumSlateBlue }, { "mediumspringgreen", Color.MediumSpringGreen }, { "mediumturquoise", Color.MediumTurquoise }, { "mediumvioletred", Color.MediumVioletRed }, { "midnightblue", Color.MidnightBlue }, { "mintcream", Color.MintCream }, { "mistyrose", Color.MistyRose }, { "moccasin", Color.Moccasin }, { "navajowhite", Color.NavajoWhite }, { "navy", Color.Navy }, { "oldlace", Color.OldLace }, { "olive", Color.Olive }, { "olivedrab", Color.OliveDrab }, { "orange", Color.Orange }, { "orangered", Color.OrangeRed }, { "orchid", Color.Orchid }, { "palegoldenrod", Color.PaleGoldenrod }, { "palegreen", Color.PaleGreen }, { "paleturquoise", Color.PaleTurquoise }, { "palevioletred", Color.PaleVioletRed }, { "papayawhip", Color.PapayaWhip }, { "peachpuff", Color.PeachPuff }, { "peru", Color.Peru }, { "pink", Color.Pink }, { "plum", Color.Plum }, { "powderblue", Color.PowderBlue }, { "purple", Color.Purple }, { "red", Color.Red }, { "rosybrown", Color.RosyBrown }, { "royalblue", Color.RoyalBlue }, { "saddlebrown", Color.SaddleBrown }, { "salmon", Color.Salmon }, { "sandybrown", Color.SandyBrown }, { "seagreen", Color.SeaGreen }, { "seashell", Color.SeaShell }, { "sienna", Color.Sienna }, { "silver", Color.Silver }, { "skyblue", Color.SkyBlue }, { "slateblue", Color.SlateBlue }, { "slategray", Color.SlateGray }, { "snow", Color.Snow }, { "springgreen", Color.SpringGreen }, { "steelblue", Color.SteelBlue }, { "tan", Color.Tan }, { "teal", Color.Teal }, { "thistle", Color.Thistle }, { "tomato", Color.Tomato }, { "turquoise", Color.Turquoise }, { "violet", Color.Violet }, { "wheat", Color.Wheat }, { "white", Color.White }, { "whitesmoke", Color.WhiteSmoke }, { "yellow", Color.Yellow }, { "yellowgreen", Color.YellowGreen } };
			public IMyTextSurface s = null;

			public SpriteHUDLCD(IMyTextSurface s)
			{
				this.s = s;
			}
			int ltick = -1;
			string lasttext = "-1";
			public void setLCD(string text)
			{
				if (text != lasttext || tick - ltick > 120)
				{
					ltick = tick;
					lasttext = text;
					s.WriteText(text);
					List<object> tok = new List<object>();
					string[] tokens = text.Split(new string[] { "<color=" }, StringSplitOptions.None);
					for (int i = 0; i < tokens.Length; i++)
					{
						var t = tokens[i];
						foreach (var kvp in ColorList)
						{
							if (t.StartsWith(kvp.Key + ">"))
							{
								t = t.Substring(kvp.Key.Length + 1);
								tok.Add(kvp.Value);
								break;
							}
						}
						tok.Add(t);
					}

					s.ContentType = ContentType.SCRIPT;
					s.Script = "";
					RectangleF _viewport;
					_viewport = new RectangleF(
			(s.TextureSize - s.SurfaceSize) / 2f,
			s.SurfaceSize
		);
					using (var frame = s.DrawFrame())
					{
						var zpos = new Vector2(0, 0) + _viewport.Position + new Vector2(s.TextPadding / 100 * s.SurfaceSize.X, s.TextPadding / 100 * s.SurfaceSize.Y);
						var position = zpos;
						Color cColor = Color.White;
						foreach (var t in tok)
						{
							if (t is Color) cColor = (Color)t;
							else if (t is string) writeText((string)t, frame, ref position, zpos, s.FontSize, cColor);
						}
					}
				}
			}

			public void writeText(string text, MySpriteDrawFrame frame, ref Vector2 pos, Vector2 zpos, float textSize, Color color)
			{
				string[] lines = text.Split('\n');
				for (int l = 0; l < lines.Length; l++)
				{
					var line = lines[l];
					if (line.Length > 0)
					{
						MySprite sprite = MySprite.CreateText(line, "Monospace", color, textSize, TextAlignment.LEFT);
						sprite.Position = pos;
						frame.Add(sprite);
					}
					if (l < lines.Length - 1)
					{
						pos.X = zpos.X;
						pos.Y += 28 * textSize;
					}
					else pos.X += 20 * textSize * line.Length;
				}
			}
		}
	}
}
