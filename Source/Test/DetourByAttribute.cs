#if TEST_DETOUR
using System;
using HugsLib.Source.Detour;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Test
{
    public class DetourByAttribute : ModBase
    {
        public override string ModIdentifier
        {
            get { return "DetourByAttribute"; }
        }

        public static DetourByAttribute Instance { get; private set; }

        internal static new ModLogger Logger = new ModLogger( "DetoursByAttribute");

        public DetourByAttribute()
        {
            Instance = this;
        }

        // simple methods
        [DetourMethod( typeof( DetourTestSources ), "PublicInstanceMethod" )]
        public void PublicInstanceMethod()
        {
            Logger.Message( "public instance method" );
        }

        [DetourMethod( typeof( DetourTestSources ), "PrivateInstanceMethod" )]
        private void PrivateInstanceMethod()
        {
            Logger.Message( "private instance method" );
        }

        [DetourMethod( typeof( DetourTestSources ), "PublicStaticMethod" )]
        public static void PublicStaticMethod()
        {
            Logger.Message( "public static method" );
        }

        [DetourMethod( typeof( DetourTestSources ), "PrivateStaticMethod" )]
        private static void PrivateStaticMethod()
        {
            Logger.Message( "private static method" );
        }

        // parameter overloads
        [DetourMethod( typeof( DetourTestSources ), "Overload" )]
        public void Overload( string asd, string qwe )
        {
            Logger.Message( "overload string" );
        }

        [DetourMethod( typeof( DetourTestSources ), "Overload" )]
        public void Overload( int asd, int qwe )
        {
            Logger.Message( "overload int" );
        }

        // properties
        [DetourProperty( typeof( DetourTestSources ), "GetterOnly", DetourProperty.Getter )]
        public string GetterOnly
        {
            get
            {
                Logger.Message( "public getterOnly getter" );
                return "asd";
            }
            set
            {
                Logger.Error( "public getterOnly setter" );
            }
        }

        [DetourProperty( typeof( DetourTestSources ), "SetterOnly", DetourProperty.Setter )]
        public string SetterOnly
        {
            get
            {
                Logger.Error( "public setterOnly getter" );
                return "asd";
            }
            set
            {
                Logger.Message( "public setterOnly setter" );
            }
        }

        [DetourProperty( typeof( DetourTestSources ), "Both" )]
        public string Both
        {
            get
            {
                Logger.Message( "public both getter" );
                return "asd";
            }
            set
            {
                Logger.Message( "public both setter" );
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            // instance tests
            Logger.Message( "Running tests..." );
            Type sourceType = typeof( DetourTestSources );
            DetourTestSources sources = new DetourTestSources();
            sources.PublicInstanceMethod();
            sourceType.GetMethod( "PrivateInstanceMethod", Helpers.AllBindingFlags ).Invoke( sources, null );

            // static tests
            DetourTestSources.PublicStaticMethod();
            sourceType.GetMethod( "PrivateStaticMethod", Helpers.AllBindingFlags ).Invoke( null, null );

            // overloads
            sources.Overload( 1, 1 );
            sources.Overload( "asd", "qwe" );

            // properties
            var x = sources.GetterOnly;
            sources.GetterOnly = "asd";
            x = sources.SetterOnly;
            sources.SetterOnly = "asd";
            x = sources.Both;
            sources.Both = "asd";
        }
    }

    public class DetourTestSources
    {
        public static ModLogger Logger = new ModLogger( "DetourByAttribute" );

        // simple methods
        public void PublicInstanceMethod()
        {
            Logger.Error( "public instance method" );
        }
        private void PrivateInstanceMethod()
        {
            Logger.Error( "private instance method" );
        }
        public static void PublicStaticMethod()
        {
            Logger.Error( "public static method" );
        }
        private static void PrivateStaticMethod()
        {
            Logger.Error( "private static method" );
        }

        // parameter overloads
        public void Overload( string asd, string qwe )
        {
            Logger.Error( "public overload string" );
        }
        public void Overload( int asd, int qwe )
        {
            Logger.Error( "public overload int" );
        }

        // properties
        public string GetterOnly
        {
            get
            {
                Logger.Error( "public getterOnly getter" );
                return "asd";
            }
            set
            {
                Logger.Message( "public getterOnly setter" );
            }
        }

        public string SetterOnly
        {
            get
            {
                Logger.Message( "public setterOnly getter" );
                return "asd";
            }
            set
            {
                Logger.Error( "public setterOnly setter" );
            }
        }
        public string Both
        {
            get
            {
                Logger.Error( "public both getter" );
                return "asd";
            }
            set
            {
                Logger.Error( "public both setter" );
            }
        }
    }
}
#endif