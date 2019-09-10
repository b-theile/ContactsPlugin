using Plugin.Contacts.Abstractions;
using System;

namespace Plugin.Contacts
{
    /// <summary>
    /// Cross platform Contacts implemenations
    /// </summary>
    public static class CrossContacts
    {
        private static readonly Lazy<IContacts> _implementation = new Lazy<IContacts>(() => CreateContacts(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Current settings to use
        /// </summary>
        public static IContacts Current
        {
            get
            {
                var ret = _implementation.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        static IContacts CreateContacts()
        {
#if NETSTANDARD2_0
            return null;
#else
            return new ContactsImplementation();
#endif
        }

        internal static Exception NotImplementedInReferenceAssembly()
        {
            return new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
        }
    }
}
