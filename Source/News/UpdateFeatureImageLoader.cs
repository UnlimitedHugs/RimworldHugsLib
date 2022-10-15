using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace HugsLib.News {
	[StaticConstructorOnStartup]
	internal static class UpdateFeatureImageLoader {
		private const string UpdateFeatureImageBaseFolder = UpdateFeatureManager.UpdateFeatureDefFolder;

		private static readonly string[] PossibleTextureFileExtensions = {
			".png",
			".jpg",
			".jpeg",
			".psd"
		};

		private static readonly Texture2D missingTexturePlaceholder = ContentFinder<Texture2D>.Get(BaseContent.BadTexPath);

		public static IEnumerable<KeyValuePair<string, Texture2D>> LoadImagesForMod(
			ModContentPack modContent, IEnumerable<string> filenamesNoExtension) {
			return filenamesNoExtension.Select(
				filename => new KeyValuePair<string, Texture2D>(filename, GetImage(modContent, filename))
			);
		}

		private static Texture2D GetImage(ModContentPack modContent, string relativeFilePathNoExtension) {
			try {
				var newsFolderTex = TryResolveTextureRelativeToNewsFolder(modContent, relativeFilePathNoExtension);
				if (newsFolderTex != null) return newsFolderTex;
				// try getting the texture from the common resources as fallback
				var resourcesTex = ContentFinder<Texture2D>.Get(relativeFilePathNoExtension, false);
				if (resourcesTex != null) return resourcesTex;
			} catch (Exception e) {
				HugsLibController.Logger.Warning("Exception while loading texture: "+e);	
			}
			// if all else fails, return purple "missing image" texture
			HugsLibController.Logger.Warning($"Failed to resolve update feature texture mod:{modContent.PackageIdPlayerFacing} " +
											$"file:{relativeFilePathNoExtension}, using placeholder");
			return missingTexturePlaceholder;
		}

		private static Texture2D? TryResolveTextureRelativeToNewsFolder(ModContentPack modContent, string relativeFilePathNoExtension) {
			var modSpecificNewsFolderPath = Path.Combine(modContent.RootDir, UpdateFeatureImageBaseFolder);
			if (Directory.Exists(modSpecificNewsFolderPath)) {
				var imageFilePathNoExtension = Path.Combine(modSpecificNewsFolderPath, relativeFilePathNoExtension);
				foreach (var possibleFileExtension in PossibleTextureFileExtensions) {
					var newsFolderImageFileInfo = new FileInfo(imageFilePathNoExtension + possibleFileExtension);
					if (newsFolderImageFileInfo.Exists) {
						var modContentTex = LoadTextureFromFile(newsFolderImageFileInfo);
						return modContentTex;
					}
				}
			}
			return null;
		}

		private static Texture2D LoadTextureFromFile(FileInfo file) {
			try {
				var fileBytes = File.ReadAllBytes(file.FullName);
				var tex = new Texture2D(2, 2, TextureFormat.Alpha8, true);
				tex.LoadImage(fileBytes);
				tex.name = Path.GetFileNameWithoutExtension(file.Name);
				tex.Compress(true);
				tex.filterMode = FilterMode.Bilinear;
				tex.anisoLevel = 2;
				tex.Apply(true, true);
				return tex;
			} catch (Exception e) {
				throw new IOException($"Failed to load texture at path \"{file.FullName}\"", e);
			}
		}
	}
}