using System;
using System.Collections.Generic;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Logs {
	/// <summary>
	/// The front-end for LogPublisher.
	/// Shows the status of the upload operation, provides controls and shows the produced URL.
	/// </summary>
	[StaticConstructorOnStartup]
	public class Dialog_PublishLogs : Window {
		private const float StatusLabelHeight = 60f;
		private const int MaxResultUrlLength = 32;
		private static readonly Texture2D UrlBackgroundTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.25f, 0.25f, 0.17f, 0.85f));
		private readonly Vector2 CopyButtonSize = new Vector2(100f, 40f);
		private readonly Vector2 ControlButtonSize = new Vector2(150f, 40f);
		private readonly Dictionary<LogPublisher.PublisherStatus, StatusLabelEntry> statusMessages = new Dictionary<LogPublisher.PublisherStatus, StatusLabelEntry> {
			{LogPublisher.PublisherStatus.Ready, new StatusLabelEntry("", false)},
			{LogPublisher.PublisherStatus.Uploading, new StatusLabelEntry("HugsLib_logs_uploading", true)},
			{LogPublisher.PublisherStatus.Shortening, new StatusLabelEntry("HugsLib_logs_shortening", true)},
			{LogPublisher.PublisherStatus.Done, new StatusLabelEntry("HugsLib_logs_uploaded", false)},
			{LogPublisher.PublisherStatus.Error, new StatusLabelEntry("HugsLib_logs_uploadError", false)}
		};

		public override Vector2 InitialSize {
			get { return new Vector2(500, 250); }
		}

		private readonly LogPublisher publisher;

		public Dialog_PublishLogs() {
			closeOnEscapeKey = true;
			doCloseButton = false;
			doCloseX = true;
			forcePause = true;
			onlyOneOfTypeAllowed = true;
			focusWhenOpened = true;
			draggable = true;
			publisher = HugsLibController.Instance.LogUploader;
		}

		public override void DoWindowContents(Rect inRect) {
			Text.Font = GameFont.Medium;
			var titleRect = new Rect(inRect.x, inRect.y, inRect.width, 40);
			Widgets.Label(titleRect, "HugsLib_logs_publisherTitle".Translate());
			Text.Font = GameFont.Small;
			var labelEntry = statusMessages[publisher.Status];
			var statusLabelText = labelEntry.requiresEllipsis ? labelEntry.labelKey.Translate(GenText.MarchingEllipsis(Time.realtimeSinceStartup)) : labelEntry.labelKey.Translate();
			if (publisher.Status == LogPublisher.PublisherStatus.Error) {
				statusLabelText = string.Format(statusLabelText, publisher.ErrorMessage);
			}
			var statusLabelRect = new Rect(inRect.x, inRect.y + titleRect.height, inRect.width, StatusLabelHeight);
			Widgets.Label(statusLabelRect, statusLabelText);
			if (publisher.Status == LogPublisher.PublisherStatus.Done) {
				var urlAreaRect = new Rect(inRect.x, statusLabelRect.y + statusLabelRect.height, inRect.width, CopyButtonSize.y);
				GUI.DrawTexture(urlAreaRect, UrlBackgroundTex);
				var urlLabelRect = new Rect(urlAreaRect.x, urlAreaRect.y, urlAreaRect.width - CopyButtonSize.x, urlAreaRect.height);
				Text.Font = GameFont.Medium;
				var prevAnchor = Text.Anchor;
				Text.Anchor = TextAnchor.MiddleCenter;
				var croppedResultUrl = publisher.ResultUrl;
				if (croppedResultUrl.Length > MaxResultUrlLength) {
					// crop the url in case shortening has failed and the original url is displayed
					croppedResultUrl = croppedResultUrl.Substring(0, MaxResultUrlLength)+"...";
				}
				Widgets.Label(urlLabelRect, croppedResultUrl);
				Text.Anchor = prevAnchor;
				Text.Font = GameFont.Small;
				var copyBtnRect = new Rect(inRect.width - CopyButtonSize.x, urlAreaRect.y, CopyButtonSize.x, CopyButtonSize.y);
				if (Widgets.ButtonText(copyBtnRect, "HugsLib_logs_copy".Translate())) {
					HugsLibUtility.CopyToClipboard(publisher.ResultUrl);
				}
			}
			var bottomLeftBtnRect = new Rect(inRect.x, inRect.height - ControlButtonSize.y, ControlButtonSize.x, ControlButtonSize.y);
			if (publisher.Status == LogPublisher.PublisherStatus.Error) {
				if (Widgets.ButtonText(bottomLeftBtnRect, "HugsLib_logs_retryBtn".Translate())) {
					publisher.BeginUpload();
				}
			} else if (publisher.Status == LogPublisher.PublisherStatus.Done) {
				if (Widgets.ButtonText(bottomLeftBtnRect, "HugsLib_logs_browseBtn".Translate())) {
					Application.OpenURL(publisher.ResultUrl);
				}
			}
			var bottomRightBtnRect = new Rect(inRect.width - ControlButtonSize.x, inRect.height - ControlButtonSize.y, ControlButtonSize.x, ControlButtonSize.y);
			if (publisher.Status == LogPublisher.PublisherStatus.Uploading || publisher.Status == LogPublisher.PublisherStatus.Shortening) {
				if (Widgets.ButtonText(bottomRightBtnRect, "HugsLib_logs_abortBtn".Translate())) {
					publisher.AbortUpload();
				}
			} else {
				if (Widgets.ButtonText(bottomRightBtnRect, "CloseButton".Translate())) {
					Close();
				}
			}
		}

		private class StatusLabelEntry {
			public readonly string labelKey;
			public readonly bool requiresEllipsis;

			public StatusLabelEntry(string labelKey, bool requiresEllipsis) {
				this.labelKey = labelKey;
				this.requiresEllipsis = requiresEllipsis;
			}
		}
	}
}