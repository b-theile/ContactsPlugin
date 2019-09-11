using Plugin.Contacts.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Linq.Expressions;

namespace Plugin.Contacts
{
    /// <summary>
    /// Contacts AddressBook
    /// </summary>
	public sealed class AddressBook : IQueryable<Contact>
	{
        /// <summary>
        /// Initialize Addressbook
        /// </summary>
		public AddressBook()
		{
			_provider = new ContactQueryProvider();
		}

        /// <summary>
        /// Load Contacts by id
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns></returns>
        public Contact Load(string id) => throw new NotSupportedException();

        /// <summary>
        /// Element type
        /// </summary>
        public Type ElementType => typeof(Contact);

        /// <summary>
        /// Expression
        /// </summary>
		public Expression Expression => Expression.Constant(this);

        private readonly ContactQueryProvider _provider;
        /// <summary>
        /// Provider
        /// </summary>
		public IQueryProvider Provider => _provider;

        /// <summary>
        /// Contacts as Enumerator
        /// </summary>
        /// <returns></returns>
		public IEnumerator<Contact> GetEnumerator() => _provider.GetContacts().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();        
	}
}
