using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HugsLib.News {
	/**
	 * Displays a list to update feature defs with basic image and formatting support. See UpdateFeatureDef for proper syntax.
	 */
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


		private readonly Color TitleLineColor = new Color(0.3f, 0.3f, 0.3f);
		private readonly Color LinkTextColor = new Color(.7f, .7f, 1f);
		private readonly Dictionary<string, Texture2D> resolvedImages = new Dictionary<string, Texture2D>(); 
		private List<FeatureEntry> entries;
		private float totalContentHeight = -1;
		private Vector2 scrollPosition;
		private List<string> pendingImages;

		public override Vector2 InitialSize {
			get { return new Vector2(650f, 700f); }
		}

		public Dialog_UpdateFeatures(List<UpdateFeatureDef> featureDefs) {
			closeOnEscapeKey = true;
			doCloseButton = true;
			doCloseX = true;
			forcePause = true;
			draggable = true;
			absorbInputAroundWindow = false;
			resizeable = false;
			GenerateDrawableEntries(featureDefs);
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
			if (pendingImages == null) {
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
				var labelRect = new Rect(0f, curY, width - EntryTitleLinkWidth, EntryTitleLabelHeight).ContractedBy(EntryTitleLabelPadding);
				Text.Font = GameFont.Medium;
				var titleText = entry.def.titleOverride ?? "HugsLib_features_update".Translate(entry.def.modNameReadable, entry.def.assemblyVersion);
				Widgets.Label(labelRect, string.Format("<size={0}>{1}</size>", EntryTitleFontSize, titleText));
				Text.Font = GameFont.Small;
				if (entry.def.linkUrl != null) {
					Text.Anchor = TextAnchor.MiddleCenter;
					var linkRect = new Rect(width - EntryTitleLinkWidth, curY, EntryTitleLinkWidth, EntryTitleLabelHeight);
					var prevColor = GUI.color;
					GUI.color = LinkTextColor;
					Widgets.Label(linkRect, "HugsLib_features_link".Translate());
					GUI.color = prevColor;
					GenUI.ResetLabelAlign();
					if (Widgets.ButtonInvisible(linkRect, true)) {
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
			pendingImages = new List<string>();
			foreach (var def in defs) {
				entries.Add(new FeatureEntry(def, ParseEntryContent(def.content)));
			}
			LongEventHandler.ExecuteWhenFinished(ResolveImages);
		}

		private void ResolveImages() {
			foreach (var imageName in pendingImages) {
				var tex = ContentFinder<Texture2D>.Get(imageName, false);
				if (tex == null) {
					HugsLibController.Logger.Warning("No texture named {0} for use in update feature text", imageName);
				}
				resolvedImages[imageName] = tex;
			}
			pendingImages = null;
		}
		
		private List<DescriptionSegment> ParseEntryContent(string content) {
			const char SegmentSeparator = '|';
			const char ArgumentSeparator = ':';
			const char ListSeparator = ',';
			const string ImageSegmentTag = "img:";
			const string CaptionSegmentTag = "caption:";
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
							pendingImages.Add(images[i]);
						}
					} else if (segmentString.StartsWith(CaptionSegmentTag)) {
						if (segmentList.Count == 0 || segmentList[segmentList.Count - 1].type != DescriptionSegment.SegmentType.Image) {
							HugsLibController.Logger.Warning("Improperly formatted feature content. Caption must follow img. Content:" + content);
							continue;
						}
						segmentType = DescriptionSegment.SegmentType.Caption;
						var parts = segmentString.Split(ArgumentSeparator);
						text = parts[1];
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
					// can't skipDrawing this one, since it's drawn at a negative offest
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