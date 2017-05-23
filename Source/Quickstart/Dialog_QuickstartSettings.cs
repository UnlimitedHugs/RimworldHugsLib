using System;
using UnityEngine;
using Verse;

namespace HugsLib.Quickstart {
	public class Dialog_QuickstartSettings : Window {
		public override Vector2 InitialSize {
			get { return new Vector2(600f, 500f); }
		}

		public Dialog_QuickstartSettings() {
			closeOnEscapeKey = true;
			doCloseButton = false;
			doCloseX = true;
			resizeable = false;
			draggable = true;
		}

		public override void DoWindowContents(Rect inRect) {
			const float categoryPadding = 10f;
			const float categoryInset = 30f;
			const float radioLabelInset = 40f;
			const float mainListingSpacing = 6f;
			const float subListingSpacing = 6f;
			const float subListingLabelWidth = 100f;
			const float subListingRowHeight = 30f;
			const float checkboxListingWidth = 250f;
			const float listingColumnSpacing = 17f;
			var mainListing = new Listing_Standard();
			mainListing.verticalSpacing = mainListingSpacing;
			mainListing.Begin(inRect);
			Text.Font = GameFont.Medium;
			mainListing.Label("Quickstart settings");
			Text.Font = GameFont.Small;
			mainListing.GapLine();
			mainListing.Gap();
			LabledRadioButton(mainListing, radioLabelInset, "Quickstart off", true);
			LabledRadioButton(mainListing, radioLabelInset, "Quickstart: load save file", false);
			var expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 1;
			MakeSubListing(mainListing, 0, expectedHeight, categoryPadding, categoryInset, subListingSpacing, (sub, width) => {
				sub.ColumnWidth = subListingLabelWidth;
				Text.Anchor = TextAnchor.MiddleLeft;
				sub.Label("Save file:", subListingRowHeight);
				Text.Anchor = TextAnchor.UpperLeft;
				sub.NewColumn();
				sub.ColumnWidth = width - subListingLabelWidth - listingColumnSpacing;
				sub.ButtonText("Select a save file");
			});			
			LabledRadioButton(mainListing, radioLabelInset, "Quickstart: generate map", false);
			expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 2;
			MakeSubListing(mainListing, 0, expectedHeight, categoryPadding, categoryInset, subListingSpacing, (sub, width) => {
				sub.ColumnWidth = subListingLabelWidth;
				Text.Anchor = TextAnchor.MiddleLeft;
				sub.Label("Scenario:", subListingRowHeight);
				sub.Label("Map size:", subListingRowHeight);
				Text.Anchor = TextAnchor.UpperLeft;
				sub.NewColumn();
				sub.ColumnWidth = width - subListingLabelWidth - listingColumnSpacing;
				sub.ButtonText("Crash Landing");
				sub.ButtonText("250x250 (default)");
			});
			expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 2;
			MakeSubListing(mainListing, checkboxListingWidth, expectedHeight, categoryPadding, 0f, subListingSpacing, (sub, width) => {
				var warning = false;
				sub.CheckboxLabeled("Abort quickstart on error", ref warning);
				sub.CheckboxLabeled("Abort quickstart on warning", ref warning);
			});
			mainListing.End();
			Text.Anchor = TextAnchor.UpperLeft;

			var btnSize = new Vector2(180f, 40f);
			var buttonYStart = inRect.height - btnSize.y;
			Widgets.ButtonText(new Rect(inRect.x, buttonYStart, btnSize.x, btnSize.y), "Help & Command line");
			Widgets.ButtonText(new Rect(inRect.width / 2f - btnSize.x / 2f, buttonYStart, btnSize.x, btnSize.y), "Generate map now");
			Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "Close");
		}

		private bool LabledRadioButton(Listing_Standard listing, float labelInset, string label, bool selected) {
			const float labelTopMargin = -4f;
			const float fontSize = 16f;
			float lineHeight = Text.LineHeight;
			var entryRect = listing.GetRect(lineHeight + listing.verticalSpacing);
			bool result = Widgets.RadioButton(entryRect.x, entryRect.y, selected);
			var labelRect = new Rect(entryRect.x + labelInset, entryRect.y + labelTopMargin, entryRect.width - labelInset, entryRect.height - labelTopMargin);
			Text.Font = GameFont.Medium;
			var emphasizedLabel = String.Format("<size={0}>{1}</size>", fontSize, label);
			Widgets.Label(labelRect, emphasizedLabel);
			Text.Font = GameFont.Small;
			return result;
		}

		private void MakeSubListing(Listing_Standard mainListing, float width, float allocatedHeight, float padding, float extraInset, float verticalSpacing, Action<Listing_Standard, float> drawContents) {
			var subRect = mainListing.GetRect(allocatedHeight);
			width = width > 0 ? width : subRect.width - (padding + extraInset);
			subRect = new Rect(subRect.x+padding+extraInset, subRect.y+padding, width, subRect.height-padding*2f);
			var sub = new Listing_Standard {verticalSpacing = verticalSpacing};
			sub.Begin(subRect);
			drawContents(sub, width);
			sub.End();
		}
	}
}