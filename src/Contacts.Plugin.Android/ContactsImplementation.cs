using Plugin.Contacts.Abstractions;
using Plugin.Permissions;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace Plugin.Contacts
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    [Android.Runtime.Preserve(AllMembers = true)]
    public class ContactsImplementation : IContacts
    {
        /// <summary>
        /// Request Permission
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RequestPermission()
        {
            var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permissions.Abstractions.Permission.Contacts).ConfigureAwait(false);
            if (status != Permissions.Abstractions.PermissionStatus.Granted)
            {
                Console.WriteLine("Currently does not have Contacts permissions, requesting permissions");

                var request = await CrossPermissions.Current.RequestPermissionsAsync(Permissions.Abstractions.Permission.Contacts);

                if (request[Permissions.Abstractions.Permission.Contacts] != Permissions.Abstractions.PermissionStatus.Granted)
                {
                    Console.WriteLine("Contacts permission denied, can not get positions async.");
                    return false;
                }
            }

            return true;
        }

        private AddressBook _addressBook;
        /// <summary>
        /// Contacts
        /// </summary>
        public IQueryable<Contact> Contacts => (IQueryable<Contact>)AddressBook;

        private AddressBook AddressBook => _addressBook ?? (_addressBook = new AddressBook(Android.App.Application.Context));

        /// <summary>
        /// Load contacts by id
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns></returns>
        public Contact LoadContact(string id) => AddressBook.Load(id);

        /// <summary>
        /// Load Supported
        /// </summary>
        public bool LoadSupported => true;

        /// <summary>
        /// Prefer ContactAggregation
        /// </summary>
        public bool PreferContactAggregation
        {
            get => AddressBook.PreferContactAggregation;
            set => AddressBook.PreferContactAggregation = value;
        }

        /// <summary>
        /// Aggregate Contacts Supported
        /// </summary>
        public bool AggregateContactsSupported => true;

        /// <summary>
        /// Single Contacts Supported
        /// </summary>
        public bool SingleContactsSupported => true;

        /// <summary>
        /// Is ReadOnly
        /// </summary>
        public bool IsReadOnly => true;
    }
}