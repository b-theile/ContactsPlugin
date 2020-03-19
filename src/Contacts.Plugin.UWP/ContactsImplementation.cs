using Plugin.Contacts.Abstractions;
using Xamarin.Essentials;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace Plugin.Contacts
{
    /// <summary>
    /// Implementation for Contacts
    /// </summary>
    public class ContactsImplementation : IContacts
	{
		/// <summary>
		/// Request permissions for Contacts
		/// </summary>
		/// <returns></returns>
		public async Task<bool> RequestPermission()
		{

#warning TODO
            //var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permissions.Abstractions.Permission.Contacts).ConfigureAwait(false);
            //if (status != Permissions.Abstractions.PermissionStatus.Granted)
            //{
            //	Debug.WriteLine("Currently does not have Contacts permissions, requesting permissions");

            //	var request = await CrossPermissions.Current.RequestPermissionsAsync(Permissions.Abstractions.Permission.Contacts);

            //	if (request[Permissions.Abstractions.Permission.Contacts] != Permissions.Abstractions.PermissionStatus.Granted)
            //	{
            //                 Debug.WriteLine("Contacts permission denied, can not get positions async.");
            //		return false;
            //	}
            //}

            return true;
		}

        private AddressBook _addressBook;
        private AddressBook AddressBook => _addressBook ?? (_addressBook = new AddressBook());

        /// <summary>
        /// Contacts
        /// </summary>
        public IQueryable<Contact> Contacts => AddressBook;

        /// <summary>
        /// Load Contact by id
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns>Contact</returns>
        public Contact LoadContact(string id) => AddressBook.Load(id);

        /// <summary>
        /// Load Supported
        /// </summary>
        public bool LoadSupported => false;

        /// <summary>
        /// Prefer Contact Aggregation
        /// </summary>
        public bool PreferContactAggregation { get; set; }

        /// <summary>
        /// Aggregate Contacts Supported
        /// </summary>
		public bool AggregateContactsSupported => true;

        /// <summary>
        /// Single Contacts Supported
        /// </summary>
		public bool SingleContactsSupported => false;

        /// <summary>
        /// Is ReadOnly
        /// </summary>
		public bool IsReadOnly => true;		
	}
}