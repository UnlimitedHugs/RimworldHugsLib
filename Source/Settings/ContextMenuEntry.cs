using System;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Settings {
	/// <summary>
	/// Contains data used for the creation of a <see cref="Verse.FloatMenuOption"/>.
	/// </summary>
	public struct ContextMenuEntry {
		/// <summary>
		/// A name for the entry to show to the player.
		/// </summary>
		public string Label { get; }
		/// <summary>
		/// The delegate that will be called when the menu entry is clicked.
		/// </summary>
		public Action Action { get; }
		/// <summary>
		/// Set to true to make a greyed-out, non-selectable menu entry.
		/// </summary>
		public bool Disabled { get; set; }

		/// <param name="label"><inheritdoc cref="Label"/></param>
		/// <param name="action"><inheritdoc cref="Action"/></param>
		public ContextMenuEntry(string label, Action action) : this() {
			Label = label;
			Action = action;
		}

		internal void Validate() {
			if (string.IsNullOrEmpty(Label)) {
				throw new FormatException($"{nameof(ContextMenuEntry)} must have a non-empty label: {this}");
			}
			if (Action == null) {
				throw new FormatException($"{nameof(ContextMenuEntry)} must have non-null action: {this}");
			}
		}
		
		public override string ToString() {
			return $"[{nameof(Label)}:{Label.ToStringSafe()}, {nameof(Action)}:{HugsLibUtility.DescribeDelegate(Action)}]";
		}
	}
}