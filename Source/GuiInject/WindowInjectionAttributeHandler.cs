using System;
using System.Reflection;
using HugsLib.Source.Attrib;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.GuiInject {
	// Handles WindowInjectionAttribute marked methods and adds them as injections to WindowInjectionManager
	internal class WindowInjectionAttributeHandler {
		// called by AttributeDetector for every method marked with WindowInjectionAttribute
		[DetectableAttributeHandler(typeof (WindowInjectionAttribute))]
		private static void AddWindowInjectorByAttribute(MemberInfo info, Attribute attrib) {
			var injectionAttribute = attrib as WindowInjectionAttribute;
			var destination = info as MethodInfo;
			if (destination == null || injectionAttribute == null) throw new Exception("Null or unexpected argument types!");
			if (!destination.IsStatic || !destination.MethodMatchesSignature(typeof (void), typeof (Window), typeof (Rect))) {
				HugsLibController.Logger.Error("Window injection method ({0}) must be static and match the following signature: {1}", info, WindowInjectionAttribute.ExpectedSignature);
			}
			try {
				var callback = (WindowInjectionManager.DrawInjectedContents) Delegate.CreateDelegate(typeof (WindowInjectionManager.DrawInjectedContents), destination);
				var declaringTypeName = destination.DeclaringType != null ? destination.DeclaringType.FullName : "";
				var injectionId = declaringTypeName + "." + destination.Name;
				WindowInjectionManager.AddInjection(injectionAttribute.WindowType, callback, injectionId, injectionAttribute.Mode);
			} catch (Exception e) {
				HugsLibController.Logger.ReportException(e);
			}
		}	 
	}
}