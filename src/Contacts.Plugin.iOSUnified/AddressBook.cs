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
using System.Linq;
using System.Linq.Expressions;
using AddressBook;
using UIKit;
using Foundation;
using System.Collections.Generic;
using System.Threading.Tasks;
using Plugin.Contacts.Abstractions;

namespace Plugin.Contacts
{
    /// <summary>
    /// Addressbook
    /// </summary>
    [Preserve(AllMembers = true)]
    public class AddressBook : IQueryable<Contact>
    {
        /// <summary>
        /// Addressbook will be provided
        /// </summary>
        public AddressBook()
        {
            this._provider = new ContactQueryProvider(this._addressBook);
        }

        /// <summary>
        /// Resquest permission for contacts usage
        /// </summary>
        /// <returns>RequestAccess</returns>
        public Task<bool> RequestPermission()
        {
            var info = NSBundle.MainBundle.InfoDictionary;

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                if (!info.ContainsKey(new NSString("NSContactsUsageDescription")))
                    throw new UnauthorizedAccessException("On iOS 10 and higher you must set NSContactsUsageDescription in your Info.plist file to enable Authorization Requests for Photo Library access!");
            }

            var tcs = new TaskCompletionSource<bool>();
            if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
            {
                var status = ABAddressBook.GetAuthorizationStatus();
                if (status == ABAuthorizationStatus.Denied || status == ABAuthorizationStatus.Restricted)
                    tcs.SetResult(false);
                else
                {
                    if (this._addressBook == null)
                    {
                        this._addressBook = new ABAddressBook();
                        this._provider = new ContactQueryProvider(this._addressBook);
                    }

                    if (status == ABAuthorizationStatus.NotDetermined)
                    {
                        this._addressBook.RequestAccess((s, e) =>
                        {
                            tcs.SetResult(s);
                            if (!s)
                            {
                                this._addressBook.Dispose();
                                this._addressBook = null;
                                this._provider = null;
                            }
                        });
                    }
                    else
                        tcs.SetResult(true);
                }
            }
            else
                tcs.SetResult(true);

            return tcs.Task;
        }

        /// <summary>
        /// Get enumerator of all people in contacts
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Contact> GetEnumerator()
        {
            CheckStatus();
            return this._addressBook.GetPeople().Select(ContactHelper.GetContact).GetEnumerator();
        }

        /// <summary>
        /// Load contact by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Contact Load(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            CheckStatus();

            if (!int.TryParse(id, out int rowId))
                throw new ArgumentException("Not a valid contact ID", nameof(id));

            ABPerson person = this._addressBook.GetPerson(rowId);
            if (person == null)
                return null;

            return ContactHelper.GetContact(person);
        }

        private ABAddressBook _addressBook;
        private IQueryProvider _provider;

        private void CheckStatus()
        {

            var info = NSBundle.MainBundle.InfoDictionary;

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                if (!info.ContainsKey(new NSString("NSContactsUsageDescription")))
                    throw new UnauthorizedAccessException("On iOS 10 and higher you must set NSContactsUsageDescription in your Info.plist file to enable Authorization Requests for Photo Library access!");
            }

            if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
            {
                var status = ABAddressBook.GetAuthorizationStatus();
                if (status != ABAuthorizationStatus.Authorized)
                    throw new System.Security.SecurityException("AddressBook has not been granted permission");
            }

            if (this._addressBook == null)
            {
                this._addressBook = new ABAddressBook();
                this._provider = new ContactQueryProvider(this._addressBook);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        Type IQueryable.ElementType => typeof(Contact);

        Expression IQueryable.Expression => Expression.Constant(this);

        IQueryProvider IQueryable.Provider => this._provider;

    }
}