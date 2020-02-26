using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.News {
	/// <summary>
	/// Displays a list to update feature defs with basic image and formatting support. See <see cref="UpdateFeatureDef"/> for proper syntax.
	/// </summary>
	public class Dialog_UpdateFeatures : Window {
		private const float HeaderLabelHeight = 40f; 
		private const float EntryTitleLabelHeight = 40f;
		private const float EntryTitleLabelPadding = 4f;
		private const float EntryTitleHeight = EntryTitleLabelHeight + EntryTitleLabelPadding;
		private const float EntryTitleLinkWidth = 40f;
		private const float EntryContentIndent = 4f;
		private const float EntryFooterHeight = 16f;
		private const float ScrollBarWidthMargin = 18f;
		private const int EntryTitleFontSize = 18;
		private const float SegmentImageMargin = 6f;
		private const float SegmentTextMargin = 2f;

		private readonly UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders;
		private readonly Color TitleLineColor = new Color(0.3f, 0.3f, 0.3f);
		private readonly Color LinkTextColor = new Color(.7f, .7f, 1f);
		private readonly Dictionary<string, Texture2D> resolvedImages = new Dictionary<string, Texture2D>();
		private readonly string ignoreToggleTip = "HugsLib_features_ignoreTooltip".Translate();
		private List<FeatureEntry> entries;
		private float totalContentHeight = -1;
		private Vector2 scrollPosition;
		private bool anyImagesPending;

		public override Vector2 InitialSize {
			get { return new Vector2(650f, 700f); }
		}

		public Dialog_UpdateFeatures(List<UpdateFeatureDef> featureDefs, UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders) {
			this.ignoredNewsProviders = ignoredNewsProviders;
			closeOnCancel = true;
			doCloseButton = true;
			doCloseX = true;
			forcePause = true;
			draggable = true;
			absorbInputAroundWindow = false;
			resizeable = false;
			GenerateDrawableEntries(featureDefs);
		}

		public override void Close(bool doCloseSound = true) {
			base.Close(doCloseSound);
			foreach (var resolvedTexture in resolvedImages.Values) {
				UnityEngine.Object.Destroy(resolvedTexture);
			}
			resolvedImages.Clear();
		}

		public override void DoWindowContents(Rect inRect) {
			var windowButtonSize = CloseButSize;
			var contentRect = new Rect(0, 0, inRect.width, inRect.height - (windowButtonSize.y + 10f)).ContractedBy(10f);
			GUI.BeginGroup(contentRect);
			var titleRect = new Rect(0f, 0f, contentRect.width, HeaderLabelHeight);
			Text.Font = GameFont.Medium;
			GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
			Widgets.Label(titleRect, "HugsLib_features_title".Translate());
			if (Mouse.IsOver(titleRect)) {
				Widgets.DrawHighlight(titleRect);
				TooltipHandler.TipRegion(titleRect, "HugsLib_features_description".Translate());
			}
			GenUI.ResetLabelAlign();
			if (!anyImagesPending) {
				Text.Font = GameFont.Small;
				var scrollViewRect = new Rect(0f, titleRect.height, contentRect.width, contentRect.height - titleRect.height);
				var scrollBarVisible = totalContentHeight > scrollViewRect.height;
				if (!scrollBarVisible) {
					scrollViewRect.x += ScrollBarWidthMargin/2f;
				}
				var scrollContentWidth = scrollViewRect.width - ScrollBarWidthMargin;
				if(totalContentHeight<0) CalculateContentHeight(scrollContentWidth);
				var scrollContentRect = new Rect(0f, 0f, scrollContentWidth, totalContentHeight);
				Widgets.BeginScrollView(scrollViewRect, ref scrollPosition, scrollContentRect);
				var curY = 0f;
				for (int i = 0; i < entries.Count; i++) {
					var entry = entries[i];
					bool skipDrawing = curY - scrollPosition.y + EntryTitleHeight < 0f || curY - scrollPosition.y > scrollViewRect.height;
					DrawEntryTitle(entry, scrollContentRect.width, ref curY, skipDrawing);
					var indentedContent = new Rect(EntryContentIndent, 0, scrollContentRect.width - EntryContentIndent, scrollContentRect.height);
					DescriptionSegment lastSegment = null;
					for (int j = 0; j < entry.segments.Count; j++) {
						var segment = entry.segments[j];
						skipDrawing = curY - scrollPosition.y + segment.expectedHeight < 0f || curY - scrollPosition.y > scrollViewRect.height;
						segment.Draw(indentedContent, ref curY, resolvedImages, lastSegment, skipDrawing);
						lastSegment = segment;
					}
					curY += EntryFooterHeight;
				}
				Widgets.EndScrollView();
			}
			GUI.EndGroup();
		}

		private void DrawEntryTitle(FeatureEntry entry, float width, ref float curY, bool skipDrawing) {
			if (!skipDrawing) {
				const float toggleSize = Widgets.CheckboxSize;
				var togglePos = new Vector2(EntryTitleLabelPadding, curY + (EntryTitleLabelHeight / 2f - toggleSize / 2f));
				DoIgnoreNewsProviderToggle(togglePos, entry);
				var labelRect = new Rect(togglePos.x + toggleSize, curY,
					width - EntryTitleLinkWidth, EntryTitleLabelHeight).ContractedBy(EntryTitleLabelPadding);
				Text.Font = GameFont.Medium;
				var titleText = entry.def.titleOverride ?? "HugsLib_features_update".Translate(entry.def.modNameReadable, entry.def.version);
				Widgets.Label(labelRect, $"<size={EntryTitleFontSize}>{titleText}</size>");
				Text.Font = GameFont.Small;
				if (entry.def.linkUrl != null) {
					Text.Anchor = TextAnchor.MiddleCenter;
					var linkRect = new Rect(width - EntryTitleLinkWidth, curY, EntryTitleLinkWidth, EntryTitleLabelHeight);
					var prevColor = GUI.color;
					GUI.color = LinkTextColor;
					Widgets.Label(linkRect, "HugsLib_features_link".Translate());
					GUI.color = prevColor;
					GenUI.ResetLabelAlign();
					if (Widgets.ButtonInvisible(linkRect)) {
						Application.OpenURL(entry.def.linkUrl);
					}
					if (Mouse.IsOver(linkRect)) {
						Widgets.DrawHighlight(linkRect);
						TooltipHandler.TipRegion(linkRect, "HugsLib_features_linkDesc".Translate(entry.def.linkUrl));
					}
				}
			}
			curY += EntryTitleLabelHeight;
			if (!skipDrawing) {
				var color = GUI.color;
				GUI.color = TitleLineColor;
				Widgets.DrawLineHorizontal(0f, curY, width);
				GUI.color = color;
			}
			curY += EntryTitleLabelPadding;
		}

		private void DoIgnoreNewsProviderToggle(Vector2 togglePos, FeatureEntry entry) {
			var ownerPackageId = entry.def.modContentPack.PackageId;
			var wasOn = !ignoredNewsProviders.Contains(ownerPackageId);
			var isOn = wasOn;
			Widgets.Checkbox(togglePos, ref isOn);
			if (wasOn != isOn) {
				void ToggleIgnoredState() {
					ignoredNewsProviders.SetIgnored(ownerPackageId, !isOn);
				}
				if (isOn || HugsLibUtility.ShiftIsHeld) {
					ToggleIgnoredState();
				} else {
					Find.WindowStack.Add(new Dialog_Confirm("HugsLib_features_confirmIgnore".Translate(entry.def.modNameReadable), 
						ToggleIgnoredState, false,"HugsLib_features_confirmIgnoreTitle".Translate()));
				}
			}
			var tooltipRect = new Rect(togglePos.x, togglePos.y, Widgets.CheckboxSize, Widgets.CheckboxSize);
			TooltipHandler.TipRegion(tooltipRect, ignoreToggleTip);
		}

		// After writing and testing this whole thing it occurs to me that calculating the height is optional- we could just draw everything once and store curY as totalContentHeight
		private void CalculateContentHeight(float textWidth) {
			var labelStyle = GetLabelStyle();
			totalContentHeight = 0;
			foreach (var entry in entries) {
				totalContentHeight += EntryTitleHeight;
				foreach (var segment in entry.segments) {
					totalContentHeight += segment.CalculateHeight(labelStyle, textWidth, resolvedImages);
				}
			}
			totalContentHeight += EntryFooterHeight*entries.Count;
		}

		private void GenerateDrawableEntries(List<UpdateFeatureDef> defs) {
			entries = new List<FeatureEntry>(defs.Count);
			var requiredImagePairs = new List<(ModContentPack pack, string fileName)>();
			foreach (var def in defs) {
				entries.Add(new FeatureEntry(def, ParseEntryContent(def.content, out IEnumerable<string> requiredImages)));
				foreach (var imageFileName in requiredImages) {
					requiredImagePairs.Add((def.modContentPack, imageFileName));
				}
			}
			if (requiredImagePairs.Count > 0) {
				anyImagesPending = true; // TODO: is this required? Current setup loads images synchronously
				var requiredImagesGroupedByMod = requiredImagePairs
					.GroupBy(pair => pair.pack, pair => pair.fileName);
				HugsLibController.Instance.DoLater.DoNextUpdate(() => {
					// this must be done in the main thread due to Unity API calls
					foreach (var group in requiredImagesGroupedByMod) {
						ResolveImagesForMod(group.Key, group);
					}
					anyImagesPending = false;
				});
			}
		}

		private void ResolveImagesForMod(ModContentPack mod, IEnumerable<string> imageFileNames) {
			foreach (var nameTexturePair in UpdateFeatureImageLoader.LoadImagesForMod(mod, imageFileNames)) {
				resolvedImages[nameTexturePair.Key] = nameTexturePair.Value;
			}
		}
		
		private List<DescriptionSegment> ParseEntryContent(string content, out IEnumerable<string> requiredImages) {
			const char SegmentSeparator = '|';
			const char ArgumentSeparator = ':';
			const char ListSeparator = ',';
			const string ImageSegmentTag = "img:";
			const string CaptionSegmentTag = "caption:";
			var requiredImagesList = new List<string>();
			requiredImages = requiredImagesList;
			try {
				content = content.Replace("\\n", "\n");
				var segmentStrings = content.Split(SegmentSeparator);
				var segmentList = new List<DescriptionSegment>();
				foreach (var segmentString in segmentStrings) {
					DescriptionSegment.SegmentType segmentType;
					string[] images = null;
					string text = null;
					if (segmentString.StartsWith(ImageSegmentTag)) {
						segmentType = DescriptionSegment.SegmentType.Image;
						var parts = segmentString.Split(ArgumentSeparator);
						if (parts[1].Length == 0) continue;
						images = parts[1].Split(ListSeparator).Where(s => s.Length > 0).ToArray();
						for (int i = 0; i < images.Length; i++) {
							requiredImagesList.Add(images[i]);
						}
					} else if (segmentString.StartsWith(CaptionSegmentTag)) {
						if (segmentList.Count == 0 || segmentList[segmentList.Count - 1].type != DescriptionSegment.SegmentType.Image) {
							HugsLibController.Logger.Warning("Improperly formatted feature content. Caption must follow img. Content:" + content);
							continue;
						}
						segmentType = DescriptionSegment.SegmentType.Caption;
						if (segmentString.Length > CaptionSegmentTag.Length) {
							text = segmentString.Substring(CaptionSegmentTag.Length);
						} else {
							text = "";
						}
					} else {
						segmentType = DescriptionSegment.SegmentType.Text;
						text = segmentString;
					}
					var seg = new DescriptionSegment(segmentType) {imageNames = images, text = text};
					segmentList.Add(seg);
				}
				return segmentList;
			} catch (Exception e) {
				HugsLibController.Logger.Warning("Failed to parse UpdateFeatureDef content: {0} \n Exception was: {1}", content, e);
				return new List<DescriptionSegment>();
			}
		}

		private GUIStyle GetLabelStyle() {
			var style = Text.fontStyles[(int)GameFont.Small];
			style.alignment = TextAnchor.UpperLeft;
			style.wordWrap = true;
			return style;
		}

		private class FeatureEntry {
			public readonly UpdateFeatureDef def;
			public readonly List<DescriptionSegment> segments; 

			public FeatureEntry(UpdateFeatureDef def, List<DescriptionSegment> segments) {
				this.def = def;
				this.segments = segments;
			}
		}

		private class DescriptionSegment {
			public enum SegmentType {
				Text, Image, Caption
			}

			public readonly SegmentType type;
			public string[] imageNames;
			public float expectedHeight;
			private float expectedWidth;
			public string text;
			private List<Texture2D> cachedTextures;

			public DescriptionSegment(SegmentType type) {
				this.type = type;
			}

			public float CalculateHeight(GUIStyle style, float width, Dictionary<string, Texture2D> images) {
				if (type == SegmentType.Image && imageNames != null) {
					if (cachedTextures == null) cachedTextures = CacheOwnTextures(images);
					return expectedHeight;
				} else if (type == SegmentType.Caption && text != null) {
					return 0;
				} else if (type == SegmentType.Text && text != null) {
					return expectedHeight = style.CalcHeight(new GUIContent(text), width) + SegmentTextMargin*2;
				}
				return 0;
			}

			public void Draw(Rect rect, ref float curY, Dictionary<string, Texture2D> images, DescriptionSegment previousSegment, bool skipDrawing) {
				if (type == SegmentType.Image && imageNames != null) {
					if (!skipDrawing) {
						if (cachedTextures == null) cachedTextures = CacheOwnTextures(images);
						var curX = rect.x;
						for (int i = 0; i < cachedTextures.Count; i++) {
							var tex = cachedTextures[i];
							var texRect = new Rect(curX, curY + SegmentImageMargin, tex.width, tex.height);
							Widgets.DrawTextureFitted(texRect, tex, 1);
							curX += tex.width + SegmentImageMargin;
						}
					}
					curY += expectedHeight;
				} else if (type == SegmentType.Caption && text != null && previousSegment!=null) {
					// can't skipDrawing this one, since it's drawn at a negative offset
					var offset = previousSegment.expectedWidth + SegmentImageMargin;
					var textRect = new Rect(offset, curY - previousSegment.expectedHeight, rect.width - offset, previousSegment.expectedHeight);
					GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
					Widgets.Label(textRect, text);
					GenUI.ResetLabelAlign();
				} else if (type == SegmentType.Text && text != null) {
					if (!skipDrawing) {
						var textRect = new Rect(rect.x, curY + SegmentTextMargin, rect.width, expectedHeight);
						Widgets.Label(textRect, text);
					}
					curY += expectedHeight;
				}
			}

			private List<Texture2D> CacheOwnTextures(Dictionary<string, Texture2D> images) {
				var cached = new List<Texture2D>(imageNames.Length);
				expectedHeight = expectedWidth = 0;
				for (int i = 0; i < imageNames.Length; i++) {
					Texture2D tex;
					if (!images.TryGetValue(imageNames[i], out tex) || tex == null) continue;
					cached.Add(tex);
					if (tex.height > expectedHeight) expectedHeight = tex.height;
					expectedWidth += tex.width;
				}
				expectedHeight += SegmentImageMargin*2;
				expectedWidth += cached.Count*SegmentImageMargin;
				return cached;
			}
		}
	}
}