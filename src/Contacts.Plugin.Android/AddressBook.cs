//
//  Copyright 2011-2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.Provider;
using Plugin.Contacts.Abstractions;

namespace Plugin.Contacts
{
    [Android.Runtime.Preserve(AllMembers=true)]
    public sealed class AddressBook
      : IQueryable<Contact>
    {
        public AddressBook(Context context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            this._content = context.ContentResolver;
            this._resources = context.Resources;
            this._contactsProvider = new ContactQueryProvider(context.ContentResolver, context.Resources);
        }


        public bool PreferContactAggregation
        {
            get => !this._contactsProvider.UseRawContacts;
            set => this._contactsProvider.UseRawContacts = !value;
        }


        public IEnumerator<Contact> GetEnumerator()
        {
            return ContactHelper.GetContacts(!PreferContactAggregation, this._content, this._resources).GetEnumerator();
        }

        /// <summary>
        /// Attempts to load a contact for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The <see cref="Contact"/> if found, <c>null</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="id"/> is empty.</exception>
        public Contact Load(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(id.Trim()))
                throw new ArgumentException("Invalid ID", nameof(id));

            Android.Net.Uri curi; string column;
            if (PreferContactAggregation)
            {
                curi = ContactsContract.Contacts.ContentUri;
                column = ContactsContract.ContactsColumns.LookupKey;
            }
            else
            {
                curi = ContactsContract.RawContacts.ContentUri;
                column = ContactsContract.RawContactsColumns.ContactId;
            }

            ICursor c = null;
            try
            {
                c = this._content.Query(curi, null, column + " = ?", new[] { id }, null);
                return (c.MoveToNext() ? ContactHelper.GetContact(!PreferContactAggregation, this._content, this._resources, c) : null);
            }
            finally
            {
                if (c != null)
                    c.Close(); // .Deactivate();
            }
        }

        //public Contact SaveNew (Contact contact)
        //{
        //    if (contact == null)
        //        throw new ArgumentNullException ("contact");
        //    if (contact.Id != null)
        //        throw new ArgumentException ("Contact is not new", "contact");

        //    throw new NotImplementedException();
        //}

        //public Contact SaveExisting (Contact contact)
        //{
        //    if (contact == null)
        //        throw new ArgumentNullException ("contact");
        //    if (String.IsNullOrWhiteSpace (contact.Id))
        //        throw new ArgumentException ("Contact is not existing");

        //    throw new NotImplementedException();

        //    return Load (contact.Id);
        //}

        //public Contact Save (Contact contact)
        //{
        //    if (contact == null)
        //        throw new ArgumentNullException ("contact");

        //    return (String.IsNullOrWhiteSpace (contact.Id) ? SaveNew (contact) : SaveExisting (contact));
        //}

        //public void Delete (Contact contact)
        //{
        //    if (contact == null)
        //        throw new ArgumentNullException ("contact");
        //    if (!String.IsNullOrWhiteSpace (contact.Id))
        //        throw new ArgumentException ("Contact is not a persisted instance", "contact");

        //    // TODO: Does this cascade?
        //    this.content.Delete (ContactsContract.RawContacts.ContentUri, ContactsContract.RawContactsColumns.ContactId + " = ?", new[] { contact.Id });
        //}

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        Type IQueryable.ElementType => typeof(Contact);

        Expression IQueryable.Expression => Expression.Constant(this);

        IQueryProvider IQueryable.Provider => this._contactsProvider;

        private readonly ContactQueryProvider _contactsProvider;
        private readonly ContentResolver _content;
        private readonly Resources _resources;
    }
}