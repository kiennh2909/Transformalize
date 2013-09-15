﻿#region License

// /*
// Transformalize - Replicate, Transform, and Denormalize Your Data...
// Copyright (C) 2013 Dale Newman
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace Transformalize.Libs.RazorEngine.Templating
{
    /// <summary>
    ///     Defines a dynamic view bag.
    /// </summary>
    public class DynamicViewBag : DynamicObject
    {
        #region Fields

        private readonly IDictionary<string, object> _dict = new Dictionary<string, object>();

        #endregion

        #region DynamicObject Overrides

        /// <summary>
        ///     Gets the set of dynamic member names.
        /// </summary>
        /// <returns>
        ///     An instance of <see cref="IEnumerable{String}" />.
        /// </returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _dict.Keys;
        }

        /// <summary>
        ///     Attempts to read a dynamic member from the object.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="result">The result instance.</param>
        /// <returns>True, always.</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_dict.ContainsKey(binder.Name))
                result = _dict[binder.Name];
            else
                result = null;

            return true;
        }

        /// <summary>
        ///     Attempts to set a value on the object.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>True, always.</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (_dict.ContainsKey(binder.Name))
                _dict[binder.Name] = value;
            else
                _dict.Add(binder.Name, value);

            return true;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        ///     Add a value to this instance of DynamicViewBag.
        /// </summary>
        /// <param name="propertyName">
        ///     The property name through which this value can be get/set.
        /// </param>
        /// <param name="value">
        ///     The value that will be assigned to this property name.
        /// </param>
        public void AddValue(string propertyName, object value)
        {
            if (propertyName == null)
                throw new ArgumentNullException("The propertyName parameter may not be NULL.");

            if (_dict.ContainsKey(propertyName))
                throw new ArgumentException("Attempt to add duplicate value for the '" + propertyName + "' property.");

            _dict.Add(propertyName, value);
        }

        /// <summary>
        ///     Adds values from the specified valueList to this instance of DynamicViewBag.
        /// </summary>
        /// <param name="valueList">
        ///     A list of objects.  Each must have a public property of keyPropertyName.
        /// </param>
        /// <param name="keyPropertyName">
        ///     The property name that will be retrieved for each object in the specified valueList
        ///     and used as the key (property name) for the ViewBag.  This property must be of type string.
        /// </param>
        public void AddListValues(IList valueList, string keyPropertyName)
        {
            foreach (var value in valueList)
            {
                if (value == null)
                    throw new ArgumentNullException("Invalid NULL value in initializer list.");

                var type = value.GetType();
                object objKey = type.GetProperty(keyPropertyName);

                if (objKey.GetType() != typeof (string))
                    throw new ArgumentNullException("The keyPropertyName property must be of type string.");

                var strKey = (string) objKey;

                if (_dict.ContainsKey(strKey))
                    throw new ArgumentException("Attempt to add duplicate value for the '" + strKey + "' property.");

                _dict.Add(strKey, value);
            }
        }

        /// <summary>
        ///     Adds values from the specified valueDictionary to this instance of DynamicViewBag.
        /// </summary>
        /// <param name="valueDictionary">
        ///     A dictionary of objects.  The Key of each item in the dictionary will be used
        ///     as the key (property name) for the ViewBag.
        /// </param>
        public void AddDictionaryValues(IDictionary valueDictionary)
        {
            foreach (var objKey in valueDictionary.Keys)
            {
                if (objKey.GetType() != typeof (string))
                    throw new ArgumentNullException("The Key in valueDictionary must be of type string.");

                var strKey = (string) objKey;

                if (_dict.ContainsKey(strKey))
                    throw new ArgumentException("Attempt to add duplicate value for the '" + strKey + "' property.");

                var value = valueDictionary[strKey];

                _dict.Add(strKey, value);
            }
        }

        /// <summary>
        ///     Adds values from the specified valueDictionary to this instance of DynamicViewBag.
        /// </summary>
        /// <param name="valueDictionary">
        ///     A generic dictionary of {string, object} objects.  The Key of each item in the
        ///     dictionary will be used as the key (property name) for the ViewBag.
        /// </param>
        /// <remarks>
        ///     This method was intentionally not overloaded from AddDictionaryValues due to an ambiguous
        ///     signature when the caller passes in a Dictionary&lt;string, object&gt; as the valueDictionary.
        ///     This is because the Dictionary&lt;TK, TV&gt;() class implements both IDictionary and IDictionary&lt;TK, TV&gt;.
        ///     A Dictionary&lt;string, ???&gt; (any other type than object) will resolve to AddDictionaryValues.
        ///     This is specifically for a generic List&lt;string, object&gt;, which does not resolve to
        ///     an IDictionary interface.
        /// </remarks>
        public void AddDictionaryValuesEx(IDictionary<string, object> valueDictionary)
        {
            foreach (var strKey in valueDictionary.Keys)
            {
                if (_dict.ContainsKey(strKey))
                    throw new ArgumentException("Attempt to add duplicate value for the '" + strKey + "' property.");

                var value = valueDictionary[strKey];

                _dict.Add(strKey, value);
            }
        }

        #endregion
    }
}