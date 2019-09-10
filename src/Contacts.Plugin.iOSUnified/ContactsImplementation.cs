using Plugin.Contacts.Abstractions;
using System.Threading.Tasks;
using System.Linq;
using Foundation;

namespace Plugin.Contacts
{
    /// <summary>
    /// Implementation for Contacts
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ContactsImplementation : IContacts
    {
        private AddressBook _addressBook;
        private AddressBook AddressBook => _addressBook ?? (_addressBook = new AddressBook());

        /// <summary>
        /// Request Permission
        /// </summary>
        /// <returns></returns>
        public Task<bool> RequestPermission() => AddressBook.RequestPermission();

        /// <summary>
        /// Contacts
        /// </summary>
        public IQueryable<Contact> Contacts => (IQueryable<Contact>)AddressBook;

        /// <summary>
        /// Load Contact
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Contact LoadContact(string id) => AddressBook.Load(id);

        /// <summary>
        /// Load Supported
        /// </summary>
        public bool LoadSupported => true;

        /// <summary>
        /// Prefer Contact Aggregation
        /// </summary>
        public bool PreferContactAggregation { get; set; }

        /// <summary>
        /// Aggregate Contacts Supported
        /// </summary>
        public bool AggregateContactsSupported => true;

        /// <summary>
        /// is Single Contacts Supported
        /// </summary>
        public bool SingleContactsSupported => true;

        /// <summary>
        /// Is Read Only
        /// </summary>
        public bool IsReadOnly => true;
    }
}