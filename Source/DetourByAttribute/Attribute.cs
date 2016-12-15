using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;

namespace HugsLib.DetourByAttribute
{
    [Flags]
    public enum DetourProperty
    {
        None = 0,
        Getter = 1,
        Setter = 2,
        Both = Getter | Setter
    }

    [AttributeUsage( AttributeTargets.Method, AllowMultiple = true, Inherited = false )]
    public class DetourMethodAttribute : Attribute
    {
        // store references needed for detours
        public Type sourceType;
        public string sourceMethodName;
        public MethodInfo sourceMethodInfo;

        // check how this attribute was created
        public bool WasSetByMethodInfo => sourceMethodInfo != null;

        // disable default constructor
        private DetourMethodAttribute() { }

        // get by name
        public DetourMethodAttribute( Type sourceType, string sourceMethodName )
        {
            this.sourceType = sourceType;
            this.sourceMethodName = sourceMethodName;
        }

        // get by methodInfo
        public DetourMethodAttribute( MethodInfo methodInfo )
        {
            sourceType = methodInfo.DeclaringType;
            sourceMethodInfo = methodInfo;
        }
    }


    [AttributeUsage( AttributeTargets.Property, AllowMultiple = true, Inherited = false )]
    public class DetourPropertyAttribute : Attribute
    {
        // store references needed for detours
        public PropertyInfo sourcePropertyInfo;
        public DetourProperty detourProperty;

        // disable default constructor
        private DetourPropertyAttribute() { }

        // get by name
        public DetourPropertyAttribute( Type sourceType, string sourcePropertyName,
                                        DetourProperty detourProperty = DetourProperty.Both )
        {
            sourcePropertyInfo = sourceType.GetProperty( sourcePropertyName, Helpers.AllBindingFlags );
            this.detourProperty = detourProperty;
        }

        // get by propertyInfo
        public DetourPropertyAttribute( PropertyInfo sourcePropertyInfo,
                                        DetourProperty detourProperty = DetourProperty.Both )
        {
            this.sourcePropertyInfo = sourcePropertyInfo;
            this.detourProperty = detourProperty;
        }
    }
}
