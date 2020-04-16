using System;
using System.Collections.Generic;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.News {
	internal interface IUpdateNewsDevActions {
		bool GetFirstTimeUserStatus(string packageId);
		Version GetLastSeenNewsVersion(string modIdentifier);
		void ReloadAllUpdateFeatureDefs();
		bool TryShowAutomaticNewsPopupDialog();
		void SetLastSeenNewsVersion(string modIdentifier, Version version);
		void ToggleFirstTimeUserStatus(string packageId);
	}
	
	/// <summary>
	/// Displays the update news dev tools button and menu options in the extended update news dialog. 
	/// </summary>
	internal class UpdateFeaturesDialogDevTools {
		public event Action<IEnumerable<UpdateFeatureDef>> UpdateFeatureDefsReloaded;
		
		private readonly IUpdateNewsDevActions handler;
		private readonly Texture2D buttonTexture;

		public UpdateFeaturesDialogDevTools(IUpdateNewsDevActions handler, Texture2D buttonTexture) {
			this.handler = handler;
			this.buttonTexture = buttonTexture;
		}

		public void OnGUI() {
			if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.F5) {
				Event.current.Use();
				ReloadNewsDefs();
			}
		}

		public void DrawMenuButton(Rect inRect, UpdateFeatureDef forDef) {
			if (Mouse.IsOver(inRect)) {
				Widgets.DrawHighlight(inRect);
			}
			var textureRect = new Rect(
				inRect.center.x - buttonTexture.width / 2f, inRect.center.y - buttonTexture.height / 2f,
				buttonTexture.width, buttonTexture.height
			);
			Widgets.DrawTextureFitted(textureRect, buttonTexture, 1f);
			if (Widgets.ButtonInvisible(inRect)) {
				OpenDropdownMenu(forDef);
			}
		}

		private void OpenDropdownMenu(UpdateFeatureDef forDef) {
			var modName = forDef.modNameReadable;
			Find.WindowStack.Add(
				new FloatMenu(new List<FloatMenuOption> {
					new FloatMenuOption(GetNewsProviderStatusMessage(forDef), () => { }) {Disabled = true},
					new FloatMenuOption("Reload all news (F5)",
						ReloadNewsDefs),
					new FloatMenuOption("Try show automatic news popup",
						TryShowAutomaticNewsPopupDialog),
					new FloatMenuOption($"{modName}: toggle first time user status",
						() => BecomeFirstTimeUser(forDef)),
					new FloatMenuOption($"{modName}: set last seen news version to {forDef.Version}",
						() => SetLastSeenNewsVersion(forDef)),
					new FloatMenuOption($"{modName}: reset last seen news version",
						() => ResetLastSeenNewsVersion(forDef)),
				})
			);
		}

		private string GetNewsProviderStatusMessage(UpdateFeatureDef forDef) {
			return $"{forDef.modNameReadable} status:\n" +
				$"Last seen version: {handler.GetLastSeenNewsVersion(forDef.OwningModId).ToSemanticString("none")}, " +
				$"first time user: {(handler.GetFirstTimeUserStatus(forDef.OwningPackageId) ? "yes" : "no")}";
		}
		
		private void ReloadNewsDefs() {
			handler.ReloadAllUpdateFeatureDefs();
			UpdateFeatureDefsReloaded?.Invoke(DefDatabase<UpdateFeatureDef>.AllDefs);
		}

		private void TryShowAutomaticNewsPopupDialog() {
			var anyNewsToDisplay = handler.TryShowAutomaticNewsPopupDialog();
			if (!anyNewsToDisplay) {
				Messages.Message("Found no relevant unread update news to display. Automatic popup will not appear.",
					MessageTypeDefOf.RejectInput);
			}
		}

		private void BecomeFirstTimeUser(UpdateFeatureDef forDef) {
			handler.ToggleFirstTimeUserStatus(forDef.OwningPackageId);
			var confirmationMessage = handler.GetFirstTimeUserStatus(forDef.OwningPackageId)
				? $"Set player as first time user of {forDef.modNameReadable}."
				: $"Set player as returning user of {forDef.modNameReadable}.";
			Messages.Message(confirmationMessage, MessageTypeDefOf.TaskCompletion);
		}

		private void SetLastSeenNewsVersion(UpdateFeatureDef forDef) {
			handler.SetLastSeenNewsVersion(forDef.OwningModId, forDef.Version);
			Messages.Message($"Last seen news version have been set to {forDef.Version} for {forDef.modNameReadable}.",
				MessageTypeDefOf.TaskCompletion);
		}

		private void ResetLastSeenNewsVersion(UpdateFeatureDef forDef) {
			handler.SetLastSeenNewsVersion(forDef.OwningModId, null);
			Messages.Message($"Last seen news version have been cleared for {forDef.modNameReadable}.", 
				MessageTypeDefOf.TaskCompletion);
		}
	}
}