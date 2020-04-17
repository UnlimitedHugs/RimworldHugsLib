using System;
using System.Collections.Generic;
using HugsLib.Utils;

namespace HugsLib.News {
	internal interface IModSpotterDevActions {
		bool GetFirstTimeUserStatus(string packageId);
		void ToggleFirstTimeUserStatus(string packageId);
	}

	internal interface IUpdateFeaturesDevActions {
		Version GetLastSeenNewsVersion(string modIdentifier);
		IEnumerable<UpdateFeatureDef> ReloadAllUpdateFeatureDefs();
		bool TryShowAutomaticNewsPopupDialog();
		void SetLastSeenNewsVersion(string modIdentifier, Version version);
	}

	internal interface IStatusMessageSender {
		void Send(string message, bool success);
	}
	
	/// <summary>
	/// Provides the options for the dev tools dropdown menu in the extended update news dialog. 
	/// </summary>
	internal class UpdateFeaturesDevMenu {
		public event Action<IEnumerable<UpdateFeatureDef>> UpdateFeatureDefsReloaded;
		private readonly IUpdateFeaturesDevActions news;
		private readonly IModSpotterDevActions spotter;
		private readonly IStatusMessageSender messages;

		public UpdateFeaturesDevMenu(IUpdateFeaturesDevActions news, IModSpotterDevActions spotter, IStatusMessageSender messages) {
			this.news = news;
			this.spotter = spotter;
			this.messages = messages;
		}

		public IEnumerable<(string label, Action action, bool disabled)> GetMenuOptions(UpdateFeatureDef forDef) {
			var modName = forDef.modNameReadable;
			return new(string, Action, bool)[] {
				(GetNewsProviderStatusMessage(forDef), () => { }, true),
				("Reload all news (F5)", 
					ReloadNewsDefs, false),
				("Try show automatic news popup", 
					TryShowAutomaticNewsPopupDialog, false),
				($"{modName}: toggle first time user status", 
					() => ToggleFirstTimeUserStatus(forDef), false),
				($"{modName}: set last seen news version to {forDef.Version}", 
					() => SetLastSeenNewsVersion(forDef), false),
				($"{modName}: reset last seen news version", 
					() => ResetLastSeenNewsVersion(forDef), false)
			};
		}

		public void ReloadNewsDefs() {
			var newlyLoadedDefs = news.ReloadAllUpdateFeatureDefs();
			UpdateFeatureDefsReloaded?.Invoke(newlyLoadedDefs);
		}

		private string GetNewsProviderStatusMessage(UpdateFeatureDef forDef) {
			return $"{forDef.modNameReadable} status:\n" +
				$"Last seen version: {news.GetLastSeenNewsVersion(forDef.OwningModId).ToSemanticString("none")}, " +
				$"first time user: {(spotter.GetFirstTimeUserStatus(forDef.OwningPackageId) ? "Yes" : "No")}";
		}

		private void TryShowAutomaticNewsPopupDialog() {
			var anyNewsToDisplay = news.TryShowAutomaticNewsPopupDialog();
			if (!anyNewsToDisplay) {
				messages.Send(
					"Found no relevant unread update news to display. Automatic popup will not appear.", false);
			}
		}

		private void ToggleFirstTimeUserStatus(UpdateFeatureDef forDef) {
			spotter.ToggleFirstTimeUserStatus(forDef.OwningPackageId);
			var confirmationMessage = spotter.GetFirstTimeUserStatus(forDef.OwningPackageId)
				? $"Set player as first time user of {forDef.modNameReadable}."
				: $"Set player as returning user of {forDef.modNameReadable}.";
			messages.Send(confirmationMessage, true);
		}

		private void SetLastSeenNewsVersion(UpdateFeatureDef forDef) {
			news.SetLastSeenNewsVersion(forDef.OwningModId, forDef.Version);
			messages.Send(
				$"Last seen news version has been set to {forDef.Version} for {forDef.modNameReadable}.", true);
		}

		private void ResetLastSeenNewsVersion(UpdateFeatureDef forDef) {
			news.SetLastSeenNewsVersion(forDef.OwningModId, null);
			messages.Send($"Last seen news version has been cleared for {forDef.modNameReadable}.", true);
		}
	}
}