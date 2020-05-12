using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	/// <summary>
	/// Caches the result of a string translation for performance.
	/// Also caches the calculated size of the label that would accomodate the translated string.
	/// </summary>
	internal class CachedLabel {
		public static CachedLabel FromKey(string key) {
			return new CachedLabel(key.Translate());
		}

		private float height;
		private int cachedForMaxWidth = int.MinValue;

		public TaggedString Text { get; }
		public Vector2 Size { get; }

		public CachedLabel(TaggedString text) {
			Text = text;
			Size = Verse.Text.CalcSize(text);
		}

		public float GetHeight(float maxWidth) {
			if (cachedForMaxWidth != (int)maxWidth) {
				cachedForMaxWidth = (int)maxWidth;
				height = Verse.Text.CalcHeight(Text, maxWidth);
			}
			return height;
		}

		public static implicit operator string(CachedLabel cached) {
			return cached.Text;
		}
	}
}