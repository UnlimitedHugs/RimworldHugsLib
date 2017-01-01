using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib.Source.Detour;
using HugsLib.Utils;

namespace HugsLib.Source.Attrib {
	/**
	 * Detects types and members with specific attributes and calls handlers marked with 
	 * DetectableAttributeHandler to do something with the marked type or member.
	 * Detectable attributes must implement IDetectableAttribute.
	 * New types and members are detected after a def reload and each type/member is processed only once.
	 */
	internal static class AttributeDetector {
		private static readonly HashSet<Type> seenTypes = new HashSet<Type>();
		private static readonly List<AttributePair> knownHandlers = new List<AttributePair>();
		
		public static void ProcessNewTypes() {
			try {
				// get all types from all loaded assemblies
				var allTypes = new List<Type>();
				foreach (var assembly in HugsLibUtility.GetAllActiveAssemblies()) {
					try {
						allTypes.AddRange(assembly.GetTypes());
					} catch {
						// just in case
					}
				}

				// get all types and members marked with IDetectableAttribute attributes
				var thingsWithAttributes = new List<AttributePair>();
				foreach (var type in allTypes) {
					try {
						// skip types that were already processed
						if (seenTypes.Contains(type)) continue;
						seenTypes.Add(type);
						// add type if attributed
						var typeAttr = TryGetFirstRelevantAttribute(type);
						if (typeAttr != null) {
							thingsWithAttributes.Add(new AttributePair(type, typeAttr));
						}
						// get attributed members from type
						var members = type.GetMembers(Helpers.AllBindingFlags);
						foreach (var member in members) {
							var memberAttr = TryGetFirstRelevantAttribute(member);
							if (memberAttr != null) {
								thingsWithAttributes.Add(new AttributePair(member, memberAttr));
							}
						}
					} catch {
						// just in case
					}
				}
				// find and validate new handlers
				var candidateAttributeHandlers = thingsWithAttributes.Where(pair => pair.attribute is DetectableAttributeHandler).ToList();
				ValidateAttributeHandlers(candidateAttributeHandlers);
				knownHandlers.AddRange(candidateAttributeHandlers);
				// make handler lookup for faster processing
				var handlersLookup = PrepareHandlerLookup(knownHandlers);
				// process all attributes
				CallHandlersForAttributes(thingsWithAttributes, handlersLookup);
			} catch (Exception e) {
				HugsLibController.Logger.ReportException(e);
			}
		}

		private static void CallHandlersForAttributes(List<AttributePair> thingsWithAttributes, Dictionary<Type, List<Action<MemberInfo, Attribute>>> handlersLookup) {
			foreach (var attributePair in thingsWithAttributes) {
				List<Action<MemberInfo, Attribute>> handlerList;
				// get list of handlers by attribute type
				handlersLookup.TryGetValue(attributePair.attribute.GetType(), out handlerList);
				if(handlerList == null) continue;
				// call handlers
				for (int i = 0; i < handlerList.Count; i++) {
					var handler = handlerList[i];
					try {
						handler(attributePair.memberOrType, attributePair.attribute);
					} catch (Exception e) {
						HugsLibController.Logger.Error("Exception while calling attribute handler ({0}). Exception was: {1}", handler.Method.FullName(), e);						
					}
				}
			}	
		}

		// this creates lists of delegates to call for each attribute type
		private static Dictionary<Type, List<Action<MemberInfo, Attribute>>> PrepareHandlerLookup(List<AttributePair> attributeHandlers) {
			var lookup = new Dictionary<Type, List<Action<MemberInfo, Attribute>>>();
			foreach (var handler in attributeHandlers) {
				List<Action<MemberInfo, Attribute>> handlersList;
				var handlerAttribute = (DetectableAttributeHandler) handler.attribute;
				lookup.TryGetValue(handlerAttribute.detectedAttributeType, out handlersList);
				if (handlersList == null) {
					handlersList = new List<Action<MemberInfo, Attribute>>();
					lookup.Add(handlerAttribute.detectedAttributeType, handlersList);
				}
				try {
					var action = (Action<MemberInfo, Attribute>)Delegate.CreateDelegate(typeof(Action<MemberInfo, Attribute>), (MethodInfo)handler.memberOrType);
					handlersList.Add(action);
				} catch(Exception e) { // just in case
					HugsLibController.Logger.Error("Exception during PrepareHandlerLookup ({0}), Exception was: {1}", handler.memberOrType, e);						
				}
			}
			return lookup;
		}

		// get first attribute that implements IDetectableAttribute
		private static Attribute TryGetFirstRelevantAttribute(MemberInfo member) {
			try {
				var attrs = member.GetCustomAttributes(typeof(IDetectableAttribute), false);
				if (attrs.Length > 0) {
					return (Attribute)attrs[0];
				}
				return null;
			} catch {
				return null;
			}
		}

		// check callback methods for proper signature
		private static void ValidateAttributeHandlers(List<AttributePair> handlers){
			for (int i = handlers.Count-1; i >= 0; i--) { // iterate backwards to allow for removal of items
				var handler = handlers[i];
				var method = handler.memberOrType as MethodInfo;
				if (method == null || !method.MethodMatchesSignature(typeof(void), typeof (MemberInfo), typeof(Attribute))) {
					handlers.RemoveAt(i);
					HugsLibController.Logger.Error("Improper DetectableAttributeHandler ({0}). Must be static and match signature: {1}", method.FullName(), DetectableAttributeHandler.ExpectedSignature);
				}
			}
		}

		private struct AttributePair {
			public readonly MemberInfo memberOrType;
			public readonly Attribute attribute;

			public AttributePair(MemberInfo memberOrType, Attribute attribute) {
				this.memberOrType = memberOrType;
				this.attribute = attribute;
			}

			public override string ToString() {
				return string.Format("({0}, {1})", 
					memberOrType != null ? memberOrType.Name : "null", 
					attribute!=null?attribute.GetType().FullName:"null");
			}
		}
	}
}